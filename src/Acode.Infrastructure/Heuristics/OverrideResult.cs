namespace Acode.Infrastructure.Heuristics;

/// <summary>
/// Result of override resolution.
/// </summary>
public sealed class OverrideResult
{
    /// <summary>
    /// Gets the resolved model ID.
    /// </summary>
    public required string ModelId { get; init; }

    /// <summary>
    /// Gets the source of the override.
    /// </summary>
    public required OverrideSource Source { get; init; }
}
