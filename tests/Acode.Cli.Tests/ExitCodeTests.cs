using FluentAssertions;

namespace Acode.Cli.Tests;

/// <summary>
/// Tests for <see cref="ExitCode"/>.
/// </summary>
public class ExitCodeTests
{
    [Fact]
    public void Success_HasValueZero()
    {
        // Arrange & Act
        var exitCode = ExitCode.Success;

        // Assert
        ((int)exitCode).Should().Be(0);
    }

    [Fact]
    public void GeneralError_HasValueOne()
    {
        // Arrange & Act
        var exitCode = ExitCode.GeneralError;

        // Assert
        ((int)exitCode).Should().Be(1);
    }

    [Fact]
    public void InvalidArguments_HasValueTwo()
    {
        // Arrange & Act
        var exitCode = ExitCode.InvalidArguments;

        // Assert
        ((int)exitCode).Should().Be(2);
    }

    [Fact]
    public void ConfigurationError_HasValueThree()
    {
        // Arrange & Act
        var exitCode = ExitCode.ConfigurationError;

        // Assert
        ((int)exitCode).Should().Be(3);
    }

    [Fact]
    public void RuntimeError_HasValueFour()
    {
        // Arrange & Act
        var exitCode = ExitCode.RuntimeError;

        // Assert
        ((int)exitCode).Should().Be(4);
    }

    [Fact]
    public void UserCancellation_HasValueFive()
    {
        // Arrange & Act
        var exitCode = ExitCode.UserCancellation;

        // Assert
        ((int)exitCode).Should().Be(5);
    }

    [Fact]
    public void SignalInterrupt_HasValue130()
    {
        // Arrange & Act
        var exitCode = ExitCode.SignalInterrupt;

        // Assert
        ((int)exitCode).Should().Be(130);
    }

    [Fact]
    public void AllExitCodes_HaveDistinctValues()
    {
        // Arrange
        var exitCodes = Enum.GetValues<ExitCode>();

        // Act
        var distinctValues = exitCodes.Select(e => (int)e).Distinct().ToList();

        // Assert
        distinctValues.Should().HaveCount(exitCodes.Length, "all exit codes should have unique values");
    }
}
