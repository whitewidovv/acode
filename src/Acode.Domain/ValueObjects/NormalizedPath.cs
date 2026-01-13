using Acode.Domain.Security.PathProtection;

namespace Acode.Domain.ValueObjects;

/// <summary>
/// Value object representing a normalized file system path.
/// Ensures paths are consistently normalized before validation.
/// </summary>
public sealed record NormalizedPath
{
    /// <summary>
    /// Gets the normalized path value.
    /// </summary>
    public required string Value { get; init; }

    /// <summary>
    /// Implicit conversion to string.
    /// </summary>
    /// <param name="path">The normalized path.</param>
    public static implicit operator string(NormalizedPath path)
    {
        ArgumentNullException.ThrowIfNull(path);
        return path.Value;
    }

    /// <summary>
    /// Creates a NormalizedPath from a raw path string.
    /// </summary>
    /// <param name="path">The raw path to normalize.</param>
    /// <param name="normalizer">The path normalizer to use.</param>
    /// <returns>A new NormalizedPath instance.</returns>
    public static NormalizedPath From(string path, IPathNormalizer normalizer)
    {
        ArgumentNullException.ThrowIfNull(path);
        ArgumentNullException.ThrowIfNull(normalizer);

        var normalized = normalizer.Normalize(path);
        return new NormalizedPath { Value = normalized };
    }
}
