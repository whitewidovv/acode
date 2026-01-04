using System.Text.Json.Serialization;

namespace Acode.Infrastructure.Ollama.Models;

/// <summary>
/// Generation options for controlling model behavior.
/// </summary>
public sealed record OllamaOptions
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OllamaOptions"/> class.
    /// </summary>
    /// <param name="temperature">The sampling temperature.</param>
    /// <param name="topP">The nucleus sampling threshold.</param>
    /// <param name="seed">The random seed (optional).</param>
    /// <param name="numCtx">The context window size (optional).</param>
    /// <param name="stop">The stop sequences (optional).</param>
    public OllamaOptions(
        double temperature,
        double topP,
        int? seed = null,
        int? numCtx = null,
        string[]? stop = null)
    {
        this.Temperature = temperature;
        this.TopP = topP;
        this.Seed = seed;
        this.NumCtx = numCtx;
        this.Stop = stop;
    }

    /// <summary>
    /// Gets the sampling temperature (0.0 to 2.0).
    /// </summary>
    [JsonPropertyName("temperature")]
    public double Temperature { get; init; }

    /// <summary>
    /// Gets the nucleus sampling threshold (0.0 to 1.0).
    /// </summary>
    [JsonPropertyName("top_p")]
    public double TopP { get; init; }

    /// <summary>
    /// Gets the random seed for deterministic generation (optional).
    /// </summary>
    [JsonPropertyName("seed")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? Seed { get; init; }

    /// <summary>
    /// Gets the context window size in tokens (optional).
    /// </summary>
    [JsonPropertyName("num_ctx")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? NumCtx { get; init; }

    /// <summary>
    /// Gets the stop sequences for generation (optional).
    /// </summary>
    [JsonPropertyName("stop")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string[]? Stop { get; init; }
}
