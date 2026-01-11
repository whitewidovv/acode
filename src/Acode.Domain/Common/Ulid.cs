// src/Acode.Domain/Common/Ulid.cs
namespace Acode.Domain.Common;

using System;
using System.Linq;

/// <summary>
/// Utility for generating ULID (Universally Unique Lexicographically Sortable Identifier).
/// ULIDs are 26-character base32-encoded strings with timestamp-based sortability.
/// </summary>
public static class Ulid
{
    private const string Base32Chars = "0123456789ABCDEFGHJKMNPQRSTVWXYZ"; // Crockford Base32 (excludes I, L, O, U)

    /// <summary>
    /// Generates a new ULID based on the current UTC timestamp and random bytes.
    /// </summary>
    /// <returns>A 26-character ULID string.</returns>
    public static string NewUlid()
    {
        // 10 chars for timestamp (48 bits) + 16 chars for randomness (80 bits) = 26 chars total
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var random = new byte[10];
        System.Random.Shared.NextBytes(random);

        var result = new char[26];

        // Encode timestamp (48 bits = 10 base32 chars)
        for (int i = 9; i >= 0; i--)
        {
            result[i] = Base32Chars[(int)(timestamp & 0x1F)];
            timestamp >>= 5;
        }

        // Encode random bytes (80 bits = 16 base32 chars)
        long randomValue = 0;
        int bitsAvailable = 0;
        int outputIndex = 10;

        foreach (var b in random)
        {
            randomValue = (randomValue << 8) | b;
            bitsAvailable += 8;

            while (bitsAvailable >= 5 && outputIndex < 26)
            {
                result[outputIndex++] = Base32Chars[(int)((randomValue >> (bitsAvailable - 5)) & 0x1F)];
                bitsAvailable -= 5;
            }
        }

        return new string(result);
    }

    /// <summary>
    /// Validates whether a string is a valid ULID format (26 characters, Crockford Base32).
    /// </summary>
    /// <param name="value">The string to validate.</param>
    /// <returns>True if the string is a valid ULID; otherwise false.</returns>
    public static bool IsValid(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length != 26)
        {
            return false;
        }

        return value.All(c => Base32Chars.Contains(c, StringComparison.Ordinal));
    }
}
