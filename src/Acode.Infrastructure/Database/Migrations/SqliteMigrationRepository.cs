#pragma warning disable CA2007 // Consider calling ConfigureAwait on the awaited task - await using doesn't support ConfigureAwait

using Acode.Application.Database;
using Microsoft.Extensions.Logging;

namespace Acode.Infrastructure.Database.Migrations;

/// <summary>
/// SQLite implementation of the migration repository.
/// </summary>
/// <remarks>
/// Manages the __migrations table for tracking applied database migrations.
/// Uses Dapper for simple SQL execution via IDbConnection abstraction.
/// </remarks>
public sealed class SqliteMigrationRepository : IMigrationRepository
{
    private readonly IConnectionFactory _connectionFactory;
    private readonly ILogger<SqliteMigrationRepository> _logger;

    public SqliteMigrationRepository(
        IConnectionFactory connectionFactory,
        ILogger<SqliteMigrationRepository> logger)
    {
        ArgumentNullException.ThrowIfNull(connectionFactory);
        ArgumentNullException.ThrowIfNull(logger);

        _connectionFactory = connectionFactory;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<bool> EnsureMigrationsTableExistsAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.CreateAsync(cancellationToken).ConfigureAwait(false);

        const string checkTableSql = @"
            SELECT COUNT(*)
            FROM sqlite_master
            WHERE type='table' AND name='__migrations';";

        var exists = await connection.QuerySingleAsync<int>(checkTableSql, cancellationToken: cancellationToken).ConfigureAwait(false);

        if (exists > 0)
        {
            _logger.LogDebug("__migrations table already exists");
            return false;
        }

        const string createTableSql = @"
            CREATE TABLE __migrations (
                version TEXT PRIMARY KEY NOT NULL,
                checksum TEXT NOT NULL,
                applied_at TEXT NOT NULL,
                duration_ms INTEGER NOT NULL,
                applied_by TEXT,
                status TEXT NOT NULL DEFAULT 'Applied'
            );
            CREATE INDEX idx_migrations_applied_at ON __migrations(applied_at);";

        await connection.ExecuteAsync(createTableSql, cancellationToken: cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Created __migrations table");
        return true;
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<AppliedMigration>> GetAppliedMigrationsAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.CreateAsync(cancellationToken).ConfigureAwait(false);

        const string sql = @"
            SELECT version AS Version, checksum AS Checksum, applied_at AS AppliedAt,
                   duration_ms AS DurationMs, applied_by AS AppliedBy, status AS Status
            FROM __migrations
            ORDER BY version ASC;";

        var rows = await connection.QueryAsync<MigrationRow>(sql, cancellationToken: cancellationToken).ConfigureAwait(false);

        return rows.Select(MapToAppliedMigration).ToList();
    }

    /// <inheritdoc/>
    public async Task<AppliedMigration?> GetAppliedMigrationAsync(string version, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(version);

        await using var connection = await _connectionFactory.CreateAsync(cancellationToken).ConfigureAwait(false);

        const string sql = @"
            SELECT version AS Version, checksum AS Checksum, applied_at AS AppliedAt,
                   duration_ms AS DurationMs, applied_by AS AppliedBy, status AS Status
            FROM __migrations
            WHERE version = @Version;";

        var rows = await connection.QueryAsync<MigrationRow>(sql, new { Version = version }, cancellationToken).ConfigureAwait(false);
        var row = rows.FirstOrDefault();

        return row != null ? MapToAppliedMigration(row) : null;
    }

    /// <inheritdoc/>
    public async Task RecordMigrationAsync(AppliedMigration migration, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(migration);

        await using var connection = await _connectionFactory.CreateAsync(cancellationToken).ConfigureAwait(false);

        const string sql = @"
            INSERT INTO __migrations (version, checksum, applied_at, duration_ms, applied_by, status)
            VALUES (@Version, @Checksum, @AppliedAt, @DurationMs, @AppliedBy, @Status);";

        var parameters = new
        {
            migration.Version,
            migration.Checksum,
            AppliedAt = migration.AppliedAt.ToString("O"), // ISO 8601
            DurationMs = (long)migration.Duration.TotalMilliseconds,
            migration.AppliedBy,
            Status = migration.Status.ToString(),
        };

        await connection.ExecuteAsync(sql, parameters, cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Recorded migration {Version} (duration: {Duration}ms)", migration.Version, migration.Duration.TotalMilliseconds);
    }

    /// <inheritdoc/>
    public async Task<bool> RemoveMigrationAsync(string version, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(version);

        await using var connection = await _connectionFactory.CreateAsync(cancellationToken).ConfigureAwait(false);

        const string sql = "DELETE FROM __migrations WHERE version = @Version;";

        var rowsAffected = await connection.ExecuteAsync(sql, new { Version = version }, cancellationToken).ConfigureAwait(false);

        if (rowsAffected > 0)
        {
            _logger.LogInformation("Removed migration {Version}", version);
            return true;
        }

        _logger.LogWarning("Migration {Version} not found for removal", version);
        return false;
    }

    /// <inheritdoc/>
    public async Task<AppliedMigration?> GetLatestMigrationAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.CreateAsync(cancellationToken).ConfigureAwait(false);

        const string sql = @"
            SELECT version AS Version, checksum AS Checksum, applied_at AS AppliedAt,
                   duration_ms AS DurationMs, applied_by AS AppliedBy, status AS Status
            FROM __migrations
            ORDER BY version DESC
            LIMIT 1;";

        var rows = await connection.QueryAsync<MigrationRow>(sql, cancellationToken: cancellationToken).ConfigureAwait(false);
        var row = rows.FirstOrDefault();

        return row != null ? MapToAppliedMigration(row) : null;
    }

    /// <inheritdoc/>
    public async Task<bool> IsMigrationAppliedAsync(string version, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(version);

        await using var connection = await _connectionFactory.CreateAsync(cancellationToken).ConfigureAwait(false);

        const string sql = "SELECT COUNT(*) FROM __migrations WHERE version = @Version;";

        var count = await connection.QuerySingleAsync<int>(sql, new { Version = version }, cancellationToken).ConfigureAwait(false);

        return count > 0;
    }

    private static AppliedMigration MapToAppliedMigration(MigrationRow row)
    {
        return new AppliedMigration
        {
            Version = row.Version,
            Checksum = row.Checksum,
            AppliedAt = DateTime.Parse(row.AppliedAt, null, System.Globalization.DateTimeStyles.RoundtripKind),
            Duration = TimeSpan.FromMilliseconds(row.DurationMs),
            AppliedBy = row.AppliedBy,
            Status = Enum.Parse<MigrationStatus>(row.Status),
        };
    }

    private sealed class MigrationRow
    {
        public string Version { get; set; } = string.Empty;

        public string Checksum { get; set; } = string.Empty;

        public string AppliedAt { get; set; } = string.Empty;

        public long DurationMs { get; set; }

        public string? AppliedBy { get; set; }

        public string Status { get; set; } = "Applied";
    }
}
