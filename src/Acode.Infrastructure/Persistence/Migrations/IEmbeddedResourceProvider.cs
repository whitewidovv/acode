// src/Acode.Infrastructure/Persistence/Migrations/IEmbeddedResourceProvider.cs
namespace Acode.Infrastructure.Persistence.Migrations;

/// <summary>
/// Abstraction for discovering embedded migration resources.
/// </summary>
public interface IEmbeddedResourceProvider
{
    /// <summary>
    /// Gets all embedded migration resources from the assembly.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Array of embedded migration resources.</returns>
    Task<EmbeddedResource[]> GetMigrationResourcesAsync(CancellationToken ct = default);
}
