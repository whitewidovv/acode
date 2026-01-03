using Acode.Domain.Modes;
using FluentAssertions;
using Xunit;

namespace Acode.Domain.Tests.Modes;

/// <summary>
/// Tests for the ModeMatrix static class.
/// Verifies the complete mode-capability permission matrix per Task 001.a.
/// </summary>
public class ModeMatrixTests
{
    // Core matrix structure tests
    [Fact]
    public void ModeMatrix_ShouldDefineEntriesForAllModesAndCapabilities()
    {
        // Arrange
        var modes = Enum.GetValues<OperatingMode>();
        var capabilities = Enum.GetValues<Capability>();
        var expectedEntries = modes.Length * capabilities.Length;

        // Act
        var entries = ModeMatrix.GetAllEntries();

        // Assert
        entries.Should().HaveCount(
            expectedEntries,
            "matrix must define permission for every mode-capability combination");
    }

    [Fact]
    public void ModeMatrix_GetPermission_ShouldReturnCorrectValue()
    {
        // Arrange
        var mode = OperatingMode.LocalOnly;
        var capability = Capability.OllamaLocal;

        // Act
        var permission = ModeMatrix.GetPermission(mode, capability);

        // Assert
        permission.Should().Be(
            Permission.Allowed,
            "Ollama local is allowed in LocalOnly mode per FR-001a-32");
    }

    [Fact]
    public void ModeMatrix_GetEntry_ShouldReturnCompleteEntry()
    {
        // Arrange
        var mode = OperatingMode.Airgapped;
        var capability = Capability.ExternalNetwork;

        // Act
        var entry = ModeMatrix.GetEntry(mode, capability);

        // Assert
        entry.Should().NotBeNull();
        entry.Mode.Should().Be(mode);
        entry.Capability.Should().Be(capability);
        entry.Permission.Should().Be(
            Permission.Denied,
            "external network is denied in Airgapped mode per HC-02");
        entry.Rationale.Should().NotBeNullOrWhiteSpace();
    }

    // Specific hard constraint tests (HC-01, HC-02, HC-03)
    [Fact]
    public void ModeMatrix_HC01_LocalOnly_ShouldDenyExternalLlmApis()
    {
        // HC-01: No external LLM API in LocalOnly mode

        // Assert
        ModeMatrix.GetPermission(OperatingMode.LocalOnly, Capability.OpenAiApi)
            .Should().Be(Permission.Denied, "HC-01");
        ModeMatrix.GetPermission(OperatingMode.LocalOnly, Capability.AnthropicApi)
            .Should().Be(Permission.Denied, "HC-01");
        ModeMatrix.GetPermission(OperatingMode.LocalOnly, Capability.AzureOpenAiApi)
            .Should().Be(Permission.Denied, "HC-01");
        ModeMatrix.GetPermission(OperatingMode.LocalOnly, Capability.CustomLlmApi)
            .Should().Be(Permission.Denied, "HC-01");
    }

    [Fact]
    public void ModeMatrix_HC02_Airgapped_ShouldDenyAllNetwork()
    {
        // HC-02: No network access in Airgapped mode

        // Assert
        ModeMatrix.GetPermission(OperatingMode.Airgapped, Capability.LocalhostNetwork)
            .Should().Be(Permission.Denied, "HC-02: Airgapped blocks all network");
        ModeMatrix.GetPermission(OperatingMode.Airgapped, Capability.LocalAreaNetwork)
            .Should().Be(Permission.Denied, "HC-02");
        ModeMatrix.GetPermission(OperatingMode.Airgapped, Capability.ExternalNetwork)
            .Should().Be(Permission.Denied, "HC-02");
        ModeMatrix.GetPermission(OperatingMode.Airgapped, Capability.DnsLookup)
            .Should().Be(Permission.Denied, "HC-02");
    }

    [Fact]
    public void ModeMatrix_HC03_Burst_ShouldRequireConsentForExternalApis()
    {
        // HC-03: Consent required for Burst mode external APIs

        // Assert
        ModeMatrix.GetPermission(OperatingMode.Burst, Capability.OpenAiApi)
            .Should().Be(Permission.ConditionalOnConsent, "HC-03");
        ModeMatrix.GetPermission(OperatingMode.Burst, Capability.AnthropicApi)
            .Should().Be(Permission.ConditionalOnConsent, "HC-03");
        ModeMatrix.GetPermission(OperatingMode.Burst, Capability.SendPrompts)
            .Should().Be(Permission.ConditionalOnConsent, "HC-03");
    }

    // LocalOnly mode tests
    [Fact]
    public void ModeMatrix_LocalOnly_ShouldAllowLocalOperations()
    {
        // Assert - Local operations are allowed
        ModeMatrix.GetPermission(OperatingMode.LocalOnly, Capability.ReadProjectFiles)
            .Should().Be(Permission.Allowed);
        ModeMatrix.GetPermission(OperatingMode.LocalOnly, Capability.WriteProjectFiles)
            .Should().Be(Permission.Allowed);
        ModeMatrix.GetPermission(OperatingMode.LocalOnly, Capability.DotnetCli)
            .Should().Be(Permission.Allowed);
        ModeMatrix.GetPermission(OperatingMode.LocalOnly, Capability.GitOperations)
            .Should().Be(Permission.Allowed);
    }

    [Fact]
    public void ModeMatrix_LocalOnly_ShouldAllowLocalhostForOllama()
    {
        // Assert - Localhost allowed for Ollama access
        ModeMatrix.GetPermission(OperatingMode.LocalOnly, Capability.LocalhostNetwork)
            .Should().Be(Permission.Allowed, "required for Ollama");
        ModeMatrix.GetPermission(OperatingMode.LocalOnly, Capability.OllamaLocal)
            .Should().Be(Permission.Allowed);
    }

    // Burst mode tests
    [Fact]
    public void ModeMatrix_Burst_ShouldAllowNetworkAccess()
    {
        // Assert - Burst allows network for compute
        ModeMatrix.GetPermission(OperatingMode.Burst, Capability.LocalhostNetwork)
            .Should().Be(Permission.Allowed);
        ModeMatrix.GetPermission(OperatingMode.Burst, Capability.ExternalNetwork)
            .Should().Be(Permission.Allowed);
        ModeMatrix.GetPermission(OperatingMode.Burst, Capability.DnsLookup)
            .Should().Be(Permission.Allowed);
    }

    // Performance test
    [Fact]
    public void ModeMatrix_GetPermission_ShouldBeFast()
    {
        // Arrange
        var mode = OperatingMode.LocalOnly;
        var capability = Capability.ReadProjectFiles;
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        // Act - Run 10,000 lookups
        for (int i = 0; i < 10000; i++)
        {
            _ = ModeMatrix.GetPermission(mode, capability);
        }

        stopwatch.Stop();

        // Assert - Should be < 50ms total (< 5μs per lookup)
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(
            50,
            "matrix lookups must be fast (FrozenDictionary, includes JIT warmup)");
    }

    // Validation tests
    [Fact]
    public void ModeMatrix_AllEntries_ShouldHaveNonEmptyRationale()
    {
        // Arrange
        var entries = ModeMatrix.GetAllEntries();

        // Assert
        entries.Should().AllSatisfy(entry =>
        {
            entry.Rationale.Should().NotBeNullOrWhiteSpace(
                "every matrix entry must explain its permission");
        });
    }

    [Fact]
    public void ModeMatrix_GetEntriesForMode_ShouldReturnAllCapabilities()
    {
        // Arrange
        var mode = OperatingMode.Burst;
        var capabilityCount = Enum.GetValues<Capability>().Length;

        // Act
        var entries = ModeMatrix.GetEntriesForMode(mode);

        // Assert
        entries.Should().HaveCount(
            capabilityCount,
            "mode must have entry for every capability");
    }
}
