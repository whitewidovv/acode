using FluentAssertions;

namespace Acode.Cli.Tests.Commands;

/// <summary>
/// Tests for SecurityCommand.
/// </summary>
public sealed class SecurityCommandTests
{
    private readonly global::Acode.Cli.Commands.SecurityCommand _command;

    public SecurityCommandTests()
    {
        _command = new global::Acode.Cli.Commands.SecurityCommand();
    }

    [Fact]
    public void ShowStatus_ReturnsSuccess()
    {
        // Arrange
        using var output = new StringWriter();
        Console.SetOut(output);

        // Act
        var result = _command.ShowStatus();

        // Assert
        result.Should().Be(0);
        output.ToString().Should().Contain("Security Status");
    }

    [Fact]
    public void ShowDenylist_ReturnsSuccess()
    {
        // Arrange
        using var output = new StringWriter();
        Console.SetOut(output);

        // Act
        var result = _command.ShowDenylist();

        // Assert
        result.Should().Be(0);
        output.ToString().Should().Contain("Protected Paths Denylist");
    }

    [Fact]
    public void CheckPath_AllowedPath_ReturnsSuccess()
    {
        // Arrange
        using var output = new StringWriter();
        Console.SetOut(output);

        // Act
        var result = _command.CheckPath("src/Program.cs");

        // Assert
        result.Should().Be(0);
        output.ToString().Should().Contain("ALLOWED");
    }

    [Fact]
    public void CheckPath_ProtectedPath_ReturnsFailure()
    {
        // Arrange
        using var output = new StringWriter();
        Console.SetOut(output);

        // Act
        var result = _command.CheckPath("~/.ssh/id_rsa");

        // Assert
        result.Should().Be(1);
        output.ToString().Should().Contain("BLOCKED");
    }
}
