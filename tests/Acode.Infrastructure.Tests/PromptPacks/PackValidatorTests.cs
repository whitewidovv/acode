using Acode.Application.PromptPacks;
using Acode.Domain.PromptPacks;
using Acode.Infrastructure.PromptPacks;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;

namespace Acode.Infrastructure.Tests.PromptPacks;

/// <summary>
/// Tests for PackValidator.
/// </summary>
public class PackValidatorTests : IDisposable
{
    private readonly string _tempDir;
    private readonly PackValidator _validator;
    private readonly ManifestParser _parser;

    /// <summary>
    /// Initializes a new instance of the <see cref="PackValidatorTests"/> class.
    /// </summary>
    public PackValidatorTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"acode-validator-test-{Guid.NewGuid()}");
        Directory.CreateDirectory(_tempDir);

        _parser = new ManifestParser();
        _validator = new PackValidator(_parser, NullLogger<PackValidator>.Instance);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, true);
        }

        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Test that a valid pack passes validation.
    /// </summary>
    [Fact]
    public void Should_Validate_Valid_Pack()
    {
        // Arrange
        var pack = CreateValidPack();

        // Act
        var result = _validator.Validate(pack);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    /// <summary>
    /// Test that empty pack ID fails validation.
    /// </summary>
    [Fact]
    public void Should_Fail_On_Invalid_PackId()
    {
        // Arrange
        var pack = CreateValidPackWithId("AB"); // Too short, must be 3+ chars

        // Act
        var result = _validator.Validate(pack);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Code == "ACODE-VAL-002");
    }

    /// <summary>
    /// Test that pack with no components fails validation.
    /// </summary>
    [Fact]
    public void Should_Fail_On_Empty_Components()
    {
        // Arrange
        var pack = new PromptPack(
            "empty-pack",
            PackVersion.Parse("1.0.0"),
            "Empty Pack",
            "No components",
            PackSource.User,
            _tempDir,
            null,
            Array.Empty<LoadedComponent>());

        // Act
        var result = _validator.Validate(pack);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Code == "ACODE-VAL-001");
    }

    /// <summary>
    /// Test that oversized pack fails validation.
    /// </summary>
    [Fact]
    public void Should_Fail_On_Size_Exceeds_Limit()
    {
        // Arrange - Create pack with content over 5MB
        var largeContent = new string('x', 6 * 1024 * 1024); // 6MB

        var components = new[]
        {
            new LoadedComponent("large.md", ComponentType.System, largeContent, null),
        };

        var pack = new PromptPack(
            "large-pack",
            PackVersion.Parse("1.0.0"),
            "Large Pack",
            "Oversized",
            PackSource.User,
            _tempDir,
            null,
            components);

        // Act
        var result = _validator.Validate(pack);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Code == "ACODE-VAL-006");
    }

    /// <summary>
    /// Test that component with empty path fails validation.
    /// </summary>
    [Fact]
    public void Should_Fail_On_Empty_Component_Path()
    {
        // Arrange
        var components = new[]
        {
            new LoadedComponent(string.Empty, ComponentType.System, "content", null),
        };

        var pack = new PromptPack(
            "test-pack",
            PackVersion.Parse("1.0.0"),
            "Test Pack",
            "Has empty path",
            PackSource.User,
            _tempDir,
            null,
            components);

        // Act
        var result = _validator.Validate(pack);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Code == "ACODE-VAL-001");
    }

    /// <summary>
    /// Test that component size limit is enforced.
    /// </summary>
    [Fact]
    public void Should_Fail_On_Component_Too_Large()
    {
        // Arrange - Single component over 1MB limit
        var largeContent = new string('x', 2 * 1024 * 1024); // 2MB

        var components = new[]
        {
            new LoadedComponent("large.md", ComponentType.System, largeContent, null),
        };

        var pack = new PromptPack(
            "component-large",
            PackVersion.Parse("1.0.0"),
            "Large Component",
            "Has component over limit",
            PackSource.User,
            _tempDir,
            null,
            components);

        // Act
        var result = _validator.Validate(pack);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Code == "ACODE-VAL-006");
    }

    /// <summary>
    /// Test that ValidatePath returns errors for missing manifest.
    /// </summary>
    [Fact]
    public void ValidatePath_Should_Fail_On_Missing_Manifest()
    {
        // Arrange
        var packPath = Path.Combine(_tempDir, "no-manifest");
        Directory.CreateDirectory(packPath);

        // Act
        var result = _validator.ValidatePath(packPath);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Code == "ACODE-VAL-001");
    }

    /// <summary>
    /// Test that ValidatePath returns errors for missing components.
    /// </summary>
    [Fact]
    public void ValidatePath_Should_Fail_On_Missing_Component()
    {
        // Arrange
        var packPath = Path.Combine(_tempDir, "missing-comp");
        Directory.CreateDirectory(packPath);

        var manifest = @"
format_version: '1.0'
id: test-pack
version: 1.0.0
name: Test Pack
description: A test prompt pack for missing component validation
created_at: 2025-01-01T00:00:00Z
components:
  - path: missing.md
    type: system
";
        File.WriteAllText(Path.Combine(packPath, "manifest.yml"), manifest);

        // Act
        var result = _validator.ValidatePath(packPath);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Code == "ACODE-VAL-004");
    }

    /// <summary>
    /// Test that ValidatePath validates pack ID format during parsing.
    /// </summary>
    [Fact]
    public void ValidatePath_Should_Fail_On_Invalid_PackId()
    {
        // Arrange
        var packPath = Path.Combine(_tempDir, "bad-id");
        Directory.CreateDirectory(packPath);

        // Pack ID "AB" is too short (min 3 chars) - this causes ManifestParser to throw
        var manifest = @"
format_version: '1.0'
id: AB
version: 1.0.0
name: Test Pack
description: A test prompt pack for invalid ID validation
created_at: 2025-01-01T00:00:00Z
components:
  - path: system.md
    type: system
";
        File.WriteAllText(Path.Combine(packPath, "manifest.yml"), manifest);
        File.WriteAllText(Path.Combine(packPath, "system.md"), "content");

        // Act
        var result = _validator.ValidatePath(packPath);

        // Assert - Parse fails because ID is invalid
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Code == "ACODE-VAL-001" && e.Message.Contains("AB"));
    }

    /// <summary>
    /// Test that ValidationResult.Success creates valid result.
    /// </summary>
    [Fact]
    public void ValidationResult_Success_Should_Be_Valid()
    {
        // Act
        var result = ValidationResult.Success();

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    /// <summary>
    /// Test that ValidationResult.Failure creates invalid result.
    /// </summary>
    [Fact]
    public void ValidationResult_Failure_Should_Be_Invalid()
    {
        // Act
        var result = ValidationResult.Failure("CODE-001", "Error message");

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().HaveCount(1);
        result.Errors[0].Code.Should().Be("CODE-001");
        result.Errors[0].Message.Should().Be("Error message");
    }

    /// <summary>
    /// Test that ValidatePath fails on invalid version format.
    /// </summary>
    [Fact]
    public void ValidatePath_Should_Fail_On_Invalid_Version()
    {
        // Arrange
        var packPath = Path.Combine(_tempDir, "bad-version");
        Directory.CreateDirectory(packPath);

        var manifest = @"
format_version: '1.0'
id: test-pack
version: not-semver
name: Test Pack
description: A test prompt pack for invalid version validation
created_at: 2025-01-01T00:00:00Z
components:
  - path: system.md
    type: system
";
        File.WriteAllText(Path.Combine(packPath, "manifest.yml"), manifest);
        File.WriteAllText(Path.Combine(packPath, "system.md"), "content");

        // Act
        var result = _validator.ValidatePath(packPath);

        // Assert - Parse fails because version is invalid
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Code == "ACODE-VAL-003" || e.Message.Contains("version"));
    }

    /// <summary>
    /// Test that strict validation detects undeclared template variables.
    /// </summary>
    [Fact]
    public void Should_Detect_Undeclared_Template_Variables_With_Strict_Validation()
    {
        // Arrange - Component uses {{project_name}} but no variables declared
        var components = new[]
        {
            new LoadedComponent("system.md", ComponentType.System, "Welcome to {{project_name}}! Use {{language}} for coding.", null),
        };

        var pack = new PromptPack(
            "template-pack",
            PackVersion.Parse("1.0.0"),
            "Template Pack",
            "Has template variables",
            PackSource.User,
            _tempDir,
            null,
            components);

        // Act - Use strict validation
        var errors = new List<ValidationError>();
        _validator.ValidateTemplateVariablesStrict(pack, errors);

        // Assert - Strict validation should report ACODE-VAL-005 for undeclared variables
        errors.Should().Contain(e => e.Code == "ACODE-VAL-005");
        errors.Should().HaveCount(2); // project_name and language
    }

    /// <summary>
    /// Test that normal validation allows undeclared template variables (for runtime substitution).
    /// </summary>
    [Fact]
    public void Should_Allow_Undeclared_Template_Variables_For_Runtime_Substitution()
    {
        // Arrange - Component uses {{project_name}} but no variables declared
        var components = new[]
        {
            new LoadedComponent("system.md", ComponentType.System, "Welcome to {{project_name}}!", null),
        };

        var pack = new PromptPack(
            "template-pack",
            PackVersion.Parse("1.0.0"),
            "Template Pack",
            "Has template variables",
            PackSource.User,
            _tempDir,
            null,
            components);

        // Act - Normal validation
        var result = _validator.Validate(pack);

        // Assert - Normal validation should NOT error on undeclared variables
        // They can be provided at composition time
        result.Errors.Should().NotContain(e => e.Code == "ACODE-VAL-005");
    }

    /// <summary>
    /// Test that declared template variables do not cause errors.
    /// </summary>
    [Fact]
    public void Should_Pass_When_Template_Variables_Are_Declared()
    {
        // Arrange - Component uses {{project_name}} which is declared in metadata
        var components = new[]
        {
            new LoadedComponent(
                "system.md",
                ComponentType.System,
                "Welcome to {{project_name}}!",
                new Dictionary<string, string> { ["project_name"] = "MyProject" }),
        };

        var pack = new PromptPack(
            "template-pack",
            PackVersion.Parse("1.0.0"),
            "Template Pack",
            "Has declared template variables",
            PackSource.User,
            _tempDir,
            null,
            components);

        // Act
        var result = _validator.Validate(pack);

        // Assert - Should pass since variable is declared
        result.Errors.Should().NotContain(e => e.Code == "ACODE-VAL-005");
    }

    private PromptPack CreateValidPack()
    {
        return CreateValidPackWithId("test-pack");
    }

    private PromptPack CreateValidPackWithId(string id)
    {
        var components = new[]
        {
            new LoadedComponent("system.md", ComponentType.System, "System prompt content", null),
        };

        return new PromptPack(
            id,
            PackVersion.Parse("1.0.0"),
            "Test Pack",
            "Valid test pack",
            PackSource.User,
            _tempDir,
            null,
            components);
    }
}
