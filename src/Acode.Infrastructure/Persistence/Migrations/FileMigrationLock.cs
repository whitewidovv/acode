// src/Acode.Infrastructure/Persistence/Migrations/FileMigrationLock.cs
namespace Acode.Infrastructure.Persistence.Migrations;

using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Acode.Application.Database;

/// <summary>
/// File-based migration lock for preventing concurrent migrations.
/// </summary>
/// <remarks>
/// Uses a lock file with process information to prevent concurrent migration execution.
/// Suitable for SQLite and as a fallback for other database types.
/// </remarks>
public sealed class FileMigrationLock : IMigrationLock
{
    private readonly string _lockFilePath;
    private readonly TimeSpan _timeout;
    private FileStream? _lockFileStream;
    private LockInfo? _acquiredLockInfo;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="FileMigrationLock"/> class.
    /// </summary>
    /// <param name="lockFilePath">Path to the lock file.</param>
    /// <param name="timeout">Lock acquisition timeout.</param>
    public FileMigrationLock(string lockFilePath, TimeSpan timeout)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(lockFilePath);

        _lockFilePath = lockFilePath;
        _timeout = timeout;
    }

    /// <inheritdoc/>
    public async Task<bool> TryAcquireAsync(CancellationToken ct = default)
    {
        var startTime = DateTime.UtcNow;

        while (DateTime.UtcNow - startTime < _timeout)
        {
            try
            {
                // Check if lock file exists and is stale
                if (File.Exists(_lockFilePath))
                {
                    var existingLock = await ReadLockInfoFromFileAsync(ct).ConfigureAwait(false);
                    if (existingLock != null && DateTime.UtcNow - existingLock.AcquiredAt > _timeout)
                    {
                        // Stale lock, remove it
                        File.Delete(_lockFilePath);
                    }
                    else
                    {
                        // Active lock held by another process
                        await Task.Delay(100, ct).ConfigureAwait(false);
                        continue;
                    }
                }

                // Ensure directory exists
                var directory = Path.GetDirectoryName(_lockFilePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // Try to acquire lock by opening file exclusively
                _lockFileStream = new FileStream(
                    _lockFilePath,
                    FileMode.Create,
                    FileAccess.Write,
                    FileShare.None);

                // Write lock info
                var lockInfo = new LockInfo(
                    LockId: Guid.NewGuid().ToString(),
                    HolderId: Environment.ProcessId.ToString(),
                    AcquiredAt: DateTime.UtcNow,
                    MachineName: Environment.MachineName);

                // Store lock info for later retrieval
                _acquiredLockInfo = lockInfo;

                var jsonBytes = JsonSerializer.SerializeToUtf8Bytes(lockInfo);
                await _lockFileStream.WriteAsync(jsonBytes, ct).ConfigureAwait(false);
                await _lockFileStream.FlushAsync(ct).ConfigureAwait(false);

                return true;
            }
            catch (IOException)
            {
                // File is locked by another process, retry
                await Task.Delay(100, ct).ConfigureAwait(false);
            }
        }

        return false;
    }

    /// <inheritdoc/>
    public Task ForceReleaseAsync(CancellationToken ct = default)
    {
        if (File.Exists(_lockFilePath))
        {
            File.Delete(_lockFilePath);
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public async Task<LockInfo?> GetLockInfoAsync(CancellationToken ct = default)
    {
        // If this instance holds the lock, return the stored info
        if (_acquiredLockInfo != null)
        {
            return _acquiredLockInfo;
        }

        // Otherwise, read from file (if it exists)
        if (!File.Exists(_lockFilePath))
        {
            return null;
        }

        return await ReadLockInfoFromFileAsync(ct).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        if (_lockFileStream != null)
        {
            await _lockFileStream.DisposeAsync().ConfigureAwait(false);
            _lockFileStream = null;
        }

        if (File.Exists(_lockFilePath))
        {
            try
            {
                File.Delete(_lockFilePath);
            }
            catch (IOException)
            {
                // Lock file may be in use, ignore
            }
        }

        _disposed = true;
    }

    private async Task<LockInfo?> ReadLockInfoFromFileAsync(CancellationToken ct)
    {
        try
        {
            var json = await File.ReadAllTextAsync(_lockFilePath, ct).ConfigureAwait(false);
            return JsonSerializer.Deserialize<LockInfo>(json);
        }
        catch (Exception)
        {
            return null;
        }
    }
}
