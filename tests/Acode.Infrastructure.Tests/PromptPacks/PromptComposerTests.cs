using Acode.Application.PromptPacks;
using Acode.Domain.PromptPacks;
using Acode.Infrastructure.PromptPacks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;

#pragma warning disable SA1615 // Element return value should be documented - not needed for xUnit tests

namespace Acode.Infrastructure.Tests.PromptPacks;

/// <summary>
/// Unit tests for PromptComposer from Task 008 parent spec (Tests 9-16).
/// </summary>
public class PromptComposerTests
{
    /// <summary>
    /// Test 9: Should Compose Base System Prompt Only.
    /// </summary>
    [Fact]
    public async Task Should_Compose_Base_System_Prompt_Only()
    {
        // Arrange
        var pack = CreatePack(
            new LoadedComponent("system.md", ComponentType.System, "You are a coding assistant.", null));
        var templateEngine = CreatePassThroughTemplateEngine();
        var composer = new PromptComposer(templateEngine);

        // Act
        var result = await composer.ComposeAsync(pack, new CompositionContext());

        // Assert
        result.Should().Be("You are a coding assistant.");
    }

    /// <summary>
    /// Test 10: Should Compose Base Plus Role Prompt.
    /// </summary>
    [Fact]
    public async Task Should_Compose_Base_Plus_Role_Prompt()
    {
        // Arrange
        var pack = CreatePack(
            new LoadedComponent("system.md", ComponentType.System, "You are a coding assistant.", null),
            new LoadedComponent(
                "roles/coder.md",
                ComponentType.Role,
                "Focus on clean, testable code.",
                new Dictionary<string, string> { ["role"] = "coder" }));
        var templateEngine = CreatePassThroughTemplateEngine();
        var composer = new PromptComposer(templateEngine);
        var context = new CompositionContext { Role = "coder" };

        // Act
        var result = await composer.ComposeAsync(pack, context);

        // Assert
        result.Should().Contain("You are a coding assistant.");
        result.Should().Contain("Focus on clean, testable code.");
    }

    /// <summary>
    /// Test 11: Should Compose Full Stack With Language And Framework.
    /// </summary>
    [Fact]
    public async Task Should_Compose_Full_Stack_With_Language_And_Framework()
    {
        // Arrange
        var pack = CreatePack(
            new LoadedComponent("system.md", ComponentType.System, "Base system prompt.", null),
            new LoadedComponent(
                "roles/coder.md",
                ComponentType.Role,
                "Role: coder guidance.",
                new Dictionary<string, string> { ["role"] = "coder" }),
            new LoadedComponent(
                "languages/csharp.md",
                ComponentType.Language,
                "C# conventions.",
                new Dictionary<string, string> { ["language"] = "csharp" }),
            new LoadedComponent(
                "frameworks/aspnetcore.md",
                ComponentType.Framework,
                "ASP.NET Core patterns.",
                new Dictionary<string, string> { ["framework"] = "aspnetcore" }));
        var templateEngine = CreatePassThroughTemplateEngine();
        var composer = new PromptComposer(templateEngine);
        var context = new CompositionContext
        {
            Role = "coder",
            Language = "csharp",
            Framework = "aspnetcore",
        };

        // Act
        var result = await composer.ComposeAsync(pack, context);

        // Assert
        result.Should().Contain("Base system prompt.");
        result.Should().Contain("Role: coder guidance.");
        result.Should().Contain("C# conventions.");
        result.Should().Contain("ASP.NET Core patterns.");
    }

    /// <summary>
    /// Test 12: Should Skip Optional Missing Components.
    /// </summary>
    [Fact]
    public async Task Should_Skip_Optional_Missing_Components()
    {
        // Arrange
        var pack = CreatePack(
            new LoadedComponent("system.md", ComponentType.System, "Base prompt.", null));
        var templateEngine = CreatePassThroughTemplateEngine();
        var composer = new PromptComposer(templateEngine);
        var context = new CompositionContext
        {
            Role = "coder", // Requested but not available
            Language = "python", // Requested but not available
        };

        // Act
        var result = await composer.ComposeAsync(pack, context);

        // Assert
        result.Should().Be("Base prompt.");
    }

    /// <summary>
    /// Test 13: Should Deduplicate Repeated Sections.
    /// </summary>
    [Fact]
    public async Task Should_Deduplicate_Repeated_Sections()
    {
        // Arrange
        var pack = CreatePack(
            new LoadedComponent(
                "system.md",
                ComponentType.System,
                "# Code Quality\n\nWrite clean code.\n\n# Testing\n\nWrite tests.",
                null),
            new LoadedComponent(
                "roles/coder.md",
                ComponentType.Role,
                "\n\n# Code Quality\n\nWrite clean code.\n\n# Additional Guidance\n\nUse TDD.",
                new Dictionary<string, string> { ["role"] = "coder" }));
        var templateEngine = CreatePassThroughTemplateEngine();
        var composer = new PromptComposer(templateEngine);
        var context = new CompositionContext { Role = "coder" };

        // Act
        var result = await composer.ComposeAsync(pack, context);

        // Assert
        result.Should().Contain("# Code Quality");
        result.Should().Contain("Write clean code.");
        result.Should().Contain("# Testing");
        result.Should().Contain("# Additional Guidance");

        // Should not contain duplicate "# Code Quality" section
        var codeQualityCount = System.Text.RegularExpressions.Regex.Matches(result, "# Code Quality").Count;
        codeQualityCount.Should().Be(1);
    }

    /// <summary>
    /// Test 14: Should Enforce Maximum Prompt Length.
    /// </summary>
    [Fact]
    public async Task Should_Enforce_Maximum_Prompt_Length()
    {
        // Arrange
        var largeContent = new string('x', 20000);
        var pack = CreatePack(
            new LoadedComponent("system.md", ComponentType.System, largeContent, null),
            new LoadedComponent(
                "roles/coder.md",
                ComponentType.Role,
                largeContent,
                new Dictionary<string, string> { ["role"] = "coder" }));
        var templateEngine = CreatePassThroughTemplateEngine();
        var composer = new PromptComposer(templateEngine, maxLength: 32000);
        var context = new CompositionContext { Role = "coder" };

        // Act
        var result = await composer.ComposeAsync(pack, context);

        // Assert
        result.Length.Should().BeLessOrEqualTo(32000);
    }

    /// <summary>
    /// Test 15: Should Log Composition Hash.
    /// </summary>
    [Fact]
    public async Task Should_Log_Composition_Hash()
    {
        // Arrange
        var pack = CreatePack(
            new LoadedComponent("system.md", ComponentType.System, "Test prompt.", null));
        var templateEngine = CreatePassThroughTemplateEngine();
        var logger = Substitute.For<ILogger<PromptComposer>>();
        var composer = new PromptComposer(templateEngine, logger: logger);

        // Act
        await composer.ComposeAsync(pack, new CompositionContext());

        // Assert - verify logger was called
        logger.ReceivedCalls().Should().NotBeEmpty();
    }

    /// <summary>
    /// Test 16: Should Apply Template Variables During Composition.
    /// </summary>
    [Fact]
    public async Task Should_Apply_Template_Variables_During_Composition()
    {
        // Arrange
        var pack = CreatePack(
            new LoadedComponent(
                "system.md",
                ComponentType.System,
                "Working on {{workspace_name}} using {{language}}.",
                null));
        var templateEngine = Substitute.For<ITemplateEngine>();
        templateEngine
            .Substitute(Arg.Any<string>(), Arg.Any<CompositionContext>())
            .Returns(callInfo =>
            {
                var content = callInfo.ArgAt<string>(0);
                return content
                    .Replace("{{workspace_name}}", "MyProject", StringComparison.Ordinal)
                    .Replace("{{language}}", "csharp", StringComparison.Ordinal);
            });

        var composer = new PromptComposer(templateEngine);
        var context = new CompositionContext
        {
            Variables = new Dictionary<string, string>
            {
                ["workspace_name"] = "MyProject",
                ["language"] = "csharp",
            },
        };

        // Act
        var result = await composer.ComposeAsync(pack, context);

        // Assert
        result.Should().Be("Working on MyProject using csharp.");
        templateEngine.Received(1).Substitute(Arg.Any<string>(), context);
    }

    private static PromptPack CreatePack(params LoadedComponent[] components)
    {
        return new PromptPack(
            "test-pack",
            new PackVersion(1, 0, 0),
            "Test Pack",
            null,
            PackSource.BuiltIn,
            "/test",
            null,
            components.ToList().AsReadOnly());
    }

    private static ITemplateEngine CreatePassThroughTemplateEngine()
    {
        var templateEngine = Substitute.For<ITemplateEngine>();
        templateEngine
            .Substitute(Arg.Any<string>(), Arg.Any<CompositionContext>())
            .Returns(callInfo => callInfo.ArgAt<string>(0));
        return templateEngine;
    }
}
