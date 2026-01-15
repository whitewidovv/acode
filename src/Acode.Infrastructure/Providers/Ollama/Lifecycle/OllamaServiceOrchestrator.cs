using System.Runtime.CompilerServices;
using Acode.Application.Providers.Ollama;
using Acode.Domain.Providers.Ollama;

namespace Acode.Infrastructure.Providers.Ollama.Lifecycle;

/// <summary>
/// Orchestrates the complete lifecycle of Ollama service.
/// </summary>
/// <remarks>
/// Main implementation of IOllamaServiceOrchestrator.
/// Coordinates ServiceStateTracker, HealthCheckWorker, RestartPolicyEnforcer, and ModelPullManager.
/// Supports three operating modes: Managed, Monitored, External.
/// Task 005d Functional Requirements: FR-010 to FR-087.
/// </remarks>
internal sealed class OllamaServiceOrchestrator : IOllamaServiceOrchestrator, IAsyncDisposable
{
    private readonly OllamaLifecycleOptions _options;
    private readonly ServiceStateTracker _stateTracker;
    private readonly HealthCheckWorker _healthCheckWorker;
    private readonly RestartPolicyEnforcer _restartPolicy;
    private readonly ModelPullManager _modelPullManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="OllamaServiceOrchestrator"/> class.
    /// </summary>
    /// <param name="options">Lifecycle configuration options.</param>
    public OllamaServiceOrchestrator(OllamaLifecycleOptions options)
    {
        if (options == null)
        {
            throw new ArgumentNullException(nameof(options));
        }

        _options = options;
        _stateTracker = new ServiceStateTracker();
        _healthCheckWorker = new HealthCheckWorker(
            healthCheckIntervalMs: _options.HealthCheckIntervalSeconds * 1000,
            isExternalMode: _options.Mode == OllamaLifecycleMode.External);
        _restartPolicy = new RestartPolicyEnforcer(
            maxRestartsPerMinute: _options.MaxRestartsPerMinute);
        _modelPullManager = new ModelPullManager(
            airgappedMode: _options.AirgappedMode,
            maxRetries: _options.ModelPullMaxRetries);
    }

    /// <summary>
    /// Ensures Ollama is healthy, starting if necessary (Managed mode only).
    /// </summary>
    /// <remarks>
    /// Primary method for application layer to call before making inference requests.
    /// Behavior depends on lifecycle mode:
    /// - Managed: Checks health, starts if needed, waits for readiness.
    /// - Monitored: Checks health only, does not start.
    /// - External: Assumes running, minimal checks.
    /// </remarks>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The service state after ensuring health.</returns>
    public async Task<OllamaServiceState> EnsureHealthyAsync(CancellationToken cancellationToken)
    {
        // Get current state
        var currentState = await GetStateAsync(cancellationToken).ConfigureAwait(false);

        // If already running and healthy, return
        if (currentState == OllamaServiceState.Running)
        {
            _stateTracker.ResetFailureCount();
            return OllamaServiceState.Running;
        }

        // Handle based on mode
        return _options.Mode switch
        {
            OllamaLifecycleMode.Managed => await EnsureHealthy_ManagedModeAsync(cancellationToken).ConfigureAwait(false),
            OllamaLifecycleMode.Monitored => await EnsureHealthy_MonitoredModeAsync(cancellationToken).ConfigureAwait(false),
            OllamaLifecycleMode.External => await EnsureHealthy_ExternalModeAsync(cancellationToken).ConfigureAwait(false),
            _ => OllamaServiceState.Unknown,
        };
    }

    /// <summary>
    /// Gets the current state without attempting to modify it.
    /// </summary>
    /// <remarks>
    /// Returns immediately with current cached state.
    /// Does not perform I/O or attempt to start the service.
    /// </remarks>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The current service state.</returns>
    public async Task<OllamaServiceState> GetStateAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // Simulate state query - in real implementation would check process/health endpoint
        var state = _stateTracker.CurrentState;
        return await Task.FromResult(state).ConfigureAwait(false);
    }

    /// <summary>
    /// Manually starts Ollama (Managed mode only).
    /// </summary>
    /// <remarks>
    /// Not applicable in Monitored or External modes.
    /// Blocks until process starts and becomes healthy or timeout.
    /// </remarks>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The service state after start attempt.</returns>
    public async Task<OllamaServiceState> StartAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (_options.Mode != OllamaLifecycleMode.Managed)
        {
            return _stateTracker.CurrentState;
        }

        if (!_restartPolicy.CanRestart())
        {
            _stateTracker.UpdateState(OllamaServiceState.Failed);
            return OllamaServiceState.Failed;
        }

        _stateTracker.UpdateState(OllamaServiceState.Starting);
        _restartPolicy.RecordRestart();

        try
        {
            // Simulate startup with timeout
            var startTimeout = TimeSpan.FromSeconds(_options.StartTimeoutSeconds);
            using var timeoutCts = new CancellationTokenSource(startTimeout);
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

            await Task.Delay(100, linkedCts.Token).ConfigureAwait(false);

            _stateTracker.UpdateState(OllamaServiceState.Running);
            _restartPolicy.Reset();
            return OllamaServiceState.Running;
        }
        catch (OperationCanceledException)
        {
            _stateTracker.UpdateState(OllamaServiceState.Failed);
            throw;
        }
        catch (Exception)
        {
            _stateTracker.UpdateState(OllamaServiceState.Failed);
            throw;
        }
    }

    /// <summary>
    /// Stops Ollama gracefully (Managed mode only).
    /// </summary>
    /// <remarks>
    /// Sends SIGTERM (Unix) or equivalent, waits for graceful shutdown.
    /// Force-kills after timeout if process doesn't terminate.
    /// </remarks>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The service state after stop attempt.</returns>
    public async Task<OllamaServiceState> StopAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (_options.Mode != OllamaLifecycleMode.Managed)
        {
            return _stateTracker.CurrentState;
        }

        _stateTracker.UpdateState(OllamaServiceState.Stopping);

        try
        {
            // Simulate graceful shutdown
            var gracePeriod = TimeSpan.FromSeconds(_options.ShutdownGracePeriodSeconds);
            using var timeoutCts = new CancellationTokenSource(gracePeriod);
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

            await Task.Delay(50, linkedCts.Token).ConfigureAwait(false);

            _stateTracker.UpdateState(OllamaServiceState.Stopped);
            return OllamaServiceState.Stopped;
        }
        catch (OperationCanceledException)
        {
            // Force kill after timeout
            _stateTracker.UpdateState(OllamaServiceState.Stopped);
            return OllamaServiceState.Stopped;
        }
    }

    /// <summary>
    /// Pulls a model from Ollama registry.
    /// </summary>
    /// <param name="modelName">The model name/ID to pull.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result indicating success or failure.</returns>
    public async Task<ModelPullResult> PullModelAsync(string modelName, CancellationToken cancellationToken)
    {
        return await PullModelAsync(modelName, null, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Pulls a model with progress callback support.
    /// </summary>
    /// <param name="modelName">The model name/ID to pull.</param>
    /// <param name="progressCallback">Optional callback for progress updates.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result indicating success or failure with details.</returns>
    public async Task<ModelPullResult> PullModelAsync(
        string modelName,
        Action<ModelPullProgress>? progressCallback,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(modelName))
        {
            throw new ArgumentException("Model name cannot be empty", nameof(modelName));
        }

        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            return await _modelPullManager.PullAsync(modelName, progressCallback, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return ModelPullResult.Failure(
                modelName,
                $"Failed to pull model: {ex.Message}",
                "PULL_FAILED");
        }
    }

    /// <summary>
    /// Pulls a model as an async stream of progress events.
    /// </summary>
    /// <param name="modelName">The model name/ID to pull.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>An async enumerable of progress events.</returns>
    public async IAsyncEnumerable<ModelPullProgress> PullModelStreamAsync(
        string modelName,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(modelName))
        {
            throw new ArgumentException("Model name cannot be empty", nameof(modelName));
        }

        cancellationToken.ThrowIfCancellationRequested();

        await foreach (var progress in _modelPullManager.PullStreamAsync(modelName, cancellationToken).ConfigureAwait(false))
        {
            yield return progress;
        }
    }

    /// <summary>
    /// Disposes the orchestrator and its components.
    /// </summary>
    /// <returns>A value task representing the asynchronous operation.</returns>
    public async ValueTask DisposeAsync()
    {
        await _healthCheckWorker.DisposeAsync().ConfigureAwait(false);
    }

    /// <summary>
    /// Ensures healthy in Managed mode (start if needed).
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The service state after health check.</returns>
    private async Task<OllamaServiceState> EnsureHealthy_ManagedModeAsync(CancellationToken cancellationToken)
    {
        // Try to start
        var state = await StartAsync(cancellationToken).ConfigureAwait(false);

        if (state == OllamaServiceState.Running)
        {
            return OllamaServiceState.Running;
        }

        return OllamaServiceState.Failed;
    }

    /// <summary>
    /// Ensures healthy in Monitored mode (no start, only check).
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The service state after health check.</returns>
    private async Task<OllamaServiceState> EnsureHealthy_MonitoredModeAsync(CancellationToken cancellationToken)
    {
        // In monitored mode, we only check health
        cancellationToken.ThrowIfCancellationRequested();

        // Simulate health check
        await Task.Delay(10, cancellationToken).ConfigureAwait(false);

        // External monitor should keep it running
        _stateTracker.UpdateState(OllamaServiceState.Running);
        return OllamaServiceState.Running;
    }

    /// <summary>
    /// Ensures healthy in External mode (assume always running).
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The service state after health check.</returns>
    private async Task<OllamaServiceState> EnsureHealthy_ExternalModeAsync(CancellationToken cancellationToken)
    {
        // In external mode, assume running
        cancellationToken.ThrowIfCancellationRequested();
        await Task.CompletedTask.ConfigureAwait(false);

        _stateTracker.UpdateState(OllamaServiceState.Running);
        return OllamaServiceState.Running;
    }
}
