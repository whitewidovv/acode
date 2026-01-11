namespace Acode.Application.Routing;

/// <summary>
/// Defines the agent roles supported by the routing policy.
/// </summary>
/// <remarks>
/// Each role has different model requirements:
/// - Planner: Requires strong reasoning capabilities for task decomposition
/// - Coder: Requires precise instruction following and code syntax knowledge
/// - Reviewer: Requires critical analysis and edge case identification
///
/// FR-009-013 through FR-009-016: Agent roles defined for routing decisions.
/// </remarks>
public enum AgentRole
{
    /// <summary>
    /// Default role when no specific role is assigned. Uses default_model from configuration.
    /// </summary>
    /// <remarks>
    /// AC-016: Default role uses the default_model configuration.
    /// </remarks>
    Default = 0,

    /// <summary>
    /// Planning role—breaks down high-level tasks into actionable steps.
    /// Requires strong reasoning capabilities.
    /// Typically assigned to large models (70B parameters).
    /// </summary>
    /// <remarks>
    /// AC-013: Planner role defined for task decomposition.
    /// </remarks>
    Planner = 1,

    /// <summary>
    /// Coding role—implements concrete code changes.
    /// Requires precise instruction following and code syntax knowledge.
    /// Typically assigned to medium or small models (7B-13B parameters).
    /// </summary>
    /// <remarks>
    /// AC-014: Coder role defined for implementation tasks.
    /// </remarks>
    Coder = 2,

    /// <summary>
    /// Reviewer role—verifies correctness and provides feedback.
    /// Requires critical analysis and edge case identification.
    /// Typically assigned to large models (70B parameters).
    /// </summary>
    /// <remarks>
    /// AC-015: Reviewer role defined for verification tasks.
    /// </remarks>
    Reviewer = 3,
}
