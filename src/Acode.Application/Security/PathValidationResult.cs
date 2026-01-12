using Acode.Domain.Security.PathProtection;

namespace Acode.Application.Security;

/// <summary>
/// Result of path validation indicating if a path is protected.
/// </summary>
public sealed record PathValidationResult
{
    /// <summary>
    /// Gets a value indicating whether the path is protected.
    /// </summary>
    public required bool IsProtected { get; init; }

    /// <summary>
    /// Gets the denylist pattern that matched (if protected).
    /// </summary>
    public string? MatchedPattern { get; init; }

    /// <summary>
    /// Gets the human-readable reason for protection (if protected).
    /// </summary>
    public string? Reason { get; init; }

    /// <summary>
    /// Gets the risk identifier associated with this protection (if protected).
    /// </summary>
    public string? RiskId { get; init; }

    /// <summary>
    /// Gets the category of protected path (if protected).
    /// </summary>
    public PathCategory? Category { get; init; }

    /// <summary>
    /// Gets the detailed error information (if protected).
    /// </summary>
    public ProtectedPathError? Error { get; init; }

    /// <summary>
    /// Creates an allowed result (path not protected).
    /// </summary>
    /// <returns>Validation result indicating path is allowed.</returns>
    public static PathValidationResult Allowed() =>
        new() { IsProtected = false };

    /// <summary>
    /// Creates a blocked result from a denylist entry.
    /// </summary>
    /// <param name="entry">The denylist entry that blocked this path.</param>
    /// <returns>Validation result indicating path is blocked.</returns>
    public static PathValidationResult Blocked(DenylistEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);

        return new()
        {
            IsProtected = true,
            MatchedPattern = entry.Pattern,
            Reason = entry.Reason,
            RiskId = entry.RiskId,
            Category = entry.Category,
            Error = ProtectedPathError.FromDenylistEntry(entry)
        };
    }
}
