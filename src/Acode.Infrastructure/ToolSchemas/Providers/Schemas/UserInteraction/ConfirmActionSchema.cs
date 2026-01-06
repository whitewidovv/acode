namespace Acode.Infrastructure.ToolSchemas.Providers.Schemas.UserInteraction;

using System.Text.Json;
using Acode.Domain.Models.Inference;

/// <summary>
/// JSON Schema for the confirm_action tool.
/// </summary>
internal static class ConfirmActionSchema
{
    public static ToolDefinition CreateToolDefinition()
    {
        var properties = new Dictionary<string, JsonElement>
        {
            ["action"] = SchemaBuilder.StringProperty(
                "Description of the action to confirm (minimum 10 characters for meaningful description)",
                minLength: 10,
                maxLength: 500),
            ["destructive"] = SchemaBuilder.BooleanProperty(
                "Mark as destructive action requiring extra caution (default: false)",
                defaultValue: false),
            ["default_confirm"] = SchemaBuilder.BooleanProperty(
                "Default response if user provides no input (default: false = deny by default)",
                defaultValue: false),
            ["timeout_seconds"] = SchemaBuilder.IntegerProperty(
                "Maximum time to wait for user response (default: 60)",
                minimum: 10,
                maximum: 600,
                defaultValue: 60)
        };

        var schema = SchemaBuilder.CreateObjectSchema(properties, new[] { "action" });

        return new ToolDefinition(
            "confirm_action",
            "Request user confirmation before performing a potentially impactful action.",
            schema);
    }
}
