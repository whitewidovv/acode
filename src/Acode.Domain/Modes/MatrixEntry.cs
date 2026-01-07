namespace Acode.Domain.Modes;

/// <summary>
/// Represents a single entry in the mode matrix.
/// Maps a mode-capability combination to a permission and rationale.
/// </summary>
/// <param name="Mode">The operating mode.</param>
/// <param name="Capability">The capability being checked.</param>
/// <param name="Permission">The permission level granted.</param>
/// <param name="Rationale">Explanation for why this permission is granted/denied.</param>
/// <param name="Prerequisite">Optional prerequisite condition for conditional permissions.</param>
/// <remarks>
/// Immutable record type. Used by ModeMatrix for lookups per Task 001.a.
/// Prerequisite is used for ConditionalOnConsent and ConditionalOnConfig permissions
/// to specify what condition must be met.
/// </remarks>
public sealed record MatrixEntry(
    OperatingMode Mode,
    Capability Capability,
    Permission Permission,
    string Rationale,
    string? Prerequisite = null);
