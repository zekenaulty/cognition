using Cognition.Data.Relational.Modules.Common;

namespace Cognition.Data.Relational.Modules.Fiction;

public class FictionPlanTranscript : BaseEntity
{
    public Guid FictionPlanId { get; set; }
    public FictionPlan FictionPlan { get; set; } = null!;

    public string Phase { get; set; } = string.Empty;
    public Guid? FictionChapterBlueprintId { get; set; }
    public FictionChapterBlueprint? FictionChapterBlueprint { get; set; }
    public Guid? FictionChapterSceneId { get; set; }
    public FictionChapterScene? FictionChapterScene { get; set; }

    public Guid? AgentId { get; set; }
    public Guid? ConversationId { get; set; }
    public Guid? ConversationMessageId { get; set; }

    public int Attempt { get; set; } = 1;
    public string? RequestPayload { get; set; }
    public string? ResponsePayload { get; set; }
    public int? PromptTokens { get; set; }
    public int? CompletionTokens { get; set; }
    public double? LatencyMs { get; set; }

    public FictionTranscriptValidationStatus ValidationStatus { get; set; } = FictionTranscriptValidationStatus.Unknown;
    public string? ValidationDetails { get; set; }
    public bool IsRetry { get; set; }
    public Dictionary<string, object?>? Metadata { get; set; }
}

public enum FictionTranscriptValidationStatus
{
    Unknown,
    Passed,
    Failed,
    Skipped
}
