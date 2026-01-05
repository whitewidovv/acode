namespace Acode.Infrastructure.Vllm.StructuredOutput.Configuration;

using Microsoft.Extensions.Configuration;

/// <summary>
/// Configuration for vLLM structured output enforcement.
/// </summary>
/// <remarks>
/// FR-007e: Structured output configuration support.
/// Configures guided decoding, fallback behavior, and schema limits.
/// </remarks>
public sealed class StructuredOutputConfiguration
{
    /// <summary>
    /// Gets or sets a value indicating whether structured output is enabled globally.
    /// Default: true.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the default mode for structured output.
    /// Supported: "json_object", "json_schema".
    /// Default: "json_schema".
    /// </summary>
    public string DefaultMode { get; set; } = "json_schema";

    /// <summary>
    /// Gets or sets the fallback configuration.
    /// </summary>
    public FallbackConfiguration Fallback { get; set; } = new();

    /// <summary>
    /// Gets or sets the schema processing configuration.
    /// </summary>
    public SchemaConfiguration Schema { get; set; } = new();

    /// <summary>
    /// Gets the per-model configuration overrides.
    /// </summary>
    public Dictionary<string, ModelStructuredOutputConfig> Models { get; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Loads configuration from IConfiguration.
    /// </summary>
    /// <param name="configuration">The configuration source.</param>
    /// <returns>A structured output configuration instance.</returns>
    public static StructuredOutputConfiguration FromConfiguration(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var section = configuration.GetSection("model:providers:vllm:structured_output");
        var config = new StructuredOutputConfiguration();

        if (section.Exists())
        {
            section.Bind(config);
        }

        // Environment variable overrides
        var envEnabled = Environment.GetEnvironmentVariable("ACODE_VLLM_STRUCTURED_OUTPUT_ENABLED");
        if (envEnabled is not null && bool.TryParse(envEnabled, out var enabled))
        {
            config.Enabled = enabled;
        }

        var envMode = Environment.GetEnvironmentVariable("ACODE_VLLM_STRUCTURED_OUTPUT_MODE");
        if (!string.IsNullOrEmpty(envMode))
        {
            config.DefaultMode = envMode;
        }

        return config;
    }

    /// <summary>
    /// Validates the configuration.
    /// </summary>
    /// <returns>A validation result.</returns>
    public ConfigurationValidationResult Validate()
    {
        var errors = new List<string>();

        if (this.Fallback.MaxRetries < 0 || this.Fallback.MaxRetries > 10)
        {
            errors.Add("Fallback.MaxRetries must be between 0 and 10");
        }

        if (this.Schema.MaxDepth < 1 || this.Schema.MaxDepth > 20)
        {
            errors.Add("Schema.MaxDepth must be between 1 and 20");
        }

        if (this.Schema.MaxSizeBytes < 1024 || this.Schema.MaxSizeBytes > 1_048_576)
        {
            errors.Add("Schema.MaxSizeBytes must be between 1KB and 1MB");
        }

        if (string.IsNullOrWhiteSpace(this.DefaultMode))
        {
            errors.Add("DefaultMode must be specified");
        }
        else if (this.DefaultMode != "json_object" && this.DefaultMode != "json_schema")
        {
            errors.Add("DefaultMode must be 'json_object' or 'json_schema'");
        }

        return new ConfigurationValidationResult
        {
            IsValid = errors.Count == 0,
            Errors = errors,
        };
    }

    /// <summary>
    /// Gets the effective configuration for a specific model.
    /// </summary>
    /// <param name="modelId">The model identifier.</param>
    /// <returns>The effective model configuration.</returns>
    public ModelStructuredOutputConfig GetModelConfig(string modelId)
    {
        if (string.IsNullOrEmpty(modelId))
        {
            return new ModelStructuredOutputConfig
            {
                Enabled = this.Enabled,
                Fallback = this.Fallback,
            };
        }

        if (this.Models.TryGetValue(modelId, out var modelConfig))
        {
            return modelConfig;
        }

        // Return default configuration for unknown models
        return new ModelStructuredOutputConfig
        {
            Enabled = this.Enabled,
            Fallback = this.Fallback,
        };
    }
}
