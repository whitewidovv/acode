// src/Acode.Infrastructure/Persistence/Transactions/UnitOfWork.cs
namespace Acode.Infrastructure.Persistence.Transactions;

using System.Data;
using System.Diagnostics;
using Acode.Application.Interfaces.Persistence;
using Acode.Domain.Exceptions;
using Microsoft.Extensions.Logging;

/// <summary>
/// Represents a unit of work with a shared transaction.
/// Manages database connection and transaction lifecycle.
/// </summary>
public sealed class UnitOfWork : IUnitOfWork
{
    private readonly ILogger<UnitOfWork> _logger;
    private readonly Stopwatch _stopwatch;
    private bool _isCompleted;
    private bool _isDisposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="UnitOfWork"/> class.
    /// </summary>
    /// <param name="connection">Database connection (must be open).</param>
    /// <param name="isolationLevel">Transaction isolation level.</param>
    /// <param name="logger">Logger instance.</param>
    public UnitOfWork(
        IDbConnection connection,
        IsolationLevel isolationLevel,
        ILogger<UnitOfWork> logger)
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentNullException.ThrowIfNull(logger);

        Connection = connection;
        _logger = logger;
        _stopwatch = Stopwatch.StartNew();

        Transaction = connection.BeginTransaction(isolationLevel);

        _logger.LogDebug(
            "Transaction started. IsolationLevel={IsolationLevel}",
            isolationLevel);
    }

    /// <inheritdoc/>
    public IDbConnection Connection { get; }

    /// <inheritdoc/>
    public IDbTransaction Transaction { get; }

    /// <inheritdoc/>
    public Task CommitAsync(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        EnsureNotCompleted();

        try
        {
            Transaction.Commit();
            _isCompleted = true;

            _stopwatch.Stop();
            _logger.LogDebug(
                "Transaction committed. Duration={Duration}ms",
                _stopwatch.ElapsedMilliseconds);

            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Transaction commit failed");
            throw DatabaseException.TransactionFailed("commit", ex);
        }
    }

    /// <inheritdoc/>
    public Task RollbackAsync(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        EnsureNotCompleted();

        try
        {
            Transaction.Rollback();
            _isCompleted = true;

            _stopwatch.Stop();
            _logger.LogInformation(
                "Transaction rolled back. Duration={Duration}ms",
                _stopwatch.ElapsedMilliseconds);

            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Transaction rollback failed");
            throw DatabaseException.TransactionFailed("rollback", ex);
        }
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        if (_isDisposed)
        {
            return;
        }

        _isDisposed = true;

        if (!_isCompleted)
        {
            try
            {
                Transaction.Rollback();
                _logger.LogWarning(
                    "Transaction auto-rolled back on dispose. Duration={Duration}ms",
                    _stopwatch.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to rollback transaction on dispose");
            }
        }

        Transaction.Dispose();

        if (Connection is IAsyncDisposable asyncDisposable)
        {
            await asyncDisposable.DisposeAsync().ConfigureAwait(false);
        }
        else
        {
            Connection.Dispose();
        }
    }

    private void EnsureNotCompleted()
    {
        if (_isCompleted)
        {
            throw new InvalidOperationException(
                "Transaction has already been committed or rolled back");
        }
    }
}
