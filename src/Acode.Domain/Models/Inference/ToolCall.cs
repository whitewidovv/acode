namespace Acode.Domain.Models.Inference;

using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

/// <summary>
/// Represents a tool call request from a language model.
/// </summary>
/// <remarks>
/// FR-004a-36: ToolCall MUST be a record type (immutable, value equality).
/// FR-004a-37 to FR-004a-39: Must have Id, Name, Arguments properties.
/// FR-004a-40 to FR-004a-45: Validation rules for each property.
/// FR-004a-47 to FR-004a-48: JSON serialization support.
/// FR-004a-49 to FR-004a-53: Helper methods for argument parsing.
/// </remarks>
[method: JsonConstructor]
public sealed record ToolCall(string Id, string Name, string Arguments)
{
    private static readonly Regex NamePattern = new Regex(@"^[a-zA-Z0-9_]+$", RegexOptions.Compiled);

    /// <summary>
    /// Gets the unique identifier for this tool call.
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; init; } = ValidateId(Id);

    /// <summary>
    /// Gets the name of the tool to invoke.
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; init; } = ValidateName(Name);

    /// <summary>
    /// Gets the JSON-encoded arguments for the tool call.
    /// </summary>
    [JsonPropertyName("arguments")]
    public string Arguments { get; init; } = ValidateArguments(Arguments).Trim();

    /// <summary>
    /// Attempts to retrieve a specific argument value by key.
    /// </summary>
    /// <typeparam name="T">The expected type of the argument value.</typeparam>
    /// <param name="key">The argument key to retrieve.</param>
    /// <param name="value">The parsed value if successful, default(T) otherwise.</param>
    /// <returns>True if the key exists and can be deserialized to T, false otherwise.</returns>
    /// <remarks>
    /// FR-004a-49: ToolCall MUST provide TryGetArgument method.
    /// FR-004a-50: Returns false if key not found.
    /// FR-004a-51: Returns false if type mismatch.
    /// </remarks>
    public bool TryGetArgument<T>(string key, out T? value)
    {
        try
        {
            using var doc = JsonDocument.Parse(this.Arguments);
            if (doc.RootElement.TryGetProperty(key, out var element))
            {
                value = JsonSerializer.Deserialize<T>(element.GetRawText());
                return value is not null;
            }

            value = default;
            return false;
        }
        catch
        {
            value = default;
            return false;
        }
    }

    /// <summary>
    /// Attempts to deserialize the entire Arguments string into a strongly-typed object.
    /// </summary>
    /// <typeparam name="T">The target type.</typeparam>
    /// <returns>The deserialized object, or null if deserialization fails.</returns>
    /// <remarks>
    /// FR-004a-52: ToolCall MUST provide GetArgumentsAs method.
    /// FR-004a-53: Returns null if deserialization fails.
    /// </remarks>
    public T? GetArgumentsAs<T>()
        where T : class
    {
        try
        {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
            };
            return JsonSerializer.Deserialize<T>(this.Arguments, options);
        }
        catch
        {
            return null;
        }
    }

    private static string ValidateId(string id)
    {
        // FR-004a-40: Id MUST be non-null and non-empty
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("ToolCall Id must be non-empty.", nameof(Id));
        }

        return id;
    }

    private static string ValidateName(string name)
    {
        // FR-004a-41: Name MUST be non-null and non-empty
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("ToolCall Name must be non-empty.", nameof(Name));
        }

        // FR-004a-43: Name MUST be <= 64 characters
        if (name.Length > 64)
        {
            throw new ArgumentException("ToolCall Name must be 64 characters or less.", nameof(Name));
        }

        // FR-004a-44: Name MUST be alphanumeric + underscore only
        if (!NamePattern.IsMatch(name))
        {
            throw new ArgumentException("ToolCall Name must contain only alphanumeric characters and underscores.", nameof(Name));
        }

        return name;
    }

    private static string ValidateArguments(string arguments)
    {
        // FR-004a-42: Arguments MUST be non-null and non-empty
        if (string.IsNullOrWhiteSpace(arguments))
        {
            throw new ArgumentException("ToolCall Arguments must be non-empty.", nameof(Arguments));
        }

        // FR-004a-45: Arguments MUST be valid JSON
        try
        {
            using var doc = JsonDocument.Parse(arguments);

            // Ensure it's valid JSON by accessing root
            _ = doc.RootElement;
        }
        catch (JsonException ex)
        {
            throw new ArgumentException("ToolCall Arguments must be valid JSON.", nameof(Arguments), ex);
        }

        return arguments;
    }
}
