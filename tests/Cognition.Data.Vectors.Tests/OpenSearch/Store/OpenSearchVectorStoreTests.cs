using System;
using System.Threading;
using System.Threading.Tasks;
using Cognition.Data.Vectors.OpenSearch.OpenSearch.Configuration;
using Cognition.Data.Vectors.OpenSearch.OpenSearch.Models;
using Cognition.Data.Vectors.OpenSearch.OpenSearch.Store;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using OpenSearch.Client;
using Xunit;

namespace Cognition.Data.Vectors.Tests.OpenSearch.Store;

public class OpenSearchVectorStoreTests
{
    [Fact]
    public async Task UpsertAsync_should_throw_when_embedding_missing_and_pipeline_disabled()
    {
        var client = Substitute.For<IOpenSearchClient>();
        var logger = Substitute.For<ILogger<OpenSearchVectorStore>>();
        var options = Options.Create(new OpenSearchVectorsOptions
        {
            UseEmbeddingPipeline = false,
            Dimension = 4,
            DefaultIndex = "vectors"
        });
        var store = new OpenSearchVectorStore(client, options, logger, provisioner: null!);

        var item = new VectorItem
        {
            Id = "doc",
            TenantKey = "tenant",
            Kind = "test",
            Text = "content",
            Embedding = null
        };

        var act = () => store.UpsertAsync(item, CancellationToken.None);

        await Assert.ThrowsAsync<ArgumentNullException>(act);
        await client.DidNotReceiveWithAnyArgs().IndexAsync<object>(default!, default!, default);
    }

    [Fact]
    public async Task UpsertAsync_should_throw_when_dimension_mismatch()
    {
        var client = Substitute.For<IOpenSearchClient>();
        var logger = Substitute.For<ILogger<OpenSearchVectorStore>>();
        var options = Options.Create(new OpenSearchVectorsOptions
        {
            UseEmbeddingPipeline = false,
            Dimension = 3,
            DefaultIndex = "vectors"
        });
        var store = new OpenSearchVectorStore(client, options, logger, provisioner: null!);

        var item = new VectorItem
        {
            Id = "doc",
            TenantKey = "tenant",
            Kind = "test",
            Text = "content",
            Embedding = new[] { 1f, 2f } // only 2 dims, expect guard to fail
        };

        var act = () => store.UpsertAsync(item, CancellationToken.None);

        await Assert.ThrowsAsync<ArgumentException>(act);
        await client.DidNotReceiveWithAnyArgs().IndexAsync<object>(default!, default!, default);
    }
}
