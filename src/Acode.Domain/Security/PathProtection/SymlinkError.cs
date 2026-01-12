namespace Acode.Domain.Security.PathProtection;

/// <summary>
/// Errors that can occur during symlink resolution.
/// </summary>
public enum SymlinkError
{
    /// <summary>
    /// No error occurred.
    /// </summary>
    None,

    /// <summary>
    /// Circular reference detected (symlink points to itself through a chain).
    /// </summary>
    CircularReference,

    /// <summary>
    /// Maximum symlink depth exceeded (default: 40 levels).
    /// </summary>
    MaxDepthExceeded,

    /// <summary>
    /// Symlink target does not exist.
    /// </summary>
    TargetNotFound,

    /// <summary>
    /// Access denied when reading symlink or target.
    /// </summary>
    AccessDenied,

    /// <summary>
    /// Unknown error occurred during resolution.
    /// </summary>
    Unknown,
}
