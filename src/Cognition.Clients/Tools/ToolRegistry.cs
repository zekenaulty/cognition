using System.Linq;
using System.Reflection;
using Cognition.Clients.Tools.Planning;

namespace Cognition.Clients.Tools;

public interface IToolRegistry
{
    bool TryResolveByClassPath(string classPath, out Type type);
    bool IsKnownClassPath(string classPath);
    IReadOnlyDictionary<string, Type> Map { get; }
    IReadOnlyCollection<Type> GetPlannersByCapability(string capability);
}

public class ToolRegistry : IToolRegistry
{
    private readonly Dictionary<string, Type> _map = new(StringComparer.Ordinal);
    private readonly Dictionary<string, HashSet<Type>> _plannerCapabilities = new(StringComparer.OrdinalIgnoreCase);

    public ToolRegistry()
    {
        var assembly = typeof(ITool).Assembly;
        foreach (var t in assembly.GetTypes())
        {
            if (t.IsAbstract || t.IsInterface) continue;
            if (!typeof(ITool).IsAssignableFrom(t)) continue;

            var asmName = t.Assembly.GetName().Name;
            var aqn = $"{t.FullName}, {asmName}";
            if (t.FullName != null) _map[t.FullName] = t;
            _map[aqn] = t;

            if (typeof(IPlannerTool).IsAssignableFrom(t))
            {
                var capabilities = t.GetCustomAttribute<PlannerCapabilitiesAttribute>()?.Capabilities ?? Array.Empty<string>();
                foreach (var capability in capabilities)
                {
                    if (!_plannerCapabilities.TryGetValue(capability, out var set))
                    {
                        set = new HashSet<Type>();
                        _plannerCapabilities[capability] = set;
                    }
                    set.Add(t);
                }
            }
        }
    }

    public bool TryResolveByClassPath(string classPath, out Type type)
    {
        return _map.TryGetValue(classPath, out type!);
    }

    public bool IsKnownClassPath(string classPath) => _map.ContainsKey(classPath);

    public IReadOnlyDictionary<string, Type> Map => _map;

    public IReadOnlyCollection<Type> GetPlannersByCapability(string capability)
    {
        if (_plannerCapabilities.TryGetValue(capability, out var set))
        {
            return set.ToList().AsReadOnly();
        }
        return Array.Empty<Type>();
    }
}
