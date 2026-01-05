namespace Acode.Infrastructure.ToolSchemas.Providers.Schemas.VersionControl;

using System.Text.Json;
using Acode.Domain.Models.Inference;

/// <summary>
/// JSON Schema for the git_commit tool.
/// </summary>
internal static class GitCommitSchema
{
    public static ToolDefinition CreateToolDefinition()
    {
        var properties = new Dictionary<string, JsonElement>
        {
            ["message"] = SchemaBuilder.StringProperty(
                "Commit message (required, 1-500 characters)",
                minLength: 1,
                maxLength: 500),
            ["path"] = SchemaBuilder.StringProperty(
                "Repository path (default: current directory)",
                maxLength: 4096,
                defaultValue: "."),
            ["all"] = SchemaBuilder.BooleanProperty(
                "Stage all tracked modified files before committing (default: false)",
                defaultValue: false),
            ["amend"] = SchemaBuilder.BooleanProperty(
                "Amend the previous commit instead of creating a new one (default: false)",
                defaultValue: false)
        };

        var schema = SchemaBuilder.CreateObjectSchema(properties, new[] { "message" });

        return new ToolDefinition(
            "git_commit",
            "Create a git commit with the specified message. Optionally stage all changes or amend the previous commit.",
            schema);
    }
}
