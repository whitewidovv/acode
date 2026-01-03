namespace Acode.Domain.Modes;

/// <summary>
/// Permission levels for mode-capability combinations.
/// Defines whether a capability is allowed, denied, or conditionally permitted.
/// </summary>
/// <remarks>
/// Used by ModeMatrix to determine runtime permissions per Task 001.a.
/// </remarks>
public enum Permission
{
    /// <summary>
    /// Action is permitted unconditionally.
    /// No prerequisites or checks required.
    /// </summary>
    Allowed,

    /// <summary>
    /// Action is prohibited.
    /// Will fail with error if attempted.
    /// </summary>
    Denied,

    /// <summary>
    /// Action is permitted only if user has given explicit consent.
    /// Used primarily for Burst mode external API calls.
    /// Enforces HC-03: Consent required for Burst mode.
    /// </summary>
    ConditionalOnConsent,

    /// <summary>
    /// Action is permitted only if explicitly enabled in configuration.
    /// Requires repo contract or user config to allowlist.
    /// </summary>
    ConditionalOnConfig,

    /// <summary>
    /// Action is permitted but with limited scope/restrictions.
    /// For example, read system files but only specific allowed paths.
    /// </summary>
    LimitedScope,
}
