namespace Acode.Infrastructure.Vllm.StructuredOutput.ResponseFormat;

using System.Text.Json;

/// <summary>
/// Builds guided decoding parameters for vLLM structured output requests.
/// </summary>
/// <remarks>
/// FR-030 through FR-036: Guided decoding parameter construction.
/// Supports guided_json, guided_choice, and guided_regex modes.
/// </remarks>
public sealed class GuidedDecodingBuilder
{
    /// <summary>
    /// Builds a guided_json parameter for JSON-constrained generation.
    /// </summary>
    /// <param name="schema">The JSON schema to use for guided decoding.</param>
    /// <returns>A guided JSON parameter object with schema information.</returns>
    /// <exception cref="ArgumentNullException">Thrown when schema is null.</exception>
    public GuidedJsonParameter BuildGuidedJson(JsonElement schema)
    {
        if (schema.ValueKind == JsonValueKind.Undefined)
        {
            throw new ArgumentNullException(nameof(schema), "Schema cannot be undefined");
        }

        return new GuidedJsonParameter
        {
            Type = "json_schema",
            Schema = schema.GetRawText(),
        };
    }

    /// <summary>
    /// Builds a guided_choice parameter for constrained choice generation.
    /// </summary>
    /// <param name="choices">Array of valid choice values (e.g., enum values).</param>
    /// <returns>A guided choice parameter object with choice constraints.</returns>
    /// <exception cref="ArgumentException">Thrown when choices is empty or null.</exception>
    public GuidedChoiceParameter BuildGuidedChoice(string[] choices)
    {
        if (choices == null || choices.Length == 0)
        {
            throw new ArgumentException("Choices must not be empty", nameof(choices));
        }

        return new GuidedChoiceParameter
        {
            Type = "choice",
            Choices = choices,
        };
    }

    /// <summary>
    /// Builds a guided_regex parameter for regex-constrained generation.
    /// </summary>
    /// <param name="pattern">The regular expression pattern to constrain output.</param>
    /// <returns>A guided regex parameter object with pattern information.</returns>
    /// <exception cref="ArgumentException">Thrown when pattern is empty or null.</exception>
    public GuidedRegexParameter BuildGuidedRegex(string pattern)
    {
        if (string.IsNullOrWhiteSpace(pattern))
        {
            throw new ArgumentException("Pattern must not be empty", nameof(pattern));
        }

        return new GuidedRegexParameter
        {
            Type = "regex",
            Pattern = pattern,
        };
    }

    /// <summary>
    /// Determines the appropriate guided decoding parameter based on schema characteristics.
    /// </summary>
    /// <param name="schema">The JSON schema to analyze.</param>
    /// <returns>A guided decoding parameter appropriate for the schema type.</returns>
    /// <remarks>
    /// Logic:
    /// - If schema contains enum constraint, returns guided_choice.
    /// - If schema contains pattern constraint, returns guided_regex.
    /// - Otherwise returns guided_json.
    /// </remarks>
    public object SelectGuidedParameter(JsonElement schema)
    {
        if (schema.ValueKind != JsonValueKind.Object)
        {
            return this.BuildGuidedJson(schema);
        }

        // Check for enum constraint (choice)
        if (schema.TryGetProperty("enum", out var enumProperty) && enumProperty.ValueKind == JsonValueKind.Array)
        {
            var choices = enumProperty.EnumerateArray()
                .Where(item => item.ValueKind == JsonValueKind.String)
                .Select(item => item.GetString() ?? string.Empty)
                .ToList();

            if (choices.Count > 0)
            {
                return this.BuildGuidedChoice(choices.ToArray());
            }
        }

        // Check for pattern constraint (regex)
        if (schema.TryGetProperty("pattern", out var patternProperty) && patternProperty.ValueKind == JsonValueKind.String)
        {
            var pattern = patternProperty.GetString();
            if (!string.IsNullOrWhiteSpace(pattern))
            {
                return this.BuildGuidedRegex(pattern);
            }
        }

        // Default to guided_json
        return this.BuildGuidedJson(schema);
    }
}

/// <summary>
/// Represents a guided JSON parameter for vLLM.
/// </summary>
public sealed class GuidedJsonParameter
{
    /// <summary>
    /// Gets or sets the type (always "json_schema").
    /// </summary>
    public string Type { get; set; } = "json_schema";

    /// <summary>
    /// Gets or sets the JSON schema as a string.
    /// </summary>
    public string Schema { get; set; } = string.Empty;
}

/// <summary>
/// Represents a guided choice parameter for vLLM.
/// </summary>
public sealed class GuidedChoiceParameter
{
    /// <summary>
    /// Gets or sets the type (always "choice").
    /// </summary>
    public string Type { get; set; } = "choice";

    /// <summary>
    /// Gets or sets the valid choice values.
    /// </summary>
    public string[] Choices { get; set; } = Array.Empty<string>();
}

/// <summary>
/// Represents a guided regex parameter for vLLM.
/// </summary>
public sealed class GuidedRegexParameter
{
    /// <summary>
    /// Gets or sets the type (always "regex").
    /// </summary>
    public string Type { get; set; } = "regex";

    /// <summary>
    /// Gets or sets the regular expression pattern.
    /// </summary>
    public string Pattern { get; set; } = string.Empty;
}
