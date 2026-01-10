namespace Acode.Infrastructure.Heuristics;

/// <summary>
/// Configuration for heuristic system loaded from config.
/// </summary>
/// <remarks>
/// AC-020: Tiers configurable. AC-061-065: Configuration validation.
/// </remarks>
public sealed class HeuristicConfiguration
{
    /// <summary>
    /// Gets or sets a value indicating whether heuristics are enabled.
    /// When disabled, routing falls back to strategy defaults.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets the weight multipliers for each heuristic.
    /// Higher weights give a heuristic more influence in the combined score.
    /// </summary>
    public Dictionary<string, double> Weights { get; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Gets or sets the complexity thresholds.
    /// </summary>
    public ComplexityThresholds Thresholds { get; set; } = new();

    /// <summary>
    /// Gets the list of heuristic names to disable.
    /// </summary>
    public HashSet<string> DisabledHeuristics { get; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Validates the configuration and throws if invalid.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when configuration is invalid.</exception>
    public void Validate()
    {
        // Validate weights are positive
        foreach (var (name, weight) in Weights)
        {
            if (weight < 0)
            {
                throw new InvalidOperationException(
                    $"Heuristic weight for '{name}' must be non-negative, was {weight}"
                );
            }
        }

        // Validate threshold ordering
        if (Thresholds.Low >= Thresholds.High)
        {
            throw new InvalidOperationException(
                $"Low threshold ({Thresholds.Low}) must be less than high threshold ({Thresholds.High})"
            );
        }

        if (Thresholds.Low < 0 || Thresholds.Low > 100)
        {
            throw new InvalidOperationException(
                $"Low threshold must be between 0 and 100, was {Thresholds.Low}"
            );
        }

        if (Thresholds.High < 0 || Thresholds.High > 100)
        {
            throw new InvalidOperationException(
                $"High threshold must be between 0 and 100, was {Thresholds.High}"
            );
        }
    }
}

/// <summary>
/// Complexity score thresholds for tier determination.
/// </summary>
public sealed class ComplexityThresholds
{
    /// <summary>
    /// Gets or sets the threshold below which scores are considered low complexity.
    /// Default is 30.
    /// </summary>
    public int Low { get; set; } = 30;

    /// <summary>
    /// Gets or sets the threshold at or above which scores are considered high complexity.
    /// Default is 70.
    /// </summary>
    public int High { get; set; } = 70;
}
