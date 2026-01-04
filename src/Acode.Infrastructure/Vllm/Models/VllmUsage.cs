using System.Text.Json.Serialization;

namespace Acode.Infrastructure.Vllm.Models;

/// <summary>
/// Represents token usage information.
/// </summary>
public sealed class VllmUsage
{
    /// <summary>
    /// Gets or sets the number of prompt tokens.
    /// </summary>
    [JsonPropertyName("prompt_tokens")]
    public int PromptTokens { get; set; }

    /// <summary>
    /// Gets or sets the number of completion tokens.
    /// </summary>
    [JsonPropertyName("completion_tokens")]
    public int CompletionTokens { get; set; }

    /// <summary>
    /// Gets or sets the total number of tokens.
    /// </summary>
    [JsonPropertyName("total_tokens")]
    public int TotalTokens { get; set; }
}
