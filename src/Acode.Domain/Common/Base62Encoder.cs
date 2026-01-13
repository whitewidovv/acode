namespace Acode.Domain.Common;

using System.Numerics;
using System.Text;

/// <summary>
/// Utility for encoding GUIDs to Base62 strings.
/// Base62 uses: 0-9, A-Z, a-z (62 characters).
/// </summary>
public static class Base62Encoder
{
    private const string Alphabet = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";

    /// <summary>
    /// Encodes a GUID to a Base62 string.
    /// </summary>
    /// <param name="id">The GUID to encode.</param>
    /// <returns>Base62-encoded string.</returns>
    public static string Encode(Guid id)
    {
        var bytes = id.ToByteArray();
        var value = new BigInteger(bytes.Concat(new byte[] { 0 }).ToArray());

        if (value == BigInteger.Zero)
        {
            return "0";
        }

        var result = new StringBuilder();
        var base62 = new BigInteger(62);

        while (value > BigInteger.Zero)
        {
            var remainder = (int)(value % base62);
            result.Insert(0, Alphabet[remainder]);
            value /= base62;
        }

        return result.ToString();
    }
}
