using Acode.Cli.Events;

namespace Acode.Cli.JSONL;

/// <summary>
/// Emits events to the output stream.
/// </summary>
public interface IEventEmitter
{
    /// <summary>
    /// Emits an event to the output stream.
    /// </summary>
    /// <param name="baseEvent">The event to emit.</param>
    void Emit(BaseEvent baseEvent);

    /// <summary>
    /// Configures the event emitter.
    /// </summary>
    /// <param name="options">Configuration options.</param>
    void Configure(EventEmitterOptions options);

    /// <summary>
    /// Gets statistics about emitted events.
    /// </summary>
    /// <returns>Statistics about events emitted in this session.</returns>
    EventEmitterStats GetStats();
}
