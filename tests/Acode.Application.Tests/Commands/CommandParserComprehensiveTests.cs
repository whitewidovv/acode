using Acode.Application.Commands;
using FluentAssertions;

namespace Acode.Application.Tests.Commands;

/// <summary>
/// Comprehensive command parser tests.
/// Covers UT-002c-23, UT-002c-24, and UT-002c-25.
/// </summary>
public class CommandParserComprehensiveTests
{
    private readonly ICommandParser _parser = new CommandParser();
    private readonly ICommandValidator _validator = new CommandValidator();

    // UT-002c-23: All groups parseable → All six groups parse
    [Theory]
    [InlineData("npm install")]
    [InlineData("npm run build")]
    [InlineData("npm test")]
    [InlineData("npm run lint")]
    [InlineData("npm run format")]
    [InlineData("npm start")]
    public void Parse_AllSixCommandGroups_ParsesSuccessfully(string command)
    {
        // Act
        var specs = _parser.Parse(command);

        // Assert
        specs.Should().HaveCount(1, "all six command groups should parse to single spec");
        specs[0].Run.Should().Be(command);
    }

    [Fact]
    public void Parse_AllSixGroupsWithDifferentFormats_AllParseCorrectly()
    {
        // Arrange - simulate all six command groups with different formats
        var setup = "npm install"; // string
        var build = new object[] { "npm run clean", "npm run build" }; // array
        var test = new Dictionary<string, object> { { "run", "npm test" }, { "timeout", 600 } }; // object
        var lint = "npm run lint"; // string
        var format = new Dictionary<string, object> { { "run", "npm run format" } }; // object
        var start = "npm start"; // string

        // Act
        var setupSpecs = _parser.Parse(setup);
        var buildSpecs = _parser.Parse(build);
        var testSpecs = _parser.Parse(test);
        var lintSpecs = _parser.Parse(lint);
        var formatSpecs = _parser.Parse(format);
        var startSpecs = _parser.Parse(start);

        // Assert
        setupSpecs.Should().HaveCount(1);
        buildSpecs.Should().HaveCount(2);
        testSpecs.Should().HaveCount(1);
        lintSpecs.Should().HaveCount(1);
        formatSpecs.Should().HaveCount(1);
        startSpecs.Should().HaveCount(1);

        testSpecs[0].Timeout.Should().Be(600, "object format should preserve timeout");
    }

    // UT-002c-24: Missing group handled → Returns null/error appropriately
    [Fact]
    public void Parse_WithNullCommand_ThrowsArgumentNullException()
    {
        // Arrange
        object? nullCommand = null;

        // Act
        var act = () => _parser.Parse(nullCommand!);

        // Assert
        act.Should().Throw<ArgumentNullException>("null commands should be rejected");
    }

    [Fact]
    public void Parse_WithUnsupportedType_ThrowsArgumentException()
    {
        // Arrange - integer is not a supported command type
        var unsupportedCommand = 12345;

        // Act
        var act = () => _parser.Parse(unsupportedCommand);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Unsupported command format*", "unsupported types should be rejected with clear error");
    }

    [Fact]
    public void ParseString_WithNullString_ThrowsArgumentNullException()
    {
        // Arrange
        string? nullString = null;

        // Act
        var act = () => _parser.ParseString(nullString!);

        // Assert
        act.Should().Throw<ArgumentNullException>("null string should be rejected");
    }

    [Fact]
    public void ParseArray_WithNullArray_ThrowsArgumentNullException()
    {
        // Arrange
        IEnumerable<object>? nullArray = null;

        // Act
        var act = () => _parser.ParseArray(nullArray!);

        // Assert
        act.Should().Throw<ArgumentNullException>("null array should be rejected");
    }

    [Fact]
    public void ParseObject_WithNullObject_ThrowsArgumentNullException()
    {
        // Arrange
        IDictionary<string, object>? nullObject = null;

        // Act
        var act = () => _parser.ParseObject(nullObject!);

        // Assert
        act.Should().Throw<ArgumentNullException>("null object should be rejected");
    }

    // UT-002c-25: Command validation → Invalid commands rejected
    [Fact]
    public void ParseAndValidate_WithCompletelyInvalidCommand_RejectsCommand()
    {
        // Arrange
        var invalidCommand = new Dictionary<string, object>
        {
            { "run", "npm test" },
            { "cwd", "/absolute/path" }, // Invalid: absolute path
            { "timeout", -1 }, // Invalid: negative timeout
            { "retry", 20 } // Invalid: exceeds max retry count of 10
        };

        // Act
        var specs = _parser.Parse(invalidCommand);
        var validation = _validator.Validate(specs[0], "/repo");

        // Assert
        specs.Should().HaveCount(1, "command should parse even if invalid");
        validation.IsValid.Should().BeFalse("validation should fail for invalid command");
        validation.ErrorMessage.Should().NotBeNullOrEmpty("error message should explain what's wrong");
    }

    [Fact]
    public void Validate_WithAllValidationTypes_DetectsAllIssues()
    {
        // Arrange - test each validation rule separately
        var repositoryRoot = "/repo";

        var invalidCwd = new Dictionary<string, object> { { "run", "test" }, { "cwd", "../outside" } };
        var invalidTimeout = new Dictionary<string, object> { { "run", "test" }, { "timeout", -100 } };
        var invalidRetry = new Dictionary<string, object> { { "run", "test" }, { "retry", 15 } };
        var invalidEnv = new Dictionary<string, object>
        {
            { "run", "test" },
            { "env", new Dictionary<string, object> { { "INVALID=NAME", "value" } } }
        };

        // Act & Assert - each should fail validation
        var cwdSpec = _parser.Parse(invalidCwd)[0];
        var cwdValidation = _validator.Validate(cwdSpec, repositoryRoot);
        cwdValidation.IsValid.Should().BeFalse("path traversal should be rejected");

        var timeoutSpec = _parser.Parse(invalidTimeout)[0];
        var timeoutValidation = _validator.Validate(timeoutSpec, repositoryRoot);
        timeoutValidation.IsValid.Should().BeFalse("negative timeout should be rejected");

        var retrySpec = _parser.Parse(invalidRetry)[0];
        var retryValidation = _validator.Validate(retrySpec, repositoryRoot);
        retryValidation.IsValid.Should().BeFalse("excessive retry count should be rejected");

        var envSpec = _parser.Parse(invalidEnv)[0];
        var envValidation = _validator.Validate(envSpec, repositoryRoot);
        envValidation.IsValid.Should().BeFalse("invalid env var names should be rejected");
    }

    [Fact]
    public void ParseAndValidate_WithValidCommand_PassesValidation()
    {
        // Arrange
        var validCommand = new Dictionary<string, object>
        {
            { "run", "npm test" },
            { "cwd", "src/app" },
            { "timeout", 300 },
            { "retry", 2 },
            { "env", new Dictionary<string, object> { { "NODE_ENV", "test" } } }
        };

        // Act
        var specs = _parser.Parse(validCommand);
        var validation = _validator.Validate(specs[0], "/repo");

        // Assert
        validation.IsValid.Should().BeTrue("valid command should pass validation");
        validation.ErrorMessage.Should().BeNull("valid command should have no error message");
    }
}
