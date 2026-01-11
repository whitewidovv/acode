using Acode.Domain.PromptPacks;

namespace Acode.Application.PromptPacks;

/// <summary>
/// Provides content hashing functionality for prompt pack integrity verification.
/// </summary>
/// <remarks>
/// The content hasher computes SHA-256 hashes of pack components to enable
/// integrity verification. Key features:
/// - Deterministic hashing (same content = same hash).
/// - Path sorting for consistent ordering.
/// - Line ending normalization (CRLF → LF).
/// - UTF-8 encoding.
/// </remarks>
public interface IContentHasher
{
    /// <summary>
    /// Computes a hash from path/content pairs directly.
    /// </summary>
    /// <param name="components">The path and content pairs to hash.</param>
    /// <returns>The computed content hash.</returns>
    /// <exception cref="ArgumentNullException">Components is null.</exception>
    ContentHash ComputeHash(IEnumerable<(string Path, string Content)> components);

    /// <summary>
    /// Computes a hash from file contents within a pack directory.
    /// </summary>
    /// <param name="packPath">The path to the pack directory.</param>
    /// <param name="componentPaths">The relative paths to component files.</param>
    /// <returns>The computed content hash.</returns>
    /// <exception cref="ArgumentNullException">PackPath or componentPaths is null.</exception>
    ContentHash ComputeHash(string packPath, IEnumerable<string> componentPaths);
}
