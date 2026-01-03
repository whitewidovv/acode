namespace Acode.Domain.Risks;

/// <summary>
/// Represents a security risk in the threat model.
/// </summary>
public sealed record Risk
{
    /// <summary>
    /// Gets the unique risk identifier.
    /// </summary>
    public required RiskId RiskId { get; init; }

    /// <summary>
    /// Gets the risk title.
    /// </summary>
    public required string Title { get; init; }

    /// <summary>
    /// Gets the detailed risk description.
    /// </summary>
    public required string Description { get; init; }

    /// <summary>
    /// Gets the STRIDE category of this risk.
    /// </summary>
    public required RiskCategory Category { get; init; }

    /// <summary>
    /// Gets the DREAD score for this risk.
    /// </summary>
    public required DreadScore DreadScore { get; init; }

    /// <summary>
    /// Gets the calculated severity based on DREAD score.
    /// </summary>
    public Severity Severity => DreadScore.Severity;

    /// <summary>
    /// Gets the mitigations for this risk.
    /// </summary>
    public required IReadOnlyList<Mitigation> Mitigations { get; init; }

    /// <summary>
    /// Gets the attack vectors associated with this risk.
    /// </summary>
    public IReadOnlyList<string>? AttackVectors { get; init; }
}
