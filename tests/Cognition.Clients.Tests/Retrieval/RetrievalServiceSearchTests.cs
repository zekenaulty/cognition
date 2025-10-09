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
using Cognition.Data.Vectors.OpenSearch.OpenSearch.Store;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Xunit;

namespace Cognition.Clients.Tests.Retrieval;

public class RetrievalServiceSearchTests
{
    [Fact]
    public async Task SearchAsync_ShouldQueryConversationThenAgentAndDeduplicate()
    {
        var embedding = new[] { 0.1f, 0.2f };
        var embeddingsClient = new EmbeddingsClientStub(embedding);
        var vectorStore = new VectorStoreStub();
        vectorStore.EnqueueResult(new List<SearchResult>
        {
            new() { Item = new VectorItem { Id = "conv", Text = "Conversation" }, Score = 0.9 }
        });
        vectorStore.EnqueueResult(new List<SearchResult>
        {
            new() { Item = new VectorItem { Id = "conv", Text = "Duplicate" }, Score = 0.8 },
            new() { Item = new VectorItem { Id = "agent", Text = "Agent" }, Score = 0.7 }
        });

        var service = CreateService(vectorStore, embeddingsClient);
        var scope = new ScopeToken(Guid.NewGuid(), null, null, Guid.NewGuid(), Guid.NewGuid(), null, null);
        var filters = new Dictionary<string, object?> { ["Existing"] = "value", ["NullValue"] = null };

        var results = await service.SearchAsync(scope, "query", k: 2, filters);

        results.Should().HaveCount(2);
        results.Select(r => r.Id).Should().ContainInOrder("conv", "agent");

        vectorStore.Calls.Should().HaveCount(2);
        vectorStore.Calls[0].TenantKey.Should().Be(scope.TenantId!.Value.ToString());
        vectorStore.Calls[0].Filters.Should().ContainKey("Existing");
        vectorStore.Calls[0].Filters.Should().ContainKey("ConversationId");
        vectorStore.Calls[1].Filters.Should().ContainKey("AgentId");
    }

    [Fact]
    public async Task SearchAsync_ShouldReturnEmpty_WhenEmbeddingIsEmpty()
    {
        var embeddingsClient = new EmbeddingsClientStub(Array.Empty<float>());
        var vectorStore = new VectorStoreStub();
        var service = CreateService(vectorStore, embeddingsClient);

        var result = await service.SearchAsync(new ScopeToken(null, null, null, null, null, null, null), "query");

        result.Should().BeEmpty();
        vectorStore.Calls.Should().BeEmpty();
    }

    [Fact]
    public async Task SearchAsync_ShouldQueryAgentWhenConversationMissing()
    {
        var embeddingsClient = new EmbeddingsClientStub(new[] { 0.4f, 0.5f });
        var vectorStore = new VectorStoreStub();
        vectorStore.EnqueueResult(new List<SearchResult>
        {
            new() { Item = new VectorItem { Id = "agent", Text = "Agent" }, Score = 0.6 }
        });

        var service = CreateService(vectorStore, embeddingsClient);
        var scope = new ScopeToken(null, Guid.NewGuid(), null, Guid.NewGuid(), null, null, null);

        var result = await service.SearchAsync(scope, "query", k: 1);

        result.Should().HaveCount(1);
        vectorStore.Calls.Should().HaveCount(1);
        vectorStore.Calls[0].Filters.Should().ContainKey("AgentId");
        vectorStore.Calls[0].Filters.Should().NotContainKey("ConversationId");
    }

    private static RetrievalService CreateService(VectorStoreStub store, EmbeddingsClientStub embeddings)
    {
        var options = Options.Create(new OpenSearchVectorsOptions
        {
            UseEmbeddingPipeline = true,
            DefaultIndex = "vectors-test"
        });
        var logger = LoggerFactory.Create(b => { }).CreateLogger<RetrievalService>();
        return new RetrievalService(store, options, logger, embeddings);
    }

    private sealed class EmbeddingsClientStub : IEmbeddingsClient
    {
        private readonly float[] _vector;

        public EmbeddingsClientStub(float[] vector) => _vector = vector;

        public Task<float[]> EmbedAsync(string text, CancellationToken ct = default)
            => Task.FromResult(_vector);
    }

    private sealed class VectorStoreStub : IVectorStore
    {
        private readonly Queue<IReadOnlyList<SearchResult>> _responses = new();

        public List<SimilarityCall> Calls { get; } = new();

        public void EnqueueResult(IReadOnlyList<SearchResult> results) => _responses.Enqueue(results);

        public Task<IReadOnlyList<SearchResult>> SimilaritySearchAsync(float[] queryEmbedding, int topK, string tenantKey,
            IDictionary<string, object>? filters, string? kind, CancellationToken ct)
        {
            Calls.Add(new SimilarityCall(queryEmbedding.ToArray(), topK, tenantKey,
                filters != null ? new Dictionary<string, object>(filters) : new Dictionary<string, object>(), kind));
            var result = _responses.Count > 0 ? _responses.Dequeue() : Array.Empty<SearchResult>();
            return Task.FromResult(result);
        }

        public record SimilarityCall(float[] Embedding, int TopK, string TenantKey, Dictionary<string, object> Filters, string? Kind);

        #region Unused members
        public Task EnsureProvisionedAsync(CancellationToken ct) => Task.CompletedTask;
        public Task UpsertAsync(VectorItem item, CancellationToken ct) => throw new NotSupportedException();
        public Task UpsertManyAsync(IEnumerable<VectorItem> items, CancellationToken ct) => throw new NotSupportedException();
        public Task DeleteAsync(string id, string tenantKey, string? kind, CancellationToken ct) => throw new NotSupportedException();
        #endregion
    }
}
