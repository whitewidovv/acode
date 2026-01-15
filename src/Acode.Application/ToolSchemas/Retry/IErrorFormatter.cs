namespace Acode.Application.ToolSchemas.Retry;

/// <summary>
/// Formats validation errors into model-comprehensible messages.
/// </summary>
/// <remarks>
/// Spec Reference: Implementation Prompt lines 3334-3355.
/// Implementations format errors for inclusion in ToolResult responses.
/// </remarks>
public interface IErrorFormatter
{
    /// <summary>
    /// Formats a collection of validation errors into a single message for the model.
    /// </summary>
    /// <param name="toolName">The name of the tool that failed validation.</param>
    /// <param name="errors">The validation errors to format.</param>
    /// <param name="attemptNumber">Current retry attempt number (1-based).</param>
    /// <param name="maxAttempts">Maximum allowed retry attempts.</param>
    /// <returns>Formatted error message ready for inclusion in ToolResult.</returns>
    string FormatErrors(string toolName, IEnumerable<ValidationError> errors, int attemptNumber, int maxAttempts);
}
