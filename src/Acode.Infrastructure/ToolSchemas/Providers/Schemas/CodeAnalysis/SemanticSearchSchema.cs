namespace Acode.Infrastructure.ToolSchemas.Providers.Schemas.CodeAnalysis;

using System.Text.Json;
using Acode.Domain.Models.Inference;

/// <summary>
/// JSON Schema for the semantic_search tool.
/// </summary>
internal static class SemanticSearchSchema
{
    public static ToolDefinition CreateToolDefinition()
    {
        var properties = new Dictionary<string, JsonElement>
        {
            ["query"] = SchemaBuilder.StringProperty(
                "Natural language query describing what to find (e.g., 'authentication logic')",
                minLength: 3,
                maxLength: 500),
            ["path"] = SchemaBuilder.StringProperty(
                "Directory to search in (default: current directory)",
                maxLength: 4096,
                defaultValue: "."),
            ["max_results"] = SchemaBuilder.IntegerProperty(
                "Maximum number of results to return (default: 10)",
                minimum: 1,
                maximum: 50,
                defaultValue: 10),
            ["file_pattern"] = SchemaBuilder.StringProperty(
                "Glob pattern to filter files (e.g., '*.cs', '*.py')",
                maxLength: 256),
            ["include_context"] = SchemaBuilder.BooleanProperty(
                "Include surrounding code context in results (default: true)",
                defaultValue: true)
        };

        var schema = SchemaBuilder.CreateObjectSchema(properties, new[] { "query" });

        return new ToolDefinition(
            "semantic_search",
            "Search code using natural language. Finds relevant code based on meaning, not just text matches.",
            schema);
    }
}
