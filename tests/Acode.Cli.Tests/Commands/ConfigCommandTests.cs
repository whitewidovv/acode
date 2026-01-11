using Acode.Application.Configuration;
using Acode.Cli.Commands;
using Acode.Domain.Configuration;
using FluentAssertions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace Acode.Cli.Tests.Commands;

/// <summary>
/// Tests for config validate and config show commands.
/// </summary>
public class ConfigCommandTests
{
    private readonly IConfigLoader _mockLoader;
    private readonly IConfigValidator _mockValidator;

    public ConfigCommandTests()
    {
        _mockLoader = Substitute.For<IConfigLoader>();
        _mockValidator = Substitute.For<IConfigValidator>();
    }

    [Fact]
    public async Task ExecuteAsync_ValidateWithValidConfig_ReturnsSuccess()
    {
        // Arrange
        var command = new ConfigCommand(_mockLoader, _mockValidator);
        var config = new AcodeConfig { SchemaVersion = "1.0.0" };
        var validationResult = new ValidationResult { IsValid = true };

        _mockLoader.LoadAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(config);
        _mockValidator.Validate(config)
            .Returns(validationResult);

        var output = new StringWriter();
        var context = new CommandContext
        {
            Configuration = new Dictionary<string, object>(),
            Args = new[] { "validate" },
            Formatter = new ConsoleFormatter(output, enableColors: false),
            Output = output,
            CancellationToken = CancellationToken.None,
        };

        // Act
        var exitCode = await command.ExecuteAsync(context).ConfigureAwait(true);

        // Assert
        exitCode.Should().Be(ExitCode.Success, "validation should succeed for valid config");
        output.ToString().Should().Contain("✓", "success marker should be shown");
        output.ToString().Should().Contain("valid", "validation result should be shown");
    }

    [Fact]
    public async Task ExecuteAsync_ValidateWithInvalidConfig_ReturnsError()
    {
        // Arrange
        var command = new ConfigCommand(_mockLoader, _mockValidator);
        var config = new AcodeConfig { SchemaVersion = "1.0.0" };
        var validationResult = new ValidationResult
        {
            IsValid = false,
            Errors = new List<ValidationError>
            {
                new()
                {
                    Code = "INVALID_MODE",
                    Message = "Invalid mode",
                    Path = "mode.default",
                    Severity = ValidationSeverity.Error,
                },
            }.AsReadOnly(),
        };

        _mockLoader.LoadAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(config);
        _mockValidator.Validate(config)
            .Returns(validationResult);

        var output = new StringWriter();
        var context = new CommandContext
        {
            Configuration = new Dictionary<string, object>(),
            Args = new[] { "validate" },
            Formatter = new ConsoleFormatter(output, enableColors: false),
            Output = output,
            CancellationToken = CancellationToken.None,
        };

        // Act
        var exitCode = await command.ExecuteAsync(context).ConfigureAwait(true);

        // Assert
        exitCode.Should().Be(ExitCode.GeneralError, "validation should return error code for invalid config");
        output.ToString().Should().Contain("Invalid mode", "error message should be shown");
    }

    [Fact]
    public async Task ExecuteAsync_ValidateWithFileNotFound_ReturnsError()
    {
        // Arrange
        var command = new ConfigCommand(_mockLoader, _mockValidator);

        _mockLoader.LoadAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new FileNotFoundException("Config file not found"));

        var output = new StringWriter();
        var context = new CommandContext
        {
            Configuration = new Dictionary<string, object>(),
            Args = new[] { "validate" },
            Formatter = new ConsoleFormatter(output, enableColors: false),
            Output = output,
            CancellationToken = CancellationToken.None,
        };

        // Act
        var exitCode = await command.ExecuteAsync(context).ConfigureAwait(true);

        // Assert
        exitCode.Should().Be(ExitCode.ConfigurationError, "missing config file should return configuration error code");
        output.ToString().Should().Contain("not found", "error message should be shown");
    }

    [Fact]
    public async Task ExecuteAsync_ShowWithValidConfig_DisplaysConfig()
    {
        // Arrange
        var command = new ConfigCommand(_mockLoader, _mockValidator);
        var config = new AcodeConfig
        {
            SchemaVersion = "1.0.0",
            Project = new ProjectConfig { Name = "test-project" },
        };

        _mockLoader.LoadAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(config);

        var output = new StringWriter();
        var context = new CommandContext
        {
            Configuration = new Dictionary<string, object>(),
            Args = new[] { "show" },
            Formatter = new ConsoleFormatter(output, enableColors: false),
            Output = output,
            CancellationToken = CancellationToken.None,
        };

        // Act
        var exitCode = await command.ExecuteAsync(context).ConfigureAwait(true);

        // Assert
        exitCode.Should().Be(ExitCode.Success, "show should succeed for valid config");
        var outputText = output.ToString();
        outputText.Should().Contain("1.0.0", "schema version should be shown");
        outputText.Should().Contain("test-project", "project name should be shown");
    }

    [Fact]
    public async Task ExecuteAsync_ShowWithJsonFormat_DisplaysJson()
    {
        // Arrange
        var command = new ConfigCommand(_mockLoader, _mockValidator);
        var config = new AcodeConfig
        {
            SchemaVersion = "1.0.0",
            Project = new ProjectConfig { Name = "test-project" },
        };

        _mockLoader.LoadAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(config);

        var output = new StringWriter();
        var context = new CommandContext
        {
            Configuration = new Dictionary<string, object>(),
            Args = new[] { "show", "--format", "json" },
            Formatter = new ConsoleFormatter(output, enableColors: false),
            Output = output,
            CancellationToken = CancellationToken.None,
        };

        // Act
        var exitCode = await command.ExecuteAsync(context).ConfigureAwait(true);

        // Assert
        exitCode.Should().Be(ExitCode.Success, "show should succeed for JSON format");
        var outputText = output.ToString();
        outputText.Should().Contain("{", "JSON should start with brace");
        outputText.Should().Contain("\"schema_version\"", "JSON should contain snake_case keys");
        outputText.Should().Contain("\"test-project\"", "JSON should contain project name");
    }

    [Fact]
    public async Task ExecuteAsync_ShowWithFileNotFound_ReturnsError()
    {
        // Arrange
        var command = new ConfigCommand(_mockLoader, _mockValidator);

        _mockLoader.LoadAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new FileNotFoundException("Config file not found"));

        var output = new StringWriter();
        var context = new CommandContext
        {
            Configuration = new Dictionary<string, object>(),
            Args = new[] { "show" },
            Formatter = new ConsoleFormatter(output, enableColors: false),
            Output = output,
            CancellationToken = CancellationToken.None,
        };

        // Act
        var exitCode = await command.ExecuteAsync(context).ConfigureAwait(true);

        // Assert
        exitCode.Should().Be(ExitCode.ConfigurationError, "file not found should return error code");
        output.ToString().Should().Contain("not found", "error message should be shown");
    }

    [Fact]
    public async Task ExecuteAsync_WithNoSubcommand_ReturnsInvalidArguments()
    {
        // Arrange
        var command = new ConfigCommand(_mockLoader, _mockValidator);

        var output = new StringWriter();
        var context = new CommandContext
        {
            Configuration = new Dictionary<string, object>(),
            Args = Array.Empty<string>(),
            Formatter = new ConsoleFormatter(output, enableColors: false),
            Output = output,
            CancellationToken = CancellationToken.None,
        };

        // Act
        var exitCode = await command.ExecuteAsync(context).ConfigureAwait(true);

        // Assert
        exitCode.Should().Be(ExitCode.InvalidArguments, "missing subcommand should return invalid arguments");
        output.ToString().Should().Contain("Missing subcommand", "error message should be shown");
    }

    [Fact]
    public async Task ExecuteAsync_WithUnknownSubcommand_ReturnsInvalidArguments()
    {
        // Arrange
        var command = new ConfigCommand(_mockLoader, _mockValidator);

        var output = new StringWriter();
        var context = new CommandContext
        {
            Configuration = new Dictionary<string, object>(),
            Args = new[] { "unknown" },
            Formatter = new ConsoleFormatter(output, enableColors: false),
            Output = output,
            CancellationToken = CancellationToken.None,
        };

        // Act
        var exitCode = await command.ExecuteAsync(context).ConfigureAwait(true);

        // Assert
        exitCode.Should().Be(ExitCode.InvalidArguments, "unknown subcommand should return invalid arguments");
        output.ToString().Should().Contain("Unknown subcommand", "error message should be shown");
    }

    [Fact]
    public async Task ExecuteAsync_ShowWithSensitiveData_RedactsSecrets()
    {
        // Arrange - NFR-002b-06: Config logging MUST redact sensitive fields
        var command = new ConfigCommand(_mockLoader, _mockValidator);
        var config = new AcodeConfig
        {
            SchemaVersion = "1.0.0",
            Project = new ProjectConfig { Name = "test-project" },
            Storage = new StorageConfig
            {
                Remote = new StorageRemoteConfig
                {
                    Postgres = new StoragePostgresConfig
                    {
                        Dsn = "postgresql://user:password@localhost:5432/db"
                    }
                }
            }
        };

        _mockLoader.LoadAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(config);

        var output = new StringWriter();
        var context = new CommandContext
        {
            Configuration = new Dictionary<string, object>(),
            Args = new[] { "show" },
            Formatter = new ConsoleFormatter(output, enableColors: false),
            Output = output,
            CancellationToken = CancellationToken.None,
        };

        // Act
        var exitCode = await command.ExecuteAsync(context).ConfigureAwait(true);

        // Assert
        exitCode.Should().Be(ExitCode.Success, "show should succeed");
        var outputText = output.ToString();
        outputText.Should().Contain("[REDACTED:dsn]", "DSN should be redacted per NFR-002b-08");
        outputText.Should().NotContain("password", "actual password should not be shown");
        outputText.Should().Contain("test-project", "non-sensitive fields should be shown");
    }

    [Fact]
    public async Task ExecuteAsync_ShowWithSensitiveDataInJson_RedactsSecrets()
    {
        // Arrange - NFR-002b-06: Config logging MUST redact sensitive fields
        var command = new ConfigCommand(_mockLoader, _mockValidator);
        var config = new AcodeConfig
        {
            SchemaVersion = "1.0.0",
            Storage = new StorageConfig
            {
                Remote = new StorageRemoteConfig
                {
                    Postgres = new StoragePostgresConfig
                    {
                        Dsn = "postgresql://admin:secret123@db.example.com:5432/mydb"
                    }
                }
            }
        };

        _mockLoader.LoadAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(config);

        var output = new StringWriter();
        var context = new CommandContext
        {
            Configuration = new Dictionary<string, object>(),
            Args = new[] { "show", "--format", "json" },
            Formatter = new ConsoleFormatter(output, enableColors: false),
            Output = output,
            CancellationToken = CancellationToken.None,
        };

        // Act
        var exitCode = await command.ExecuteAsync(context).ConfigureAwait(true);

        // Assert
        exitCode.Should().Be(ExitCode.Success, "show should succeed");
        var outputText = output.ToString();
        outputText.Should().Contain("[REDACTED:dsn]", "DSN should be redacted in JSON output");
        outputText.Should().NotContain("secret123", "actual password should not be in JSON");
        outputText.Should().NotContain("admin", "actual username should not be in JSON");
    }

    [Fact]
    public async Task ExecuteAsync_Init_CreatesMinimalConfig()
    {
        // Arrange
        var command = new ConfigCommand(_mockLoader, _mockValidator);
        var output = new StringWriter();
        var tempDir = Path.Combine(Path.GetTempPath(), $"acode-test-{Guid.NewGuid()}");
        Directory.CreateDirectory(tempDir);
        var originalDir = Directory.GetCurrentDirectory();

        try
        {
            Directory.SetCurrentDirectory(tempDir);

            var context = new CommandContext
            {
                Configuration = new Dictionary<string, object>(),
                Args = new[] { "init" },
                Formatter = new ConsoleFormatter(output, enableColors: false),
                Output = output,
                CancellationToken = CancellationToken.None,
            };

            // Act
            var exitCode = await command.ExecuteAsync(context).ConfigureAwait(true);

            // Assert
            exitCode.Should().Be(ExitCode.Success, "init should succeed");
            var configPath = Path.Combine(tempDir, ".agent", "config.yml");
            File.Exists(configPath).Should().BeTrue("config file should be created");

            var content = await File.ReadAllTextAsync(configPath).ConfigureAwait(true);
            content.Should().Contain("schema_version:", "minimal config should include schema version");
            output.ToString().Should().Contain("Created", "success message should be shown");
        }
        finally
        {
            Directory.SetCurrentDirectory(originalDir);
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, recursive: true);
            }
        }
    }

    [Fact]
    public async Task ExecuteAsync_InitWhenConfigExists_ReturnsError()
    {
        // Arrange
        var command = new ConfigCommand(_mockLoader, _mockValidator);
        var output = new StringWriter();
        var tempDir = Path.Combine(Path.GetTempPath(), $"acode-test-{Guid.NewGuid()}");
        var agentDir = Path.Combine(tempDir, ".agent");
        Directory.CreateDirectory(agentDir);
        var configPath = Path.Combine(agentDir, "config.yml");
        await File.WriteAllTextAsync(configPath, "schema_version: \"1.0.0\"").ConfigureAwait(true);
        var originalDir = Directory.GetCurrentDirectory();

        try
        {
            Directory.SetCurrentDirectory(tempDir);

            var context = new CommandContext
            {
                Configuration = new Dictionary<string, object>(),
                Args = new[] { "init" },
                Formatter = new ConsoleFormatter(output, enableColors: false),
                Output = output,
                CancellationToken = CancellationToken.None,
            };

            // Act
            var exitCode = await command.ExecuteAsync(context).ConfigureAwait(true);

            // Assert
            exitCode.Should().Be(ExitCode.GeneralError, "init should fail when config already exists");
            output.ToString().Should().Contain("already exists", "error message should indicate file exists");
        }
        finally
        {
            Directory.SetCurrentDirectory(originalDir);
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, recursive: true);
            }
        }
    }

    [Fact]
    public async Task ExecuteAsync_Reload_InvalidatesCache()
    {
        // Arrange
        var mockCache = Substitute.For<IConfigCache>();
        var command = new ConfigCommand(_mockLoader, _mockValidator, mockCache);
        var output = new StringWriter();

        var context = new CommandContext
        {
            Configuration = new Dictionary<string, object>(),
            Args = new[] { "reload" },
            Formatter = new ConsoleFormatter(output, enableColors: false),
            Output = output,
            CancellationToken = CancellationToken.None,
        };

        // Act
        var exitCode = await command.ExecuteAsync(context).ConfigureAwait(true);

        // Assert
        exitCode.Should().Be(ExitCode.Success, "reload should succeed");
        mockCache.Received(1).InvalidateAll();
        output.ToString().Should().Contain("Configuration cache invalidated", "success message should indicate cache invalidation");
    }

    [Fact]
    public async Task ExecuteAsync_ValidateWithStrictFlagAndWarnings_TreatsWarningsAsErrors()
    {
        // Arrange
        var command = new ConfigCommand(_mockLoader, _mockValidator);
        var config = new AcodeConfig { SchemaVersion = "1.0.0" };
        var validationResult = new ValidationResult
        {
            IsValid = true, // No errors, but has warnings
            Errors = new List<ValidationError>
            {
                new()
                {
                    Code = "UNKNOWN_FIELD",
                    Message = "Unknown field 'foo'",
                    Path = "foo",
                    Severity = ValidationSeverity.Warning,
                },
            }.AsReadOnly(),
        };

        _mockLoader.LoadAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(config);
        _mockValidator.Validate(config)
            .Returns(validationResult);

        var output = new StringWriter();
        var context = new CommandContext
        {
            Configuration = new Dictionary<string, object>(),
            Args = new[] { "validate", "--strict" },
            Formatter = new ConsoleFormatter(output, enableColors: false),
            Output = output,
            CancellationToken = CancellationToken.None,
        };

        // Act
        var exitCode = await command.ExecuteAsync(context).ConfigureAwait(true);

        // Assert
        exitCode.Should().Be(ExitCode.GeneralError, "strict mode should treat warnings as errors");
        output.ToString().Should().Contain("Unknown field", "warning message should be shown");
    }

    [Fact]
    public async Task ExecuteAsync_ValidateWithStrictFlagAndNoWarnings_ReturnsSuccess()
    {
        // Arrange
        var command = new ConfigCommand(_mockLoader, _mockValidator);
        var config = new AcodeConfig { SchemaVersion = "1.0.0" };
        var validationResult = new ValidationResult { IsValid = true };

        _mockLoader.LoadAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(config);
        _mockValidator.Validate(config)
            .Returns(validationResult);

        var output = new StringWriter();
        var context = new CommandContext
        {
            Configuration = new Dictionary<string, object>(),
            Args = new[] { "validate", "--strict" },
            Formatter = new ConsoleFormatter(output, enableColors: false),
            Output = output,
            CancellationToken = CancellationToken.None,
        };

        // Act
        var exitCode = await command.ExecuteAsync(context).ConfigureAwait(true);

        // Assert
        exitCode.Should().Be(ExitCode.Success, "strict mode should succeed when no warnings");
        output.ToString().Should().Contain("valid", "success message should be shown");
    }
}
