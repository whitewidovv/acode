namespace Acode.Domain.Models.Inference;

using System;
using System.Text.Json.Serialization;

/// <summary>
/// Token consumption metrics for an inference request.
/// </summary>
/// <remarks>
/// FR-004b-030: UsageInfo MUST be defined as an immutable record type.
/// FR-004b-031 to FR-004b-041: Properties, validation, operators, methods.
/// </remarks>
[method: JsonConstructor]
public sealed record UsageInfo(
    int PromptTokens,
    int CompletionTokens,
    int? CachedTokens = null,
    int? ReasoningTokens = null)
{
    /// <summary>
    /// Gets the count of tokens in the input messages.
    /// </summary>
    /// <remarks>
    /// FR-004b-031: UsageInfo MUST include PromptTokens property (int, non-negative).
    /// </remarks>
    [JsonPropertyName("promptTokens")]
    public int PromptTokens { get; init; } = ValidateNonNegative(PromptTokens, nameof(PromptTokens));

    /// <summary>
    /// Gets the count of tokens in the generated response.
    /// </summary>
    /// <remarks>
    /// FR-004b-032: UsageInfo MUST include CompletionTokens property (int, non-negative).
    /// </remarks>
    [JsonPropertyName("completionTokens")]
    public int CompletionTokens { get; init; } = ValidateNonNegative(CompletionTokens, nameof(CompletionTokens));

    /// <summary>
    /// Gets the total token count (Prompt + Completion).
    /// </summary>
    /// <remarks>
    /// FR-004b-033: UsageInfo MUST include TotalTokens computed property (Prompt + Completion).
    /// </remarks>
    [JsonPropertyName("totalTokens")]
    public int TotalTokens => this.PromptTokens + this.CompletionTokens;

    /// <summary>
    /// Gets the count of tokens served from KV cache (provider-specific).
    /// </summary>
    /// <remarks>
    /// FR-004b-034: UsageInfo MUST include optional CachedTokens property (int?, non-negative when present).
    /// </remarks>
    [JsonPropertyName("cachedTokens")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? CachedTokens { get; init; } = ValidateNonNegativeNullable(CachedTokens, nameof(CachedTokens));

    /// <summary>
    /// Gets the count of reasoning tokens for models with Chain-of-Thought.
    /// </summary>
    /// <remarks>
    /// FR-004b-035: UsageInfo MUST include optional ReasoningTokens property for models with CoT.
    /// </remarks>
    [JsonPropertyName("reasoningTokens")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? ReasoningTokens { get; init; } = ValidateNonNegativeNullable(ReasoningTokens, nameof(ReasoningTokens));

    /// <summary>
    /// Gets an empty UsageInfo with all zeros.
    /// </summary>
    /// <remarks>
    /// FR-004b-037: UsageInfo MUST provide static Empty property returning zeros.
    /// </remarks>
    public static UsageInfo Empty { get; } = new UsageInfo(0, 0);

    /// <summary>
    /// Combines usage from multiple requests.
    /// </summary>
    /// <param name="other">The other usage to add.</param>
    /// <returns>A new UsageInfo with combined token counts.</returns>
    /// <remarks>
    /// FR-004b-040: UsageInfo MUST provide Add method for combining usage across requests.
    /// </remarks>
    public UsageInfo Add(UsageInfo other)
    {
        ArgumentNullException.ThrowIfNull(other);

        int? cachedSum = (this.CachedTokens, other.CachedTokens) switch
        {
            (null, null) => (int?)null,
            (int a, null) => (int?)a,
            (null, int b) => (int?)b,
            (int a, int b) => (int?)(a + b),
        };

        int? reasoningSum = (this.ReasoningTokens, other.ReasoningTokens) switch
        {
            (null, null) => (int?)null,
            (int a, null) => (int?)a,
            (null, int b) => (int?)b,
            (int a, int b) => (int?)(a + b),
        };

        return new UsageInfo(
            this.PromptTokens + other.PromptTokens,
            this.CompletionTokens + other.CompletionTokens,
            cachedSum,
            reasoningSum);
    }

    /// <summary>
    /// Returns a string representation of the usage.
    /// </summary>
    /// <returns>A string showing token counts.</returns>
    /// <remarks>
    /// FR-004b-041: UsageInfo MUST provide ToString showing "Prompt: X, Completion: Y, Total: Z".
    /// </remarks>
    public override string ToString()
    {
        return $"Prompt: {this.PromptTokens}, Completion: {this.CompletionTokens}, Total: {this.TotalTokens}";
    }

    private static int ValidateNonNegative(int value, string paramName)
    {
        // FR-004b-036: UsageInfo MUST validate non-negative values on construction
        if (value < 0)
        {
            throw new ArgumentException($"{paramName} must be non-negative.", paramName);
        }

        return value;
    }

    private static int? ValidateNonNegativeNullable(int? value, string paramName)
    {
        // FR-004b-036: UsageInfo MUST validate non-negative values on construction
        if (value.HasValue && value.Value < 0)
        {
            throw new ArgumentException($"{paramName} must be non-negative when present.", paramName);
        }

        return value;
    }
}
