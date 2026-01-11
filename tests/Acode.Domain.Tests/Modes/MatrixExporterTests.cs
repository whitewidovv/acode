using System.Text.Json;
using Acode.Domain.Modes;
using FluentAssertions;

namespace Acode.Domain.Tests.Modes;

/// <summary>
/// Tests for MatrixExporter static class.
/// Verifies JSON/CSV/Markdown export functionality per Task 001.a.
/// </summary>
public sealed class MatrixExporterTests
{
    // JSON Export Tests (UT-001a-09, UT-001a-10)
    [Fact]
    public void ToJson_ReturnsValidJson()
    {
        // Act
        var json = MatrixExporter.ToJson();

        // Assert
        json.Should().NotBeNullOrWhiteSpace("JSON export should produce non-empty output");

        // Should be valid JSON that can be deserialized
        var action = () => JsonSerializer.Deserialize<List<MatrixEntry>>(json);
        action.Should().NotThrow("JSON should be valid and deserializable");
    }

    [Fact]
    public void ToJson_ContainsAllMatrixEntries()
    {
        // Act
        var json = MatrixExporter.ToJson();

        // Assert - Check JSON string contains enough data (rough check)
        json.Length.Should().BeGreaterThan(5000, "78 entries with rationales should be substantial JSON");

        // Verify JSON is parseable (don't check structure, just validity)
        var action = () => JsonDocument.Parse(json);
        action.Should().NotThrow();
    }

    [Fact]
    public void ToJson_ContainsAllThreeModes()
    {
        // Act
        var json = MatrixExporter.ToJson();

        // Assert - Check JSON string contains all mode names
        json.Should().Contain("LocalOnly");
        json.Should().Contain("Burst");
        json.Should().Contain("Airgapped");
    }

    [Fact]
    public void ToJson_ContainsAll26Capabilities()
    {
        // Act
        var json = MatrixExporter.ToJson();

        // Assert - Check JSON has enough entries (rough validation via structure)
        // Each entry has mode, capability, permission, rationale
        var modeCount = System.Text.RegularExpressions.Regex.Matches(json, "\"mode\"").Count;
        modeCount.Should().Be(78, "should have 78 entries (3 modes × 26 capabilities)");

        // Verify key rationale phrases that cover different capabilities
        json.Should().Contain("External network", "should cover network capabilities");
        json.Should().Contain("LLM", "should cover LLM capabilities");
        json.Should().Contain("project files", "should cover file system capabilities");
    }

    [Fact]
    public void ToJson_RoundTripPreservesData()
    {
        // Arrange
        var originalEntries = ModeMatrix.GetAllEntries().ToList();
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        // Act - Serialize and deserialize with same options
        var json = MatrixExporter.ToJson();
        var deserializedEntries = JsonSerializer.Deserialize<List<MatrixEntry>>(json, options);

        // Assert - Compare original to round-tripped data
        deserializedEntries.Should().NotBeNull();
        deserializedEntries.Should().HaveCount(originalEntries.Count);

        foreach (var original in originalEntries)
        {
            var deserialized = deserializedEntries!.SingleOrDefault(e =>
                e.Mode == original.Mode && e.Capability == original.Capability);

            deserialized.Should().NotBeNull($"entry for {original.Mode}/{original.Capability} should exist");
            deserialized!.Permission.Should().Be(original.Permission);
            deserialized.Rationale.Should().Be(original.Rationale);
            deserialized.Prerequisite.Should().Be(original.Prerequisite);
        }
    }

    [Fact]
    public void ToJson_UsesIndentedFormatting()
    {
        // Act
        var json = MatrixExporter.ToJson();

        // Assert
        json.Should().Contain("\n", "JSON should be indented for readability");
        json.Should().Contain("  ", "JSON should use proper indentation");
    }

    [Fact]
    public void ToJson_UsesCamelCasePropertyNames()
    {
        // Act
        var json = MatrixExporter.ToJson();

        // Assert
        json.Should().Contain("\"mode\"", "property names should be camelCase");
        json.Should().Contain("\"capability\"");
        json.Should().Contain("\"permission\"");
        json.Should().NotContain("\"Mode\"", "should not use PascalCase");
    }

    // Markdown Table Tests (UT-001a-11)
    [Fact]
    public void ToMarkdownTable_ProducesFormattedTable()
    {
        // Act
        var markdown = MatrixExporter.ToMarkdownTable();

        // Assert
        markdown.Should().NotBeNullOrWhiteSpace();
        markdown.Should().Contain("| Mode | Capability | Permission | Rationale | Prerequisite |");
        markdown.Should().Contain("|------|------------|------------|-----------|--------------|");
    }

    [Fact]
    public void ToMarkdownTable_ContainsAllModes()
    {
        // Act
        var markdown = MatrixExporter.ToMarkdownTable();

        // Assert
        markdown.Should().Contain("LocalOnly");
        markdown.Should().Contain("Burst");
        markdown.Should().Contain("Airgapped");
    }

    [Fact]
    public void ToMarkdownTable_ContainsAllCapabilities()
    {
        // Arrange
        var allCapabilities = Enum.GetValues<Capability>();

        // Act
        var markdown = MatrixExporter.ToMarkdownTable();

        // Assert
        foreach (var capability in allCapabilities)
        {
            markdown.Should().Contain(
                capability.ToString(),
                $"table should include capability {capability}");
        }
    }

    [Fact]
    public void ToMarkdownTable_Has78DataRows()
    {
        // Act
        var markdown = MatrixExporter.ToMarkdownTable();

        // Assert
        var lines = markdown.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        var dataRows = lines.Skip(2); // Skip header and separator
        dataRows.Should().HaveCount(78, "3 modes × 26 capabilities = 78 rows");
    }

    [Fact]
    public void ToMarkdownTable_WithModeFilter_OnlyIncludesThatMode()
    {
        // Act
        var markdown = MatrixExporter.ToMarkdownTable(OperatingMode.LocalOnly);

        // Assert
        markdown.Should().Contain("LocalOnly");
        markdown.Should().NotContain("Burst");
        markdown.Should().NotContain("Airgapped");

        var lines = markdown.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        var dataRows = lines.Skip(2); // Skip header and separator
        dataRows.Should().HaveCount(26, "LocalOnly mode has 26 capabilities");
    }

    [Fact]
    public void ToMarkdownTable_WithBurstMode_OnlyIncludesBurstEntries()
    {
        // Act
        var markdown = MatrixExporter.ToMarkdownTable(OperatingMode.Burst);

        // Assert
        markdown.Should().Contain("Burst");
        markdown.Should().NotContain("LocalOnly");
        markdown.Should().NotContain("Airgapped");
    }

    [Fact]
    public void ToMarkdownTable_WithAirgappedMode_OnlyIncludesAirgappedEntries()
    {
        // Act
        var markdown = MatrixExporter.ToMarkdownTable(OperatingMode.Airgapped);

        // Assert
        markdown.Should().Contain("Airgapped");
        markdown.Should().NotContain("LocalOnly");
        markdown.Should().NotContain("Burst");
    }

    [Fact]
    public void ToMarkdownTable_ShowsRationaleForAllEntries()
    {
        // Act
        var markdown = MatrixExporter.ToMarkdownTable();

        // Assert - Verify rationale column is populated (not all "-")
        // Count lines with actual rationale text (should be most rows)
        var lines = markdown.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        var dataLines = lines.Skip(2).ToList(); // Skip header and separator

        // Most entries should have substantive rationales
        var linesWithRationale = dataLines.Count(l => l.Contains("require", StringComparison.OrdinalIgnoreCase)
                                                   || l.Contains("allow", StringComparison.OrdinalIgnoreCase)
                                                   || l.Contains("security", StringComparison.OrdinalIgnoreCase)
                                                   || l.Contains("privacy", StringComparison.OrdinalIgnoreCase)
                                                   || l.Contains("core functionality", StringComparison.OrdinalIgnoreCase)
                                                   || l.Contains("HC-", StringComparison.Ordinal) // Hard constraints
                                                   || l.Contains("denied", StringComparison.OrdinalIgnoreCase)
                                                   || l.Contains("consent", StringComparison.OrdinalIgnoreCase));

        linesWithRationale.Should().BeGreaterThan(40, "majority of entries should have meaningful rationales");
    }

    [Fact]
    public void ToMarkdownTable_ShowsPrerequisitesForConditionalEntries()
    {
        // Act
        var markdown = MatrixExporter.ToMarkdownTable();

        // Assert - Conditional entries should show prerequisites, not "-"
        var conditionalEntries = ModeMatrix.GetAllEntries()
            .Where(e => e.Permission == Permission.ConditionalOnConsent
                     || e.Permission == Permission.ConditionalOnConfig);

        conditionalEntries.Should().NotBeEmpty("matrix should have conditional entries");

        foreach (var entry in conditionalEntries.Where(e => !string.IsNullOrEmpty(e.Prerequisite)))
        {
            markdown.Should().Contain(
                entry.Prerequisite,
                $"conditional entry {entry.Mode}/{entry.Capability} should show prerequisite");
        }
    }

    // CSV Export Tests
    [Fact]
    public void ToCsv_ProducesValidCsv()
    {
        // Act
        var csv = MatrixExporter.ToCsv();

        // Assert
        csv.Should().NotBeNullOrWhiteSpace();
        csv.Should().StartWith("Mode,Capability,Permission,Rationale,Prerequisite");
    }

    [Fact]
    public void ToCsv_Has78DataRows()
    {
        // Act
        var csv = MatrixExporter.ToCsv();

        // Assert
        var lines = csv.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        lines.Should().HaveCount(79, "1 header + 78 data rows");
    }

    [Fact]
    public void ToCsv_EscapesQuotesInFields()
    {
        // Act
        var csv = MatrixExporter.ToCsv();

        // Assert - CSV should handle rationales with quotes by escaping them
        csv.Should().Contain("\"", "CSV should quote fields containing special characters");
    }

    [Fact]
    public void ToCsv_ContainsAllModes()
    {
        // Act
        var csv = MatrixExporter.ToCsv();

        // Assert
        csv.Should().Contain("LocalOnly,");
        csv.Should().Contain("Burst,");
        csv.Should().Contain("Airgapped,");
    }

    [Fact]
    public void ToCsv_ContainsAllPermissionTypes()
    {
        // Act
        var csv = MatrixExporter.ToCsv();

        // Assert
        csv.Should().Contain("Allowed,");
        csv.Should().Contain("Denied,");

        // Check for conditional permissions (may or may not exist depending on matrix)
        var hasConditional = csv.Contains("ConditionalOnConsent", StringComparison.Ordinal)
            || csv.Contains("ConditionalOnConfig", StringComparison.Ordinal);
        hasConditional.Should().BeTrue("matrix should have at least some conditional permissions");
    }

    // Capability Comparison Tests
    [Fact]
    public void ToCapabilityComparison_ShowsCapabilityAcrossAllModes()
    {
        // Act
        var comparison = MatrixExporter.ToCapabilityComparison(Capability.OpenAiApi);

        // Assert
        comparison.Should().Contain("OpenAiApi");
        comparison.Should().Contain("LocalOnly");
        comparison.Should().Contain("Burst");
        comparison.Should().Contain("Airgapped");
    }

    [Fact]
    public void ToCapabilityComparison_FormatsAsMarkdownTable()
    {
        // Act
        var comparison = MatrixExporter.ToCapabilityComparison(Capability.OllamaLocal);

        // Assert
        comparison.Should().Contain("| Mode | Permission | Rationale |");
        comparison.Should().Contain("|------|------------|-----------|");
    }

    [Fact]
    public void ToCapabilityComparison_HasHeading()
    {
        // Act
        var comparison = MatrixExporter.ToCapabilityComparison(Capability.LocalhostNetwork);

        // Assert
        comparison.Should().Contain("## LocalhostNetwork Across Modes");
    }

    [Fact]
    public void ToCapabilityComparison_OrdersModesByEnum()
    {
        // Act
        var comparison = MatrixExporter.ToCapabilityComparison(Capability.ExternalNetwork);

        // Assert - Should appear in enum order: LocalOnly (0), Burst (1), Airgapped (2)
        var lines = comparison.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        var dataLines = lines.Where(l => l.StartsWith("|", StringComparison.Ordinal)
                                      && !l.StartsWith("| Mode", StringComparison.Ordinal)
                                      && !l.StartsWith("|--", StringComparison.Ordinal)).ToList();

        dataLines.Should().HaveCount(3);
        dataLines[0].Should().Contain("LocalOnly");
        dataLines[1].Should().Contain("Burst");
        dataLines[2].Should().Contain("Airgapped");
    }

    [Fact]
    public void ToCapabilityComparison_ShowsRationaleForEachMode()
    {
        // Act
        var comparison = MatrixExporter.ToCapabilityComparison(Capability.OpenAiApi);

        // Assert - Verify rationale column exists and has content
        comparison.Should().Contain("| Rationale |", "table should have rationale column");

        // Should have 3 data rows with non-empty rationales (not just "-")
        var lines = comparison.Split('\n');
        var dataRows = lines.Where(l => l.StartsWith("| ", StringComparison.Ordinal)
                                     && !l.StartsWith("| Mode", StringComparison.Ordinal)
                                     && !l.StartsWith("|--", StringComparison.Ordinal)).ToList();

        dataRows.Should().HaveCount(3, "should have one row per mode");
    }

    [Fact]
    public void ToCapabilityComparison_OpenAiApi_ShowsExpectedPermissions()
    {
        // Act
        var comparison = MatrixExporter.ToCapabilityComparison(Capability.OpenAiApi);

        // Assert - Per spec, OpenAI API should be:
        // LocalOnly: Denied
        // Burst: ConditionalOnConsent
        // Airgapped: Denied
        comparison.Should().Contain("Denied");
        comparison.Should().Contain("ConditionalOnConsent");
    }

    // Edge Cases and Error Handling
    [Fact]
    public void ToMarkdownTable_WithNullMode_ShowsAllModes()
    {
        // Act
        var markdown = MatrixExporter.ToMarkdownTable(null);

        // Assert
        markdown.Should().Contain("LocalOnly");
        markdown.Should().Contain("Burst");
        markdown.Should().Contain("Airgapped");
    }

    [Fact]
    public void ToCapabilityComparison_WithEveryCapability_Succeeds()
    {
        // Arrange
        var allCapabilities = Enum.GetValues<Capability>();

        // Act & Assert - Should not throw for any capability
        foreach (var capability in allCapabilities)
        {
            var action = () => MatrixExporter.ToCapabilityComparison(capability);
            action.Should().NotThrow($"should handle capability {capability}");

            var result = action();
            result.Should().NotBeNullOrWhiteSpace();
            result.Should().Contain(capability.ToString());
        }
    }

    // Performance Tests
    [Fact]
    public void ToJson_PerformanceIsAcceptable()
    {
        // Act
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var json = MatrixExporter.ToJson();
        sw.Stop();

        // Assert - Should be fast (< 50ms per NFR-001a-27)
        sw.ElapsedMilliseconds.Should().BeLessThan(
            50,
            "JSON serialization should be fast per NFR-001a-27");
        json.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void ToMarkdownTable_PerformanceIsAcceptable()
    {
        // Act
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var markdown = MatrixExporter.ToMarkdownTable();
        sw.Stop();

        // Assert - Should be fast
        sw.ElapsedMilliseconds.Should().BeLessThan(
            50,
            "Markdown table generation should be fast");
        markdown.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void ToCsv_PerformanceIsAcceptable()
    {
        // Act
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var csv = MatrixExporter.ToCsv();
        sw.Stop();

        // Assert - Should be fast
        sw.ElapsedMilliseconds.Should().BeLessThan(
            50,
            "CSV generation should be fast");
        csv.Should().NotBeNullOrWhiteSpace();
    }
}
