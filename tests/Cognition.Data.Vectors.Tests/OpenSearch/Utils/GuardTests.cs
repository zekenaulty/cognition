using System;
using System.Collections.Generic;
using System.Linq;
using Cognition.Data.Vectors.OpenSearch.OpenSearch.Models;
using Cognition.Data.Vectors.OpenSearch.OpenSearch.Utils;
using FluentAssertions;
using Xunit;

namespace Cognition.Data.Vectors.Tests.OpenSearch.Utils;

public class GuardTests
{
    [Theory]
    [InlineData("", "name")]
    [InlineData("   ", "another")]
    [InlineData(null, "value")]
    public void NotNullOrEmpty_ShouldThrow_ForNullOrWhitespace(string? input, string parameter)
    {
        Action act = () => Guard.NotNullOrEmpty(input, parameter);
        act.Should().Throw<ArgumentException>()
            .WithMessage("*is required.*")
            .And.ParamName.Should().Be(parameter);
    }

    [Fact]
    public void NotNullOrEmpty_ShouldPass_ForValidValue()
    {
        Guard.NotNullOrEmpty("hello", "value");
    }

    [Fact]
    public void EnsureDimension_ShouldThrow_WhenVectorNull()
    {
        Action act = () => Guard.EnsureDimension(null, 3);
        act.Should().Throw<ArgumentNullException>()
            .And.ParamName.Should().Be("vector");
    }

    [Fact]
    public void EnsureDimension_ShouldThrow_WhenLengthMismatch()
    {
        Action act = () => Guard.EnsureDimension(new[] { 1f, 2f }, 3);
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Expected 3, got 2.*");
    }

    [Fact]
    public void EnsureDimension_ShouldPass_WhenLengthMatches()
    {
        Guard.EnsureDimension(new[] { 1f, 2f, 3f }, 3);
    }
}

public class ScoreUtilsTests
{
    [Theory]
    [InlineData(-1, 0)]
    [InlineData(0, 0.5)]
    [InlineData(1, 1)]
    [InlineData(0.2, 0.6)]
    public void NormalizeCosine_ShouldMapRangeCorrectly(double input, double expected)
    {
        ScoreUtils.NormalizeCosine(input).Should().BeApproximately(expected, 1e-9);
    }

    [Fact]
    public void RescoreRerank_ShouldApplyNewScoresAndSortDescending()
    {
        var original = new List<SearchResult>
        {
            new() { Item = new VectorItem { Id = "a" }, Score = 0.1, Highlights = new Dictionary<string, object>() },
            new() { Item = new VectorItem { Id = "b" }, Score = 0.2, Highlights = new Dictionary<string, object>() },
            new() { Item = new VectorItem { Id = "c" }, Score = 0.3 }
        };
        var snapshot = original.ToList();

        ScoreUtils.RescoreRerank(original, r => r.Item.Id switch
        {
            "a" => 5d,
            "b" => 10d,
            _ => 1d
        });

        original.Select(r => r.Item.Id).Should().ContainInOrder("b", "a", "c");
        original[0].Score.Should().Be(10d);
        original[1].Score.Should().Be(5d);
        original[0].Highlights.Should().BeSameAs(snapshot[1].Highlights);
    }

    [Fact]
    public void RescoreRerank_ShouldTreatNaNAsLowestAndRemainStableForTies()
    {
        var results = new List<SearchResult>
        {
            new() { Item = new VectorItem { Id = "x" }, Score = 0.1 },
            new() { Item = new VectorItem { Id = "y" }, Score = 0.2 },
            new() { Item = new VectorItem { Id = "z" }, Score = 0.3 }
        };

        ScoreUtils.RescoreRerank(results, r => r.Item.Id switch
        {
            "x" => 2d,
            "y" => double.NaN,
            _ => 2d
        });

        results.Select(r => r.Item.Id).Should().ContainInOrder("x", "z", "y");
    }
}
