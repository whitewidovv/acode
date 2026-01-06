namespace Acode.Infrastructure.ToolSchemas.Providers.Schemas.FileOperations;

using System.Text.Json;
using Acode.Domain.Models.Inference;

/// <summary>
/// JSON Schema for the list_directory tool.
/// </summary>
internal static class ListDirectorySchema
{
    public static ToolDefinition CreateToolDefinition()
    {
        var properties = new Dictionary<string, JsonElement>
        {
            ["path"] = SchemaBuilder.StringProperty(
                "Path to the directory to list (default: current directory)",
                maxLength: 4096,
                defaultValue: "."),
            ["recursive"] = SchemaBuilder.BooleanProperty(
                "List files recursively in subdirectories (default: false)",
                defaultValue: false),
            ["max_depth"] = SchemaBuilder.IntegerProperty(
                "Maximum depth for recursive listing (default: 3)",
                minimum: 1,
                maximum: 10,
                defaultValue: 3),
            ["include_hidden"] = SchemaBuilder.BooleanProperty(
                "Include hidden files and directories (default: false)",
                defaultValue: false),
            ["pattern"] = SchemaBuilder.StringProperty(
                "Glob pattern to filter files (e.g., '*.cs', '**/*.json')",
                maxLength: 256)
        };

        var schema = SchemaBuilder.CreateObjectSchema(properties, Array.Empty<string>());

        return new ToolDefinition(
            "list_directory",
            "List files and directories in a given path. Returns names and types.",
            schema);
    }
}
