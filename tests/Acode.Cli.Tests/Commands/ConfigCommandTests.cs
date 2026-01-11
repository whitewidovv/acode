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
}
