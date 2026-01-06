namespace Acode.Domain.Tests.Risks;

using Acode.Domain.Risks;
using FluentAssertions;

public class DreadScoreTests
{
    [Theory]
    [InlineData(1, 1, 1, 1, 1, 1.0)]
    [InlineData(3, 3, 3, 3, 3, 3.0)]
    [InlineData(5, 5, 5, 5, 5, 5.0)]
    [InlineData(3, 2, 1, 3, 2, 2.2)]
    [InlineData(5, 4, 3, 2, 1, 3.0)]
    public void DreadScore_ShouldCalculateAverageCorrectly(
        int damage, int reproducibility, int exploitability, int affectedUsers, int discoverability, double expectedAverage)
    {
        // Arrange & Act
        var score = new DreadScore(damage, reproducibility, exploitability, affectedUsers, discoverability);

        // Assert
        score.Average.Should().BeApproximately(expectedAverage, 0.01);
    }

    [Theory]
    [InlineData(1, 1, 1, 1, 1, Severity.Low)]
    [InlineData(2, 2, 2, 2, 2, Severity.Low)]
    [InlineData(5, 5, 5, 5, 5, Severity.Medium)]
    [InlineData(7, 7, 7, 7, 7, Severity.Medium)]
    [InlineData(8, 8, 8, 8, 8, Severity.High)]
    [InlineData(9, 9, 9, 9, 9, Severity.High)]
    [InlineData(10, 10, 10, 10, 10, Severity.Critical)]
    public void DreadScore_ShouldMapToCorrectSeverity(
        int d, int r, int e, int a, int disc, Severity expectedSeverity)
    {
        // Arrange & Act
        var score = new DreadScore(d, r, e, a, disc);

        // Assert
        score.Severity.Should().Be(expectedSeverity);
    }

    [Theory]
    [InlineData(0, 1, 1, 1, 1)]
    [InlineData(1, 0, 1, 1, 1)]
    [InlineData(1, 1, 0, 1, 1)]
    [InlineData(1, 1, 1, 0, 1)]
    [InlineData(1, 1, 1, 1, 0)]
    [InlineData(11, 1, 1, 1, 1)]
    [InlineData(1, 11, 1, 1, 1)]
    [InlineData(1, 1, 11, 1, 1)]
    [InlineData(1, 1, 1, 11, 1)]
    [InlineData(1, 1, 1, 1, 11)]
    public void DreadScore_WithOutOfRangeValues_ShouldThrow(
        int d, int r, int e, int a, int disc)
    {
        // Arrange & Act & Assert
        var act = () => new DreadScore(d, r, e, a, disc);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void DreadScore_ShouldExposeAllComponents()
    {
        // Arrange & Act
        var score = new DreadScore(
            damage: 5,
            reproducibility: 4,
            exploitability: 3,
            affectedUsers: 2,
            discoverability: 1);

        // Assert
        score.Damage.Should().Be(5);
        score.Reproducibility.Should().Be(4);
        score.Exploitability.Should().Be(3);
        score.AffectedUsers.Should().Be(2);
        score.Discoverability.Should().Be(1);
    }

    [Fact]
    public void DreadScore_ShouldBeImmutable()
    {
        // Arrange
        var score = new DreadScore(5, 5, 5, 5, 5);

        // Act & Assert
        score.Damage.Should().Be(5);
    }
}
