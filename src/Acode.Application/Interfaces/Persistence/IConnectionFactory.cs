// src/Acode.Application/Interfaces/Persistence/IConnectionFactory.cs
namespace Acode.Application.Interfaces.Persistence;

using System.Data;
using Acode.Domain.Enums;

/// <summary>
/// Factory for creating database connections.
/// Implementations are responsible for connection configuration,
/// pooling (PostgreSQL), and PRAGMA settings (SQLite).
/// </summary>
public interface IConnectionFactory
{
    /// <summary>
    /// Gets the type of database this factory creates connections for.
    /// </summary>
    DatabaseType DatabaseType { get; }

    /// <summary>
    /// Creates and opens a new database connection.
    /// The returned connection is in Open state and ready for use.
    /// Caller is responsible for disposing the connection.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>An open database connection.</returns>
    /// <exception cref="Domain.Exceptions.DatabaseException">When connection cannot be established.</exception>
    /// <exception cref="OperationCanceledException">When cancelled.</exception>
    Task<IDbConnection> CreateAsync(CancellationToken ct);
}
