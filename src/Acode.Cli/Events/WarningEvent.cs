namespace Acode.Cli.Events;

/// <summary>
/// Event emitted for warnings.
/// </summary>
public sealed record WarningEvent : BaseEvent
{
    /// <summary>
    /// Gets the warning code.
    /// </summary>
    public required string Code { get; init; }

    /// <summary>
    /// Gets the warning message.
    /// </summary>
    public required string Message { get; init; }

    /// <summary>
    /// Gets the component where the warning originated.
    /// </summary>
    public string? Component { get; init; }
}
