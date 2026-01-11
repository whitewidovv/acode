using Acode.Application.Commands;
using FluentAssertions;

namespace Acode.Integration.Tests.Commands;

/// <summary>
/// Integration tests for command parsing.
/// Tests IT-002c-01: Load config with all command groups → All groups accessible.
/// </summary>
public class CommandParsingIntegrationTests
{
    private readonly ICommandParser _parser = new CommandParser();
    private readonly ICommandValidator _validator = new CommandValidator();

    [Fact]
    public void ParseAndValidate_WithCompleteConfig_Succeeds()
    {
        // Arrange - simulate a complete config with all command groups
        var setupCommands = new object[]
        {
            "npm install",
            new Dictionary<string, object>
            {
                { "run", "npm run postinstall" },
                { "timeout", 600 }
            }
        };

        var buildCommand = new Dictionary<string, object>
        {
            { "run", "npm run build" },
            { "cwd", "src" },
            { "env", new Dictionary<string, object> { { "NODE_ENV", "production" } } },
            { "timeout", 300 }
        };

        var testCommand = "npm test";
        var lintCommand = "npm run lint";
        var formatCommand = "npm run format";
        var startCommand = "npm start";

        // Act - parse all command groups
        var setupSpecs = _parser.Parse(setupCommands);
        var buildSpecs = _parser.Parse(buildCommand);
        var testSpecs = _parser.Parse(testCommand);
        var lintSpecs = _parser.Parse(lintCommand);
        var formatSpecs = _parser.Parse(formatCommand);
        var startSpecs = _parser.Parse(startCommand);

        // Validate all parsed commands
        var repositoryRoot = "/repo";
        var setupValidation = _validator.Validate(setupSpecs[0], repositoryRoot);
        var buildValidation = _validator.Validate(buildSpecs[0], repositoryRoot);
        var testValidation = _validator.Validate(testSpecs[0], repositoryRoot);
        var lintValidation = _validator.Validate(lintSpecs[0], repositoryRoot);
        var formatValidation = _validator.Validate(formatSpecs[0], repositoryRoot);
        var startValidation = _validator.Validate(startSpecs[0], repositoryRoot);

        // Assert - all groups parsed and validated successfully
        setupSpecs.Should().HaveCount(2);
        buildSpecs.Should().HaveCount(1);
        testSpecs.Should().HaveCount(1);
        lintSpecs.Should().HaveCount(1);
        formatSpecs.Should().HaveCount(1);
        startSpecs.Should().HaveCount(1);

        setupValidation.IsValid.Should().BeTrue();
        buildValidation.IsValid.Should().BeTrue();
        testValidation.IsValid.Should().BeTrue();
        lintValidation.IsValid.Should().BeTrue();
        formatValidation.IsValid.Should().BeTrue();
        startValidation.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Parse_AllSixCommandGroups_ReturnsValidSpecs()
    {
        // Arrange - all six command groups per FR-002c-01 and FR-002c-02
        var commandGroups = new Dictionary<string, object>
        {
            { "setup", "npm install" },
            { "build", "npm run build" },
            { "test", "npm test" },
            { "lint", "npm run lint" },
            { "format", "npm run format" },
            { "start", "npm start" }
        };

        // Act & Assert - parse each group
        foreach (var (group, command) in commandGroups)
        {
            var specs = _parser.Parse(command);
            specs.Should().HaveCount(1, $"command group '{group}' should parse to exactly one spec");
            specs[0].Run.Should().NotBeNullOrEmpty();
        }
    }

    [Fact]
    public void ParseAndValidate_WithPlatformVariants_SelectsCorrectVariant()
    {
        // Arrange - IT-002c-13: Platform variant selected → Correct variant used
        var currentPlatform = PlatformDetector.GetCurrentPlatform();
        var commandObject = new Dictionary<string, object>
        {
            { "run", "build.sh" },
            {
                "platforms", new Dictionary<string, object>
                {
                    { "windows", "build.bat" },
                    { "linux", "build.sh" },
                    { "macos", "build.sh" }
                }
            }
        };

        // Act - parse command
        var specs = _parser.Parse(commandObject);
        var spec = specs[0];

        // Select platform-specific command
        var selectedCommand = PlatformDetector.SelectCommand(spec.Run, spec.Platforms);

        // Assert
        spec.Platforms.Should().NotBeNull();
        spec.Platforms.Should().ContainKey(currentPlatform);
        selectedCommand.Should().Be(spec.Platforms![currentPlatform]);
    }

    [Fact]
    public void ParseAndValidate_ComplexMixedConfig_HandlesAllFormats()
    {
        // Arrange - complex real-world config with mixed formats
        var commands = new object[]
        {
            "echo 'Starting setup'",
            new Dictionary<string, object>
            {
                { "run", "npm ci" },
                { "timeout", 600 },
                { "retry", 2 }
            },
            new Dictionary<string, object>
            {
                { "run", "npm run generate" },
                { "cwd", "codegen" },
                { "env", new Dictionary<string, object> { { "DEBUG", "true" } } }
            },
            "echo 'Setup complete'"
        };

        // Act
        var specs = _parser.Parse(commands);

        // Assert
        specs.Should().HaveCount(4);
        specs[0].Run.Should().Contain("Starting setup");
        specs[1].Run.Should().Be("npm ci");
        specs[1].Timeout.Should().Be(600);
        specs[1].Retry.Should().Be(2);
        specs[2].Run.Should().Be("npm run generate");
        specs[2].Cwd.Should().Be("codegen");
        specs[2].Env.Should().ContainKey("DEBUG");
        specs[3].Run.Should().Contain("Setup complete");

        // Validate all
        var repositoryRoot = "/repo";
        foreach (var spec in specs)
        {
            var validation = _validator.Validate(spec, repositoryRoot);
            validation.IsValid.Should().BeTrue($"spec with command '{spec.Run}' should be valid");
        }
    }

    [Fact]
    public void ParseAndValidate_WithInvalidConfig_ReturnsValidationError()
    {
        // Arrange - invalid config with path traversal
        var commandObject = new Dictionary<string, object>
        {
            { "run", "npm test" },
            { "cwd", "../../../etc" }
        };

        // Act
        var specs = _parser.Parse(commandObject);
        var validation = _validator.Validate(specs[0], "/repo");

        // Assert
        validation.IsValid.Should().BeFalse("path traversal should be rejected");
        validation.ErrorMessage.Should().Contain("path traversal");
    }

    [Fact]
    public void Parse_EmptyArray_ReturnsEmptyList()
    {
        // Arrange - per FR-002c-46: empty array allowed (no-op)
        var commands = Array.Empty<object>();

        // Act
        var specs = _parser.Parse(commands);

        // Assert
        specs.Should().BeEmpty();
    }
}
