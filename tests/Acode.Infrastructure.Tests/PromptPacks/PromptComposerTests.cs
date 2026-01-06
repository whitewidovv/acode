using Acode.Application.PromptPacks;
using Acode.Domain.PromptPacks;
using Acode.Infrastructure.PromptPacks;
using FluentAssertions;
using NSubstitute;

namespace Acode.Infrastructure.Tests.PromptPacks;

/// <summary>
/// Tests for <see cref="PromptComposer"/>.
/// </summary>
public class PromptComposerTests
{
    [Fact]
    public void Compose_BaseSystemPromptOnly_ReturnsSystemPrompt()
    {
        // Arrange
        var templateEngine = Substitute.For<ITemplateEngine>();
        templateEngine.Substitute(Arg.Any<string>(), Arg.Any<Dictionary<string, string>>())
            .Returns(call => call.Arg<string>()); // Return template unchanged

        var composer = new PromptComposer(templateEngine);

        var pack = CreatePackWithComponents(
            ("system.md", "This is the system prompt."));

        var context = new CompositionContext();

        // Act
        var result = composer.Compose(pack, context);

        // Assert
        result.Should().Be("This is the system prompt.");
    }

    [Fact]
    public void Compose_WithRole_IncludesRoleComponent()
    {
        // Arrange
        var templateEngine = Substitute.For<ITemplateEngine>();
        templateEngine.Substitute(Arg.Any<string>(), Arg.Any<Dictionary<string, string>>())
            .Returns(call => call.Arg<string>());

        var composer = new PromptComposer(templateEngine);

        var pack = CreatePackWithComponents(
            ("system.md", "System prompt."),
            ("roles/coder.md", "Coder role guidance."));

        var context = CompositionContext.ForRole("coder");

        // Act
        var result = composer.Compose(pack, context);

        // Assert
        result.Should().Contain("System prompt.");
        result.Should().Contain("Coder role guidance.");
        result.Should().Contain("\n\n"); // Components separated by double newline
    }

    [Fact]
    public void Compose_FullStack_IncludesAllComponents()
    {
        // Arrange
        var templateEngine = Substitute.For<ITemplateEngine>();
        templateEngine.Substitute(Arg.Any<string>(), Arg.Any<Dictionary<string, string>>())
            .Returns(call => call.Arg<string>());

        var composer = new PromptComposer(templateEngine);

        var pack = CreatePackWithComponents(
            ("system.md", "System."),
            ("roles/coder.md", "Coder."),
            ("languages/csharp.md", "C# guidance."),
            ("frameworks/aspnetcore.md", "ASP.NET Core."));

        var context = new CompositionContext(
            Role: "coder",
            Language: "csharp",
            Framework: "aspnetcore");

        // Act
        var result = composer.Compose(pack, context);

        // Assert
        result.Should().Contain("System.");
        result.Should().Contain("Coder.");
        result.Should().Contain("C# guidance.");
        result.Should().Contain("ASP.NET Core.");
    }

    [Fact]
    public void Compose_MissingOptionalComponent_SkipsGracefully()
    {
        // Arrange
        var templateEngine = Substitute.For<ITemplateEngine>();
        templateEngine.Substitute(Arg.Any<string>(), Arg.Any<Dictionary<string, string>>())
            .Returns(call => call.Arg<string>());

        var composer = new PromptComposer(templateEngine);

        var pack = CreatePackWithComponents(
            ("system.md", "System prompt."));

        var context = CompositionContext.ForRole("nonexistent");

        // Act
        var result = composer.Compose(pack, context);

        // Assert
        result.Should().Be("System prompt.");
    }

    [Fact]
    public void Compose_WithVariables_CallsTemplateEngineWithVariables()
    {
        // Arrange
        var templateEngine = Substitute.For<ITemplateEngine>();
        var composer = new PromptComposer(templateEngine);

        var pack = CreatePackWithComponents(
            ("system.md", "Hello {{name}}!"));

        var variables = new Dictionary<string, string>
        {
            ["name"] = "World",
        };
        var context = CompositionContext.WithVariables(variables);

        templateEngine.Substitute("Hello {{name}}!", Arg.Any<Dictionary<string, string>>())
            .Returns("Hello World!");

        // Act
        var result = composer.Compose(pack, context);

        // Assert
        result.Should().Be("Hello World!");
        templateEngine.Received(1).Substitute("Hello {{name}}!", Arg.Is<Dictionary<string, string>>(
            d => d.GetValueOrDefault("name") == "World"));
    }

    [Fact]
    public void Compose_ExceedsMaxLength_TruncatesWithWarning()
    {
        // Arrange
        var templateEngine = Substitute.For<ITemplateEngine>();
        templateEngine.Substitute(Arg.Any<string>(), Arg.Any<Dictionary<string, string>>())
            .Returns(call => call.Arg<string>());

        var composer = new PromptComposer(templateEngine);

        var longContent = new string('a', 35000); // Exceeds 32,000 char limit
        var pack = CreatePackWithComponents(
            ("system.md", longContent));

        var context = new CompositionContext();

        // Act
        var result = composer.Compose(pack, context);

        // Assert
        result.Length.Should().BeLessOrEqualTo(32000);
    }

    [Fact]
    public void Compose_ComponentsSeparatedByDoubleNewline()
    {
        // Arrange
        var templateEngine = Substitute.For<ITemplateEngine>();
        templateEngine.Substitute(Arg.Any<string>(), Arg.Any<Dictionary<string, string>>())
            .Returns(call => call.Arg<string>());

        var composer = new PromptComposer(templateEngine);

        var pack = CreatePackWithComponents(
            ("system.md", "First."),
            ("roles/planner.md", "Second."));

        var context = CompositionContext.ForRole("planner");

        // Act
        var result = composer.Compose(pack, context);

        // Assert
        result.Should().Be("First.\n\nSecond.");
    }

    [Fact]
    public void Compose_EmptySystemPrompt_ReturnsEmptyAfterSubstitution()
    {
        // Arrange
        var templateEngine = Substitute.For<ITemplateEngine>();
        templateEngine.Substitute(Arg.Any<string>(), Arg.Any<Dictionary<string, string>>())
            .Returns(string.Empty);

        var composer = new PromptComposer(templateEngine);

        var pack = CreatePackWithComponents(
            ("system.md", string.Empty));

        var context = new CompositionContext();

        // Act
        var result = composer.Compose(pack, context);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void Compose_NullContextVariables_UsesEmptyDictionary()
    {
        // Arrange
        var templateEngine = Substitute.For<ITemplateEngine>();
        templateEngine.Substitute(Arg.Any<string>(), Arg.Any<Dictionary<string, string>>())
            .Returns(call => call.Arg<string>());

        var composer = new PromptComposer(templateEngine);

        var pack = CreatePackWithComponents(
            ("system.md", "Test."));

        var context = new CompositionContext();

        // Act
        var result = composer.Compose(pack, context);

        // Assert
        result.Should().Be("Test.");
        templateEngine.Received().Substitute("Test.", Arg.Any<Dictionary<string, string>>());
    }

    private static PromptPack CreatePackWithComponents(params (string Path, string Content)[] components)
    {
        var packComponents = new Dictionary<string, PackComponent>();
        var componentsList = new List<PackComponent>();

        foreach (var (path, content) in components)
        {
            var type = DetermineComponentType(path);
            var component = new PackComponent
            {
                Path = path,
                Type = type,
                Content = content,
            };
            packComponents[path] = component;
            componentsList.Add(component);
        }

        var manifest = new PackManifest
        {
            FormatVersion = "1.0",
            Id = "test-pack",
            Version = PackVersion.Parse("1.0.0"),
            Name = "Test Pack",
            Description = "Test pack for composer tests",
            ContentHash = new ContentHash("0000000000000000000000000000000000000000000000000000000000000000"),
            CreatedAt = DateTime.UtcNow,
            Components = componentsList,
        };

        return new PromptPack
        {
            Manifest = manifest,
            Components = packComponents,
            Source = PackSource.User,
        };
    }

    private static ComponentType DetermineComponentType(string path)
    {
        if (path.Equals("system.md", StringComparison.OrdinalIgnoreCase))
        {
            return ComponentType.System;
        }

        if (path.StartsWith("roles/", StringComparison.OrdinalIgnoreCase))
        {
            return ComponentType.Role;
        }

        if (path.StartsWith("languages/", StringComparison.OrdinalIgnoreCase))
        {
            return ComponentType.Language;
        }

        if (path.StartsWith("frameworks/", StringComparison.OrdinalIgnoreCase))
        {
            return ComponentType.Framework;
        }

        return ComponentType.Custom;
    }
}
