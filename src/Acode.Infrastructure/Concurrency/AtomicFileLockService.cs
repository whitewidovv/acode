// src/Acode.Infrastructure/Concurrency/AtomicFileLockService.cs
namespace Acode.Infrastructure.Concurrency;

using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Acode.Application.Concurrency;
using Acode.Domain.Concurrency;
using Acode.Domain.Worktree;
using Microsoft.Extensions.Logging;

/// <summary>
/// File-based lock service using atomic operations.
/// Provides worktree-level locking with stale detection and automatic cleanup.
/// </summary>
public sealed class AtomicFileLockService : ILockService
{
    /// <summary>
    /// Default threshold for considering a lock stale (5 minutes).
    /// Can be configured based on deployment needs.
    /// </summary>
    private static readonly TimeSpan DefaultStaleLockThreshold = TimeSpan.FromMinutes(5);

    private readonly string _locksDirectory;
    private readonly ILogger<AtomicFileLockService> _logger;
    private readonly SafeLockPathResolver _pathResolver;
    private readonly TimeSpan _staleLockThreshold;

    /// <summary>
    /// Initializes a new instance of the <see cref="AtomicFileLockService"/> class.
    /// </summary>
    /// <param name="workspaceRoot">The workspace root directory.</param>
    /// <param name="logger">The logger instance.</param>
    /// <param name="staleLockThreshold">Optional threshold for stale lock detection. Defaults to 5 minutes.</param>
    public AtomicFileLockService(
        string workspaceRoot,
        ILogger<AtomicFileLockService> logger,
        TimeSpan? staleLockThreshold = null)
    {
        _locksDirectory = Path.Combine(workspaceRoot, ".agent", "locks");
        _logger = logger;
        _pathResolver = new SafeLockPathResolver(workspaceRoot, logger);
        _staleLockThreshold = staleLockThreshold ?? DefaultStaleLockThreshold;

        Directory.CreateDirectory(_locksDirectory);
    }

    /// <inheritdoc />
    public async Task<IAsyncDisposable> AcquireAsync(
        WorktreeId worktreeId,
        TimeSpan? timeout,
        CancellationToken ct)
    {
        var lockFilePath = _pathResolver.GetLockFilePath(worktreeId);
        var tempFilePath = lockFilePath + ".tmp";

        var lockData = new LockData(
            ProcessId: Environment.ProcessId,
            LockedAt: DateTimeOffset.UtcNow,
            Hostname: Environment.MachineName,
            Terminal: WorktreeLock.GetTerminalId());

        var startTime = DateTimeOffset.UtcNow;

        while (true)
        {
            try
            {
                // Write to temp file
                var json = JsonSerializer.Serialize(lockData);
                await File.WriteAllTextAsync(tempFilePath, json, ct).ConfigureAwait(false);

                // Set permissions (Unix only)
                if (!OperatingSystem.IsWindows())
                {
                    File.SetUnixFileMode(
                        tempFilePath,
                        UnixFileMode.UserRead | UnixFileMode.UserWrite);
                }

                // Atomic rename
                File.Move(tempFilePath, lockFilePath, overwrite: false);

                // Verify ownership
                var verify = await File.ReadAllTextAsync(lockFilePath, ct).ConfigureAwait(false);
                var verifyData = JsonSerializer.Deserialize<LockData>(verify);

                if (verifyData?.ProcessId != Environment.ProcessId)
                {
                    throw new LockCorruptedException(worktreeId, "Ownership verification failed");
                }

                _logger.LogInformation("Lock acquired for {Worktree}", worktreeId);

                return new FileLock(lockFilePath, _logger);
            }
            catch (IOException) when (File.Exists(lockFilePath))
            {
                // Lock exists - check if stale
                var status = await GetStatusAsync(worktreeId, ct).ConfigureAwait(false);

                if (status.IsStale)
                {
                    _logger.LogWarning("Removing stale lock for {Worktree}", worktreeId);
                    File.Delete(lockFilePath);
                    continue;  // Retry acquisition
                }

                // Lock is active - wait or error
                if (timeout.HasValue)
                {
                    var elapsed = DateTimeOffset.UtcNow - startTime;
                    if (elapsed >= timeout.Value)
                    {
                        throw new TimeoutException(
                            $"Timeout waiting for lock on {worktreeId} after {elapsed.TotalSeconds:F1}s");
                    }

                    _logger.LogDebug("Waiting for lock on {Worktree}...", worktreeId);
                    await Task.Delay(TimeSpan.FromSeconds(2), ct).ConfigureAwait(false);
                    continue;
                }

                throw new LockBusyException(worktreeId, status);
            }
        }
    }

    /// <inheritdoc />
    public async Task<LockStatus> GetStatusAsync(
        WorktreeId worktreeId,
        CancellationToken ct)
    {
        var lockFilePath = _pathResolver.GetLockFilePath(worktreeId);

        if (!File.Exists(lockFilePath))
        {
            return new LockStatus(false, false, TimeSpan.Zero, null, null, null);
        }

        var json = await File.ReadAllTextAsync(lockFilePath, ct).ConfigureAwait(false);
        var data = JsonSerializer.Deserialize<LockData>(json);

        if (data is null)
        {
            return new LockStatus(false, false, TimeSpan.Zero, null, null, null);
        }

        var age = DateTimeOffset.UtcNow - data.LockedAt;
        var isStale = age > _staleLockThreshold;

        return new LockStatus(true, isStale, age, data.ProcessId, data.Hostname, data.Terminal);
    }

    /// <inheritdoc />
    public async Task ReleaseStaleLocksAsync(TimeSpan threshold, CancellationToken ct)
    {
        var lockFiles = Directory.GetFiles(_locksDirectory, "*.lock");

        foreach (var lockFile in lockFiles)
        {
            var json = await File.ReadAllTextAsync(lockFile, ct).ConfigureAwait(false);
            var data = JsonSerializer.Deserialize<LockData>(json);

            if (data is null)
            {
                continue;
            }

            var age = DateTimeOffset.UtcNow - data.LockedAt;

            if (age > threshold)
            {
                _logger.LogWarning(
                    "Removing stale lock: {LockFile}, Age={Age}s, PID={ProcessId}",
                    lockFile,
                    age.TotalSeconds,
                    data.ProcessId);

                File.Delete(lockFile);
            }
        }
    }

    /// <inheritdoc />
    public async Task ForceUnlockAsync(WorktreeId worktreeId, CancellationToken ct)
    {
        var lockFilePath = _pathResolver.GetLockFilePath(worktreeId);

        if (File.Exists(lockFilePath))
        {
            File.Delete(lockFilePath);
            _logger.LogWarning("Force-unlocked worktree {Worktree}", worktreeId);
        }

        await Task.CompletedTask.ConfigureAwait(false);
    }

    /// <summary>
    /// Disposable lock handle that releases the lock on dispose.
    /// </summary>
    private sealed class FileLock : IAsyncDisposable
    {
        private readonly string _lockFilePath;
        private readonly ILogger _logger;
        private bool _disposed;

        public FileLock(string lockFilePath, ILogger logger)
        {
            _lockFilePath = lockFilePath;
            _logger = logger;
        }

        public ValueTask DisposeAsync()
        {
            if (_disposed)
            {
                return ValueTask.CompletedTask;
            }

            try
            {
                if (File.Exists(_lockFilePath))
                {
                    File.Delete(_lockFilePath);
                    _logger.LogInformation("Lock released: {LockFile}", _lockFilePath);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to release lock: {LockFile}", _lockFilePath);
            }

            _disposed = true;
            return ValueTask.CompletedTask;
        }
    }
}
