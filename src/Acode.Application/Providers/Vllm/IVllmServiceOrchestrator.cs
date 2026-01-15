using Acode.Domain.Providers.Vllm;

namespace Acode.Application.Providers.Vllm;

/// <summary>
/// Orchestrates the lifecycle of a vLLM service instance.
/// </summary>
public interface IVllmServiceOrchestrator : IDisposable
{
    /// <summary>
    /// Ensures vLLM service is healthy and running with specified model.
    /// Restarts if needed. Returns immediately if already healthy.
    /// </summary>
    /// <param name="modelIdOverride">Optional model ID to override current model.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task EnsureHealthyAsync(string? modelIdOverride = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Starts vLLM service with specified model.
    /// </summary>
    /// <param name="modelId">Huggingface model ID to load.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task StartAsync(string modelId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Stops vLLM service gracefully.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task StopAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Restarts vLLM service with current or new model.
    /// </summary>
    /// <param name="modelId">Optional new model ID (uses current if null).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task RestartAsync(string? modelId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets current service status.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Current VllmServiceStatus.</returns>
    Task<VllmServiceStatus> GetStatusAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets available GPU devices.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of available GPU devices.</returns>
    Task<IReadOnlyList<GpuInfo>> GetAvailableGpusAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Current status of vLLM service.
/// </summary>
public sealed class VllmServiceStatus
{
    /// <summary>
    /// Gets the current service state.
    /// </summary>
    public VllmServiceState State { get; init; }

    /// <summary>
    /// Gets the process ID (if running).
    /// </summary>
    public int? ProcessId { get; init; }

    /// <summary>
    /// Gets when the service started (UTC).
    /// </summary>
    public DateTime? UpSinceUtc { get; init; }

    /// <summary>
    /// Gets the currently loaded model ID.
    /// </summary>
    public string CurrentModel { get; init; } = string.Empty;

    /// <summary>
    /// Gets available GPU devices.
    /// </summary>
    public IReadOnlyList<GpuInfo> GpuDevices { get; init; } = [];

    /// <summary>
    /// Gets when the last health check occurred (UTC).
    /// </summary>
    public DateTime? LastHealthCheckUtc { get; init; }

    /// <summary>
    /// Gets a value indicating whether the last health check succeeded.
    /// </summary>
    public bool LastHealthCheckHealthy { get; init; }

    /// <summary>
    /// Gets the error message (if any).
    /// </summary>
    public string ErrorMessage { get; init; } = string.Empty;
}
