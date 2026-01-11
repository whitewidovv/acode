// src/Acode.Infrastructure/Sync/SyncEngine.cs
#pragma warning disable CS0649 // Field is never assigned - will be used when Chat domain models exist

namespace Acode.Infrastructure.Sync;

using System;
using System.Threading;
using System.Threading.Tasks;
using Acode.Application.Sync;
using Acode.Domain.Sync;

/// <summary>
/// Background sync engine for managing outbox processing.
/// </summary>
public sealed class SyncEngine : ISyncEngine, IDisposable
{
    private readonly IOutboxRepository _outboxRepository;
    private readonly int _pollingIntervalMs;
    private readonly SemaphoreSlim _syncLock = new(1, 1);

    private CancellationTokenSource? _cancellationTokenSource;
    private Task? _backgroundTask;
    private bool _isRunning;
    private bool _isPaused;
    private DateTimeOffset? _startedAt;
    private DateTimeOffset? _lastSyncAt;
    private long _totalProcessed;
    private long _totalFailed;

    /// <summary>
    /// Initializes a new instance of the <see cref="SyncEngine"/> class.
    /// </summary>
    /// <param name="outboxRepository">The outbox repository.</param>
    /// <param name="pollingIntervalMs">Polling interval in milliseconds (default 5000).</param>
    public SyncEngine(IOutboxRepository outboxRepository, int pollingIntervalMs = 5000)
    {
        ArgumentNullException.ThrowIfNull(outboxRepository);

        if (pollingIntervalMs <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(pollingIntervalMs), "Polling interval must be greater than zero");
        }

        _outboxRepository = outboxRepository;
        _pollingIntervalMs = pollingIntervalMs;
    }

    /// <inheritdoc/>
    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (_isRunning)
        {
            return Task.CompletedTask; // Already running, idempotent
        }

        _isRunning = true;
        _isPaused = false;
        _startedAt = DateTimeOffset.UtcNow;
        _cancellationTokenSource = new CancellationTokenSource();
        _backgroundTask = RunBackgroundWorkerAsync(_cancellationTokenSource.Token);

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        if (!_isRunning || _cancellationTokenSource == null)
        {
            return; // Not running, idempotent
        }

        _isRunning = false;
        _isPaused = false;
        _cancellationTokenSource.Cancel();

        if (_backgroundTask != null)
        {
            try
            {
                await _backgroundTask.ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // Expected when cancelling
            }
        }

        _cancellationTokenSource?.Dispose();
        _cancellationTokenSource = null;
        _backgroundTask = null;
    }

    /// <inheritdoc/>
    public async Task SyncNowAsync(CancellationToken cancellationToken = default)
    {
        await _syncLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await ProcessPendingEntriesAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _syncLock.Release();
        }
    }

    /// <inheritdoc/>
    public async Task<SyncStatus> GetStatusAsync(CancellationToken cancellationToken = default)
    {
        var pendingEntries = await _outboxRepository.GetPendingAsync(limit: 1000, cancellationToken).ConfigureAwait(false);

        TimeSpan? syncLag = null;
        if (pendingEntries.Count > 0)
        {
            var oldestEntry = pendingEntries[0]; // GetPendingAsync returns ordered by CreatedAt ASC
            syncLag = DateTimeOffset.UtcNow - oldestEntry.CreatedAt;
        }

        return new SyncStatus
        {
            IsRunning = _isRunning,
            IsPaused = _isPaused,
            PendingOutboxCount = pendingEntries.Count,
            LastSyncAt = _lastSyncAt,
            StartedAt = _startedAt,
            SyncLag = syncLag,
            TotalProcessed = _totalProcessed,
            TotalFailed = _totalFailed
        };
    }

    /// <inheritdoc/>
    public Task PauseAsync(CancellationToken cancellationToken = default)
    {
        _isPaused = true;
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task ResumeAsync(CancellationToken cancellationToken = default)
    {
        _isPaused = false;
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        StopAsync(CancellationToken.None).GetAwaiter().GetResult();
        _syncLock?.Dispose();
    }

    private async Task RunBackgroundWorkerAsync(CancellationToken cancellationToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(_pollingIntervalMs));

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await timer.WaitForNextTickAsync(cancellationToken).ConfigureAwait(false);

                if (_isPaused)
                {
                    continue; // Skip processing when paused
                }

                await _syncLock.WaitAsync(cancellationToken).ConfigureAwait(false);
                try
                {
                    await ProcessPendingEntriesAsync(cancellationToken).ConfigureAwait(false);
                }
                finally
                {
                    _syncLock.Release();
                }
            }
            catch (OperationCanceledException)
            {
                // Expected when stopping
                break;
            }
            catch (Exception)
            {
                // Log error and continue (resilient background worker)
                // TODO: Add structured logging when logger is available
                _totalFailed++;
            }
        }
    }

    private async Task ProcessPendingEntriesAsync(CancellationToken cancellationToken)
    {
        // Retrieve pending entries
        var pendingEntries = await _outboxRepository.GetPendingAsync(limit: 50, cancellationToken).ConfigureAwait(false);

        if (pendingEntries.Count == 0)
        {
            return; // No work to do
        }

        // TODO: When Chat/Run/Message domain models exist:
        // 1. Use OutboxBatcher to create batches
        // 2. For each batch:
        //    a. Deserialize payloads to domain objects
        //    b. Use RetryPolicy to sync to Postgres
        //    c. Mark entries as completed on success
        //    d. Mark entries as failed and schedule retry on transient errors
        //    e. Move to dead letter on permanent errors
        // 3. Update _totalProcessed and _totalFailed counters
        // 4. Update _lastSyncAt timestamp

        // For now, just update last sync timestamp (stub)
        _lastSyncAt = DateTimeOffset.UtcNow;
    }
}
