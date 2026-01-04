namespace Acode.Application.Inference;

using System;
using System.Text.Json.Serialization;

/// <summary>
/// Model inference parameters for controlling generation behavior.
/// </summary>
/// <remarks>
/// FR-004-55: ModelParameters record defined.
/// FR-004-56 to FR-004-65: Properties and validation.
/// </remarks>
public sealed record ModelParameters
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ModelParameters"/> class.
    /// </summary>
    /// <param name="model">Model identifier.</param>
    /// <param name="temperature">Sampling temperature (0.0-2.0).</param>
    /// <param name="maxTokens">Maximum tokens to generate.</param>
    /// <param name="topP">Nucleus sampling probability.</param>
    /// <param name="stopSequences">Sequences that stop generation.</param>
    /// <param name="seed">Random seed for deterministic generation.</param>
    public ModelParameters(
        string model,
        double temperature = 0.7,
        int? maxTokens = null,
        double topP = 1.0,
        string[]? stopSequences = null,
        int? seed = null)
    {
        if (string.IsNullOrWhiteSpace(model))
        {
            throw new ArgumentException("Model must be non-empty", "Model");
        }

        if (temperature < 0.0 || temperature > 2.0)
        {
            throw new ArgumentOutOfRangeException(nameof(temperature), temperature, "Temperature must be in range [0.0, 2.0]");
        }

        this.Model = model;
        this.Temperature = temperature;
        this.MaxTokens = maxTokens;
        this.TopP = topP;
        this.StopSequences = stopSequences;
        this.Seed = seed;
    }

    /// <summary>
    /// Gets the model identifier.
    /// </summary>
    /// <remarks>
    /// FR-004-56, FR-004-57: Model is required model identifier string.
    /// </remarks>
    [JsonPropertyName("model")]
    public string Model { get; init; }

    /// <summary>
    /// Gets the sampling temperature.
    /// </summary>
    /// <remarks>
    /// FR-004-58, FR-004-59, FR-004-60: Temperature defaults to 0.7, range [0.0, 2.0].
    /// </remarks>
    [JsonPropertyName("temperature")]
    public double Temperature { get; init; }

    /// <summary>
    /// Gets the maximum tokens to generate.
    /// </summary>
    /// <remarks>
    /// FR-004-61, FR-004-62: MaxTokens nullable (use model default if null).
    /// </remarks>
    [JsonPropertyName("maxTokens")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? MaxTokens { get; init; }

    /// <summary>
    /// Gets the nucleus sampling probability.
    /// </summary>
    /// <remarks>
    /// FR-004-63, FR-004-64: TopP defaults to 1.0.
    /// </remarks>
    [JsonPropertyName("topP")]
    public double TopP { get; init; }

    /// <summary>
    /// Gets the stop sequences.
    /// </summary>
    /// <remarks>
    /// FR-004-65: StopSequences property (nullable).
    /// </remarks>
    [JsonPropertyName("stopSequences")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string[]? StopSequences { get; init; }

    /// <summary>
    /// Gets the random seed for deterministic generation.
    /// </summary>
    [JsonPropertyName("seed")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? Seed { get; init; }
}
