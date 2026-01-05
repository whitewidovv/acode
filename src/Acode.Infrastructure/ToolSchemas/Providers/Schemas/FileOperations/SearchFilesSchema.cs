namespace Acode.Infrastructure.ToolSchemas.Providers.Schemas.FileOperations;

using System.Text.Json;
using Acode.Domain.Models.Inference;

/// <summary>
/// JSON Schema for the search_files tool.
/// </summary>
internal static class SearchFilesSchema
{
    public static ToolDefinition CreateToolDefinition()
    {
        var properties = new Dictionary<string, JsonElement>
        {
            ["query"] = SchemaBuilder.StringProperty(
                "Search query (text or regex pattern to find in files)",
                minLength: 1,
                maxLength: 1000),
            ["path"] = SchemaBuilder.StringProperty(
                "Directory to search in (default: current directory)",
                maxLength: 4096,
                defaultValue: "."),
            ["file_pattern"] = SchemaBuilder.StringProperty(
                "Glob pattern to filter files (e.g., '*.cs', '*.py')",
                maxLength: 256),
            ["case_sensitive"] = SchemaBuilder.BooleanProperty(
                "Perform case-sensitive search (default: false)",
                defaultValue: false),
            ["regex"] = SchemaBuilder.BooleanProperty(
                "Interpret query as a regular expression (default: false)",
                defaultValue: false),
            ["max_results"] = SchemaBuilder.IntegerProperty(
                "Maximum number of results to return (default: 100)",
                minimum: 1,
                maximum: 1000,
                defaultValue: 100),
            ["include_context"] = SchemaBuilder.BooleanProperty(
                "Include surrounding lines for context (default: true)",
                defaultValue: true),
            ["context_lines"] = SchemaBuilder.IntegerProperty(
                "Number of context lines before and after match (default: 2)",
                minimum: 0,
                maximum: 10,
                defaultValue: 2)
        };

        var schema = SchemaBuilder.CreateObjectSchema(properties, new[] { "query" });

        return new ToolDefinition(
            "search_files",
            "Search for text or patterns in files. Returns matching lines with file paths and line numbers.",
            schema);
    }
}
