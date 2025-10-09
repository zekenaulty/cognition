namespace Cognition.Contracts.Time;

/// <summary>
/// Provides the current time so call sites can be tested deterministically.
/// </summary>
public interface IClock
{
    DateTimeOffset Now { get; }
}
