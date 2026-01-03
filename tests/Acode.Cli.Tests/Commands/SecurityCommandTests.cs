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
        // Act
        var result = _command.ShowStatus();

        // Assert
        result.Should().Be(0);
    }

    [Fact]
    public void ShowDenylist_ReturnsSuccess()
    {
        // Act
        var result = _command.ShowDenylist();

        // Assert
        result.Should().Be(0);
    }

    [Fact]
    public void CheckPath_AllowedPath_ReturnsSuccess()
    {
        // Act
        var result = _command.CheckPath("src/Program.cs");

        // Assert
        result.Should().Be(0);
    }

    [Fact]
    public void CheckPath_ProtectedPath_ReturnsFailure()
    {
        // Act
        var result = _command.CheckPath("~/.ssh/id_rsa");

        // Assert
        result.Should().Be(1);
    }
}
