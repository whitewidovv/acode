namespace Acode.Infrastructure.Vllm.Health;

/// <summary>
/// Represents the load status of a vLLM server.
/// </summary>
public sealed class VllmLoadStatus
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

    /// <summary>
    /// Gets the overall load score (0-100).
    /// </summary>
    public int LoadScore { get; init; }

    /// <summary>
    /// Gets a value indicating whether the server is overloaded.
    /// </summary>
    public bool IsOverloaded { get; init; }

    /// <summary>
    /// Gets the reason for overload status.
    /// </summary>
    public string? OverloadReason { get; init; }

    /// <summary>
    /// Creates a load status with overload detection.
    /// </summary>
    /// <param name="runningRequests">Number of currently running requests.</param>
    /// <param name="waitingRequests">Number of requests waiting in queue.</param>
    /// <param name="gpuUtilizationPercent">GPU utilization percentage.</param>
    /// <param name="queueThreshold">Queue threshold for overload detection.</param>
    /// <param name="gpuThreshold">GPU threshold percentage for overload detection.</param>
    /// <returns>A new VllmLoadStatus with overload detection applied.</returns>
    public static VllmLoadStatus Create(
        int runningRequests,
        int waitingRequests,
        double gpuUtilizationPercent,
        int queueThreshold,
        double gpuThreshold)
    {
        var loadScore = CalculateLoadScore(runningRequests, waitingRequests, gpuUtilizationPercent);

        var isOverloaded = false;
        string? reason = null;

        if (waitingRequests > queueThreshold)
        {
            isOverloaded = true;
            reason = $"Request queue exceeds threshold ({waitingRequests} > {queueThreshold})";
        }
        else if (gpuUtilizationPercent > gpuThreshold)
        {
            isOverloaded = true;
            reason = $"GPU utilization exceeds threshold ({gpuUtilizationPercent:F1}% > {gpuThreshold}%)";
        }

        return new VllmLoadStatus
        {
            RunningRequests = runningRequests,
            WaitingRequests = waitingRequests,
            GpuUtilizationPercent = gpuUtilizationPercent,
            LoadScore = loadScore,
            IsOverloaded = isOverloaded,
            OverloadReason = reason
        };
    }

    private static int CalculateLoadScore(int running, int waiting, double gpuUtilization)
    {
        // Simple load score: weighted average
        var queueScore = Math.Min((running + waiting) * 10, 100);
        var gpuScore = gpuUtilization;

        return (int)((queueScore * 0.5) + (gpuScore * 0.5));
    }
}
