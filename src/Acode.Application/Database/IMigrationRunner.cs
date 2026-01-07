// src/Acode.Application/Database/IMigrationRunner.cs
namespace Acode.Application.Database;

using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// High-level migration orchestration service.
/// </summary>
public interface IMigrationRunner
{
    /// <summary>
    /// Migrates the database to the latest version by applying all pending migrations.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Migration result.</returns>
    Task<MigrationResult> MigrateAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Rolls back the last applied migration.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Rollback result.</returns>
    Task<RollbackResult> RollbackAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current migration status including applied and pending migrations.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Migration status.</returns>
    Task<MigrationState> GetStatusAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates all applied migrations against discovered migration files.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Validation result.</returns>
    Task<ValidationResult> ValidateAsync(CancellationToken cancellationToken = default);
}
