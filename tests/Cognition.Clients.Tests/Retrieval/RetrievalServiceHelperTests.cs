using System;
using System.Collections.Generic;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using Cognition.Clients.Retrieval;
using Cognition.Contracts;
using FluentAssertions;
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
    public void ComputeContentHash_ShouldIncludeTrimmedContentAndScope()
    {
        var scope = new ScopeToken(Guid.NewGuid(), Guid.NewGuid(), null, Guid.NewGuid(), null, null, null);
        var metadata = new Dictionary<string, object>
        {
            ["Source"] = "manual",
            ["Other"] = "ignored"
        };

        var content = "  hello world  ";
        var hash = Invoke<string>(ComputeContentHashMethod, content, scope, metadata);

        var expected = ExpectedHash(content, scope, metadata);
        hash.Should().Be(expected);
    }

    [Fact]
    public void ComputeContentHash_ShouldChange_WhenScopeDiffers()
    {
        var scopeA = new ScopeToken(Guid.NewGuid(), null, null, null, null, null, null);
        var scopeB = new ScopeToken(Guid.NewGuid(), null, null, null, null, null, null);
        var metadata = new Dictionary<string, object>();

        var hashA = Invoke<string>(ComputeContentHashMethod, "content", scopeA, metadata);
        var hashB = Invoke<string>(ComputeContentHashMethod, "content", scopeB, metadata);

        hashA.Should().NotBe(hashB);
    }

    private static string ExpectedHash(string content, ScopeToken scope, Dictionary<string, object> meta)
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

        using var sha = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(sb.ToString());
        return Convert.ToHexString(sha.ComputeHash(bytes));
    }

    private static MethodInfo GetMethod(string name)
    {
        return typeof(RetrievalService).GetMethod(name, BindingFlags.NonPublic | BindingFlags.Static)
               ?? throw new InvalidOperationException($"Method {name} not found on RetrievalService");
    }

    private static T Invoke<T>(MethodInfo method, params object?[] args)
    {
        return (T)method.Invoke(null, args)!;
    }
}



