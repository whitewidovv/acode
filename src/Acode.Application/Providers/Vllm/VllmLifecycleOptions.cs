using Acode.Domain.Providers.Vllm;

namespace Acode.Application.Providers.Vllm;

/// <summary>
/// Configuration options for vLLM lifecycle management.
/// </summary>
public sealed class VllmLifecycleOptions
{
    /// <summary>
    /// Gets or sets the lifecycle management mode.
    /// </summary>
    public VllmLifecycleMode Mode { get; set; } = VllmLifecycleMode.Managed;

    /// <summary>
    /// Gets or sets the timeout for starting vLLM (seconds).
    /// </summary>
    public int StartTimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Gets or sets the interval for health checks (seconds).
    /// </summary>
    public int HealthCheckIntervalSeconds { get; set; } = 60;

    /// <summary>
    /// Gets or sets maximum restarts per minute.
    /// </summary>
    public int MaxRestartsPerMinute { get; set; } = 3;

    /// <summary>
    /// Gets or sets the timeout for model lazy loading (seconds).
    /// </summary>
    public int ModelLoadTimeoutSeconds { get; set; } = 300;

    /// <summary>
    /// Gets or sets the HTTP port for vLLM API.
    /// </summary>
    public int Port { get; set; } = 8000;

    /// <summary>
    /// Gets or sets a value indicating whether to stop vLLM on Acode exit.
    /// </summary>
    public bool StopOnExit { get; set; } = false;

    /// <summary>
    /// Gets or sets GPU memory utilization (0.0-1.0).
    /// </summary>
    public double GpuMemoryUtilization { get; set; } = 0.9;

    /// <summary>
    /// Gets or sets tensor parallelism for multi-GPU (1 = single GPU).
    /// </summary>
    public int TensorParallelSize { get; set; } = 1;

    /// <summary>
    /// Validates configuration and throws on invalid values.
    /// </summary>
    public void Validate()
    {
        if (!Enum.IsDefined(typeof(VllmLifecycleMode), Mode))
        {
            throw new ArgumentException($"Invalid lifecycle mode: {Mode}");
        }

        if (StartTimeoutSeconds <= 0)
        {
            throw new ArgumentException($"StartTimeoutSeconds must be positive, got {StartTimeoutSeconds}");
        }

        if (HealthCheckIntervalSeconds <= 0)
        {
            throw new ArgumentException($"HealthCheckIntervalSeconds must be positive, got {HealthCheckIntervalSeconds}");
        }

        if (MaxRestartsPerMinute <= 0)
        {
            throw new ArgumentException($"MaxRestartsPerMinute must be positive, got {MaxRestartsPerMinute}");
        }

        if (ModelLoadTimeoutSeconds <= 0)
        {
            throw new ArgumentException($"ModelLoadTimeoutSeconds must be positive, got {ModelLoadTimeoutSeconds}");
        }

        if (Port < 1024 || Port > 65535)
        {
            throw new ArgumentException($"Port must be 1024-65535, got {Port}");
        }

        if (GpuMemoryUtilization < 0.0 || GpuMemoryUtilization > 1.0)
        {
            throw new ArgumentException($"GpuMemoryUtilization must be 0.0-1.0, got {GpuMemoryUtilization}");
        }

        if (TensorParallelSize < 1)
        {
            throw new ArgumentException($"TensorParallelSize must be >= 1, got {TensorParallelSize}");
        }
    }
}
