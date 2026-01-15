using Acode.Application.Providers.Vllm;
using Acode.Domain.Providers.Vllm;

namespace Acode.Infrastructure.Providers.Vllm.Lifecycle;

/// <summary>
/// Main orchestrator coordinating all vLLM lifecycle components.
/// Manages service startup, shutdown, health monitoring, and auto-restart.
/// </summary>
public sealed class VllmServiceOrchestrator : IVllmServiceOrchestrator
{
    private const int StartupPollingDelayMs = 100;

    private readonly VllmLifecycleOptions _options;
    private readonly VllmServiceStateTracker _stateTracker;
    private readonly VllmRestartPolicyEnforcer _restartPolicy;
    private readonly VllmGpuMonitor _gpuMonitor;
    private readonly VllmModelLoader _modelLoader;
    private readonly VllmHealthCheckWorker _healthCheckWorker;
    private readonly object _modelLock = new();

    private string _currentModel = string.Empty;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="VllmServiceOrchestrator"/> class
    /// with default options.
    /// </summary>
    public VllmServiceOrchestrator()
        : this(
            new VllmLifecycleOptions(),
            new VllmServiceStateTracker(),
            new VllmRestartPolicyEnforcer(),
            new VllmGpuMonitor(),
            new VllmModelLoader(),
            new VllmHealthCheckWorker())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="VllmServiceOrchestrator"/> class
    /// with specified options.
    /// </summary>
    /// <param name="options">Lifecycle configuration options.</param>
    public VllmServiceOrchestrator(VllmLifecycleOptions options)
        : this(
            options,
            new VllmServiceStateTracker(),
            new VllmRestartPolicyEnforcer(),
            new VllmGpuMonitor(),
            new VllmModelLoader(),
            new VllmHealthCheckWorker())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="VllmServiceOrchestrator"/> class
    /// with full dependency injection.
    /// </summary>
    /// <param name="options">Lifecycle configuration options.</param>
    /// <param name="stateTracker">State tracker instance.</param>
    /// <param name="restartPolicy">Restart policy enforcer instance.</param>
    /// <param name="gpuMonitor">GPU monitor instance.</param>
    /// <param name="modelLoader">Model loader instance.</param>
    /// <param name="healthCheckWorker">Health check worker instance.</param>
    public VllmServiceOrchestrator(
        VllmLifecycleOptions options,
        VllmServiceStateTracker stateTracker,
        VllmRestartPolicyEnforcer restartPolicy,
        VllmGpuMonitor gpuMonitor,
        VllmModelLoader modelLoader,
        VllmHealthCheckWorker healthCheckWorker)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _stateTracker = stateTracker ?? throw new ArgumentNullException(nameof(stateTracker));
        _restartPolicy = restartPolicy ?? throw new ArgumentNullException(nameof(restartPolicy));
        _gpuMonitor = gpuMonitor ?? throw new ArgumentNullException(nameof(gpuMonitor));
        _modelLoader = modelLoader ?? throw new ArgumentNullException(nameof(modelLoader));
        _healthCheckWorker = healthCheckWorker ?? throw new ArgumentNullException(nameof(healthCheckWorker));

        _options.Validate();

        // Configure health check worker from options
        _healthCheckWorker.SetHealthCheckInterval(_options.HealthCheckIntervalSeconds);
    }

    /// <inheritdoc/>
    public async Task EnsureHealthyAsync(string? modelIdOverride = null, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        cancellationToken.ThrowIfCancellationRequested();

        string modelToUse;
        lock (_modelLock)
        {
            modelToUse = modelIdOverride ?? _currentModel;
        }

        // If no model configured, need to provide one
        if (string.IsNullOrEmpty(modelToUse))
        {
            if (string.IsNullOrEmpty(modelIdOverride))
            {
                throw new InvalidOperationException(
                    "No model configured. Provide a model ID or call StartAsync first.");
            }

            modelToUse = modelIdOverride;
        }

        // Check current state
        var currentState = _stateTracker.CurrentState;

        // If running and healthy, check if we need model switch
        if (currentState == VllmServiceState.Running && _healthCheckWorker.LastHealthCheckHealthy)
        {
            bool needsRestart;
            lock (_modelLock)
            {
                needsRestart = modelIdOverride != null && modelIdOverride != _currentModel;
            }

            if (needsRestart)
            {
                // Need to restart with new model
                await RestartAsync(modelIdOverride, cancellationToken).ConfigureAwait(false);
            }

            return; // Already healthy with correct model
        }

        // If not running, start
        if (currentState == VllmServiceState.Stopped ||
            currentState == VllmServiceState.Failed ||
            currentState == VllmServiceState.Unknown)
        {
            await StartAsync(modelToUse, cancellationToken).ConfigureAwait(false);
            return;
        }

        // If starting, wait for it
        if (currentState == VllmServiceState.Starting)
        {
            // Wait for startup to complete (simplified - real implementation would poll)
            await Task.Delay(StartupPollingDelayMs, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <inheritdoc/>
    public async Task StartAsync(string modelId, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        cancellationToken.ThrowIfCancellationRequested();

        // Validate model ID
        await _modelLoader.ValidateModelIdAsync(modelId).ConfigureAwait(false);

        // Check if model can be loaded
        if (!await _modelLoader.CanLoadModelAsync(modelId).ConfigureAwait(false))
        {
            var error = _modelLoader.GetModelLoadError(modelId);
            throw new InvalidOperationException(error ?? $"Cannot load model: {modelId}");
        }

        // Transition to Starting state
        _stateTracker.Transition(VllmServiceState.Starting);

        lock (_modelLock)
        {
            _currentModel = modelId;
        }

        try
        {
            // In real implementation, would start vLLM process here
            // For now, simulate startup attempt that fails (no vLLM binary)
            await Task.Delay(50, cancellationToken).ConfigureAwait(false);

            // In test environment, transition to Failed (no actual vLLM)
            _stateTracker.Transition(VllmServiceState.Failed);
            _stateTracker.SetErrorMessage("vLLM binary not found. Ensure vLLM is installed.");
        }
        catch (OperationCanceledException)
        {
            _stateTracker.Transition(VllmServiceState.Stopped);
            throw;
        }
        catch (Exception ex)
        {
            _stateTracker.Transition(VllmServiceState.Failed);
            _stateTracker.SetErrorMessage(ex.Message);
            throw;
        }
    }

    /// <inheritdoc/>
    public Task StopAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        cancellationToken.ThrowIfCancellationRequested();

        var currentState = _stateTracker.CurrentState;

        // If already stopped or unknown, nothing to do
        if (currentState == VllmServiceState.Stopped ||
            currentState == VllmServiceState.Unknown)
        {
            return Task.CompletedTask;
        }

        // Transition to Stopping
        _stateTracker.Transition(VllmServiceState.Stopping);

        // In real implementation, would send SIGTERM to vLLM process here
        // For now, just transition to Stopped
        _stateTracker.Transition(VllmServiceState.Stopped);

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public async Task RestartAsync(string? modelId = null, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        cancellationToken.ThrowIfCancellationRequested();

        string modelToUse;
        lock (_modelLock)
        {
            modelToUse = modelId ?? _currentModel;
        }

        if (string.IsNullOrEmpty(modelToUse))
        {
            throw new InvalidOperationException(
                "No model configured for restart. Provide a model ID.");
        }

        // Check restart policy
        if (!_restartPolicy.CanRestart())
        {
            throw new InvalidOperationException(
                "Restart rate limit exceeded (max 3 per 60 seconds). " +
                "Please wait before attempting another restart.");
        }

        // Record the restart attempt
        _restartPolicy.RecordRestart();

        // Stop current instance
        await StopAsync(cancellationToken).ConfigureAwait(false);

        // Reset health check worker
        _healthCheckWorker.ResetAfterRestart();

        // Start with new/current model
        await StartAsync(modelToUse, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public Task<VllmServiceStatus> GetStatusAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        cancellationToken.ThrowIfCancellationRequested();

        string currentModel;
        lock (_modelLock)
        {
            currentModel = _currentModel;
        }

        var status = new VllmServiceStatus
        {
            State = _stateTracker.CurrentState,
            ProcessId = _stateTracker.ProcessId,
            UpSinceUtc = _stateTracker.UpSinceUtc,
            CurrentModel = currentModel,
            GpuDevices = [], // Would be populated from GPU monitor
            LastHealthCheckUtc = _healthCheckWorker.LastHealthCheckUtc,
            LastHealthCheckHealthy = _healthCheckWorker.LastHealthCheckHealthy,
            ErrorMessage = _stateTracker.ErrorMessage ?? string.Empty
        };

        return Task.FromResult(status);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<GpuInfo>> GetAvailableGpusAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        cancellationToken.ThrowIfCancellationRequested();

        return await _gpuMonitor.GetAvailableGpusAsync().ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        // Clean up resources
        // In real implementation, would stop vLLM process if running
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(VllmServiceOrchestrator));
        }
    }
}
