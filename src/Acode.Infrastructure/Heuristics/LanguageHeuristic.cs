namespace Acode.Infrastructure.Heuristics;

using Acode.Application.Heuristics;

/// <summary>
/// Heuristic that scores complexity based on programming language.
/// More complex languages (C++, Rust) score higher than simple ones (Markdown, JSON).
/// </summary>
/// <remarks>
/// AC-023: LanguageHeuristic works. AC-024: Returns valid scores. AC-025: Includes reasoning.
/// </remarks>
public sealed class LanguageHeuristic : IRoutingHeuristic
{
    /// <summary>
    /// Language complexity ratings by file extension.
    /// </summary>
    private static readonly Dictionary<string, int> LanguageComplexity = new(
        StringComparer.OrdinalIgnoreCase
    )
    {
        // Simple formats
        [".md"] = 5,
        [".txt"] = 5,
        [".json"] = 10,
        [".yml"] = 10,
        [".yaml"] = 10,
        [".xml"] = 10,

        // Scripting languages
        [".sh"] = 15,
        [".bash"] = 15,
        [".ps1"] = 20,

        // Standard languages
        [".js"] = 25,
        [".ts"] = 30,
        [".py"] = 25,
        [".rb"] = 25,
        [".go"] = 30,
        [".java"] = 35,
        [".cs"] = 35,

        // Complex languages
        [".cpp"] = 40,
        [".cc"] = 40,
        [".c"] = 35,
        [".rs"] = 45,
        [".hs"] = 45,
    };

    /// <inheritdoc />
    public string Name => "Language";

    /// <inheritdoc />
    public int Priority => 3; // Run after FileCount and TaskType

    /// <inheritdoc />
    public HeuristicResult Evaluate(HeuristicContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (context.Files.Count == 0)
        {
            return new HeuristicResult
            {
                Score = 0,
                Confidence = 0.0,
                Reasoning = "No files to analyze",
            };
        }

        var scores = context
            .Files.Select(Path.GetExtension)
            .Where(ext => !string.IsNullOrEmpty(ext))
            .Select(ext => LanguageComplexity.TryGetValue(ext!, out var score) ? score : 25)
            .ToList();

        if (scores.Count == 0)
        {
            return new HeuristicResult
            {
                Score = 25, // Default score
                Confidence = 0.3,
                Reasoning = "Unable to detect file languages",
            };
        }

        var avgScore = (int)scores.Average();
        var languages = context
            .Files.Select(Path.GetExtension)
            .Where(ext => !string.IsNullOrEmpty(ext))
            .Distinct()
            .ToList();

        var reasoning =
            languages.Count == 1
                ? $"Single language: {languages[0]}"
                : $"Multiple languages: {string.Join(", ", languages)}";

        return new HeuristicResult
        {
            Score = avgScore,
            Confidence = 0.9,
            Reasoning = reasoning,
        };
    }
}
