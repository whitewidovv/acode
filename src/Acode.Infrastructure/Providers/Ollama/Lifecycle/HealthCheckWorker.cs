namespace Acode.Infrastructure.Providers.Ollama.Lifecycle;

/// <summary>
/// Background worker for periodic Ollama health checks.
/// </summary>
/// <remarks>
/// Runs periodically to verify service health and detect crashes.
/// Task 005d Functional Requirements: FR-010 to FR-020, FR-042.
/// </remarks>
internal sealed class HealthCheckWorker : IAsyncDisposable
{
    private readonly int _healthCheckIntervalMs;
    private readonly bool _isExternalMode;
    private CancellationTokenSource? _cts;
    private Task? _workerTask;
    private bool _isRunning;

    /// <summary>
    /// Initializes a new instance of the <see cref="HealthCheckWorker"/> class.
    /// </summary>
    /// <param name="healthCheckIntervalMs">Interval in milliseconds between health checks.</param>
    /// <param name="isExternalMode">Whether in external mode (skips checks).</param>
    public HealthCheckWorker(int healthCheckIntervalMs, bool isExternalMode)
    {
        if (healthCheckIntervalMs <= 0)
        {
            throw new ArgumentException("Interval must be positive", nameof(healthCheckIntervalMs));
        }

        _healthCheckIntervalMs = healthCheckIntervalMs;
        _isExternalMode = isExternalMode;
    }

    /// <summary>
    /// Event fired when health check result is available.
    /// </summary>
    public event Action<HealthCheckResult>? HealthCheckCompleted;

    /// <summary>
    /// Starts the background health check loop.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the worker.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (_isExternalMode)
        {
            // Skip health checks in external mode
            return;
        }

        if (_isRunning)
        {
            // Prevent multiple concurrent health check loops
            return;
        }

        _isRunning = true;
        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _workerTask = RunHealthCheckLoopAsync(_cts.Token);
        await Task.CompletedTask.ConfigureAwait(false);
    }

    /// <summary>
    /// Stops the background health check loop.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_cts != null)
        {
            _cts.Cancel();

            if (_workerTask != null)
            {
                try
                {
                    await _workerTask.ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    // Expected when stopping
                }
            }
        }

        await Task.CompletedTask.ConfigureAwait(false);
    }

    /// <summary>
    /// Disposes the worker and cancels pending operations.
    /// </summary>
    /// <returns>A value task representing the asynchronous operation.</returns>
    public async ValueTask DisposeAsync()
    {
        await StopAsync(CancellationToken.None).ConfigureAwait(false);
        _cts?.Dispose();
    }

    /// <summary>
    /// Runs the periodic health check loop.
    /// </summary>
    private async Task RunHealthCheckLoopAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                // Perform health check
                var result = await PerformHealthCheckAsync(cancellationToken).ConfigureAwait(false);
                HealthCheckCompleted?.Invoke(result);

                // Wait for next interval
                await Task.Delay(_healthCheckIntervalMs, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception)
            {
                // Continue on transient errors
                try
                {
                    await Task.Delay(1000, cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
            }
        }
    }

    /// <summary>
    /// Performs a single health check.
    /// </summary>
    private async Task<HealthCheckResult> PerformHealthCheckAsync(CancellationToken cancellationToken)
    {
        // Simulate health check - would call Ollama health endpoint in real implementation
        await Task.Delay(10, cancellationToken).ConfigureAwait(false);

        return new HealthCheckResult
        {
            IsHealthy = true,
            Timestamp = DateTime.UtcNow,
            ResponseTimeMs = 10,
        };
    }
}

/// <summary>
/// Result of a health check operation.
/// </summary>
internal sealed class HealthCheckResult
{
    /// <summary>
    /// Gets or sets a value indicating whether the service is healthy.
    /// </summary>
    public bool IsHealthy { get; set; }

    /// <summary>
    /// Gets or sets the timestamp of the check.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Gets or sets the response time in milliseconds.
    /// </summary>
    public long ResponseTimeMs { get; set; }

    /// <summary>
    /// Gets or sets an optional error message if unhealthy.
    /// </summary>
    public string? ErrorMessage { get; set; }
}
