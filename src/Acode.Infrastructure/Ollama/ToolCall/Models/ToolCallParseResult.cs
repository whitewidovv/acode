namespace Acode.Infrastructure.Ollama.ToolCall.Models;

using Acode.Domain.Models.Inference;

/// <summary>
/// Result of parsing tool calls from an Ollama response.
/// May contain a mix of successful parses and errors.
/// </summary>
public sealed class ToolCallParseResult
{
    /// <summary>
    /// Gets the successfully parsed tool calls.
    /// </summary>
    public IReadOnlyList<ToolCall> ToolCalls { get; init; } = Array.Empty<ToolCall>();

    /// <summary>
    /// Gets the errors encountered during parsing.
    /// </summary>
    public IReadOnlyList<ToolCallError> Errors { get; init; } = Array.Empty<ToolCallError>();

    /// <summary>
    /// Gets a value indicating whether all tool calls were parsed successfully.
    /// </summary>
    public bool AllSucceeded => Errors.Count == 0;

    /// <summary>
    /// Gets a value indicating whether any errors occurred during parsing.
    /// </summary>
    public bool HasErrors => Errors.Count > 0;

    /// <summary>
    /// Gets the total number of tool calls (successful + failed).
    /// </summary>
    public int TotalCount => ToolCalls.Count + Errors.Count;

    /// <summary>
    /// Gets the repair details if any JSON was repaired.
    /// </summary>
    public IReadOnlyList<RepairResult> Repairs { get; init; } = Array.Empty<RepairResult>();

    /// <summary>
    /// Create an empty result (no tool calls in response).
    /// </summary>
    /// <returns>An empty parse result.</returns>
    public static ToolCallParseResult Empty() => new()
    {
        ToolCalls = Array.Empty<ToolCall>(),
        Errors = Array.Empty<ToolCallError>()
    };

    /// <summary>
    /// Create a result with parsed tool calls.
    /// </summary>
    /// <param name="toolCalls">The parsed tool calls.</param>
    /// <returns>A successful parse result.</returns>
    public static ToolCallParseResult Success(IReadOnlyList<ToolCall> toolCalls)
    {
        return new ToolCallParseResult
        {
            ToolCalls = toolCalls,
            Errors = Array.Empty<ToolCallError>()
        };
    }

    /// <summary>
    /// Create a result with an error.
    /// </summary>
    /// <param name="error">The error.</param>
    /// <returns>A failed parse result.</returns>
    public static ToolCallParseResult Failure(ToolCallError error)
    {
        return new ToolCallParseResult
        {
            ToolCalls = Array.Empty<ToolCall>(),
            Errors = new[] { error }
        };
    }
}
