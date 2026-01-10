namespace Acode.Application.Roles;

using Acode.Domain.Roles;

/// <summary>
/// Service interface for managing agent roles.
/// Provides role lookup, current role tracking, and role transition management.
/// </summary>
/// <remarks>
/// AC-015 to AC-019: IRoleRegistry interface and methods.
/// </remarks>
public interface IRoleRegistry
{
    /// <summary>
    /// Gets the definition for a specific role.
    /// </summary>
    /// <param name="role">The role to retrieve.</param>
    /// <returns>Complete role definition.</returns>
    /// <exception cref="ArgumentException">Thrown if role is not recognized.</exception>
    /// <remarks>AC-016: GetRole method exists.</remarks>
    RoleDefinition GetRole(AgentRole role);

    /// <summary>
    /// Lists all available roles in the system.
    /// </summary>
    /// <returns>Read-only list of all role definitions.</returns>
    /// <remarks>AC-017: ListRoles method exists.</remarks>
    IReadOnlyList<RoleDefinition> ListRoles();

    /// <summary>
    /// Gets the currently active role for this session.
    /// </summary>
    /// <returns>The active role. Defaults to <see cref="AgentRole.Default"/>.</returns>
    /// <remarks>AC-018: GetCurrentRole method exists. AC-036: Initial is Default.</remarks>
    AgentRole GetCurrentRole();

    /// <summary>
    /// Transitions to a new role with the given reason.
    /// Validates transition rules and logs the transition for audit purposes.
    /// </summary>
    /// <param name="role">The role to transition to.</param>
    /// <param name="reason">Human-readable reason for the transition.</param>
    /// <exception cref="InvalidRoleTransitionException">Thrown if transition violates preconditions.</exception>
    /// <remarks>AC-019: SetCurrentRole method exists. AC-037: Changes explicit. AC-039: Logged on change.</remarks>
    void SetCurrentRole(AgentRole role, string reason);

    /// <summary>
    /// Gets the history of role transitions for the current session.
    /// </summary>
    /// <returns>List of transition entries in chronological order.</returns>
    IReadOnlyList<RoleTransitionEntry> GetRoleHistory();
}
