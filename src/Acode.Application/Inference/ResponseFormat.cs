namespace Acode.Application.Inference;

using System.Text.Json;

/// <summary>
/// Specifies the response format for structured output enforcement.
/// </summary>
/// <remarks>
/// FR-008 to FR-014: Response format support for json_object and json_schema types.
/// </remarks>
public sealed class ResponseFormat
{
    /// <summary>
    /// Gets or sets the response format type.
    /// Supported values: "json_object", "json_schema".
    /// </summary>
    public string Type { get; set; } = "json_object";

    /// <summary>
    /// Gets or sets the JSON schema format details (used when Type is "json_schema").
    /// </summary>
    public JsonSchemaFormat? JsonSchema { get; set; }
}

/// <summary>
/// Details for JSON schema response format.
/// </summary>
public sealed class JsonSchemaFormat
{
    /// <summary>
    /// Gets or sets the name of the schema (used for identification).
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the JSON schema document as a JsonElement.
    /// </summary>
    public JsonElement Schema { get; set; }
}
