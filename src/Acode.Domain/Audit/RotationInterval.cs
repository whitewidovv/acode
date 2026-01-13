namespace Acode.Domain.Audit;

/// <summary>
/// Rotation interval for audit logs.
/// </summary>
public enum RotationInterval
{
    /// <summary>
    /// Rotate logs hourly.
    /// </summary>
    Hourly,

    /// <summary>
    /// Rotate logs daily.
    /// </summary>
    Daily,

    /// <summary>
    /// Rotate logs weekly.
    /// </summary>
    Weekly,
}
