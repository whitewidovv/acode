using System.Data;

namespace Acode.Application.Database;

/// <summary>
/// Represents an abstract database connection for workspace operations.
/// </summary>
/// <remarks>
/// This interface abstracts the underlying database provider (SQLite, PostgreSQL)
/// and provides a consistent API for executing queries, commands, and transactions.
/// Implementations wrap ADO.NET IDbConnection with additional Acode-specific features.
/// </remarks>
public interface IDbConnection : IAsyncDisposable
{
    /// <summary>
    /// Gets the connection state.
    /// </summary>
    /// <remarks>
    /// State should be Open after CreateAsync returns.
    /// Implementations must track state correctly throughout lifecycle.
    /// </remarks>
    ConnectionState State { get; }

    /// <summary>
    /// Gets the database provider type for this connection.
    /// </summary>
    DatabaseProvider ProviderType { get; }

    /// <summary>
    /// Executes a SQL command and returns the number of rows affected.
    /// </summary>
    /// <param name="sql">The SQL command to execute.</param>
    /// <param name="parameters">Optional parameters for the command.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The number of rows affected.</returns>
    /// <remarks>
    /// Use for INSERT, UPDATE, DELETE operations.
    /// Parameters prevent SQL injection and improve performance.
    /// </remarks>
    Task<int> ExecuteAsync(
        string sql,
        object? parameters = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes a query and returns a single scalar value.
    /// </summary>
    /// <typeparam name="T">The type of the scalar value.</typeparam>
    /// <param name="sql">The SQL query to execute.</param>
    /// <param name="parameters">Optional parameters for the query.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The scalar value of type T.</returns>
    /// <remarks>
    /// Use for COUNT, MAX, AVG, or other aggregate queries.
    /// Returns first column of first row.
    /// </remarks>
    Task<T> QuerySingleAsync<T>(
        string sql,
        object? parameters = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes a query and returns all matching rows.
    /// </summary>
    /// <typeparam name="T">The type to map rows to.</typeparam>
    /// <param name="sql">The SQL query to execute.</param>
    /// <param name="parameters">Optional parameters for the query.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Collection of rows mapped to type T.</returns>
    /// <remarks>
    /// Uses Dapper for efficient object mapping.
    /// Returns empty collection if no rows match.
    /// </remarks>
    Task<IEnumerable<T>> QueryAsync<T>(
        string sql,
        object? parameters = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Begins a database transaction.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Transaction instance for commit/rollback.</returns>
    /// <remarks>
    /// Transaction must be explicitly committed to persist changes.
    /// Rollback occurs automatically on disposal if not committed.
    /// Nested transactions are not supported.
    /// </remarks>
    Task<ITransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);
}
