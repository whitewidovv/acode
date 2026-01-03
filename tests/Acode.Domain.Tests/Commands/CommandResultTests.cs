using Acode.Domain.Commands;
using FluentAssertions;
using Xunit;

namespace Acode.Domain.Tests.Commands;

/// <summary>
/// Tests for CommandResult record.
/// Verifies command execution results are captured correctly.
/// </summary>
public class CommandResultTests
{
    [Fact]
    public void CommandResult_ShouldBeImmutableRecord()
    {
        // Arrange & Act
        var result = new CommandResult
        {
            ExitCode = 0,
            Stdout = "Build succeeded",
            Stderr = string.Empty,
            Duration = TimeSpan.FromSeconds(5),
            TimedOut = false,
            AttemptCount = 1
        };

        // Assert
        result.Should().NotBeNull();
        result.ExitCode.Should().Be(0);
    }

    [Fact]
    public void CommandResult_Success_ShouldBeTrueWhenExitCodeIsZero()
    {
        // Arrange & Act
        var result = new CommandResult
        {
            ExitCode = 0,
            Stdout = "All tests passed",
            Stderr = string.Empty,
            Duration = TimeSpan.FromSeconds(10),
            TimedOut = false,
            AttemptCount = 1
        };

        // Assert
        result.Success.Should().BeTrue("exit code 0 means success");
    }

    [Fact]
    public void CommandResult_Success_ShouldBeFalseWhenExitCodeIsNonZero()
    {
        // Arrange & Act
        var result = new CommandResult
        {
            ExitCode = 1,
            Stdout = string.Empty,
            Stderr = "Build failed",
            Duration = TimeSpan.FromSeconds(2),
            TimedOut = false,
            AttemptCount = 1
        };

        // Assert
        result.Success.Should().BeFalse("non-zero exit code means failure");
    }

    [Fact]
    public void CommandResult_TimedOut_ShouldBeTracked()
    {
        // Arrange & Act
        var result = new CommandResult
        {
            ExitCode = 124,
            Stdout = "Partial output...",
            Stderr = "Process killed due to timeout",
            Duration = TimeSpan.FromSeconds(300),
            TimedOut = true,
            AttemptCount = 1
        };

        // Assert
        result.TimedOut.Should().BeTrue();
        result.ExitCode.Should().Be(124, "timeout exit code per FR-002c-106");
    }

    [Fact]
    public void CommandResult_AttemptCount_ShouldBeTracked()
    {
        // Arrange & Act
        var result = new CommandResult
        {
            ExitCode = 0,
            Stdout = "Success on retry",
            Stderr = string.Empty,
            Duration = TimeSpan.FromSeconds(3),
            TimedOut = false,
            AttemptCount = 3
        };

        // Assert
        result.AttemptCount.Should().Be(3, "retry attempts should be tracked");
    }

    [Fact]
    public void CommandResult_Duration_ShouldBeTracked()
    {
        // Arrange & Act
        var duration = TimeSpan.FromMilliseconds(1234);
        var result = new CommandResult
        {
            ExitCode = 0,
            Stdout = "Done",
            Stderr = string.Empty,
            Duration = duration,
            TimedOut = false,
            AttemptCount = 1
        };

        // Assert
        result.Duration.Should().Be(duration);
        result.Duration.TotalMilliseconds.Should().Be(1234);
    }

    [Fact]
    public void CommandResult_SupportsValueEquality()
    {
        // Arrange
        var result1 = new CommandResult
        {
            ExitCode = 0,
            Stdout = "Success",
            Stderr = string.Empty,
            Duration = TimeSpan.FromSeconds(5),
            TimedOut = false,
            AttemptCount = 1
        };

        var result2 = new CommandResult
        {
            ExitCode = 0,
            Stdout = "Success",
            Stderr = string.Empty,
            Duration = TimeSpan.FromSeconds(5),
            TimedOut = false,
            AttemptCount = 1
        };

        // Act & Assert
        result1.Should().Be(result2);
    }

    [Fact]
    public void CommandResult_AllPropertiesRequired()
    {
        // Arrange & Act
        var result = new CommandResult
        {
            ExitCode = 127,
            Stdout = string.Empty,
            Stderr = "command not found",
            Duration = TimeSpan.Zero,
            TimedOut = false,
            AttemptCount = 1
        };

        // Assert - all required properties are set
        result.ExitCode.Should().Be(127);
        result.Stdout.Should().NotBeNull();
        result.Stderr.Should().NotBeNull();
        result.Duration.Should().Be(TimeSpan.Zero);
        result.TimedOut.Should().BeFalse();
        result.AttemptCount.Should().BeGreaterOrEqualTo(1);
    }
}
