using System;
using System.Collections.Generic;

namespace Cognition.Data.Vectors.OpenSearch.OpenSearch.Utils;

public static class ScoreUtils
{
    // OpenSearch kNN with cosine similarity typically returns higher-is-better
    // Normalize into [0,1] if already in [-1,1] or similar
    public static double NormalizeCosine(double score)
        => (score + 1d) / 2d;

    public static void RescoreRerank(List<Models.SearchResult> results, Func<Models.SearchResult, double> func)
    {
        if (results is null)
        {
            throw new ArgumentNullException(nameof(results));
        }

        if (func is null)
        {
            throw new ArgumentNullException(nameof(func));
        }

        if (results.Count == 0)
        {
            return;
        }

        var rescored = new List<(Models.SearchResult Original, double Score, int Index)>(results.Count);
        for (var i = 0; i < results.Count; i++)
        {
            var original = results[i];
            var score = func(original);
            if (double.IsNaN(score))
            {
                score = double.NegativeInfinity;
            }

            rescored.Add((original, score, i));
        }

        rescored.Sort(static (left, right) =>
        {
            var scoreCompare = right.Score.CompareTo(left.Score);
            return scoreCompare != 0 ? scoreCompare : left.Index.CompareTo(right.Index);
        });

        for (var i = 0; i < rescored.Count; i++)
        {
            var entry = rescored[i];
            results[i] = new Models.SearchResult
            {
                Item = entry.Original.Item,
                Score = entry.Score,
                Highlights = entry.Original.Highlights
            };
        }
    }
}

