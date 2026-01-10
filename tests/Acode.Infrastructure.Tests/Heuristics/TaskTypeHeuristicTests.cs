namespace Acode.Infrastructure.Tests.Heuristics;

using Acode.Application.Heuristics;
using Acode.Infrastructure.Heuristics;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

/// <summary>
/// Unit tests for <see cref="TaskTypeHeuristic"/>.
/// Tests AC-022: TaskTypeHeuristic scores correctly.
/// Tests AC-068: Security keywords force high scores.
/// </summary>
public sealed class TaskTypeHeuristicTests
{
    private readonly TaskTypeHeuristic _sut;

    /// <summary>
    /// Initializes a new instance of the <see cref="TaskTypeHeuristicTests"/> class.
    /// </summary>
    public TaskTypeHeuristicTests()
    {
        ILogger<TaskTypeHeuristic> logger = NullLogger<TaskTypeHeuristic>.Instance;
        _sut = new TaskTypeHeuristic(logger);
    }

    /// <summary>
    /// Test that bug fixes score lower than features.
    /// </summary>
    [Fact]
    public void Should_Score_Bug_Fix_Lower_Than_Feature()
    {
        // Arrange
        var bugContext = new HeuristicContext
        {
            TaskDescription = "Fix null reference exception in login handler",
            Files = new List<string> { "LoginHandler.cs" },
        };

        var featureContext = new HeuristicContext
        {
            TaskDescription = "Implement new password reset feature",
            Files = new List<string> { "PasswordReset.cs" },
        };

        // Act
        var bugResult = _sut.Evaluate(bugContext);
        var featureResult = _sut.Evaluate(featureContext);

        // Assert
        bugResult.Score.Should().BeLessThan(featureResult.Score);
        bugResult.Reasoning.Should().ContainAny("bug", "Bug");
    }

    /// <summary>
    /// Test that refactoring scores higher than enhancement.
    /// </summary>
    [Fact]
    public void Should_Score_Refactor_Higher_Than_Enhancement()
    {
        // Arrange
        var enhancementContext = new HeuristicContext
        {
            TaskDescription = "Add validation to existing form",
            Files = new List<string> { "Form.cs" },
        };

        var refactorContext = new HeuristicContext
        {
            TaskDescription = "Refactor the data transformation layer for better performance",
            Files = new List<string> { "DataLayer.cs" },
        };

        // Act
        var enhancementResult = _sut.Evaluate(enhancementContext);
        var refactorResult = _sut.Evaluate(refactorContext);

        // Assert
        refactorResult.Score.Should().BeGreaterThan(enhancementResult.Score);
        refactorResult.Reasoning.Should().ContainAny("refactor", "Refactor");
    }

    /// <summary>
    /// Test that task types get expected score ranges.
    /// </summary>
    /// <param name="taskDescription">The task description.</param>
    /// <param name="expectedMinScore">The expected minimum score.</param>
    /// <param name="expectedMaxScore">The expected maximum score.</param>
    [Theory]
    [InlineData("Fix typo in comment", 10, 35)]
    [InlineData("Add input validation", 25, 55)]
    [InlineData("Create new data export feature", 45, 75)]
    [InlineData("Refactor to clean architecture", 65, 95)]
    public void Should_Assign_Expected_Score_Range_For_Task_Type(
        string taskDescription,
        int expectedMinScore,
        int expectedMaxScore
    )
    {
        // Arrange
        var context = new HeuristicContext
        {
            TaskDescription = taskDescription,
            Files = new List<string> { "file.cs" },
        };

        // Act
        var result = _sut.Evaluate(context);

        // Assert
        result.Score.Should().BeInRange(expectedMinScore, expectedMaxScore);
    }

    /// <summary>
    /// Test that security keywords force high score (AC-068).
    /// </summary>
    [Fact]
    public void Should_Detect_Security_Critical_Task_And_Force_High_Score()
    {
        // Arrange
        var context = new HeuristicContext
        {
            TaskDescription = "Update authentication token validation logic",
            Files = new List<string> { "Auth.cs" },
        };

        // Act
        var result = _sut.Evaluate(context);

        // Assert
        result.Score.Should().BeGreaterThan(70);
        result.Confidence.Should().Be(1.0);
        result.Reasoning.Should().ContainAny("security", "Security");
    }

    /// <summary>
    /// Test that all security keywords force high score.
    /// </summary>
    /// <param name="taskDescription">The task description with security keyword.</param>
    [Theory]
    [InlineData("Implement encryption for user passwords")]
    [InlineData("Fix SQL injection vulnerability")]
    [InlineData("Add CSRF protection")]
    [InlineData("Update authorization rules")]
    [InlineData("Implement OAuth flow")]
    [InlineData("Add JWT token validation")]
    public void Should_Force_High_Score_For_All_Security_Keywords(string taskDescription)
    {
        // Arrange
        var context = new HeuristicContext
        {
            TaskDescription = taskDescription,
            Files = new List<string> { "Security.cs" },
        };

        // Act
        var result = _sut.Evaluate(context);

        // Assert
        result.Score.Should().BeGreaterThan(70);
        result.Confidence.Should().Be(1.0);
    }

    /// <summary>
    /// Test that ambiguous tasks get medium scores with lower confidence.
    /// </summary>
    [Fact]
    public void Should_Return_Medium_Score_For_Ambiguous_Task()
    {
        // Arrange
        var context = new HeuristicContext
        {
            TaskDescription = "Do something with the code",
            Files = new List<string> { "file.cs" },
        };

        // Act
        var result = _sut.Evaluate(context);

        // Assert
        result.Score.Should().BeInRange(40, 60);
        result.Confidence.Should().BeLessThanOrEqualTo(0.5);
    }

    /// <summary>
    /// Test that name and priority are set correctly.
    /// </summary>
    [Fact]
    public void Should_Return_Name_And_Priority()
    {
        // Act & Assert
        _sut.Name.Should().Be("TaskType");
        _sut.Priority.Should().BeGreaterThan(0);
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
    /// Test that empty task description gets low confidence.
    /// </summary>
    [Fact]
    public void Should_Return_Low_Confidence_For_Empty_Description()
    {
        // Arrange
        var context = new HeuristicContext
        {
            TaskDescription = string.Empty,
            Files = new List<string> { "file.cs" },
        };

        // Act
        var result = _sut.Evaluate(context);

        // Assert
        result.Confidence.Should().BeLessThan(0.6);
    }
}
