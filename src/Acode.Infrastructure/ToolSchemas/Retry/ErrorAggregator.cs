using Acode.Application.ToolSchemas.Retry;

namespace Acode.Infrastructure.ToolSchemas.Retry;

/// <summary>
/// Aggregates, deduplicates, and sorts validation errors for optimal model comprehension.
/// </summary>
/// <remarks>
/// Spec Reference: Implementation Prompt lines 3809-3856.
/// Deduplicates by FieldPath+ErrorCode, sorts by severity (descending) then path (ascending).
/// </remarks>
public sealed class ErrorAggregator
{
    private readonly int maxErrors;

    /// <summary>
    /// Initializes a new instance of the <see cref="ErrorAggregator"/> class.
    /// </summary>
    /// <param name="maxErrors">Maximum number of errors to return.</param>
    public ErrorAggregator(int maxErrors)
    {
        this.maxErrors = maxErrors;
    }

    /// <summary>
    /// Aggregates validation errors by deduplicating, sorting, and limiting count.
    /// </summary>
    /// <param name="errors">The errors to aggregate.</param>
    /// <returns>Aggregated list of errors.</returns>
    public IReadOnlyList<ValidationError> Aggregate(IEnumerable<ValidationError>? errors)
    {
        if (errors is null)
        {
            return Array.Empty<ValidationError>();
        }

        // Deduplicate by field path + error code (keep first occurrence)
        var deduplicated = errors
            .GroupBy(e => $"{e.FieldPath}|{e.ErrorCode}")
            .Select(g => g.First())
            .ToList();

        // Sort by severity (descending: Error > Warning > Info), then by field path (ascending)
        var sorted = deduplicated
            .OrderByDescending(e => e.Severity)
            .ThenBy(e => e.FieldPath, StringComparer.Ordinal)
            .ToList();

        // Limit count
        if (sorted.Count > this.maxErrors)
        {
            sorted = sorted.Take(this.maxErrors).ToList();
        }

        return sorted;
    }
}
