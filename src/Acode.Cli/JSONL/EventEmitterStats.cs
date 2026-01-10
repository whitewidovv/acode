namespace Acode.Cli.JSONL;

/// <summary>
/// Statistics about emitted events.
/// </summary>
/// <param name="TotalEvents">Total events emitted.</param>
/// <param name="ErrorCount">Number of error events.</param>
/// <param name="WarningCount">Number of warning events.</param>
public sealed record EventEmitterStats(int TotalEvents, int ErrorCount, int WarningCount);
