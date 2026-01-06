namespace Acode.Cli;

/// <summary>
/// Message types for styled output.
/// </summary>
public enum MessageType
{
    /// <summary>
    /// Informational message (default).
    /// </summary>
    Info,

    /// <summary>
    /// Success message (green).
    /// </summary>
    Success,

    /// <summary>
    /// Warning message (yellow).
    /// </summary>
    Warning,

    /// <summary>
    /// Error message (red).
    /// </summary>
    Error,

    /// <summary>
    /// Debug/verbose message (dim/gray).
    /// </summary>
    Debug,
}
