namespace Acode.Application.ToolSchemas.Retry;

/// <summary>
/// Formats escalation messages for human intervention after max retries exceeded.
/// </summary>
/// <remarks>
/// Spec Reference: Implementation Prompt lines 3414-3435.
/// Produces comprehensive escalation reports with validation history and recommendations.
/// </remarks>
public interface IEscalationFormatter
{
    /// <summary>
    /// Formats an escalation message with validation history.
    /// </summary>
    /// <param name="toolName">The name of the tool that failed validation.</param>
    /// <param name="toolCallId">Unique identifier for the tool call.</param>
    /// <param name="validationHistory">List of error messages from all attempts.</param>
    /// <param name="maxAttempts">Maximum allowed retry attempts.</param>
    /// <returns>Formatted escalation message for human operator.</returns>
    string FormatEscalation(string toolName, string toolCallId, IReadOnlyList<string> validationHistory, int maxAttempts);
}
