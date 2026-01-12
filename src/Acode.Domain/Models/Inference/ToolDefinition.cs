namespace Acode.Domain.Models.Inference;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

/// <summary>
/// Represents a tool definition for language model tool calling.
/// </summary>
/// <remarks>
/// FR-004a-71: System MUST define ToolDefinition record.
/// FR-004a-72: ToolDefinition MUST be immutable.
/// FR-004a-73 to FR-004a-88: Properties, validation, serialization.
/// </remarks>
[method: JsonConstructor]
public sealed record ToolDefinition(string Name, string Description, JsonElement Parameters, bool Strict = true)
{
    private static readonly Regex NamePattern = new Regex(@"^[a-zA-Z0-9_]+$", RegexOptions.Compiled);

    /// <summary>
    /// Gets the name of the tool.
    /// </summary>
    /// <remarks>
    /// FR-004a-73, FR-004a-74: Name follows same rules as ToolCall.Name.
    /// </remarks>
    [JsonPropertyName("name")]
    public string Name { get; init; } = ValidateName(Name);

    /// <summary>
    /// Gets the description of what the tool does.
    /// </summary>
    /// <remarks>
    /// FR-004a-75, FR-004a-76: Description MUST be non-empty.
    /// FR-004a-77: Description SHOULD be max 1024 characters.
    /// </remarks>
    [JsonPropertyName("description")]
    public string Description { get; init; } = ValidateDescription(Description);

    /// <summary>
    /// Gets the JSON Schema defining the tool's parameters.
    /// </summary>
    /// <remarks>
    /// FR-004a-78, FR-004a-79: Parameters MUST be JsonElement (JSON Schema).
    /// FR-004a-80, FR-004a-81: Parameters MUST be valid JSON Schema with type: "object".
    /// </remarks>
    [JsonPropertyName("parameters")]
    public JsonElement Parameters { get; init; } = ValidateParameters(Parameters);

    /// <summary>
    /// Gets a value indicating whether strict schema validation is enforced.
    /// </summary>
    /// <remarks>
    /// FR-004a-82: ToolDefinition MAY have Strict property.
    /// FR-004a-83: Strict MUST default to true.
    /// FR-004a-84: Strict=true enforces additionalProperties: false.
    /// </remarks>
    [JsonPropertyName("strict")]
    public bool Strict { get; init; } = Strict;

    public bool Equals(ToolDefinition? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        // Compare all properties including Parameters using JSON text equality
        return this.Name == other.Name
            && this.Description == other.Description
            && this.Strict == other.Strict
            && this.Parameters.GetRawText() == other.Parameters.GetRawText();
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(this.Name, this.Description, this.Strict, this.Parameters.GetRawText());
    }

    /// <summary>
    /// Creates a ToolDefinition from a C# type using JSON schema generation.
    /// </summary>
    /// <typeparam name="T">The parameter type to generate schema for.</typeparam>
    /// <param name="name">The tool name.</param>
    /// <param name="description">The tool description.</param>
    /// <param name="strict">Whether to enforce strict schema validation.</param>
    /// <returns>A ToolDefinition with auto-generated parameter schema.</returns>
    /// <remarks>
    /// FR-004a-89, FR-004a-90: CreateFromType MUST generate schema from C# type.
    /// Uses reflection to generate JSON Schema for record types and classes with properties.
    /// Supports: string, int, bool, double, DateTime, and nullable variants.
    /// </remarks>
    public static ToolDefinition CreateFromType<T>(string name, string description, bool strict = true)
    {
        var type = typeof(T);
        var properties = new Dictionary<string, object>();
        var required = new List<string>();

        // Get all public properties
        foreach (var prop in type.GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance))
        {
            var propSchema = new Dictionary<string, object>();
            var propType = prop.PropertyType;

            // Check if nullable
            if (propType.IsGenericType && propType.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                // Nullable<T> - optional in schema
                propType = Nullable.GetUnderlyingType(propType)!;
            }
            else if (propType.IsValueType)
            {
                // Non-nullable value type (int, bool, etc.) - required in schema
                required.Add(ToCamelCase(prop.Name));
            }
            else
            {
                // Reference type - required only if marked with RequiredMemberAttribute
                var requiredMember = prop.GetCustomAttributes(typeof(System.Runtime.CompilerServices.RequiredMemberAttribute), false).Any();
                if (requiredMember)
                {
                    required.Add(ToCamelCase(prop.Name));
                }
            }

            // Map C# type to JSON Schema type
            if (propType == typeof(string))
            {
                propSchema["type"] = "string";
            }
            else if (propType == typeof(int) || propType == typeof(long))
            {
                propSchema["type"] = "integer";
            }
            else if (propType == typeof(double) || propType == typeof(float) || propType == typeof(decimal))
            {
                propSchema["type"] = "number";
            }
            else if (propType == typeof(bool))
            {
                propSchema["type"] = "boolean";
            }
            else if (propType == typeof(DateTime) || propType == typeof(DateTimeOffset))
            {
                propSchema["type"] = "string";
                propSchema["format"] = "date-time";
            }
            else
            {
                // Default to string for unknown types
                propSchema["type"] = "string";
            }

            properties[ToCamelCase(prop.Name)] = propSchema;
        }

        // Build schema JSON
        var schema = new Dictionary<string, object>
        {
            ["type"] = "object",
            ["properties"] = properties,
        };

        if (required.Count > 0)
        {
            schema["required"] = required;
        }

        // Serialize to JsonElement
        var jsonString = JsonSerializer.Serialize(schema);
        var jsonDoc = JsonDocument.Parse(jsonString);
        var parameters = jsonDoc.RootElement.Clone();

        return new ToolDefinition(name, description, parameters, strict);
    }

    private static string ToCamelCase(string str)
    {
        if (string.IsNullOrEmpty(str) || char.IsLower(str[0]))
        {
            return str;
        }

        return char.ToLowerInvariant(str[0]) + str[1..];
    }

    private static string ValidateName(string name)
    {
        // FR-004a-74: Name MUST follow same rules as ToolCall.Name
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("ToolDefinition Name must be non-empty.", nameof(Name));
        }

        if (name.Length > 64)
        {
            throw new ArgumentException("ToolDefinition Name must be 64 characters or less.", nameof(Name));
        }

        if (!NamePattern.IsMatch(name))
        {
            throw new ArgumentException("ToolDefinition Name must contain only alphanumeric characters and underscores.", nameof(Name));
        }

        return name;
    }

    private static string ValidateDescription(string description)
    {
        // FR-004a-76: Description MUST be non-empty
        if (string.IsNullOrWhiteSpace(description))
        {
            throw new ArgumentException("ToolDefinition Description must be non-empty.", nameof(Description));
        }

        // FR-004a-77: Description SHOULD be max 1024 characters
        if (description.Length > 1024)
        {
            throw new ArgumentException("ToolDefinition Description must be 1024 characters or less.", nameof(Description));
        }

        return description;
    }

    private static JsonElement ValidateParameters(JsonElement parameters)
    {
        // FR-004a-80: Parameters MUST be valid JSON Schema object
        // FR-004a-81: Parameters MUST have type: "object"
        if (parameters.ValueKind != JsonValueKind.Object)
        {
            throw new ArgumentException("ToolDefinition Parameters must be a JSON object.", nameof(Parameters));
        }

        if (!parameters.TryGetProperty("type", out var typeElement))
        {
            throw new ArgumentException("ToolDefinition Parameters must have a 'type' property.", nameof(Parameters));
        }

        if (typeElement.GetString() != "object")
        {
            throw new ArgumentException("ToolDefinition Parameters type must be 'object'.", nameof(Parameters));
        }

        return parameters;
    }
}
