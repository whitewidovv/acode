namespace Acode.Infrastructure.Vllm.StructuredOutput.ResponseFormat;

using System.Text.Json;

/// <summary>
/// Response format specification for vLLM requests.
/// </summary>
public sealed class VllmResponseFormat
{
    /// <summary>
    /// Gets or sets the response format type.
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the JSON schema (for json_schema mode).
    /// </summary>
    public JsonElement? JsonSchema { get; set; }
}
