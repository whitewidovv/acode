using Acode.Domain.PromptPacks;
using Acode.Infrastructure.PromptPacks;
using FluentAssertions;

namespace Acode.Infrastructure.Tests.PromptPacks;

/// <summary>
/// Tests for <see cref="PackValidator"/>.
/// </summary>
public class PackValidatorTests
{
    [Fact]
    public void Validate_ValidPack_ShouldReturnSuccess()
    {
        // Arrange
        var validator = new PackValidator();
        var pack = CreateValidPack();

        // Act
        var result = validator.Validate(pack);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void Validate_PackWithMissingId_ShouldReturnError()
    {
        // Arrange
        var validator = new PackValidator();
        var pack = CreateValidPack();
        pack = pack with
        {
            Manifest = pack.Manifest with { Id = string.Empty },
        };

        // Act
        var result = validator.Validate(pack);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.Code == "PACK_ID_REQUIRED");
    }

    [Fact]
    public void Validate_PackWithInvalidIdFormat_ShouldReturnError()
    {
        // Arrange
        var validator = new PackValidator();
        var pack = CreateValidPack();
        pack = pack with
        {
            Manifest = pack.Manifest with { Id = "Invalid_Pack_ID" },
        };

        // Act
        var result = validator.Validate(pack);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e =>
            e.Code == "PACK_ID_INVALID_FORMAT" &&
            e.Message.Contains("lowercase"));
    }

    [Fact]
    public void Validate_PackWithMissingName_ShouldReturnError()
    {
        // Arrange
        var validator = new PackValidator();
        var pack = CreateValidPack();
        pack = pack with
        {
            Manifest = pack.Manifest with { Name = string.Empty },
        };

        // Act
        var result = validator.Validate(pack);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.Code == "PACK_NAME_REQUIRED");
    }

    [Fact]
    public void Validate_PackWithMissingDescription_ShouldReturnError()
    {
        // Arrange
        var validator = new PackValidator();
        var pack = CreateValidPack();
        pack = pack with
        {
            Manifest = pack.Manifest with { Description = string.Empty },
        };

        // Act
        var result = validator.Validate(pack);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.Code == "PACK_DESCRIPTION_REQUIRED");
    }

    [Fact]
    public void Validate_ComponentWithAbsolutePath_ShouldReturnError()
    {
        // Arrange
        var validator = new PackValidator();
        var pack = CreateValidPack();
        var component = new PackComponent
        {
            Path = "/absolute/path/to/component.md",
            Type = ComponentType.Custom,
            Content = "Test content",
        };
        pack = pack with
        {
            Manifest = pack.Manifest with { Components = [component] },
            Components = new Dictionary<string, PackComponent>
            {
                [component.Path] = component,
            },
        };

        // Act
        var result = validator.Validate(pack);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e =>
            e.Code == "COMPONENT_PATH_ABSOLUTE" &&
            e.Path == "/absolute/path/to/component.md");
    }

    [Fact]
    public void Validate_ComponentWithPathTraversal_ShouldReturnError()
    {
        // Arrange
        var validator = new PackValidator();
        var pack = CreateValidPack();
        var component = new PackComponent
        {
            Path = "../../../etc/passwd",
            Type = ComponentType.System,
            Content = "Malicious content",
        };
        pack = pack with
        {
            Manifest = pack.Manifest with { Components = [component] },
            Components = new Dictionary<string, PackComponent>
            {
                [component.Path] = component,
            },
        };

        // Act
        var result = validator.Validate(pack);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e =>
            e.Code == "COMPONENT_PATH_TRAVERSAL" &&
            e.Path == "../../../etc/passwd");
    }

    [Fact]
    public void Validate_InvalidTemplateVariableSyntax_ShouldReturnError()
    {
        // Arrange
        var validator = new PackValidator();
        var pack = CreateValidPack();
        var component = new PackComponent
        {
            Path = "roles/coder.md",
            Type = ComponentType.Role,
            Content = "Use {{invalid variable}} and {single_brace} syntax",
        };
        pack = pack with
        {
            Manifest = pack.Manifest with { Components = [component] },
            Components = new Dictionary<string, PackComponent>
            {
                [component.Path] = component,
            },
        };

        // Act
        var result = validator.Validate(pack);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.Code == "INVALID_TEMPLATE_VARIABLE" &&
            e.Path == "roles/coder.md");
    }

    [Fact]
    public void Validate_ValidTemplateVariableSyntax_ShouldPass()
    {
        // Arrange
        var validator = new PackValidator();
        var pack = CreateValidPack();
        var component = new PackComponent
        {
            Path = "roles/coder.md",
            Type = ComponentType.Role,
            Content = "Use {{variable_name}} and {{another_var}} syntax",
        };
        pack = pack with
        {
            Manifest = pack.Manifest with { Components = [component] },
            Components = new Dictionary<string, PackComponent>
            {
                [component.Path] = component,
            },
        };

        // Act
        var result = validator.Validate(pack);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_PackExceeds5MBLimit_ShouldReturnError()
    {
        // Arrange
        var validator = new PackValidator();
        var pack = CreateValidPack();

        // Create component with > 5MB content (5 * 1024 * 1024 + 1 bytes)
        var largeContent = new string('x', (5 * 1024 * 1024) + 1);
        var component = new PackComponent
        {
            Path = "large-file.md",
            Type = ComponentType.Custom,
            Content = largeContent,
        };
        pack = pack with
        {
            Manifest = pack.Manifest with { Components = [component] },
            Components = new Dictionary<string, PackComponent>
            {
                [component.Path] = component,
            },
        };

        // Act
        var result = validator.Validate(pack);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e =>
            e.Code == "PACK_SIZE_EXCEEDS_LIMIT" &&
            e.Message.Contains("5 MB"));
    }

    [Fact]
    public void Validate_PackUnder5MBLimit_ShouldPass()
    {
        // Arrange
        var validator = new PackValidator();
        var pack = CreateValidPack();

        // Create component with < 5MB content
        var content = new string('x', 1024 * 1024); // 1 MB
        var component = new PackComponent
        {
            Path = "medium-file.md",
            Type = ComponentType.Custom,
            Content = content,
        };
        pack = pack with
        {
            Manifest = pack.Manifest with { Components = [component] },
            Components = new Dictionary<string, PackComponent>
            {
                [component.Path] = component,
            },
        };

        // Act
        var result = validator.Validate(pack);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_MultipleErrors_ShouldReturnAllErrors()
    {
        // Arrange
        var validator = new PackValidator();
        var pack = CreateValidPack();

        // Create pack with multiple validation errors
        var component = new PackComponent
        {
            Path = "/absolute/path.md",
            Type = ComponentType.Custom,
            Content = "Invalid {{variable name}} syntax",
        };
        pack = pack with
        {
            Manifest = pack.Manifest with
            {
                Id = string.Empty, // Missing ID
                Name = string.Empty, // Missing name
                Components = [component],
            },
            Components = new Dictionary<string, PackComponent>
            {
                [component.Path] = component,
            },
        };

        // Act
        var result = validator.Validate(pack);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().HaveCountGreaterThan(2);
        result.Errors.Should().Contain(e => e.Code == "PACK_ID_REQUIRED");
        result.Errors.Should().Contain(e => e.Code == "PACK_NAME_REQUIRED");
        result.Errors.Should().Contain(e => e.Code == "COMPONENT_PATH_ABSOLUTE");
    }

    [Fact]
    public void Validate_Performance_ShouldCompleteUnder100ms()
    {
        // Arrange
        var validator = new PackValidator();
        var pack = CreateValidPack();

        // Add multiple components to make validation more realistic
        var components = new Dictionary<string, PackComponent>();
        for (int i = 0; i < 50; i++)
        {
            var component = new PackComponent
            {
                Path = $"components/component-{i}.md",
                Type = ComponentType.Custom,
                Content = $"Content for component {i} with {{variable_{i}}}",
            };
            components[component.Path] = component;
        }

        pack = pack with
        {
            Manifest = pack.Manifest with { Components = components.Values.ToList() },
            Components = components,
        };

        // Act
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var result = validator.Validate(pack);
        stopwatch.Stop();

        // Assert
        result.Should().NotBeNull();
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(100, "validation must complete within 100ms");
    }

    private static PromptPack CreateValidPack()
    {
        var manifest = new PackManifest
        {
            FormatVersion = "1.0",
            Id = "test-pack",
            Version = PackVersion.Parse("1.0.0"),
            Name = "Test Pack",
            Description = "A test pack for validation",
            ContentHash = new ContentHash("a".PadRight(64, '1')),
            CreatedAt = DateTime.UtcNow,
            Components =
            [
                new PackComponent
                {
                    Path = "roles/coder.md",
                    Type = ComponentType.Role,
                    Role = "coder",
                    Content = "You are a coding assistant with {{skill_level}} expertise.",
                },
            ],
        };

        var components = new Dictionary<string, PackComponent>
        {
            ["roles/coder.md"] = manifest.Components[0],
        };

        return new PromptPack
        {
            Manifest = manifest,
            Components = components,
            Source = PackSource.User,
        };
    }
}
