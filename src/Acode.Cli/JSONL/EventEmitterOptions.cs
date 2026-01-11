namespace Acode.Cli.JSONL;

/// <summary>
/// Configuration options for the event emitter.
/// </summary>
/// <param name="IncludeFileContent">Whether to include file content in events.</param>
/// <param name="IncludeStackTraces">Whether to include stack traces in error events.</param>
/// <param name="PrettyPrint">Whether to format JSON with indentation.</param>
public sealed record EventEmitterOptions(
    bool IncludeFileContent = false,
    bool IncludeStackTraces = false,
    bool PrettyPrint = false
);
