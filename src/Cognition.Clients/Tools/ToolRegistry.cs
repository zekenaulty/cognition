using System.Reflection;

namespace Cognition.Clients.Tools;

public interface IToolRegistry
{
    bool TryResolveByClassPath(string classPath, out Type type);
    bool IsKnownClassPath(string classPath);
    IReadOnlyDictionary<string, Type> Map { get; }
}

public class ToolRegistry : IToolRegistry
{
    private readonly Dictionary<string, Type> _map = new(StringComparer.Ordinal);

    public ToolRegistry()
    {
        // Scan the assembly containing ITool for implementations
        var assembly = typeof(ITool).Assembly;
        foreach (var t in assembly.GetTypes())
        {
            if (t.IsAbstract || t.IsInterface) continue;
            if (!typeof(ITool).IsAssignableFrom(t)) continue;

            var asmName = t.Assembly.GetName().Name;
            var aqn = $"{t.FullName}, {asmName}"; // assembly-qualified short form
            if (t.FullName != null) _map[t.FullName] = t;     // Namespace.Type
            _map[aqn] = t;                                    // Namespace.Type, Assembly
            _map[t.Name] = t;                                 // TypeName (last resort)
        }
    }

    public bool TryResolveByClassPath(string classPath, out Type type)
    {
        return _map.TryGetValue(classPath, out type!);
    }

    public bool IsKnownClassPath(string classPath) => _map.ContainsKey(classPath);

    public IReadOnlyDictionary<string, Type> Map => _map;
}

