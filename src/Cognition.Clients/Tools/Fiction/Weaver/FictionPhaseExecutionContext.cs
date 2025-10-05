using System;
using System.Collections.Generic;

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
    public static FictionPhaseExecutionContext ForPlan(Guid planId, Guid agentId, Guid conversationId, string branchSlug = "main")
        => new(planId, agentId, conversationId, branchSlug);
}