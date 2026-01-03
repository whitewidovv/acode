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
    public void Main_WithNoArguments_PrintsVersionInformation()
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
        output.Should().Contain("0.1.0-alpha", "the output should contain the version number");
    }

    [Fact]
    public void Main_WithArguments_IgnoresThemAndReturnsSuccess()
    {
        // Arrange
        var args = new[] { "some", "random", "arguments" };

        // Capture console output
        using var consoleOutput = new StringWriter();
        Console.SetOut(consoleOutput);

        // Act
        var exitCode = Program.Main(args);

        // Assert
        exitCode.Should().Be(0, "the program should ignore arguments and return success (placeholder behavior)");
    }
}
