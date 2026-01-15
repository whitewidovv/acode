namespace Acode.Application.ToolSchemas.Retry;

/// <summary>
/// Tracks retry attempts and validation history for tool calls.
/// Thread-safe for concurrent access.
/// </summary>
/// <remarks>
/// Spec Reference: Implementation Prompt lines 3358-3411.
/// Implementations must be thread-safe using ConcurrentDictionary and Interlocked operations.
/// </remarks>
public interface IRetryTracker
{
    /// <summary>
    /// Increments the retry attempt counter for a tool call.
    /// </summary>
    /// <param name="toolCallId">Unique identifier for the tool call.</param>
    /// <returns>The new attempt number (1-based).</returns>
    int IncrementAttempt(string toolCallId);

    /// <summary>
    /// Gets the current attempt number for a tool call.
    /// </summary>
    /// <param name="toolCallId">Unique identifier for the tool call.</param>
    /// <returns>The current attempt number, or 0 if not tracked.</returns>
    int GetCurrentAttempt(string toolCallId);

    /// <summary>
    /// Records an error message in the validation history.
    /// </summary>
    /// <param name="toolCallId">Unique identifier for the tool call.</param>
    /// <param name="errorMessage">The formatted error message.</param>
    void RecordError(string toolCallId, string errorMessage);

    /// <summary>
    /// Gets the validation history for a tool call.
    /// </summary>
    /// <param name="toolCallId">Unique identifier for the tool call.</param>
    /// <returns>List of error messages from all attempts.</returns>
    IReadOnlyList<string> GetHistory(string toolCallId);

    /// <summary>
    /// Checks if the maximum retry attempts have been exceeded.
    /// </summary>
    /// <param name="toolCallId">Unique identifier for the tool call.</param>
    /// <returns>True if max retries exceeded, false otherwise.</returns>
    bool HasExceededMaxRetries(string toolCallId);

    /// <summary>
    /// Clears the retry tracking for a tool call (e.g., after successful validation).
    /// </summary>
    /// <param name="toolCallId">Unique identifier for the tool call.</param>
    void Clear(string toolCallId);
}
