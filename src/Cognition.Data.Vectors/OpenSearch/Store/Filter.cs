namespace Cognition.Data.Vectors.OpenSearch.OpenSearch.Store;

public abstract record Filter;
public sealed record TermFilter(string Field, object Value) : Filter;
public sealed record TermsFilter(string Field, IEnumerable<object> Values) : Filter;
public sealed record RangeFilter(string Field, object? Gte = null, object? Lte = null) : Filter;

