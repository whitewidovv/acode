namespace Acode.Infrastructure.Vllm.StructuredOutput;

using System.Text.Json;
using Acode.Application.Inference;
using Acode.Domain.Models.Inference;
using Acode.Infrastructure.Vllm.StructuredOutput.Capability;
using Acode.Infrastructure.Vllm.StructuredOutput.Configuration;
using Acode.Infrastructure.Vllm.StructuredOutput.Fallback;
using Acode.Infrastructure.Vllm.StructuredOutput.ResponseFormat;
using Acode.Infrastructure.Vllm.StructuredOutput.Schema;

/// <summary>
/// Enumeration of validation failure reasons.
/// </summary>
public enum ValidationFailureReason
{
    /// <summary>
    /// Structured output is disabled.
    /// </summary>
    Disabled,

    /// <summary>
    /// The provided schema is invalid.
    /// </summary>
    InvalidSchema,

    /// <summary>
    /// The model does not support structured output.
    /// </summary>
    UnsupportedModel,

    /// <summary>
    /// A general enrichment error occurred.
    /// </summary>
    EnrichmentError,
}

/// <summary>
/// Orchestrates structured output enforcement for vLLM requests.
/// </summary>
/// <remarks>
/// FR-014 through FR-018, FR-054 through FR-058: Main orchestrator coordinating all structured output components.
/// Integrates: SchemaTransformer, SchemaValidator, CapabilityDetector, ResponseFormatBuilder, FallbackHandler.
/// </remarks>
public sealed class StructuredOutputHandler
{
    private readonly StructuredOutputConfiguration _config;
    private readonly SchemaValidator _schemaValidator;
    private readonly CapabilityDetector _capabilityDetector;
    private readonly CapabilityCache _capabilityCache;
    private readonly ResponseFormatBuilder _responseFormatBuilder;
    private readonly GuidedDecodingBuilder _guidedDecodingBuilder;
    private readonly FallbackHandler _fallbackHandler;

    /// <summary>
    /// Initializes a new instance of the <see cref="StructuredOutputHandler"/> class.
    /// </summary>
    /// <param name="config">The structured output configuration.</param>
    /// <param name="schemaValidator">The schema validator.</param>
    /// <param name="capabilityDetector">The capability detector.</param>
    /// <param name="capabilityCache">The capability cache.</param>
    /// <param name="responseFormatBuilder">The response format builder.</param>
    /// <param name="guidedDecodingBuilder">The guided decoding builder.</param>
    /// <param name="fallbackHandler">The fallback handler.</param>
    public StructuredOutputHandler(
        StructuredOutputConfiguration config,
        SchemaValidator schemaValidator,
        CapabilityDetector capabilityDetector,
        CapabilityCache capabilityCache,
        ResponseFormatBuilder responseFormatBuilder,
        GuidedDecodingBuilder guidedDecodingBuilder,
        FallbackHandler fallbackHandler)
    {
        this._config = config ?? throw new ArgumentNullException(nameof(config));
        this._schemaValidator = schemaValidator ?? throw new ArgumentNullException(nameof(schemaValidator));
        this._capabilityDetector = capabilityDetector ?? throw new ArgumentNullException(nameof(capabilityDetector));
        this._capabilityCache = capabilityCache ?? throw new ArgumentNullException(nameof(capabilityCache));
        this._responseFormatBuilder = responseFormatBuilder ?? throw new ArgumentNullException(nameof(responseFormatBuilder));
        this._guidedDecodingBuilder = guidedDecodingBuilder ?? throw new ArgumentNullException(nameof(guidedDecodingBuilder));
        this._fallbackHandler = fallbackHandler ?? throw new ArgumentNullException(nameof(fallbackHandler));
    }

    /// <summary>
    /// Enriches a vLLM request with structured output parameters.
    /// </summary>
    /// <param name="modelId">The model identifier.</param>
    /// <param name="schema">The JSON schema for structured output.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>An enrichment result with response format and metadata.</returns>
    public async Task<EnrichmentResult> EnrichRequestAsync(string modelId, JsonElement schema, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(modelId))
        {
            return EnrichmentResult.CreateDisabled("Model ID required");
        }

        // Check if structured output is enabled for this model
        if (!this._config.IsEnabled(modelId))
        {
            return EnrichmentResult.CreateDisabled("Structured output disabled for model");
        }

        // Validate schema
        var validationResult = this._schemaValidator.Validate(schema);
        if (!validationResult.IsValid)
        {
            return EnrichmentResult.CreateFailed(
                string.Join("; ", validationResult.Errors),
                ValidationFailureReason.InvalidSchema);
        }

        // Get or detect model capabilities
        var capabilities = await this.GetModelCapabilitiesAsync(modelId, cancellationToken).ConfigureAwait(false);
        if (capabilities == null || !capabilities.SupportsGuidedJson)
        {
            return EnrichmentResult.CreateFailed(
                $"Model {modelId} does not support structured output",
                ValidationFailureReason.UnsupportedModel);
        }

        // Select appropriate guided decoding parameter based on schema
        var guidedParameter = this._guidedDecodingBuilder.SelectGuidedParameter(schema);

        // Build response format
        var responseFormat = this._responseFormatBuilder.Build(ResponseFormatType.JsonSchema, schema);

        return EnrichmentResult.CreateSuccess(responseFormat, guidedParameter, capabilities);
    }

    /// <summary>
    /// Handles validation failure with fallback logic.
    /// </summary>
    /// <param name="modelId">The model identifier.</param>
    /// <param name="invalidOutput">The invalid output to recover from.</param>
    /// <param name="schema">The schema for validation.</param>
    /// <param name="maxAttempts">Maximum fallback attempts allowed.</param>
    /// <returns>A fallback handling result.</returns>
    public FallbackResult HandleValidationFailure(string modelId, string invalidOutput, string schema, int maxAttempts = 3)
    {
        if (string.IsNullOrEmpty(invalidOutput))
        {
            return new FallbackResult
            {
                Success = false,
                Reason = FallbackReason.Unrecoverable,
                Message = "No output provided for fallback recovery",
            };
        }

        var context = new FallbackContext
        {
            ModelId = modelId,
            FallbackMode = this._config.IsEnabled(modelId) ? "Managed" : "Disabled",
            InvalidOutput = invalidOutput,
            MaxFallbackAttempts = maxAttempts,
            InitiatedUtc = DateTime.UtcNow,
            ShouldRegenerateOutput = true,
        };

        return this._fallbackHandler.Handle(context, schema);
    }

    /// <summary>
    /// Validates output against the schema.
    /// </summary>
    /// <param name="output">The output to validate.</param>
    /// <param name="schema">The schema as JSON string.</param>
    /// <returns>True if output is valid, false otherwise.</returns>
    public bool ValidateOutput(string output, string schema)
    {
        if (string.IsNullOrEmpty(output) || string.IsNullOrEmpty(schema))
        {
            return false;
        }

        return this._fallbackHandler.Validate(output, schema);
    }

    /// <summary>
    /// Apply structured output constraints to a vLLM request based on ChatRequest.
    /// Handles both ResponseFormat and Tool schemas.
    /// </summary>
    /// <param name="chatRequest">The chat request with possible ResponseFormat or Tools.</param>
    /// <param name="modelId">The model identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>An enrichment result indicating whether structured output should be applied.</returns>
    public async Task<EnrichmentResult> ApplyToRequestAsync(
        ChatRequest chatRequest,
        string modelId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(chatRequest);

        if (!this._config.IsEnabled(modelId))
        {
            return EnrichmentResult.CreateDisabled($"Structured output disabled for model {modelId}");
        }

        // Check ResponseFormat first (higher priority)
        if (chatRequest.ResponseFormat is not null)
        {
            return await this.ApplyResponseFormatAsync(chatRequest.ResponseFormat, modelId, cancellationToken).ConfigureAwait(false);
        }

        // Check Tools second - transform tool schemas
        if (chatRequest.Tools?.Any() == true)
        {
            return this.ApplyToolSchemas(chatRequest.Tools, modelId);
        }

        // No structured output needed
        return EnrichmentResult.CreateDisabled("No ResponseFormat or Tools configured");
    }

    private async Task<EnrichmentResult> ApplyResponseFormatAsync(
        Application.Inference.ResponseFormat format,
        string modelId,
        CancellationToken cancellationToken)
    {
        try
        {
            // Detect model capabilities
            var capabilities = await this.GetModelCapabilitiesAsync(modelId, cancellationToken).ConfigureAwait(false);

            if (format.Type == "json_object")
            {
                if (capabilities == null || !capabilities.SupportsGuidedJson)
                {
                    return EnrichmentResult.CreateFailed(
                        $"Model {modelId} does not support json_object mode",
                        ValidationFailureReason.UnsupportedModel);
                }

                var vllmFormat = new VllmResponseFormat { Type = "json_object" };
                return EnrichmentResult.CreateSuccess(vllmFormat, new object(), capabilities);
            }

            if (format.Type == "json_schema" && format.JsonSchema is not null)
            {
                if (capabilities == null || !capabilities.SupportsGuidedJson)
                {
                    return EnrichmentResult.CreateFailed(
                        $"Model {modelId} does not support json_schema mode",
                        ValidationFailureReason.UnsupportedModel);
                }

                // Use existing EnrichRequestAsync for schema transformation
                return await this.EnrichRequestAsync(modelId, format.JsonSchema.Schema, cancellationToken).ConfigureAwait(false);
            }

            return EnrichmentResult.CreateFailed(
                $"Unknown response format type: {format.Type}",
                ValidationFailureReason.InvalidSchema);
        }
        catch (Exception ex)
        {
            return EnrichmentResult.CreateFailed(
                $"Error applying response format: {ex.Message}",
                ValidationFailureReason.EnrichmentError);
        }
    }

    private EnrichmentResult ApplyToolSchemas(
        ToolDefinition[] tools,
        string modelId)
    {
        try
        {
            if (tools.Length == 0)
            {
                return EnrichmentResult.CreateDisabled("No tools provided");
            }

            // Collect all tool parameter schemas
            var toolSchemas = new List<JsonElement>();
            foreach (var tool in tools)
            {
                toolSchemas.Add(tool.Parameters);
            }

            // Get capabilities for tool schema validation
            var capabilities = this._capabilityCache.TryGetCached(modelId, out var cached) && cached != null ? cached : new ModelCapabilities { ModelId = modelId };

            // Return success with tool schemas for vLLM to apply
            return EnrichmentResult.CreateSuccess(new VllmResponseFormat { Type = "json_object" }, toolSchemas.ToArray(), capabilities);
        }
        catch (Exception ex)
        {
            return EnrichmentResult.CreateFailed(
                $"Error applying tool schemas: {ex.Message}",
                ValidationFailureReason.EnrichmentError);
        }
    }

    private async Task<ModelCapabilities?> GetModelCapabilitiesAsync(string modelId, CancellationToken cancellationToken)
    {
        // Try cache first
        if (this._capabilityCache.TryGetCached(modelId, out var cached) && cached != null)
        {
            // Check if refresh needed
            if (!this._capabilityDetector.RequiresRefresh(cached))
            {
                return cached;
            }
        }

        // Detect capabilities
        var capabilities = await this._capabilityDetector.DetectCapabilitiesAsync(modelId, cancellationToken).ConfigureAwait(false);

        // Cache result
        this._capabilityCache.Cache(capabilities);

        return capabilities;
    }
}

/// <summary>
/// Result of request enrichment operation.
/// </summary>
public sealed class EnrichmentResult
{
    /// <summary>
    /// Gets or sets a value indicating whether enrichment was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the response format (if successful).
    /// </summary>
    public VllmResponseFormat? ResponseFormat { get; set; }

    /// <summary>
    /// Gets or sets the guided parameter (if applicable).
    /// </summary>
    public object? GuidedParameter { get; set; }

    /// <summary>
    /// Gets or sets the model capabilities (if successful).
    /// </summary>
    public ModelCapabilities? Capabilities { get; set; }

    /// <summary>
    /// Gets or sets the failure reason (if not successful).
    /// </summary>
    public string? FailureReason { get; set; }

    /// <summary>
    /// Gets or sets the structured validation failure reason code.
    /// </summary>
    public ValidationFailureReason FailureReasonCode { get; set; }

    /// <summary>
    /// Creates a successful enrichment result.
    /// </summary>
    /// <param name="responseFormat">The response format for the request.</param>
    /// <param name="guidedParameter">The guided decoding parameter (choice/regex/json).</param>
    /// <param name="capabilities">The detected model capabilities.</param>
    /// <returns>A successful enrichment result.</returns>
    public static EnrichmentResult CreateSuccess(VllmResponseFormat responseFormat, object guidedParameter, ModelCapabilities capabilities)
    {
        return new EnrichmentResult
        {
            Success = true,
            ResponseFormat = responseFormat,
            GuidedParameter = guidedParameter,
            Capabilities = capabilities,
        };
    }

    /// <summary>
    /// Creates a disabled enrichment result.
    /// </summary>
    /// <param name="reason">The reason structured output is disabled.</param>
    /// <returns>A disabled enrichment result.</returns>
    public static EnrichmentResult CreateDisabled(string reason)
    {
        return new EnrichmentResult
        {
            Success = false,
            FailureReason = reason,
            FailureReasonCode = ValidationFailureReason.Disabled,
        };
    }

    /// <summary>
    /// Creates a failed enrichment result.
    /// </summary>
    /// <param name="reason">The failure reason message.</param>
    /// <param name="reasonCode">The structured failure reason code.</param>
    /// <returns>A failed enrichment result.</returns>
    public static EnrichmentResult CreateFailed(string reason, ValidationFailureReason reasonCode)
    {
        return new EnrichmentResult
        {
            Success = false,
            FailureReason = reason,
            FailureReasonCode = reasonCode,
        };
    }
}
