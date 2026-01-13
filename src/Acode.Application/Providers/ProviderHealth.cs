namespace Acode.Application.Providers;

using System;

/// <summary>
/// Health status tracking for a provider.
/// </summary>
/// <remarks>
/// FR-052 to FR-056 from task-004c spec.
/// Gap #6 from task-004c completion checklist.
/// </remarks>
public sealed record ProviderHealth
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ProviderHealth"/> class.
    /// </summary>
    /// <param name="status">The current health status.</param>
    /// <param name="lastChecked">When the health was last checked.</param>
    /// <param name="lastError">The last error message, if any.</param>
    /// <param name="consecutiveFailures">Number of consecutive health check failures.</param>
    public ProviderHealth(
        HealthStatus status = HealthStatus.Unknown,
        DateTime? lastChecked = null,
        string? lastError = null,
        int consecutiveFailures = 0)
    {
        if (consecutiveFailures < 0)
        {
            throw new ArgumentException(
                "ConsecutiveFailures must be >= 0",
                nameof(consecutiveFailures));
        }

        Status = status;
        LastChecked = lastChecked;
        LastError = lastError;
        ConsecutiveFailures = consecutiveFailures;
    }

    /// <summary>
    /// Gets the current health status.
    /// </summary>
    public HealthStatus Status { get; init; }

    /// <summary>
    /// Gets when the health was last checked.
    /// </summary>
    public DateTime? LastChecked { get; init; }

    /// <summary>
    /// Gets the last error message, if any.
    /// </summary>
    public string? LastError { get; init; }

    /// <summary>
    /// Gets the number of consecutive health check failures.
    /// </summary>
    public int ConsecutiveFailures { get; init; }
}
