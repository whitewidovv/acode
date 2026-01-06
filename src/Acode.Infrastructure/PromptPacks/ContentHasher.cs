using System.Security.Cryptography;
using System.Text;
using Acode.Application.PromptPacks;
using Acode.Domain.PromptPacks;

namespace Acode.Infrastructure.PromptPacks;

/// <summary>
/// Service for computing and verifying deterministic SHA-256 content hashes.
/// </summary>
/// <remarks>
/// Implements deterministic hashing by:
/// - Sorting component paths alphabetically.
/// - Normalizing line endings to LF (\n).
/// - Using UTF-8 encoding without BOM.
/// This ensures hash stability across platforms and tool versions.
/// </remarks>
public sealed class ContentHasher : IContentHasher
{
    /// <inheritdoc/>
    public ContentHash Compute(IReadOnlyDictionary<string, string> components)
    {
        ArgumentNullException.ThrowIfNull(components);

        // Sort paths alphabetically for deterministic order
        var sortedPaths = components.Keys.OrderBy(k => k, StringComparer.Ordinal).ToList();

        // Build combined content with normalized line endings
        var combinedContent = new StringBuilder();
        foreach (var path in sortedPaths)
        {
            var content = components[path];
            var normalizedContent = NormalizeLineEndings(content);

            // Include both path and content to detect path changes
            combinedContent.Append(path);
            combinedContent.Append('\n');
            combinedContent.Append(normalizedContent);
            combinedContent.Append('\n');
        }

        // Compute SHA-256 hash
        var bytes = Encoding.UTF8.GetBytes(combinedContent.ToString());
        var hashBytes = SHA256.HashData(bytes);

        // Convert to lowercase hex string
        var hashHex = Convert.ToHexString(hashBytes).ToLowerInvariant();

        return new ContentHash(hashHex);
    }

    /// <inheritdoc/>
    public bool Verify(IReadOnlyDictionary<string, string> components, ContentHash expectedHash)
    {
        ArgumentNullException.ThrowIfNull(components);
        ArgumentNullException.ThrowIfNull(expectedHash);

        var actualHash = Compute(components);
        return actualHash.Equals(expectedHash);
    }

    /// <summary>
    /// Normalizes line endings to LF (\n) for cross-platform stability.
    /// </summary>
    /// <param name="content">The content to normalize.</param>
    /// <returns>Content with normalized line endings.</returns>
    private static string NormalizeLineEndings(string content)
    {
        if (string.IsNullOrEmpty(content))
        {
            return content;
        }

        // Replace CRLF (\r\n) with LF (\n), then remove any remaining CR (\r)
        return content.Replace("\r\n", "\n", StringComparison.Ordinal).Replace("\r", "\n", StringComparison.Ordinal);
    }
}
