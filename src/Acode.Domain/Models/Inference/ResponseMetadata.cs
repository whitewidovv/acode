namespace Acode.Domain.Models.Inference;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

/// <summary>
/// Auxiliary information about an inference response.
/// </summary>
/// <remarks>
/// FR-004b-042: ResponseMetadata MUST be defined as an immutable record type.
/// FR-004b-043 to FR-004b-053: Properties, validation, provider extensions.
/// </remarks>
[method: JsonConstructor]
public sealed record ResponseMetadata(
    string ProviderId,
    string ModelId,
    TimeSpan RequestDuration,
    TimeSpan? TimeToFirstToken = null,
    IReadOnlyDictionary<string, JsonElement>? Extensions = null)
{
    /// <summary>
    /// Gets the provider identifier (e.g., "ollama", "vllm").
    /// </summary>
    /// <remarks>
    /// FR-004b-043: ResponseMetadata MUST include ProviderId property.
    /// FR-004b-051: ResponseMetadata MUST validate ProviderId is non-empty.
    /// </remarks>
    [JsonPropertyName("providerId")]
    public string ProviderId { get; init; } = ValidateNonEmpty(ProviderId, nameof(ProviderId));

    /// <summary>
    /// Gets the exact model identifier.
    /// </summary>
    /// <remarks>
    /// FR-004b-044: ResponseMetadata MUST include ModelId property.
    /// FR-004b-052: ResponseMetadata MUST validate ModelId is non-empty.
    /// </remarks>
    [JsonPropertyName("modelId")]
    public string ModelId { get; init; } = ValidateNonEmpty(ModelId, nameof(ModelId));

    /// <summary>
    /// Gets the time elapsed from request submission to completion.
    /// </summary>
    /// <remarks>
    /// FR-004b-045: ResponseMetadata MUST include RequestDuration property.
    /// FR-004b-053: ResponseMetadata MUST validate RequestDuration is non-negative.
    /// </remarks>
    [JsonPropertyName("requestDuration")]
    public TimeSpan RequestDuration { get; init; } = ValidateNonNegative(RequestDuration, nameof(RequestDuration));

    /// <summary>
    /// Gets the time to first token (null for non-streaming).
    /// </summary>
    /// <remarks>
    /// FR-004b-046: ResponseMetadata MUST include TimeToFirstToken property (TimeSpan?, null for non-streaming).
    /// FR-004b-050: ResponseMetadata MUST support null TimeToFirstToken for non-streaming responses.
    /// </remarks>
    [JsonPropertyName("timeToFirstToken")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public TimeSpan? TimeToFirstToken { get; init; } = TimeToFirstToken;

    /// <summary>
    /// Gets the tokens per second rate (placeholder - needs completion tokens).
    /// </summary>
    /// <remarks>
    /// FR-004b-047: ResponseMetadata MUST include TokensPerSecond computed property.
    /// Note: This will be computed from CompletionTokens / Duration in ChatResponse.
    /// </remarks>
    [JsonPropertyName("tokensPerSecond")]
    public double? TokensPerSecond => null; // Will be computed in ChatResponse with Usage data

    /// <summary>
    /// Gets arbitrary provider-specific fields.
    /// </summary>
    /// <remarks>
    /// FR-004b-048: ResponseMetadata MUST include Extensions property.
    /// FR-004b-049: ResponseMetadata MUST preserve arbitrary provider-specific fields in Extensions.
    /// </remarks>
    [JsonPropertyName("extensions")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyDictionary<string, JsonElement> Extensions { get; init; } = Extensions ?? new Dictionary<string, JsonElement>();

    public bool Equals(ResponseMetadata? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        // Compare all properties including Extensions using element-wise comparison
        return this.ProviderId == other.ProviderId
            && this.ModelId == other.ModelId
            && this.RequestDuration == other.RequestDuration
            && this.TimeToFirstToken == other.TimeToFirstToken
            && DictionariesEqual(this.Extensions, other.Extensions);
    }

    public override int GetHashCode()
    {
        var hash = default(HashCode);
        hash.Add(this.ProviderId);
        hash.Add(this.ModelId);
        hash.Add(this.RequestDuration);
        hash.Add(this.TimeToFirstToken);

        // Include extensions in hash
        foreach (var kvp in this.Extensions.OrderBy(x => x.Key))
        {
            hash.Add(kvp.Key);
            hash.Add(kvp.Value.GetRawText());
        }

        return hash.ToHashCode();
    }

    private static bool DictionariesEqual(IReadOnlyDictionary<string, JsonElement> a, IReadOnlyDictionary<string, JsonElement> b)
    {
        if (a.Count != b.Count)
        {
            return false;
        }

        foreach (var kvp in a)
        {
            if (!b.TryGetValue(kvp.Key, out var otherValue))
            {
                return false;
            }

            if (kvp.Value.GetRawText() != otherValue.GetRawText())
            {
                return false;
            }
        }

        return true;
    }

    private static string ValidateNonEmpty(string value, string paramName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException($"{paramName} must be non-empty.", paramName);
        }

        return value;
    }

    private static TimeSpan ValidateNonNegative(TimeSpan value, string paramName)
    {
        if (value < TimeSpan.Zero)
        {
            throw new ArgumentException($"{paramName} must be non-negative.", paramName);
        }

        return value;
    }
}
