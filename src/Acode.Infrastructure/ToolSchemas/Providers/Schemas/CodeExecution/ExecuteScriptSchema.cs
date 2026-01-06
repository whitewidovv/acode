namespace Acode.Infrastructure.ToolSchemas.Providers.Schemas.CodeExecution;

using System.Text.Json;
using Acode.Domain.Models.Inference;

/// <summary>
/// JSON Schema for the execute_script tool.
/// </summary>
internal static class ExecuteScriptSchema
{
    public static ToolDefinition CreateToolDefinition()
    {
        var properties = new Dictionary<string, JsonElement>
        {
            ["script"] = SchemaBuilder.StringProperty(
                "Script content to execute (max 64KB)",
                minLength: 1,
                maxLength: 65536),
            ["language"] = SchemaBuilder.StringProperty(
                "Script language/interpreter",
                enumValues: new[] { "python", "bash", "powershell", "node" }),
            ["working_directory"] = SchemaBuilder.StringProperty(
                "Directory to execute the script in (default: current directory)",
                maxLength: 4096),
            ["timeout_seconds"] = SchemaBuilder.IntegerProperty(
                "Maximum time to wait for script completion in seconds (default: 120)",
                minimum: 1,
                maximum: 3600,
                defaultValue: 120),
            ["capture_output"] = SchemaBuilder.BooleanProperty(
                "Capture stdout and stderr (default: true)",
                defaultValue: true)
        };

        var schema = SchemaBuilder.CreateObjectSchema(properties, new[] { "script", "language" });

        return new ToolDefinition(
            "execute_script",
            "Execute a script in a specified language. Script content is passed directly to the interpreter.",
            schema);
    }
}
