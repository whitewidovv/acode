namespace Acode.Application.Tools.Retry;

using Acode.Domain.Tools;

/// <summary>
/// Interface for tracking validation retry attempts.
/// </summary>
/// <remarks>
/// FR-007b: Validation error retry contract.
/// FR-036 to FR-040: Retry tracking requirements.
/// </remarks>
public interface IRetryTracker
{
    /// <summary>
    /// Records a validation attempt for a tool call.
    /// </summary>
    /// <param name="toolCallId">The unique identifier of the tool call.</param>
    /// <param name="errors">The validation errors from this attempt.</param>
    void RecordAttempt(string toolCallId, IReadOnlyCollection<SchemaValidationError> errors);

    /// <summary>
    /// Gets the current attempt number for a tool call.
    /// </summary>
    /// <param name="toolCallId">The unique identifier of the tool call.</param>
    /// <returns>The attempt number (1-based), or 0 if no attempts recorded.</returns>
    int GetAttemptCount(string toolCallId);

    /// <summary>
    /// Gets the validation history for a tool call.
    /// </summary>
    /// <param name="toolCallId">The unique identifier of the tool call.</param>
    /// <returns>The list of validation attempts, or empty if no attempts recorded.</returns>
    IReadOnlyList<ValidationAttempt> GetHistory(string toolCallId);

    /// <summary>
    /// Checks if a tool call has exceeded the maximum retry limit.
    /// </summary>
    /// <param name="toolCallId">The unique identifier of the tool call.</param>
    /// <returns>True if max retries exceeded, false otherwise.</returns>
    bool HasExceededMaxRetries(string toolCallId);

    /// <summary>
    /// Clears the tracking state for a tool call.
    /// </summary>
    /// <param name="toolCallId">The unique identifier of the tool call.</param>
    void Clear(string toolCallId);
}
