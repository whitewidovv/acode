using Acode.Application.PromptPacks;
using Acode.Domain.PromptPacks;
using FluentAssertions;

namespace Acode.Application.Tests.PromptPacks;

/// <summary>
/// Tests for <see cref="ManifestSchemaValidator"/>.
/// </summary>
public class ManifestSchemaValidatorTests
{
    [Fact]
    public void Validate_ValidManifest_ShouldNotThrow()
    {
        // Arrange
        var validator = new ManifestSchemaValidator();
        var manifest = CreateValidManifest();

        // Act
        var act = () => validator.Validate(manifest);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void Validate_IdWithUppercase_ShouldThrow()
    {
        // Arrange
        var validator = new ManifestSchemaValidator();
        var manifest = CreateValidManifest() with { Id = "Acode-Standard" };

        // Act
        var act = () => validator.Validate(manifest);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*lowercase*");
    }

    [Fact]
    public void Validate_IdWithSpaces_ShouldThrow()
    {
        // Arrange
        var validator = new ManifestSchemaValidator();
        var manifest = CreateValidManifest() with { Id = "acode standard" };

        // Act
        var act = () => validator.Validate(manifest);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*lowercase*hyphens*");
    }

    [Fact]
    public void Validate_IdWithUnderscores_ShouldThrow()
    {
        // Arrange
        var validator = new ManifestSchemaValidator();
        var manifest = CreateValidManifest() with { Id = "acode_standard" };

        // Act
        var act = () => validator.Validate(manifest);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*lowercase*hyphens*");
    }

    [Fact]
    public void Validate_ComponentPathWithTraversal_ShouldThrow()
    {
        // Arrange
        var validator = new ManifestSchemaValidator();
        var components = new List<PackComponent>
        {
            new() { Path = "../../../etc/passwd", Type = ComponentType.System },
        };
        var manifest = CreateValidManifest() with { Components = components };

        // Act
        var act = () => validator.Validate(manifest);

        // Assert
        act.Should().Throw<PathTraversalException>()
            .WithMessage("*path traversal*");
    }

    [Fact]
    public void Validate_ComponentPathAbsolute_ShouldThrow()
    {
        // Arrange
        var validator = new ManifestSchemaValidator();
        var components = new List<PackComponent>
        {
            new() { Path = "/etc/passwd", Type = ComponentType.System },
        };
        var manifest = CreateValidManifest() with { Components = components };

        // Act
        var act = () => validator.Validate(manifest);

        // Assert
        act.Should().Throw<PathTraversalException>()
            .WithMessage("*absolute path*");
    }

    [Fact]
    public void Validate_EmptyId_ShouldThrow()
    {
        // Arrange
        var validator = new ManifestSchemaValidator();
        var manifest = CreateValidManifest() with { Id = string.Empty };

        // Act
        var act = () => validator.Validate(manifest);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*required*");
    }

    [Fact]
    public void Validate_EmptyName_ShouldThrow()
    {
        // Arrange
        var validator = new ManifestSchemaValidator();
        var manifest = CreateValidManifest() with { Name = string.Empty };

        // Act
        var act = () => validator.Validate(manifest);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*required*");
    }

    [Fact]
    public void Validate_EmptyDescription_ShouldThrow()
    {
        // Arrange
        var validator = new ManifestSchemaValidator();
        var manifest = CreateValidManifest() with { Description = string.Empty };

        // Act
        var act = () => validator.Validate(manifest);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*required*");
    }

    [Fact]
    public void Validate_InvalidFormatVersion_ShouldThrow()
    {
        // Arrange
        var validator = new ManifestSchemaValidator();
        var manifest = CreateValidManifest() with { FormatVersion = "2.0" };

        // Act
        var act = () => validator.Validate(manifest);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*format_version*1.0*");
    }

    [Fact]
    public void Validate_EmptyComponents_ShouldNotThrow()
    {
        // Arrange
        var validator = new ManifestSchemaValidator();
        var manifest = CreateValidManifest() with { Components = new List<PackComponent>() };

        // Act
        var act = () => validator.Validate(manifest);

        // Assert
        act.Should().NotThrow("empty components list is allowed");
    }

    private static PackManifest CreateValidManifest()
    {
        return new PackManifest
        {
            FormatVersion = "1.0",
            Id = "acode-standard",
            Version = PackVersion.Parse("1.0.0"),
            Name = "Acode Standard Pack",
            Description = "Standard prompts for Acode",
            ContentHash = new ContentHash("a".PadRight(64, '1')),
            CreatedAt = new DateTime(2024, 1, 15, 10, 30, 0, DateTimeKind.Utc),
            Components = new List<PackComponent>
            {
                new() { Path = "roles/coder.md", Type = ComponentType.Role, Role = "coder" },
            },
        };
    }
}
