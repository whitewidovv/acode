namespace Acode.Infrastructure.Vllm.Health;

/// <summary>
/// Configuration for vLLM health checking.
/// </summary>
public sealed class VllmHealthConfiguration
{
    /// <summary>
    /// Gets or sets the health check endpoint.
    /// </summary>
    public string HealthEndpoint { get; set; } = "/health";

    /// <summary>
    /// Gets or sets the health check timeout in seconds.
    /// </summary>
    public int TimeoutSeconds { get; set; } = 10;

    /// <summary>
    /// Gets or sets the response time threshold for healthy status in milliseconds.
    /// </summary>
    public int HealthyThresholdMs { get; set; } = 1000;

    /// <summary>
    /// Gets or sets the response time threshold for degraded status in milliseconds.
    /// </summary>
    public int DegradedThresholdMs { get; set; } = 5000;

    /// <summary>
    /// Gets or sets the load monitoring configuration.
    /// </summary>
    public LoadMonitoringConfiguration LoadMonitoring { get; set; } = new();

    /// <summary>
    /// Validates the configuration.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when configuration is invalid.</exception>
    public void Validate()
    {
        if (TimeoutSeconds <= 0)
        {
            throw new ArgumentException("TimeoutSeconds must be greater than 0.", nameof(TimeoutSeconds));
        }

        if (HealthyThresholdMs <= 0)
        {
            throw new ArgumentException("HealthyThresholdMs must be greater than 0.", nameof(HealthyThresholdMs));
        }

        if (DegradedThresholdMs <= HealthyThresholdMs)
        {
            throw new ArgumentException("DegradedThresholdMs must be greater than HealthyThresholdMs.", nameof(DegradedThresholdMs));
        }
    }
}

/// <summary>
/// Configuration for load monitoring.
/// </summary>
public sealed class LoadMonitoringConfiguration
{
    /// <summary>
    /// Gets or sets a value indicating whether load monitoring is enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the metrics endpoint for Prometheus metrics.
    /// </summary>
    public string MetricsEndpoint { get; set; } = "/metrics";

    /// <summary>
    /// Gets or sets the queue depth threshold for overload detection.
    /// </summary>
    public int QueueThreshold { get; set; } = 10;

    /// <summary>
    /// Gets or sets the GPU utilization threshold percentage for overload detection.
    /// </summary>
    public double GpuThresholdPercent { get; set; } = 95.0;
}
