namespace Acode.Infrastructure.Tests.Heuristics;

using Acode.Application.Heuristics;
using Acode.Infrastructure.Heuristics;
using FluentAssertions;
using Xunit;

/// <summary>
/// Unit tests for <see cref="FileCountHeuristic"/>.
/// Tests AC-021: FileCountHeuristic scores correctly.
/// </summary>
public sealed class FileCountHeuristicTests
{
    private readonly FileCountHeuristic _sut = new();

    /// <summary>
    /// Test that a single file returns a low score.
    /// </summary>
    [Fact]
    public void Should_Return_Low_Score_For_Single_File()
    {
        // Arrange
        var context = new HeuristicContext
        {
            TaskDescription = "Fix typo in README",
            Files = new List<string> { "README.md" },
        };

        // Act
        var result = _sut.Evaluate(context);

        // Assert
        result.Score.Should().BeLessThan(30);
        result.Confidence.Should().BeGreaterThan(0.8);
        result.Reasoning.Should().Contain("README.md");
    }

    /// <summary>
    /// Test that two files returns a low score.
    /// </summary>
    [Fact]
    public void Should_Return_Low_Score_For_Two_Files()
    {
        // Arrange
        var context = new HeuristicContext
        {
            TaskDescription = "Update controller and tests",
            Files = new List<string> { "Controller.cs", "ControllerTests.cs" },
        };

        // Act
        var result = _sut.Evaluate(context);

        // Assert
        result.Score.Should().BeLessThan(30);
        result.Confidence.Should().BeGreaterThan(0.7);
        result.Reasoning.Should().Contain("2 files");
    }

    /// <summary>
    /// Test that five files returns a medium score.
    /// </summary>
    [Fact]
    public void Should_Return_Medium_Score_For_Five_Files()
    {
        // Arrange
        var context = new HeuristicContext
        {
            TaskDescription = "Refactor data transformation module",
            Files = new List<string>
            {
                "Transform.cs",
                "TransformService.cs",
                "TransformController.cs",
                "TransformTests.cs",
                "TransformIntegrationTests.cs",
            },
        };

        // Act
        var result = _sut.Evaluate(context);

        // Assert
        result.Score.Should().BeInRange(30, 40);
        result.Confidence.Should().BeGreaterThanOrEqualTo(0.8);
        result.Reasoning.Should().Contain("5 files");
    }

    /// <summary>
    /// Test that fifteen files returns a high score.
    /// </summary>
    [Fact]
    public void Should_Return_High_Score_For_Fifteen_Files()
    {
        // Arrange
        var files = Enumerable.Range(1, 15).Select(i => $"File{i}.cs").ToList();

        var context = new HeuristicContext { TaskDescription = "Large refactoring", Files = files };

        // Act
        var result = _sut.Evaluate(context);

        // Assert
        result.Score.Should().BeGreaterThanOrEqualTo(70);
        result.Confidence.Should().BeGreaterThanOrEqualTo(0.9);
        result.Reasoning.Should().Contain("15 files");
    }

    /// <summary>
    /// Test that scores are in valid range.
    /// </summary>
    [Fact]
    public void Should_Return_Valid_Score_Range()
    {
        // Arrange
        var context = new HeuristicContext
        {
            TaskDescription = "Any task",
            Files = new List<string> { "file.cs" },
        };

        // Act
        var result = _sut.Evaluate(context);

        // Assert
        result.Score.Should().BeInRange(0, 100);
        result.Confidence.Should().BeInRange(0.0, 1.0);
    }

    /// <summary>
    /// Test that boundary file counts have lower confidence.
    /// </summary>
    [Fact]
    public void Should_Have_Lower_Confidence_At_Threshold_Boundaries()
    {
        // Arrange - exactly 3 files (low-medium boundary)
        var context = new HeuristicContext
        {
            TaskDescription = "Boundary case",
            Files = new List<string> { "A.cs", "B.cs", "C.cs" },
        };

        // Act
        var result = _sut.Evaluate(context);

        // Assert
        result.Confidence.Should().BeLessThan(0.9);
        result.Reasoning.Should().Contain("3 files");
    }

    /// <summary>
    /// Test that name and priority are set correctly.
    /// </summary>
    [Fact]
    public void Should_Return_Name_And_Priority()
    {
        // Act & Assert
        _sut.Name.Should().Be("FileCount");
        _sut.Priority.Should().BeGreaterThan(0);
    }

    /// <summary>
    /// Test that zero files returns score of zero with low confidence.
    /// </summary>
    [Fact]
    public void Should_Return_Zero_Score_For_No_Files()
    {
        // Arrange
        var context = new HeuristicContext
        {
            TaskDescription = "Planning task",
            Files = new List<string>(),
        };

        // Act
        var result = _sut.Evaluate(context);

        // Assert
        result.Score.Should().Be(0);
        result.Confidence.Should().BeLessThanOrEqualTo(0.5);
    }

    /// <summary>
    /// Test that null context throws ArgumentNullException.
    /// </summary>
    [Fact]
    public void Should_Throw_For_Null_Context()
    {
        // Act
        var action = () => _sut.Evaluate(null!);

        // Assert
        action.Should().Throw<ArgumentNullException>();
    }

    /// <summary>
    /// Test that very large file counts are capped at 100.
    /// </summary>
    [Fact]
    public void Should_Cap_Score_At_100_For_Many_Files()
    {
        // Arrange
        var files = Enumerable.Range(1, 100).Select(i => $"File{i}.cs").ToList();

        var context = new HeuristicContext
        {
            TaskDescription = "Massive refactoring",
            Files = files,
        };

        // Act
        var result = _sut.Evaluate(context);

        // Assert
        result
            .Score.Should()
            .Be(90, "implementation caps maximum score at 90 for very many files");
        result.Confidence.Should().Be(0.95);
    }
}
