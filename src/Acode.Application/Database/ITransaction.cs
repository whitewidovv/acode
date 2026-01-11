namespace Acode.Application.Database;

/// <summary>
/// Represents a database transaction scope.
/// </summary>
/// <remarks>
/// Transactions provide ACID properties for multi-statement operations.
/// Must be explicitly committed to persist changes.
/// Rollback occurs automatically on disposal if not committed.
/// Nested transactions are not supported.
/// </remarks>
public interface ITransaction : IAsyncDisposable
{
    /// <summary>
    /// Commits the transaction, persisting all changes.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Task representing the asynchronous operation.</returns>
    /// <remarks>
    /// After commit, the transaction is complete and cannot be rolled back.
    /// Attempting to commit twice throws InvalidOperationException.
    /// </remarks>
    Task CommitAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Rolls back the transaction, discarding all changes.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Task representing the asynchronous operation.</returns>
    /// <remarks>
    /// After rollback, the transaction is complete and cannot be committed.
    /// Rollback is called automatically on disposal if not explicitly committed.
    /// </remarks>
    Task RollbackAsync(CancellationToken cancellationToken = default);
}
