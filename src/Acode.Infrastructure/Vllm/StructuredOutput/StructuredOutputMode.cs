namespace Acode.Infrastructure.Vllm.StructuredOutput;

/// <summary>
/// Enumeration of structured output modes that can be applied to vLLM requests.
/// </summary>
public enum StructuredOutputMode
{
    /// <summary>
    /// JSON object mode: Forces output to be valid JSON conforming to json_object type.
    /// </summary>
    JsonObject,

    /// <summary>
    /// JSON schema mode: Forces output to conform to a specific JSON schema.
    /// </summary>
    JsonSchema,

    /// <summary>
    /// Tool schemas mode: Forces output to conform to tool parameter schemas.
    /// </summary>
    ToolSchemas
}
