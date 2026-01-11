using Acode.Domain.Commands;
using FluentAssertions;

namespace Acode.Domain.Tests.Commands;

/// <summary>
/// Tests for CommandLogFields static class.
/// Verifies all logging field constants are defined per Task 002.c spec lines 1106-1121.
/// </summary>
public class CommandLogFieldsTests
{
    [Fact]
    public void CommandLogFields_ShouldHaveCommandGroupConstant()
    {
        // Arrange & Act
        var value = CommandLogFields.CommandGroup;

        // Assert
        value.Should().Be("command_group", "per spec line 1110");
    }

    [Fact]
    public void CommandLogFields_ShouldHaveCommandConstant()
    {
        // Arrange & Act
        var value = CommandLogFields.Command;

        // Assert
        value.Should().Be("command", "per spec line 1111");
    }

    [Fact]
    public void CommandLogFields_ShouldHaveWorkingDirectoryConstant()
    {
        // Arrange & Act
        var value = CommandLogFields.WorkingDirectory;

        // Assert
        value.Should().Be("working_directory", "per spec line 1112");
    }

    [Fact]
    public void CommandLogFields_ShouldHaveExitCodeConstant()
    {
        // Arrange & Act
        var value = CommandLogFields.ExitCode;

        // Assert
        value.Should().Be("exit_code", "per spec line 1113");
    }

    [Fact]
    public void CommandLogFields_ShouldHaveDurationMsConstant()
    {
        // Arrange & Act
        var value = CommandLogFields.DurationMs;

        // Assert
        value.Should().Be("duration_ms", "per spec line 1114");
    }

    [Fact]
    public void CommandLogFields_ShouldHaveAttemptConstant()
    {
        // Arrange & Act
        var value = CommandLogFields.Attempt;

        // Assert
        value.Should().Be("attempt", "per spec line 1115");
    }

    [Fact]
    public void CommandLogFields_ShouldHaveTimedOutConstant()
    {
        // Arrange & Act
        var value = CommandLogFields.TimedOut;

        // Assert
        value.Should().Be("timed_out", "per spec line 1116");
    }

    [Fact]
    public void CommandLogFields_ShouldHavePlatformConstant()
    {
        // Arrange & Act
        var value = CommandLogFields.Platform;

        // Assert
        value.Should().Be("platform", "per spec line 1117");
    }

    [Fact]
    public void CommandLogFields_ShouldHaveEnvVarCountConstant()
    {
        // Arrange & Act
        var value = CommandLogFields.EnvVarCount;

        // Assert
        value.Should().Be("env_var_count", "per spec line 1118");
    }

    [Fact]
    public void CommandLogFields_AllConstants_ShouldBePublic()
    {
        // Arrange
        var type = typeof(CommandLogFields);

        // Act
        var fields = type.GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);

        // Assert
        fields.Should().HaveCountGreaterOrEqualTo(9, "should have at least 9 public constants");
        fields.Should().OnlyContain(f => f.IsLiteral && f.IsPublic, "all fields should be public constants");
    }
}
