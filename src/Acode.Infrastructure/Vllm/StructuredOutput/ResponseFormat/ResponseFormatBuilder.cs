namespace Acode.Infrastructure.Vllm.StructuredOutput.ResponseFormat;

using System.Text.Json;

/// <summary>
/// Builds response_format parameters for vLLM structured output requests.
/// </summary>
/// <remarks>
/// FR-008 through FR-013: Response format construction for guided decoding.
/// Supports both json_object and json_schema modes.
/// </remarks>
public sealed class ResponseFormatBuilder
{
    /// <summary>
    /// Builds a response format specification.
    /// </summary>
    /// <param name="type">The response format type.</param>
    /// <param name="schema">The JSON schema (required for json_schema mode).</param>
    /// <returns>A response format specification.</returns>
    /// <exception cref="ArgumentException">Thrown when schema is required but null.</exception>
    public VllmResponseFormat Build(ResponseFormatType type, JsonElement? schema = null)
    {
        return type switch
        {
            ResponseFormatType.JsonObject => new VllmResponseFormat
            {
                Type = "json_object",
            },
            ResponseFormatType.JsonSchema => new VllmResponseFormat
            {
                Type = "json_schema",
                JsonSchema = schema ?? throw new ArgumentException("Schema is required for json_schema mode", nameof(schema)),
            },
            _ => throw new ArgumentException($"Unsupported response format type: {type}", nameof(type)),
        };
    }
}
