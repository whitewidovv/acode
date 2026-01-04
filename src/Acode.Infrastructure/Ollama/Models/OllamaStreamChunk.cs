using System.Text.Json.Serialization;

namespace Acode.Infrastructure.Ollama.Models;

/// <summary>
/// A single chunk from Ollama's streaming /api/chat endpoint (NDJSON format).
/// </summary>
public sealed record OllamaStreamChunk
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OllamaStreamChunk"/> class.
    /// </summary>
    /// <param name="model">The model name.</param>
    /// <param name="message">The message delta.</param>
    /// <param name="done">Whether this is the final chunk.</param>
    /// <param name="doneReason">The reason generation stopped (final chunk only).</param>
    /// <param name="totalDuration">Total duration in nanoseconds (final chunk only).</param>
    /// <param name="evalCount">Number of completion tokens (final chunk only).</param>
    /// <param name="promptEvalCount">Number of prompt tokens (final chunk only).</param>
    public OllamaStreamChunk(
        string model,
        OllamaMessage message,
        bool done,
        string? doneReason = null,
        long? totalDuration = null,
        int? evalCount = null,
        int? promptEvalCount = null)
    {
        this.Model = model;
        this.Message = message;
        this.Done = done;
        this.DoneReason = doneReason;
        this.TotalDuration = totalDuration;
        this.EvalCount = evalCount;
        this.PromptEvalCount = promptEvalCount;
    }

    /// <summary>
    /// Gets the model name (e.g., "llama3.2:8b").
    /// </summary>
    [JsonPropertyName("model")]
    public string Model { get; init; }

    /// <summary>
    /// Gets the message delta (incremental content or tool call).
    /// </summary>
    [JsonPropertyName("message")]
    public OllamaMessage Message { get; init; }

    /// <summary>
    /// Gets a value indicating whether this is the final chunk.
    /// </summary>
    [JsonPropertyName("done")]
    public bool Done { get; init; }

    /// <summary>
    /// Gets the reason generation stopped (only present in final chunk).
    /// </summary>
    [JsonPropertyName("done_reason")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? DoneReason { get; init; }

    /// <summary>
    /// Gets the total duration in nanoseconds (only present in final chunk).
    /// </summary>
    [JsonPropertyName("total_duration")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public long? TotalDuration { get; init; }

    /// <summary>
    /// Gets the number of completion tokens (only present in final chunk).
    /// </summary>
    [JsonPropertyName("eval_count")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? EvalCount { get; init; }

    /// <summary>
    /// Gets the number of prompt tokens (only present in final chunk).
    /// </summary>
    [JsonPropertyName("prompt_eval_count")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? PromptEvalCount { get; init; }
}
