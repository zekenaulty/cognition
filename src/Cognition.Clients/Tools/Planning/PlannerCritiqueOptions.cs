namespace Cognition.Clients.Tools.Planning;

public sealed class PlannerCritiqueOptions
{
    public const string SectionName = "PlannerCritique";

    public bool Enabled { get; set; } = false;

    public PlannerCritiqueBudget Defaults { get; set; } = new();

    public Dictionary<string, PlannerCritiqueBudget> PlannerOverrides { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    public Dictionary<string, PlannerCritiquePlannerSettings> PlannerSettings { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    public PlannerCritiqueBudget ResolveBudget(string plannerName)
    {
        var baseline = Defaults?.Clone() ?? new PlannerCritiqueBudget();
        if (!string.IsNullOrWhiteSpace(plannerName))
        {
            if (PlannerSettings is not null &&
                PlannerSettings.TryGetValue(plannerName, out var settings) &&
                settings.Budget is not null)
            {
                baseline.Apply(settings.Budget);
            }
            else if (PlannerOverrides is not null &&
                     PlannerOverrides.TryGetValue(plannerName, out var legacyOverride))
            {
                baseline.Apply(legacyOverride);
            }
        }

        return baseline;
    }

    public bool IsPlannerEnabled(string plannerName, Guid? personaId, bool fallback = false)
    {
        if (!Enabled)
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(plannerName) &&
            PlannerSettings is not null &&
            PlannerSettings.TryGetValue(plannerName, out var settings))
        {
            if (!settings.Enabled)
            {
                return false;
            }

            if (settings.PersonaAllowList is { Count: > 0 })
            {
                return personaId.HasValue && settings.PersonaAllowList.Contains(personaId.Value);
            }

            return true;
        }

        return fallback;
    }
}

public sealed class PlannerCritiquePlannerSettings
{
    public bool Enabled { get; set; } = false;

    public PlannerCritiqueBudget? Budget { get; set; }

    public List<Guid> PersonaAllowList { get; set; } = new();
}

public sealed class PlannerCritiqueBudget
{
    public int? MaxTotalCritiques { get; set; } = 0;

    public int? MaxCritiquesPerStep { get; set; } = 0;

    public double? MaxTotalCritiqueTokens { get; set; } = 0;

    internal PlannerCritiqueBudget Clone()
    {
        return new PlannerCritiqueBudget
        {
            MaxTotalCritiques = MaxTotalCritiques,
            MaxCritiquesPerStep = MaxCritiquesPerStep,
            MaxTotalCritiqueTokens = MaxTotalCritiqueTokens
        };
    }

    internal void Apply(PlannerCritiqueBudget other)
    {
        if (other is null)
        {
            return;
        }

        if (other.MaxTotalCritiques.HasValue)
        {
            MaxTotalCritiques = other.MaxTotalCritiques;
        }
        if (other.MaxCritiquesPerStep.HasValue)
        {
            MaxCritiquesPerStep = other.MaxCritiquesPerStep;
        }
        if (other.MaxTotalCritiqueTokens.HasValue)
        {
            MaxTotalCritiqueTokens = other.MaxTotalCritiqueTokens;
        }
    }
}
