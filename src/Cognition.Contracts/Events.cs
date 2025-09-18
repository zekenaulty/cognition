using System;
using System.Collections.Generic;

namespace Cognition.Contracts.Events
{
    public record UserMessageAppended(Guid ConversationId, Guid PersonaId, string Input);
    public record PlanRequested(Guid ConversationId, Guid PersonaId, string Input);
    public record PlanReady(Guid ConversationId, Guid PersonaId, ToolPlan Plan);
    public record ToolExecutionRequested(Guid ConversationId, Guid PersonaId, Guid ToolId, Dictionary<string, object?> Args);
    public record ToolExecutionCompleted(Guid ConversationId, Guid PersonaId, Guid ToolId, object? Result, bool Success, string? Error);
    public record AssistantMessageAppended(Guid ConversationId, Guid PersonaId, string Content);

    // ToolPlan definition (minimal for now)
    public record ToolPlan(string Json);
}
