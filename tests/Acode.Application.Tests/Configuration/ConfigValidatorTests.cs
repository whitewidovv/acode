using Acode.Application.Configuration;
using Acode.Domain.Configuration;
using FluentAssertions;
using NSubstitute;

namespace Acode.Application.Tests.Configuration;

/// <summary>
/// Tests for ConfigValidator.
/// Verifies integration of schema and semantic validation.
/// </summary>
public sealed class ConfigValidatorTests
{
    [Fact]
    public void Validate_WithNullConfig_ShouldThrowArgumentNullException()
    {
        // Arrange
        var validator = new ConfigValidator();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => validator.Validate(null!));
    }

    [Fact]
    public void Validate_WithMissingSchemaVersion_ShouldReturnError()
    {
        // Arrange
        var validator = new ConfigValidator();
        var config = new AcodeConfig
        {
            SchemaVersion = string.Empty // Empty schema version (effectively missing)
        };

        // Act
        var result = validator.Validate(config);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.Code == ConfigErrorCodes.RequiredFieldMissing);
        result.Errors.Should().ContainSingle(e => e.Path == "schema_version");
    }

    [Fact]
    public void Validate_WithValidConfig_ShouldCallSemanticValidator()
    {
        // Arrange
        var validator = new ConfigValidator();
        var config = new AcodeConfig
        {
            SchemaVersion = "1.0.0"
        };

        // Act
        var result = validator.Validate(config);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WithSemanticErrors_ShouldReturnAggregatedErrors()
    {
        // Arrange
        var validator = new ConfigValidator();
        var config = new AcodeConfig
        {
            SchemaVersion = "1.0.0",
            Mode = new ModeConfig
            {
                Default = "burst" // FR-002b-51: Default cannot be burst
            }
        };

        // Act
        var result = validator.Validate(config);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Code == "INVALID_DEFAULT_MODE");
    }

    [Fact]
    public void Validate_WithMultipleErrors_ShouldReturnAllErrors()
    {
        // Arrange
        var validator = new ConfigValidator();
        var config = new AcodeConfig
        {
            SchemaVersion = string.Empty, // Missing schema version
            Mode = new ModeConfig
            {
                Default = "burst", // Invalid default mode
                AirgappedLock = true
            }
        };

        // Act
        var result = validator.Validate(config);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().HaveCountGreaterThan(1);
        result.Errors.Should().Contain(e => e.Code == ConfigErrorCodes.RequiredFieldMissing);
        result.Errors.Should().Contain(e => e.Code == "INVALID_DEFAULT_MODE");
    }

    [Fact]
    public async Task ValidateFileAsync_WithNonExistentFile_ShouldReturnFileNotFoundError()
    {
        // Arrange
        var validator = new ConfigValidator();
        var nonExistentPath = Path.Combine(Path.GetTempPath(), $"nonexistent-{Guid.NewGuid()}.yml");

        // Act
        var result = await validator.ValidateFileAsync(nonExistentPath).ConfigureAwait(true);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.Code == ConfigErrorCodes.FileNotFound);
    }

    [Fact]
    public async Task ValidateFileAsync_WithSchemaValidator_ShouldCallIt()
    {
        // Arrange
        var schemaValidator = Substitute.For<ISchemaValidator>();
        var validator = new ConfigValidator(schemaValidator);
        var tempFile = Path.GetTempFileName();

        try
        {
            // Create a minimal valid YAML file
            await File.WriteAllTextAsync(tempFile, "schema_version: \"1.0.0\"").ConfigureAwait(true);

            schemaValidator.ValidateAsync(tempFile, Arg.Any<CancellationToken>())
                .Returns(ValidationResult.Success());

            // Act
            var result = await validator.ValidateFileAsync(tempFile).ConfigureAwait(true);

            // Assert
            result.IsValid.Should().BeTrue();
            await schemaValidator.Received(1).ValidateAsync(tempFile, Arg.Any<CancellationToken>()).ConfigureAwait(true);
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }

    [Fact]
    public async Task ValidateFileAsync_WithSchemaErrors_ShouldReturnSchemaErrors()
    {
        // Arrange
        var schemaValidator = Substitute.For<ISchemaValidator>();
        var validator = new ConfigValidator(schemaValidator);
        var tempFile = Path.GetTempFileName();

        try
        {
            await File.WriteAllTextAsync(tempFile, "invalid: yaml").ConfigureAwait(true);

            var schemaError = new ValidationError
            {
                Code = ConfigErrorCodes.TypeMismatch,
                Message = "Schema validation failed",
                Severity = ValidationSeverity.Error
            };

            schemaValidator.ValidateAsync(tempFile, Arg.Any<CancellationToken>())
                .Returns(ValidationResult.Failure(schemaError));

            // Act
            var result = await validator.ValidateFileAsync(tempFile).ConfigureAwait(true);

            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().ContainSingle(e => e.Code == ConfigErrorCodes.TypeMismatch);
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }

    [Fact]
    public async Task ValidateFileAsync_WithTooLargeFile_ShouldReturnFileTooLargeError()
    {
        // Arrange
        var validator = new ConfigValidator();
        var tempFile = Path.GetTempFileName();

        try
        {
            // Create a file larger than 1MB
            var largeContent = new string('x', 1_048_577);
            await File.WriteAllTextAsync(tempFile, largeContent).ConfigureAwait(true);

            // Act
            var result = await validator.ValidateFileAsync(tempFile).ConfigureAwait(true);

            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().ContainSingle(e => e.Code == ConfigErrorCodes.FileTooLarge);
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }

    [Fact]
    public void Validate_WithWarnings_ShouldReturnSuccessWithWarnings()
    {
        // Arrange
        var validator = new ConfigValidator();
        var config = new AcodeConfig
        {
            SchemaVersion = "1.0.0",
            Project = new ProjectConfig
            {
                Languages = new[] { "csharp", "csharp" } // Duplicate - should be warning
            }
        };

        // Act
        var result = validator.Validate(config);

        // Assert - warnings don't make IsValid false
        result.WarningsOnly.Should().NotBeEmpty();
        result.WarningsOnly.Should().ContainSingle(e => e.Code == "DUPLICATE_LANGUAGES");
    }

    [Fact]
    public async Task Validate_ConcurrentValidation_ShouldBeThreadSafe()
    {
        // Arrange - Test thread safety with concurrent validation calls
        var validator = new ConfigValidator();
        var config = new AcodeConfig
        {
            SchemaVersion = "1.0.0",
            Mode = new ModeConfig
            {
                Default = "local-only"
            }
        };

        // Act - Run 10 concurrent validations
        var tasks = Enumerable.Range(0, 10)
            .Select(_ => Task.Run(() => validator.Validate(config)))
            .ToArray();

        var results = await Task.WhenAll(tasks).ConfigureAwait(true);

        // Assert - All validations should succeed
        foreach (var result in results)
        {
            result.IsValid.Should().BeTrue();
        }
    }

    [Fact]
    public async Task ValidateFileAsync_WithValidConfigAndNoSchemaValidator_ShouldReturnSuccess()
    {
        // Arrange - When no schema validator, should still succeed with valid file
        var validator = new ConfigValidator();
        var tempFile = Path.GetTempFileName();

        try
        {
            await File.WriteAllTextAsync(tempFile, "schema_version: \"1.0.0\"").ConfigureAwait(true);

            // Act
            var result = await validator.ValidateFileAsync(tempFile).ConfigureAwait(true);

            // Assert
            result.IsValid.Should().BeTrue();
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }

    [Fact]
    public async Task ValidateFileAsync_WithComplexValidConfig_ShouldReturnSuccess()
    {
        // Arrange
        var validator = new ConfigValidator();
        var tempFile = Path.GetTempFileName();

        try
        {
            var configContent = @"
schema_version: ""1.0.0""
project:
  name: test-project
  type: dotnet
mode:
  default: local-only
  allow_burst: false
";
            await File.WriteAllTextAsync(tempFile, configContent).ConfigureAwait(true);

            // Act
            var result = await validator.ValidateFileAsync(tempFile).ConfigureAwait(true);

            // Assert
            result.IsValid.Should().BeTrue();
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }

    [Fact]
    public void Validate_WithAllValidationLayersSucceeding_ShouldReturnSuccess()
    {
        // Arrange - Config that passes both schema and semantic validation
        var validator = new ConfigValidator();
        var config = new AcodeConfig
        {
            SchemaVersion = "1.0.0",
            Project = new ProjectConfig
            {
                Name = "test-project",
                Type = "dotnet",
                Languages = new[] { "csharp" }
            },
            Mode = new ModeConfig
            {
                Default = "local-only",
                AllowBurst = false
            },
            Model = new ModelConfig
            {
                Provider = "ollama",
                Name = "codellama:7b"
            }
        };

        // Act
        var result = validator.Validate(config);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
        result.WarningsOnly.Should().BeEmpty();
    }

    [Fact]
    public async Task ValidateFileAsync_WithSchemaErrors_ShouldReturnEarlyWithoutSemanticValidation()
    {
        // Arrange - When schema validation fails, validation stops early
        var schemaValidator = Substitute.For<ISchemaValidator>();
        var validator = new ConfigValidator(schemaValidator);
        var tempFile = Path.GetTempFileName();

        try
        {
            // Create a file (content doesn't matter - schema validator will fail it)
            await File.WriteAllTextAsync(tempFile, "invalid: config").ConfigureAwait(true);

            // Mock schema error
            var schemaError = new ValidationError
            {
                Code = ConfigErrorCodes.TypeMismatch,
                Message = "Schema validation failed",
                Severity = ValidationSeverity.Error
            };

            schemaValidator.ValidateAsync(tempFile, Arg.Any<CancellationToken>())
                .Returns(ValidationResult.Failure(schemaError));

            // Act
            var result = await validator.ValidateFileAsync(tempFile).ConfigureAwait(true);

            // Assert - Should only have schema error (validation stops early)
            result.IsValid.Should().BeFalse();
            result.Errors.Should().ContainSingle(e => e.Code == ConfigErrorCodes.TypeMismatch);
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }
}
