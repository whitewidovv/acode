using System.Text;
using System.Text.Json;

namespace Acode.Domain.Modes;

/// <summary>
/// Exports the mode matrix to various formats for documentation and tooling.
/// </summary>
/// <remarks>
/// Per Task 001.a, provides serialization to JSON, Markdown tables, and CSV.
/// </remarks>
public static class MatrixExporter
{
    /// <summary>
    /// Export matrix to JSON format.
    /// </summary>
    /// <returns>JSON representation of all matrix entries.</returns>
    public static string ToJson()
    {
        var entries = ModeMatrix.GetAllEntries();
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
        return JsonSerializer.Serialize(entries, options);
    }

    /// <summary>
    /// Export matrix to Markdown table format.
    /// </summary>
    /// <param name="mode">Optional: filter by specific mode.</param>
    /// <returns>Markdown table of matrix entries.</returns>
    public static string ToMarkdownTable(OperatingMode? mode = null)
    {
        var entries = mode.HasValue
            ? ModeMatrix.GetEntriesForMode(mode.Value)
            : ModeMatrix.GetAllEntries();

        var sb = new StringBuilder();
        sb.AppendLine("| Mode | Capability | Permission | Rationale | Prerequisite |");
        sb.AppendLine("|------|------------|------------|-----------|--------------|");

        foreach (var entry in entries)
        {
            sb.AppendLine($"| {entry.Mode} | {entry.Capability} | {entry.Permission} | {entry.Rationale} | {entry.Prerequisite ?? "-"} |");
        }

        return sb.ToString();
    }

    /// <summary>
    /// Export matrix to CSV format for spreadsheet import.
    /// </summary>
    /// <returns>CSV representation of all matrix entries.</returns>
    public static string ToCsv()
    {
        var entries = ModeMatrix.GetAllEntries();
        var sb = new StringBuilder();

        // Header
        sb.AppendLine("Mode,Capability,Permission,Rationale,Prerequisite");

        // Data rows
        foreach (var entry in entries)
        {
            var prerequisite = entry.Prerequisite?.Replace("\"", "\"\"", StringComparison.Ordinal) ?? string.Empty;
            var rationale = entry.Rationale.Replace("\"", "\"\"", StringComparison.Ordinal);
            sb.AppendLine($"{entry.Mode},{entry.Capability},{entry.Permission},\"{rationale}\",\"{prerequisite}\"");
        }

        return sb.ToString();
    }

    /// <summary>
    /// Export capability comparison across all modes.
    /// </summary>
    /// <param name="capability">Capability to compare.</param>
    /// <returns>Markdown table showing how capability varies across modes.</returns>
    public static string ToCapabilityComparison(Capability capability)
    {
        var entries = ModeMatrix.GetEntriesForCapability(capability);

        var sb = new StringBuilder();
        sb.AppendLine($"## {capability} Across Modes");
        sb.AppendLine();
        sb.AppendLine("| Mode | Permission | Rationale |");
        sb.AppendLine("|------|------------|-----------|");

        foreach (var entry in entries.OrderBy(e => e.Mode))
        {
            sb.AppendLine($"| {entry.Mode} | {entry.Permission} | {entry.Rationale} |");
        }

        return sb.ToString();
    }
}
