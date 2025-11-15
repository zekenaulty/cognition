using System;
using System.Collections.Generic;
using Cognition.Contracts;
using Cognition.Contracts.Scopes;

namespace Cognition.Clients.Tools.Fiction.Weaver;

public record FictionPhaseExecutionContext(
    Guid PlanId,
    Guid AgentId,
    Guid ConversationId,
    string BranchSlug,
    Guid? ChapterBlueprintId = null,
    Guid? ChapterScrollId = null,
    Guid? ChapterSceneId = null,
    int? IterationIndex = null,
    string? InvokedByJobId = null,
    IReadOnlyDictionary<string, string>? Metadata = null)
{
    public ScopeToken? ScopeToken { get; init; }

    public ScopePath? ScopePath { get; init; }

    public static FictionPhaseExecutionContext ForPlan(Guid planId, Guid agentId, Guid conversationId, string branchSlug = "main")
        => new(planId, agentId, conversationId, branchSlug);
}
