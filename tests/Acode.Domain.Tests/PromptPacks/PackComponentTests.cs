using Acode.Domain.PromptPacks;
using FluentAssertions;

namespace Acode.Domain.Tests.PromptPacks;

/// <summary>
/// Tests for <see cref="PackComponent"/> record.
/// </summary>
public class PackComponentTests
{
    [Fact]
    public void Constructor_WithPathAndType_ShouldSucceed()
    {
        // Arrange
        var path = "system.md";
        var type = ComponentType.System;

        // Act
        var component = new PackComponent
        {
            Path = path,
            Type = type
        };

        // Assert
        component.Path.Should().Be(path);
        component.Type.Should().Be(type);
        component.Role.Should().BeNull();
        component.Language.Should().BeNull();
        component.Framework.Should().BeNull();
        component.Content.Should().BeNull();
    }

    [Fact]
    public void Constructor_WithRoleMetadata_ShouldIncludeRole()
    {
        // Arrange
        var path = "roles/coder.md";
        var type = ComponentType.Role;
        var role = "coder";

        // Act
        var component = new PackComponent
        {
            Path = path,
            Type = type,
            Role = role
        };

        // Assert
        component.Path.Should().Be(path);
        component.Type.Should().Be(type);
        component.Role.Should().Be(role);
    }

    [Fact]
    public void Constructor_WithLanguageMetadata_ShouldIncludeLanguage()
    {
        // Arrange
        var path = "languages/csharp.md";
        var type = ComponentType.Language;
        var language = "csharp";

        // Act
        var component = new PackComponent
        {
            Path = path,
            Type = type,
            Language = language
        };

        // Assert
        component.Path.Should().Be(path);
        component.Type.Should().Be(type);
        component.Language.Should().Be(language);
    }

    [Fact]
    public void Constructor_WithFrameworkMetadata_ShouldIncludeFramework()
    {
        // Arrange
        var path = "frameworks/aspnetcore.md";
        var type = ComponentType.Framework;
        var framework = "aspnetcore";

        // Act
        var component = new PackComponent
        {
            Path = path,
            Type = type,
            Framework = framework
        };

        // Assert
        component.Path.Should().Be(path);
        component.Type.Should().Be(type);
        component.Framework.Should().Be(framework);
    }

    [Fact]
    public void Constructor_WithContent_ShouldIncludeContent()
    {
        // Arrange
        var path = "system.md";
        var type = ComponentType.System;
        var content = "You are a coding assistant.";

        // Act
        var component = new PackComponent
        {
            Path = path,
            Type = type,
            Content = content
        };

        // Assert
        component.Content.Should().Be(content);
    }

    [Fact]
    public void Immutability_ShouldBeEnforced()
    {
        // Arrange
        var component = new PackComponent
        {
            Path = "system.md",
            Type = ComponentType.System
        };

        // Act
        var component2 = component with { Path = "modified.md" };

        // Assert
        component.Path.Should().Be("system.md");
        component2.Path.Should().Be("modified.md");
    }

    [Fact]
    public void Equality_SamePathAndType_ShouldBeEqual()
    {
        // Arrange
        var component1 = new PackComponent
        {
            Path = "system.md",
            Type = ComponentType.System,
            Content = "Content 1"
        };

        var component2 = new PackComponent
        {
            Path = "system.md",
            Type = ComponentType.System,
            Content = "Content 1"
        };

        // Act & Assert
        component1.Should().Be(component2);
    }

    [Fact]
    public void Equality_DifferentPath_ShouldNotBeEqual()
    {
        // Arrange
        var component1 = new PackComponent
        {
            Path = "system.md",
            Type = ComponentType.System
        };

        var component2 = new PackComponent
        {
            Path = "other.md",
            Type = ComponentType.System
        };

        // Act & Assert
        component1.Should().NotBe(component2);
    }
}
