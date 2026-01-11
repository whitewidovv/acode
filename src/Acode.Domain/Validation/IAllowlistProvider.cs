namespace Acode.Domain.Validation;

/// <summary>
/// Provider for allowlist entries.
/// </summary>
/// <remarks>
/// Per Task 001.b FR-001b-41 to FR-001b-55:
/// Provides the list of explicitly allowed endpoints that bypass mode restrictions.
/// </remarks>
public interface IAllowlistProvider
{
    /// <summary>
    /// Gets the default allowlist entries.
    /// </summary>
    /// <returns>Read-only list of allowlist entries.</returns>
    IReadOnlyList<AllowlistEntry> GetDefaultAllowlist();

    /// <summary>
    /// Checks if a URI is allowed by any entry in the allowlist.
    /// </summary>
    /// <param name="uri">URI to check.</param>
    /// <returns>True if URI is allowed, false otherwise.</returns>
    bool IsAllowed(Uri uri);
}
