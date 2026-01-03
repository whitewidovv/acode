using FluentAssertions;

namespace Acode.Cli.Tests;

/// <summary>
/// Tests for the CLI Program entry point.
/// </summary>
public class ProgramTests
{
    [Fact]
    public void Main_WithNoArguments_ReturnsSuccessExitCode()
    {
        // Arrange
        var args = Array.Empty<string>();

        // Capture console output
        using var consoleOutput = new StringWriter();
        Console.SetOut(consoleOutput);

        // Act
        var exitCode = Program.Main(args);

        // Assert
        exitCode.Should().Be(0, "the program should return success exit code when no errors occur");
    }

    [Fact]
    public void Main_WithNoArguments_PrintsHelp()
    {
        // Arrange
        var args = Array.Empty<string>();

        // Capture console output
        using var consoleOutput = new StringWriter();
        Console.SetOut(consoleOutput);

        // Act
        Program.Main(args);
        var output = consoleOutput.ToString();

        // Assert
        output.Should().Contain("Acode", "the output should contain the application name");
        output.Should().Contain("Usage:", "the output should show usage information");
    }

    [Fact]
    public void Main_WithUnknownCommand_ReturnsErrorCode()
    {
        // Arrange
        var args = new[] { "unknown", "command" };

        // Capture console output
        using var consoleOutput = new StringWriter();
        Console.SetOut(consoleOutput);

        // Act
        var exitCode = Program.Main(args);

        // Assert
        exitCode.Should().Be(1, "unknown commands should return error code");
        consoleOutput.ToString().Should().Contain("Unknown command", "error message should be shown");
    }

    [Fact]
    public void Main_WithHelpFlag_PrintsHelp()
    {
        // Arrange
        var args = new[] { "--help" };

        // Capture console output
        using var consoleOutput = new StringWriter();
        Console.SetOut(consoleOutput);

        // Act
        var exitCode = Program.Main(args);

        // Assert
        exitCode.Should().Be(0, "help should return success code");
        consoleOutput.ToString().Should().Contain("Usage:", "help text should be shown");
    }

    [Fact]
    public void Main_WithVersionFlag_PrintsVersion()
    {
        // Arrange
        var args = new[] { "--version" };

        // Capture console output
        using var consoleOutput = new StringWriter();
        Console.SetOut(consoleOutput);

        // Act
        var exitCode = Program.Main(args);

        // Assert
        exitCode.Should().Be(0, "version should return success code");
        var output = consoleOutput.ToString();
        output.Should().Contain("Acode", "version output should contain app name");
        output.Should().Contain("0.1.0-alpha", "version output should contain version number");
    }
}
