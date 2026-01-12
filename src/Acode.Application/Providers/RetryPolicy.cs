namespace Acode.Application.Providers;

using System;

/// <summary>
/// Retry behavior configuration for provider operations.
/// </summary>
/// <remarks>
/// FR-047 to FR-051 from task-004c spec.
/// Gap #5 from task-004c completion checklist.
/// </remarks>
public sealed record RetryPolicy
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RetryPolicy"/> class.
    /// </summary>
    /// <param name="maxAttempts">Maximum number of retry attempts (defaults to 3).</param>
    /// <param name="initialDelay">Initial delay before first retry (defaults to 1 second).</param>
    /// <param name="maxDelay">Maximum delay between retries (defaults to 30 seconds).</param>
    /// <param name="backoffMultiplier">Backoff multiplier for exponential backoff (defaults to 2.0).</param>
    public RetryPolicy(
        int? maxAttempts = null,
        TimeSpan? initialDelay = null,
        TimeSpan? maxDelay = null,
        double? backoffMultiplier = null)
    {
        var actualMaxAttempts = maxAttempts ?? 3;
        var actualInitialDelay = initialDelay ?? TimeSpan.FromSeconds(1);
        var actualMaxDelay = maxDelay ?? TimeSpan.FromSeconds(30);
        var actualBackoffMultiplier = backoffMultiplier ?? 2.0;

        if (actualMaxAttempts < 0)
        {
            throw new ArgumentException(
                "MaxAttempts must be >= 0",
                nameof(maxAttempts));
        }

        if (actualInitialDelay <= TimeSpan.Zero)
        {
            throw new ArgumentException(
                "InitialDelay must be positive",
                nameof(initialDelay));
        }

        if (actualMaxDelay <= TimeSpan.Zero)
        {
            throw new ArgumentException(
                "MaxDelay must be positive",
                nameof(maxDelay));
        }

        if (actualBackoffMultiplier < 1.0)
        {
            throw new ArgumentException(
                "BackoffMultiplier must be >= 1.0",
                nameof(backoffMultiplier));
        }

        MaxAttempts = actualMaxAttempts;
        InitialDelay = actualInitialDelay;
        MaxDelay = actualMaxDelay;
        BackoffMultiplier = actualBackoffMultiplier;
    }

    /// <summary>
    /// Gets the maximum number of retry attempts.
    /// </summary>
    public int MaxAttempts { get; init; }

    /// <summary>
    /// Gets the initial delay before first retry.
    /// </summary>
    public TimeSpan InitialDelay { get; init; }

    /// <summary>
    /// Gets the maximum delay between retries.
    /// </summary>
    public TimeSpan MaxDelay { get; init; }

    /// <summary>
    /// Gets the backoff multiplier for exponential backoff.
    /// </summary>
    public double BackoffMultiplier { get; init; }
}
