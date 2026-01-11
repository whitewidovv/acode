// src/Acode.Infrastructure/Persistence/Connections/SqliteConnectionFactory.cs
namespace Acode.Infrastructure.Persistence.Connections;

using System.Data;
using System.Diagnostics;
using Acode.Application.Interfaces.Persistence;
using Acode.Domain.Enums;
using Acode.Domain.Exceptions;
using Acode.Infrastructure.Configuration;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

/// <summary>
/// Connection factory for SQLite embedded database.
/// Handles directory creation, PRAGMA configuration, and connection pooling.
/// </summary>
public sealed class SqliteConnectionFactory : IConnectionFactory
{
    private readonly LocalDatabaseOptions _options;
    private readonly ILogger<SqliteConnectionFactory> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="SqliteConnectionFactory"/> class.
    /// </summary>
    /// <param name="options">Database configuration options.</param>
    /// <param name="logger">Logger instance.</param>
    public SqliteConnectionFactory(
        IOptions<DatabaseOptions> options,
        ILogger<SqliteConnectionFactory> logger)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        _options = options.Value.Local;
        _logger = logger;

        // Ensure directory exists
        var directory = Path.GetDirectoryName(_options.Path);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
            _logger.LogDebug("Created database directory: {Directory}", directory);
        }
    }

    /// <inheritdoc/>
    public DatabaseType DatabaseType => DatabaseType.Sqlite;

    /// <inheritdoc/>
    public async Task<IDbConnection> CreateAsync(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var sw = Stopwatch.StartNew();

        try
        {
            var connectionString = new SqliteConnectionStringBuilder
            {
                DataSource = _options.Path,
                Mode = SqliteOpenMode.ReadWriteCreate,
                Cache = SqliteCacheMode.Shared,
            }.ToString();

            var connection = new SqliteConnection(connectionString);
            await connection.OpenAsync(ct).ConfigureAwait(false);

            // Configure SQLite pragmas for performance and safety
            await ExecutePragmaAsync(connection, "journal_mode", _options.WalMode ? "WAL" : "DELETE", ct).ConfigureAwait(false);
            await ExecutePragmaAsync(connection, "busy_timeout", _options.BusyTimeoutMs.ToString(), ct).ConfigureAwait(false);
            await ExecutePragmaAsync(connection, "foreign_keys", "ON", ct).ConfigureAwait(false);
            await ExecutePragmaAsync(connection, "synchronous", "NORMAL", ct).ConfigureAwait(false);

            sw.Stop();

            _logger.LogDebug(
                "SQLite connection opened. Path={Path}, WAL={WalMode}, Duration={Duration}ms",
                _options.Path,
                _options.WalMode,
                sw.ElapsedMilliseconds);

            return connection;
        }
        catch (SqliteException ex)
        {
            sw.Stop();
            _logger.LogError(
                ex,
                "Failed to open SQLite connection. Path={Path}, Duration={Duration}ms",
                _options.Path,
                sw.ElapsedMilliseconds);

            throw DatabaseException.ConnectionFailed(
                $"SQLite connection failed: {ex.Message}",
                ex);
        }
    }

    private static async Task ExecutePragmaAsync(
        SqliteConnection connection,
        string pragma,
        string value,
        CancellationToken ct)
    {
        var cmd = connection.CreateCommand();
        await using (cmd.ConfigureAwait(false))
        {
            cmd.CommandText = $"PRAGMA {pragma} = {value};";
            await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
        }
    }
}
