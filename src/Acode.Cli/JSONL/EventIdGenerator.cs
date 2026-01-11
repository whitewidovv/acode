namespace Acode.Cli.JSONL;

/// <summary>
/// Generates unique event IDs within a session.
/// </summary>
/// <remarks>
/// Thread-safe counter with configurable prefix.
/// </remarks>
public sealed class EventIdGenerator
{
    private readonly string _prefix;
    private int _counter;

    /// <summary>
    /// Initializes a new instance of the <see cref="EventIdGenerator"/> class.
    /// </summary>
    /// <param name="prefix">Prefix for generated IDs. Defaults to "evt".</param>
    public EventIdGenerator(string? prefix = "evt")
    {
        _prefix = prefix ?? "evt";
    }

    /// <summary>
    /// Gets the current count without incrementing.
    /// </summary>
    public int CurrentCount => _counter;

    /// <summary>
    /// Generates the next unique event ID.
    /// </summary>
    /// <returns>A unique event ID like "evt_001".</returns>
    public string Next()
    {
        var count = Interlocked.Increment(ref _counter);
        return $"{_prefix}_{count:D3}";
    }

    /// <summary>
    /// Resets the counter to zero.
    /// </summary>
    public void Reset()
    {
        Interlocked.Exchange(ref _counter, 0);
    }
}
