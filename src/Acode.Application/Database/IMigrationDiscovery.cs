// src/Acode.Application/Database/IMigrationDiscovery.cs
namespace Acode.Application.Database;

/// <summary>
/// Interface for discovering migration files from various sources.
/// </summary>
public interface IMigrationDiscovery
{
    /// <summary>
    /// Discovers all available migrations from embedded resources and file system.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>List of all discovered migration files.</returns>
    Task<IReadOnlyList<MigrationFile>> DiscoverAsync(CancellationToken ct = default);

    /// <summary>
    /// Gets only pending (not yet applied) migrations.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>List of pending migration files.</returns>
    Task<IReadOnlyList<MigrationFile>> GetPendingAsync(CancellationToken ct = default);
}
