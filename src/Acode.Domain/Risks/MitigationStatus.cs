namespace Acode.Domain.Risks;

/// <summary>
/// Status of a risk mitigation.
/// </summary>
public enum MitigationStatus
{
    /// <summary>
    /// Mitigation is implemented and active.
    /// </summary>
    Implemented,

    /// <summary>
    /// Mitigation implementation in progress.
    /// </summary>
    InProgress,

    /// <summary>
    /// Mitigation planned but not started.
    /// </summary>
    Pending,

    /// <summary>
    /// Mitigation not applicable to this risk.
    /// </summary>
    NotApplicable
}
