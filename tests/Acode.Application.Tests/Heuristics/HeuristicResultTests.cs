namespace Acode.Application.Tests.Heuristics;

using Acode.Application.Heuristics;
using FluentAssertions;
using Xunit;

/// <summary>
/// Unit tests for <see cref="HeuristicResult"/>.
/// </summary>
public sealed class HeuristicResultTests
{
    /// <summary>
    /// Test that valid scores pass validation.
    /// </summary>
    /// <param name="score">The score to test.</param>
    [Theory]
    [InlineData(0)]
    [InlineData(50)]
    [InlineData(100)]
    public void Validate_Should_Pass_For_Valid_Score(int score)
    {
        // Arrange
        var result = new HeuristicResult
        {
            Score = score,
            Confidence = 0.5,
            Reasoning = "Test reasoning",
        };

        // Act
        var action = () => result.Validate();

        // Assert
        action.Should().NotThrow();
    }

    /// <summary>
    /// Test that invalid scores fail validation.
    /// </summary>
    /// <param name="score">The invalid score to test.</param>
    [Theory]
    [InlineData(-1)]
    [InlineData(101)]
    [InlineData(-100)]
    [InlineData(200)]
    public void Validate_Should_Throw_For_Invalid_Score(int score)
    {
        // Arrange
        var result = new HeuristicResult
        {
            Score = score,
            Confidence = 0.5,
            Reasoning = "Test",
        };

        // Act
        var action = () => result.Validate();

        // Assert
        action
            .Should()
            .Throw<ArgumentOutOfRangeException>()
            .WithMessage("*Score must be between 0 and 100*");
    }

    /// <summary>
    /// Test that valid confidence values pass validation.
    /// </summary>
    /// <param name="confidence">The confidence value to test.</param>
    [Theory]
    [InlineData(0.0)]
    [InlineData(0.5)]
    [InlineData(1.0)]
    public void Validate_Should_Pass_For_Valid_Confidence(double confidence)
    {
        // Arrange
        var result = new HeuristicResult
        {
            Score = 50,
            Confidence = confidence,
            Reasoning = "Test",
        };

        // Act
        var action = () => result.Validate();

        // Assert
        action.Should().NotThrow();
    }

    /// <summary>
    /// Test that invalid confidence values fail validation.
    /// </summary>
    /// <param name="confidence">The invalid confidence value to test.</param>
    [Theory]
    [InlineData(-0.1)]
    [InlineData(1.1)]
    [InlineData(-1.0)]
    [InlineData(2.0)]
    public void Validate_Should_Throw_For_Invalid_Confidence(double confidence)
    {
        // Arrange
        var result = new HeuristicResult
        {
            Score = 50,
            Confidence = confidence,
            Reasoning = "Test",
        };

        // Act
        var action = () => result.Validate();

        // Assert
        action
            .Should()
            .Throw<ArgumentOutOfRangeException>()
            .WithMessage("*Confidence must be between 0*");
    }

    /// <summary>
    /// Test that null reasoning fails validation.
    /// </summary>
    [Fact]
    public void Validate_Should_Throw_For_Null_Reasoning()
    {
        // Arrange
        var result = new HeuristicResult
        {
            Score = 50,
            Confidence = 0.5,
            Reasoning = null!,
        };

        // Act
        var action = () => result.Validate();

        // Assert
        action.Should().Throw<ArgumentException>().WithMessage("*Reasoning must not be empty*");
    }

    /// <summary>
    /// Test that empty reasoning fails validation.
    /// </summary>
    [Fact]
    public void Validate_Should_Throw_For_Empty_Reasoning()
    {
        // Arrange
        var result = new HeuristicResult
        {
            Score = 50,
            Confidence = 0.5,
            Reasoning = string.Empty,
        };

        // Act
        var action = () => result.Validate();

        // Assert
        action.Should().Throw<ArgumentException>().WithMessage("*Reasoning must not be empty*");
    }
}
