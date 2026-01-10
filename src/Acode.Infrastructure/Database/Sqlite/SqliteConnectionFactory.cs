#pragma warning disable CA2007 // Library/infrastructure code does not depend on a synchronization context and this file may use 'await using', which cannot call ConfigureAwait; CA2007 is intentionally disabled for the entire file.

using Acode.Application.Database;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Acode.Infrastructure.Database.Sqlite;

/// <summary>
/// Factory for creating SQLite database connections with optimal configuration.
/// </summary>
/// <remarks>
/// Creates connections to embedded SQLite database with:
/// - Automatic directory creation.
/// - WAL mode enabled for concurrent reads.
/// - Foreign key enforcement.
/// - Performance optimizations (synchronous=NORMAL, temp_store=MEMORY, mmap).
/// - Health checking support.
/// </remarks>
public sealed class SqliteConnectionFactory : IConnectionFactory, IDisposable
{
    private readonly DatabaseOptions _options;
    private readonly ILogger<SqliteConnectionFactory> _logger;
    private readonly string _connectionString;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="SqliteConnectionFactory"/> class.
    /// </summary>
    /// <param name="options">Database configuration options.</param>
    /// <param name="logger">Logger for diagnostics.</param>
    public SqliteConnectionFactory(
        IOptions<DatabaseOptions> options,
        ILogger<SqliteConnectionFactory> logger)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        _options = options.Value;
        _logger = logger;

        var dbPath = _options.Local?.Path
            ?? Path.Combine(Environment.CurrentDirectory, ".agent", "data", "workspace.db");

        // Ensure directory exists
        var directory = Path.GetDirectoryName(dbPath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
            _logger.LogDebug("Created database directory: {Directory}", directory);
        }

        _connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = dbPath,
            Mode = SqliteOpenMode.ReadWriteCreate,
            Cache = SqliteCacheMode.Shared,
        }.ToString();

        _logger.LogDebug("SQLite connection factory initialized. Path: {Path}", dbPath);
    }

    /// <inheritdoc/>
    public DatabaseProvider Provider => DatabaseProvider.SQLite;

    /// <inheritdoc/>
    public async Task<Application.Database.IDbConnection> CreateAsync(CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        try
        {
            var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync(ct).ConfigureAwait(false);

            // Configure connection for optimal performance
            await ConfigureConnectionAsync(connection, ct).ConfigureAwait(false);

            _logger.LogTrace("SQLite connection created successfully");
            return new SqliteDbConnection(connection, _logger);
        }
        catch (SqliteException ex)
        {
            _logger.LogError(ex, "Failed to create SQLite connection");
            throw new DatabaseConnectionException(
                "ACODE-DB-001",
                $"Failed to connect to SQLite database: {ex.Message}",
                ex);
        }
    }

    /// <inheritdoc/>
    public async Task<HealthCheckResult> CheckHealthAsync(CancellationToken ct = default)
    {
        var data = new Dictionary<string, object>();

        try
        {
            // Check file exists
            var dbPath = new SqliteConnectionStringBuilder(_connectionString).DataSource;
            if (!File.Exists(dbPath))
            {
                return new HealthCheckResult(
                    HealthStatus.Unhealthy,
                    $"Database file not found: {dbPath}",
                    data);
            }

            data["path"] = dbPath;
            data["size_bytes"] = new FileInfo(dbPath).Length;

            // Test connection
            await using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync(ct);

            // Run integrity check (quick version)
            await using var cmd = connection.CreateCommand();
            cmd.CommandText = "PRAGMA quick_check;";
            var result = await cmd.ExecuteScalarAsync(ct);

            if (result?.ToString() != "ok")
            {
                return new HealthCheckResult(
                    HealthStatus.Degraded,
                    $"Database integrity check returned: {result}",
                    data);
            }

            // Get WAL size
            var walPath = dbPath + "-wal";
            if (File.Exists(walPath))
            {
                data["wal_size_bytes"] = new FileInfo(walPath).Length;
            }

            return new HealthCheckResult(
                HealthStatus.Healthy,
                "SQLite database is healthy",
                data);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "SQLite health check failed");
            return new HealthCheckResult(
                HealthStatus.Unhealthy,
                $"Health check failed: {ex.Message}",
                data);
        }
    }

    /// <summary>
    /// Disposes resources used by this factory.
    /// </summary>
    public void Dispose()
    {
        _disposed = true;
    }

    /// <summary>
    /// Executes a PRAGMA statement on the connection.
    /// </summary>
    /// <param name="connection">The connection.</param>
    /// <param name="pragma">The pragma name.</param>
    /// <param name="value">The pragma value.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    private static async Task ExecutePragmaAsync(
        SqliteConnection connection,
        string pragma,
        string value,
        CancellationToken ct)
    {
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = $"PRAGMA {pragma}={value};";
        await cmd.ExecuteNonQueryAsync(ct);
    }

    /// <summary>
    /// Configures a new SQLite connection with optimal settings.
    /// </summary>
    /// <param name="connection">The connection to configure.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    private async Task ConfigureConnectionAsync(SqliteConnection connection, CancellationToken ct)
    {
        // Enable WAL mode for better concurrency
        await ExecutePragmaAsync(connection, "journal_mode", "WAL", ct).ConfigureAwait(false);

        // Set busy timeout
        var busyTimeout = _options.Local?.BusyTimeoutMs ?? 5000;
        await ExecutePragmaAsync(connection, "busy_timeout", busyTimeout.ToString(), ct).ConfigureAwait(false);

        // Enable foreign keys
        await ExecutePragmaAsync(connection, "foreign_keys", "ON", ct).ConfigureAwait(false);

        // Optimize for performance
        await ExecutePragmaAsync(connection, "synchronous", "NORMAL", ct).ConfigureAwait(false);
        await ExecutePragmaAsync(connection, "temp_store", "MEMORY", ct).ConfigureAwait(false);
        await ExecutePragmaAsync(connection, "mmap_size", "268435456", ct).ConfigureAwait(false); // 256MB
    }
}
