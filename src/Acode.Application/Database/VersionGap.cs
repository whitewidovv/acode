// src/Acode.Application/Database/VersionGap.cs
namespace Acode.Application.Database;

/// <summary>
/// Represents a gap in the migration version sequence.
/// </summary>
/// <remarks>
/// For example, if migrations 001, 002, and 004 exist, there is a gap at version 003.
/// </remarks>
public sealed record VersionGap
{
    /// <summary>
    /// Gets the missing version number.
    /// </summary>
    public required string MissingVersion { get; init; }

    /// <summary>
    /// Gets the version before the gap.
    /// </summary>
    public required string BeforeVersion { get; init; }

    /// <summary>
    /// Gets the version after the gap.
    /// </summary>
    public required string AfterVersion { get; init; }
}
