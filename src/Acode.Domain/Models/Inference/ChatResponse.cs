namespace Acode.Domain.Models.Inference;

using System;
using System.Text.Json.Serialization;

/// <summary>
/// Primary record encapsulating a complete model completion result.
/// </summary>
/// <remarks>
/// FR-004b-001: ChatResponse MUST be defined as an immutable record type in the Domain layer.
/// FR-004b-002 to FR-004b-018: Properties, validation, computed properties.
/// </remarks>
[method: JsonConstructor]
public sealed record ChatResponse(
    string Id,
    ChatMessage Message,
    FinishReason FinishReason,
    UsageInfo Usage,
    ResponseMetadata Metadata,
    DateTimeOffset Created,
    string Model,
    string? Refusal = null)
{
    /// <summary>
    /// Gets the unique response identifier.
    /// </summary>
    /// <remarks>
    /// FR-004b-002: ChatResponse MUST include an Id property as a unique response identifier (string, GUID format).
    /// FR-004b-016: ChatResponse MUST validate that Id is non-empty on construction.
    /// </remarks>
    [JsonPropertyName("id")]
    public string Id { get; init; } = ValidateNonEmpty(Id, nameof(Id));

    /// <summary>
    /// Gets the response message.
    /// </summary>
    /// <remarks>
    /// FR-004b-003: ChatResponse MUST include a Message property of type ChatMessage (from Task 004.a).
    /// FR-004b-017: ChatResponse MUST validate that Message is not null on construction.
    /// </remarks>
    [JsonPropertyName("message")]
    public ChatMessage Message { get; init; } = ValidateNotNull(Message, nameof(Message));

    /// <summary>
    /// Gets the reason generation stopped.
    /// </summary>
    /// <remarks>
    /// FR-004b-004: ChatResponse MUST include a FinishReason property indicating generation termination cause.
    /// FR-004b-018: ChatResponse MUST validate that FinishReason is a valid enum value.
    /// </remarks>
    [JsonPropertyName("finishReason")]
    public FinishReason FinishReason { get; init; } = ValidateFinishReason(FinishReason);

    /// <summary>
    /// Gets the token usage information.
    /// </summary>
    /// <remarks>
    /// FR-004b-005: ChatResponse MUST include a Usage property of type UsageInfo.
    /// </remarks>
    [JsonPropertyName("usage")]
    public UsageInfo Usage { get; init; } = Usage;

    /// <summary>
    /// Gets the response metadata.
    /// </summary>
    /// <remarks>
    /// FR-004b-006: ChatResponse MUST include a Metadata property of type ResponseMetadata.
    /// </remarks>
    [JsonPropertyName("metadata")]
    public ResponseMetadata Metadata { get; init; } = Metadata;

    /// <summary>
    /// Gets the creation timestamp (UTC).
    /// </summary>
    /// <remarks>
    /// FR-004b-007: ChatResponse MUST include a Created timestamp (DateTimeOffset, UTC).
    /// </remarks>
    [JsonPropertyName("created")]
    public DateTimeOffset Created { get; init; } = Created;

    /// <summary>
    /// Gets the model identifier that generated the response.
    /// </summary>
    /// <remarks>
    /// FR-004b-008: ChatResponse MUST include a Model property identifying the model that generated the response.
    /// </remarks>
    [JsonPropertyName("model")]
    public string Model { get; init; } = Model;

    /// <summary>
    /// Gets the refusal message if request was declined.
    /// </summary>
    /// <remarks>
    /// FR-004b-009: ChatResponse MUST include an optional Refusal property for declined requests.
    /// FR-004b-015: ChatResponse MUST support null Refusal when request was not declined.
    /// </remarks>
    [JsonPropertyName("refusal")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Refusal { get; init; } = Refusal;

    /// <summary>
    /// Gets a value indicating whether the response completed normally.
    /// </summary>
    /// <remarks>
    /// FR-004b-010: ChatResponse MUST provide a bool IsComplete property (FinishReason == Stop).
    /// </remarks>
    [JsonIgnore]
    public bool IsComplete => this.FinishReason == FinishReason.Stop;

    /// <summary>
    /// Gets a value indicating whether the response was truncated due to length.
    /// </summary>
    /// <remarks>
    /// FR-004b-011: ChatResponse MUST provide a bool IsTruncated property (FinishReason == Length).
    /// </remarks>
    [JsonIgnore]
    public bool IsTruncated => this.FinishReason == FinishReason.Length;

    /// <summary>
    /// Gets a value indicating whether the response includes tool calls.
    /// </summary>
    /// <remarks>
    /// FR-004b-012: ChatResponse MUST provide a bool HasToolCalls property (Message.ToolCalls != null &amp;&amp; Count &gt; 0).
    /// </remarks>
    [JsonIgnore]
    public bool HasToolCalls => this.Message.ToolCalls is not null && this.Message.ToolCalls.Count > 0;

    private static string ValidateNonEmpty(string value, string paramName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException($"{paramName} must be non-empty.", paramName);
        }

        return value;
    }

    private static ChatMessage ValidateNotNull(ChatMessage? value, string paramName)
    {
        if (value is null)
        {
            throw new ArgumentException($"{paramName} must not be null.", paramName);
        }

        return value;
    }

    private static FinishReason ValidateFinishReason(FinishReason value)
    {
        if (!Enum.IsDefined(typeof(FinishReason), value))
        {
            throw new ArgumentException($"FinishReason must be a valid enum value.", nameof(FinishReason));
        }

        return value;
    }
}
