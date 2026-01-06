namespace Acode.Infrastructure.ToolSchemas.Providers.Schemas;

using System.Text.Json;

/// <summary>
/// Helper class for building JSON Schema elements.
/// </summary>
internal static class SchemaBuilder
{
    /// <summary>
    /// Creates a JSON Schema for an object type with properties.
    /// </summary>
    /// <param name="properties">The properties dictionary.</param>
    /// <param name="required">The required field names.</param>
    /// <returns>A JsonElement representing the schema.</returns>
    public static JsonElement CreateObjectSchema(
        Dictionary<string, JsonElement> properties,
        IEnumerable<string> required)
    {
        var schema = new Dictionary<string, object>
        {
            ["type"] = "object",
            ["properties"] = properties,
            ["required"] = required.ToArray()
        };

        return JsonSerializer.SerializeToElement(schema);
    }

    /// <summary>
    /// Creates a string property with optional constraints.
    /// </summary>
    /// <param name="description">The property description.</param>
    /// <param name="minLength">Minimum string length.</param>
    /// <param name="maxLength">Maximum string length.</param>
    /// <param name="pattern">Regex pattern for validation.</param>
    /// <param name="defaultValue">Default value.</param>
    /// <param name="enumValues">Allowed enum values.</param>
    /// <returns>A JsonElement representing the property schema.</returns>
    public static JsonElement StringProperty(
        string description,
        int? minLength = null,
        int? maxLength = null,
        string? pattern = null,
        string? defaultValue = null,
        string[]? enumValues = null)
    {
        var prop = new Dictionary<string, object>
        {
            ["type"] = "string",
            ["description"] = description
        };

        if (minLength.HasValue)
        {
            prop["minLength"] = minLength.Value;
        }

        if (maxLength.HasValue)
        {
            prop["maxLength"] = maxLength.Value;
        }

        if (pattern != null)
        {
            prop["pattern"] = pattern;
        }

        if (defaultValue != null)
        {
            prop["default"] = defaultValue;
        }

        if (enumValues != null)
        {
            prop["enum"] = enumValues;
        }

        return JsonSerializer.SerializeToElement(prop);
    }

    /// <summary>
    /// Creates an integer property with optional constraints.
    /// </summary>
    /// <param name="description">The property description.</param>
    /// <param name="minimum">Minimum value.</param>
    /// <param name="maximum">Maximum value.</param>
    /// <param name="defaultValue">Default value.</param>
    /// <returns>A JsonElement representing the property schema.</returns>
    public static JsonElement IntegerProperty(
        string description,
        int? minimum = null,
        int? maximum = null,
        int? defaultValue = null)
    {
        var prop = new Dictionary<string, object>
        {
            ["type"] = "integer",
            ["description"] = description
        };

        if (minimum.HasValue)
        {
            prop["minimum"] = minimum.Value;
        }

        if (maximum.HasValue)
        {
            prop["maximum"] = maximum.Value;
        }

        if (defaultValue.HasValue)
        {
            prop["default"] = defaultValue.Value;
        }

        return JsonSerializer.SerializeToElement(prop);
    }

    /// <summary>
    /// Creates a boolean property.
    /// </summary>
    /// <param name="description">The property description.</param>
    /// <param name="defaultValue">Default value.</param>
    /// <returns>A JsonElement representing the property schema.</returns>
    public static JsonElement BooleanProperty(
        string description,
        bool? defaultValue = null)
    {
        var prop = new Dictionary<string, object>
        {
            ["type"] = "boolean",
            ["description"] = description
        };

        if (defaultValue.HasValue)
        {
            prop["default"] = defaultValue.Value;
        }

        return JsonSerializer.SerializeToElement(prop);
    }

    /// <summary>
    /// Creates an array property.
    /// </summary>
    /// <param name="description">The property description.</param>
    /// <param name="itemSchema">Schema for array items.</param>
    /// <param name="minItems">Minimum number of items.</param>
    /// <param name="maxItems">Maximum number of items.</param>
    /// <returns>A JsonElement representing the property schema.</returns>
    public static JsonElement ArrayProperty(
        string description,
        JsonElement itemSchema,
        int? minItems = null,
        int? maxItems = null)
    {
        var prop = new Dictionary<string, object>
        {
            ["type"] = "array",
            ["description"] = description,
            ["items"] = itemSchema
        };

        if (minItems.HasValue)
        {
            prop["minItems"] = minItems.Value;
        }

        if (maxItems.HasValue)
        {
            prop["maxItems"] = maxItems.Value;
        }

        return JsonSerializer.SerializeToElement(prop);
    }
}
