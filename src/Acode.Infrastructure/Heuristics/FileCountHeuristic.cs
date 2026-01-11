namespace Acode.Infrastructure.Heuristics;

using Acode.Application.Heuristics;

/// <summary>
/// Heuristic that scores complexity based on number of files affected.
/// Fewer files = simpler task, more files = more complex task.
/// </summary>
/// <remarks>
/// AC-021: FileCountHeuristic works. AC-024: Returns valid scores. AC-025: Includes reasoning.
/// </remarks>
public sealed class FileCountHeuristic : IRoutingHeuristic
{
    /// <inheritdoc />
    public string Name => "FileCount";

    /// <inheritdoc />
    public int Priority => 1; // Run first - objective metric

    /// <inheritdoc />
    public HeuristicResult Evaluate(HeuristicContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var fileCount = context.Files.Count;

        // Score assignment
        var (score, confidence) = fileCount switch
        {
            0 => (0, 0.5), // No files - uncertain
            1 => (10, 0.9), // Single file - very simple
            2 => (20, 0.85), // Two files - simple
            <= 5 => (35, 0.8), // Few files - medium-low
            <= 10 => (55, 0.85), // Several files - medium
            <= 20 => (75, 0.9), // Many files - complex
            _ => (90, 0.95), // Very many files - very complex
        };

        // Lower confidence at threshold boundaries
        if (fileCount == 3 || fileCount == 10)
        {
            confidence -= 0.2;
        }

        var reasoning = fileCount switch
        {
            0 => "No files specified",
            1 => $"Single file: {context.Files[0]}",
            <= 5 => $"{fileCount} files - limited scope",
            <= 10 => $"{fileCount} files - moderate scope",
            _ => $"{fileCount} files - large scope",
        };

        return new HeuristicResult
        {
            Score = score,
            Confidence = confidence,
            Reasoning = reasoning,
        };
    }
}
