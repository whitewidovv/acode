using Acode.Application.Database;
using Microsoft.Extensions.Logging;

namespace Acode.Infrastructure.Database.Sqlite;

/// <summary>
/// Factory for creating SQLite database connections.
/// </summary>
/// <remarks>
/// Creates connections to embedded SQLite database with:
/// - Automatic directory creation.
/// - WAL mode enabled for concurrent reads.
/// - Configurable busy timeout.
/// - Proper resource cleanup via IAsyncDisposable.
/// </remarks>
public sealed class SqliteConnectionFactory : IConnectionFactory
{
    private readonly string _databasePath;
    private readonly ILogger<SqliteConnectionFactory> _logger;
    private readonly int _busyTimeoutMs;

    /// <summary>
    /// Initializes a new instance of the <see cref="SqliteConnectionFactory"/> class.
    /// </summary>
    /// <param name="databasePath">Path to the SQLite database file.</param>
    /// <param name="logger">Logger for diagnostics.</param>
    /// <param name="busyTimeoutMs">Busy timeout in milliseconds (default: 5000).</param>
    public SqliteConnectionFactory(
        string databasePath,
        ILogger<SqliteConnectionFactory> logger,
        int busyTimeoutMs = 5000)
    {
        ArgumentNullException.ThrowIfNull(databasePath);
        ArgumentNullException.ThrowIfNull(logger);

        _databasePath = databasePath;
        _logger = logger;
        _busyTimeoutMs = busyTimeoutMs;
    }

    /// <inheritdoc/>
    public DbProviderType ProviderType => DbProviderType.SQLite;

    /// <inheritdoc/>
    public string ConnectionString => $"Data Source={_databasePath}";

    /// <inheritdoc/>
    public async Task<Application.Database.IDbConnection> CreateAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // Ensure parent directory exists
        var directory = Path.GetDirectoryName(_databasePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
            _logger.LogInformation("Created database directory: {Directory}", directory);
        }

        // Create ADO.NET connection
        var sqliteConnection = new Microsoft.Data.Sqlite.SqliteConnection(ConnectionString);

        try
        {
            await sqliteConnection.OpenAsync(cancellationToken).ConfigureAwait(false);
            _logger.LogDebug("Opened SQLite connection: {DatabasePath}", _databasePath);

            // Enable WAL mode for concurrent reads
            using (var command = sqliteConnection.CreateCommand())
            {
                command.CommandText = "PRAGMA journal_mode=WAL;";
                await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            }

            // Set busy timeout
            using (var command = sqliteConnection.CreateCommand())
            {
                command.CommandText = $"PRAGMA busy_timeout={_busyTimeoutMs};";
                await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            }

            _logger.LogInformation("Configured SQLite connection (WAL mode, busy timeout: {BusyTimeoutMs}ms)", _busyTimeoutMs);

            // Wrap in our abstraction
            return new SqliteConnection(sqliteConnection, _logger);
        }
        catch
        {
            await sqliteConnection.DisposeAsync().ConfigureAwait(false);
            throw;
        }
    }
}
