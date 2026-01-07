// src/Acode.Infrastructure/Persistence/Migrations/PostgreSqlAdvisoryLock.cs
namespace Acode.Infrastructure.Persistence.Migrations;

using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Acode.Application.Database;

/// <summary>
/// PostgreSQL advisory lock for preventing concurrent migrations.
/// </summary>
/// <remarks>
/// Uses PostgreSQL's advisory lock functions (pg_try_advisory_lock) for
/// session-level locking. Advisory locks are automatically released when
/// the connection closes.
/// </remarks>
public sealed class PostgreSqlAdvisoryLock : IMigrationLock
{
    private const string LockName = "acode_migration_lock";
    private readonly System.Data.IDbConnection _connection;
    private readonly TimeSpan _timeout;
    private readonly long _lockId;
    private bool _lockAcquired;
    private LockInfo? _acquiredLockInfo;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="PostgreSqlAdvisoryLock"/> class.
    /// </summary>
    /// <param name="connection">Database connection.</param>
    /// <param name="timeout">Lock acquisition timeout.</param>
    public PostgreSqlAdvisoryLock(System.Data.IDbConnection connection, TimeSpan timeout)
    {
        ArgumentNullException.ThrowIfNull(connection);

        _connection = connection;
        _timeout = timeout;
        _lockId = GenerateLockId(LockName);
    }

    /// <inheritdoc/>
    public async Task<bool> TryAcquireAsync(CancellationToken ct = default)
    {
        var startTime = DateTime.UtcNow;

        while (DateTime.UtcNow - startTime < _timeout)
        {
            if (await TryAcquireLockInternalAsync(ct).ConfigureAwait(false))
            {
                _lockAcquired = true;
                _acquiredLockInfo = new LockInfo(
                    LockId: _lockId.ToString(),
                    HolderId: Environment.ProcessId.ToString(),
                    AcquiredAt: DateTime.UtcNow,
                    MachineName: Environment.MachineName);
                return true;
            }

            await Task.Delay(100, ct).ConfigureAwait(false);
        }

        return false;
    }

    /// <inheritdoc/>
    public Task ForceReleaseAsync(CancellationToken ct = default)
    {
        using var command = _connection.CreateCommand();
        command.CommandText = "SELECT pg_advisory_unlock_all()";
        command.ExecuteNonQuery();

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task<LockInfo?> GetLockInfoAsync(CancellationToken ct = default)
    {
        return Task.FromResult(_acquiredLockInfo);
    }

    /// <inheritdoc/>
    public ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return ValueTask.CompletedTask;
        }

        if (_lockAcquired)
        {
            try
            {
                using var command = _connection.CreateCommand();
                command.CommandText = $"SELECT pg_advisory_unlock({_lockId})";
                command.ExecuteNonQuery();
            }
            catch (Exception)
            {
                // Ignore errors during cleanup - connection might be closed
            }
        }

        _disposed = true;
        return ValueTask.CompletedTask;
    }

    private static long GenerateLockId(string lockName)
    {
        // Generate a stable 64-bit hash from the lock name
        var bytes = Encoding.UTF8.GetBytes(lockName);
        var hash = SHA256.HashData(bytes);

        // Take first 8 bytes and convert to long
        return BitConverter.ToInt64(hash, 0);
    }

    private async Task<bool> TryAcquireLockInternalAsync(CancellationToken ct)
    {
        try
        {
            using var command = _connection.CreateCommand();
            command.CommandText = $"SELECT pg_try_advisory_lock({_lockId})";

            using var reader = command.ExecuteReader();
            if (await Task.Run(() => reader.Read(), ct).ConfigureAwait(false))
            {
                return reader.GetBoolean(0);
            }

            return false;
        }
        catch (Exception)
        {
            return false;
        }
    }
}
