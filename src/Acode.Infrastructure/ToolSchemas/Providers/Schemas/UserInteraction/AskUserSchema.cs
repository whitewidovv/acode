namespace Acode.Infrastructure.ToolSchemas.Providers.Schemas.UserInteraction;

using System.Text.Json;
using Acode.Domain.Models.Inference;

/// <summary>
/// JSON Schema for the ask_user tool.
/// </summary>
internal static class AskUserSchema
{
    public static ToolDefinition CreateToolDefinition()
    {
        var properties = new Dictionary<string, JsonElement>
        {
            ["question"] = SchemaBuilder.StringProperty(
                "Question to ask the user (clear and concise)",
                minLength: 5,
                maxLength: 1000),
            ["options"] = SchemaBuilder.ArrayProperty(
                "Optional list of choices for the user to select from",
                SchemaBuilder.StringProperty("A choice option", minLength: 1, maxLength: 200),
                minItems: 2,
                maxItems: 10),
            ["default_option"] = SchemaBuilder.StringProperty(
                "Default option if user provides no input (must be one of the options)",
                maxLength: 200),
            ["timeout_seconds"] = SchemaBuilder.IntegerProperty(
                "Maximum time to wait for user response (default: 300 = 5 minutes)",
                minimum: 10,
                maximum: 3600,
                defaultValue: 300)
        };

        var schema = SchemaBuilder.CreateObjectSchema(properties, new[] { "question" });

        return new ToolDefinition(
            "ask_user",
            "Ask the user a question and wait for their response. Use for clarification or choices.",
            schema);
    }
}
