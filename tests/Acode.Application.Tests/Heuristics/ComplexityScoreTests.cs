namespace Acode.Application.Tests.Heuristics;

using Acode.Application.Heuristics;
using FluentAssertions;
using Xunit;

/// <summary>
/// Unit tests for <see cref="ComplexityScore"/>.
/// </summary>
public sealed class ComplexityScoreTests
{
    /// <summary>
    /// Test that low scores return Low tier.
    /// </summary>
    /// <param name="score">The score to test.</param>
    [Theory]
    [InlineData(0)]
    [InlineData(15)]
    [InlineData(30)]
    public void Should_Return_Low_Tier_For_Low_Scores(int score)
    {
        // Arrange & Act
        var complexityScore = new ComplexityScore(
            score,
            Array.Empty<(string, HeuristicResult)>(),
            lowThreshold: 30,
            highThreshold: 70
        );

        // Assert
        complexityScore.Tier.Should().Be(ComplexityTier.Low);
    }

    /// <summary>
    /// Test that medium scores return Medium tier.
    /// </summary>
    /// <param name="score">The score to test.</param>
    [Theory]
    [InlineData(31)]
    [InlineData(50)]
    [InlineData(69)]
    public void Should_Return_Medium_Tier_For_Medium_Scores(int score)
    {
        // Arrange & Act
        var complexityScore = new ComplexityScore(
            score,
            Array.Empty<(string, HeuristicResult)>(),
            lowThreshold: 30,
            highThreshold: 70
        );

        // Assert
        complexityScore.Tier.Should().Be(ComplexityTier.Medium);
    }

    /// <summary>
    /// Test that high scores return High tier.
    /// </summary>
    /// <param name="score">The score to test.</param>
    [Theory]
    [InlineData(70)]
    [InlineData(85)]
    [InlineData(100)]
    public void Should_Return_High_Tier_For_High_Scores(int score)
    {
        // Arrange & Act
        var complexityScore = new ComplexityScore(
            score,
            Array.Empty<(string, HeuristicResult)>(),
            lowThreshold: 30,
            highThreshold: 70
        );

        // Assert
        complexityScore.Tier.Should().Be(ComplexityTier.High);
    }

    /// <summary>
    /// Test that individual results are preserved.
    /// </summary>
    [Fact]
    public void Should_Preserve_Individual_Results()
    {
        // Arrange
        var result1 = new HeuristicResult
        {
            Score = 30,
            Confidence = 0.8,
            Reasoning = "Test 1",
        };
        var result2 = new HeuristicResult
        {
            Score = 60,
            Confidence = 0.9,
            Reasoning = "Test 2",
        };
        var results = new (string, HeuristicResult)[]
        {
            ("FileCount", result1),
            ("TaskType", result2),
        };

        // Act
        var complexityScore = new ComplexityScore(45, results, 30, 70);

        // Assert
        complexityScore.IndividualResults.Should().HaveCount(2);
        complexityScore
            .IndividualResults.Should()
            .Contain(x => x.Name == "FileCount" && x.Result.Score == 30);
        complexityScore
            .IndividualResults.Should()
            .Contain(x => x.Name == "TaskType" && x.Result.Score == 60);
    }

    /// <summary>
    /// Test that combined score is stored.
    /// </summary>
    [Fact]
    public void Should_Store_Combined_Score()
    {
        // Arrange & Act
        var complexityScore = new ComplexityScore(
            75,
            Array.Empty<(string, HeuristicResult)>(),
            30,
            70
        );

        // Assert
        complexityScore.CombinedScore.Should().Be(75);
    }

    /// <summary>
    /// Test with custom thresholds.
    /// </summary>
    [Fact]
    public void Should_Respect_Custom_Thresholds()
    {
        // Arrange & Act - Use narrow thresholds
        var complexityScore = new ComplexityScore(
            50,
            Array.Empty<(string, HeuristicResult)>(),
            lowThreshold: 20,
            highThreshold: 40
        );

        // Assert - 50 should be high with these thresholds
        complexityScore.Tier.Should().Be(ComplexityTier.High);
    }

    /// <summary>
    /// Test edge case at low threshold boundary.
    /// </summary>
    [Fact]
    public void Score_Equal_To_Low_Threshold_Should_Be_Low()
    {
        // Arrange & Act
        var complexityScore = new ComplexityScore(
            30,
            Array.Empty<(string, HeuristicResult)>(),
            lowThreshold: 30,
            highThreshold: 70
        );

        // Assert
        complexityScore.Tier.Should().Be(ComplexityTier.Low);
    }

    /// <summary>
    /// Test edge case at high threshold boundary.
    /// </summary>
    [Fact]
    public void Score_Equal_To_High_Threshold_Should_Be_High()
    {
        // Arrange & Act
        var complexityScore = new ComplexityScore(
            70,
            Array.Empty<(string, HeuristicResult)>(),
            lowThreshold: 30,
            highThreshold: 70
        );

        // Assert
        complexityScore.Tier.Should().Be(ComplexityTier.High);
    }
}
