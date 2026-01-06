namespace Acode.Application.Tools.Retry;

using Acode.Domain.Tools;

/// <summary>
/// Interface for formatting validation errors into model-comprehensible messages.
/// </summary>
/// <remarks>
/// FR-007b: Validation error retry contract.
/// FR-018 to FR-025: Error formatting requirements.
/// </remarks>
public interface IValidationErrorFormatter
{
    /// <summary>
    /// Formats validation errors into a message for model retry.
    /// </summary>
    /// <param name="toolName">Name of the tool that failed validation.</param>
    /// <param name="errors">Collection of validation errors.</param>
    /// <param name="attemptNumber">Current retry attempt number (1-based).</param>
    /// <param name="maxAttempts">Maximum number of allowed attempts.</param>
    /// <returns>Formatted error message suitable for model comprehension.</returns>
    string FormatErrors(
        string toolName,
        IReadOnlyCollection<SchemaValidationError> errors,
        int attemptNumber,
        int maxAttempts);

    /// <summary>
    /// Formats an escalation message after max retries exceeded.
    /// </summary>
    /// <param name="toolName">Name of the tool that failed validation.</param>
    /// <param name="history">History of validation attempts with their errors.</param>
    /// <returns>Formatted escalation message for user intervention.</returns>
    string FormatEscalation(
        string toolName,
        IReadOnlyList<ValidationAttempt> history);
}

/// <summary>
/// Represents a single validation attempt with its errors.
/// </summary>
/// <param name="AttemptNumber">The attempt number (1-based).</param>
/// <param name="Errors">The validation errors for this attempt.</param>
/// <param name="Timestamp">When the attempt occurred.</param>
public sealed record ValidationAttempt(
    int AttemptNumber,
    IReadOnlyCollection<SchemaValidationError> Errors,
    DateTimeOffset Timestamp);
