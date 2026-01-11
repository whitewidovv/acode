namespace Acode.Application.Roles;

using Acode.Domain.Roles;

/// <summary>
/// Represents a single role transition event in the audit trail.
/// Immutable record of when, why, and what role transition occurred.
/// </summary>
/// <remarks>
/// AC-035 to AC-040: State management for role transitions.
/// </remarks>
public sealed record RoleTransitionEntry
{
    /// <summary>
    /// Gets the role transitioned from.
    /// </summary>
    /// <remarks>Null for initial role setting.</remarks>
    public AgentRole? FromRole { get; init; }

    /// <summary>
    /// Gets the role transitioned to.
    /// </summary>
    public required AgentRole ToRole { get; init; }

    /// <summary>
    /// Gets the human-readable reason for the transition.
    /// </summary>
    public required string Reason { get; init; }

    /// <summary>
    /// Gets the UTC timestamp when the transition occurred.
    /// </summary>
    public required DateTime Timestamp { get; init; }
}
