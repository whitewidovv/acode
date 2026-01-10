namespace Acode.Domain.Roles;

/// <summary>
/// Defines the core agent roles that structure agentic workflows.
/// Each role has specific responsibilities, capabilities, and constraints.
/// </summary>
/// <remarks>
/// <para>FR-009a-001: Four core roles (Default, Planner, Coder, Reviewer) are defined.</para>
/// <para>Roles structure AI-assisted coding into focused, specialized phases:</para>
/// <list type="bullet">
/// <item>Planner: Decomposes complex requests into actionable steps</item>
/// <item>Coder: Implements plan steps with minimal, focused changes</item>
/// <item>Reviewer: Verifies changes for correctness and quality</item>
/// <item>Default: General-purpose, no specialization</item>
/// </list>
/// </remarks>
public enum AgentRole
{
    /// <summary>
    /// General-purpose role with no specialization.
    /// Used for exploratory tasks, answering questions, and explaining code.
    /// </summary>
    /// <remarks>
    /// AC-005: Default value exists. AC-007: Unknown roles fallback to Default.
    /// </remarks>
    Default = 0,

    /// <summary>
    /// Planning role focused on task decomposition and strategy.
    /// Responsible for breaking down complex requests into actionable steps.
    /// </summary>
    /// <remarks>
    /// <para>AC-002: Planner value exists.</para>
    /// <para>Capabilities: read-only file access, analysis tools.</para>
    /// <para>Constraints: cannot modify files or execute commands.</para>
    /// </remarks>
    Planner = 1,

    /// <summary>
    /// Implementation role focused on writing and modifying code.
    /// Responsible for executing plan steps with minimal, focused changes.
    /// </summary>
    /// <remarks>
    /// <para>AC-003: Coder value exists.</para>
    /// <para>Capabilities: full file access, command execution, test running.</para>
    /// <para>Constraints: must follow plan, strict minimal diff.</para>
    /// </remarks>
    Coder = 2,

    /// <summary>
    /// Review role focused on verification and quality assurance.
    /// Responsible for checking changes for correctness, style, and adherence to requirements.
    /// </summary>
    /// <remarks>
    /// <para>AC-004: Reviewer value exists.</para>
    /// <para>Capabilities: read-only access, diff analysis.</para>
    /// <para>Constraints: cannot modify files or execute commands.</para>
    /// </remarks>
    Reviewer = 3,
}
