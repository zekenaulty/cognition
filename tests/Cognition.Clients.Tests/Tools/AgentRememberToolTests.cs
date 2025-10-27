using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cognition.Clients.Configuration;
using Cognition.Clients.LLM;
using Cognition.Clients.Retrieval;
using Cognition.Clients.Scope;
using Cognition.Clients.Tools;
using Cognition.Data.Vectors.OpenSearch.OpenSearch.Configuration;
using Cognition.Testing.LLM;
using Cognition.Testing.Retrieval;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Cognition.Clients.Tests.Tools;

public class AgentRememberToolTests
{
    [Fact]
    public async Task AgentRememberTool_promotes_to_agent_scope_and_is_idempotent()
    {
        var agentId = Guid.NewGuid();
        var conversationId = Guid.NewGuid();
        var services = new ServiceCollection().BuildServiceProvider();

        var store = new InMemoryVectorStore();
        var retrieval = CreateRetrieval(store, new ScriptedEmbeddingsClient(), dualWrite: false);
        var tool = new AgentRememberTool(retrieval);
        var ctx = new ToolContext(agentId, conversationId, PersonaId: null, Services: services, Ct: CancellationToken.None);
        var args = new Dictionary<string, object?> { ["text"] = "Keep this forever" };

        var first = await tool.ExecuteAsync(ctx, args);
        var firstSnapshot = store.Snapshot();

        await tool.ExecuteAsync(ctx, args); // same content again
        var secondSnapshot = store.Snapshot();

        first.Should().BeEquivalentTo(new { ok = true });

        firstSnapshot.Should().HaveCount(1);
        var item = firstSnapshot.Single();
        item.Kind.Should().Be("agent");
        item.Metadata.Should().NotBeNull();
        item.Metadata.Should().ContainKey("AgentId");
        item.Metadata.Should().NotContainKey("ConversationId");
        item.Metadata!["Source"].Should().Be("tool_remember");

        secondSnapshot.Should().HaveCount(1);
        secondSnapshot.Single().Id.Should().Be(item.Id);
    }

    [Fact]
    public async Task AgentRememberTool_dual_write_populates_scope_metadata()
    {
        var agentId = Guid.NewGuid();
        var services = new ServiceCollection().BuildServiceProvider();
        var store = new InMemoryVectorStore();
        var retrieval = CreateRetrieval(store, new ScriptedEmbeddingsClient(), dualWrite: true);
        var tool = new AgentRememberTool(retrieval);
        var conversationId = Guid.NewGuid();
        var ctx = new ToolContext(agentId, conversationId, PersonaId: null, Services: services, Ct: CancellationToken.None);

        await tool.ExecuteAsync(ctx, new Dictionary<string, object?> { ["text"] = "Remember dual write" });

        var snapshot = store.Snapshot();
        snapshot.Should().HaveCount(1);
        var item = snapshot.Single();
        item.ScopePath.Should().NotBeNullOrWhiteSpace();
        item.ScopePrincipalType.Should().Be("agent");
        item.ScopePrincipalId.Should().Be(agentId.ToString("D"));
        item.ScopeSegments.Should().NotBeNull();
        item.Metadata.Should().ContainKey("ScopePath");
        if (item.ScopeSegments!.Count > 0)
        {
            item.Metadata.Should().ContainKey("ScopeSegments");
        }
        else
        {
            item.Metadata.Should().NotContainKey("ScopeSegments");
        }
        item.Metadata.Should().ContainKey("ScopePrincipalType");
    }

    private static RetrievalService CreateRetrieval(InMemoryVectorStore store, IEmbeddingsClient embeddings, bool dualWrite)
    {
        var options = Options.Create(new OpenSearchVectorsOptions
        {
            UseEmbeddingPipeline = true,
            DefaultIndex = "vectors-test"
        });

        var scopeOptions = Options.Create(new ScopePathOptions
        {
            PathAwareHashingEnabled = dualWrite,
            DualWriteEnabled = dualWrite
        });
        var diagnostics = new ScopePathDiagnostics();
        var logger = NullLogger<RetrievalService>.Instance;
        var scopePathBuilder = new ScopePathBuilder();
        return new RetrievalService(store, options, scopeOptions, diagnostics, scopePathBuilder, logger, embeddings);
    }
}
