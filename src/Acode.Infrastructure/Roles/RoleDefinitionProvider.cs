namespace Acode.Infrastructure.Roles;

using Acode.Domain.Roles;

/// <summary>
/// Provides hardcoded definitions for the four core agent roles.
/// In MVP, role definitions are not configurable—they're fixed in code.
/// </summary>
/// <remarks>
/// AC-020 to AC-034: Role definitions for Planner, Coder, Reviewer.
/// </remarks>
internal static class RoleDefinitionProvider
{
    /// <summary>
    /// Gets all core role definitions.
    /// </summary>
    /// <returns>Read-only list of the four core role definitions.</returns>
    public static IReadOnlyList<RoleDefinition> GetCoreRoles() =>
        new[] { GetDefaultRole(), GetPlannerRole(), GetCoderRole(), GetReviewerRole() };

    private static RoleDefinition GetDefaultRole() =>
        new()
        {
            Role = AgentRole.Default,
            Name = "Default",
            Description =
                "General-purpose role with no specialization. Handles exploratory tasks, questions, and explanations.",
            Capabilities = new[] { "all" }, // No tool restrictions for Default role
            Constraints = Array.Empty<string>(),
            PromptKey = "system.md",
            ContextStrategy = ContextStrategy.Adaptive,
        };

    private static RoleDefinition GetPlannerRole() =>
        new()
        {
            Role = AgentRole.Planner,
            Name = "Planner",
            Description =
                "Task decomposition and planning. Analyzes requests and creates structured implementation plans with clear steps and dependencies.",
            Capabilities = new[]
            {
                "read_file",
                "list_directory",
                "grep_search",
                "semantic_search",
            },
            Constraints = new[]
            {
                "Cannot modify files (read-only access)",
                "Cannot execute commands",
                "Must not provide implementation details (focus on WHAT, not HOW)",
            },
            PromptKey = "roles/planner.md",
            ContextStrategy = ContextStrategy.Broad,
        };

    private static RoleDefinition GetCoderRole() =>
        new()
        {
            Role = AgentRole.Coder,
            Name = "Coder",
            Description =
                "Implementation and code changes. Executes plan steps with minimal, focused diffs following the plan strictly.",
            Capabilities = new[]
            {
                "read_file",
                "write_file",
                "create_file",
                "delete_file",
                "execute_command",
                "run_tests",
                "list_directory",
                "grep_search",
            },
            Constraints = new[]
            {
                "Must follow the plan (no scope creep)",
                "Strict minimal diff (only necessary changes)",
                "Cannot deviate from task without explanation",
            },
            PromptKey = "roles/coder.md",
            ContextStrategy = ContextStrategy.Focused,
        };

    private static RoleDefinition GetReviewerRole() =>
        new()
        {
            Role = AgentRole.Reviewer,
            Name = "Reviewer",
            Description =
                "Verification and quality assurance. Reviews changes for correctness, style, security, and adherence to requirements.",
            Capabilities = new[] { "read_file", "list_directory", "analyze_diff", "grep_search" },
            Constraints = new[]
            {
                "Cannot modify files (read-only access)",
                "Cannot execute commands",
                "Provides feedback only (cannot make changes directly)",
            },
            PromptKey = "roles/reviewer.md",
            ContextStrategy = ContextStrategy.ChangeFocused,
        };
}
