namespace Acode.Application.Database;

/// <summary>
/// Represents a discovered migration file with its content and metadata.
/// </summary>
/// <remarks>
/// Migration files contain SQL DDL/DML statements to modify database schema.
/// Optionally include "down" scripts for rollback capability.
/// </remarks>
public sealed record MigrationFile
{
    /// <summary>
    /// Gets the migration version (e.g., "001", "002", "047").
    /// </summary>
    public required string Version { get; init; }

    /// <summary>
    /// Gets the SQL content for applying the migration (up script).
    /// </summary>
    public required string UpContent { get; init; }

    /// <summary>
    /// Gets the SQL content for rolling back the migration (down script), if available.
    /// </summary>
    public string? DownContent { get; init; }

    /// <summary>
    /// Gets the source location of this migration.
    /// </summary>
    public required MigrationSource Source { get; init; }

    /// <summary>
    /// Gets a value indicating whether this migration has a down script for rollback.
    /// </summary>
    public bool HasDownScript => DownContent is not null;

    /// <summary>
    /// Gets the SHA-256 checksum of the up script content.
    /// </summary>
    /// <remarks>
    /// Used to detect tampering or accidental modification of applied migrations.
    /// </remarks>
    public required string Checksum { get; init; }

    /// <summary>
    /// Gets the human-readable description of this migration (optional).
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Gets the author who created this migration (optional).
    /// </summary>
    public string? Author { get; init; }

    /// <summary>
    /// Gets the creation timestamp of this migration (optional).
    /// </summary>
    public DateTime? CreatedAt { get; init; }
}
