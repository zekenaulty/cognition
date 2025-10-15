using System.Collections.ObjectModel;
using System.Linq;
using Cognition.Clients.Tools;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Cognition.Clients.Tools.Planning;

public sealed record PlannerDescriptor(Type ImplementationType, PlannerMetadata Metadata);

public interface IPlannerCatalog
{
    IReadOnlyList<PlannerDescriptor> GetByCapability(string capability);
    PlannerDescriptor? TryResolveByName(string plannerName);
}

public sealed class PlannerCatalog : IPlannerCatalog
{
    private readonly IToolRegistry _registry;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<PlannerCatalog> _logger;

    public PlannerCatalog(IToolRegistry registry, IServiceScopeFactory scopeFactory, ILogger<PlannerCatalog> logger)
    {
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
        _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public IReadOnlyList<PlannerDescriptor> GetByCapability(string capability)
    {
        if (string.IsNullOrWhiteSpace(capability))
            throw new ArgumentException("Capability is required.", nameof(capability));

        var plannerTypes = _registry.GetPlannersByCapability(capability);
        if (plannerTypes.Count == 0)
        {
            return Array.Empty<PlannerDescriptor>();
        }

        using var scope = _scopeFactory.CreateScope();
        var descriptors = new List<PlannerDescriptor>(plannerTypes.Count);
        foreach (var type in plannerTypes)
        {
            if (scope.ServiceProvider.GetService(type) is not IPlannerTool planner)
            {
                _logger.LogWarning("Planner type {PlannerType} is indexed for capability {Capability} but is not registered in DI.", type.FullName, capability);
                continue;
            }

            descriptors.Add(new PlannerDescriptor(type, planner.Metadata));
        }

        return descriptors.Count == 0
            ? Array.Empty<PlannerDescriptor>()
            : new ReadOnlyCollection<PlannerDescriptor>(descriptors);
    }

    public PlannerDescriptor? TryResolveByName(string plannerName)
    {
        if (string.IsNullOrWhiteSpace(plannerName))
        {
            return null;
        }

        using var scope = _scopeFactory.CreateScope();
        foreach (var type in _registry.Map.Values.Where(t => typeof(IPlannerTool).IsAssignableFrom(t)))
        {
            if (scope.ServiceProvider.GetService(type) is not IPlannerTool planner)
            {
                continue;
            }

            if (string.Equals(planner.Metadata.Name, plannerName, StringComparison.OrdinalIgnoreCase))
            {
                return new PlannerDescriptor(type, planner.Metadata);
            }
        }

        return null;
    }
}
