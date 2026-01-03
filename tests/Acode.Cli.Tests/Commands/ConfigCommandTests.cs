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
    public async Task ValidateAsync_WithValidConfig_ReturnsSuccessExitCode()
    {
        // Arrange
        var command = new ConfigCommand(_mockLoader, _mockValidator);
        var config = new AcodeConfig { SchemaVersion = "1.0.0" };
        var validationResult = new ValidationResult { IsValid = true };

        _mockLoader.LoadAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(config);
        _mockValidator.Validate(config)
            .Returns(validationResult);

        using var consoleOutput = new StringWriter();
        Console.SetOut(consoleOutput);

        // Act
        var exitCode = await command.ValidateAsync(".agent/config.yml").ConfigureAwait(true);

        // Assert
        exitCode.Should().Be(0, "validation should succeed for valid config");
        consoleOutput.ToString().Should().Contain("✓", "success marker should be shown");
        consoleOutput.ToString().Should().Contain("valid", "validation result should be shown");
    }

    [Fact]
    public async Task ValidateAsync_WithInvalidConfig_ReturnsErrorExitCode()
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

        using var consoleOutput = new StringWriter();
        Console.SetOut(consoleOutput);

        // Act
        var exitCode = await command.ValidateAsync(".agent/config.yml").ConfigureAwait(true);

        // Assert
        exitCode.Should().Be(1, "validation should return error code for invalid config");
        consoleOutput.ToString().Should().Contain("Invalid mode", "error message should be shown");
    }

    [Fact]
    public async Task ValidateAsync_WithFileNotFound_ReturnsErrorExitCode()
    {
        // Arrange
        var command = new ConfigCommand(_mockLoader, _mockValidator);

        _mockLoader.LoadAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new FileNotFoundException("Config file not found"));

        using var consoleOutput = new StringWriter();
        Console.SetOut(consoleOutput);

        // Act
        var exitCode = await command.ValidateAsync(".agent/config.yml").ConfigureAwait(true);

        // Assert
        exitCode.Should().Be(1, "file not found should return error code");
        consoleOutput.ToString().Should().Contain("not found", "error message should be shown");
    }

    [Fact]
    public async Task ShowAsync_WithValidConfig_DisplaysConfig()
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

        using var consoleOutput = new StringWriter();
        Console.SetOut(consoleOutput);

        // Act
        var exitCode = await command.ShowAsync(".agent/config.yml", format: "yaml").ConfigureAwait(true);

        // Assert
        exitCode.Should().Be(0, "show should succeed for valid config");
        var output = consoleOutput.ToString();
        output.Should().Contain("1.0.0", "schema version should be shown");
        output.Should().Contain("test-project", "project name should be shown");
    }

    [Fact]
    public async Task ShowAsync_WithJsonFormat_DisplaysJson()
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

        using var consoleOutput = new StringWriter();
        Console.SetOut(consoleOutput);

        // Act
        var exitCode = await command.ShowAsync(".agent/config.yml", format: "json").ConfigureAwait(true);

        // Assert
        exitCode.Should().Be(0, "show should succeed for JSON format");
        var output = consoleOutput.ToString();
        output.Should().Contain("{", "JSON should start with brace");
        output.Should().Contain("\"schema_version\"", "JSON should contain snake_case keys");
        output.Should().Contain("\"test-project\"", "JSON should contain project name");
    }

    [Fact]
    public async Task ShowAsync_WithFileNotFound_ReturnsErrorExitCode()
    {
        // Arrange
        var command = new ConfigCommand(_mockLoader, _mockValidator);

        _mockLoader.LoadAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new FileNotFoundException("Config file not found"));

        using var consoleOutput = new StringWriter();
        Console.SetOut(consoleOutput);

        // Act
        var exitCode = await command.ShowAsync(".agent/config.yml").ConfigureAwait(true);

        // Assert
        exitCode.Should().Be(1, "file not found should return error code");
        consoleOutput.ToString().Should().Contain("not found", "error message should be shown");
    }
}
