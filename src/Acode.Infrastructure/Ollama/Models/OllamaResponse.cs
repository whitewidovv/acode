using System.Text.Json.Serialization;

namespace Acode.Infrastructure.Ollama.Models;

/// <summary>
/// Response from Ollama's /api/chat endpoint (non-streaming).
/// </summary>
public sealed record OllamaResponse
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OllamaResponse"/> class.
    /// </summary>
    /// <param name="model">The model name.</param>
    /// <param name="createdAt">The timestamp when the response was created.</param>
    /// <param name="message">The assistant's message.</param>
    /// <param name="done">Whether generation is complete.</param>
    /// <param name="doneReason">The reason generation stopped (optional).</param>
    /// <param name="totalDuration">Total duration in nanoseconds (optional).</param>
    /// <param name="promptEvalCount">Number of prompt tokens evaluated (optional).</param>
    /// <param name="evalCount">Number of completion tokens generated (optional).</param>
    public OllamaResponse(
        string model,
        string createdAt,
        OllamaMessage message,
        bool done,
        string? doneReason = null,
        long? totalDuration = null,
        int? promptEvalCount = null,
        int? evalCount = null)
    {
        this.Model = model;
        this.CreatedAt = createdAt;
        this.Message = message;
        this.Done = done;
        this.DoneReason = doneReason;
        this.TotalDuration = totalDuration;
        this.PromptEvalCount = promptEvalCount;
        this.EvalCount = evalCount;
    }

    /// <summary>
    /// Gets the model name (e.g., "llama3.2:8b").
    /// </summary>
    [JsonPropertyName("model")]
    public string Model { get; init; }

    /// <summary>
    /// Gets the timestamp when the response was created.
    /// </summary>
    [JsonPropertyName("created_at")]
    public string CreatedAt { get; init; }

    /// <summary>
    /// Gets the assistant's message.
    /// </summary>
    [JsonPropertyName("message")]
    public OllamaMessage Message { get; init; }

    /// <summary>
    /// Gets a value indicating whether generation is complete.
    /// </summary>
    [JsonPropertyName("done")]
    public bool Done { get; init; }

    /// <summary>
    /// Gets the reason generation stopped: "stop", "length", or "tool_calls" (optional).
    /// </summary>
    [JsonPropertyName("done_reason")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? DoneReason { get; init; }

    /// <summary>
    /// Gets the total duration in nanoseconds (optional).
    /// </summary>
    [JsonPropertyName("total_duration")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public long? TotalDuration { get; init; }

    /// <summary>
    /// Gets the number of prompt tokens evaluated (optional).
    /// </summary>
    [JsonPropertyName("prompt_eval_count")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? PromptEvalCount { get; init; }

    /// <summary>
    /// Gets the number of completion tokens generated (optional).
    /// </summary>
    [JsonPropertyName("eval_count")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? EvalCount { get; init; }
}
