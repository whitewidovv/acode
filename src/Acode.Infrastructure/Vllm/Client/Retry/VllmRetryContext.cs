namespace Acode.Infrastructure.Vllm.Client.Retry;

/// <summary>
/// Context for tracking retry state.
/// </summary>
/// <remarks>
/// FR-075: Tracks retry state for debugging and logging.
/// </remarks>
public sealed class VllmRetryContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="VllmRetryContext"/> class.
    /// </summary>
    /// <param name="attemptNumber">Current attempt number (1-indexed).</param>
    /// <param name="lastException">Exception from previous attempt.</param>
    /// <param name="delayBeforeRetry">Delay before next retry.</param>
    public VllmRetryContext(
        int attemptNumber,
        Exception? lastException,
        TimeSpan delayBeforeRetry)
    {
        AttemptNumber = attemptNumber;
        LastException = lastException;
        DelayBeforeRetry = delayBeforeRetry;
    }

    /// <summary>
    /// Gets the current attempt number (1-indexed).
    /// </summary>
    public int AttemptNumber { get; }

    /// <summary>
    /// Gets the exception from the previous attempt.
    /// </summary>
    public Exception? LastException { get; }

    /// <summary>
    /// Gets the delay before next retry.
    /// </summary>
    public TimeSpan DelayBeforeRetry { get; }
}
