namespace Acode.Application.Inference;

using System.Text.Json.Serialization;

/// <summary>
/// Describes capabilities and limitations of a model provider.
/// </summary>
/// <remarks>
/// FR-004-73: ProviderCapabilities record defined.
/// FR-004-74 to FR-004-80: Properties for capabilities and defaults.
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
    /// <param name="maxContextLength">Maximum context length in tokens (null if unknown/unlimited).</param>
    /// <param name="supportedModels">List of supported model identifiers (null if dynamic).</param>
    /// <param name="defaultModel">Default model to use if not specified (null if required).</param>
    public ProviderCapabilities(
        bool supportsStreaming = false,
        bool supportsTools = false,
        bool supportsSystemMessages = false,
        bool supportsVision = false,
        int? maxContextLength = null,
        string[]? supportedModels = null,
        string? defaultModel = null)
    {
        this.SupportsStreaming = supportsStreaming;
        this.SupportsTools = supportsTools;
        this.SupportsSystemMessages = supportsSystemMessages;
        this.SupportsVision = supportsVision;
        this.MaxContextLength = maxContextLength;
        this.SupportedModels = supportedModels;
        this.DefaultModel = defaultModel;
    }

    /// <summary>
    /// Gets a value indicating whether the provider supports streaming responses.
    /// </summary>
    /// <remarks>
    /// FR-004-73: SupportsStreaming property (bool, defaults to false).
    /// </remarks>
    [JsonPropertyName("supportsStreaming")]
    public bool SupportsStreaming { get; init; }

    /// <summary>
    /// Gets a value indicating whether the provider supports tool/function calling.
    /// </summary>
    /// <remarks>
    /// FR-004-74: SupportsTools property (bool, defaults to false).
    /// </remarks>
    [JsonPropertyName("supportsTools")]
    public bool SupportsTools { get; init; }

    /// <summary>
    /// Gets a value indicating whether the provider supports system messages.
    /// </summary>
    /// <remarks>
    /// FR-004-75: SupportsSystemMessages property (bool, defaults to false).
    /// </remarks>
    [JsonPropertyName("supportsSystemMessages")]
    public bool SupportsSystemMessages { get; init; }

    /// <summary>
    /// Gets a value indicating whether the provider supports vision (image) inputs.
    /// </summary>
    /// <remarks>
    /// FR-004-76: SupportsVision property (bool, defaults to false).
    /// </remarks>
    [JsonPropertyName("supportsVision")]
    public bool SupportsVision { get; init; }

    /// <summary>
    /// Gets the maximum context length in tokens (null if unknown/unlimited).
    /// </summary>
    /// <remarks>
    /// FR-004-77, FR-004-78: MaxContextLength property (nullable int).
    /// </remarks>
    [JsonPropertyName("maxContextLength")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? MaxContextLength { get; init; }

    /// <summary>
    /// Gets the list of supported model identifiers (null if dynamic/unknown).
    /// </summary>
    /// <remarks>
    /// FR-004-79: SupportedModels property (nullable array).
    /// </remarks>
    [JsonPropertyName("supportedModels")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string[]? SupportedModels { get; init; }

    /// <summary>
    /// Gets the default model identifier to use if not specified (null if required).
    /// </summary>
    /// <remarks>
    /// FR-004-80: DefaultModel property (nullable string).
    /// </remarks>
    [JsonPropertyName("defaultModel")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? DefaultModel { get; init; }
}
