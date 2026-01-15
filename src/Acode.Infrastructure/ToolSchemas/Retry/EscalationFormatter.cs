using System.Text;
using Acode.Application.ToolSchemas.Retry;

namespace Acode.Infrastructure.ToolSchemas.Retry;

/// <summary>
/// Formats escalation messages for human intervention after max retries exceeded.
/// </summary>
/// <remarks>
/// Spec Reference: Implementation Prompt lines 3957-4017.
/// Produces comprehensive reports with validation history and recommendations.
/// </remarks>
public sealed class EscalationFormatter : IEscalationFormatter
{
    /// <inheritdoc/>
    public string FormatEscalation(string toolName, string toolCallId, IReadOnlyList<string> validationHistory, int maxAttempts)
    {
        ArgumentException.ThrowIfNullOrEmpty(toolName);
        ArgumentException.ThrowIfNullOrEmpty(toolCallId);

        var sb = new StringBuilder();

        // Header
        sb.AppendLine("═══════════════════════════════════════════════════════════════");
        sb.AppendLine("                    ESCALATION REQUIRED                         ");
        sb.AppendLine("═══════════════════════════════════════════════════════════════");
        sb.AppendLine();
        sb.AppendLine($"Tool '{toolName}' validation failed after {maxAttempts} attempts.");
        sb.AppendLine($"Tool Call ID: {toolCallId}");
        sb.AppendLine();

        // Validation History
        sb.AppendLine("── Validation History ──────────────────────────────────────────");
        if (validationHistory is null || validationHistory.Count == 0)
        {
            sb.AppendLine("  No validation history recorded.");
        }
        else
        {
            for (int i = 0; i < validationHistory.Count; i++)
            {
                sb.AppendLine($"  Attempt {i + 1}:");

                // Indent each line of the error message
                foreach (var line in validationHistory[i].Split('\n'))
                {
                    if (!string.IsNullOrWhiteSpace(line))
                    {
                        sb.AppendLine($"    {line.TrimEnd()}");
                    }
                }

                sb.AppendLine();
            }
        }

        // Analysis Section
        sb.AppendLine("── Analysis ────────────────────────────────────────────────────");
        var totalAttempts = validationHistory?.Count ?? 0;
        sb.AppendLine($"  Total validation attempts: {totalAttempts}");
        sb.AppendLine($"  All attempts failed to produce valid input.");
        sb.AppendLine();

        // Recommendations
        sb.AppendLine("── Recommended Actions ─────────────────────────────────────────");
        sb.AppendLine("  1. Review the tool schema documentation for correct parameter types");
        sb.AppendLine("  2. Check if required parameters are being provided");
        sb.AppendLine("  3. Verify parameter values match expected formats and constraints");
        sb.AppendLine("  4. Consider simplifying the request if possible");
        sb.AppendLine("  5. Report this issue if the schema appears incorrect");
        sb.AppendLine();

        // Escalation prompt
        sb.AppendLine("═══════════════════════════════════════════════════════════════");
        sb.AppendLine("Human intervention required to resolve this validation failure.");
        sb.AppendLine("═══════════════════════════════════════════════════════════════");

        return sb.ToString();
    }
}
