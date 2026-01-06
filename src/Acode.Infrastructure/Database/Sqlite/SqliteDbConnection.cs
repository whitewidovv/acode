using System.Data;
using Acode.Application.Database;
using Dapper;
using Microsoft.Extensions.Logging;

namespace Acode.Infrastructure.Database.Sqlite;

/// <summary>
/// Wrapper around ADO.NET SqliteConnection implementing IDbConnection.
/// </summary>
/// <remarks>
/// Provides async query/execute methods using Dapper for efficient object mapping.
/// Manages transaction lifecycle and proper resource cleanup.
/// </remarks>
public sealed class SqliteDbConnection : Application.Database.IDbConnection
{
    private readonly Microsoft.Data.Sqlite.SqliteConnection _connection;
    private readonly ILogger _logger;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="SqliteDbConnection"/> class.
    /// </summary>
    /// <param name="connection">The underlying ADO.NET SqliteConnection.</param>
    /// <param name="logger">Logger for diagnostics.</param>
    public SqliteDbConnection(Microsoft.Data.Sqlite.SqliteConnection connection, ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentNullException.ThrowIfNull(logger);

        _connection = connection;
        _logger = logger;
    }

    /// <inheritdoc/>
    public ConnectionState State => _connection.State;

    /// <inheritdoc/>
    public DatabaseProvider ProviderType => DatabaseProvider.SQLite;

    /// <inheritdoc/>
    public async Task<int> ExecuteAsync(
        string sql,
        object? parameters = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(sql);
        ThrowIfDisposed();

        _logger.LogDebug("Executing SQL: {Sql}", sql);
        var commandDefinition = new CommandDefinition(sql, parameters, cancellationToken: cancellationToken);
        return await _connection.ExecuteAsync(commandDefinition).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<T> QuerySingleAsync<T>(
        string sql,
        object? parameters = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(sql);
        ThrowIfDisposed();

        _logger.LogDebug("Querying single: {Sql}", sql);
        var commandDefinition = new CommandDefinition(sql, parameters, cancellationToken: cancellationToken);
        return await _connection.QuerySingleAsync<T>(commandDefinition).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<T>> QueryAsync<T>(
        string sql,
        object? parameters = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(sql);
        ThrowIfDisposed();

        _logger.LogDebug("Querying: {Sql}", sql);
        var commandDefinition = new CommandDefinition(sql, parameters, cancellationToken: cancellationToken);
        return await _connection.QueryAsync<T>(commandDefinition).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<ITransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        var transaction = (Microsoft.Data.Sqlite.SqliteTransaction)await _connection.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        _logger.LogDebug("Transaction started");
        return new SqliteTransaction(transaction, _logger);
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        await _connection.DisposeAsync().ConfigureAwait(false);
        _logger.LogDebug("Connection disposed");
        _disposed = true;
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(SqliteDbConnection));
        }
    }
}
