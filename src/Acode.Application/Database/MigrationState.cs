// src/Acode.Application/Database/MigrationState.cs
namespace Acode.Application.Database;

using System.Collections.Generic;

/// <summary>
/// Current migration state including applied and pending migrations.
/// </summary>
public sealed record MigrationState
{
    /// <summary>
    /// Gets the list of applied migrations.
    /// </summary>
    public required IReadOnlyList<AppliedMigration> AppliedMigrations { get; init; }

    /// <summary>
    /// Gets the list of pending migrations.
    /// </summary>
    public required IReadOnlyList<MigrationFile> PendingMigrations { get; init; }

    /// <summary>
    /// Gets the list of checksum mismatches detected.
    /// </summary>
    public required IReadOnlyList<ChecksumMismatch> ChecksumMismatches { get; init; }

    /// <summary>
    /// Gets the list of version gaps detected.
    /// </summary>
    public required IReadOnlyList<VersionGap> VersionGaps { get; init; }
}
