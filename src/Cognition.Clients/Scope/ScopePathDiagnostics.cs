using System.Collections.Concurrent;
using System.Linq;
using Cognition.Contracts.Scopes;

namespace Cognition.Clients.Scope;

public interface IScopePathDiagnostics
{
    void RecordLegacyWrite();
    void RecordPathWrite(string hash, ScopePathProjection projection);
    void RecordBackfill(int updated, int skipped);
    ScopePathDiagnosticsSnapshot Snapshot();
}

public sealed record ScopePathDiagnosticsSnapshot(
    long LegacyWrites,
    long PathWrites,
    DateTime LastUpdatedUtc,
    IReadOnlyDictionary<string, long> PrincipalCounts,
    long CollisionCount,
    DateTime LastCollisionUtc,
    long BackfillUpdated,
    long BackfillSkipped,
    DateTime LastBackfillUtc);

public sealed class ScopePathDiagnostics : IScopePathDiagnostics
{
    private long _legacyWrites;
    private long _pathWrites;
    private long _lastUpdatedTicks;
    private long _collisionCount;
    private long _lastCollisionTicks;
    private long _backfillUpdated;
    private long _backfillSkipped;
    private long _lastBackfillTicks;
    private readonly ConcurrentDictionary<string, long> _principalCounts = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, string> _hashToPath = new(StringComparer.Ordinal);

    public void RecordLegacyWrite()
    {
        Interlocked.Increment(ref _legacyWrites);
        Interlocked.Exchange(ref _lastUpdatedTicks, DateTime.UtcNow.Ticks);
    }

    public void RecordPathWrite(string hash, ScopePathProjection projection)
    {
        Interlocked.Increment(ref _pathWrites);
        var nowTicks = DateTime.UtcNow.Ticks;
        Interlocked.Exchange(ref _lastUpdatedTicks, nowTicks);

        var principalType = string.IsNullOrWhiteSpace(projection.PrincipalType)
            ? "none"
            : projection.PrincipalType;
        _principalCounts.AddOrUpdate(principalType, 1, (_, current) => current + 1);

        _hashToPath.AddOrUpdate(
            hash,
            projection.Canonical,
            (_, existing) =>
            {
                if (!string.Equals(existing, projection.Canonical, StringComparison.Ordinal))
                {
                    Interlocked.Increment(ref _collisionCount);
                    Interlocked.Exchange(ref _lastCollisionTicks, nowTicks);
                    return projection.Canonical;
                }
                return existing;
            });
    }

    public void RecordBackfill(int updated, int skipped)
    {
        if (updated == 0 && skipped == 0) return;
        Interlocked.Add(ref _backfillUpdated, updated);
        Interlocked.Add(ref _backfillSkipped, skipped);
        Interlocked.Exchange(ref _lastBackfillTicks, DateTime.UtcNow.Ticks);
    }

    public ScopePathDiagnosticsSnapshot Snapshot()
    {
        static DateTime Normalize(long ticks) => ticks == 0 ? DateTime.MinValue : new DateTime(ticks, DateTimeKind.Utc);

        var counts = _principalCounts.ToDictionary(kvp => kvp.Key, kvp => kvp.Value, StringComparer.OrdinalIgnoreCase);
        return new ScopePathDiagnosticsSnapshot(
            LegacyWrites: Interlocked.Read(ref _legacyWrites),
            PathWrites: Interlocked.Read(ref _pathWrites),
            LastUpdatedUtc: Normalize(Interlocked.Read(ref _lastUpdatedTicks)),
            PrincipalCounts: counts,
            CollisionCount: Interlocked.Read(ref _collisionCount),
            LastCollisionUtc: Normalize(Interlocked.Read(ref _lastCollisionTicks)),
            BackfillUpdated: Interlocked.Read(ref _backfillUpdated),
            BackfillSkipped: Interlocked.Read(ref _backfillSkipped),
            LastBackfillUtc: Normalize(Interlocked.Read(ref _lastBackfillTicks)));
    }
}
