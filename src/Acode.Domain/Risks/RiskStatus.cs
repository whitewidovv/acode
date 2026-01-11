namespace Acode.Domain.Risks;

/// <summary>
/// Status of a security risk in the risk register.
/// </summary>
public enum RiskStatus
{
    /// <summary>
    /// Risk is active and requires ongoing attention.
    /// </summary>
    Active,

    /// <summary>
    /// Risk is deprecated and no longer applicable.
    /// </summary>
    Deprecated,

    /// <summary>
    /// Risk is accepted with documented residual risk.
    /// </summary>
    Accepted
}
