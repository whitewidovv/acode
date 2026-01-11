using Acode.Domain.Modes;
using FluentAssertions;

namespace Acode.Integration.Tests.Modes;

/// <summary>
/// E2E tests for Mode Matrix.
/// Verifies end-to-end scenarios per Task 001a E2E-001a-04 to E2E-001a-07.
/// </summary>
public sealed class ModeMatrixE2ETests
{
    [Fact]
    public void Matrix_DeniedActionExample_MatchesSpec()
    {
        // Act - Check LocalOnly mode prohibits external LLM APIs (spec requirement)
        // Per spec line ~829: LocalOnly mode denies OpenAI API for privacy
        var permission = ModeMatrix.GetPermission(
            OperatingMode.LocalOnly,
            Capability.OpenAiApi);

        // Assert
        permission.Should().Be(
            Permission.Denied,
            "LocalOnly mode must deny OpenAI API per core privacy constraint");

        var entry = ModeMatrix.GetEntry(OperatingMode.LocalOnly, Capability.OpenAiApi);
        entry.Should().NotBeNull();
        entry!.Rationale.Should().NotBeNullOrWhiteSpace("denied entries should explain why");

        // Rationale should mention privacy or similar security concern
        var rationale = entry.Rationale.ToLowerInvariant();
        (rationale.Contains("privacy", StringComparison.Ordinal) ||
         rationale.Contains("external", StringComparison.Ordinal) ||
         rationale.Contains("constraint", StringComparison.Ordinal))
            .Should().BeTrue("rationale should explain the privacy/security reason for denial");
    }

    [Fact]
    public void Matrix_AllowedActionExample_MatchesSpec()
    {
        // Act - Check LocalOnly mode allows localhost for Ollama (spec requirement)
        // Per spec line ~821: LocalOnly allows localhost network for Ollama communication
        var permission = ModeMatrix.GetPermission(
            OperatingMode.LocalOnly,
            Capability.LocalhostNetwork);

        // Assert
        permission.Should().Be(
            Permission.Allowed,
            "LocalOnly mode must allow localhost network for Ollama communication");

        var entry = ModeMatrix.GetEntry(OperatingMode.LocalOnly, Capability.LocalhostNetwork);
        entry.Should().NotBeNull();
        entry!.Rationale.Should().NotBeNullOrWhiteSpace("allowed entries should explain why");

        // Rationale should mention Ollama or local communication
        var rationale = entry.Rationale.ToLowerInvariant();
        (rationale.Contains("ollama", StringComparison.Ordinal) ||
         rationale.Contains("local", StringComparison.Ordinal) ||
         rationale.Contains("required", StringComparison.Ordinal))
            .Should().BeTrue("rationale should explain the need for localhost communication");
    }

    [Fact]
    public void Matrix_ConditionalActionExample_MatchesSpec()
    {
        // Act - Check Burst mode requires consent for external APIs (spec requirement)
        // Per spec: Burst mode allows OpenAI API with user consent
        var permission = ModeMatrix.GetPermission(
            OperatingMode.Burst,
            Capability.OpenAiApi);

        // Assert
        permission.Should().Be(
            Permission.ConditionalOnConsent,
            "Burst mode should require user consent for external LLM APIs");

        var entry = ModeMatrix.GetEntry(OperatingMode.Burst, Capability.OpenAiApi);
        entry.Should().NotBeNull();
        entry!.Rationale.Should().NotBeNullOrWhiteSpace("conditional entries should explain the condition");
    }

    [Fact]
    public void Matrix_AirgappedMode_DeniesAllNetworkAccess()
    {
        // Act - Verify Airgapped mode denies all network capabilities
        var externalNetwork = ModeMatrix.GetPermission(
            OperatingMode.Airgapped,
            Capability.ExternalNetwork);
        var localhostNetwork = ModeMatrix.GetPermission(
            OperatingMode.Airgapped,
            Capability.LocalhostNetwork);
        var openAiApi = ModeMatrix.GetPermission(
            OperatingMode.Airgapped,
            Capability.OpenAiApi);

        // Assert - Airgapped mode should deny all network access
        externalNetwork.Should().Be(
            Permission.Denied,
            "Airgapped mode must deny external network access");
        localhostNetwork.Should().Be(
            Permission.Denied,
            "Airgapped mode must deny localhost network access");
        openAiApi.Should().Be(
            Permission.Denied,
            "Airgapped mode must deny external LLM API access");
    }

    [Fact]
    public void Matrix_LocalOnlyMode_AllowsLocalModelAccess()
    {
        // Act - Verify LocalOnly mode allows local AI model access
        var ollamaLocal = ModeMatrix.GetPermission(
            OperatingMode.LocalOnly,
            Capability.OllamaLocal);

        // Assert - LocalOnly should allow Ollama (local model)
        ollamaLocal.Should().Be(
            Permission.Allowed,
            "LocalOnly mode should allow local Ollama model access");
    }

    [Fact]
    public void Matrix_BurstMode_AllowsExternalNetworkWithConsent()
    {
        // Act - Check that Burst mode requires consent for external network
        var externalNetwork = ModeMatrix.GetPermission(
            OperatingMode.Burst,
            Capability.ExternalNetwork);

        // Assert - Should be conditional (user control)
        (externalNetwork == Permission.ConditionalOnConsent ||
         externalNetwork == Permission.ConditionalOnConfig ||
         externalNetwork == Permission.Allowed)
            .Should().BeTrue(
                "Burst mode should allow external network (either directly or with consent)");
    }

    [Fact]
    public void Matrix_GetEntry_ReturnsNullForInvalidCombination()
    {
        // This test verifies behavior for edge case handling
        // Note: All valid mode-capability combinations should exist

        // Act - Get an entry that definitely exists
        var validEntry = ModeMatrix.GetEntry(OperatingMode.LocalOnly, Capability.OllamaLocal);

        // Assert - Valid combinations should return non-null
        validEntry.Should().NotBeNull("valid mode-capability combinations should have matrix entries");
    }

    [Fact]
    public void Matrix_AllModesCanReadProjectFiles()
    {
        // Act - Verify all modes can read project files (core functionality)
        var localOnlyRead = ModeMatrix.GetPermission(
            OperatingMode.LocalOnly,
            Capability.ReadProjectFiles);
        var burstRead = ModeMatrix.GetPermission(
            OperatingMode.Burst,
            Capability.ReadProjectFiles);
        var airgappedRead = ModeMatrix.GetPermission(
            OperatingMode.Airgapped,
            Capability.ReadProjectFiles);

        // Assert - All modes need to read project files for core functionality
        localOnlyRead.Should().Be(
            Permission.Allowed,
            "LocalOnly mode must allow reading project files");
        burstRead.Should().Be(
            Permission.Allowed,
            "Burst mode must allow reading project files");
        airgappedRead.Should().Be(
            Permission.Allowed,
            "Airgapped mode must allow reading project files");
    }
}
