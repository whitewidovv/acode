namespace Acode.Domain.Security.PathProtection;

/// <summary>
/// Entry in the denylist defining a protected path pattern.
/// Immutable record - cannot be modified after creation.
/// </summary>
public sealed record DenylistEntry
{
    /// <summary>
    /// Gets the glob pattern for matching protected paths.
    /// Examples: "~/.ssh/", "*.pem", "**/.env".
    /// </summary>
    public required string Pattern { get; init; }

    /// <summary>
    /// Gets the human-readable reason why this path is protected.
    /// Should explain the security risk.
    /// </summary>
    public required string Reason { get; init; }

    /// <summary>
    /// Gets the risk ID this entry mitigates.
    /// References a risk from the risk register (e.g., "RISK-E-003").
    /// </summary>
    public required string RiskId { get; init; }

    /// <summary>
    /// Gets the category of protected path.
    /// Used for organization and documentation.
    /// </summary>
    public required PathCategory Category { get; init; }

    /// <summary>
    /// Gets the platforms this entry applies to.
    /// Can be specific (Windows, Linux, MacOS) or All.
    /// </summary>
    public required IReadOnlyList<Platform> Platforms { get; init; }

    /// <summary>
    /// Gets a value indicating whether this is a default entry.
    /// Default entries are built-in and cannot be removed.
    /// User-defined entries have IsDefault = false.
    /// Defaults to true.
    /// </summary>
    public bool IsDefault { get; init; } = true;
}
