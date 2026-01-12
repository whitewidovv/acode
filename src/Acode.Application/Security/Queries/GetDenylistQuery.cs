using Acode.Domain.Security.PathProtection;

namespace Acode.Application.Security.Queries;

/// <summary>
/// Query to retrieve the denylist with optional filtering.
/// </summary>
public sealed record GetDenylistQuery
{
    /// <summary>
    /// Gets the category filter (optional).
    /// If specified, only entries in this category are returned.
    /// </summary>
    public PathCategory? CategoryFilter { get; init; }

    /// <summary>
    /// Gets the platform filter (optional).
    /// If specified, only entries for this platform are returned.
    /// </summary>
    public Platform? PlatformFilter { get; init; }

    /// <summary>
    /// Gets a value indicating whether to include user-defined paths.
    /// Defaults to true.
    /// </summary>
    public bool IncludeUserDefined { get; init; } = true;
}
