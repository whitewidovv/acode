using Acode.Domain.PromptPacks;

namespace Acode.Application.PromptPacks;

/// <summary>
/// Service for loading prompt packs from disk or embedded resources.
/// </summary>
public interface IPromptPackLoader
{
    /// <summary>
    /// Loads a prompt pack from the specified directory path.
    /// </summary>
    /// <param name="packPath">Absolute path to the pack directory containing manifest.yml.</param>
    /// <returns>The loaded prompt pack.</returns>
    /// <exception cref="PackLoadException">Thrown when the pack cannot be loaded.</exception>
    /// <remarks>
    /// The loader performs the following steps:
    /// 1. Reads and parses manifest.yml.
    /// 2. Loads all component files referenced in the manifest.
    /// 3. Computes content hash and compares with manifest (warning if mismatch).
    /// 4. Constructs and returns a PromptPack instance.
    ///
    /// Security:
    /// - All paths are validated for traversal attempts.
    /// - Symlinks are rejected.
    /// - All paths must be relative to pack root.
    /// </remarks>
    PromptPack LoadPack(string packPath);

    /// <summary>
    /// Loads a built-in prompt pack from embedded resources.
    /// </summary>
    /// <param name="packId">The ID of the built-in pack to load.</param>
    /// <returns>The loaded prompt pack.</returns>
    /// <exception cref="PackNotFoundException">Thrown when the built-in pack is not found.</exception>
    /// <exception cref="PackLoadException">Thrown when the pack cannot be loaded.</exception>
    PromptPack LoadBuiltInPack(string packId);
}
