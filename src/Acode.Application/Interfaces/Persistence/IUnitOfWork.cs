// src/Acode.Application/Interfaces/Persistence/IUnitOfWork.cs
namespace Acode.Application.Interfaces.Persistence;

using System.Data;

/// <summary>
/// Represents a unit of work with a shared transaction.
/// All operations using this UoW share the same transaction and connection.
/// </summary>
public interface IUnitOfWork : IAsyncDisposable
{
    /// <summary>Gets the shared database connection for this unit of work.</summary>
    IDbConnection Connection { get; }

    /// <summary>Gets the active transaction for this unit of work.</summary>
    IDbTransaction Transaction { get; }

    /// <summary>
    /// Commits all changes within this unit of work.
    /// After commit, this UoW should not be reused.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A task representing the asynchronous commit operation.</returns>
    /// <exception cref="InvalidOperationException">If already committed or rolled back.</exception>
    /// <exception cref="Domain.Exceptions.DatabaseException">If commit fails.</exception>
    Task CommitAsync(CancellationToken ct);

    /// <summary>
    /// Rolls back all changes within this unit of work.
    /// After rollback, this UoW should not be reused.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A task representing the asynchronous rollback operation.</returns>
    /// <exception cref="InvalidOperationException">If already committed or rolled back.</exception>
    /// <exception cref="Domain.Exceptions.DatabaseException">If rollback fails.</exception>
    Task RollbackAsync(CancellationToken ct);
}
