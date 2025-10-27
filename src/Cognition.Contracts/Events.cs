using System;
using System.Collections.Generic;

namespace Cognition.Contracts.Events
{
    public record UserMessageAppended(Guid ConversationId, Guid PersonaId, string Input);

    public record PlanRequested(
        Guid ConversationId,
        Guid AgentId,
        Guid PersonaId,
        Guid ProviderId,
        Guid? ModelId,
        string Input,
        int MinSteps,
        int MaxSteps,
        Guid FictionPlanId,
        string BranchSlug,
        Dictionary<string, object?>? Metadata = null);

    public record PlanReady(
        Guid ConversationId,
        Guid AgentId,
        Guid PersonaId,
        Guid ProviderId,
        Guid? ModelId,
        ToolPlan Plan,
        Guid ConversationPlanId,
        Guid FictionPlanId,
        string BranchSlug,
        Dictionary<string, object?>? Metadata = null);

    public record ToolExecutionRequested(
        Guid ConversationId,
        Guid AgentId,
        Guid PersonaId,
        string Tool,
        Dictionary<string, object?> Args,
        Guid? ConversationPlanId = null,
        int StepNumber = 0,
        Guid? FictionPlanId = null,
        string BranchSlug = "main",
        Dictionary<string, object?>? Metadata = null);

    public record ToolExecutionCompleted(
        Guid ConversationId,
        Guid AgentId,
        Guid PersonaId,
        string Tool,
        object? Result,
        bool Success,
        string? Error,
        Guid? ConversationPlanId = null,
        int StepNumber = 0,
        Guid? FictionPlanId = null,
        string BranchSlug = "main",
        Dictionary<string, object?>? Metadata = null);

    public record FictionPhaseProgressed(
        Guid PlanId,
        Guid ConversationId,
        Guid AgentId,
        string BranchSlug,
        string Phase,
        string Status,
        string? Summary,
        Dictionary<string, object?>? Payload,
        string? BacklogItemId = null);

    public record AssistantMessageAppended(Guid ConversationId, Guid PersonaId, string Content);

    public record ToolPlan(string Json);
}
