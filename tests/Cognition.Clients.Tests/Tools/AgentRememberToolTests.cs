using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cognition.Clients.LLM;
using Cognition.Clients.Retrieval;
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
        var retrieval = CreateRetrieval(store, new ScriptedEmbeddingsClient());
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

    private static RetrievalService CreateRetrieval(InMemoryVectorStore store, IEmbeddingsClient embeddings)
    {
        var options = Options.Create(new OpenSearchVectorsOptions
        {
            UseEmbeddingPipeline = true,
            DefaultIndex = "vectors-test"
        });

        return new RetrievalService(store, options, NullLogger<RetrievalService>.Instance, embeddings);
    }
}
