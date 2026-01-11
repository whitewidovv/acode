using Acode.Domain.PromptPacks;

namespace Acode.Application.PromptPacks;

/// <summary>
/// Interface for loading prompt packs from various sources.
/// </summary>
public interface IPromptPackLoader
{
    /// <summary>
    /// Loads a pack from the specified path.
    /// </summary>
    /// <param name="path">The path to the pack directory.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The loaded prompt pack.</returns>
    Task<PromptPack> LoadPackAsync(string path, CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads a built-in pack by ID.
    /// </summary>
    /// <param name="packId">The pack ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The loaded prompt pack.</returns>
    Task<PromptPack> LoadBuiltInPackAsync(string packId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads a user pack from the specified path.
    /// </summary>
    /// <param name="path">The path to the pack directory.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The loaded prompt pack.</returns>
    Task<PromptPack> LoadUserPackAsync(string path, CancellationToken cancellationToken = default);

    /// <summary>
    /// Attempts to load a pack, returning success/failure without throwing.
    /// </summary>
    /// <param name="path">The path to the pack directory.</param>
    /// <param name="pack">The loaded pack if successful.</param>
    /// <param name="errorMessage">The error message if unsuccessful.</param>
    /// <returns><c>true</c> if the pack was loaded; otherwise, <c>false</c>.</returns>
    bool TryLoadPack(string path, out PromptPack? pack, out string? errorMessage);
}
