using FluentAssertions;

namespace Acode.Cli.Tests;

/// <summary>
/// Placeholder tests for CLI layer.
/// </summary>
public class ProgramTests
{
    [Fact]
    public void Main_ShouldReturnZero_WhenExecuted()
    {
        // Arrange
        var args = Array.Empty<string>();

        // Act
        var exitCode = Program.Main(args);

        // Assert
        exitCode.Should().Be(0);
    }
}
