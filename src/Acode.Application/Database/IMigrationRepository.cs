namespace Acode.Application.Database;

/// <summary>
/// Repository for tracking applied database migrations in the __migrations table.
/// </summary>
/// <remarks>
/// The __migrations table is the single source of truth for which migrations
/// have been executed. This repository provides CRUD operations for that table.
/// </remarks>
public interface IMigrationRepository
{
    /// <summary>
    /// Ensures the __migrations table exists in the database.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if table was created, false if it already existed.</returns>
    Task<bool> EnsureMigrationsTableExistsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all applied migrations ordered by version ascending.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of applied migrations.</returns>
    Task<IReadOnlyList<AppliedMigration>> GetAppliedMigrationsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a specific applied migration by version.
    /// </summary>
    /// <param name="version">Migration version to retrieve.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Applied migration if found, null otherwise.</returns>
    Task<AppliedMigration?> GetAppliedMigrationAsync(string version, CancellationToken cancellationToken = default);

    /// <summary>
    /// Records that a migration was successfully applied.
    /// </summary>
    /// <param name="migration">Migration execution record to store.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task RecordMigrationAsync(AppliedMigration migration, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a migration record (used during rollback).
    /// </summary>
    /// <param name="version">Version of migration to remove.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if migration was removed, false if not found.</returns>
    Task<bool> RemoveMigrationAsync(string version, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the latest (highest version) applied migration.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Latest migration if any exist, null if no migrations applied.</returns>
    Task<AppliedMigration?> GetLatestMigrationAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a specific migration version has been applied.
    /// </summary>
    /// <param name="version">Migration version to check.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if migration has been applied.</returns>
    Task<bool> IsMigrationAppliedAsync(string version, CancellationToken cancellationToken = default);
}
