namespace Acode.Cli.Events;

/// <summary>
/// Event emitted at the start of a CLI session.
/// </summary>
/// <remarks>
/// Contains run identification and command information for session correlation.
/// </remarks>
public sealed record SessionStartEvent : BaseEvent
{
    /// <summary>
    /// Gets the unique run identifier.
    /// </summary>
    public string RunId { get; init; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Gets the command being executed.
    /// </summary>
    public string Command { get; init; } = string.Empty;

    /// <summary>
    /// Gets the command-line arguments.
    /// </summary>
    public IReadOnlyList<string>? Args { get; init; }

    /// <summary>
    /// Gets the list of supported schema versions.
    /// </summary>
    public IReadOnlyList<string> SchemaVersionsSupported { get; } = new[] { "1.0.0" };
}
