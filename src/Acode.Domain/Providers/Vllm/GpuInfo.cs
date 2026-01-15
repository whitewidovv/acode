namespace Acode.Domain.Providers.Vllm;

/// <summary>
/// Represents GPU device information.
/// </summary>
public sealed class GpuInfo
{
    /// <summary>
    /// Gets the GPU device ID (0, 1, 2, etc.).
    /// </summary>
    public int DeviceId { get; init; }

    /// <summary>
    /// Gets the GPU device name (e.g., "NVIDIA RTX 4090").
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Gets total memory in MB.
    /// </summary>
    public long TotalMemoryMb { get; init; }

    /// <summary>
    /// Gets available memory in MB.
    /// </summary>
    public long AvailableMemoryMb { get; init; }

    /// <summary>
    /// Gets GPU utilization percentage (0-100).
    /// </summary>
    public double UtilizationPercent { get; init; }

    /// <summary>
    /// Gets GPU temperature in celsius (or null if unavailable).
    /// </summary>
    public double? TemperatureCelsius { get; init; }

    /// <summary>
    /// Gets a value indicating whether GPU memory is currently available for loading models.
    /// </summary>
    public bool IsAvailable => AvailableMemoryMb > 0;

    /// <summary>
    /// Gets memory usage percentage (0-100).
    /// </summary>
    public double MemoryUsagePercent =>
        TotalMemoryMb > 0 ? ((TotalMemoryMb - AvailableMemoryMb) * 100.0) / TotalMemoryMb : 0;
}
