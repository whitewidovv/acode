using System.Text;
using Acode.Domain.PromptPacks;

namespace Acode.Infrastructure.PromptPacks;

/// <summary>
/// Provides content hashing functionality for prompt pack integrity verification.
/// </summary>
public sealed class ContentHasher
{
    /// <summary>
    /// Computes a hash from path/content pairs directly.
    /// </summary>
    /// <param name="components">The path and content pairs to hash.</param>
    /// <returns>The computed content hash.</returns>
    public ContentHash ComputeHash(IEnumerable<(string Path, string Content)> components)
    {
        ArgumentNullException.ThrowIfNull(components);

        var contents = new List<(string Path, string Content)>();

        foreach (var (path, content) in components.OrderBy(c => c.Path, StringComparer.Ordinal))
        {
            // Normalize line endings (CRLF -> LF)
            var normalizedContent = content.Replace("\r\n", "\n", StringComparison.Ordinal);
            contents.Add((path, normalizedContent));
        }

        return ContentHash.Compute(contents);
    }

    /// <summary>
    /// Computes a hash from file contents within a pack directory.
    /// </summary>
    /// <param name="packPath">The path to the pack directory.</param>
    /// <param name="componentPaths">The relative paths to component files.</param>
    /// <returns>The computed content hash.</returns>
    public ContentHash ComputeHash(string packPath, IEnumerable<string> componentPaths)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(packPath);
        ArgumentNullException.ThrowIfNull(componentPaths);

        var contents = new List<(string Path, string Content)>();

        foreach (var relativePath in componentPaths)
        {
            PathNormalizer.EnsurePathSafe(relativePath);
            var normalizedPath = PathNormalizer.Normalize(relativePath);
            var fullPath = Path.Combine(packPath, normalizedPath.Replace('/', Path.DirectorySeparatorChar));

            if (File.Exists(fullPath))
            {
                var content = File.ReadAllText(fullPath, Encoding.UTF8);
                contents.Add((normalizedPath, content));
            }
        }

        return ComputeHash(contents);
    }

    /// <summary>
    /// Asynchronously computes a hash from file contents within a pack directory.
    /// </summary>
    /// <param name="packPath">The path to the pack directory.</param>
    /// <param name="componentPaths">The relative paths to component files.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the async operation with the computed hash.</returns>
    public async Task<ContentHash> ComputeHashAsync(
        string packPath,
        IEnumerable<string> componentPaths,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(packPath);
        ArgumentNullException.ThrowIfNull(componentPaths);

        var contents = new List<(string Path, string Content)>();

        foreach (var relativePath in componentPaths)
        {
            PathNormalizer.EnsurePathSafe(relativePath);
            var normalizedPath = PathNormalizer.Normalize(relativePath);
            var fullPath = Path.Combine(packPath, normalizedPath.Replace('/', Path.DirectorySeparatorChar));

            if (File.Exists(fullPath))
            {
                var content = await File.ReadAllTextAsync(fullPath, Encoding.UTF8, cancellationToken)
                    .ConfigureAwait(false);
                contents.Add((normalizedPath, content));
            }
        }

        return ComputeHash(contents);
    }

    /// <summary>
    /// Regenerates the content hash for a pack and updates the manifest.
    /// </summary>
    /// <param name="manifest">The pack manifest.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A new manifest with updated content hash.</returns>
    public async Task<PackManifest> RegenerateAsync(
        PackManifest manifest,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(manifest);

        var componentPaths = manifest.Components.Select(c => c.Path);
        var newHash = await ComputeHashAsync(manifest.PackPath, componentPaths, cancellationToken)
            .ConfigureAwait(false);

        return manifest with { ContentHash = newHash };
    }
}
