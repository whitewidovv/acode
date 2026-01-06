// src/Acode.Application/Database/MigrationStatusReport.cs
namespace Acode.Application.Database;

/// <summary>
/// Report on current migration status including applied and pending migrations.
/// </summary>
public sealed record MigrationStatusReport
{
    /// <summary>
    /// Gets the current migration version (highest applied version).
    /// </summary>
    public required string? CurrentVersion { get; init; }

    /// <summary>
    /// Gets the list of migrations that have been applied to the database.
    /// </summary>
    public required IReadOnlyList<AppliedMigration> AppliedMigrations { get; init; }

    /// <summary>
    /// Gets the list of migrations that are pending (not yet applied).
    /// </summary>
    public required IReadOnlyList<MigrationFile> PendingMigrations { get; init; }

    /// <summary>
    /// Gets the database provider name (e.g., "SQLite", "PostgreSQL").
    /// </summary>
    public required string DatabaseProvider { get; init; }

    /// <summary>
    /// Gets a value indicating whether all applied migrations have valid checksums.
    /// </summary>
    public required bool ChecksumsValid { get; init; }

    /// <summary>
    /// Gets warnings about checksum mismatches or validation issues (optional).
    /// </summary>
    public IReadOnlyList<string>? ChecksumWarnings { get; init; }
}
