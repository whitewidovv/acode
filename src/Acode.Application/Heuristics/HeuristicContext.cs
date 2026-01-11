namespace Acode.Application.Heuristics;

using Acode.Domain.Roles;

/// <summary>
/// Context containing task metadata provided to heuristics for evaluation.
/// </summary>
public sealed class HeuristicContext
{
    /// <summary>
    /// Gets the user-provided task description.
    /// Used by TaskTypeHeuristic for keyword analysis.
    /// </summary>
    public required string TaskDescription { get; init; }

    /// <summary>
    /// Gets the list of file paths affected by this task.
    /// Used by FileCountHeuristic and LanguageHeuristic.
    /// </summary>
    public required IReadOnlyList<string> Files { get; init; }

    /// <summary>
    /// Gets the optional agent role from Task 009.a (Planner, Coder, Reviewer).
    /// Can influence combined routing decisions.
    /// </summary>
    public AgentRole? Role { get; init; }

    /// <summary>
    /// Gets the optional metadata dictionary for custom heuristics.
    /// Enables extensibility without modifying core context.
    /// </summary>
    public IReadOnlyDictionary<string, object>? Metadata { get; init; }
}
