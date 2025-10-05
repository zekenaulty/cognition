using System;
using System.Collections.Generic;
using Cognition.Data.Relational.Modules.Fiction;

namespace Cognition.Clients.Tools.Fiction.Weaver;

public record FictionPhaseResult(
    FictionPhase Phase,
    FictionPhaseStatus Status,
    string? Summary = null,
    IReadOnlyDictionary<string, object?>? Data = null,
    Exception? Exception = null,
    IReadOnlyList<FictionPhaseTranscript>? Transcripts = null)
{
    public static FictionPhaseResult Success(
        FictionPhase phase,
        string? summary = null,
        IReadOnlyDictionary<string, object?>? data = null,
        IReadOnlyList<FictionPhaseTranscript>? transcripts = null)
        => new(phase, FictionPhaseStatus.Completed, summary, data, null, transcripts ?? Array.Empty<FictionPhaseTranscript>());

    public static FictionPhaseResult Skipped(
        FictionPhase phase,
        string? summary = null,
        IReadOnlyDictionary<string, object?>? data = null)
        => new(phase, FictionPhaseStatus.Skipped, summary, data, null, Array.Empty<FictionPhaseTranscript>());

    public static FictionPhaseResult Pending(
        FictionPhase phase,
        string? summary = null,
        IReadOnlyDictionary<string, object?>? data = null)
        => new(phase, FictionPhaseStatus.Pending, summary, data, null, Array.Empty<FictionPhaseTranscript>());

    public static FictionPhaseResult NotImplemented(
        FictionPhase phase,
        string? summary = null,
        IReadOnlyDictionary<string, object?>? data = null)
        => new(phase, FictionPhaseStatus.NotImplemented, summary, data, null, Array.Empty<FictionPhaseTranscript>());

    public static FictionPhaseResult Failed(
        FictionPhase phase,
        string summary,
        Exception exception,
        IReadOnlyDictionary<string, object?>? data = null,
        IReadOnlyList<FictionPhaseTranscript>? transcripts = null)
        => new(phase, FictionPhaseStatus.Failed, summary, data, exception, transcripts ?? Array.Empty<FictionPhaseTranscript>());

    public static FictionPhaseResult Cancelled(
        FictionPhase phase,
        string? summary = null,
        IReadOnlyDictionary<string, object?>? data = null)
        => new(phase, FictionPhaseStatus.Cancelled, summary, data, null, Array.Empty<FictionPhaseTranscript>());
}

public record FictionPhaseTranscript(
    Guid? AgentId,
    Guid? ConversationId,
    Guid? ConversationMessageId,
    Guid? ChapterBlueprintId,
    Guid? ChapterScrollId,
    Guid? ChapterSceneId,
    int Attempt,
    bool IsRetry,
    string? RequestPayload,
    string? ResponsePayload,
    int? PromptTokens,
    int? CompletionTokens,
    double? LatencyMs,
    FictionTranscriptValidationStatus ValidationStatus,
    string? ValidationDetails,
    IReadOnlyDictionary<string, object?>? Metadata = null);
