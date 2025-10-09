using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cognition.Clients.LLM;
using Cognition.Clients.Retrieval;
using Cognition.Contracts;
using Cognition.Data.Vectors.OpenSearch.OpenSearch.Configuration;
using Cognition.Data.Vectors.OpenSearch.OpenSearch.Models;
using Cognition.Testing.LLM;
using Cognition.Testing.Retrieval;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Cognition.Clients.Tests.Retrieval;

public class RetrievalServiceSearchTests_ConversationThenAgent
{
    [Fact]
    public async Task SearchAsync_prefers_conversation_then_falls_back_to_agent()
    {
        var agentId = Guid.NewGuid();
        var conversationId = Guid.NewGuid();
        var otherConversationId = Guid.NewGuid();

        var store = new InMemoryVectorStore();
        await store.UpsertAsync(new VectorItem
        {
            Id = "conv-item",
            Kind = "conversation",
            TenantKey = "default",
            Text = "Conversation scoped",
            Metadata = new Dictionary<string, object>
            {
                ["AgentId"] = agentId.ToString(),
                ["ConversationId"] = conversationId.ToString()
            }
        }, CancellationToken.None);

        await store.UpsertAsync(new VectorItem
        {
            Id = "other-conv",
            Kind = "conversation",
            TenantKey = "default",
            Text = "Other conversation",
            Metadata = new Dictionary<string, object>
            {
                ["AgentId"] = agentId.ToString(),
                ["ConversationId"] = otherConversationId.ToString()
            }
        }, CancellationToken.None);

        await store.UpsertAsync(new VectorItem
        {
            Id = "agent-item",
            Kind = "agent",
            TenantKey = "default",
            Text = "Agent scoped",
            Metadata = new Dictionary<string, object>
            {
                ["AgentId"] = agentId.ToString()
            }
        }, CancellationToken.None);

        var embeddings = new ScriptedEmbeddingsClient().When(_ => true, new[] { 0.5f, 0.5f });
        var service = CreateService(store, embeddings);
        var scope = new ScopeToken(null, null, null, agentId, conversationId, null, null);

        var first = await service.SearchAsync(scope, "anything", k: 1);

        store.Calls.Should().HaveCount(1);
        store.Calls[0].Filters.Should().ContainKey("ConversationId");
        first.Should().ContainSingle(x => x.Id == "conv-item");

        store.Calls.Clear();
        await store.DeleteAsync("conv-item", "default", "conversation", CancellationToken.None);

        var second = await service.SearchAsync(scope, "anything", k: 1);

        store.Calls.Should().HaveCount(2);
        store.Calls[1].Filters.Should().ContainKey("AgentId");
        second.Should().ContainSingle(x => x.Id == "agent-item");
        second.Select(x => x.Id).Should().NotContain("other-conv");
    }

    private static RetrievalService CreateService(InMemoryVectorStore store, IEmbeddingsClient embeddings)
    {
        var options = Options.Create(new OpenSearchVectorsOptions
        {
            UseEmbeddingPipeline = true,
            DefaultIndex = "vectors-test"
        });

        return new RetrievalService(store, options, NullLogger<RetrievalService>.Instance, embeddings);
    }
}
