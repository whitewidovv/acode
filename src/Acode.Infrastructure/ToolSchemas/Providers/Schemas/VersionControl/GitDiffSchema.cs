namespace Acode.Infrastructure.ToolSchemas.Providers.Schemas.VersionControl;

using System.Text.Json;
using Acode.Domain.Models.Inference;

/// <summary>
/// JSON Schema for the git_diff tool.
/// </summary>
internal static class GitDiffSchema
{
    public static ToolDefinition CreateToolDefinition()
    {
        var properties = new Dictionary<string, JsonElement>
        {
            ["path"] = SchemaBuilder.StringProperty(
                "Repository path (default: current directory)",
                maxLength: 4096,
                defaultValue: "."),
            ["file"] = SchemaBuilder.StringProperty(
                "Specific file to diff (optional, diffs all files if not specified)",
                maxLength: 4096),
            ["staged"] = SchemaBuilder.BooleanProperty(
                "Show staged changes instead of unstaged (default: false)",
                defaultValue: false),
            ["commit"] = SchemaBuilder.StringProperty(
                "Commit hash or ref to compare against (default: HEAD for staged, working tree for unstaged)",
                maxLength: 256),
            ["context_lines"] = SchemaBuilder.IntegerProperty(
                "Number of context lines to include (default: 3)",
                minimum: 0,
                maximum: 20,
                defaultValue: 3)
        };

        var schema = SchemaBuilder.CreateObjectSchema(properties, Array.Empty<string>());

        return new ToolDefinition(
            "git_diff",
            "Show git diff for current changes, staged changes, or between commits.",
            schema);
    }
}
