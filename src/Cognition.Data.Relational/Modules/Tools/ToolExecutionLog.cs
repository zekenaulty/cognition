using Cognition.Data.Relational.Modules.Common;

namespace Cognition.Data.Relational.Modules.Tools;

public class ToolExecutionLog : BaseEntity
{
    public Guid ToolId { get; set; }
    public Tool Tool { get; set; } = null!;

    public Guid? AgentId { get; set; }
    public Modules.Agents.Agent? Agent { get; set; }

    public DateTime StartedAtUtc { get; set; } = DateTime.UtcNow;
    public int DurationMs { get; set; }
    public bool Success { get; set; }
    public Dictionary<string, object?>? Request { get; set; }
    public Dictionary<string, object?>? Response { get; set; }
    public string? Error { get; set; }
}
