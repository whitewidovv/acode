// <copyright file="SignalHandler.cs" company="Acode">
// Copyright (c) Acode. All rights reserved.
// </copyright>

using Microsoft.Extensions.Logging;

namespace Acode.Cli.NonInteractive;

/// <summary>
/// Handles OS signals for graceful shutdown.
/// </summary>
/// <remarks>
/// FR-058 through FR-063: Signal handling requirements.
/// </remarks>
public sealed class SignalHandler
{
    private readonly bool _isInteractive;
    private readonly ILogger<SignalHandler>? _logger;
    private readonly CancellationTokenSource _shutdownCts;
    private bool _isRegistered;
    private bool _shutdownRequested;

    /// <summary>
    /// Initializes a new instance of the <see cref="SignalHandler"/> class.
    /// </summary>
    /// <param name="isInteractive">Whether the CLI is running in interactive mode.</param>
    /// <param name="logger">Optional logger for signal events.</param>
    public SignalHandler(bool isInteractive, ILogger<SignalHandler>? logger = null)
    {
        _isInteractive = isInteractive;
        _logger = logger;
        _shutdownCts = new CancellationTokenSource();
    }

    /// <summary>
    /// Event raised when a signal is received.
    /// </summary>
    public event EventHandler<SignalEventArgs>? SignalReceived;

    /// <summary>
    /// Event raised when shutdown is requested.
    /// </summary>
    public event EventHandler? ShutdownRequested;

    /// <summary>
    /// Event raised when a broken pipe is detected.
    /// </summary>
    public event EventHandler? PipeError;

    /// <summary>
    /// Gets the grace period for shutdown.
    /// </summary>
    /// <remarks>
    /// FR-062: Shutdown MUST have maximum duration (30s).
    /// Non-interactive mode uses shorter grace period.
    /// </remarks>
    public TimeSpan GracePeriod =>
        _isInteractive ? TimeSpan.FromSeconds(30) : TimeSpan.FromSeconds(10);

    /// <summary>
    /// Gets the cancellation token for shutdown.
    /// </summary>
    public CancellationToken ShutdownToken => _shutdownCts.Token;

    /// <summary>
    /// Registers signal handlers.
    /// </summary>
    public void Register()
    {
        if (_isRegistered)
        {
            return;
        }

        // FR-058: SIGINT MUST trigger graceful shutdown
        Console.CancelKeyPress += OnCancelKeyPress;

        // FR-059: SIGTERM MUST trigger graceful shutdown
        AppDomain.CurrentDomain.ProcessExit += OnProcessExit;

        _isRegistered = true;
        _logger?.LogDebug(
            "Signal handlers registered (interactive: {Interactive})",
            _isInteractive
        );
    }

    /// <summary>
    /// Unregisters signal handlers.
    /// </summary>
    public void Unregister()
    {
        if (!_isRegistered)
        {
            return;
        }

        Console.CancelKeyPress -= OnCancelKeyPress;
        AppDomain.CurrentDomain.ProcessExit -= OnProcessExit;

        _isRegistered = false;
        _logger?.LogDebug("Signal handlers unregistered");
    }

    /// <summary>
    /// Requests graceful shutdown.
    /// </summary>
    public void RequestShutdown()
    {
        if (_shutdownRequested)
        {
            return;
        }

        _shutdownRequested = true;
        _logger?.LogInformation("Shutdown requested, grace period: {GracePeriod}", GracePeriod);

        ShutdownRequested?.Invoke(this, EventArgs.Empty);
        _shutdownCts.CancelAfter(GracePeriod);
    }

    /// <summary>
    /// Waits for shutdown to complete.
    /// </summary>
    /// <param name="timeout">Maximum time to wait for shutdown.</param>
    /// <returns>A task that completes when shutdown is complete or timeout expires.</returns>
    public async Task WaitForShutdownAsync(TimeSpan timeout)
    {
        if (!_shutdownRequested)
        {
            return;
        }

        try
        {
            await Task.Delay(timeout, _shutdownCts.Token).ConfigureAwait(true);
        }
        catch (OperationCanceledException)
        {
            // Expected - shutdown completed or grace period expired
        }
    }

    /// <summary>
    /// Called when a broken pipe is detected.
    /// </summary>
    /// <remarks>
    /// FR-060: SIGPIPE MUST NOT crash.
    /// </remarks>
    public void OnBrokenPipe()
    {
        _logger?.LogWarning("Broken pipe detected");
        PipeError?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Handles the CancelKeyPress event (SIGINT).
    /// </summary>
    /// <param name="sender">The event sender.</param>
    /// <param name="e">The event arguments.</param>
    internal void OnCancelKeyPress(object? sender, ConsoleCancelEventArgs e)
    {
        _logger?.LogInformation("SIGINT received");
        SignalReceived?.Invoke(this, new SignalEventArgs("SIGINT"));

        // FR-058: SIGINT MUST trigger graceful shutdown
        // In non-interactive mode, don't cancel the event - allow graceful shutdown
        if (!_isInteractive)
        {
            e.Cancel = false;
            RequestShutdown();
        }
        else
        {
            // In interactive mode, we might prompt the user
            // For now, just allow the cancellation
            e.Cancel = false;
        }
    }

    /// <summary>
    /// Handles the ProcessExit event (SIGTERM).
    /// </summary>
    /// <param name="sender">The event sender.</param>
    /// <param name="e">The event arguments.</param>
    internal void OnProcessExit(object? sender, EventArgs e)
    {
        _logger?.LogInformation("SIGTERM received");
        SignalReceived?.Invoke(this, new SignalEventArgs("SIGTERM"));

        // FR-059: SIGTERM MUST trigger graceful shutdown
        RequestShutdown();
    }
}
