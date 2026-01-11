namespace Acode.Application.Commands;

/// <summary>
/// Retry policy for command execution.
/// Implements exponential backoff per Task 002.c FR-002c-103 through FR-002c-110.
/// </summary>
public static class RetryPolicy
{
    private const int MaxDelaySeconds = 30;
    private const int BaseDelaySeconds = 1;

    /// <summary>
    /// Calculates the delay before the next retry attempt using exponential backoff.
    /// Formula: min(baseDelay * 2^(attemptNumber - 1), maxDelay).
    /// </summary>
    /// <param name="attemptNumber">The attempt number (1-based). First retry is attempt 1.</param>
    /// <returns>The delay to wait before retrying.</returns>
    /// <remarks>
    /// Exponential backoff sequence: 1s, 2s, 4s, 8s, 16s, 30s (capped).
    /// Per FR-002c-105, FR-002c-106, FR-002c-107.
    /// </remarks>
    public static TimeSpan CalculateDelay(int attemptNumber)
    {
        if (attemptNumber <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(attemptNumber), "Attempt number must be positive.");
        }

        // Exponential backoff: 1 * 2^(n-1)
        // Attempt 1: 1 * 2^0 = 1 second
        // Attempt 2: 1 * 2^1 = 2 seconds
        // Attempt 3: 1 * 2^2 = 4 seconds
        // etc.
        var exponentialDelay = BaseDelaySeconds * Math.Pow(2, attemptNumber - 1);

        // Cap at max delay
        var delaySeconds = Math.Min(exponentialDelay, MaxDelaySeconds);

        return TimeSpan.FromSeconds(delaySeconds);
    }

    /// <summary>
    /// Determines whether a command should be retried based on exit code and attempt count.
    /// </summary>
    /// <param name="exitCode">The exit code from the command. Zero indicates success.</param>
    /// <param name="attemptCount">The number of attempts made so far (1-based).</param>
    /// <param name="maxRetries">The maximum number of retry attempts allowed.</param>
    /// <returns>True if the command should be retried, false otherwise.</returns>
    /// <remarks>
    /// Per FR-002c-103, FR-002c-104, FR-002c-109:
    /// - Exit code 0 (success) = no retry.
    /// - Non-zero exit code + attempts remaining = retry.
    /// - Default max retries is 0 (no retries).
    /// </remarks>
    public static bool ShouldRetry(int exitCode, int attemptCount, int maxRetries)
    {
        // Never retry on success
        if (exitCode == 0)
        {
            return false;
        }

        // Check if retries remaining
        // attemptCount is 1-based (1 = initial attempt, 2 = first retry, etc.)
        // So: attemptCount <= maxRetries + 1
        // Example: maxRetries=3 means we can make up to 4 attempts total (initial + 3 retries)
        return attemptCount <= maxRetries + 1;
    }
}
