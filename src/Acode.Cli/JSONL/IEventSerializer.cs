using Acode.Cli.Events;

namespace Acode.Cli.JSONL;

/// <summary>
/// Serializes events to JSON format.
/// </summary>
public interface IEventSerializer
{
    /// <summary>
    /// Serializes an event to a JSON string.
    /// </summary>
    /// <param name="baseEvent">The event to serialize.</param>
    /// <returns>JSON string representation.</returns>
    string Serialize(BaseEvent baseEvent);
}
