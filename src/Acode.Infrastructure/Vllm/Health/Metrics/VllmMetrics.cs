namespace Acode.Infrastructure.Vllm.Health.Metrics;

/// <summary>
/// Parsed vLLM metrics from Prometheus endpoint.
/// </summary>
public sealed class VllmMetrics
{
    /// <summary>
    /// Gets the number of currently running requests.
    /// </summary>
    public int RunningRequests { get; init; }

    /// <summary>
    /// Gets the number of requests waiting in queue.
    /// </summary>
    public int WaitingRequests { get; init; }

    /// <summary>
    /// Gets the GPU cache utilization percentage.
    /// </summary>
    public double GpuUtilizationPercent { get; init; }
}
