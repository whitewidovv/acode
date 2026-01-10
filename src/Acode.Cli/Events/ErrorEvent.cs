namespace Acode.Cli.Events;

/// <summary>
/// Event emitted when an error occurs.
/// </summary>
/// <remarks>
/// Contains structured error information for programmatic handling.
/// </remarks>
public sealed record ErrorEvent : BaseEvent
{
    /// <summary>
    /// Gets the error code (e.g., ACODE-CLI-001).
    /// </summary>
    public required string Code { get; init; }

    /// <summary>
    /// Gets the error message.
    /// </summary>
    public required string Message { get; init; }

    /// <summary>
    /// Gets the component where the error originated.
    /// </summary>
    public required string Component { get; init; }

    /// <summary>
    /// Gets the stack trace, if available and verbose mode is enabled.
    /// </summary>
    public string? StackTrace { get; init; }

    /// <summary>
    /// Gets suggested remediation steps.
    /// </summary>
    public string? Remediation { get; init; }
}
