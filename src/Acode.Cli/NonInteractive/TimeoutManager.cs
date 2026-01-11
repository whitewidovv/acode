// <copyright file="TimeoutManager.cs" company="Acode">
// Copyright (c) Acode. All rights reserved.
// </copyright>

using Microsoft.Extensions.Logging;

namespace Acode.Cli.NonInteractive;

/// <summary>
/// Manages timeouts for non-interactive operations.
/// </summary>
/// <remarks>
/// FR-025 through FR-032: Timeout configuration requirements.
/// </remarks>
public sealed class TimeoutManager : IDisposable
{
    private readonly TimeSpan _timeout;
    private readonly ILogger<TimeoutManager>? _logger;
    private readonly CancellationTokenSource _cts;
    private DateTimeOffset _startTime;
    private bool _isStarted;
    private bool _isDisposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="TimeoutManager"/> class.
    /// </summary>
    /// <param name="timeout">The timeout duration.</param>
    /// <param name="logger">Optional logger for timeout events.</param>
    public TimeoutManager(TimeSpan timeout, ILogger<TimeoutManager>? logger = null)
    {
        _timeout = timeout;
        _logger = logger;
        _cts = new CancellationTokenSource();
        _startTime = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Gets the configured timeout duration.
    /// </summary>
    public TimeSpan Timeout => _timeout;

    /// <summary>
    /// Gets the remaining time before timeout.
    /// </summary>
    /// <remarks>
    /// FR-030: Remaining time MUST be logged periodically.
    /// </remarks>
    public TimeSpan Remaining
    {
        get
        {
            // FR-028: Timeout 0 MUST mean no timeout (infinite remaining)
            if (
                !_isStarted
                || _timeout == TimeSpan.Zero
                || _timeout == System.Threading.Timeout.InfiniteTimeSpan
            )
            {
                return System.Threading.Timeout.InfiniteTimeSpan;
            }

            var elapsed = DateTimeOffset.UtcNow - _startTime;
            var remaining = _timeout - elapsed;
            return remaining < TimeSpan.Zero ? TimeSpan.Zero : remaining;
        }
    }

    /// <summary>
    /// Gets a value indicating whether the timeout has expired.
    /// </summary>
    public bool IsExpired => _isStarted && Remaining == TimeSpan.Zero;

    /// <summary>
    /// Gets the cancellation token for the timeout.
    /// </summary>
    public CancellationToken Token => _cts.Token;

    /// <summary>
    /// Starts the timeout countdown.
    /// </summary>
    public void Start()
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);

        if (_isStarted)
        {
            return;
        }

        _isStarted = true;
        _startTime = DateTimeOffset.UtcNow;

        // FR-028: Timeout 0 MUST mean no timeout
        if (_timeout == TimeSpan.Zero || _timeout == System.Threading.Timeout.InfiniteTimeSpan)
        {
            _logger?.LogDebug("Timeout disabled (infinite or zero duration)");
            return;
        }

        _logger?.LogInformation("Timeout started: {Timeout:c}", _timeout);
        _cts.CancelAfter(_timeout);
    }

    /// <summary>
    /// Cancels the timeout.
    /// </summary>
    public void Cancel()
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);

        if (!_isStarted)
        {
            return;
        }

        _logger?.LogDebug("Timeout cancelled");
        _cts.Cancel();
    }

    /// <summary>
    /// Waits for the timeout to expire.
    /// </summary>
    /// <param name="cancellationToken">Additional cancellation token.</param>
    /// <returns>A task that completes when the timeout expires.</returns>
    public async Task WaitAsync(CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);

        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
            _cts.Token,
            cancellationToken
        );

        try
        {
            await Task.Delay(System.Threading.Timeout.InfiniteTimeSpan, linkedCts.Token)
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // Expected when timeout expires or operation is cancelled
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        _cts.Dispose();
        _isDisposed = true;
    }
}
