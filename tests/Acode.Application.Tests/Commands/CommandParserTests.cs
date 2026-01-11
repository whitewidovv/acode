using Acode.Application.Commands;
using FluentAssertions;

namespace Acode.Application.Tests.Commands;

/// <summary>
/// Tests for CommandParser implementation.
/// Covers all command parsing formats per Task 002.c spec lines 826-851.
/// </summary>
public class CommandParserTests
{
    private readonly ICommandParser _parser = new CommandParser();

    // UT-002c-01: Parse string command → Returns CommandSpec
    [Fact]
    public void ParseString_WithValidCommand_ReturnsCommandSpec()
    {
        // Arrange
        var command = "npm install";

        // Act
        var result = _parser.ParseString(command);

        // Assert
        result.Should().NotBeNull();
        result.Run.Should().Be("npm install");
        result.Cwd.Should().Be(".", "default working directory");
        result.Timeout.Should().Be(300, "default timeout");
        result.Retry.Should().Be(0, "default retry count");
    }

    // UT-002c-05: Reject empty string → Returns validation error
    [Fact]
    public void ParseString_WithEmptyString_ThrowsArgumentException()
    {
        // Arrange
        var command = string.Empty;

        // Act
        var act = () => _parser.ParseString(command);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*cannot be empty*");
    }

    // UT-002c-07: Reject whitespace-only → Returns validation error
    [Fact]
    public void ParseString_WithWhitespaceOnly_ThrowsArgumentException()
    {
        // Arrange
        var command = "   \t\n  ";

        // Act
        var act = () => _parser.ParseString(command);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*cannot be empty*");
    }

    // UT-002c-08: Trim command string → Whitespace removed
    [Fact]
    public void ParseString_WithLeadingAndTrailingWhitespace_TrimsCommand()
    {
        // Arrange
        var command = "  npm test  \n";

        // Act
        var result = _parser.ParseString(command);

        // Assert
        result.Run.Should().Be("npm test", "whitespace should be trimmed");
    }

    // UT-002c-09: Preserve multi-line → Lines preserved
    [Fact]
    public void ParseString_WithMultiLineCommand_PreservesNewlines()
    {
        // Arrange
        var command = "echo line1\necho line2";

        // Act
        var result = _parser.ParseString(command);

        // Assert
        result.Run.Should().Contain("\n", "newlines should be preserved within command");
        result.Run.Should().Be("echo line1\necho line2");
    }

    // UT-002c-02: Parse array command → Returns CommandSpec[]
    [Fact]
    public void ParseArray_WithMultipleStrings_ReturnsCommandSpecList()
    {
        // Arrange
        var commands = new object[] { "npm install", "npm run build" };

        // Act
        var result = _parser.ParseArray(commands);

        // Assert
        result.Should().HaveCount(2);
        result[0].Run.Should().Be("npm install");
        result[1].Run.Should().Be("npm run build");
    }

    // UT-002c-06: Accept empty array → Returns empty CommandSpec[]
    [Fact]
    public void ParseArray_WithEmptyArray_ReturnsEmptyList()
    {
        // Arrange
        var commands = Array.Empty<object>();

        // Act
        var result = _parser.ParseArray(commands);

        // Assert
        result.Should().BeEmpty("empty arrays are allowed as no-op");
    }

    // UT-002c-04: Parse mixed array → Returns mixed CommandSpec[]
    [Fact]
    public void ParseArray_WithMixedFormats_ParsesAllFormats()
    {
        // Arrange
        var commands = new object[]
        {
            "npm install", // string
            new Dictionary<string, object> // object
            {
                { "run", "npm run build" },
                { "timeout", 600 }
            }
        };

        // Act
        var result = _parser.ParseArray(commands);

        // Assert
        result.Should().HaveCount(2);
        result[0].Run.Should().Be("npm install");
        result[0].Timeout.Should().Be(300, "default timeout for string format");
        result[1].Run.Should().Be("npm run build");
        result[1].Timeout.Should().Be(600, "custom timeout from object format");
    }

    // UT-002c-03: Parse object command → Returns CommandSpec with options
    [Fact]
    public void ParseObject_WithFullOptions_ReturnsCommandSpecWithAllOptions()
    {
        // Arrange
        var commandObject = new Dictionary<string, object>
        {
            { "run", "dotnet test" },
            { "cwd", "tests" },
            { "timeout", 120 },
            { "retry", 2 },
            { "continue_on_error", true },
            { "env", new Dictionary<string, object> { { "TEST_MODE", "fast" } } }
        };

        // Act
        var result = _parser.ParseObject(commandObject);

        // Assert
        result.Run.Should().Be("dotnet test");
        result.Cwd.Should().Be("tests");
        result.Timeout.Should().Be(120);
        result.Retry.Should().Be(2);
        result.ContinueOnError.Should().BeTrue();
        result.Env.Should().ContainKey("TEST_MODE");
        result.Env["TEST_MODE"].Should().Be("fast");
    }

    [Fact]
    public void ParseObject_WithMinimalOptions_ReturnsCommandSpecWithDefaults()
    {
        // Arrange
        var commandObject = new Dictionary<string, object>
        {
            { "run", "echo hello" }
        };

        // Act
        var result = _parser.ParseObject(commandObject);

        // Assert
        result.Run.Should().Be("echo hello");
        result.Cwd.Should().Be(".", "default working directory");
        result.Timeout.Should().Be(300, "default timeout");
        result.Retry.Should().Be(0, "default retry count");
        result.ContinueOnError.Should().BeFalse("default continue on error");
    }

    [Fact]
    public void ParseObject_WithoutRunProperty_ThrowsArgumentException()
    {
        // Arrange
        var commandObject = new Dictionary<string, object>
        {
            { "cwd", "src" },
            { "timeout", 60 }
        };

        // Act
        var act = () => _parser.ParseObject(commandObject);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*'run' property is required*");
    }

    [Fact]
    public void ParseObject_WithPlatformVariants_IncludesPlatforms()
    {
        // Arrange
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

        // Act
        var result = _parser.ParseObject(commandObject);

        // Assert
        result.Run.Should().Be("build.sh");
        result.Platforms.Should().NotBeNull();
        result.Platforms.Should().HaveCount(3);
        result.Platforms!["windows"].Should().Be("build.bat");
        result.Platforms["linux"].Should().Be("build.sh");
        result.Platforms["macos"].Should().Be("build.sh");
    }

    [Fact]
    public void Parse_WithString_DelegatesToParseString()
    {
        // Arrange
        var commandValue = "npm test";

        // Act
        var result = _parser.Parse(commandValue);

        // Assert
        result.Should().HaveCount(1);
        result[0].Run.Should().Be("npm test");
    }

    [Fact]
    public void Parse_WithArray_DelegatesToParseArray()
    {
        // Arrange
        var commandValue = new object[] { "npm install", "npm build" };

        // Act
        var result = _parser.Parse(commandValue);

        // Assert
        result.Should().HaveCount(2);
        result[0].Run.Should().Be("npm install");
        result[1].Run.Should().Be("npm build");
    }

    [Fact]
    public void Parse_WithObject_DelegatesToParseObject()
    {
        // Arrange
        var commandValue = new Dictionary<string, object>
        {
            { "run", "dotnet build" },
            { "timeout", 180 }
        };

        // Act
        var result = _parser.Parse(commandValue);

        // Assert
        result.Should().HaveCount(1);
        result[0].Run.Should().Be("dotnet build");
        result[0].Timeout.Should().Be(180);
    }

    [Fact]
    public void Parse_WithNullValue_ThrowsArgumentNullException()
    {
        // Arrange
        object? commandValue = null;

        // Act
        var act = () => _parser.Parse(commandValue!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Parse_WithUnsupportedType_ThrowsArgumentException()
    {
        // Arrange
        var commandValue = 12345; // integer, not supported

        // Act
        var act = () => _parser.Parse(commandValue);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Unsupported command format*");
    }
}
