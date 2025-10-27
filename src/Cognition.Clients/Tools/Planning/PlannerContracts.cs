using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cognition.Clients.Tools;
using Cognition.Contracts.Scopes;
using Microsoft.Extensions.Logging;

namespace Cognition.Clients.Tools.Planning;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public sealed class PlannerCapabilitiesAttribute : Attribute
{
    public PlannerCapabilitiesAttribute(params string[] capabilities)
    {
        Capabilities = capabilities ?? Array.Empty<string>();
    }

    public IReadOnlyList<string> Capabilities { get; }
}

public enum PlannerOutcome
{
    Success,
    Partial,
    Failed,
    Cancelled
}

public enum PlannerStepStatus
{
    Completed,
    Skipped,
    Failed
}

public interface IPlannerTool : ITool
{
    PlannerMetadata Metadata { get; }
    Task<PlannerResult> PlanAsync(PlannerContext context, PlannerParameters parameters, CancellationToken ct = default);
}

public sealed record PlannerStepDescriptor(
    string Id,
    string DisplayName,
    string? Purpose = null,
    IReadOnlyList<string>? InputKeys = null,
    IReadOnlyList<string>? OutputKeys = null,
    string? TemplateId = null);

public sealed record PlannerMetadata(
    string Name,
    string Description,
    IReadOnlyList<string> Capabilities,
    IReadOnlyList<PlannerStepDescriptor> Steps,
    IReadOnlyDictionary<string, object?>? DefaultSettings = null,
    IReadOnlyDictionary<string, string>? TelemetryTags = null,
    PlannerCritiqueProfile? CritiqueProfile = null)
{
    public static PlannerMetadata Create(
        string name,
        string description,
        IEnumerable<string>? capabilities = null,
        IEnumerable<PlannerStepDescriptor>? steps = null,
        IReadOnlyDictionary<string, object?>? defaultSettings = null,
        IReadOnlyDictionary<string, string>? telemetryTags = null,
        PlannerCritiqueProfile? critiqueProfile = null)
    {
        return new PlannerMetadata(
            name,
            description,
            capabilities is null ? Array.Empty<string>() : new ReadOnlyCollection<string>(capabilities.ToList()),
            steps is null ? Array.Empty<PlannerStepDescriptor>() : new ReadOnlyCollection<PlannerStepDescriptor>(steps.ToList()),
            defaultSettings,
            telemetryTags,
            critiqueProfile ?? PlannerCritiqueProfile.Disabled);
    }
}

public sealed record PlannerCritiqueProfile(
    bool Enabled,
    PlannerCritiqueBudget Budget,
    IReadOnlyCollection<Guid>? PersonaAllowList = null)
{
    public static PlannerCritiqueProfile Disabled { get; } = new(false, new PlannerCritiqueBudget(), Array.Empty<Guid>());

    public bool AllowsPersona(Guid? personaId)
    {
        if (!Enabled)
        {
            return false;
        }

        if (PersonaAllowList is null || PersonaAllowList.Count == 0)
        {
            return true;
        }

        return personaId.HasValue && PersonaAllowList.Contains(personaId.Value);
    }
}

public sealed record PlannerStepRecord(
    string StepId,
    PlannerStepStatus Status,
    IReadOnlyDictionary<string, object?> Output,
    TimeSpan Duration);

public enum PlannerBacklogStatus
{
    Pending,
    InProgress,
    Complete
}

public sealed record PlannerBacklogItem(
    string Id,
    string Description,
    PlannerBacklogStatus Status,
    IReadOnlyList<string> Inputs,
    IReadOnlyList<string> Outputs);

public sealed record PlannerTranscriptEntry(
    DateTime TimestampUtc,
    string Role,
    string Message,
    IReadOnlyDictionary<string, object?>? Metadata = null);

public sealed class PlannerResult
{
    private readonly Dictionary<string, object?> _artifacts = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<PlannerStepRecord> _steps = new();
    private readonly List<PlannerTranscriptEntry> _transcript = new();
    private readonly Dictionary<string, double> _metrics = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, string> _diagnostics = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<PlannerBacklogItem> _backlog = new();

    private PlannerResult(PlannerOutcome outcome)
    {
        Outcome = outcome;
    }

    public PlannerOutcome Outcome { get; }
    public IReadOnlyDictionary<string, object?> Artifacts => _artifacts;
    public IReadOnlyList<PlannerStepRecord> Steps => _steps;
    public IReadOnlyList<PlannerTranscriptEntry> Transcript => _transcript;
    public IReadOnlyDictionary<string, double> Metrics => _metrics;
    public IReadOnlyDictionary<string, string> Diagnostics => _diagnostics;
    public IReadOnlyList<PlannerBacklogItem> Backlog => _backlog;

    public static PlannerResult Success() => new(PlannerOutcome.Success);

    public static PlannerResult FromOutcome(PlannerOutcome outcome) => new(outcome);

    public PlannerResult AddArtifact(string key, object? value)
    {
        _artifacts[key] = value;
        return this;
    }

    public PlannerResult AddArtifacts(IEnumerable<KeyValuePair<string, object?>> artifacts)
    {
        foreach (var kv in artifacts)
        {
            _artifacts[kv.Key] = kv.Value;
        }
        return this;
    }

    public PlannerResult SetBacklog(IEnumerable<PlannerBacklogItem> items)
    {
        _backlog.Clear();
        _backlog.AddRange(items);
        return this;
    }

    public PlannerResult AddBacklog(params PlannerBacklogItem[] items)
    {
        _backlog.AddRange(items);
        return this;
    }

    public PlannerResult AddStep(PlannerStepRecord step)
    {
        _steps.Add(step);
        return this;
    }

    public PlannerResult AddTranscript(IEnumerable<PlannerTranscriptEntry> entries)
    {
        _transcript.AddRange(entries);
        return this;
    }

    public PlannerResult AddTranscript(params PlannerTranscriptEntry[] entries)
        => AddTranscript((IEnumerable<PlannerTranscriptEntry>)entries);

    public PlannerResult AddMetric(string name, double value)
    {
        _metrics[name] = value;
        return this;
    }

    public PlannerResult AddDiagnostics(string key, string value)
    {
        _diagnostics[key] = value;
        return this;
    }

    public bool TryGetArtifact<T>(string key, out T? value)
    {
        if (_artifacts.TryGetValue(key, out var raw) && raw is T typed)
        {
            value = typed;
            return true;
        }

        value = default;
        return false;
    }

    public Dictionary<string, object?> ToDictionary() => new(StringComparer.OrdinalIgnoreCase)
    {
        ["outcome"] = Outcome.ToString(),
        ["artifacts"] = _artifacts,
        ["steps"] = _steps.Select(s => new Dictionary<string, object?>
        {
            ["id"] = s.StepId,
            ["status"] = s.Status.ToString(),
            ["durationMs"] = s.Duration.TotalMilliseconds,
            ["output"] = s.Output
        }).ToList(),
        ["transcript"] = _transcript.Select(t => new Dictionary<string, object?>
        {
            ["timestampUtc"] = t.TimestampUtc,
            ["role"] = t.Role,
            ["message"] = t.Message,
            ["metadata"] = t.Metadata
        }).ToList(),
        ["metrics"] = _metrics,
        ["diagnostics"] = _diagnostics,
        ["backlog"] = _backlog.Select(item => new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
        {
            ["id"] = item.Id,
            ["description"] = item.Description,
            ["status"] = item.Status.ToSerializedString(),
            ["inputs"] = item.Inputs,
            ["outputs"] = item.Outputs
        }).ToList()
    };
}

public sealed record PlannerTelemetryContext(
    Guid? ToolId,
    string PlannerName,
    IReadOnlyList<string> Capabilities,
    Guid? AgentId,
    Guid? ConversationId,
    Guid? PrimaryAgentId,
    string? Environment,
    string? ScopePath,
    bool SupportsSelfCritique,
    IReadOnlyDictionary<string, string>? TelemetryTags);

public interface IPlannerTelemetry
{
    Task PlanStartedAsync(PlannerTelemetryContext context, CancellationToken ct);
    Task PlanCompletedAsync(PlannerTelemetryContext context, PlannerResult result, CancellationToken ct);
    Task PlanFailedAsync(PlannerTelemetryContext context, Exception exception, CancellationToken ct);
}

public sealed class LoggerPlannerTelemetry : IPlannerTelemetry
{
    private readonly ILogger<LoggerPlannerTelemetry> _logger;

    public LoggerPlannerTelemetry(ILogger<LoggerPlannerTelemetry> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task PlanStartedAsync(PlannerTelemetryContext context, CancellationToken ct)
    {
        LogEvent("planner.started", context, payload => { });
        return Task.CompletedTask;
    }

    public Task PlanCompletedAsync(PlannerTelemetryContext context, PlannerResult result, CancellationToken ct)
    {
        LogEvent("planner.completed", context, payload =>
        {
            payload["outcome"] = result.Outcome.ToString();
            payload["durationMs"] = result.Metrics.TryGetValue("durationMs", out var duration) ? duration : 0d;
            payload["metrics"] = result.Metrics;
            payload["diagnostics"] = result.Diagnostics;
            payload["steps"] = result.Steps.Select(s => new Dictionary<string, object?>
            {
                ["id"] = s.StepId,
                ["status"] = s.Status.ToString(),
                ["durationMs"] = s.Duration.TotalMilliseconds
            }).ToList();
        });
        return Task.CompletedTask;
    }

    public Task PlanFailedAsync(PlannerTelemetryContext context, Exception exception, CancellationToken ct)
    {
        LogEvent("planner.failed", context, payload =>
        {
            payload["error"] = exception.Message;
            payload["exceptionType"] = exception.GetType().FullName;
        });
        return Task.CompletedTask;
    }

    private void LogEvent(string name, PlannerTelemetryContext context, Action<Dictionary<string, object?>> enrich)
    {
        var payload = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
        {
            ["planner"] = context.PlannerName,
            ["event"] = name,
            ["toolId"] = context.ToolId,
            ["agentId"] = context.AgentId,
            ["conversationId"] = context.ConversationId,
            ["primaryAgentId"] = context.PrimaryAgentId,
            ["environment"] = context.Environment,
            ["scopePath"] = context.ScopePath,
            ["supportsSelfCritique"] = context.SupportsSelfCritique,
            ["capabilities"] = context.Capabilities,
            ["tags"] = context.TelemetryTags
        };
        enrich(payload);
        _logger.LogInformation("{EventName} {@Payload}", name, payload);
    }
}

public static class PlannerBacklogStatusExtensions
{
    public static PlannerBacklogStatus Parse(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return PlannerBacklogStatus.Pending;
        }

        return value.Trim().ToLowerInvariant() switch
        {
            "in_progress" => PlannerBacklogStatus.InProgress,
            "in-progress" => PlannerBacklogStatus.InProgress,
            "inprogress" => PlannerBacklogStatus.InProgress,
            "complete" => PlannerBacklogStatus.Complete,
            "completed" => PlannerBacklogStatus.Complete,
            _ => PlannerBacklogStatus.Pending
        };
    }

    public static string ToSerializedString(this PlannerBacklogStatus status) => status switch
    {
        PlannerBacklogStatus.InProgress => "in_progress",
        PlannerBacklogStatus.Complete => "complete",
        _ => "pending"
    };
}

public sealed record PlannerContext(
    ToolContext ToolContext,
    Guid? ToolId,
    ScopePath? ScopePath,
    Guid? PrimaryAgentId,
    IReadOnlyDictionary<string, object?> ConversationState,
    string? Environment,
    bool SupportsSelfCritique)
{
    public static PlannerContext FromToolContext(
        ToolContext toolContext,
        ScopePath? scopePath = null,
        Guid? primaryAgentId = null,
        IReadOnlyDictionary<string, object?>? conversationState = null,
        string? environment = null,
        bool supportsSelfCritique = false)
        => new(toolContext, null, scopePath, primaryAgentId, conversationState ?? new Dictionary<string, object?>(), environment, supportsSelfCritique);

    public static PlannerContext FromToolContext(
        ToolContext toolContext,
        Guid? toolId,
        ScopePath? scopePath = null,
        Guid? primaryAgentId = null,
        IReadOnlyDictionary<string, object?>? conversationState = null,
        string? environment = null,
        bool supportsSelfCritique = false)
        => new(toolContext, toolId, scopePath, primaryAgentId, conversationState ?? new Dictionary<string, object?>(), environment, supportsSelfCritique);
}

public class PlannerParameters
{
    private readonly Dictionary<string, object?> _values;

    public PlannerParameters(IDictionary<string, object?>? values = null)
    {
        _values = values is null
            ? new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, object?>(values, StringComparer.OrdinalIgnoreCase);
    }

    public IReadOnlyDictionary<string, object?> AsReadOnlyDictionary() => _values;

    public T? Get<T>(string key)
    {
        if (!_values.TryGetValue(key, out var value) || value is null)
        {
            return default;
        }
        return (T)value;
    }

    public PlannerParameters Set(string key, object? value)
    {
        _values[key] = value;
        return this;
    }
}

public interface IPlannerTranscriptStore
{
    Task StoreAsync(PlannerContext context, PlannerMetadata metadata, PlannerResult result, CancellationToken ct);
}

public sealed class NullPlannerTranscriptStore : IPlannerTranscriptStore
{
    public Task StoreAsync(PlannerContext context, PlannerMetadata metadata, PlannerResult result, CancellationToken ct)
        => Task.CompletedTask;
}

public interface IPlannerTemplateRepository
{
    Task<string?> GetTemplateAsync(string templateId, CancellationToken ct);
}

public sealed class NullPlannerTemplateRepository : IPlannerTemplateRepository
{
    public Task<string?> GetTemplateAsync(string templateId, CancellationToken ct)
        => Task.FromResult<string?>(null);
}
