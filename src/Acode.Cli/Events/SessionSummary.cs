namespace Acode.Cli.Events;

/// <summary>
/// Summary statistics for a completed session.
/// </summary>
/// <param name="EventsEmitted">Total number of events emitted.</param>
/// <param name="ErrorsCount">Number of error events.</param>
/// <param name="WarningsCount">Number of warning events.</param>
public sealed record SessionSummary(int EventsEmitted, int ErrorsCount, int WarningsCount);
