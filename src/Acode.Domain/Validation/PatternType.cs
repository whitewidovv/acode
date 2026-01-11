namespace Acode.Domain.Validation;

/// <summary>
/// Type of pattern used for endpoint matching.
/// </summary>
/// <remarks>
/// Per Task 001.b FR-001b-13, FR-001b-34, FR-001b-35:
/// Supports exact matching, wildcard matching, and regex matching
/// for denylist and allowlist patterns.
/// </remarks>
public enum PatternType
{
    /// <summary>
    /// Exact hostname match (e.g., "api.openai.com").
    /// </summary>
    Exact = 0,

    /// <summary>
    /// Wildcard subdomain match (e.g., "*.openai.com" matches "chat.openai.com").
    /// </summary>
    Wildcard = 1,

    /// <summary>
    /// Regular expression match (e.g., "bedrock.*\\.amazonaws\\.com").
    /// </summary>
    Regex = 2,
}
