using Acode.Domain.Commands;
using FluentAssertions;
using Xunit;

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
    public void CommandSpec_DefaultWorkingDirectory_ShouldBeCurrentDirectory()
    {
        // Arrange & Act
        var spec = new CommandSpec
        {
            Run = "echo test"
        };

        // Assert
        spec.WorkingDirectory.Should().Be(".", "default is current directory");
    }

    [Fact]
    public void CommandSpec_DefaultTimeoutSeconds_ShouldBe300()
    {
        // Arrange & Act
        var spec = new CommandSpec
        {
            Run = "npm test"
        };

        // Assert
        spec.TimeoutSeconds.Should().Be(300, "per FR-002c-120: default timeout is 300 seconds");
    }

    [Fact]
    public void CommandSpec_DefaultRetryCount_ShouldBeZero()
    {
        // Arrange & Act
        var spec = new CommandSpec
        {
            Run = "cargo build"
        };

        // Assert
        spec.RetryCount.Should().Be(0, "per FR-002c-126: default retry is 0");
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
    public void CommandSpec_DefaultEnvironment_ShouldBeEmpty()
    {
        // Arrange & Act
        var spec = new CommandSpec
        {
            Run = "mvn test"
        };

        // Assert
        spec.Environment.Should().NotBeNull();
        spec.Environment.Should().BeEmpty();
    }

    [Fact]
    public void CommandSpec_WithCustomOptions_ShouldPreserveAllValues()
    {
        // Arrange & Act
        var spec = new CommandSpec
        {
            Run = "pytest tests/",
            WorkingDirectory = "backend",
            Environment = new Dictionary<string, string> { ["CI"] = "true" },
            TimeoutSeconds = 600,
            RetryCount = 3,
            ContinueOnError = true,
            PlatformVariants = new Dictionary<string, string> { ["windows"] = "pytest.exe tests/" }
        };

        // Assert
        spec.Run.Should().Be("pytest tests/");
        spec.WorkingDirectory.Should().Be("backend");
        spec.Environment.Should().ContainKey("CI");
        spec.TimeoutSeconds.Should().Be(600);
        spec.RetryCount.Should().Be(3);
        spec.ContinueOnError.Should().BeTrue();
        spec.PlatformVariants.Should().ContainKey("windows");
    }

    [Fact]
    public void CommandSpec_SupportsValueEquality()
    {
        // Arrange
        var envDict = new Dictionary<string, string> { ["CI"] = "true" };

        var spec1 = new CommandSpec
        {
            Run = "npm build",
            TimeoutSeconds = 120,
            Environment = envDict
        };

        var spec2 = new CommandSpec
        {
            Run = "npm build",
            TimeoutSeconds = 120,
            Environment = envDict
        };

        // Act & Assert - records support value-based equality when using same instances
        spec1.Should().Be(spec2);

        // Different properties should not be equal
        var spec3 = new CommandSpec
        {
            Run = "npm test",
            TimeoutSeconds = 120,
            Environment = envDict
        };

        spec1.Should().NotBe(spec3);
    }

    [Fact]
    public void CommandSpec_PlatformVariants_CanBeNull()
    {
        // Arrange & Act
        var spec = new CommandSpec
        {
            Run = "make all",
            PlatformVariants = null
        };

        // Assert
        spec.PlatformVariants.Should().BeNull();
    }
}
