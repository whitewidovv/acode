namespace Acode.Domain.Modes;

/// <summary>
/// Operating modes controlling Acode's network and API access.
/// Defines the security posture for all operations.
/// </summary>
/// <remarks>
/// These modes enforce HC-01 (No External LLM API) constraint per Task 001.
/// </remarks>
public enum OperatingMode
{
    /// <summary>
    /// Default mode. Local inference only, no external LLM APIs.
    /// Enforces HC-01: No external LLM API calls.
    /// </summary>
    LocalOnly = 0,

    /// <summary>
    /// Temporary mode allowing external LLM APIs with explicit consent.
    /// Session-scoped only; cannot be persisted.
    /// Requires consent per HC-03.
    /// </summary>
    Burst = 1,

    /// <summary>
    /// Permanent mode with no network access whatsoever.
    /// Cannot be changed at runtime per HC-02.
    /// </summary>
    Airgapped = 2,
}
