namespace Acode.Domain.Models.Inference;

using System.Text.Json.Serialization;

/// <summary>
/// Content moderation categories.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<FilterCategory>))]
public enum FilterCategory
{
    /// <summary>
    /// Sexual content.
    /// </summary>
    Sexual = 0,

    /// <summary>
    /// Violent content.
    /// </summary>
    Violence = 1,

    /// <summary>
    /// Hateful content.
    /// </summary>
    Hate = 2,

    /// <summary>
    /// Self-harm content.
    /// </summary>
    SelfHarm = 3,
}

/// <summary>
/// Content filter severity levels.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<FilterSeverity>))]
public enum FilterSeverity
{
    /// <summary>
    /// Content is safe.
    /// </summary>
    Safe = 0,

    /// <summary>
    /// Low severity.
    /// </summary>
    Low = 1,

    /// <summary>
    /// Medium severity.
    /// </summary>
    Medium = 2,

    /// <summary>
    /// High severity - likely to be filtered.
    /// </summary>
    High = 3,
}

/// <summary>
/// Represents a content moderation result.
/// </summary>
/// <remarks>
/// FR-004b-089: ContentFilterResult MUST be defined as a record type.
/// FR-004b-090 to FR-004b-095: Properties for moderation results.
/// </remarks>
public sealed record ContentFilterResult
{
    /// <summary>
    /// Gets the content category that was evaluated.
    /// </summary>
    /// <remarks>
    /// FR-004b-090: ContentFilterResult MUST include Category property (enum: Sexual, Violence, Hate, SelfHarm).
    /// </remarks>
    [JsonPropertyName("category")]
    public required FilterCategory Category { get; init; }

    /// <summary>
    /// Gets the severity level of the content.
    /// </summary>
    /// <remarks>
    /// FR-004b-091: ContentFilterResult MUST include Severity property (enum: Safe, Low, Medium, High).
    /// </remarks>
    [JsonPropertyName("severity")]
    public required FilterSeverity Severity { get; init; }

    /// <summary>
    /// Gets a value indicating whether the content was blocked/filtered.
    /// </summary>
    /// <remarks>
    /// FR-004b-092: ContentFilterResult MUST include Filtered property (bool, true if content was blocked).
    /// </remarks>
    [JsonPropertyName("filtered")]
    public required bool Filtered { get; init; }

    /// <summary>
    /// Gets the optional explanation of why content was filtered.
    /// </summary>
    /// <remarks>
    /// FR-004b-093: ContentFilterResult MUST include optional Reason property (string description).
    /// </remarks>
    [JsonPropertyName("reason")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Reason { get; init; }
}
