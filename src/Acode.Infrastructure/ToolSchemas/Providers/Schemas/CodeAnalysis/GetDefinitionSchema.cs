namespace Acode.Infrastructure.ToolSchemas.Providers.Schemas.CodeAnalysis;

using System.Text.Json;
using Acode.Domain.Models.Inference;

/// <summary>
/// JSON Schema for the get_definition tool.
/// </summary>
internal static class GetDefinitionSchema
{
    public static ToolDefinition CreateToolDefinition()
    {
        var properties = new Dictionary<string, JsonElement>
        {
            ["file"] = SchemaBuilder.StringProperty(
                "Path to the file containing the symbol reference",
                minLength: 1,
                maxLength: 4096),
            ["line"] = SchemaBuilder.IntegerProperty(
                "Line number of the symbol reference (1-indexed)",
                minimum: 1),
            ["column"] = SchemaBuilder.IntegerProperty(
                "Column number of the symbol reference (1-indexed)",
                minimum: 1),
            ["include_source"] = SchemaBuilder.BooleanProperty(
                "Include source code of the definition in the result (default: true)",
                defaultValue: true)
        };

        var schema = SchemaBuilder.CreateObjectSchema(properties, new[] { "file", "line", "column" });

        return new ToolDefinition(
            "get_definition",
            "Go to the definition of a symbol at a specific location. Returns the definition location and optionally source.",
            schema);
    }
}
