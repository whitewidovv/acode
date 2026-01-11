using System.Text.RegularExpressions;

namespace Acode.Domain.PromptPacks;

/// <summary>
/// Represents a semantic version (SemVer 2.0) for prompt pack versioning.
/// </summary>
public sealed partial class PackVersion : IComparable<PackVersion>, IEquatable<PackVersion>
{
    private static readonly Regex SemVerRegex = SemVerPattern();

    /// <summary>
    /// Initializes a new instance of the <see cref="PackVersion"/> class.
    /// </summary>
    /// <param name="major">The major version number.</param>
    /// <param name="minor">The minor version number.</param>
    /// <param name="patch">The patch version number.</param>
    /// <param name="preRelease">The optional pre-release suffix.</param>
    /// <param name="buildMetadata">The optional build metadata.</param>
    public PackVersion(int major, int minor, int patch, string? preRelease = null, string? buildMetadata = null)
    {
        if (major < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(major), "Major version must be non-negative.");
        }

        if (minor < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(minor), "Minor version must be non-negative.");
        }

        if (patch < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(patch), "Patch version must be non-negative.");
        }

        Major = major;
        Minor = minor;
        Patch = patch;
        PreRelease = preRelease;
        BuildMetadata = buildMetadata;
    }

    /// <summary>
    /// Gets the major version number.
    /// </summary>
    public int Major { get; }

    /// <summary>
    /// Gets the minor version number.
    /// </summary>
    public int Minor { get; }

    /// <summary>
    /// Gets the patch version number.
    /// </summary>
    public int Patch { get; }

    /// <summary>
    /// Gets the optional pre-release suffix (e.g., "alpha", "beta.1").
    /// </summary>
    public string? PreRelease { get; }

    /// <summary>
    /// Gets the optional build metadata.
    /// </summary>
    public string? BuildMetadata { get; }

    /// <summary>
    /// Gets a value indicating whether this is a pre-release version.
    /// </summary>
    public bool IsPreRelease => !string.IsNullOrEmpty(PreRelease);

    /// <summary>
    /// Less than operator.
    /// </summary>
    /// <param name="left">The left operand.</param>
    /// <param name="right">The right operand.</param>
    /// <returns><c>true</c> if left is less than right; otherwise, <c>false</c>.</returns>
    public static bool operator <(PackVersion? left, PackVersion? right)
    {
        if (left is null)
        {
            return right is not null;
        }

        return left.CompareTo(right) < 0;
    }

    /// <summary>
    /// Greater than operator.
    /// </summary>
    /// <param name="left">The left operand.</param>
    /// <param name="right">The right operand.</param>
    /// <returns><c>true</c> if left is greater than right; otherwise, <c>false</c>.</returns>
    public static bool operator >(PackVersion? left, PackVersion? right)
    {
        if (left is null)
        {
            return false;
        }

        return left.CompareTo(right) > 0;
    }

    /// <summary>
    /// Less than or equal operator.
    /// </summary>
    /// <param name="left">The left operand.</param>
    /// <param name="right">The right operand.</param>
    /// <returns><c>true</c> if left is less than or equal to right; otherwise, <c>false</c>.</returns>
    public static bool operator <=(PackVersion? left, PackVersion? right)
    {
        if (left is null)
        {
            return true;
        }

        return left.CompareTo(right) <= 0;
    }

    /// <summary>
    /// Greater than or equal operator.
    /// </summary>
    /// <param name="left">The left operand.</param>
    /// <param name="right">The right operand.</param>
    /// <returns><c>true</c> if left is greater than or equal to right; otherwise, <c>false</c>.</returns>
    public static bool operator >=(PackVersion? left, PackVersion? right)
    {
        if (left is null)
        {
            return right is null;
        }

        return left.CompareTo(right) >= 0;
    }

    /// <summary>
    /// Equality operator.
    /// </summary>
    /// <param name="left">The left operand.</param>
    /// <param name="right">The right operand.</param>
    /// <returns><c>true</c> if equal; otherwise, <c>false</c>.</returns>
    public static bool operator ==(PackVersion? left, PackVersion? right)
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
    public static bool operator !=(PackVersion? left, PackVersion? right)
    {
        return !(left == right);
    }

    /// <summary>
    /// Parses a version string into a <see cref="PackVersion"/>.
    /// </summary>
    /// <param name="version">The version string to parse.</param>
    /// <returns>The parsed version.</returns>
    /// <exception cref="FormatException">Thrown when the version string is invalid.</exception>
    public static PackVersion Parse(string version)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(version);

        if (!TryParse(version, out var result) || result is null)
        {
            throw new FormatException($"Invalid semantic version format: '{version}'");
        }

        return result;
    }

    /// <summary>
    /// Attempts to parse a version string into a <see cref="PackVersion"/>.
    /// </summary>
    /// <param name="version">The version string to parse.</param>
    /// <param name="result">The parsed version if successful.</param>
    /// <returns><c>true</c> if parsing succeeded; otherwise, <c>false</c>.</returns>
    public static bool TryParse(string? version, out PackVersion? result)
    {
        result = null;

        if (string.IsNullOrWhiteSpace(version))
        {
            return false;
        }

        var match = SemVerRegex.Match(version);
        if (!match.Success)
        {
            return false;
        }

        var major = int.Parse(match.Groups["major"].Value, System.Globalization.CultureInfo.InvariantCulture);
        var minor = int.Parse(match.Groups["minor"].Value, System.Globalization.CultureInfo.InvariantCulture);
        var patch = int.Parse(match.Groups["patch"].Value, System.Globalization.CultureInfo.InvariantCulture);
        var prerelease = match.Groups["prerelease"].Success ? match.Groups["prerelease"].Value : null;
        var buildMetadata = match.Groups["buildmetadata"].Success ? match.Groups["buildmetadata"].Value : null;

        result = new PackVersion(major, minor, patch, prerelease, buildMetadata);
        return true;
    }

    /// <inheritdoc/>
    public int CompareTo(PackVersion? other)
    {
        if (other is null)
        {
            return 1;
        }

        var majorCompare = Major.CompareTo(other.Major);
        if (majorCompare != 0)
        {
            return majorCompare;
        }

        var minorCompare = Minor.CompareTo(other.Minor);
        if (minorCompare != 0)
        {
            return minorCompare;
        }

        var patchCompare = Patch.CompareTo(other.Patch);
        if (patchCompare != 0)
        {
            return patchCompare;
        }

        // Pre-release versions have lower precedence
        if (IsPreRelease && !other.IsPreRelease)
        {
            return -1;
        }

        if (!IsPreRelease && other.IsPreRelease)
        {
            return 1;
        }

        if (IsPreRelease && other.IsPreRelease)
        {
            return string.Compare(PreRelease, other.PreRelease, StringComparison.Ordinal);
        }

        return 0;
    }

    /// <inheritdoc/>
    public bool Equals(PackVersion? other)
    {
        if (other is null)
        {
            return false;
        }

        return Major == other.Major
            && Minor == other.Minor
            && Patch == other.Patch
            && string.Equals(PreRelease, other.PreRelease, StringComparison.Ordinal);
    }

    /// <inheritdoc/>
    public override bool Equals(object? obj)
    {
        return obj is PackVersion other && Equals(other);
    }

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        return HashCode.Combine(Major, Minor, Patch, PreRelease);
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        var version = $"{Major}.{Minor}.{Patch}";

        if (!string.IsNullOrEmpty(PreRelease))
        {
            version += $"-{PreRelease}";
        }

        if (!string.IsNullOrEmpty(BuildMetadata))
        {
            version += $"+{BuildMetadata}";
        }

        return version;
    }

    [GeneratedRegex(
        @"^(?<major>0|[1-9]\d*)\.(?<minor>0|[1-9]\d*)\.(?<patch>0|[1-9]\d*)(?:-(?<prerelease>(?:0|[1-9]\d*|\d*[a-zA-Z-][0-9a-zA-Z-]*)(?:\.(?:0|[1-9]\d*|\d*[a-zA-Z-][0-9a-zA-Z-]*))*))?(?:\+(?<buildmetadata>[0-9a-zA-Z-]+(?:\.[0-9a-zA-Z-]+)*))?$",
        RegexOptions.Compiled)]
    private static partial Regex SemVerPattern();
}
