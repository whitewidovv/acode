using Acode.Domain.PromptPacks;
using Acode.Infrastructure.PromptPacks;
using FluentAssertions;
using Xunit;

namespace Acode.Infrastructure.Tests.PromptPacks;

/// <summary>
/// Unit tests for ComponentMerger from Task 008 parent spec (Tests 17-22).
/// </summary>
public class ComponentMergerTests
{
    /// <summary>
    /// Test 17: Should Merge Components In Correct Order.
    /// </summary>
    [Fact]
    public void Should_Merge_Components_In_Correct_Order()
    {
        // Arrange
        var components = new List<LoadedComponent>
        {
            new LoadedComponent(
                "framework/aspnetcore.md",
                ComponentType.Framework,
                "D",
                new Dictionary<string, string> { ["framework"] = "aspnetcore" }),
            new LoadedComponent(
                "languages/csharp.md",
                ComponentType.Language,
                "C",
                new Dictionary<string, string> { ["language"] = "csharp" }),
            new LoadedComponent(
                "roles/coder.md",
                ComponentType.Role,
                "B",
                new Dictionary<string, string> { ["role"] = "coder" }),
            new LoadedComponent(
                "system.md",
                ComponentType.System,
                "A",
                null),
        };
        var merger = new ComponentMerger();
        var context = new CompositionContext
        {
            Role = "coder",
            Language = "csharp",
            Framework = "aspnetcore",
        };

        // Act
        var result = merger.Merge(components, context);

        // Assert
        result.Should().StartWith("A");
        result.Should().Contain("B");
        result.Should().Contain("C");
        result.Should().EndWith("D");
    }

    /// <summary>
    /// Test 18: Should Handle Override Markers.
    /// </summary>
    [Fact]
    public void Should_Handle_Override_Markers()
    {
        // Arrange
        var components = new List<LoadedComponent>
        {
            new LoadedComponent(
                "system.md",
                ComponentType.System,
                "# Section A\n\nOriginal content for A.\n\n# Section B\n\nOriginal content for B.",
                null),
            new LoadedComponent(
                "roles/coder.md",
                ComponentType.Role,
                "\n\n# OVERRIDE: Section A\n\nReplacement content for A.",
                new Dictionary<string, string> { ["role"] = "coder" }),
        };
        var merger = new ComponentMerger();
        var context = new CompositionContext { Role = "coder" };

        // Act
        var result = merger.Merge(components, context);

        // Assert
        result.Should().Contain("Replacement content for A");
        result.Should().NotContain("Original content for A");
        result.Should().Contain("Original content for B");
    }

    /// <summary>
    /// Test 19: Should Filter Components By Context.
    /// </summary>
    [Fact]
    public void Should_Filter_Components_By_Context()
    {
        // Arrange
        var components = new List<LoadedComponent>
        {
            new LoadedComponent("system.md", ComponentType.System, "Base", null),
            new LoadedComponent(
                "roles/coder.md",
                ComponentType.Role,
                "Coder role",
                new Dictionary<string, string> { ["role"] = "coder" }),
            new LoadedComponent(
                "roles/planner.md",
                ComponentType.Role,
                "Planner role",
                new Dictionary<string, string> { ["role"] = "planner" }),
            new LoadedComponent(
                "languages/csharp.md",
                ComponentType.Language,
                "C#",
                new Dictionary<string, string> { ["language"] = "csharp" }),
            new LoadedComponent(
                "languages/python.md",
                ComponentType.Language,
                "Python",
                new Dictionary<string, string> { ["language"] = "python" }),
        };
        var merger = new ComponentMerger();
        var context = new CompositionContext
        {
            Role = "coder",
            Language = "csharp",
        };

        // Act
        var result = merger.Merge(components, context);

        // Assert
        result.Should().Contain("Base");
        result.Should().Contain("Coder role");
        result.Should().Contain("C#");
        result.Should().NotContain("Planner role");
        result.Should().NotContain("Python");
    }

    /// <summary>
    /// Test 20: Should Remove Duplicate Markdown Headings.
    /// </summary>
    [Fact]
    public void Should_Remove_Duplicate_Markdown_Headings()
    {
        // Arrange
        var components = new List<LoadedComponent>
        {
            new LoadedComponent(
                "system.md",
                ComponentType.System,
                "# Code Quality\n\nContent 1.\n\n# Performance\n\nContent 2.",
                null),
            new LoadedComponent(
                "roles/coder.md",
                ComponentType.Role,
                "\n\n# Code Quality\n\nAdditional content.\n\n# New Section\n\nContent 3.",
                new Dictionary<string, string> { ["role"] = "coder" }),
        };
        var merger = new ComponentMerger(deduplicateHeadings: true);
        var context = new CompositionContext { Role = "coder" };

        // Act
        var result = merger.Merge(components, context);

        // Assert
        var headingMatches = System.Text.RegularExpressions.Regex.Matches(result, @"^# Code Quality$", System.Text.RegularExpressions.RegexOptions.Multiline);
        headingMatches.Count.Should().Be(1);
    }

    /// <summary>
    /// Test 21: Should Preserve Component Separation With Newlines.
    /// </summary>
    [Fact]
    public void Should_Preserve_Component_Separation_With_Newlines()
    {
        // Arrange
        var components = new List<LoadedComponent>
        {
            new LoadedComponent("system.md", ComponentType.System, "System prompt.", null),
            new LoadedComponent(
                "roles/coder.md",
                ComponentType.Role,
                "Role prompt.",
                new Dictionary<string, string> { ["role"] = "coder" }),
        };
        var merger = new ComponentMerger();
        var context = new CompositionContext { Role = "coder" };

        // Act
        var result = merger.Merge(components, context);

        // Assert
        result.Should().Contain("\n\n");
        result.Should().MatchRegex(@"System prompt\.\s+Role prompt\.");
    }

    /// <summary>
    /// Test 22: Should Handle Empty Components Gracefully.
    /// </summary>
    [Fact]
    public void Should_Handle_Empty_Components_Gracefully()
    {
        // Arrange
        var components = new List<LoadedComponent>
        {
            new LoadedComponent("system.md", ComponentType.System, "System prompt.", null),
            new LoadedComponent(
                "roles/coder.md",
                ComponentType.Role,
                string.Empty,
                new Dictionary<string, string> { ["role"] = "coder" }),
            new LoadedComponent(
                "languages/csharp.md",
                ComponentType.Language,
                "   \n  ",
                new Dictionary<string, string> { ["language"] = "csharp" }),
        };
        var merger = new ComponentMerger();
        var context = new CompositionContext { Role = "coder", Language = "csharp" };

        // Act
        var result = merger.Merge(components, context);

        // Assert
        result.Should().Be("System prompt.");
    }
}
