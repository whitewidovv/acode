namespace Acode.Infrastructure.ToolSchemas.Providers.Schemas.FileOperations;

using System.Text.Json;
using Acode.Domain.Models.Inference;

/// <summary>
/// JSON Schema for the write_file tool.
/// </summary>
internal static class WriteFileSchema
{
    public static ToolDefinition CreateToolDefinition()
    {
        var properties = new Dictionary<string, JsonElement>
        {
            ["path"] = SchemaBuilder.StringProperty(
                "Path to the file to write (relative or absolute)",
                minLength: 1,
                maxLength: 4096),
            ["content"] = SchemaBuilder.StringProperty(
                "Content to write to the file (max 1MB)",
                minLength: 0,
                maxLength: 1048576),
            ["encoding"] = SchemaBuilder.StringProperty(
                "Text encoding for writing the file (default: utf-8)",
                enumValues: new[] { "utf-8", "ascii", "utf-16" },
                defaultValue: "utf-8"),
            ["create_directories"] = SchemaBuilder.BooleanProperty(
                "Create parent directories if they don't exist (default: false). Set to true when writing to new folder structures.",
                defaultValue: false),
            ["overwrite"] = SchemaBuilder.BooleanProperty(
                "Overwrite the file if it exists (default: true)",
                defaultValue: true)
        };

        var schema = SchemaBuilder.CreateObjectSchema(properties, new[] { "path", "content" });

        return new ToolDefinition(
            "write_file",
            "Write content to a file. Creates the file if it doesn't exist.",
            schema);
    }
}
