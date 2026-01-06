using Acode.Domain.PromptPacks;

namespace Acode.Application.PromptPacks;

/// <summary>
/// Registry for discovering, indexing, and retrieving prompt packs.
/// </summary>
public interface IPromptPackRegistry
{
    /// <summary>
    /// Initializes the registry by discovering and indexing all available packs.
    /// </summary>
    /// <remarks>
    /// Discovery sources:
    /// - Built-in packs from embedded resources.
    /// - User packs from {workspace}/.acode/prompts/.
    ///
    /// User packs override built-in packs with the same ID.
    /// </remarks>
    void Initialize();

    /// <summary>
    /// Gets metadata for all available packs.
    /// </summary>
    /// <returns>Collection of pack metadata.</returns>
    IReadOnlyList<PromptPackInfo> ListPacks();

    /// <summary>
    /// Gets a specific pack by ID.
    /// </summary>
    /// <param name="packId">The pack ID to retrieve.</param>
    /// <returns>The requested pack.</returns>
    /// <exception cref="PackNotFoundException">Thrown when the pack is not found.</exception>
    PromptPack GetPack(string packId);

    /// <summary>
    /// Gets the currently active pack based on configuration.
    /// </summary>
    /// <returns>The active pack.</returns>
    /// <remarks>
    /// Configuration precedence:
    /// 1. ACODE_PROMPT_PACK environment variable (highest).
    /// 2. .agent/config.yml prompts.pack_id setting.
    /// 3. "acode-standard" default (lowest).
    ///
    /// If the configured pack is not found, falls back to "acode-standard" with a warning.
    /// </remarks>
    PromptPack GetActivePack();

    /// <summary>
    /// Refreshes the registry by re-discovering and reloading all packs.
    /// </summary>
    /// <remarks>
    /// This is useful for hot-reloading packs during development without restarting the application.
    /// </remarks>
    void Refresh();
}
