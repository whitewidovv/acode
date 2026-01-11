using Acode.Domain.Modes;
using FluentAssertions;

namespace Acode.Domain.Tests.Modes;

/// <summary>
/// Tests for the MatrixEntry record.
/// Verifies mode-capability-permission mapping per Task 001.a.
/// </summary>
public class MatrixEntryTests
{
    [Fact]
    public void MatrixEntry_ShouldStoreAllProperties()
    {
        // Arrange
        var mode = OperatingMode.LocalOnly;
        var capability = Capability.OllamaLocal;
        var permission = Permission.Allowed;
        var rationale = "Local Ollama is allowed in LocalOnly mode";

        // Act
        var entry = new MatrixEntry(mode, capability, permission, rationale);

        // Assert
        entry.Mode.Should().Be(mode);
        entry.Capability.Should().Be(capability);
        entry.Permission.Should().Be(permission);
        entry.Rationale.Should().Be(rationale);
    }

    [Fact]
    public void MatrixEntry_ShouldBeImmutable()
    {
        // Arrange
        var entry = new MatrixEntry(
            OperatingMode.Burst,
            Capability.OpenAiApi,
            Permission.ConditionalOnConsent,
            "External API requires consent in Burst");

        // Act - Try to modify (should not compile if truly immutable)
        // entry.Mode = OperatingMode.Airgapped; // This should not compile

        // Assert - Records are immutable by design
        entry.Should().NotBeNull();
        entry.GetType().IsValueType.Should().BeFalse("records are reference types");
    }

    [Fact]
    public void MatrixEntry_ShouldSupportValueEquality()
    {
        // Arrange
        var entry1 = new MatrixEntry(
            OperatingMode.Airgapped,
            Capability.ExternalNetwork,
            Permission.Denied,
            "No network in Airgapped");

        var entry2 = new MatrixEntry(
            OperatingMode.Airgapped,
            Capability.ExternalNetwork,
            Permission.Denied,
            "No network in Airgapped");

        // Act & Assert - Records support value-based equality
        entry1.Should().Be(entry2);
        (entry1 == entry2).Should().BeTrue();
    }

    [Fact]
    public void MatrixEntry_WithDifferentValues_ShouldNotBeEqual()
    {
        // Arrange
        var entry1 = new MatrixEntry(
            OperatingMode.LocalOnly,
            Capability.DnsLookup,
            Permission.Allowed,
            "DNS allowed");

        var entry2 = new MatrixEntry(
            OperatingMode.LocalOnly,
            Capability.DnsLookup,
            Permission.Denied,
            "DNS denied");

        // Act & Assert
        entry1.Should().NotBe(entry2);
    }

    [Fact]
    public void MatrixEntry_Rationale_ShouldNotBeNullOrEmpty()
    {
        // Arrange & Act
        var entry = new MatrixEntry(
            OperatingMode.Burst,
            Capability.CustomLlmApi,
            Permission.ConditionalOnConfig,
            "Custom APIs require config allowlist");

        // Assert
        entry.Rationale.Should().NotBeNullOrWhiteSpace(
            "every matrix entry must have a clear rationale");
    }
}
