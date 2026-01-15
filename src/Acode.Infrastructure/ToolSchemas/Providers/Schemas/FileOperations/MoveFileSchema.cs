namespace Acode.Infrastructure.ToolSchemas.Providers.Schemas.FileOperations;

using System.Text.Json;
using Acode.Domain.Models.Inference;

/// <summary>
/// JSON Schema for the move_file tool.
/// </summary>
internal static class MoveFileSchema
{
    public static ToolDefinition CreateToolDefinition()
    {
        var properties = new Dictionary<string, JsonElement>
        {
            ["source"] = SchemaBuilder.StringProperty(
                "Path to the source file or directory to move",
                minLength: 1,
                maxLength: 4096),
            ["destination"] = SchemaBuilder.StringProperty(
                "Path to the destination location",
                minLength: 1,
                maxLength: 4096),
            ["overwrite"] = SchemaBuilder.BooleanProperty(
                "Overwrite destination if it exists (default: false)",
                defaultValue: false),
            ["create_directories"] = SchemaBuilder.BooleanProperty(
                "Create parent directories if they don't exist (default: true)",
                defaultValue: true)
        };

        var schema = SchemaBuilder.CreateObjectSchema(properties, new[] { "source", "destination" });

        return new ToolDefinition(
            "move_file",
            "Move or rename a file or directory. Supports cross-directory moves and overwrite options.",
            schema);
    }
}
