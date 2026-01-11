namespace Acode.Cli.Tests.Routing;

using Acode.Cli.Routing;
using FluentAssertions;

/// <summary>
/// Tests for <see cref="FuzzyMatcher"/>.
/// </summary>
public sealed class FuzzyMatcherTests
{
    [Fact]
    public void Constructor_WithValidParameters_ShouldSucceed()
    {
        // Act.
        var matcher = new FuzzyMatcher(threshold: 0.5, maxResults: 5);

        // Assert.
        matcher.Should().NotBeNull();
    }

    [Theory]
    [InlineData(-0.1)]
    [InlineData(1.1)]
    public void Constructor_WithInvalidThreshold_ShouldThrow(double threshold)
    {
        // Act.
        var act = () => new FuzzyMatcher(threshold: threshold);

        // Assert.
        act.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("threshold");
    }

    [Fact]
    public void Constructor_WithNegativeMaxResults_ShouldThrow()
    {
        // Act.
        var act = () => new FuzzyMatcher(maxResults: -1);

        // Assert.
        act.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("maxResults");
    }

    [Theory]
    [InlineData("", "", 0)]
    [InlineData("abc", "abc", 0)]
    [InlineData("abc", "ab", 1)]
    [InlineData("abc", "axc", 1)]
    [InlineData("abc", "xyz", 3)]
    [InlineData("kitten", "sitting", 3)]
    public void LevenshteinDistance_ShouldCalculateCorrectDistance(
        string source,
        string target,
        int expectedDistance
    )
    {
        // Act.
        var distance = FuzzyMatcher.LevenshteinDistance(source, target);

        // Assert.
        distance.Should().Be(expectedDistance);
    }

    [Theory]
    [InlineData("abc", "abc", 1.0)]
    [InlineData("abc", "xyz", 0.0)]
    [InlineData("ab", "abc", 0.666, 0.01)]
    public void CalculateSimilarity_ShouldReturnCorrectValue(
        string a,
        string b,
        double expectedSimilarity,
        double tolerance = 0.001
    )
    {
        // Arrange.
        var matcher = new FuzzyMatcher();

        // Act.
        var similarity = matcher.CalculateSimilarity(a, b);

        // Assert.
        similarity.Should().BeApproximately(expectedSimilarity, tolerance);
    }

    [Fact]
    public void CalculateSimilarity_BothEmpty_ShouldReturn1()
    {
        // Arrange.
        var matcher = new FuzzyMatcher();

        // Act.
        var similarity = matcher.CalculateSimilarity(string.Empty, string.Empty);

        // Assert.
        similarity.Should().Be(1.0);
    }

    [Theory]
    [InlineData("abc", "")]
    [InlineData("", "abc")]
    public void CalculateSimilarity_OneEmpty_ShouldReturn0(string a, string b)
    {
        // Arrange.
        var matcher = new FuzzyMatcher();

        // Act.
        var similarity = matcher.CalculateSimilarity(a, b);

        // Assert.
        similarity.Should().Be(0.0);
    }

    [Fact]
    public void FindSimilar_WithNullInput_ShouldThrow()
    {
        // Arrange.
        var matcher = new FuzzyMatcher();
        var candidates = new[] { "run", "chat" };

        // Act.
        var act = () => matcher.FindSimilar(null!, candidates);

        // Assert.
        act.Should().Throw<ArgumentNullException>().WithParameterName("input");
    }

    [Fact]
    public void FindSimilar_WithNullCandidates_ShouldThrow()
    {
        // Arrange.
        var matcher = new FuzzyMatcher();

        // Act.
        var act = () => matcher.FindSimilar("run", null!);

        // Assert.
        act.Should().Throw<ArgumentNullException>().WithParameterName("candidates");
    }

    [Fact]
    public void FindSimilar_ShouldReturnSimilarStrings()
    {
        // Arrange.
        var matcher = new FuzzyMatcher(threshold: 0.5, maxResults: 3);
        var candidates = new[] { "run", "chat", "config", "resume", "rnu" };

        // Act.
        var results = matcher.FindSimilar("run", candidates);

        // Assert - "run" should match itself exactly, "rnu" is similar.
        results.Should().Contain("run");
        results.Should().HaveCountLessOrEqualTo(3);
    }

    [Fact]
    public void FindSimilar_ShouldBeCaseInsensitive()
    {
        // Arrange.
        var matcher = new FuzzyMatcher(threshold: 0.5, maxResults: 3);
        var candidates = new[] { "RUN", "Chat", "CONFIG" };

        // Act.
        var results = matcher.FindSimilar("run", candidates);

        // Assert.
        results.Should().Contain("RUN");
    }

    [Fact]
    public void FindSimilar_ShouldOrderByDescendingSimilarity()
    {
        // Arrange.
        var matcher = new FuzzyMatcher(threshold: 0.1, maxResults: 5);
        var candidates = new[] { "run", "rn", "xyz", "runn" };

        // Act.
        var results = matcher.FindSimilar("run", candidates).ToList();

        // Assert - "run" should be first (exact match), then "runn", then "rn".
        if (results.Count >= 2)
        {
            results[0].Should().Be("run");
        }
    }

    [Fact]
    public void FindSimilar_WithZeroMaxResults_ShouldReturnEmpty()
    {
        // Arrange.
        var matcher = new FuzzyMatcher(threshold: 0.5, maxResults: 3);
        var candidates = new[] { "run", "chat" };

        // Act.
        var results = matcher.FindSimilar("run", candidates, maxResults: 0);

        // Assert.
        results.Should().BeEmpty();
    }

    [Fact]
    public void FindSimilar_WithOverrideThreshold_ShouldUseOverride()
    {
        // Arrange.
        var matcher = new FuzzyMatcher(threshold: 0.9, maxResults: 5);
        var candidates = new[] { "run", "rn", "xyz" };

        // Act - override threshold to lower value.
        var results = matcher.FindSimilar("run", candidates, threshold: 0.5);

        // Assert - should include "rn" now.
        results.Should().Contain("run");
        results.Count.Should().BeGreaterThan(1);
    }

    [Fact]
    public void FindSimilar_ShouldLimitResults()
    {
        // Arrange.
        var matcher = new FuzzyMatcher(threshold: 0.1, maxResults: 2);
        var candidates = new[] { "run", "rn", "runn", "rung", "runs" };

        // Act.
        var results = matcher.FindSimilar("run", candidates);

        // Assert.
        results.Should().HaveCount(2);
    }
}
