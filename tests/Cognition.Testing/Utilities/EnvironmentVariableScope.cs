namespace Cognition.Testing.Utilities;

/// <summary>
/// Temporarily sets environment variables for the duration of the scope and restores their original values on dispose.
/// </summary>
public sealed class EnvironmentVariableScope : IDisposable
{
    private readonly IReadOnlyDictionary<string, string?> _original;
    private readonly EnvironmentVariableTarget _target;
    private bool _disposed;

    private EnvironmentVariableScope(IDictionary<string, string?> original, EnvironmentVariableTarget target)
    {
        _original = new Dictionary<string, string?>(original, StringComparer.OrdinalIgnoreCase);
        _target = target;
    }

    public static EnvironmentVariableScope Set(string key, string? value, EnvironmentVariableTarget target = EnvironmentVariableTarget.Process)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("Environment variable name is required", nameof(key));
        }

        var original = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
        {
            [key] = Environment.GetEnvironmentVariable(key, target)
        };

        Environment.SetEnvironmentVariable(key, value, target);
        return new EnvironmentVariableScope(original, target);
    }

    public static EnvironmentVariableScope Set(IDictionary<string, string?> values, EnvironmentVariableTarget target = EnvironmentVariableTarget.Process)
    {
        if (values is null)
        {
            throw new ArgumentNullException(nameof(values));
        }

        var original = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        foreach (var pair in values)
        {
            if (string.IsNullOrWhiteSpace(pair.Key))
            {
                throw new ArgumentException("Environment variable name is required", nameof(values));
            }

            original[pair.Key] = Environment.GetEnvironmentVariable(pair.Key, target);
            Environment.SetEnvironmentVariable(pair.Key, pair.Value, target);
        }

        return new EnvironmentVariableScope(original, target);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        foreach (var pair in _original)
        {
            Environment.SetEnvironmentVariable(pair.Key, pair.Value, _target);
        }

        _disposed = true;
    }
}

