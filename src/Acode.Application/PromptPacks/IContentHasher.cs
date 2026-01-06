using Acode.Domain.PromptPacks;

namespace Acode.Application.PromptPacks;

/// <summary>
/// Service for computing and verifying content hashes of prompt packs.
/// </summary>
/// <remarks>
/// Content hashing ensures pack integrity by creating a deterministic SHA-256 hash
/// of all component contents. The hash must be stable across platforms and runs.
/// </remarks>
public interface IContentHasher
{
    /// <summary>
    /// Computes a deterministic SHA-256 hash of the provided components.
    /// </summary>
    /// <param name="components">Dictionary mapping component paths to their content.</param>
    /// <returns>The computed content hash.</returns>
    /// <remarks>
    /// The computation is deterministic:
    /// - Paths are sorted alphabetically.
    /// - Line endings are normalized to LF.
    /// - Encoding is UTF-8 without BOM.
    /// </remarks>
    ContentHash Compute(IReadOnlyDictionary<string, string> components);

    /// <summary>
    /// Verifies that the provided components match the expected content hash.
    /// </summary>
    /// <param name="components">Dictionary mapping component paths to their content.</param>
    /// <param name="expectedHash">The expected content hash to verify against.</param>
    /// <returns>True if the computed hash matches the expected hash; otherwise, false.</returns>
    bool Verify(IReadOnlyDictionary<string, string> components, ContentHash expectedHash);
}
