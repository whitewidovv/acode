using Acode.Domain.Modes;
using FluentAssertions;

namespace Acode.Domain.Tests.Modes;

/// <summary>
/// Tests for the Capability enum.
/// Verifies all capability categories per Task 001.a requirements.
/// </summary>
public class CapabilityTests
{
    [Fact]
    public void Capability_ShouldIncludeNetworkCapabilities()
    {
        // Arrange & Act
        var capabilities = Enum.GetValues<Capability>();

        // Assert
        capabilities.Should().Contain(Capability.LocalhostNetwork, "FR-001a-31");
        capabilities.Should().Contain(Capability.LocalAreaNetwork, "FR-001a-32");
        capabilities.Should().Contain(Capability.ExternalNetwork, "per mode matrix");
        capabilities.Should().Contain(Capability.DnsLookup, "per mode matrix");
    }

    [Fact]
    public void Capability_ShouldIncludeLlmProviderCapabilities()
    {
        // Arrange & Act
        var capabilities = Enum.GetValues<Capability>();

        // Assert
        capabilities.Should().Contain(Capability.OllamaLocal, "FR-001a-32");
        capabilities.Should().Contain(Capability.OpenAiApi, "FR-001a-38");
        capabilities.Should().Contain(Capability.AnthropicApi, "per denylist");
        capabilities.Should().Contain(Capability.AzureOpenAiApi, "per denylist");
        capabilities.Should().Contain(Capability.CustomLlmApi, "per mode matrix");
    }

    [Fact]
    public void Capability_ShouldIncludeFileSystemCapabilities()
    {
        // Arrange & Act
        var capabilities = Enum.GetValues<Capability>();

        // Assert
        capabilities.Should().Contain(Capability.ReadProjectFiles, "per mode matrix");
        capabilities.Should().Contain(Capability.WriteProjectFiles, "per mode matrix");
        capabilities.Should().Contain(Capability.ReadSystemFiles, "per mode matrix");
        capabilities.Should().Contain(Capability.WriteSystemFiles, "per mode matrix");
    }

    [Fact]
    public void Capability_ShouldIncludeToolExecutionCapabilities()
    {
        // Arrange & Act
        var capabilities = Enum.GetValues<Capability>();

        // Assert
        capabilities.Should().Contain(Capability.DotnetCli, "per mode matrix");
        capabilities.Should().Contain(Capability.GitOperations, "per mode matrix");
        capabilities.Should().Contain(Capability.NpmYarn, "per mode matrix");
        capabilities.Should().Contain(Capability.ShellCommands, "per mode matrix");
    }

    [Fact]
    public void Capability_ShouldIncludeDataTransmissionCapabilities()
    {
        // Arrange & Act
        var capabilities = Enum.GetValues<Capability>();

        // Assert
        capabilities.Should().Contain(Capability.SendPrompts, "FR-001a-57");
        capabilities.Should().Contain(Capability.SendCodeSnippets, "per mode matrix");
        capabilities.Should().Contain(Capability.SendTelemetry, "FR-001a-41");
    }

    [Fact]
    public void Capability_ShouldHaveAtLeast20Values()
    {
        // Arrange & Act
        var capabilities = Enum.GetValues<Capability>();

        // Assert - Mode matrix defines 20+ distinct capabilities
        capabilities.Should().HaveCountGreaterOrEqualTo(
            20,
            "mode matrix requires comprehensive capability coverage");
    }
}
