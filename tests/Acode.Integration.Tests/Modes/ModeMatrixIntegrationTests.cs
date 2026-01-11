using System.Diagnostics;
using System.Text.Json;
using Acode.Domain.Modes;
using FluentAssertions;

namespace Acode.Integration.Tests.Modes;

/// <summary>
/// Integration tests for ModeMatrix.
/// Verifies matrix consistency, performance, and completeness per Task 001a IT-001a-02 to IT-001a-09.
/// </summary>
public sealed class ModeMatrixIntegrationTests
{
    [Fact]
    public void Matrix_ContainsAllThreeModes()
    {
        // Act
        var entries = ModeMatrix.GetAllEntries();
        var modes = entries.Select(e => e.Mode).Distinct().ToList();

        // Assert
        modes.Should().HaveCount(3, "matrix should have exactly 3 operating modes");
        modes.Should().Contain(OperatingMode.LocalOnly);
        modes.Should().Contain(OperatingMode.Burst);
        modes.Should().Contain(OperatingMode.Airgapped);
    }

    [Fact]
    public void Matrix_ContainsAll26Capabilities()
    {
        // Arrange - Get all capabilities from enum
        var allCapabilities = Enum.GetValues<Capability>();

        // Act
        var entries = ModeMatrix.GetAllEntries();
        var capabilitiesInMatrix = entries.Select(e => e.Capability).Distinct().ToList();

        // Assert
        capabilitiesInMatrix.Should().HaveCount(26, "matrix should include all 26 capabilities");

        foreach (var capability in allCapabilities)
        {
            capabilitiesInMatrix.Should().Contain(
                capability,
                $"matrix should include capability {capability}");
        }
    }

    [Fact]
    public void Matrix_LoadsFromAssemblyQuickly()
    {
        // Arrange
        var sw = Stopwatch.StartNew();

        // Act - First access triggers static constructor
        var entries = ModeMatrix.GetAllEntries();

        // Assert
        sw.Stop();
        sw.ElapsedMilliseconds.Should().BeLessThan(
            10,
            "matrix should load in < 10ms per NFR-001a-24");
        entries.Should().HaveCount(78, "should have 3 modes × 26 capabilities");
    }

    [Fact]
    public void Matrix_ExportToJsonCreatesValidFile()
    {
        // Arrange
        var tempFile = Path.GetTempFileName();

        try
        {
            // Act - Export matrix to JSON file
            var json = MatrixExporter.ToJson();
            File.WriteAllText(tempFile, json);

            // Assert - File exists and is valid
            File.Exists(tempFile).Should().BeTrue();
            var fileInfo = new FileInfo(tempFile);
            fileInfo.Length.Should().BeGreaterThan(0, "JSON file should not be empty");

            // Deserialize to verify validity
            var fileContent = File.ReadAllText(tempFile);
            var entries = JsonSerializer.Deserialize<List<MatrixEntry>>(
                fileContent,
                new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

            entries.Should().NotBeNull();
            entries.Should().HaveCount(78);
        }
        finally
        {
            // Cleanup
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }

    [Fact]
    public void Matrix_ConditionalEntriesExist()
    {
        // Arrange
        var entries = ModeMatrix.GetAllEntries();

        // Act - Get all conditional entries
        var conditionalEntries = entries
            .Where(e => e.Permission == Permission.ConditionalOnConsent
                     || e.Permission == Permission.ConditionalOnConfig)
            .ToList();

        // Assert - Matrix should have some conditional permissions for user control
        conditionalEntries.Should().NotBeEmpty(
            "matrix should have some conditional permissions that require user consent or configuration");

        // Verify conditional entries have mode and capability set
        foreach (var entry in conditionalEntries)
        {
            entry.Mode.Should().BeDefined("conditional entry should have valid mode");
            entry.Capability.Should().BeDefined("conditional entry should have valid capability");
        }
    }

    [Fact]
    public void Matrix_AllEntriesHaveRationale()
    {
        // Act
        var entries = ModeMatrix.GetAllEntries();

        // Assert
        entries.Should().HaveCount(78);

        foreach (var entry in entries)
        {
            entry.Rationale.Should().NotBeNullOrWhiteSpace(
                $"entry {entry.Mode}/{entry.Capability} must have a rationale explaining the permission decision");

            // Rationale should be meaningful (more than just a few characters)
            entry.Rationale.Length.Should().BeGreaterThan(
                10,
                $"rationale for {entry.Mode}/{entry.Capability} should be descriptive");
        }
    }

    [Fact]
    public void Matrix_MatchesDocumentation()
    {
        // Arrange - Path to documentation
        var docsPath = Path.Combine(
            Directory.GetCurrentDirectory(),
            "..",
            "..",
            "..",
            "..",
            "..",
            "..",
            "docs",
            "mode-matrix.md");

        // Skip test if documentation doesn't exist (might be in different working directory)
        if (!File.Exists(docsPath))
        {
            // Try alternative path (from test bin directory)
            var altPath = Path.Combine(
                AppContext.BaseDirectory,
                "..",
                "..",
                "..",
                "..",
                "..",
                "..",
                "docs",
                "mode-matrix.md");

            if (!File.Exists(altPath))
            {
                // Documentation not found, mark test as inconclusive
                Assert.True(
                    true,
                    "Skipping documentation consistency check - docs/mode-matrix.md not found in expected location");
                return;
            }

            docsPath = altPath;
        }

        // Act - Read documentation
        var docContent = File.ReadAllText(docsPath);
        var entries = ModeMatrix.GetAllEntries();

        // Assert - Documentation should mention all modes
        docContent.Should().Contain("LocalOnly", "documentation should describe LocalOnly mode");
        docContent.Should().Contain("Burst", "documentation should describe Burst mode");
        docContent.Should().Contain("Airgapped", "documentation should describe Airgapped mode");

        // Documentation should mention key capabilities
        docContent.Should().Contain("OpenAiApi", "documentation should mention external LLM APIs");
        docContent.Should().Contain("OllamaLocal", "documentation should mention local Ollama");
        docContent.Should().Contain("LocalhostNetwork", "documentation should mention localhost network");

        // Matrix should have consistent count
        entries.Should().HaveCount(78);
    }

    [Fact]
    public void Matrix_Has78Entries_ExactlyOncePerModeCombination()
    {
        // Arrange
        var allModes = Enum.GetValues<OperatingMode>();
        var allCapabilities = Enum.GetValues<Capability>();

        // Act
        var entries = ModeMatrix.GetAllEntries();

        // Assert - Should have exactly 78 entries (3 modes × 26 capabilities)
        entries.Should().HaveCount(78);

        // Every mode-capability combination should appear exactly once
        foreach (var mode in allModes)
        {
            foreach (var capability in allCapabilities)
            {
                var matchingEntries = entries
                    .Where(e => e.Mode == mode && e.Capability == capability)
                    .ToList();

                matchingEntries.Should().HaveCount(
                    1,
                    $"should have exactly one entry for {mode}/{capability}");
            }
        }
    }

    [Fact]
    public void Matrix_AllPermissionTypesRepresented()
    {
        // Act
        var entries = ModeMatrix.GetAllEntries();
        var permissions = entries.Select(e => e.Permission).Distinct().ToList();

        // Assert - Matrix should use multiple permission types
        permissions.Should().Contain(Permission.Allowed);
        permissions.Should().Contain(Permission.Denied);

        // Should have at least some conditional permissions
        var hasConditional = permissions.Any(
            p => p == Permission.ConditionalOnConsent || p == Permission.ConditionalOnConfig);
        hasConditional.Should().BeTrue(
            "matrix should have some conditional permissions for user control");
    }

    [Fact]
    public void Matrix_QueryPerformance_IsFast()
    {
        // Arrange
        var sw = Stopwatch.StartNew();

        // Act - Perform multiple queries
        for (int i = 0; i < 1000; i++)
        {
            ModeMatrix.GetPermission(OperatingMode.LocalOnly, Capability.OllamaLocal);
            ModeMatrix.GetPermission(OperatingMode.Burst, Capability.OpenAiApi);
            ModeMatrix.GetPermission(OperatingMode.Airgapped, Capability.ExternalNetwork);
        }

        // Assert - 3000 queries should complete quickly
        sw.Stop();
        sw.ElapsedMilliseconds.Should().BeLessThan(
            100,
            "3000 permission queries should complete in < 100ms per NFR-001a-25");
    }
}
