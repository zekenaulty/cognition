namespace Cognition.Clients.Tools.Planning;

public sealed class PlannerQuotaOptions
{
    public const string SectionName = "PlannerQuotas";

    public PlannerQuotaLimits Defaults { get; set; } = new();

    public Dictionary<string, PlannerQuotaLimits> Planners { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    public Dictionary<Guid, PlannerPersonaQuotaOptions> Personas { get; set; } = new();

    public PlannerQuotaLimits Resolve(string plannerKey, Guid? personaId)
    {
        var resolved = Defaults?.Clone() ?? new PlannerQuotaLimits();

        if (!string.IsNullOrWhiteSpace(plannerKey) &&
            Planners is not null &&
            Planners.TryGetValue(plannerKey, out var plannerOverride))
        {
            resolved.Apply(plannerOverride);
        }

        if (personaId.HasValue &&
            personaId.Value != Guid.Empty &&
            Personas is not null &&
            Personas.TryGetValue(personaId.Value, out var personaOptions))
        {
            resolved.Apply(personaOptions.Defaults);

            if (!string.IsNullOrWhiteSpace(plannerKey) &&
                personaOptions.Planners is not null &&
                personaOptions.Planners.TryGetValue(plannerKey, out var personaPlannerOverride))
            {
                resolved.Apply(personaPlannerOverride);
            }
        }

        return resolved;
    }
}

public sealed class PlannerPersonaQuotaOptions
{
    public PlannerQuotaLimits Defaults { get; set; } = new();

    public Dictionary<string, PlannerQuotaLimits> Planners { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

public sealed class PlannerQuotaLimits
{
    public int? MaxIterations { get; set; }

    public int? MaxQueuedJobs { get; set; }

    public double? MaxTokens { get; set; }

    public PlannerQuotaLimits Clone()
    {
        return new PlannerQuotaLimits
        {
            MaxIterations = NormalizeLimit(MaxIterations),
            MaxQueuedJobs = NormalizeLimit(MaxQueuedJobs),
            MaxTokens = NormalizeLimit(MaxTokens)
        };
    }

    public void Apply(PlannerQuotaLimits? other)
    {
        if (other is null)
        {
            return;
        }

        if (other.MaxIterations.HasValue)
        {
            MaxIterations = NormalizeLimit(other.MaxIterations);
        }

        if (other.MaxQueuedJobs.HasValue)
        {
            MaxQueuedJobs = NormalizeLimit(other.MaxQueuedJobs);
        }

        if (other.MaxTokens.HasValue)
        {
            MaxTokens = NormalizeLimit(other.MaxTokens);
        }
    }

    private static int? NormalizeLimit(int? value)
    {
        if (!value.HasValue)
        {
            return null;
        }

        return value.Value <= 0 ? null : value;
    }

    private static double? NormalizeLimit(double? value)
    {
        if (!value.HasValue)
        {
            return null;
        }

        return value.Value <= 0 ? null : value;
    }
}
