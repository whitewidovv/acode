namespace Acode.Infrastructure.Vllm.StructuredOutput.Schema;

using System.Text.Json;

/// <summary>
/// Validates JSON schemas for size, depth, and reference constraints.
/// </summary>
/// <remarks>
/// FR-034: Schema validation with security checks.
/// Ensures schemas don't exceed limits and don't contain external references.
/// </remarks>
public sealed class SchemaValidator
{
    private readonly int _maxDepth;
    private readonly int _maxSize;

    /// <summary>
    /// Initializes a new instance of the <see cref="SchemaValidator"/> class.
    /// </summary>
    /// <param name="maxDepth">Maximum schema nesting depth.</param>
    /// <param name="maxSize">Maximum schema size in bytes.</param>
    public SchemaValidator(int maxDepth = 10, int maxSize = 65536)
    {
        this._maxDepth = maxDepth;
        this._maxSize = maxSize;
    }

    /// <summary>
    /// Validates a schema against size, depth, and reference constraints.
    /// </summary>
    /// <param name="schema">The schema to validate.</param>
    /// <returns>A validation result.</returns>
    public SchemaValidationResult Validate(JsonElement schema)
    {
        var errors = new List<string>();

        // Check size
        var schemaJson = schema.GetRawText();
        if (schemaJson.Length > this._maxSize)
        {
            errors.Add($"Schema exceeds maximum size ({schemaJson.Length} bytes > {this._maxSize} bytes)");
        }

        // Check depth
        var depth = this.CalculateDepth(schema);
        if (depth > this._maxDepth)
        {
            errors.Add($"Schema exceeds maximum depth ({depth} levels > {this._maxDepth} levels)");
        }

        // Check for external refs (security)
        if (this.HasExternalRefs(schema))
        {
            errors.Add("Schema contains external $ref references (only local #/... references allowed)");
        }

        return new SchemaValidationResult
        {
            IsValid = errors.Count == 0,
            Errors = errors,
        };
    }

    private int CalculateDepth(JsonElement element, int currentDepth = 0)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            return currentDepth;
        }

        var maxChildDepth = currentDepth;

        // Check properties
        if (element.TryGetProperty("properties", out var properties) && properties.ValueKind == JsonValueKind.Object)
        {
            foreach (var prop in properties.EnumerateObject())
            {
                var childDepth = this.CalculateDepth(prop.Value, currentDepth + 1);
                maxChildDepth = Math.Max(maxChildDepth, childDepth);
            }
        }

        // Check items (for arrays)
        if (element.TryGetProperty("items", out var items) && items.ValueKind == JsonValueKind.Object)
        {
            var childDepth = this.CalculateDepth(items, currentDepth + 1);
            maxChildDepth = Math.Max(maxChildDepth, childDepth);
        }

        // Check additionalProperties
        if (element.TryGetProperty("additionalProperties", out var additionalProps) && additionalProps.ValueKind == JsonValueKind.Object)
        {
            var childDepth = this.CalculateDepth(additionalProps, currentDepth + 1);
            maxChildDepth = Math.Max(maxChildDepth, childDepth);
        }

        return maxChildDepth;
    }

    private bool HasExternalRefs(JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            return false;
        }

        // Check this level's $ref
        if (element.TryGetProperty("$ref", out var refValue) && refValue.ValueKind == JsonValueKind.String)
        {
            var refString = refValue.GetString() ?? string.Empty;

            // Only local refs starting with # are allowed
            if (!refString.StartsWith("#/", StringComparison.Ordinal) && !string.IsNullOrEmpty(refString))
            {
                return true;
            }
        }

        // Check nested elements - return true if any property has external refs
        return element.EnumerateObject()
            .Any(prop => this.HasExternalRefs(prop.Value));
    }
}
