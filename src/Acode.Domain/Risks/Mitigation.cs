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
    public required MitigationId Id { get; init; }

    /// <summary>
    /// Gets the short title of this mitigation.
    /// </summary>
    public required string Title { get; init; }

    /// <summary>
    /// Gets the detailed description of the mitigation strategy.
    /// </summary>
    public required string Description { get; init; }

    /// <summary>
    /// Gets the implementation details or location.
    /// References code files, configuration, or procedures.
    /// </summary>
    public required string Implementation { get; init; }

    /// <summary>
    /// Gets the test name that verifies this mitigation is working correctly.
    /// </summary>
    public string? VerificationTest { get; init; }

    /// <summary>
    /// Gets the implementation status of this mitigation.
    /// </summary>
    public required MitigationStatus Status { get; init; }

    /// <summary>
    /// Gets the date and time when this mitigation was last verified.
    /// </summary>
    public required DateTimeOffset LastVerified { get; init; }
}
