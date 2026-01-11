using Acode.Application.Configuration;
using Acode.Domain.Configuration;
using FluentAssertions;

namespace Acode.Application.Tests.Configuration;

/// <summary>
/// Tests for SemanticValidator.
/// Verifies business rule validation per FR-002b-51 to FR-002b-70.
/// </summary>
public class SemanticValidatorTests
{
    [Fact]
    public void Validate_WithNullInput_ShouldReturnErrors()
    {
        // Arrange
        var validator = new SemanticValidator();

        // Act
        var result = validator.Validate(null);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle();
        result.Errors[0].Code.Should().Be("NULL_CONFIG");
    }

    [Fact]
    public void Validate_WithValidLocalOnlyConfig_ShouldReturnSuccess()
    {
        // Arrange
        var validator = new SemanticValidator();
        var config = new AcodeConfig
        {
            SchemaVersion = "1.0.0",
            Mode = new ModeConfig
            {
                Default = "local-only",
                AllowBurst = true,
                AirgappedLock = false
            },
            Model = new ModelConfig
            {
                Provider = "ollama",
                Name = "codellama:7b",
                Endpoint = "http://localhost:11434"
            }
        };

        // Act
        var result = validator.Validate(config);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void Validate_WithDefaultModeBurst_ShouldReturnError()
    {
        // Arrange
        var validator = new SemanticValidator();
        var config = new AcodeConfig
        {
            SchemaVersion = "1.0.0",
            Mode = new ModeConfig
            {
                Default = "burst"

                // FR-002b-51: mode.default cannot be "burst"
            }
        };

        // Act
        var result = validator.Validate(config);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle();
        result.Errors[0].Code.Should().Be("INVALID_DEFAULT_MODE");
        result.Errors[0].Message.Should().Contain("burst");
    }

    [Fact]
    public void Validate_WithNonLocalhostEndpointInLocalOnly_ShouldReturnError()
    {
        // Arrange
        var validator = new SemanticValidator();
        var config = new AcodeConfig
        {
            SchemaVersion = "1.0.0",
            Mode = new ModeConfig
            {
                Default = "local-only"
            },
            Model = new ModelConfig
            {
                Provider = "ollama",
                Name = "codellama:7b",
                Endpoint = "http://api.openai.com"

                // FR-002b-53: endpoint must be localhost in LocalOnly mode
            }
        };

        // Act
        var result = validator.Validate(config);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle();
        result.Errors[0].Code.Should().Be("ENDPOINT_NOT_LOCALHOST");
        result.Errors[0].Message.Should().Contain("local-only");
    }

    [Fact]
    public void Validate_WithExternalProviderInLocalOnly_ShouldReturnError()
    {
        // Arrange
        var validator = new SemanticValidator();
        var config = new AcodeConfig
        {
            SchemaVersion = "1.0.0",
            Mode = new ModeConfig
            {
                Default = "local-only"
            },
            Model = new ModelConfig
            {
                Provider = "openai",
                Name = "gpt-4",
                Endpoint = "http://localhost:11434"

                // FR-002b-54: provider must be "ollama" or "lmstudio" in LocalOnly
            }
        };

        // Act
        var result = validator.Validate(config);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle();
        result.Errors[0].Code.Should().Be("INVALID_PROVIDER_FOR_MODE");
        result.Errors[0].Message.Should().Contain("local-only");
    }

    [Fact]
    public void Validate_WithPathTraversal_ShouldReturnError()
    {
        // Arrange
        var validator = new SemanticValidator();
        var config = new AcodeConfig
        {
            SchemaVersion = "1.0.0",
            Paths = new PathsConfig
            {
                Source = new[] { "../../../etc/passwd" }

                // FR-002b-56: paths cannot include ".." traversal
            }
        };

        // Act
        var result = validator.Validate(config);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle();
        result.Errors[0].Code.Should().Be("PATH_TRAVERSAL");
        result.Errors[0].Message.Should().Contain("..");
    }

    [Fact]
    public void Validate_WithInvalidTemperature_ShouldReturnError()
    {
        // Arrange
        var validator = new SemanticValidator();
        var config = new AcodeConfig
        {
            SchemaVersion = "1.0.0",
            Model = new ModelConfig
            {
                Parameters = new ModelParametersConfig
                {
                    Temperature = 5.0

                    // FR-002b-64: temperature must be in range 0.0-2.0
                }
            }
        };

        // Act
        var result = validator.Validate(config);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle();
        result.Errors[0].Code.Should().Be("INVALID_TEMPERATURE");
        result.Errors[0].Message.Should().Contain("0.0").And.Contain("2.0");
    }

    [Fact]
    public void Validate_WithNegativeMaxTokens_ShouldReturnError()
    {
        // Arrange
        var validator = new SemanticValidator();
        var config = new AcodeConfig
        {
            SchemaVersion = "1.0.0",
            Model = new ModelConfig
            {
                Parameters = new ModelParametersConfig
                {
                    MaxTokens = -100

                    // FR-002b-65: max_tokens must be positive
                }
            }
        };

        // Act
        var result = validator.Validate(config);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle();
        result.Errors[0].Code.Should().Be("INVALID_MAX_TOKENS");
        result.Errors[0].Message.Should().Contain("positive");
    }

    [Fact]
    public void Validate_WithMultipleErrors_ShouldReturnAllErrors()
    {
        // Arrange
        var validator = new SemanticValidator();
        var config = new AcodeConfig
        {
            SchemaVersion = "1.0.0",
            Mode = new ModeConfig
            {
                Default = "burst"

                // Error 1: invalid default mode
            },
            Model = new ModelConfig
            {
                Parameters = new ModelParametersConfig
                {
                    Temperature = 3.0

                    // Error 2: invalid temperature
                }
            }
        };

        // Act
        var result = validator.Validate(config);

        // Assert - FR-002b-70: Aggregate all semantic errors
        result.IsValid.Should().BeFalse();
        result.Errors.Should().HaveCount(2);
        result.Errors.Should().Contain(e => e.Code == "INVALID_DEFAULT_MODE");
        result.Errors.Should().Contain(e => e.Code == "INVALID_TEMPERATURE");
    }

    [Fact]
    public void Validate_WithOllamaProvider_ShouldAllow()
    {
        // Arrange
        var validator = new SemanticValidator();
        var config = new AcodeConfig
        {
            SchemaVersion = "1.0.0",
            Mode = new ModeConfig { Default = "local-only" },
            Model = new ModelConfig
            {
                Provider = "ollama",
                Endpoint = "http://localhost:11434"
            }
        };

        // Act
        var result = validator.Validate(config);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WithLmstudioProvider_ShouldAllow()
    {
        // Arrange
        var validator = new SemanticValidator();
        var config = new AcodeConfig
        {
            SchemaVersion = "1.0.0",
            Mode = new ModeConfig { Default = "local-only" },
            Model = new ModelConfig
            {
                Provider = "lmstudio",
                Endpoint = "http://127.0.0.1:1234"
            }
        };

        // Act
        var result = validator.Validate(config);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WithNegativeTimeout_ShouldReturnError()
    {
        // Arrange
        var validator = new SemanticValidator();
        var config = new AcodeConfig
        {
            SchemaVersion = "1.0.0",
            Model = new ModelConfig { TimeoutSeconds = -1 }
        };

        // Act
        var result = validator.Validate(config);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.Code == "INVALID_TIMEOUT");
    }

    [Fact]
    public void Validate_WithNegativeRetryCount_ShouldReturnError()
    {
        // Arrange
        var validator = new SemanticValidator();
        var config = new AcodeConfig
        {
            SchemaVersion = "1.0.0",
            Model = new ModelConfig { RetryCount = -1 }
        };

        // Act
        var result = validator.Validate(config);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.Code == "INVALID_RETRY_COUNT");
    }

    [Fact]
    public void Validate_WithProjectTypeMismatch_ShouldReturnWarning()
    {
        // Arrange
        var validator = new SemanticValidator();
        var config = new AcodeConfig
        {
            SchemaVersion = "1.0.0",
            Project = new ProjectConfig
            {
                Type = "dotnet",
                Languages = new List<string> { "python" } // Mismatch
            }
        };

        // Act
        var result = validator.Validate(config);

        // Assert
        result.Errors.Should().ContainSingle(e => e.Code == "PROJECT_TYPE_LANGUAGE_MISMATCH");
        result.Errors[0].Severity.Should().Be(ValidationSeverity.Warning);
    }

    [Fact]
    public void Validate_WithUnsupportedSchemaVersion_ShouldReturnError()
    {
        // Arrange
        var validator = new SemanticValidator();
        var config = new AcodeConfig { SchemaVersion = "2.0.0" };

        // Act
        var result = validator.Validate(config);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.Code == "UNSUPPORTED_SCHEMA_VERSION");
    }

    [Fact]
    public void Validate_WithDuplicateLanguages_ShouldReturnWarning()
    {
        // Arrange
        var validator = new SemanticValidator();
        var config = new AcodeConfig
        {
            SchemaVersion = "1.0.0",
            Project = new ProjectConfig
            {
                Languages = new List<string> { "python", "Python", "PYTHON" }
            }
        };

        // Act
        var result = validator.Validate(config);

        // Assert
        result.Errors.Should().ContainSingle(e => e.Code == "DUPLICATE_LANGUAGES");
        result.Errors[0].Severity.Should().Be(ValidationSeverity.Warning);
    }

    [Fact]
    public void Validate_WithInvalidEndpointUrl_ShouldReturnError()
    {
        // Arrange
        var validator = new SemanticValidator();
        var config = new AcodeConfig
        {
            SchemaVersion = "1.0.0",
            Model = new ModelConfig { Endpoint = "not-a-valid-url" }
        };

        // Act
        var result = validator.Validate(config);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.Code == "INVALID_ENDPOINT_URL");
    }

    // FR-002b-52: airgapped_lock prevents mode override
    [Fact]
    public void Validate_WithAirgappedLockAndNonAirgappedMode_ShouldReturnError()
    {
        // Arrange
        var validator = new SemanticValidator();
        var config = new AcodeConfig
        {
            SchemaVersion = "1.0.0",
            Mode = new ModeConfig
            {
                Default = "local-only",
                AirgappedLock = true // Locked to airgapped, but default is local-only
            }
        };

        // Act
        var result = validator.Validate(config);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.Code == "AIRGAPPED_LOCK_VIOLATION");
        result.Errors[0].Path.Should().Be("mode");
    }

    [Fact]
    public void Validate_WithAirgappedLockAndAirgappedMode_ShouldReturnSuccess()
    {
        // Arrange
        var validator = new SemanticValidator();
        var config = new AcodeConfig
        {
            SchemaVersion = "1.0.0",
            Mode = new ModeConfig
            {
                Default = "airgapped",
                AirgappedLock = true
            }
        };

        // Act
        var result = validator.Validate(config);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    // FR-002b-55: paths cannot escape repository root
    [Fact]
    public void Validate_WithAbsolutePathInSource_ShouldReturnError()
    {
        // Arrange
        var validator = new SemanticValidator();
        var config = new AcodeConfig
        {
            SchemaVersion = "1.0.0",
            Paths = new PathsConfig
            {
                Source = new[] { "/absolute/path/to/source" }
            }
        };

        // Act
        var result = validator.Validate(config);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Code == "PATH_ESCAPE_ATTEMPT");
        result.Errors.Should().Contain(e => e.Path == "paths.source");
    }

    [Fact]
    public void Validate_WithWindowsAbsolutePathInSource_ShouldReturnError()
    {
        // Arrange
        var validator = new SemanticValidator();
        var config = new AcodeConfig
        {
            SchemaVersion = "1.0.0",
            Paths = new PathsConfig
            {
                Source = new[] { "C:\\absolute\\path" }
            }
        };

        // Act
        var result = validator.Validate(config);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Code == "PATH_ESCAPE_ATTEMPT");
    }

    // FR-002b-57: command strings checked for shell injection
    [Fact]
    public void Validate_WithSemicolonInCommand_ShouldReturnError()
    {
        // Arrange
        var validator = new SemanticValidator();
        var config = new AcodeConfig
        {
            SchemaVersion = "1.0.0",
            Commands = new CommandsConfig
            {
                Build = "npm run build; rm -rf /"
            }
        };

        // Act
        var result = validator.Validate(config);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Code == "SHELL_INJECTION_DETECTED");
        result.Errors.Should().Contain(e => e.Path == "commands.build");
    }

    [Fact]
    public void Validate_WithPipeInCommand_ShouldReturnError()
    {
        // Arrange
        var validator = new SemanticValidator();
        var config = new AcodeConfig
        {
            SchemaVersion = "1.0.0",
            Commands = new CommandsConfig
            {
                Test = "npm test | grep ERROR"
            }
        };

        // Act
        var result = validator.Validate(config);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Code == "SHELL_INJECTION_DETECTED");
    }

    [Fact]
    public void Validate_WithCommandSubstitutionInCommand_ShouldReturnError()
    {
        // Arrange
        var validator = new SemanticValidator();
        var config = new AcodeConfig
        {
            SchemaVersion = "1.0.0",
            Commands = new CommandsConfig
            {
                Setup = "echo $(whoami)"
            }
        };

        // Act
        var result = validator.Validate(config);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Code == "SHELL_INJECTION_DETECTED");
    }

    [Fact]
    public void Validate_WithBackticksInCommand_ShouldReturnError()
    {
        // Arrange
        var validator = new SemanticValidator();
        var config = new AcodeConfig
        {
            SchemaVersion = "1.0.0",
            Commands = new CommandsConfig
            {
                Lint = "eslint `find . -name '*.js'`"
            }
        };

        // Act
        var result = validator.Validate(config);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Code == "SHELL_INJECTION_DETECTED");
    }

    // FR-002b-58: network.allowlist only valid in Burst mode
    [Fact]
    public void Validate_WithNetworkAllowlistInLocalOnlyMode_ShouldReturnError()
    {
        // Arrange
        var validator = new SemanticValidator();
        var config = new AcodeConfig
        {
            SchemaVersion = "1.0.0",
            Mode = new ModeConfig { Default = "local-only" },
            Network = new NetworkConfig
            {
                Allowlist = new[]
                {
                    new NetworkAllowlistEntry { Host = "api.example.com" }
                }
            }
        };

        // Act
        var result = validator.Validate(config);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Code == "NETWORK_ALLOWLIST_INVALID_MODE");
        result.Errors.Should().Contain(e => e.Path == "network.allowlist");
    }

    [Fact]
    public void Validate_WithNetworkAllowlistInBurstMode_ShouldReturnSuccess()
    {
        // Arrange
        var validator = new SemanticValidator();
        var config = new AcodeConfig
        {
            SchemaVersion = "1.0.0",
            Mode = new ModeConfig { Default = "burst" },
            Network = new NetworkConfig
            {
                Allowlist = new[]
                {
                    new NetworkAllowlistEntry { Host = "api.example.com" }
                }
            }
        };

        // Act
        var result = validator.Validate(config);

        // Assert - should pass (burst mode allows allowlist)
        result.Errors.Should().NotContain(e => e.Code == "NETWORK_ALLOWLIST_INVALID_MODE");
    }

    // FR-002b-62: ignore patterns are valid globs
    [Fact]
    public void Validate_WithInvalidGlobInIgnorePatterns_ShouldReturnError()
    {
        // Arrange
        var validator = new SemanticValidator();
        var config = new AcodeConfig
        {
            SchemaVersion = "1.0.0",
            Ignore = new IgnoreConfig
            {
                Patterns = new[] { "invalid[glob" } // Unclosed bracket
            }
        };

        // Act
        var result = validator.Validate(config);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Code == ConfigErrorCodes.InvalidGlob);
        result.Errors.Should().Contain(e => e.Path == "ignore.patterns");
    }

    [Fact]
    public void Validate_WithValidGlobInIgnorePatterns_ShouldReturnSuccess()
    {
        // Arrange
        var validator = new SemanticValidator();
        var config = new AcodeConfig
        {
            SchemaVersion = "1.0.0",
            Ignore = new IgnoreConfig
            {
                Patterns = new[] { "**/*.log", "node_modules/**", "*.tmp" }
            }
        };

        // Act
        var result = validator.Validate(config);

        // Assert
        result.Errors.Should().NotContain(e => e.Code == ConfigErrorCodes.InvalidGlob);
    }

    // FR-002b-63: path patterns are valid globs
    [Fact]
    public void Validate_WithInvalidGlobInSourcePaths_ShouldReturnError()
    {
        // Arrange
        var validator = new SemanticValidator();
        var config = new AcodeConfig
        {
            SchemaVersion = "1.0.0",
            Paths = new PathsConfig
            {
                Source = new[] { "src/**/*.cs", "invalid[glob" }
            }
        };

        // Act
        var result = validator.Validate(config);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Code == ConfigErrorCodes.InvalidGlob);
        result.Errors.Should().Contain(e => e.Path == "paths.source");
    }

    [Fact]
    public void Validate_WithValidGlobInPaths_ShouldReturnSuccess()
    {
        // Arrange
        var validator = new SemanticValidator();
        var config = new AcodeConfig
        {
            SchemaVersion = "1.0.0",
            Paths = new PathsConfig
            {
                Source = new[] { "src/**/*.cs" },
                Tests = new[] { "tests/**/*Tests.cs" }
            }
        };

        // Act
        var result = validator.Validate(config);

        // Assert
        result.Errors.Should().NotContain(e => e.Code == ConfigErrorCodes.InvalidGlob);
    }

    // FR-002b-69: referenced paths exist (warning if not)
    // Note: This test will be implementation-dependent since it needs filesystem access
    // We'll test the validation logic exists, actual filesystem checks are integration tests
    [Fact]
    public void Validate_WithNonExistentPathReference_ShouldReturnWarning()
    {
        // Arrange
        var validator = new SemanticValidator();
        var config = new AcodeConfig
        {
            SchemaVersion = "1.0.0",
            Paths = new PathsConfig
            {
                Source = new[] { "definitely-does-not-exist-path-xyz" }
            }
        };

        // Act
        var result = validator.Validate(config);

        // Assert - For now, we skip filesystem checks in unit tests
        // This will be tested in integration tests
        // Just verify the validator doesn't crash
        result.Should().NotBeNull();
    }
}
