using System.Text.Json.Serialization;

namespace Acode.Infrastructure.Ollama.Models;

/// <summary>
/// Request to Ollama's /api/chat endpoint.
/// Maps Acode's ChatRequest to Ollama's expected format.
/// </summary>
public sealed record OllamaRequest
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OllamaRequest"/> class.
    /// </summary>
    /// <param name="model">The model name.</param>
    /// <param name="messages">The messages array.</param>
    /// <param name="stream">Whether to stream the response.</param>
    /// <param name="tools">Tool definitions (optional).</param>
    /// <param name="format">Response format (optional).</param>
    /// <param name="options">Generation options (optional).</param>
    /// <param name="keepAlive">Keep alive duration (optional).</param>
    public OllamaRequest(
        string model,
        OllamaMessage[] messages,
        bool stream,
        OllamaTool[]? tools = null,
        string? format = null,
        OllamaOptions? options = null,
        string? keepAlive = null)
    {
        this.Model = model;
        this.Messages = messages;
        this.Stream = stream;
        this.Tools = tools;
        this.Format = format;
        this.Options = options;
        this.KeepAlive = keepAlive;
    }

    /// <summary>
    /// Gets the model name (e.g., "llama3.2:8b").
    /// </summary>
    [JsonPropertyName("model")]
    public string Model { get; init; }

    /// <summary>
    /// Gets the array of messages in the conversation.
    /// </summary>
    [JsonPropertyName("messages")]
    public OllamaMessage[] Messages { get; init; }

    /// <summary>
    /// Gets a value indicating whether to stream the response.
    /// </summary>
    [JsonPropertyName("stream")]
    public bool Stream { get; init; }

    /// <summary>
    /// Gets the tool definitions for function calling (optional).
    /// </summary>
    [JsonPropertyName("tools")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public OllamaTool[]? Tools { get; init; }

    /// <summary>
    /// Gets the response format (e.g., "json" for JSON mode).
    /// </summary>
    [JsonPropertyName("format")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Format { get; init; }

    /// <summary>
    /// Gets the generation options (temperature, top_p, etc.).
    /// </summary>
    [JsonPropertyName("options")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public OllamaOptions? Options { get; init; }

    /// <summary>
    /// Gets the duration to keep the model loaded in memory (e.g., "5m").
    /// </summary>
    [JsonPropertyName("keep_alive")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? KeepAlive { get; init; }
}
