namespace Acode.Infrastructure.ToolSchemas.Providers.Schemas.VersionControl;

using System.Text.Json;
using Acode.Domain.Models.Inference;

/// <summary>
/// JSON Schema for the git_log tool.
/// </summary>
internal static class GitLogSchema
{
    public static ToolDefinition CreateToolDefinition()
    {
        var properties = new Dictionary<string, JsonElement>
        {
            ["path"] = SchemaBuilder.StringProperty(
                "Repository path (default: current directory)",
                maxLength: 4096,
                defaultValue: "."),
            ["count"] = SchemaBuilder.IntegerProperty(
                "Number of commits to show (default: 10)",
                minimum: 1,
                maximum: 100,
                defaultValue: 10),
            ["file"] = SchemaBuilder.StringProperty(
                "Show only commits affecting this file (optional)",
                maxLength: 4096),
            ["author"] = SchemaBuilder.StringProperty(
                "Filter commits by author name or email (optional)",
                maxLength: 256),
            ["since"] = SchemaBuilder.StringProperty(
                "Show commits after this date (ISO 8601 format, e.g., '2024-01-01')",
                maxLength: 32),
            ["until"] = SchemaBuilder.StringProperty(
                "Show commits before this date (ISO 8601 format)",
                maxLength: 32),
            ["oneline"] = SchemaBuilder.BooleanProperty(
                "Use one-line format (default: false)",
                defaultValue: false)
        };

        var schema = SchemaBuilder.CreateObjectSchema(properties, Array.Empty<string>());

        return new ToolDefinition(
            "git_log",
            "Show git commit history with optional filtering by count, author, date, or file.",
            schema);
    }
}
