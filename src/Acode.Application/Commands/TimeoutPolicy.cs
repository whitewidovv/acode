namespace Acode.Application.Commands;

/// <summary>
/// Timeout policy for command execution.
/// Defines timeout behavior per Task 002.c FR-002c-96 through FR-002c-102.
/// </summary>
public static class TimeoutPolicy
{
    /// <summary>
    /// Default timeout in seconds for command execution.
    /// </summary>
    /// <remarks>
    /// Per FR-002c-97: Default timeout is 300 seconds (5 minutes).
    /// </remarks>
    public const int DefaultTimeoutSeconds = 300;

    /// <summary>
    /// Constant indicating no timeout.
    /// </summary>
    /// <remarks>
    /// Per FR-002c-100: Timeout of 0 means no timeout.
    /// </remarks>
    public const int NoTimeout = 0;

    /// <summary>
    /// Converts a timeout value in seconds to a TimeSpan.
    /// </summary>
    /// <param name="timeoutSeconds">The timeout in seconds. Zero means no timeout.</param>
    /// <returns>A TimeSpan representing the timeout, or Timeout.InfiniteTimeSpan if no timeout.</returns>
    /// <remarks>
    /// Per FR-002c-96, FR-002c-97, FR-002c-100:
    /// - Default timeout: 300 seconds.
    /// - Zero means no timeout (returns Timeout.InfiniteTimeSpan).
    /// - Negative values are treated as no timeout.
    /// </remarks>
    public static TimeSpan GetTimeout(int timeoutSeconds)
    {
        if (timeoutSeconds <= 0)
        {
            return Timeout.InfiniteTimeSpan;
        }

        return TimeSpan.FromSeconds(timeoutSeconds);
    }

    /// <summary>
    /// Determines whether a timeout is enabled based on the timeout value.
    /// </summary>
    /// <param name="timeoutSeconds">The timeout in seconds.</param>
    /// <returns>True if timeout is enabled (positive value), false otherwise.</returns>
    public static bool IsTimeout(int timeoutSeconds)
    {
        return timeoutSeconds > 0;
    }
}
