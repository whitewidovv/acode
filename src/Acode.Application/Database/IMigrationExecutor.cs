// src/Acode.Application/Database/IMigrationExecutor.cs
namespace Acode.Application.Database;

/// <summary>
/// Executes database migrations with transaction support.
/// </summary>
public interface IMigrationExecutor
{
    /// <summary>
    /// Applies a migration by executing its up script.
    /// </summary>
    /// <param name="migration">Migration to apply.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result of migration execution.</returns>
    Task<MigrationExecutionResult> ApplyAsync(MigrationFile migration, CancellationToken cancellationToken = default);

    /// <summary>
    /// Rolls back a migration by executing its down script.
    /// </summary>
    /// <param name="migration">Migration to rollback.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result of migration rollback.</returns>
    Task<MigrationExecutionResult> RollbackAsync(MigrationFile migration, CancellationToken cancellationToken = default);
}
