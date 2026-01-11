// src/Acode.Domain/Worktree/WorktreeId.cs
namespace Acode.Domain.Worktree;

using System;
using System.Security.Cryptography;
using System.Text;

/// <summary>
/// Strongly-typed identifier for Worktree entities using ULID format.
/// </summary>
public readonly record struct WorktreeId : IComparable<WorktreeId>
{
    private readonly string _value;

    private WorktreeId(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("WorktreeId cannot be empty", nameof(value));
        }

        if (value.Length != 26)
        {
            throw new ArgumentException("WorktreeId must be 26 characters (ULID format)", nameof(value));
        }

        _value = value;
    }

    /// <summary>
    /// Gets the ULID string value.
    /// </summary>
    public string Value => _value ?? throw new InvalidOperationException("WorktreeId not initialized");

    /// <summary>
    /// Implicit conversion to string for convenience.
    /// </summary>
    /// <param name="id">The WorktreeId to convert.</param>
    public static implicit operator string(WorktreeId id) => id.Value;

    /// <summary>
    /// Creates a WorktreeId from an existing ULID string.
    /// </summary>
    /// <param name="value">The 26-character ULID string.</param>
    /// <returns>A new WorktreeId instance.</returns>
    public static WorktreeId From(string value) => new(value);

    /// <summary>
    /// Creates a deterministic WorktreeId from a worktree path.
    /// Same path always produces the same ID (AC-034, AC-035).
    /// </summary>
    /// <param name="path">The absolute worktree path.</param>
    /// <returns>A deterministic WorktreeId based on the path hash.</returns>
    public static WorktreeId FromPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("Worktree path cannot be empty", nameof(path));
        }

        // Normalize path: remove trailing slashes, convert to lowercase for consistency
        var normalizedPath = path.TrimEnd('/', '\\').ToLowerInvariant();

        // Generate deterministic hash from normalized path
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(normalizedPath));

        // Convert first 16 bytes of hash to ULID-compatible Base32 format (26 characters)
        // Use Crockford Base32 alphabet (same as ULID): 0-9, A-Z (excluding I, L, O, U)
        var ulidValue = ConvertToBase32(hashBytes);

        return new WorktreeId(ulidValue);
    }

    /// <summary>
    /// Compares this WorktreeId to another for ordering.
    /// </summary>
    /// <param name="other">The other WorktreeId to compare to.</param>
    /// <returns>A value indicating the relative order.</returns>
    public int CompareTo(WorktreeId other) => string.CompareOrdinal(_value, other._value);

    /// <summary>
    /// Returns the ULID string representation.
    /// </summary>
    /// <returns>The ULID string.</returns>
    public override string ToString() => _value;

    /// <summary>
    /// Converts bytes to Base32 using ULID/Crockford alphabet.
    /// </summary>
    /// <param name="bytes">The bytes to convert.</param>
    /// <returns>A 26-character Base32 string.</returns>
    private static string ConvertToBase32(byte[] bytes)
    {
        const string base32Alphabet = "0123456789ABCDEFGHJKMNPQRSTVWXYZ";  // Crockford Base32 (no I, L, O, U)
        var result = new char[26];

        // Convert first 16 bytes (128 bits) to 26 Base32 characters
        // Each Base32 char = 5 bits, so 26 chars = 130 bits (we use 128 bits + padding)
        long value = 0;
        int bitsAvailable = 0;
        int resultIndex = 0;
        int byteIndex = 0;

        while (resultIndex < 26)
        {
            // Load more bits if needed
            if (bitsAvailable < 5 && byteIndex < 16)
            {
                value = (value << 8) | bytes[byteIndex++];
                bitsAvailable += 8;
            }

            // Extract 5 bits
            if (bitsAvailable >= 5 || byteIndex >= 16)
            {
                int shift = Math.Max(0, bitsAvailable - 5);
                int index = (int)((value >> shift) & 0x1F);
                result[resultIndex++] = base32Alphabet[index];
                value &= (1L << shift) - 1;  // Clear used bits
                bitsAvailable = shift;
            }
        }

        return new string(result);
    }
}
