namespace Cognition.Data.Vectors.OpenSearch.OpenSearch.Utils;

public static class ScoreUtils
{
    // OpenSearch kNN with cosine similarity typically returns higher-is-better
    // Normalize into [0,1] if already in [-1,1] or similar
    public static double NormalizeCosine(double score)
        => (score + 1d) / 2d;

    public static void RescoreRerank(List<Models.SearchResult> results, Func<Models.SearchResult, double> func) { }
}
