using System.Text.RegularExpressions;

namespace Acode.Domain.PromptPacks;

/// <summary>
/// Represents a SHA-256 content hash for prompt pack integrity verification.
/// </summary>
/// <remarks>
/// ContentHash is a value object that ensures hash values are always valid:
/// - Exactly 64 characters (SHA-256 hex representation).
/// - Only hexadecimal characters (0-9, a-f).
/// - Always stored in lowercase for consistency.
/// </remarks>
public sealed record ContentHash
{
    private static readonly Regex HexPattern = new Regex("^[0-9a-fA-F]{64}$", RegexOptions.Compiled);

    /// <summary>
    /// Initializes a new instance of the <see cref="ContentHash"/> class.
    /// </summary>
    /// <param name="value">The SHA-256 hash value (64 hexadecimal characters).</param>
    /// <exception cref="ArgumentException">Thrown when the hash value is not exactly 64 hexadecimal characters.</exception>
    public ContentHash(string value)
    {
        ArgumentNullException.ThrowIfNull(value);

        if (value.Length != 64)
        {
            throw new ArgumentException(
                $"Content hash must be exactly 64 characters (SHA-256 hex). Got {value.Length} characters.",
                nameof(value));
        }

        if (!HexPattern.IsMatch(value))
        {
            throw new ArgumentException(
                "Content hash must contain only hexadecimal characters (0-9, a-f).",
                nameof(value));
        }

        Value = value.ToLowerInvariant();
    }

    /// <summary>
    /// Gets the hash value as a lowercase hexadecimal string.
    /// </summary>
    public string Value { get; }

    /// <summary>
    /// Returns the hash value as a lowercase hexadecimal string.
    /// </summary>
    /// <returns>The hash value.</returns>
    public override string ToString() => Value;
}
