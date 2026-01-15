namespace Acode.Application.Inference;

using System;
using System.Text.Json.Serialization;
using Acode.Domain.Models.Inference;

/// <summary>
/// Chat completion request with messages, model parameters, and optional tool use.
/// </summary>
/// <remarks>
/// FR-004-66: ChatRequest record defined.
/// FR-004-67 to FR-004-72: Properties and validation.
/// </remarks>
public sealed record ChatRequest
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ChatRequest"/> class.
    /// </summary>
    /// <param name="messages">Conversation messages.</param>
    /// <param name="modelParameters">Model inference parameters.</param>
    /// <param name="tools">Available tools for the model to call.</param>
    /// <param name="stream">Whether to stream the response.</param>
    /// <param name="responseFormat">Response format constraints for structured output.</param>
    public ChatRequest(
        ChatMessage[] messages,
        ModelParameters? modelParameters = null,
        ToolDefinition[]? tools = null,
        bool stream = false,
        ResponseFormat? responseFormat = null)
    {
        ArgumentNullException.ThrowIfNull(messages, "Messages");

        if (messages.Length == 0)
        {
            throw new ArgumentException("Messages array must be non-empty", "Messages");
        }

        this.Messages = messages;
        this.ModelParameters = modelParameters;
        this.Tools = tools;
        this.Stream = stream;
        this.ResponseFormat = responseFormat;
    }

    /// <summary>
    /// Gets the conversation messages.
    /// </summary>
    /// <remarks>
    /// FR-004-66, FR-004-67: Messages is required, non-empty array of ChatMessage.
    /// </remarks>
    [JsonPropertyName("messages")]
    public ChatMessage[] Messages { get; init; }

    /// <summary>
    /// Gets the model inference parameters.
    /// </summary>
    /// <remarks>
    /// FR-004-68, FR-004-69: ModelParameters is nullable (use provider defaults if null).
    /// </remarks>
    [JsonPropertyName("modelParameters")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ModelParameters? ModelParameters { get; init; }

    /// <summary>
    /// Gets the available tools.
    /// </summary>
    /// <remarks>
    /// FR-004-70: Tools is nullable array (no tool use if null).
    /// </remarks>
    [JsonPropertyName("tools")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ToolDefinition[]? Tools { get; init; }

    /// <summary>
    /// Gets a value indicating whether to stream the response.
    /// </summary>
    /// <remarks>
    /// FR-004-71, FR-004-72: Stream defaults to false (non-streaming by default).
    /// </remarks>
    [JsonPropertyName("stream")]
    public bool Stream { get; init; }

    /// <summary>
    /// Gets the response format constraints for structured output.
    /// </summary>
    /// <remarks>
    /// FR-008 to FR-014: Response format support for json_object and json_schema types.
    /// </remarks>
    [JsonPropertyName("responseFormat")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ResponseFormat? ResponseFormat { get; init; }
}
