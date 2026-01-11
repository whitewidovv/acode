namespace Acode.Application.Heuristics;

/// <summary>
/// Result of a heuristic evaluation containing score, confidence, and human-readable reasoning.
/// </summary>
/// <remarks>
/// <para>AC-003: HeuristicResult returned from Evaluate.</para>
/// <para>AC-004: Score property (0-100).</para>
/// <para>AC-005: Confidence property (0.0-1.0).</para>
/// <para>AC-006: Reasoning property for debugging.</para>
/// </remarks>
public sealed class HeuristicResult
{
    /// <summary>
    /// Gets the complexity score from 0 (simple) to 100 (complex).
    /// </summary>
    /// <remarks>AC-004: Result has score. AC-015: Range 0-100.</remarks>
    public required int Score { get; init; }

    /// <summary>
    /// Gets the confidence in this score from 0.0 (uncertain) to 1.0 (certain).
    /// Used for weighted aggregation in HeuristicEngine.
    /// </summary>
    /// <remarks>AC-005: Result has confidence.</remarks>
    public required double Confidence { get; init; }

    /// <summary>
    /// Gets the human-readable explanation of why this score was assigned.
    /// Logged for debugging and displayed in CLI introspection commands.
    /// </summary>
    /// <remarks>AC-006: Result has reasoning. AC-025: All include reasoning.</remarks>
    public required string Reasoning { get; init; }

    /// <summary>
    /// Validates that score and confidence are in valid ranges.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when score or confidence is out of range.</exception>
    /// <exception cref="ArgumentException">Thrown when reasoning is empty.</exception>
    public void Validate()
    {
        if (Score < 0 || Score > 100)
        {
            throw new ArgumentOutOfRangeException(
                nameof(Score),
                Score,
                "Score must be between 0 and 100"
            );
        }

        if (Confidence < 0.0 || Confidence > 1.0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(Confidence),
                Confidence,
                "Confidence must be between 0.0 and 1.0"
            );
        }

        if (string.IsNullOrWhiteSpace(Reasoning))
        {
            throw new ArgumentException("Reasoning must not be empty", nameof(Reasoning));
        }
    }
}
