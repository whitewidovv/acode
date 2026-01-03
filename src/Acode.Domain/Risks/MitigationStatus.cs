namespace Acode.Domain.Risks;

/// <summary>
/// Status of a risk mitigation.
/// </summary>
public enum MitigationStatus
{
    /// <summary>
    /// Mitigation planned but not started.
    /// </summary>
    Planned,

    /// <summary>
    /// Mitigation implementation in progress.
    /// </summary>
    InProgress,

    /// <summary>
    /// Mitigation implemented and active.
    /// </summary>
    Implemented,

    /// <summary>
    /// Mitigation verified through testing.
    /// </summary>
    Verified
}
