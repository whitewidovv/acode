namespace Acode.Infrastructure.ToolSchemas.Providers.Schemas.VersionControl;

using System.Text.Json;
using Acode.Domain.Models.Inference;

/// <summary>
/// JSON Schema for the git_status tool.
/// </summary>
internal static class GitStatusSchema
{
    public static ToolDefinition CreateToolDefinition()
    {
        var properties = new Dictionary<string, JsonElement>
        {
            ["path"] = SchemaBuilder.StringProperty(
                "Repository path (default: current directory)",
                maxLength: 4096,
                defaultValue: "."),
            ["include_untracked"] = SchemaBuilder.BooleanProperty(
                "Include untracked files in the status (default: true)",
                defaultValue: true),
            ["short_format"] = SchemaBuilder.BooleanProperty(
                "Use short format output (default: false)",
                defaultValue: false)
        };

        var schema = SchemaBuilder.CreateObjectSchema(properties, Array.Empty<string>());

        return new ToolDefinition(
            "git_status",
            "Get the current git repository status including staged, modified, and untracked files.",
            schema);
    }
}
