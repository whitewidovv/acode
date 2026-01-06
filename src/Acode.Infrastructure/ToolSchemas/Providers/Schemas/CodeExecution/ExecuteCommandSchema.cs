namespace Acode.Infrastructure.ToolSchemas.Providers.Schemas.CodeExecution;

using System.Text.Json;
using Acode.Domain.Models.Inference;

/// <summary>
/// JSON Schema for the execute_command tool.
/// </summary>
internal static class ExecuteCommandSchema
{
    public static ToolDefinition CreateToolDefinition()
    {
        var properties = new Dictionary<string, JsonElement>
        {
            ["command"] = SchemaBuilder.StringProperty(
                "Shell command to execute (e.g., 'dotnet build', 'npm install')",
                minLength: 1,
                maxLength: 8192),
            ["working_directory"] = SchemaBuilder.StringProperty(
                "Directory to execute the command in (default: current directory)",
                maxLength: 4096),
            ["timeout_seconds"] = SchemaBuilder.IntegerProperty(
                "Maximum time to wait for command completion in seconds (default: 120)",
                minimum: 1,
                maximum: 3600,
                defaultValue: 120),
            ["shell"] = SchemaBuilder.StringProperty(
                "Shell to use for execution (default: auto-detect)",
                enumValues: new[] { "bash", "powershell", "cmd", "sh" }),
            ["capture_output"] = SchemaBuilder.BooleanProperty(
                "Capture stdout and stderr (default: true)",
                defaultValue: true)
        };

        var schema = SchemaBuilder.CreateObjectSchema(properties, new[] { "command" });

        return new ToolDefinition(
            "execute_command",
            "Execute a shell command and return its output. Use for build, test, and system operations.",
            schema);
    }
}
