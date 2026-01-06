using Acode.Domain.Models.Routing;

namespace Acode.Application.Models;

/// <summary>
/// Provides access to role definitions and tracks the current active role.
/// </summary>
/// <remarks>
/// The role registry is populated at startup with the core role definitions
/// (Planner, Coder, Reviewer, Default). It tracks the current active role
/// as session state, enabling role transitions during workflow execution.
///
/// Role transitions are explicit - the orchestrator calls SetCurrentRole
/// when moving between workflow phases (e.g., planning → coding → reviewing).
/// </remarks>
public interface IRoleRegistry
{
    /// <summary>
    /// Gets the definition for a specified role.
    /// </summary>
    /// <param name="role">The agent role to retrieve.</param>
    /// <returns>The role definition containing capabilities, constraints, and metadata.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the role is not registered.</exception>
    RoleDefinition GetRole(AgentRole role);

    /// <summary>
    /// Lists all registered role definitions.
    /// </summary>
    /// <returns>Read-only list of all role definitions.</returns>
    /// <remarks>
    /// Returns definitions for all four core roles: Default, Planner, Coder, Reviewer.
    /// The list is ordered by role enum value (Default=0, Planner=1, Coder=2, Reviewer=3).
    /// </remarks>
    IReadOnlyList<RoleDefinition> ListRoles();

    /// <summary>
    /// Gets the currently active role.
    /// </summary>
    /// <returns>The current agent role.</returns>
    /// <remarks>
    /// Returns the role set by the most recent call to SetCurrentRole.
    /// If no role has been set, returns AgentRole.Default.
    /// </remarks>
    AgentRole GetCurrentRole();

    /// <summary>
    /// Sets the current active role and logs the transition.
    /// </summary>
    /// <param name="role">The role to activate.</param>
    /// <param name="reason">Human-readable reason for the transition (logged).</param>
    /// <exception cref="InvalidOperationException">Thrown when the role is not registered.</exception>
    /// <remarks>
    /// Role transitions are logged for audit purposes. Example transitions:
    /// - "Starting planning phase" (Default → Planner)
    /// - "Beginning implementation" (Planner → Coder)
    /// - "Reviewing generated code" (Coder → Reviewer)
    ///
    /// The reason parameter provides context for why the transition occurred,
    /// enabling debugging and workflow analysis.
    /// </remarks>
    void SetCurrentRole(AgentRole role, string reason);
}
