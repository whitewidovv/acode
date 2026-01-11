namespace Acode.Domain.Search;

/// <summary>
/// Error codes for search-related failures with remediation guidance.
/// </summary>
public static class SearchErrorCodes
{
    /// <summary>
    /// Invalid query syntax (unbalanced parens, invalid operators, etc.).
    /// </summary>
    public const string InvalidQuerySyntax = "ACODE-SRCH-001";

    /// <summary>
    /// Search query exceeded maximum execution time.
    /// </summary>
    public const string QueryTimeout = "ACODE-SRCH-002";

    /// <summary>
    /// Invalid date filter format or range.
    /// </summary>
    public const string InvalidDateFilter = "ACODE-SRCH-003";

    /// <summary>
    /// Invalid role filter value.
    /// </summary>
    public const string InvalidRoleFilter = "ACODE-SRCH-004";

    /// <summary>
    /// Search index is corrupted and needs rebuilding.
    /// </summary>
    public const string IndexCorruption = "ACODE-SRCH-005";

    /// <summary>
    /// Search index has not been initialized.
    /// </summary>
    public const string IndexNotInitialized = "ACODE-SRCH-006";
}
