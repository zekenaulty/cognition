using System;
using Cognition.Contracts;
using Cognition.Contracts.Scopes;

namespace Cognition.Clients.Scope;

/// <summary>
/// Centralised helper for constructing canonical <see cref="ScopePath"/> instances that keeps
/// scope identity logic in one place for clients, tools, and jobs.
/// </summary>
public interface IScopePathBuilder
{
    /// <summary>
    /// Builds a canonical <see cref="ScopePath"/> for the provided <see cref="ScopeToken"/>.
    /// </summary>
    ScopePath Build(ScopeToken scopeToken);

    /// <summary>
    /// Attempts to construct a path for the identifiers, returning <c>false</c> when the inputs
    /// do not yield a meaningful principal or segments.
    /// </summary>
    bool TryBuild(
        ScopeToken scopeToken,
        out ScopePath scopePath);

    /// <summary>
    /// Attempts to construct a path directly from individual identifiers.
    /// </summary>
    bool TryBuild(
        Guid? tenantId,
        Guid? appId,
        Guid? personaId,
        Guid? agentId,
        Guid? conversationId,
        Guid? planId,
        Guid? projectId,
        Guid? worldId,
        out ScopePath scopePath);

    /// <summary>
    /// Adds a segment to the supplied path, returning the existing instance if the segment is empty.
    /// </summary>
    ScopePath AppendSegment(ScopePath scopePath, ScopeSegment segment);

    /// <summary>
    /// Adds a segment (key + Guid value) to the supplied path, returning the existing instance if the value is empty.
    /// </summary>
    ScopePath AppendSegment(ScopePath scopePath, string key, Guid value);
}

public sealed class ScopePathBuilder : IScopePathBuilder
{
    public ScopePath Build(ScopeToken scopeToken)
    {
#pragma warning disable RS0030 // Do not use banned APIs
        return ScopePathFactory.Create(scopeToken);
#pragma warning restore RS0030 // Do not use banned APIs
    }

    public bool TryBuild(ScopeToken scopeToken, out ScopePath scopePath)
    {
#pragma warning disable RS0030 // Do not use banned APIs
        scopePath = ScopePathFactory.Create(scopeToken);
#pragma warning restore RS0030 // Do not use banned APIs
        return !IsEmpty(scopePath);
    }

    public bool TryBuild(
        Guid? tenantId,
        Guid? appId,
        Guid? personaId,
        Guid? agentId,
        Guid? conversationId,
        Guid? planId,
        Guid? projectId,
        Guid? worldId,
        out ScopePath scopePath)
    {
        scopePath = Build(new ScopeToken(
            tenantId,
            appId,
            personaId,
            agentId,
            conversationId,
            planId,
            projectId,
            worldId));

        return !IsEmpty(scopePath);
    }

    public ScopePath AppendSegment(ScopePath scopePath, ScopeSegment segment)
    {
        if (segment.IsEmpty) return scopePath;
        return scopePath.WithSegment(segment);
    }

    public ScopePath AppendSegment(ScopePath scopePath, string key, Guid value)
    {
        if (Guid.Empty.Equals(value)) return scopePath;
        return AppendSegment(scopePath, ScopeSegment.FromGuid(key, value));
    }

    private static bool IsEmpty(ScopePath path)
    {
        return path.Principal.IsEmpty && path.Segments.Count == 0;
    }
}
