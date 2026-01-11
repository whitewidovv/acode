namespace Acode.Infrastructure.Heuristics;

using System.Diagnostics;
using Acode.Application.Heuristics;
using Microsoft.Extensions.Logging;

/// <summary>
/// Orchestrates execution of all registered heuristics and aggregates results.
/// Implements weighted averaging based on confidence levels.
/// </summary>
/// <remarks>
/// <para>AC-009: HeuristicEngine in Infrastructure.</para>
/// <para>AC-010: Runs all heuristics.</para>
/// <para>AC-011: Aggregates results.</para>
/// <para>AC-012: Weights by confidence.</para>
/// </remarks>
public sealed class HeuristicEngine
{
    private readonly IEnumerable<IRoutingHeuristic> _heuristics;
    private readonly HeuristicConfiguration _config;
    private readonly ILogger<HeuristicEngine> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="HeuristicEngine"/> class.
    /// </summary>
    /// <param name="heuristics">The registered heuristics.</param>
    /// <param name="config">The heuristic configuration.</param>
    /// <param name="logger">The logger.</param>
    public HeuristicEngine(
        IEnumerable<IRoutingHeuristic> heuristics,
        HeuristicConfiguration config,
        ILogger<HeuristicEngine> logger
    )
    {
        ArgumentNullException.ThrowIfNull(heuristics);
        ArgumentNullException.ThrowIfNull(config);
        ArgumentNullException.ThrowIfNull(logger);

        _heuristics = heuristics;
        _config = config;
        _logger = logger;
    }

    /// <summary>
    /// Evaluates all heuristics against the context and returns aggregated score.
    /// </summary>
    /// <param name="context">The heuristic context to evaluate.</param>
    /// <returns>The aggregated complexity score.</returns>
    public ComplexityScore Evaluate(HeuristicContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (!_config.Enabled)
        {
            _logger.LogInformation("Heuristics disabled. Returning default medium score.");
            return CreateDefaultScore();
        }

        var stopwatch = Stopwatch.StartNew();
        var results = new List<(string Name, HeuristicResult Result)>();

        // Execute heuristics in priority order
        foreach (var heuristic in _heuristics.OrderBy(h => h.Priority))
        {
            // Skip disabled heuristics
            if (
                _config.DisabledHeuristics?.Contains(
                    heuristic.Name,
                    StringComparer.OrdinalIgnoreCase
                ) == true
            )
            {
                _logger.LogDebug("Skipping disabled heuristic: {Name}", heuristic.Name);
                continue;
            }

            try
            {
                var heuristicStopwatch = Stopwatch.StartNew();
                var result = heuristic.Evaluate(context);
                heuristicStopwatch.Stop();

                result.Validate();

                // Apply configured weight
                var weight = _config.Weights.GetValueOrDefault(
                    heuristic.Name.ToLowerInvariant(),
                    1.0
                );

                var weightedScore = Math.Clamp((int)(result.Score * weight), 0, 100);
                var weightedResult = new HeuristicResult
                {
                    Score = weightedScore,
                    Confidence = result.Confidence,
                    Reasoning = result.Reasoning,
                };

                results.Add((heuristic.Name, weightedResult));

                // AC-014: Logs evaluations
                _logger.LogDebug(
                    "Heuristic {Name} evaluated: score={Score} (weighted: {Weighted}), "
                        + "confidence={Confidence}, duration={Duration}ms",
                    heuristic.Name,
                    result.Score,
                    weightedScore,
                    result.Confidence,
                    heuristicStopwatch.ElapsedMilliseconds
                );
            }
            catch (Exception ex)
            {
                // AC-058: Failed heuristic skipped
                _logger.LogError(
                    ex,
                    "Heuristic {Name} failed during evaluation. Skipping.",
                    heuristic.Name
                );

                // Continue with remaining heuristics
            }
        }

        stopwatch.Stop();

        // AC-059: All failures use default score
        if (results.Count == 0)
        {
            _logger.LogError(
                "All heuristics failed for task: {Task}. Falling back to default score.",
                TruncateDescription(context.TaskDescription)
            );
            return CreateDefaultScore();
        }

        // Weighted aggregation by confidence
        var weightedSum = results.Sum(r => r.Result.Score * r.Result.Confidence);
        var totalWeight = results.Sum(r => r.Result.Confidence);
        var combinedScore = totalWeight > 0 ? (int)(weightedSum / totalWeight) : 50;

        var complexityScore = new ComplexityScore(
            combinedScore,
            results,
            _config.Thresholds.Low,
            _config.Thresholds.High
        );

        _logger.LogInformation(
            "Heuristic evaluation complete: combined_score={Score}, tier={Tier}, "
                + "heuristic_count={Count}, duration={Duration}ms",
            complexityScore.CombinedScore,
            complexityScore.Tier,
            results.Count,
            stopwatch.ElapsedMilliseconds
        );

        return complexityScore;
    }

    /// <summary>
    /// Gets all registered heuristics for introspection.
    /// </summary>
    /// <returns>List of heuristic names, priorities, and enabled status.</returns>
    public IReadOnlyList<(string Name, int Priority, bool Enabled)> GetRegisteredHeuristics()
    {
        return _heuristics
            .OrderBy(h => h.Priority)
            .Select(h =>
                (
                    h.Name,
                    h.Priority,
                    !(
                        _config.DisabledHeuristics?.Contains(
                            h.Name,
                            StringComparer.OrdinalIgnoreCase
                        ) ?? false
                    )
                )
            )
            .ToList();
    }

    private static string TruncateDescription(string description)
    {
        const int maxLength = 100;
        return description.Length <= maxLength
            ? description
            : string.Concat(description.AsSpan(0, maxLength), "...");
    }

    private ComplexityScore CreateDefaultScore()
    {
        return new ComplexityScore(
            50, // Medium score
            Array.Empty<(string, HeuristicResult)>(),
            _config.Thresholds.Low,
            _config.Thresholds.High
        );
    }
}
