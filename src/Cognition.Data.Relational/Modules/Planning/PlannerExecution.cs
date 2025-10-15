using Cognition.Data.Relational.Modules.Common;

namespace Cognition.Data.Relational.Modules.Planning;

public sealed class PlannerExecution : BaseEntity
{
    public Guid? ToolId { get; set; }
    public string PlannerName { get; set; } = string.Empty;
    public string Outcome { get; set; } = string.Empty;
    public Guid? AgentId { get; set; }
    public Guid? ConversationId { get; set; }
    public Guid? PrimaryAgentId { get; set; }
    public string? Environment { get; set; }
    public string? ScopePath { get; set; }

    public Dictionary<string, object?>? ConversationState { get; set; }
    public Dictionary<string, object?>? Artifacts { get; set; }
    public Dictionary<string, double>? Metrics { get; set; }
    public Dictionary<string, string>? Diagnostics { get; set; }
    public List<PlannerExecutionTranscriptEntry>? Transcript { get; set; }
}

public sealed class PlannerExecutionTranscriptEntry
{
    public DateTime TimestampUtc { get; set; }
    public string Role { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public Dictionary<string, object?>? Metadata { get; set; }
}
