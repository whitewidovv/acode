// src/Acode.Application/Interfaces/Persistence/IUnitOfWorkFactory.cs
namespace Acode.Application.Interfaces.Persistence;

using System.Data;

/// <summary>
/// Factory for creating Unit of Work instances with transactions.
/// </summary>
public interface IUnitOfWorkFactory
{
    /// <summary>
    /// Creates a new unit of work with default isolation level (ReadCommitted).
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A new unit of work with an active transaction.</returns>
    /// <exception cref="Domain.Exceptions.DatabaseException">If connection or transaction creation fails.</exception>
    Task<IUnitOfWork> CreateAsync(CancellationToken ct);

    /// <summary>
    /// Creates a new unit of work with specified isolation level.
    /// </summary>
    /// <param name="isolationLevel">Transaction isolation level.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A new unit of work with an active transaction at the specified isolation level.</returns>
    /// <exception cref="Domain.Exceptions.DatabaseException">If connection or transaction creation fails.</exception>
    Task<IUnitOfWork> CreateAsync(IsolationLevel isolationLevel, CancellationToken ct);
}
