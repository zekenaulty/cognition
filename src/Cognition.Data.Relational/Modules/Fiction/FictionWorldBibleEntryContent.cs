using System;

namespace Cognition.Data.Relational.Modules.Fiction;

public class FictionWorldBibleEntryContent
{
    public string Category { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string[] ContinuityNotes { get; set; } = Array.Empty<string>();
    public string Branch { get; set; } = "main";
    public int? IterationIndex { get; set; }
    public string? BacklogItemId { get; set; }
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}
