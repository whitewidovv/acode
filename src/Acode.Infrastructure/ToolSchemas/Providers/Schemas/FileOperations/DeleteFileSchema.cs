namespace Acode.Infrastructure.ToolSchemas.Providers.Schemas.FileOperations;

using System.Text.Json;
using Acode.Domain.Models.Inference;

/// <summary>
/// JSON Schema for the delete_file tool.
/// </summary>
internal static class DeleteFileSchema
{
    public static ToolDefinition CreateToolDefinition()
    {
        var properties = new Dictionary<string, JsonElement>
        {
            ["path"] = SchemaBuilder.StringProperty(
                "Path to the file or directory to delete",
                minLength: 1,
                maxLength: 4096),
            ["recursive"] = SchemaBuilder.BooleanProperty(
                "Delete directories recursively (required for non-empty directories)",
                defaultValue: false),
            ["confirm"] = SchemaBuilder.BooleanProperty(
                "Explicit confirmation for deletion (must be true to proceed)",
                defaultValue: false)
        };

        var schema = SchemaBuilder.CreateObjectSchema(properties, new[] { "path", "confirm" });

        return new ToolDefinition(
            "delete_file",
            "Delete a file or directory. Requires explicit confirmation.",
            schema);
    }
}
