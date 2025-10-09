using Cognition.Contracts.Time;

namespace Cognition.Testing.Time;

public sealed class FakeClock : IClock
{
    public DateTimeOffset Now { get; set; } = DateTimeOffset.UtcNow;

    public void Advance(TimeSpan delta) => Now = Now.Add(delta);
}
