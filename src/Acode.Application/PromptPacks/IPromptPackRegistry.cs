using Acode.Domain.PromptPacks;

namespace Acode.Application.PromptPacks;

/// <summary>
/// Manages pack discovery, caching, and selection based on configuration.
/// All methods except ListPacks are async to support non-blocking operations.
/// </summary>
public interface IPromptPackRegistry
{
    /// <summary>
    /// Initializes the registry by discovering all available packs.
    /// Must be called before other methods are used.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A task representing the initialization.</returns>
    Task InitializeAsync(CancellationToken ct = default);

    /// <summary>
    /// Gets a pack by ID, loading and caching if not already loaded.
    /// </summary>
    /// <param name="packId">The pack ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The prompt pack.</returns>
    /// <exception cref="Acode.Domain.PromptPacks.Exceptions.PackNotFoundException">Thrown when the pack is not found.</exception>
    Task<PromptPack> GetPackAsync(string packId, CancellationToken ct = default);

    /// <summary>
    /// Attempts to get a pack by ID, returning null if not found.
    /// </summary>
    /// <param name="packId">The pack ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The pack if found; otherwise, <c>null</c>.</returns>
    Task<PromptPack?> TryGetPackAsync(string packId, CancellationToken ct = default);

    /// <summary>
    /// Lists all discovered packs with their metadata.
    /// This is synchronous as it only returns cached metadata.
    /// </summary>
    /// <returns>The list of pack information.</returns>
    IReadOnlyList<PromptPackInfo> ListPacks();

    /// <summary>
    /// Gets the currently active pack based on configuration.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The active prompt pack.</returns>
    Task<PromptPack> GetActivePackAsync(CancellationToken ct = default);

    /// <summary>
    /// Gets the ID of the currently active pack.
    /// This is synchronous as it only reads from configuration.
    /// </summary>
    /// <returns>The active pack ID.</returns>
    string GetActivePackId();

    /// <summary>
    /// Refreshes pack discovery and clears cache.
    /// Useful for hot reload during development.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A task representing the refresh operation.</returns>
    Task RefreshAsync(CancellationToken ct = default);
}
