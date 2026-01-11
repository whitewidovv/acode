namespace Acode.Application.Heuristics;

/// <summary>
/// Interface for routing heuristics that estimate task complexity to inform model selection.
/// Heuristics analyze task metadata and return a score (0-100) with confidence level.
/// </summary>
/// <remarks>
/// <para>AC-001: IRoutingHeuristic in Application.</para>
/// <para>Heuristics enable intelligent, automated model selection based on task characteristics.</para>
/// </remarks>
public interface IRoutingHeuristic
{
    /// <summary>
    /// Gets the unique name of the heuristic (e.g., "FileCount", "TaskType").
    /// </summary>
    /// <remarks>AC-007: Name property exists.</remarks>
    string Name { get; }

    /// <summary>
    /// Gets the execution priority. Lower numbers execute first.
    /// Allows ordering dependencies between heuristics.
    /// </summary>
    /// <remarks>AC-008: Priority property exists.</remarks>
    int Priority { get; }

    /// <summary>
    /// Evaluates the heuristic against the provided context.
    /// </summary>
    /// <param name="context">Task metadata for evaluation.</param>
    /// <returns>Result containing score, confidence, and reasoning.</returns>
    /// <remarks>AC-002: Evaluate method exists. AC-003: Returns HeuristicResult.</remarks>
    HeuristicResult Evaluate(HeuristicContext context);
}
