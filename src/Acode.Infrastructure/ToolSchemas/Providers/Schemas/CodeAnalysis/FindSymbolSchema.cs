namespace Acode.Infrastructure.ToolSchemas.Providers.Schemas.CodeAnalysis;

using System.Text.Json;
using Acode.Domain.Models.Inference;

/// <summary>
/// JSON Schema for the find_symbol tool.
/// </summary>
internal static class FindSymbolSchema
{
    public static ToolDefinition CreateToolDefinition()
    {
        var properties = new Dictionary<string, JsonElement>
        {
            ["name"] = SchemaBuilder.StringProperty(
                "Symbol name to find (e.g., 'MyClass', 'processData')",
                minLength: 1,
                maxLength: 256),
            ["symbol_type"] = SchemaBuilder.StringProperty(
                "Type of symbol to find (optional, filters results)",
                enumValues: new[] { "class", "interface", "method", "function", "property", "field", "variable", "enum", "struct" }),
            ["path"] = SchemaBuilder.StringProperty(
                "Directory to search in (default: current directory)",
                maxLength: 4096,
                defaultValue: "."),
            ["exact_match"] = SchemaBuilder.BooleanProperty(
                "Require exact name match (default: false, allows partial matches)",
                defaultValue: false),
            ["case_sensitive"] = SchemaBuilder.BooleanProperty(
                "Perform case-sensitive search (default: false)",
                defaultValue: false)
        };

        var schema = SchemaBuilder.CreateObjectSchema(properties, new[] { "name" });

        return new ToolDefinition(
            "find_symbol",
            "Find symbol definitions (classes, methods, functions) by name in the codebase.",
            schema);
    }
}
