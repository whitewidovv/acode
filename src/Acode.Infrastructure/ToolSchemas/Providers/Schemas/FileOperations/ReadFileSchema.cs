namespace Acode.Infrastructure.ToolSchemas.Providers.Schemas.FileOperations;

using System.Text.Json;
using Acode.Domain.Models.Inference;

/// <summary>
/// JSON Schema for the read_file tool.
/// </summary>
internal static class ReadFileSchema
{
    public static ToolDefinition CreateToolDefinition()
    {
        var properties = new Dictionary<string, JsonElement>
        {
            ["path"] = SchemaBuilder.StringProperty(
                "Path to the file to read (relative or absolute, e.g., 'src/main.cs')",
                minLength: 1,
                maxLength: 4096),
            ["start_line"] = SchemaBuilder.IntegerProperty(
                "Line number to start reading from (1-indexed, optional)",
                minimum: 1),
            ["end_line"] = SchemaBuilder.IntegerProperty(
                "Line number to stop reading at, inclusive (must be >= start_line)",
                minimum: 1),
            ["encoding"] = SchemaBuilder.StringProperty(
                "Text encoding for reading the file (default: utf-8)",
                enumValues: new[] { "utf-8", "ascii", "utf-16" },
                defaultValue: "utf-8")
        };

        var schema = SchemaBuilder.CreateObjectSchema(properties, new[] { "path" });

        return new ToolDefinition(
            "read_file",
            "Read the contents of a file from the file system. Returns the file content as text.",
            schema);
    }
}
