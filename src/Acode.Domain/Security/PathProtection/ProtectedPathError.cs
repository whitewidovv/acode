namespace Acode.Domain.Security.PathProtection;

/// <summary>
/// Represents an error when a path is blocked by protection rules.
/// Contains detailed information for audit logging and user feedback.
/// </summary>
public sealed class ProtectedPathError
{
    /// <summary>
    /// Gets the error code (ACODE-SEC-003-XXX format).
    /// </summary>
    public required string ErrorCode { get; init; }

    /// <summary>
    /// Gets the human-readable error message.
    /// </summary>
    public required string Message { get; init; }

    /// <summary>
    /// Gets the pattern that matched the blocked path.
    /// </summary>
    public required string Pattern { get; init; }

    /// <summary>
    /// Gets the risk identifier (e.g., RISK-I-003).
    /// </summary>
    public required string RiskId { get; init; }

    /// <summary>
    /// Gets the category of the protected path.
    /// </summary>
    public required PathCategory Category { get; init; }

    /// <summary>
    /// Creates a ProtectedPathError from a DenylistEntry.
    /// </summary>
    /// <param name="entry">The denylist entry that matched.</param>
    /// <returns>A new ProtectedPathError instance.</returns>
    public static ProtectedPathError FromDenylistEntry(DenylistEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);

        return new ProtectedPathError
        {
            ErrorCode = GetErrorCode(entry.Category),
            Message = $"Access blocked: {entry.Reason}",
            Pattern = entry.Pattern,
            RiskId = entry.RiskId,
            Category = entry.Category
        };
    }

    private static string GetErrorCode(PathCategory category)
    {
        return category switch
        {
            PathCategory.SshKeys => "ACODE-SEC-003-001",
            PathCategory.GpgKeys => "ACODE-SEC-003-002",
            PathCategory.CloudCredentials => "ACODE-SEC-003-003",
            PathCategory.EnvironmentFiles => "ACODE-SEC-003-004",
            PathCategory.SystemFiles => "ACODE-SEC-003-005",
            PathCategory.SecretFiles => "ACODE-SEC-003-006",
            PathCategory.PackageManagerCredentials => "ACODE-SEC-003-007",
            PathCategory.GitCredentials => "ACODE-SEC-003-008",
            PathCategory.UserDefined => "ACODE-SEC-003-009",
            _ => "ACODE-SEC-003-000"
        };
    }
}
