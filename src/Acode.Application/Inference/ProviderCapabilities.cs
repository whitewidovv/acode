namespace Acode.Application.Inference;

using System;
using System.Linq;
using System.Text.Json.Serialization;

/// <summary>
/// Describes capabilities and limitations of a model provider.
/// </summary>
/// <remarks>
/// FR-029 to FR-037 from task-004c spec.
/// </remarks>
public sealed record ProviderCapabilities
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ProviderCapabilities"/> class.
    /// </summary>
    /// <param name="supportsStreaming">Whether provider supports streaming responses.</param>
    /// <param name="supportsTools">Whether provider supports tool/function calling.</param>
    /// <param name="supportsSystemMessages">Whether provider supports system messages.</param>
    /// <param name="supportsVision">Whether provider supports vision (image) inputs.</param>
    /// <param name="supportsJsonMode">Whether provider supports JSON mode output.</param>
    /// <param name="maxContextLength">Maximum context length in tokens (null if unknown/unlimited).</param>
    /// <param name="maxOutputTokens">Maximum output length in tokens (null if unknown/unlimited).</param>
    /// <param name="supportedModels">List of supported model identifiers (null if dynamic).</param>
    /// <param name="defaultModel">Default model to use if not specified (null if required).</param>
    public ProviderCapabilities(
        bool supportsStreaming = false,
        bool supportsTools = false,
        bool supportsSystemMessages = false,
        bool supportsVision = false,
        bool supportsJsonMode = false,
        int? maxContextLength = null,
        int? maxOutputTokens = null,
        string[]? supportedModels = null,
        string? defaultModel = null)
    {
        this.SupportsStreaming = supportsStreaming;
        this.SupportsToolCalls = supportsTools; // Map to spec-compliant name
        this.SupportsSystemMessages = supportsSystemMessages;
        this.SupportsVision = supportsVision;
        this.SupportsJsonMode = supportsJsonMode;
        this.MaxContextTokens = maxContextLength ?? 0; // Spec wants non-nullable
        this.MaxOutputTokens = maxOutputTokens ?? 0;
        this.SupportedModels = supportedModels;
        this.DefaultModel = defaultModel;
    }

    /// <summary>
    /// Gets a value indicating whether the provider supports streaming responses.
    /// </summary>
    /// <remarks>
    /// FR-031: SupportsStreaming property (bool, defaults to false).
    /// </remarks>
    [JsonPropertyName("supportsStreaming")]
    public bool SupportsStreaming { get; init; }

    /// <summary>
    /// Gets a value indicating whether the provider supports tool/function calling.
    /// </summary>
    /// <remarks>
    /// FR-032: SupportsToolCalls property (bool, defaults to false).
    /// </remarks>
    [JsonPropertyName("supportsToolCalls")]
    public bool SupportsToolCalls { get; init; }

    /// <summary>
    /// Gets a value indicating whether the provider supports tool/function calling.
    /// </summary>
    /// <remarks>
    /// Compatibility alias for SupportsToolCalls.
    /// </remarks>
    [JsonIgnore]
    public bool SupportsTools => this.SupportsToolCalls;

    /// <summary>
    /// Gets a value indicating whether the provider supports system messages.
    /// </summary>
    /// <remarks>
    /// Extended capability beyond spec (bonus feature).
    /// </remarks>
    [JsonPropertyName("supportsSystemMessages")]
    public bool SupportsSystemMessages { get; init; }

    /// <summary>
    /// Gets a value indicating whether the provider supports vision (image) inputs.
    /// </summary>
    /// <remarks>
    /// Extended capability beyond spec (bonus feature).
    /// </remarks>
    [JsonPropertyName("supportsVision")]
    public bool SupportsVision { get; init; }

    /// <summary>
    /// Gets a value indicating whether the provider supports JSON mode output.
    /// </summary>
    /// <remarks>
    /// FR-035: SupportsJsonMode property (bool, defaults to false).
    /// </remarks>
    [JsonPropertyName("supportsJsonMode")]
    public bool SupportsJsonMode { get; init; }

    /// <summary>
    /// Gets the maximum context length in tokens.
    /// </summary>
    /// <remarks>
    /// FR-033: MaxContextTokens property (int).
    /// </remarks>
    [JsonPropertyName("maxContextTokens")]
    public int MaxContextTokens { get; init; }

    /// <summary>
    /// Gets the maximum context length in tokens.
    /// </summary>
    /// <remarks>
    /// Compatibility alias for MaxContextTokens.
    /// </remarks>
    [JsonIgnore]
    public int? MaxContextLength => this.MaxContextTokens > 0 ? this.MaxContextTokens : null;

    /// <summary>
    /// Gets the maximum output length in tokens.
    /// </summary>
    /// <remarks>
    /// FR-034: MaxOutputTokens property (int).
    /// </remarks>
    [JsonPropertyName("maxOutputTokens")]
    public int MaxOutputTokens { get; init; }

    /// <summary>
    /// Gets the list of supported model identifiers (null if dynamic/unknown).
    /// </summary>
    /// <remarks>
    /// FR-030: SupportedModels property (IReadOnlyList&lt;string&gt;).
    /// </remarks>
    [JsonPropertyName("supportedModels")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string[]? SupportedModels { get; init; }

    /// <summary>
    /// Gets the default model identifier to use if not specified (null if required).
    /// </summary>
    /// <remarks>
    /// Extended capability beyond core spec.
    /// </remarks>
    [JsonPropertyName("defaultModel")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? DefaultModel { get; init; }

    /// <summary>
    /// Checks if this provider supports the given capability requirements.
    /// </summary>
    /// <param name="requirement">Capability requirements to check.</param>
    /// <returns>True if all requirements are met, false otherwise.</returns>
    /// <remarks>
    /// FR-036: Supports method for capability matching.
    /// </remarks>
    public bool Supports(CapabilityRequirement requirement)
    {
        ArgumentNullException.ThrowIfNull(requirement);

        if (requirement.RequiresStreaming && !this.SupportsStreaming)
        {
            return false;
        }

        if (requirement.RequiresToolCalls && !this.SupportsToolCalls)
        {
            return false;
        }

        if (requirement.RequiresJsonMode && !this.SupportsJsonMode)
        {
            return false;
        }

        if (requirement.MinContextTokens.HasValue &&
            (this.MaxContextTokens == 0 || this.MaxContextTokens < requirement.MinContextTokens.Value))
        {
            return false;
        }

        if (requirement.MinOutputTokens.HasValue &&
            (this.MaxOutputTokens == 0 || this.MaxOutputTokens < requirement.MinOutputTokens.Value))
        {
            return false;
        }

        if (requirement.RequiredModel != null &&
            this.SupportedModels != null &&
            !this.SupportedModels.Contains(requirement.RequiredModel, StringComparer.OrdinalIgnoreCase))
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// Merges this capabilities with another, taking the most capable values.
    /// </summary>
    /// <param name="other">Other capabilities to merge with.</param>
    /// <returns>New capabilities with merged values.</returns>
    /// <remarks>
    /// FR-037: Merge method for capability combination.
    /// Uses OR for boolean capabilities and MAX for numeric limits.
    /// </remarks>
    public ProviderCapabilities Merge(ProviderCapabilities other)
    {
        ArgumentNullException.ThrowIfNull(other);

        return new ProviderCapabilities(
            supportsStreaming: this.SupportsStreaming || other.SupportsStreaming,
            supportsTools: this.SupportsToolCalls || other.SupportsToolCalls,
            supportsSystemMessages: this.SupportsSystemMessages || other.SupportsSystemMessages,
            supportsVision: this.SupportsVision || other.SupportsVision,
            supportsJsonMode: this.SupportsJsonMode || other.SupportsJsonMode,
            maxContextLength: Math.Max(this.MaxContextTokens, other.MaxContextTokens),
            maxOutputTokens: Math.Max(this.MaxOutputTokens, other.MaxOutputTokens),
            supportedModels: MergeSupportedModels(this.SupportedModels, other.SupportedModels),
            defaultModel: this.DefaultModel ?? other.DefaultModel);
    }

    private static string[]? MergeSupportedModels(string[]? a, string[]? b)
    {
        if (a == null)
        {
            return b;
        }

        if (b == null)
        {
            return a;
        }

        return a.Union(b, StringComparer.OrdinalIgnoreCase).ToArray();
    }
}
