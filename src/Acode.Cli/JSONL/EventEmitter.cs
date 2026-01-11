using Acode.Cli.Events;

namespace Acode.Cli.JSONL;

/// <summary>
/// Emits events to stdout as JSONL.
/// </summary>
/// <remarks>
/// Writes one JSON object per line with immediate flushing for real-time streaming.
/// Thread-safe for concurrent event emission.
/// </remarks>
public sealed class EventEmitter : IEventEmitter
{
    private readonly TextWriter _output;
    private readonly IEventSerializer _serializer;
    private readonly object _lock = new();

    private int _totalEvents;
    private int _errorCount;
    private int _warningCount;
    private EventEmitterOptions _options = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="EventEmitter"/> class.
    /// </summary>
    /// <param name="output">Output writer (typically Console.Out).</param>
    /// <param name="serializer">Event serializer.</param>
    public EventEmitter(TextWriter output, IEventSerializer serializer)
    {
        _output = output ?? throw new ArgumentNullException(nameof(output));
        _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
    }

    /// <inheritdoc/>
    public void Configure(EventEmitterOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    /// <inheritdoc/>
    public void Emit(BaseEvent baseEvent)
    {
        ArgumentNullException.ThrowIfNull(baseEvent);

        var json = _serializer.Serialize(baseEvent);

        lock (_lock)
        {
            _output.WriteLine(json);
            _output.Flush(); // Immediate flush for real-time streaming

            _totalEvents++;

            if (baseEvent is ErrorEvent)
            {
                _errorCount++;
            }
            else if (baseEvent is WarningEvent)
            {
                _warningCount++;
            }
        }
    }

    /// <inheritdoc/>
    public EventEmitterStats GetStats() =>
        new(TotalEvents: _totalEvents, ErrorCount: _errorCount, WarningCount: _warningCount);
}
