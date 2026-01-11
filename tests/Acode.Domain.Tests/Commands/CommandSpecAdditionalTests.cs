using System.Text.Json;
using Acode.Domain.Commands;
using FluentAssertions;

namespace Acode.Domain.Tests.Commands;

/// <summary>
/// Additional tests for CommandSpec to complete coverage.
/// Covers UT-002c-21 and UT-002c-22.
/// </summary>
public class CommandSpecAdditionalTests
{
    // UT-002c-21: Command equality → Equal specs are equal
    [Fact]
    public void CommandSpec_WithSameValues_ShouldBeEqual()
    {
        // Arrange - use same dictionary instance for reference equality
        var env = new Dictionary<string, string> { { "NODE_ENV", "test" } };
        var spec1 = new CommandSpec
        {
            Run = "npm test",
            Cwd = "src",
            Timeout = 300,
            Retry = 2,
            Env = env
        };

        var spec2 = new CommandSpec
        {
            Run = "npm test",
            Cwd = "src",
            Timeout = 300,
            Retry = 2,
            Env = env
        };

        // Act & Assert
        spec1.Should().Be(spec2, "records with same values should be equal");
        (spec1 == spec2).Should().BeTrue("equality operator should work");
        spec1.GetHashCode().Should().Be(spec2.GetHashCode(), "hash codes should match");
    }

    [Fact]
    public void CommandSpec_WithDifferentRun_ShouldNotBeEqual()
    {
        // Arrange
        var spec1 = new CommandSpec { Run = "npm test" };
        var spec2 = new CommandSpec { Run = "npm build" };

        // Act & Assert
        spec1.Should().NotBe(spec2);
        (spec1 != spec2).Should().BeTrue();
    }

    [Fact]
    public void CommandSpec_WithDifferentTimeout_ShouldNotBeEqual()
    {
        // Arrange
        var spec1 = new CommandSpec { Run = "npm test", Timeout = 300 };
        var spec2 = new CommandSpec { Run = "npm test", Timeout = 600 };

        // Act & Assert
        spec1.Should().NotBe(spec2);
    }

    // UT-002c-22: Command serialization → Serializes to JSON
    [Fact]
    public void CommandSpec_CanSerializeToJson()
    {
        // Arrange
        var spec = new CommandSpec
        {
            Run = "npm test",
            Cwd = "src",
            Timeout = 300,
            Retry = 2,
            ContinueOnError = true,
            Env = new Dictionary<string, string> { { "NODE_ENV", "test" } }
        };

        // Act
        var json = JsonSerializer.Serialize(spec);

        // Assert
        json.Should().NotBeNullOrEmpty();
        json.Should().Contain("npm test");
        json.Should().Contain("src");
        json.Should().Contain("300");
        json.Should().Contain("2");
        json.Should().Contain("NODE_ENV");
        json.Should().Contain("test");
    }

    [Fact]
    public void CommandSpec_CanDeserializeFromJson()
    {
        // Arrange
        var json = """
        {
            "Run": "npm test",
            "Cwd": "src",
            "Timeout": 300,
            "Retry": 2,
            "ContinueOnError": true,
            "Env": {
                "NODE_ENV": "test"
            }
        }
        """;

        // Act
        var spec = JsonSerializer.Deserialize<CommandSpec>(json);

        // Assert
        spec.Should().NotBeNull();
        spec!.Run.Should().Be("npm test");
        spec.Cwd.Should().Be("src");
        spec.Timeout.Should().Be(300);
        spec.Retry.Should().Be(2);
        spec.ContinueOnError.Should().BeTrue();
        spec.Env.Should().ContainKey("NODE_ENV");
    }

    [Fact]
    public void CommandSpec_RoundTripSerialization_PreservesValues()
    {
        // Arrange
        var original = new CommandSpec
        {
            Run = "dotnet build",
            Cwd = "src/app",
            Timeout = 600,
            Retry = 3,
            ContinueOnError = false,
            Env = new Dictionary<string, string>
            {
                { "DOTNET_CLI_TELEMETRY_OPTOUT", "1" },
                { "Configuration", "Release" }
            },
            Platforms = new Dictionary<string, string>
            {
                { "windows", "build.bat" },
                { "linux", "build.sh" }
            }
        };

        // Act
        var json = JsonSerializer.Serialize(original);
        var deserialized = JsonSerializer.Deserialize<CommandSpec>(json);

        // Assert - check all properties individually since dictionaries use reference equality
        deserialized.Should().NotBeNull();
        deserialized!.Run.Should().Be(original.Run);
        deserialized.Cwd.Should().Be(original.Cwd);
        deserialized.Timeout.Should().Be(original.Timeout);
        deserialized.Retry.Should().Be(original.Retry);
        deserialized.ContinueOnError.Should().Be(original.ContinueOnError);
        deserialized.Env.Should().BeEquivalentTo(original.Env);
        deserialized.Platforms.Should().BeEquivalentTo(original.Platforms);
    }
}
