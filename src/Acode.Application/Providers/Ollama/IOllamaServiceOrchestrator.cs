using Acode.Domain.Providers.Ollama;

namespace Acode.Application.Providers.Ollama;

/// <summary>
/// Orchestrates the complete lifecycle of the Ollama inference service.
/// </summary>
/// <remarks>
/// This interface is implemented by the infrastructure layer and provides:
/// - Process startup, shutdown, and restart management
/// - Health monitoring and crash detection
/// - Model pulling and management
/// - Support for three operating modes: Managed, Monitored, External
/// - Integration with operating mode constraints (LocalOnly, Burst, Airgapped)
///
/// The orchestrator sits between the application layer and the provider adapter,
/// ensuring the service is ready before inference requests are made.
///
/// Task 005d Functional Requirements: FR-010 to FR-055.
/// </remarks>
public interface IOllamaServiceOrchestrator
{
    /// <summary>
    /// Ensures Ollama is running and healthy, or starts it if necessary (Managed mode only).
    /// </summary>
    /// <remarks>
    /// This is the primary method for application code to call before making inference requests.
    /// Behavior depends on the configured lifecycle mode:
    /// - Managed: Checks health, starts if not running, waits for readiness
    /// - Monitored: Checks health, logs errors if unhealthy (doesn't start)
    /// - External: Assumes running (may do minimal checks).
    /// </remarks>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The current service state after ensuring health is achieved.</returns>
    /// <exception cref="OperationCanceledException">Operation was cancelled.</exception>
    /// <exception cref="InvalidOperationException">Service cannot be made healthy (Monitored/External mode and service down).</exception>
    Task<OllamaServiceState> EnsureHealthyAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Gets the current state of the Ollama service without attempting to modify it.
    /// </summary>
    /// <remarks>
    /// Returns immediately with cached state (refreshed periodically by health check worker).
    /// Does not perform blocking I/O or attempt to start the service.
    /// </remarks>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The current service state.</returns>
    /// <exception cref="OperationCanceledException">Operation was cancelled.</exception>
    Task<OllamaServiceState> GetStateAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Manually starts the Ollama service process (Managed mode only).
    /// </summary>
    /// <remarks>
    /// Not applicable in Monitored or External modes.
    /// Blocks until the process starts and becomes healthy (or timeout).
    /// In Managed mode, automatically called by EnsureHealthyAsync if needed.
    /// </remarks>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The service state after startup attempt.</returns>
    /// <exception cref="OperationCanceledException">Operation was cancelled or timed out.</exception>
    /// <exception cref="InvalidOperationException">Operation not valid in current lifecycle mode.</exception>
    Task<OllamaServiceState> StartAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Gracefully stops the Ollama service process (Managed mode only).
    /// </summary>
    /// <remarks>
    /// Sends SIGTERM (Unix) or equivalent (Windows), waits for graceful shutdown.
    /// Force-kills after timeout if process doesn't terminate gracefully.
    /// Not applicable in Monitored or External modes.
    /// </remarks>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The service state after shutdown attempt.</returns>
    /// <exception cref="OperationCanceledException">Operation was cancelled.</exception>
    /// <exception cref="InvalidOperationException">Operation not valid in current lifecycle mode.</exception>
    Task<OllamaServiceState> StopAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Pulls (downloads) a model from the Ollama model registry.
    /// </summary>
    /// <remarks>
    /// Blocks until the model is fully downloaded and verified.
    /// Automatically retries on network errors (up to 3 times).
    /// If model already exists locally, returns immediately with success.
    /// Rejects pulls in Airgapped mode with a specific error.
    /// Non-blocking failure: If pull fails, logs warning but doesn't crash health checks.
    /// </remarks>
    /// <param name="modelName">The model name/ID to pull (e.g., "llama2:latest").</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>Result indicating success or failure with details.</returns>
    /// <exception cref="OperationCanceledException">Operation was cancelled.</exception>
    /// <exception cref="ArgumentException">Model name is invalid.</exception>
    Task<ModelPullResult> PullModelAsync(string modelName, CancellationToken cancellationToken);

    /// <summary>
    /// Pulls a model with progress callback support.
    /// </summary>
    /// <remarks>
    /// Same as <see cref="PullModelAsync(string, CancellationToken)"/> but provides progress updates.
    /// Useful for UIs showing download progress.
    /// </remarks>
    /// <param name="modelName">The model name/ID to pull.</param>
    /// <param name="progressCallback">Optional callback invoked with progress updates.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>Result indicating success or failure with details.</returns>
    /// <exception cref="OperationCanceledException">Operation was cancelled.</exception>
    /// <exception cref="ArgumentException">Model name is invalid.</exception>
    Task<ModelPullResult> PullModelAsync(string modelName, Action<ModelPullProgress>? progressCallback, CancellationToken cancellationToken);

    /// <summary>
    /// Pulls a model as an asynchronous stream of progress events.
    /// </summary>
    /// <remarks>
    /// Yields progress events as they occur during download.
    /// Final event has Status="complete" after successful pull.
    /// If pull fails, final event contains error information in Status.
    /// </remarks>
    /// <param name="modelName">The model name/ID to pull.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>Async enumerable of progress events.</returns>
    /// <exception cref="OperationCanceledException">Operation was cancelled.</exception>
    /// <exception cref="ArgumentException">Model name is invalid.</exception>
    IAsyncEnumerable<ModelPullProgress> PullModelStreamAsync(string modelName, CancellationToken cancellationToken);
}
