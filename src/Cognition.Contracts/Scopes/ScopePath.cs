using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Cognition.Contracts.Scopes;

/// <summary>
/// Canonical representation of a scope rooted at a single principal with ordered context segments.
/// </summary>
public sealed class ScopePath : IEquatable<ScopePath>
{
    private readonly ScopeSegment[] _segments;
    private readonly string _canonical;
    private readonly ReadOnlyCollection<ScopeSegment> _readOnlySegments;

    private ScopePath(ScopePrincipal principal, ScopeSegment[] segments)
    {
        Principal = principal;
        _segments = segments;
        _readOnlySegments = Array.AsReadOnly(_segments);
        _canonical = BuildCanonical(principal, segments);
    }

    public ScopePrincipal Principal { get; }

    public IReadOnlyList<ScopeSegment> Segments => _readOnlySegments;

    public string Canonical => _canonical;

    public static ScopePath Empty { get; } = new ScopePath(ScopePrincipal.None, Array.Empty<ScopeSegment>());

    internal static ScopePath Create(ScopePrincipal principal, IEnumerable<ScopeSegment> segments)
    {
        var normalizedSegments = NormalizeSegments(segments);
        return new ScopePath(NormalizePrincipal(principal), normalizedSegments);
    }

    public ScopePath WithSegment(ScopeSegment segment)
    {
        if (segment.IsEmpty) return this;
        var merged = new ScopeSegment[_segments.Length + 1];
        Array.Copy(_segments, merged, _segments.Length);
        merged[^1] = NormalizeSegment(segment);
        Array.Sort(merged, ScopeSegmentComparer.Instance);
        merged = Deduplicate(merged);
        return new ScopePath(Principal, merged);
    }

    public override string ToString() => Canonical;

    public bool Equals(ScopePath? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        if (!Principal.Equals(other.Principal)) return false;
        if (_segments.Length != other._segments.Length) return false;
        for (var i = 0; i < _segments.Length; i++)
        {
            if (!_segments[i].Equals(other._segments[i])) return false;
        }
        return true;
    }

    public override bool Equals(object? obj) => obj is ScopePath sp && Equals(sp);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(Principal);
        foreach (var segment in _segments)
        {
            hash.Add(segment);
        }
        return hash.ToHashCode();
    }

    private static ScopeSegment[] NormalizeSegments(IEnumerable<ScopeSegment> segments)
    {
        var normalized = segments
            .Select(NormalizeSegment)
            .Where(s => !s.IsEmpty)
            .ToArray();
        Array.Sort(normalized, ScopeSegmentComparer.Instance);
        return Deduplicate(normalized);
    }

    private static ScopeSegment NormalizeSegment(ScopeSegment segment)
    {
        return new ScopeSegment(segment.Key, segment.Value);
    }

    private static ScopePrincipal NormalizePrincipal(ScopePrincipal principal)
    {
        return new ScopePrincipal(principal.RootId, principal.PrincipalType);
    }

    private static string BuildCanonical(ScopePrincipal principal, IReadOnlyList<ScopeSegment> segments)
    {
        var builder = new StringBuilder();
        builder.Append(principal.Canonical);
        foreach (var segment in segments)
        {
            builder.Append('/');
            builder.Append(segment.Canonical);
        }
        return builder.ToString();
    }

    private sealed class ScopeSegmentComparer : IEqualityComparer<ScopeSegment>, IComparer<ScopeSegment>
    {
        public static ScopeSegmentComparer Instance { get; } = new();

        public bool Equals(ScopeSegment x, ScopeSegment y)
        {
            return string.Equals(x.Key, y.Key, StringComparison.Ordinal) &&
                   string.Equals(x.Value, y.Value, StringComparison.Ordinal);
        }

        public int GetHashCode([DisallowNull] ScopeSegment obj)
        {
            return HashCode.Combine(obj.Key, obj.Value);
        }

        public int Compare(ScopeSegment x, ScopeSegment y)
        {
            var keyCompare = string.CompareOrdinal(x.Key, y.Key);
            if (keyCompare != 0) return keyCompare;
            return string.CompareOrdinal(x.Value, y.Value);
        }
    }

    private static ScopeSegment[] Deduplicate(ScopeSegment[] segments)
    {
        if (segments.Length <= 1) return segments;
        var list = new List<ScopeSegment>(segments.Length);
        ScopeSegment? previous = null;
        foreach (var seg in segments)
        {
            if (previous is { } prev && ScopeSegmentComparer.Instance.Equals(prev, seg))
            {
                continue;
            }
            list.Add(seg);
            previous = seg;
        }

        return list.ToArray();
    }
}
