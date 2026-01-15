namespace Acode.Infrastructure.Vllm.StructuredOutput.ResponseFormat;

/// <summary>
/// Enumeration of supported response format types.
/// </summary>
public enum ResponseFormatType
{
    /// <summary>
    /// JSON object format without specific schema constraints.
    /// </summary>
    JsonObject = 0,

    /// <summary>
    /// JSON schema-constrained format with specific schema.
    /// </summary>
    JsonSchema = 1,
}
