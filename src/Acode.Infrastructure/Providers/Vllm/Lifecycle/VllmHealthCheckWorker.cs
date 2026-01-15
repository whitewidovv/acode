namespace Acode.Infrastructure.Providers.Vllm.Lifecycle;

/// <summary>
/// Tracks health check state and determines when restarts should be triggered.
/// This is the state tracking component; actual HTTP calls are performed by
/// the orchestrator or health checker service.
/// </summary>
public sealed class VllmHealthCheckWorker
{
    /// <summary>
    /// The vLLM health endpoint path.
    /// </summary>
    public const string HealthEndpoint = "/health";

    /// <summary>
    /// The OpenAI-compatible models endpoint for verifying model is loaded.
    /// </summary>
    public const string ModelsEndpoint = "/v1/models";

    /// <summary>
    /// Maximum consecutive failures before triggering a restart.
    /// </summary>
    public const int MaxConsecutiveFailuresForRestart = 3;

    private int _healthCheckIntervalSeconds = 60;
    private int _healthCheckTimeoutMs = 5000;
    private int _consecutiveFailures;
    private DateTime? _lastHealthCheckUtc;
    private bool _lastHealthCheckHealthy;

    /// <summary>
    /// Gets the health check interval in seconds.
    /// </summary>
    public int HealthCheckIntervalSeconds => _healthCheckIntervalSeconds;

    /// <summary>
    /// Gets the health check timeout in milliseconds.
    /// </summary>
    public int HealthCheckTimeoutMs => _healthCheckTimeoutMs;

    /// <summary>
    /// Gets the number of consecutive health check failures.
    /// </summary>
    public int ConsecutiveFailures => _consecutiveFailures;

    /// <summary>
    /// Gets the UTC time of the last health check.
    /// </summary>
    public DateTime? LastHealthCheckUtc => _lastHealthCheckUtc;

    /// <summary>
    /// Gets a value indicating whether the last health check was healthy.
    /// </summary>
    public bool LastHealthCheckHealthy => _lastHealthCheckHealthy;

    /// <summary>
    /// Sets the health check interval.
    /// </summary>
    /// <param name="intervalSeconds">Interval in seconds (must be positive).</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if interval is not positive.</exception>
    public void SetHealthCheckInterval(int intervalSeconds)
    {
        if (intervalSeconds <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(intervalSeconds),
                intervalSeconds,
                "Health check interval must be positive.");
        }

        _healthCheckIntervalSeconds = intervalSeconds;
    }

    /// <summary>
    /// Sets the health check timeout.
    /// </summary>
    /// <param name="timeoutMs">Timeout in milliseconds.</param>
    public void SetHealthCheckTimeout(int timeoutMs)
    {
        _healthCheckTimeoutMs = timeoutMs;
    }

    /// <summary>
    /// Records a successful health check.
    /// Resets the consecutive failure counter.
    /// </summary>
    public void RecordSuccess()
    {
        _consecutiveFailures = 0;
        _lastHealthCheckUtc = DateTime.UtcNow;
        _lastHealthCheckHealthy = true;
    }

    /// <summary>
    /// Records a failed health check.
    /// Increments the consecutive failure counter.
    /// </summary>
    public void RecordFailure()
    {
        _consecutiveFailures++;
        _lastHealthCheckUtc = DateTime.UtcNow;
        _lastHealthCheckHealthy = false;
    }

    /// <summary>
    /// Determines if a restart should be triggered based on consecutive failures.
    /// </summary>
    /// <returns>True if restart should be triggered (3+ consecutive failures).</returns>
    public bool ShouldTriggerRestart()
    {
        return _consecutiveFailures >= MaxConsecutiveFailuresForRestart;
    }

    /// <summary>
    /// Resets the failure counter after a restart.
    /// Preserves the last check time for diagnostics.
    /// </summary>
    public void ResetAfterRestart()
    {
        _consecutiveFailures = 0;
    }
}
