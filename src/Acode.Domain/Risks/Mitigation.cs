namespace Acode.Domain.Risks;

/// <summary>
/// Represents a mitigation strategy for a risk.
/// </summary>
public sealed record Mitigation
{
    /// <summary>
    /// Gets the unique mitigation identifier.
    /// Format: MIT-NNN.
    /// </summary>
    public required string MitigationId { get; init; }

    /// <summary>
    /// Gets the description of the mitigation strategy.
    /// </summary>
    public required string Description { get; init; }

    /// <summary>
    /// Gets the implementation status of this mitigation.
    /// </summary>
    public required MitigationStatus Status { get; init; }

    /// <summary>
    /// Gets the implementation details or location.
    /// Optional - may reference code files, configuration, or procedures.
    /// </summary>
    public string? Implementation { get; init; }
}
