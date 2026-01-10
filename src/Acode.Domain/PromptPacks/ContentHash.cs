using System.Security.Cryptography;
using System.Text;

namespace Acode.Domain.PromptPacks;

/// <summary>
/// Represents a SHA-256 content hash for integrity verification.
/// </summary>
public sealed class ContentHash : IEquatable<ContentHash>
{
    private readonly string _value;

    /// <summary>
    /// Initializes a new instance of the <see cref="ContentHash"/> class.
    /// </summary>
    /// <param name="value">The lowercase hexadecimal hash string.</param>
    /// <exception cref="ArgumentException">Thrown when value is not a valid 64-character hex string.</exception>
    public ContentHash(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        if (value.Length != 64 || !IsHexString(value))
        {
            throw new ArgumentException("Hash must be a 64-character lowercase hexadecimal string.", nameof(value));
        }

        _value = value.ToLowerInvariant();
    }

    /// <summary>
    /// Gets the hexadecimal string representation of the hash.
    /// </summary>
    public string Value => _value;

    /// <summary>
    /// Equality operator.
    /// </summary>
    /// <param name="left">The left operand.</param>
    /// <param name="right">The right operand.</param>
    /// <returns><c>true</c> if equal; otherwise, <c>false</c>.</returns>
    public static bool operator ==(ContentHash? left, ContentHash? right)
    {
        if (left is null)
        {
            return right is null;
        }

        return left.Equals(right);
    }

    /// <summary>
    /// Inequality operator.
    /// </summary>
    /// <param name="left">The left operand.</param>
    /// <param name="right">The right operand.</param>
    /// <returns><c>true</c> if not equal; otherwise, <c>false</c>.</returns>
    public static bool operator !=(ContentHash? left, ContentHash? right)
    {
        return !(left == right);
    }

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

        sha256.TransformFinalBlock([], 0, 0);
        var hashBytes = sha256.Hash!;

        return new ContentHash(Convert.ToHexString(hashBytes).ToLowerInvariant());
    }

    /// <summary>
    /// Determines whether this hash matches another hash.
    /// </summary>
    /// <param name="other">The other hash to compare.</param>
    /// <returns><c>true</c> if the hashes match; otherwise, <c>false</c>.</returns>
    public bool Matches(ContentHash? other)
    {
        if (other is null)
        {
            return false;
        }

        return string.Equals(_value, other._value, StringComparison.OrdinalIgnoreCase);
    }

    /// <inheritdoc/>
    public bool Equals(ContentHash? other)
    {
        return other is not null && Matches(other);
    }

    /// <inheritdoc/>
    public override bool Equals(object? obj)
    {
        return obj is ContentHash other && Equals(other);
    }

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        return _value.GetHashCode(StringComparison.OrdinalIgnoreCase);
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
