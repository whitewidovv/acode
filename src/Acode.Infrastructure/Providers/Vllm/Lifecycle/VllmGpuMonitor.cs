using Acode.Domain.Providers.Vllm;

namespace Acode.Infrastructure.Providers.Vllm.Lifecycle;

/// <summary>
/// Detects and monitors available GPU devices (NVIDIA, AMD).
/// </summary>
public sealed class VllmGpuMonitor
{
    /// <summary>
    /// Gets available GPU devices with current metrics.
    /// </summary>
    /// <returns>List of available GpuInfo objects (empty if no GPUs).</returns>
    public async Task<IReadOnlyList<GpuInfo>> GetAvailableGpusAsync()
    {
        // Try NVIDIA first, then AMD
        var nvidiaGpus = await TryDetectNvidiaGpusAsync().ConfigureAwait(false);
        if (nvidiaGpus.Count > 0)
        {
            return nvidiaGpus;
        }

        var amdGpus = await TryDetectAmdGpusAsync().ConfigureAwait(false);
        if (amdGpus.Count > 0)
        {
            return amdGpus;
        }

        return [];
    }

    /// <summary>
    /// Gets a value indicating whether any GPU devices are available.
    /// </summary>
    /// <returns>True if at least one GPU is available.</returns>
    public async Task<bool> IsGpuAvailableAsync()
    {
        var gpus = await GetAvailableGpusAsync().ConfigureAwait(false);
        return gpus.Count > 0;
    }

    /// <summary>
    /// Detects GPU-related errors or unavailability.
    /// </summary>
    /// <returns>Error message if GPU issue detected, null otherwise.</returns>
    public async Task<string?> DetectGpuErrorAsync()
    {
        // Check if nvidia-smi is available
        var nvidiaError = await CheckNvidiaAvailabilityAsync().ConfigureAwait(false);
        if (nvidiaError != null)
        {
            return nvidiaError;
        }

        // Check if rocm-smi is available
        var amdError = await CheckAmdAvailabilityAsync().ConfigureAwait(false);
        if (amdError != null)
        {
            return amdError;
        }

        return null;
    }

    /// <summary>
    /// Gets GPU utilization information for a specific device.
    /// </summary>
    /// <param name="deviceId">The GPU device ID (0-based).</param>
    /// <returns>GpuInfo for the device, or null if not found.</returns>
    public async Task<GpuInfo?> GetGpuUtilizationAsync(int deviceId)
    {
        var gpus = await GetAvailableGpusAsync().ConfigureAwait(false);
        return gpus.FirstOrDefault(g => g.DeviceId == deviceId);
    }

    /// <summary>
    /// Attempts to detect NVIDIA GPUs using nvidia-smi.
    /// </summary>
    /// <remarks>
    /// Note: This implementation is a placeholder. In production, it would:
    /// 1. Run: nvidia-smi --query-gpu=index,name,memory.total,memory.free,utilization.gpu --format=csv,nounits.
    /// 2. Parse CSV output.
    /// 3. Map to GpuInfo objects.
    /// </remarks>
    private async Task<IReadOnlyList<GpuInfo>> TryDetectNvidiaGpusAsync()
    {
        // Placeholder: In production, would execute nvidia-smi command
        // For now, return empty to indicate no NVIDIA GPUs detected
        return await Task.FromResult<IReadOnlyList<GpuInfo>>([]).ConfigureAwait(false);
    }

    /// <summary>
    /// Attempts to detect AMD GPUs using rocm-smi.
    /// </summary>
    /// <remarks>
    /// Note: This implementation is a placeholder. In production, it would:
    /// 1. Run: rocm-smi --json.
    /// 2. Parse JSON output.
    /// 3. Map to GpuInfo objects.
    /// </remarks>
    private async Task<IReadOnlyList<GpuInfo>> TryDetectAmdGpusAsync()
    {
        // Placeholder: In production, would execute rocm-smi command
        // For now, return empty to indicate no AMD GPUs detected
        return await Task.FromResult<IReadOnlyList<GpuInfo>>([]).ConfigureAwait(false);
    }

    /// <summary>
    /// Checks if NVIDIA GPU support is available.
    /// </summary>
    private async Task<string?> CheckNvidiaAvailabilityAsync()
    {
        // Placeholder: Would check if nvidia-smi exists and works
        return await Task.FromResult<string?>(null).ConfigureAwait(false);
    }

    /// <summary>
    /// Checks if AMD GPU support is available.
    /// </summary>
    private async Task<string?> CheckAmdAvailabilityAsync()
    {
        // Placeholder: Would check if rocm-smi exists and works
        return await Task.FromResult<string?>(null).ConfigureAwait(false);
    }
}
