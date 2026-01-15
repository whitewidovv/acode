namespace Acode.Infrastructure.Vllm.Health.Metrics;

/// <summary>
/// Parses Prometheus metrics from vLLM.
/// </summary>
public sealed class VllmMetricsParser
{
    /// <summary>
    /// Parses Prometheus text format metrics.
    /// </summary>
    /// <param name="prometheusText">The metrics text in Prometheus format.</param>
    /// <returns>Parsed metrics.</returns>
    public VllmMetrics Parse(string? prometheusText)
    {
        var runningRequests = 0;
        var waitingRequests = 0;
        var gpuUtilization = 0.0;

        if (string.IsNullOrWhiteSpace(prometheusText))
        {
            return new VllmMetrics();
        }

        var lines = prometheusText.Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(line => line.Trim())
            .Where(trimmed => !trimmed.StartsWith('#') && !string.IsNullOrWhiteSpace(trimmed));

        foreach (var trimmed in lines)
        {
            if (trimmed.StartsWith("vllm_num_requests_running", StringComparison.OrdinalIgnoreCase))
            {
                runningRequests = ExtractValue<int>(trimmed);
            }
            else if (trimmed.StartsWith("vllm_num_requests_waiting", StringComparison.OrdinalIgnoreCase))
            {
                waitingRequests = ExtractValue<int>(trimmed);
            }
            else if (trimmed.StartsWith("vllm_gpu_cache_usage_perc", StringComparison.OrdinalIgnoreCase))
            {
                gpuUtilization = ExtractValue<double>(trimmed);
            }
        }

        return new VllmMetrics
        {
            RunningRequests = runningRequests,
            WaitingRequests = waitingRequests,
            GpuUtilizationPercent = gpuUtilization
        };
    }

    /// <summary>
    /// Extracts a value from a Prometheus metric line.
    /// </summary>
    /// <typeparam name="T">The type of value to extract (int or double).</typeparam>
    /// <param name="line">The metric line.</param>
    /// <returns>The extracted value, or default if parsing fails.</returns>
    private static T ExtractValue<T>(string line)
        where T : struct
    {
        var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 2)
        {
            return default;
        }

        var valueStr = parts[^1]; // Last part is the value

        try
        {
            if (typeof(T) == typeof(int))
            {
                return (T)(object)int.Parse(valueStr);
            }

            if (typeof(T) == typeof(double))
            {
                return (T)(object)double.Parse(valueStr);
            }
        }
        catch
        {
            // Parsing failed, return default
        }

        return default;
    }
}
