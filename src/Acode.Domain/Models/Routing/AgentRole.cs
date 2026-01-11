namespace Acode.Domain.Models.Routing;

/// <summary>
/// Defines the agent's role in the agentic workflow.
/// </summary>
/// <remarks>
/// Roles structure the workflow into specialized phases:
/// - Planner: Task decomposition and strategy
/// - Coder: Implementation and code changes
/// - Reviewer: Verification and quality assurance
/// - Default: General-purpose, no specialization
///
/// Each role has distinct prompts, capabilities, and constraints.
/// Role-based routing enables optimal model selection per phase.
/// </remarks>
public enum AgentRole
{
    /// <summary>
    /// Default role for general-purpose interactions.
    /// </summary>
    /// <remarks>
    /// Used when no specific role is active (e.g., answering questions, explaining code).
    /// </remarks>
    Default = 0,

    /// <summary>
    /// Planning role for task decomposition and architecture decisions.
    /// </summary>
    /// <remarks>
    /// Responsibilities: Break down tasks, identify dependencies, estimate complexity.
    /// Typically routed to large models requiring strong reasoning capabilities.
    /// </remarks>
    Planner = 1,

    /// <summary>
    /// Coding role for implementation and code modifications.
    /// </summary>
    /// <remarks>
    /// Responsibilities: Implement changes following specifications with strict minimal diff.
    /// Typically routed to medium/small models with good instruction-following.
    /// </remarks>
    Coder = 2,

    /// <summary>
    /// Reviewer role for verification and quality assurance.
    /// </summary>
    /// <remarks>
    /// Responsibilities: Analyze code for correctness, identify edge cases, assess quality.
    /// Typically routed to large models requiring critical analysis capabilities.
    /// </remarks>
    Reviewer = 3,
}
