namespace Acode.Application.Database;

/// <summary>
/// Represents a migration that has been applied to the database.
/// </summary>
/// <remarks>
/// Stored in the __migrations table to track which migrations have been executed.
/// Includes checksum for integrity validation.
/// </remarks>
public sealed record AppliedMigration
{
    /// <summary>
    /// Gets the migration version that was applied.
    /// </summary>
    public required string Version { get; init; }

    /// <summary>
    /// Gets the SHA-256 checksum of the migration file when it was applied.
    /// </summary>
    /// <remarks>
    /// Used to detect if the migration file has been modified after being applied.
    /// </remarks>
    public required string Checksum { get; init; }

    /// <summary>
    /// Gets the timestamp when this migration was applied.
    /// </summary>
    public required DateTime AppliedAt { get; init; }

    /// <summary>
    /// Gets how long the migration took to execute.
    /// </summary>
    public required TimeSpan Duration { get; init; }

    /// <summary>
    /// Gets the user or system that applied this migration (optional).
    /// </summary>
    public string? AppliedBy { get; init; }

    /// <summary>
    /// Gets the final status of this migration execution.
    /// </summary>
    public MigrationStatus Status { get; init; } = MigrationStatus.Applied;
}
