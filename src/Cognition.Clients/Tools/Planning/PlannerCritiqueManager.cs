using System.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cognition.Clients.Tools.Planning;

public enum PlannerCritiqueBudgetStatus
{
    Allowed,
    Disabled,
    TotalLimitReached,
    StepLimitReached,
    TokenBudgetExceeded
}

public sealed class PlannerCritiqueManager
{
    private readonly bool _enabled;
    private readonly PlannerCritiqueBudget _budget;
    private readonly ILogger _logger;
    private readonly Dictionary<string, int> _perStepCounts = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<PlannerCritiqueBudgetStatus, int> _denials = new();

    private int _totalCritiques;
    private double _totalTokens;
    private bool _tokenBudgetBreached;

    private PlannerCritiqueManager(bool enabled, PlannerCritiqueBudget budget, ILogger logger)
    {
        _enabled = enabled;
        _budget = budget ?? new PlannerCritiqueBudget();
        _logger = logger;
    }

    public static PlannerCritiqueManager Disabled { get; } = new PlannerCritiqueManager(false, new PlannerCritiqueBudget(), NullLogger.Instance);

    public bool SupportsCritique => _enabled;

    public int TotalCritiques => _totalCritiques;

    public double TotalTokens => _totalTokens;

    public IReadOnlyDictionary<PlannerCritiqueBudgetStatus, int> Denials => _denials;

    public static PlannerCritiqueManager Create(
        bool enabled,
        PlannerCritiqueBudget budget,
        ILogger logger)
    {
        return new PlannerCritiqueManager(enabled, budget, logger);
    }

    public PlannerCritiqueAttempt BeginCritique(string stepId, double estimatedTokens = 0)
    {
        if (string.IsNullOrWhiteSpace(stepId))
        {
            stepId = "unknown-step";
        }

        if (!_enabled)
        {
            RegisterDenial(PlannerCritiqueBudgetStatus.Disabled);
            return new PlannerCritiqueAttempt(this, stepId, PlannerCritiqueBudgetStatus.Disabled, false);
        }

        if (_budget.MaxTotalCritiques.HasValue && _budget.MaxTotalCritiques.Value >= 0 && _totalCritiques >= _budget.MaxTotalCritiques.Value)
        {
            RegisterDenial(PlannerCritiqueBudgetStatus.TotalLimitReached);
            _logger.LogInformation("Planner critique budget denied for step {StepId}: total critique limit reached ({Limit}).", stepId, _budget.MaxTotalCritiques);
            return new PlannerCritiqueAttempt(this, stepId, PlannerCritiqueBudgetStatus.TotalLimitReached, false);
        }

        if (_budget.MaxCritiquesPerStep.HasValue && _budget.MaxCritiquesPerStep.Value >= 0)
        {
            if (_perStepCounts.TryGetValue(stepId, out var count) && count >= _budget.MaxCritiquesPerStep.Value)
            {
                RegisterDenial(PlannerCritiqueBudgetStatus.StepLimitReached);
                _logger.LogInformation("Planner critique budget denied for step {StepId}: per-step critique limit reached ({Limit}).", stepId, _budget.MaxCritiquesPerStep);
                return new PlannerCritiqueAttempt(this, stepId, PlannerCritiqueBudgetStatus.StepLimitReached, false);
            }
        }

        if (_budget.MaxTotalCritiqueTokens.HasValue && _budget.MaxTotalCritiqueTokens.Value >= 0)
        {
            if (_totalTokens + estimatedTokens > _budget.MaxTotalCritiqueTokens.Value)
            {
                RegisterDenial(PlannerCritiqueBudgetStatus.TokenBudgetExceeded);
                _logger.LogInformation("Planner critique budget denied for step {StepId}: estimated token usage would exceed limit ({Limit}).", stepId, _budget.MaxTotalCritiqueTokens);
                return new PlannerCritiqueAttempt(this, stepId, PlannerCritiqueBudgetStatus.TokenBudgetExceeded, false);
            }
        }

        _totalCritiques++;
        _perStepCounts[stepId] = _perStepCounts.TryGetValue(stepId, out var existing) ? existing + 1 : 1;

        return new PlannerCritiqueAttempt(this, stepId, PlannerCritiqueBudgetStatus.Allowed, true);
    }

    public void RecordTokens(double tokensUsed)
    {
        if (tokensUsed <= 0)
        {
            return;
        }

        _totalTokens += tokensUsed;
        if (_budget.MaxTotalCritiqueTokens.HasValue && _budget.MaxTotalCritiqueTokens.Value >= 0 && _totalTokens > _budget.MaxTotalCritiqueTokens.Value)
        {
            _tokenBudgetBreached = true;
        }
    }

    public void RegisterDenial(PlannerCritiqueBudgetStatus status)
    {
        if (!_denials.TryGetValue(status, out var count))
        {
            _denials[status] = 1;
        }
        else
        {
            _denials[status] = count + 1;
        }
    }

    public void ApplyMetrics(PlannerResult result)
    {
        if (!_enabled)
        {
            result.AddDiagnostics("critiqueStatus", "disabled");
            return;
        }

        if (_totalCritiques > 0)
        {
            result.AddMetric("critiqueCount", _totalCritiques);
        }

        if (_totalTokens > 0)
        {
            result.AddMetric("critiqueTokens", _totalTokens);
        }

        if (_denials.Count > 0)
        {
            var totalDenials = _denials.Values.Sum();
            result.AddMetric("critiqueDenied", totalDenials);
        }

        result.AddDiagnostics("critiqueStatus", DetermineStatus());
    }

    private string DetermineStatus()
    {
        if (!_enabled)
        {
            return "disabled";
        }

        if (_tokenBudgetBreached || _denials.ContainsKey(PlannerCritiqueBudgetStatus.TokenBudgetExceeded))
        {
            return "token-exhausted";
        }

        if (_denials.ContainsKey(PlannerCritiqueBudgetStatus.TotalLimitReached) || _denials.ContainsKey(PlannerCritiqueBudgetStatus.StepLimitReached))
        {
            return "count-exhausted";
        }

        if (_totalCritiques > 0)
        {
            return "used";
        }

        return "idle";
    }

}

public sealed class PlannerCritiqueAttempt : IDisposable
{
    private readonly PlannerCritiqueManager _manager;
    private readonly string _stepId;
    private readonly bool _allowed;
    private bool _completed;

    internal PlannerCritiqueAttempt(PlannerCritiqueManager manager, string stepId, PlannerCritiqueBudgetStatus status, bool allowed)
    {
        _manager = manager;
        _stepId = stepId;
        Status = status;
        _allowed = allowed;
    }

    public PlannerCritiqueBudgetStatus Status { get; }

    public bool Allowed => _allowed && Status == PlannerCritiqueBudgetStatus.Allowed;

    public void Complete(double tokensUsed)
    {
        if (!_allowed || Status != PlannerCritiqueBudgetStatus.Allowed)
        {
            return;
        }

        if (_completed)
        {
            return;
        }

        _manager.RecordTokens(tokensUsed);
        _completed = true;
    }

    public void Dispose()
    {
        if (_allowed && !_completed)
        {
            _manager.RecordTokens(0);
            _completed = true;
        }
    }
}
