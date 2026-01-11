namespace Acode.Application.Heuristics;

/// <summary>
/// Aggregated complexity score from all heuristic evaluations.
/// Contains the weighted combined score and individual heuristic results.
/// </summary>
/// <remarks>
/// AC-013: Returns combined score. AC-019: Maps to tiers.
/// </remarks>
public sealed class ComplexityScore
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ComplexityScore"/> class.
    /// </summary>
    /// <param name="combinedScore">The combined weighted score from all heuristics.</param>
    /// <param name="individualResults">The individual results from each heuristic.</param>
    /// <param name="lowThreshold">The threshold below which scores are considered low complexity.</param>
    /// <param name="highThreshold">The threshold above which scores are considered high complexity.</param>
    public ComplexityScore(
        int combinedScore,
        IReadOnlyList<(string Name, HeuristicResult Result)> individualResults,
        int lowThreshold = 30,
        int highThreshold = 70
    )
    {
        CombinedScore = Math.Clamp(combinedScore, 0, 100);
        IndividualResults = individualResults;
        LowThreshold = lowThreshold;
        HighThreshold = highThreshold;

        Tier =
            CombinedScore <= lowThreshold ? ComplexityTier.Low
            : CombinedScore >= highThreshold ? ComplexityTier.High
            : ComplexityTier.Medium;
    }

    /// <summary>
    /// Gets the combined weighted score (0-100) from all heuristics.
    /// </summary>
    public int CombinedScore { get; }

    /// <summary>
    /// Gets the complexity tier based on configured thresholds.
    /// </summary>
    public ComplexityTier Tier { get; }

    /// <summary>
    /// Gets the individual results from each heuristic for debugging.
    /// </summary>
    public IReadOnlyList<(string Name, HeuristicResult Result)> IndividualResults { get; }

    /// <summary>
    /// Gets the low threshold used for tier calculation.
    /// </summary>
    public int LowThreshold { get; }

    /// <summary>
    /// Gets the high threshold used for tier calculation.
    /// </summary>
    public int HighThreshold { get; }
}
