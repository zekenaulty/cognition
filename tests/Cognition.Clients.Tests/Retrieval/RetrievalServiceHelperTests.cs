using System;
using System.Collections.Generic;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using Cognition.Clients.Configuration;
using Cognition.Clients.LLM;
using Cognition.Clients.Scope;
using Cognition.Clients.Retrieval;
using Cognition.Contracts;
using Cognition.Contracts.Scopes;
using Cognition.Data.Vectors.OpenSearch.OpenSearch.Configuration;
using Cognition.Data.Vectors.OpenSearch.OpenSearch.Models;
using Cognition.Data.Vectors.OpenSearch.OpenSearch.Store;
using Cognition.Testing.Utilities;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Cognition.Clients.Tests.Retrieval;

public class RetrievalServiceHelperTests
{
    private static readonly MethodInfo ToFilterDictionaryMethod = GetMethod("ToFilterDictionary");
    private static readonly MethodInfo ResolveTenantKeyMethod = GetMethod("ResolveTenantKey");
    private static readonly MethodInfo BuildScopeMetadataMethod = GetMethod("BuildScopeMetadata");
    private static readonly MethodInfo ComputeContentHashMethod = GetMethod("ComputeContentHash");

    [Fact]
    public void ToFilterDictionary_ShouldExcludeNullValues()
    {
        var source = new Dictionary<string, object?>
        {
            ["TenantId"] = "tenant",
            ["Empty"] = null,
            ["Number"] = 5
        };

        var result = Invoke<Dictionary<string, object>>(ToFilterDictionaryMethod, source);

        result.Should().ContainKeys("TenantId", "Number").And.NotContainKey("Empty");
    }

    [Fact]
    public void ResolveTenantKey_ShouldPreferTenantThenApp()
    {
        var scopeWithTenant = new ScopeToken(Guid.NewGuid(), Guid.NewGuid(), null, null, null, null, null);
        Invoke<string>(ResolveTenantKeyMethod, scopeWithTenant).Should().Be(scopeWithTenant.TenantId!.Value.ToString());

        var scopeWithApp = new ScopeToken(null, Guid.NewGuid(), null, null, null, null, null);
        Invoke<string>(ResolveTenantKeyMethod, scopeWithApp).Should().Be(scopeWithApp.AppId!.Value.ToString());

        var defaultScope = new ScopeToken(null, null, null, null, null, null, null);
        Invoke<string>(ResolveTenantKeyMethod, defaultScope).Should().Be("default");
    }

    [Fact]
    public void BuildScopeMetadata_ShouldCaptureScopeAndExtras()
    {
        var scope = new ScopeToken(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        var extra = new Dictionary<string, object?>
        {
            ["Custom"] = "value",
            ["AgentId"] = "override" // should override scope value
        };

        var metadata = Invoke<Dictionary<string, object>>(BuildScopeMetadataMethod, scope, extra);

        metadata["TenantId"].Should().Be(scope.TenantId!.Value.ToString());
        metadata["AgentId"].Should().Be("override");
        metadata.Should().ContainKey("Custom");
    }

    [Fact]
    public void ComputeContentHash_ShouldIncludeTrimmedContentAndScope_WhenLegacyMode()
    {
        var scope = new ScopeToken(Guid.NewGuid(), Guid.NewGuid(), null, Guid.NewGuid(), null, null, null);
        var metadata = new Dictionary<string, object>
        {
            ["Source"] = "manual",
            ["Other"] = "ignored"
        };

        var content = "  hello world  ";
        var hash = Invoke<string>(ComputeContentHashMethod, content, scope, metadata, false);

        var expected = ExpectedHashLegacy(content, scope, metadata);
        hash.Should().Be(expected);
    }

    [Fact]
    public void ComputeContentHash_ShouldChange_WhenScopeDiffers()
    {
        var scopeA = new ScopeToken(Guid.NewGuid(), null, null, null, null, null, null);
        var scopeB = new ScopeToken(Guid.NewGuid(), null, null, null, null, null, null);
        var metadata = new Dictionary<string, object>();

        var hashA = Invoke<string>(ComputeContentHashMethod, "content", scopeA, metadata, false);
        var hashB = Invoke<string>(ComputeContentHashMethod, "content", scopeB, metadata, false);

        hashA.Should().NotBe(hashB);
    }

    [Fact]
    public void ComputeContentHash_ShouldUseCanonicalPath_WhenPathAwareEnabled()
    {
        var tenantId = Guid.NewGuid();
        var agentId = Guid.NewGuid();
        var conversationId = Guid.NewGuid();
        var scope = new ScopeToken(tenantId, null, null, agentId, conversationId, null, null);
        var metadata = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
        {
            ["Source"] = "path-aware"
        };

        var hash = Invoke<string>(ComputeContentHashMethod, "content", scope, metadata, true);

        var builder = ScopePathBuilderTestHelper.CreateBuilder();
        var path = builder.Build(scope);
        var expectedInput = new StringBuilder()
            .AppendLine("content")
            .Append("|principal=").Append(path.Principal.Canonical);

        foreach (var segment in path.Segments)
        {
            expectedInput.Append('|').Append(segment.Canonical);
        }

        if (metadata.TryGetValue("Source", out var sourceValue) && sourceValue is not null)
        {
            expectedInput.Append("|Source=").Append(sourceValue);
        }

        var expectedHash = Sha(expectedInput.ToString());
        hash.Should().Be(expectedHash);
    }

    [Fact]
    public void ComputeContentHash_InvokesScopePathBuilder_WhenPathAwareEnabled()
    {
        var builder = new RecordingScopePathBuilder();
        var service = CreateService(builder);
        var scope = new ScopeToken(Guid.NewGuid(), null, null, Guid.NewGuid(), Guid.NewGuid(), null, null);
        var metadata = new Dictionary<string, object>();

        var hash = (string)ComputeContentHashMethod.Invoke(service, new object[] { "payload", scope, metadata, true })!;

        hash.Should().NotBeNullOrEmpty();
        builder.BuildCalls.Should().Be(1);
    }

    [Fact]
    public void ComputeContentHash_DoesNotInvokeScopePathBuilder_WhenPathAwareDisabled()
    {
        var builder = new RecordingScopePathBuilder();
        var service = CreateService(builder);
        var scope = new ScopeToken(Guid.NewGuid(), null, null, Guid.NewGuid(), null, null, null);
        var metadata = new Dictionary<string, object>();

        _ = (string)ComputeContentHashMethod.Invoke(service, new object[] { "payload", scope, metadata, false })!;

        builder.BuildCalls.Should().Be(0);
    }

    private static string ExpectedHashLegacy(string content, ScopeToken scope, Dictionary<string, object> meta)
    {
        var sb = new StringBuilder();
        sb.AppendLine(content.Trim());

        void Add(string key, object? value)
        {
            if (value is null) return;
            sb.Append('|').Append(key).Append('=').Append(value);
        }

        Add("TenantId", scope.TenantId);
        Add("AppId", scope.AppId);
        Add("AgentId", scope.AgentId);
        Add("ConversationId", scope.ConversationId);
        Add("ProjectId", scope.ProjectId);
        Add("WorldId", scope.WorldId);
        if (meta.TryGetValue("Source", out var src))
        {
            Add("Source", src);
        }

        return Sha(sb.ToString());
    }

    private static string Sha(string input)
    {
        using var sha = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(input);
        return Convert.ToHexString(sha.ComputeHash(bytes));
    }

    private static MethodInfo GetMethod(string name)
    {
        return typeof(RetrievalService).GetMethod(
                   name,
                   BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance)
               ?? throw new InvalidOperationException($"Method {name} not found on RetrievalService");
    }

    private static T Invoke<T>(MethodInfo method, params object?[] args)
    {
        var target = method.IsStatic ? null : CreateService();
        return (T)method.Invoke(target, args)!;
    }

    private static RetrievalService CreateService(IScopePathBuilder? builder = null)
    {
        var store = new NullVectorStore();
        var options = Options.Create(new OpenSearchVectorsOptions { UseEmbeddingPipeline = true });
        var scopeOptions = Options.Create(new ScopePathOptions());
        var diagnostics = new ScopePathDiagnostics();
        var scopePathBuilder = builder ?? ScopePathBuilderTestHelper.CreateBuilder();
        var logger = NullLogger<RetrievalService>.Instance;
        var embeddings = new NullEmbeddingsClient();
        return new RetrievalService(store, options, scopeOptions, diagnostics, scopePathBuilder, logger, embeddings);
    }

    private sealed class NullVectorStore : IVectorStore
    {
        public Task EnsureProvisionedAsync(CancellationToken ct) => Task.CompletedTask;
        public Task UpsertAsync(VectorItem item, CancellationToken ct) => Task.CompletedTask;
        public Task UpsertManyAsync(IEnumerable<VectorItem> items, CancellationToken ct) => Task.CompletedTask;
        public Task DeleteAsync(string id, string tenantKey, string? kind, CancellationToken ct) => Task.CompletedTask;
        public Task<IReadOnlyList<SearchResult>> SimilaritySearchAsync(
            float[] queryEmbedding,
            int topK,
            string tenantKey,
            IDictionary<string, object>? filters,
            string? kind,
            CancellationToken ct)
            => Task.FromResult<IReadOnlyList<SearchResult>>(Array.Empty<SearchResult>());
    }

    private sealed class NullEmbeddingsClient : IEmbeddingsClient
    {
        public Task<float[]> EmbedAsync(string text, CancellationToken ct = default)
            => Task.FromResult(Array.Empty<float>());
    }

    private sealed class RecordingScopePathBuilder : IScopePathBuilder
    {
        private readonly ScopePathBuilder _inner = new();

        public int BuildCalls { get; private set; }
        public int TryBuildCalls { get; private set; }

        public ScopePath Build(ScopeToken scopeToken)
        {
            BuildCalls++;
            return _inner.Build(scopeToken);
        }

        public bool TryBuild(ScopeToken scopeToken, out ScopePath scopePath)
        {
            TryBuildCalls++;
            return _inner.TryBuild(scopeToken, out scopePath);
        }

        public bool TryBuild(
            Guid? tenantId,
            Guid? appId,
            Guid? personaId,
            Guid? agentId,
            Guid? conversationId,
            Guid? projectId,
            Guid? worldId,
            out ScopePath scopePath)
        {
            TryBuildCalls++;
            return _inner.TryBuild(tenantId, appId, personaId, agentId, conversationId, projectId, worldId, out scopePath);
        }

        public ScopePath AppendSegment(ScopePath scopePath, ScopeSegment segment)
            => _inner.AppendSegment(scopePath, segment);

        public ScopePath AppendSegment(ScopePath scopePath, string key, Guid value)
            => _inner.AppendSegment(scopePath, key, value);
    }
}



