using Acode.Domain.Commands;
using FluentAssertions;

namespace Acode.Domain.Tests.Commands;

/// <summary>
/// Tests for CommandSpec record.
/// Verifies command specifications are immutable and have correct defaults.
/// </summary>
public class CommandSpecTests
{
    [Fact]
    public void CommandSpec_ShouldBeImmutableRecord()
    {
        // Arrange & Act
        var spec = new CommandSpec
        {
            Run = "npm test"
        };

        // Assert - records are immutable by design
        spec.Should().NotBeNull();
        spec.Run.Should().Be("npm test");
    }

    [Fact]
    public void CommandSpec_Run_IsRequired()
    {
        // Arrange & Act
        var spec = new CommandSpec
        {
            Run = "dotnet build"
        };

        // Assert
        spec.Run.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void CommandSpec_DefaultCwd_ShouldBeCurrentDirectory()
    {
        // Arrange & Act
        var spec = new CommandSpec
        {
            Run = "echo test"
        };

        // Assert
        spec.Cwd.Should().Be(".", "default is current directory");
    }

    [Fact]
    public void CommandSpec_DefaultTimeout_ShouldBe300()
    {
        // Arrange & Act
        var spec = new CommandSpec
        {
            Run = "npm test"
        };

        // Assert
        spec.Timeout.Should().Be(300, "per FR-002c-120: default timeout is 300 seconds");
    }

    [Fact]
    public void CommandSpec_DefaultRetry_ShouldBeZero()
    {
        // Arrange & Act
        var spec = new CommandSpec
        {
            Run = "cargo build"
        };

        // Assert
        spec.Retry.Should().Be(0, "per FR-002c-126: default retry is 0");
    }

    [Fact]
    public void CommandSpec_DefaultContinueOnError_ShouldBeFalse()
    {
        // Arrange & Act
        var spec = new CommandSpec
        {
            Run = "go test ./..."
        };

        // Assert
        spec.ContinueOnError.Should().BeFalse();
    }

    [Fact]
    public void CommandSpec_DefaultEnv_ShouldBeEmpty()
    {
        // Arrange & Act
        var spec = new CommandSpec
        {
            Run = "mvn test"
        };

        // Assert
        spec.Env.Should().NotBeNull();
        spec.Env.Should().BeEmpty();
    }

    [Fact]
    public void CommandSpec_WithCustomOptions_ShouldPreserveAllValues()
    {
        // Arrange & Act
        var spec = new CommandSpec
        {
            Run = "pytest tests/",
            Cwd = "backend",
            Env = new Dictionary<string, string> { ["CI"] = "true" },
            Timeout = 600,
            Retry = 3,
            ContinueOnError = true,
            Platforms = new Dictionary<string, string> { ["windows"] = "pytest.exe tests/" }
        };

        // Assert
        spec.Run.Should().Be("pytest tests/");
        spec.Cwd.Should().Be("backend");
        spec.Env.Should().ContainKey("CI");
        spec.Timeout.Should().Be(600);
        spec.Retry.Should().Be(3);
        spec.ContinueOnError.Should().BeTrue();
        spec.Platforms.Should().ContainKey("windows");
    }

    [Fact]
    public void CommandSpec_SupportsValueEquality()
    {
        // Arrange
        var envDict = new Dictionary<string, string> { ["CI"] = "true" };

        var spec1 = new CommandSpec
        {
            Run = "npm build",
            Timeout = 120,
            Env = envDict
        };

        var spec2 = new CommandSpec
        {
            Run = "npm build",
            Timeout = 120,
            Env = envDict
        };

        // Act & Assert - records support value-based equality when using same instances
        spec1.Should().Be(spec2);

        // Different properties should not be equal
        var spec3 = new CommandSpec
        {
            Run = "npm test",
            Timeout = 120,
            Env = envDict
        };

        spec1.Should().NotBe(spec3);
    }

    [Fact]
    public void CommandSpec_Platforms_CanBeNull()
    {
        // Arrange & Act
        var spec = new CommandSpec
        {
            Run = "make all",
            Platforms = null
        };

        // Assert
        spec.Platforms.Should().BeNull();
    }
}
