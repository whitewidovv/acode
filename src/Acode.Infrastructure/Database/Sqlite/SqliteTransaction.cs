using Acode.Application.Database;
using Microsoft.Extensions.Logging;

namespace Acode.Infrastructure.Database.Sqlite;

/// <summary>
/// Wrapper around ADO.NET SqliteTransaction implementing ITransaction.
/// </summary>
/// <remarks>
/// Provides explicit commit/rollback with automatic rollback on disposal if not committed.
/// Tracks transaction state to prevent double-commit/rollback.
/// </remarks>
public sealed class SqliteTransaction : ITransaction
{
    private readonly Microsoft.Data.Sqlite.SqliteTransaction _transaction;
    private readonly ILogger _logger;
    private bool _completed;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="SqliteTransaction"/> class.
    /// </summary>
    /// <param name="transaction">The underlying ADO.NET SqliteTransaction.</param>
    /// <param name="logger">Logger for diagnostics.</param>
    public SqliteTransaction(Microsoft.Data.Sqlite.SqliteTransaction transaction, ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(transaction);
        ArgumentNullException.ThrowIfNull(logger);

        _transaction = transaction;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task CommitAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        if (_completed)
        {
            throw new InvalidOperationException("Transaction has already been completed (committed or rolled back)");
        }

        await _transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
        _logger.LogDebug("Transaction committed");
        _completed = true;
    }

    /// <inheritdoc/>
    public async Task RollbackAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        if (_completed)
        {
            throw new InvalidOperationException("Transaction has already been completed (committed or rolled back)");
        }

        await _transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
        _logger.LogDebug("Transaction rolled back");
        _completed = true;
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        if (!_completed)
        {
            _logger.LogWarning("Transaction disposed without explicit commit - rolling back");
            await _transaction.RollbackAsync().ConfigureAwait(false);
        }

        await _transaction.DisposeAsync().ConfigureAwait(false);
        _disposed = true;
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(SqliteTransaction));
        }
    }
}
