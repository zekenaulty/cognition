using Microsoft.Extensions.Options;

namespace Cognition.Clients.Tools.Planning;

public interface IPlannerQuotaService
{
    PlannerQuotaDecision Evaluate(string plannerKey, PlannerQuotaContext context, Guid? personaId);
}

public sealed record PlannerQuotaContext(
    int? IterationIndex = null,
    int? PendingJobs = null,
    double? RequestedTokens = null);

public enum PlannerQuotaLimit
{
    MaxIterations,
    MaxQueuedJobs,
    MaxTokens
}

public sealed record PlannerQuotaDecision(
    bool IsAllowed,
    PlannerQuotaLimit? Limit,
    double? LimitValue,
    string? Reason)
{
    public static PlannerQuotaDecision Allowed() => new(true, null, null, null);

    public static PlannerQuotaDecision Blocked(PlannerQuotaLimit limit, double? value, string? reason = null)
        => new(false, limit, value, reason);
}

public sealed class PlannerQuotaService : IPlannerQuotaService
{
    private readonly IOptionsMonitor<PlannerQuotaOptions> _options;

    public PlannerQuotaService(IOptionsMonitor<PlannerQuotaOptions> options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public PlannerQuotaDecision Evaluate(string plannerKey, PlannerQuotaContext context, Guid? personaId)
    {
        if (string.IsNullOrWhiteSpace(plannerKey))
        {
            return PlannerQuotaDecision.Allowed();
        }

        var limits = _options.CurrentValue.Resolve(plannerKey, personaId);
        if (limits is null)
        {
            return PlannerQuotaDecision.Allowed();
        }

        if (limits.MaxIterations.HasValue && context.IterationIndex.HasValue)
        {
            if (context.IterationIndex.Value >= limits.MaxIterations.Value)
            {
                return PlannerQuotaDecision.Blocked(
                    PlannerQuotaLimit.MaxIterations,
                    limits.MaxIterations,
                    $"Iteration {context.IterationIndex.Value} exceeds limit {limits.MaxIterations.Value}.");
            }
        }

        if (limits.MaxQueuedJobs.HasValue && context.PendingJobs.HasValue)
        {
            if (context.PendingJobs.Value >= limits.MaxQueuedJobs.Value)
            {
                return PlannerQuotaDecision.Blocked(
                    PlannerQuotaLimit.MaxQueuedJobs,
                    limits.MaxQueuedJobs,
                    $"Pending jobs {context.PendingJobs.Value} reach limit {limits.MaxQueuedJobs.Value}.");
            }
        }

        if (limits.MaxTokens.HasValue && context.RequestedTokens.HasValue)
        {
            if (context.RequestedTokens.Value > limits.MaxTokens.Value)
            {
                return PlannerQuotaDecision.Blocked(
                    PlannerQuotaLimit.MaxTokens,
                    limits.MaxTokens,
                    $"Requested tokens {context.RequestedTokens.Value} exceed limit {limits.MaxTokens.Value}.");
            }
        }

        return PlannerQuotaDecision.Allowed();
    }
}
