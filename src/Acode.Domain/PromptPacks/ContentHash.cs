using System.Security.Cryptography;
using System.Text;

namespace Acode.Domain.PromptPacks;

/// <summary>
/// Represents a SHA-256 content hash for integrity verification.
/// Hash values are normalized to lowercase for consistent comparison.
/// </summary>
public sealed record ContentHash
{
    private readonly string _value;

    /// <summary>
    /// Initializes a new instance of the <see cref="ContentHash"/> class.
    /// </summary>
    /// <param name="value">The hexadecimal hash string (will be normalized to lowercase).</param>
    /// <exception cref="ArgumentException">Thrown when value is not a valid 64-character hex string.</exception>
    public ContentHash(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        if (value.Length != 64 || !IsHexString(value))
        {
            throw new ArgumentException("Hash must be a 64-character hexadecimal string.", nameof(value));
        }

        _value = value.ToLowerInvariant();
    }

    /// <summary>
    /// Gets the lowercase hexadecimal string representation of the hash.
    /// </summary>
    public string Value => _value;

    /// <summary>
    /// Computes a content hash from a collection of path/content pairs.
    /// </summary>
    /// <param name="contents">The path/content pairs to hash, ordered by path.</param>
    /// <returns>A new content hash computed from the contents.</returns>
    public static ContentHash Compute(IEnumerable<(string Path, string Content)> contents)
    {
        ArgumentNullException.ThrowIfNull(contents);

        using var sha256 = SHA256.Create();
        var orderedContents = contents.OrderBy(c => c.Path, StringComparer.Ordinal);

        foreach (var (path, content) in orderedContents)
        {
            var normalizedPath = path.Replace("\\", "/", StringComparison.Ordinal);
            var pathBytes = Encoding.UTF8.GetBytes(normalizedPath);
            sha256.TransformBlock(pathBytes, 0, pathBytes.Length, null, 0);

            var contentBytes = Encoding.UTF8.GetBytes(content);
            sha256.TransformBlock(contentBytes, 0, contentBytes.Length, null, 0);
        }

        sha256.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
        var hashBytes = sha256.Hash!;

        return new ContentHash(Convert.ToHexString(hashBytes).ToLowerInvariant());
    }

    /// <summary>
    /// Determines whether this hash matches another hash.
    /// Since values are normalized to lowercase, standard equality is used.
    /// </summary>
    /// <param name="other">The other hash to compare.</param>
    /// <returns><c>true</c> if the hashes match; otherwise, <c>false</c>.</returns>
    public bool Matches(ContentHash? other)
    {
        return other is not null && _value == other._value;
    }

    /// <summary>
    /// Determines whether the specified object is equal to the current object.
    /// </summary>
    /// <param name="other">The object to compare with the current object.</param>
    /// <returns><c>true</c> if the specified object is equal to the current object; otherwise, <c>false</c>.</returns>
    public bool Equals(ContentHash? other)
    {
        return other is not null && _value == other._value;
    }

    /// <summary>
    /// Returns the hash code for this instance.
    /// </summary>
    /// <returns>A 32-bit signed integer hash code.</returns>
    public override int GetHashCode()
    {
        return _value.GetHashCode(StringComparison.Ordinal);
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        return _value;
    }

    private static bool IsHexString(string value)
    {
        foreach (var c in value)
        {
            if (!char.IsAsciiHexDigit(c))
            {
                return false;
            }
        }

        return true;
    }
}
