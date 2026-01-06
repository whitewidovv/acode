using System.Text.RegularExpressions;

namespace Acode.Domain.PromptPacks;

/// <summary>
/// Represents a semantic version (SemVer 2.0) for prompt packs.
/// </summary>
/// <remarks>
/// PackVersion follows Semantic Versioning 2.0.0 specification:
/// - Format: MAJOR.MINOR.PATCH[-PRERELEASE][+BUILDMETADATA].
/// - Pre-release versions have lower precedence than release versions.
/// - Build metadata is ignored in version comparisons.
/// </remarks>
public sealed record PackVersion : IComparable<PackVersion>
{
    private static readonly Regex SemVerPattern = new Regex(
        @"^(?<major>0|[1-9]\d*)\.(?<minor>0|[1-9]\d*)\.(?<patch>0|[1-9]\d*)(?:-(?<prerelease>(?:0|[1-9]\d*|\d*[a-zA-Z-][0-9a-zA-Z-]*)(?:\.(?:0|[1-9]\d*|\d*[a-zA-Z-][0-9a-zA-Z-]*))*))?(?:\+(?<buildmetadata>[0-9a-zA-Z-]+(?:\.[0-9a-zA-Z-]+)*))?$",
        RegexOptions.Compiled | RegexOptions.ExplicitCapture);

    /// <summary>
    /// Initializes a new instance of the <see cref="PackVersion"/> class.
    /// </summary>
    /// <param name="major">The major version number.</param>
    /// <param name="minor">The minor version number.</param>
    /// <param name="patch">The patch version number.</param>
    /// <param name="preRelease">The pre-release version (optional).</param>
    /// <param name="buildMetadata">The build metadata (optional).</param>
    public PackVersion(int major, int minor, int patch, string? preRelease = null, string? buildMetadata = null)
    {
        if (major < 0)
        {
            throw new ArgumentException("Major version must be non-negative.", nameof(major));
        }

        if (minor < 0)
        {
            throw new ArgumentException("Minor version must be non-negative.", nameof(minor));
        }

        if (patch < 0)
        {
            throw new ArgumentException("Patch version must be non-negative.", nameof(patch));
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
    /// Gets the pre-release version identifier.
    /// </summary>
    public string? PreRelease { get; }

    /// <summary>
    /// Gets the build metadata.
    /// </summary>
    public string? BuildMetadata { get; }

    /// <summary>
    /// Greater than operator.
    /// </summary>
    /// <param name="left">Left operand.</param>
    /// <param name="right">Right operand.</param>
    /// <returns>True if left is greater than right.</returns>
    public static bool operator >(PackVersion? left, PackVersion? right)
    {
        return left is not null && left.CompareTo(right) > 0;
    }

    /// <summary>
    /// Less than operator.
    /// </summary>
    /// <param name="left">Left operand.</param>
    /// <param name="right">Right operand.</param>
    /// <returns>True if left is less than right.</returns>
    public static bool operator <(PackVersion? left, PackVersion? right)
    {
        return left is null || left.CompareTo(right) < 0;
    }

    /// <summary>
    /// Greater than or equal operator.
    /// </summary>
    /// <param name="left">Left operand.</param>
    /// <param name="right">Right operand.</param>
    /// <returns>True if left is greater than or equal to right.</returns>
    public static bool operator >=(PackVersion? left, PackVersion? right)
    {
        return left is not null && left.CompareTo(right) >= 0;
    }

    /// <summary>
    /// Less than or equal operator.
    /// </summary>
    /// <param name="left">Left operand.</param>
    /// <param name="right">Right operand.</param>
    /// <returns>True if left is less than or equal to right.</returns>
    public static bool operator <=(PackVersion? left, PackVersion? right)
    {
        return left is null || left.CompareTo(right) <= 0;
    }

    /// <summary>
    /// Parses a semantic version string.
    /// </summary>
    /// <param name="version">The version string to parse.</param>
    /// <returns>A <see cref="PackVersion"/> instance.</returns>
    /// <exception cref="ArgumentException">Thrown when the version string is not valid Semantic Versioning 2.0.0 format.</exception>
    public static PackVersion Parse(string version)
    {
        ArgumentNullException.ThrowIfNull(version);

        var match = SemVerPattern.Match(version);
        if (!match.Success)
        {
            throw new ArgumentException(
                $"Version string '{version}' is not valid Semantic Versioning 2.0.0 format. " +
                $"Expected format: MAJOR.MINOR.PATCH[-PRERELEASE][+BUILDMETADATA]",
                nameof(version));
        }

        var major = int.Parse(match.Groups["major"].Value);
        var minor = int.Parse(match.Groups["minor"].Value);
        var patch = int.Parse(match.Groups["patch"].Value);
        var preRelease = match.Groups["prerelease"].Success ? match.Groups["prerelease"].Value : null;
        var buildMetadata = match.Groups["buildmetadata"].Success ? match.Groups["buildmetadata"].Value : null;

        return new PackVersion(major, minor, patch, preRelease, buildMetadata);
    }

    /// <summary>
    /// Compares this version to another version.
    /// </summary>
    /// <param name="other">The other version to compare to.</param>
    /// <returns>
    /// Less than 0 if this version is lower, 0 if equal, greater than 0 if this version is higher.
    /// </returns>
    public int CompareTo(PackVersion? other)
    {
        if (other is null)
        {
            return 1;
        }

        // Compare major, minor, patch
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

        // Pre-release comparison (SemVer 2.0 rule: release > pre-release)
        if (PreRelease is null && other.PreRelease is not null)
        {
            return 1; // Release version is higher than pre-release
        }

        if (PreRelease is not null && other.PreRelease is null)
        {
            return -1; // Pre-release version is lower than release
        }

        if (PreRelease is not null && other.PreRelease is not null)
        {
            // Compare pre-release versions alphanumerically
            var preReleaseCompare = string.CompareOrdinal(PreRelease, other.PreRelease);
            if (preReleaseCompare != 0)
            {
                return preReleaseCompare;
            }
        }

        // Build metadata is ignored in comparison per SemVer 2.0
        return 0;
    }

    /// <summary>
    /// Returns the version as a SemVer 2.0 string.
    /// </summary>
    /// <returns>The version string.</returns>
    public override string ToString()
    {
        var version = $"{Major}.{Minor}.{Patch}";

        if (PreRelease is not null)
        {
            version += $"-{PreRelease}";
        }

        if (BuildMetadata is not null)
        {
            version += $"+{BuildMetadata}";
        }

        return version;
    }
}
