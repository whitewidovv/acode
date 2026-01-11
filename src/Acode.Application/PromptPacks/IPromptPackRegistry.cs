using Acode.Domain.PromptPacks;

namespace Acode.Application.PromptPacks;

/// <summary>
/// Interface for the prompt pack registry that indexes and provides access to packs.
/// </summary>
public interface IPromptPackRegistry
{
    /// <summary>
    /// Gets a pack by ID.
    /// </summary>
    /// <param name="packId">The pack ID.</param>
    /// <returns>The prompt pack.</returns>
    /// <exception cref="Acode.Domain.PromptPacks.Exceptions.PackNotFoundException">Thrown when the pack is not found.</exception>
    PromptPack GetPack(string packId);

    /// <summary>
    /// Tries to get a pack by ID without throwing.
    /// </summary>
    /// <param name="packId">The pack ID.</param>
    /// <returns>The pack if found; otherwise, <c>null</c>.</returns>
    PromptPack? TryGetPack(string packId);

    /// <summary>
    /// Lists all available packs.
    /// </summary>
    /// <returns>The list of pack information.</returns>
    IReadOnlyList<PromptPackInfo> ListPacks();

    /// <summary>
    /// Gets the currently active pack based on configuration.
    /// </summary>
    /// <returns>The active prompt pack.</returns>
    PromptPack GetActivePack();

    /// <summary>
    /// Gets the ID of the currently active pack.
    /// </summary>
    /// <returns>The active pack ID.</returns>
    string GetActivePackId();

    /// <summary>
    /// Refreshes the registry by re-discovering packs and clearing cache.
    /// </summary>
    void Refresh();
}
