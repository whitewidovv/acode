namespace Acode.Infrastructure.Tests.Heuristics;

using Acode.Application.Heuristics;
using Acode.Infrastructure.Heuristics;
using FluentAssertions;
using Xunit;

/// <summary>
/// Unit tests for <see cref="LanguageHeuristic"/>.
/// Tests AC-024: LanguageHeuristic scores correctly.
/// </summary>
public sealed class LanguageHeuristicTests
{
    private readonly LanguageHeuristic _sut = new();

    /// <summary>
    /// Test that markdown files score low.
    /// </summary>
    [Fact]
    public void Should_Return_Low_Score_For_Markdown_Files()
    {
        // Arrange
        var context = new HeuristicContext
        {
            TaskDescription = "Update documentation",
            Files = new List<string> { "README.md", "CHANGELOG.md" },
        };

        // Act
        var result = _sut.Evaluate(context);

        // Assert
        result.Score.Should().BeLessThan(20);
        result.Confidence.Should().BeGreaterThan(0.8);
    }

    /// <summary>
    /// Test that JSON files score low.
    /// </summary>
    [Fact]
    public void Should_Return_Low_Score_For_Json_Files()
    {
        // Arrange
        var context = new HeuristicContext
        {
            TaskDescription = "Update configuration",
            Files = new List<string> { "config.json", "appsettings.json" },
        };

        // Act
        var result = _sut.Evaluate(context);

        // Assert
        result.Score.Should().BeLessThan(20);
    }

    /// <summary>
    /// Test that C# files score medium.
    /// </summary>
    [Fact]
    public void Should_Return_Medium_Score_For_CSharp_Files()
    {
        // Arrange
        var context = new HeuristicContext
        {
            TaskDescription = "Update service",
            Files = new List<string> { "Service.cs", "ServiceTests.cs" },
        };

        // Act
        var result = _sut.Evaluate(context);

        // Assert
        result.Score.Should().BeInRange(25, 50);
    }

    /// <summary>
    /// Test that Rust files score high.
    /// </summary>
    [Fact]
    public void Should_Return_High_Score_For_Rust_Files()
    {
        // Arrange
        var context = new HeuristicContext
        {
            TaskDescription = "Update Rust code",
            Files = new List<string> { "lib.rs", "main.rs" },
        };

        // Act
        var result = _sut.Evaluate(context);

        // Assert
        result.Score.Should().BeGreaterThan(40);
    }

    /// <summary>
    /// Test that mixed languages produce weighted average.
    /// </summary>
    [Fact]
    public void Should_Average_Scores_For_Mixed_Languages()
    {
        // Arrange - mix of low (.md) and medium (.cs) complexity
        var context = new HeuristicContext
        {
            TaskDescription = "Update code and docs",
            Files = new List<string> { "README.md", "Service.cs" },
        };

        // Act
        var result = _sut.Evaluate(context);

        // Assert - should be between low (.md ~5) and medium (.cs ~35)
        result.Score.Should().BeInRange(15, 30);
        result.Reasoning.Should().Contain("languages");
    }

    /// <summary>
    /// Test that name and priority are set correctly.
    /// </summary>
    [Fact]
    public void Should_Return_Name_And_Priority()
    {
        // Act & Assert
        _sut.Name.Should().Be("Language");
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
    /// Test that empty file list returns low score with low confidence.
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
        result.Confidence.Should().BeLessThanOrEqualTo(0.1);
    }

    /// <summary>
    /// Test that unknown extensions get default score.
    /// </summary>
    [Fact]
    public void Should_Return_Default_Score_For_Unknown_Extensions()
    {
        // Arrange
        var context = new HeuristicContext
        {
            TaskDescription = "Update files",
            Files = new List<string> { "file.xyz", "data.abc" },
        };

        // Act
        var result = _sut.Evaluate(context);

        // Assert
        result.Score.Should().Be(25);
        result.Confidence.Should().Be(0.9);
    }

    /// <summary>
    /// Test scores for various language files.
    /// </summary>
    /// <param name="extension">The file extension.</param>
    /// <param name="expectedMinScore">The expected minimum score.</param>
    /// <param name="expectedMaxScore">The expected maximum score.</param>
    [Theory]
    [InlineData(".py", 20, 35)]
    [InlineData(".js", 20, 35)]
    [InlineData(".ts", 25, 40)]
    [InlineData(".java", 30, 45)]
    [InlineData(".go", 25, 40)]
    [InlineData(".cpp", 35, 50)]
    public void Should_Score_Language_Appropriately(
        string extension,
        int expectedMinScore,
        int expectedMaxScore
    )
    {
        // Arrange
        var context = new HeuristicContext
        {
            TaskDescription = "Update code",
            Files = new List<string> { $"file{extension}" },
        };

        // Act
        var result = _sut.Evaluate(context);

        // Assert
        result.Score.Should().BeInRange(expectedMinScore, expectedMaxScore);
    }
}
