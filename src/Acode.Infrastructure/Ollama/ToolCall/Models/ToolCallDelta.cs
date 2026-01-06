namespace Acode.Infrastructure.Ollama.ToolCall.Models;

/// <summary>
/// Represents a streaming delta (fragment) of a tool call.
/// Used to accumulate tool calls from streaming responses.
/// </summary>
public sealed class ToolCallDelta
{
    /// <summary>
    /// Gets or sets the index of the tool call in the array.
    /// Used to match deltas to the correct accumulator.
    /// </summary>
    public int Index { get; set; }

    /// <summary>
    /// Gets or sets the tool call ID (if present in this delta).
    /// </summary>
    public string? Id { get; set; }

    /// <summary>
    /// Gets or sets the complete function name (if present in this delta).
    /// </summary>
    public string? FunctionName { get; set; }

    /// <summary>
    /// Gets or sets a partial function name fragment to append.
    /// </summary>
    public string? FunctionNameFragment { get; set; }

    /// <summary>
    /// Gets or sets the arguments fragment to append.
    /// </summary>
    public string? ArgumentsFragment { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this delta marks the tool call as complete.
    /// </summary>
    public bool IsComplete { get; set; }
}
