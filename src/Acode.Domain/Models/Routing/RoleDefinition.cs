namespace Acode.Domain.Models.Routing;

/// <summary>
/// Defines the characteristics and capabilities of an agent role.
/// </summary>
/// <remarks>
/// Each role has:
/// - Distinct responsibilities (what it can do)
/// - Explicit constraints (what it cannot do)
/// - Associated prompts (how it behaves)
/// - Context strategy (what information it receives)
///
/// Role definitions are immutable and established at startup.
/// </remarks>
public sealed record RoleDefinition
{
    /// <summary>
    /// Gets the agent role this definition describes.
    /// </summary>
    public required AgentRole Role { get; init; }

    /// <summary>
    /// Gets the role name (lowercase identifier).
    /// </summary>
    /// <example>"planner", "coder", "reviewer".</example>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the human-readable description of the role's purpose.
    /// </summary>
    public required string Description { get; init; }

    /// <summary>
    /// Gets the list of capabilities this role possesses.
    /// </summary>
    /// <remarks>
    /// Capabilities are operations the role is allowed to perform.
    /// Examples: "write_file", "analyze_diff", "plan_tasks".
    /// </remarks>
    public required IReadOnlyList<string> Capabilities { get; init; }

    /// <summary>
    /// Gets the list of constraints that limit this role's actions.
    /// </summary>
    /// <remarks>
    /// Constraints are explicit limitations on the role.
    /// Examples: "cannot_modify_files", "read_only_access", "strict_minimal_diff".
    /// </remarks>
    public required IReadOnlyList<string> Constraints { get; init; }
}
