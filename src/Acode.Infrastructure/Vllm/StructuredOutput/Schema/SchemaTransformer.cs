namespace Acode.Infrastructure.Vllm.StructuredOutput.Schema;

using System.Text.Json;
using Acode.Infrastructure.Vllm.StructuredOutput.Exceptions;

/// <summary>
/// Transforms tool schemas to vLLM's expected format.
/// Handles $ref resolution, depth limits, and size limits.
/// </summary>
/// <remarks>
/// FR-007e: Schema transformation for vLLM guided decoding.
/// </remarks>
public sealed class SchemaTransformer
{
    private readonly int maxDepth;
    private readonly int maxSize;
    private readonly int timeoutMs;

    /// <summary>
    /// Initializes a new instance of the <see cref="SchemaTransformer"/> class.
    /// </summary>
    /// <param name="maxDepth">Maximum schema nesting depth.</param>
    /// <param name="maxSize">Maximum schema size in bytes.</param>
    /// <param name="timeoutMs">Processing timeout in milliseconds.</param>
    public SchemaTransformer(
        int maxDepth = 10,
        int maxSize = 65536,
        int timeoutMs = 100)
    {
        this.maxDepth = maxDepth;
        this.maxSize = maxSize;
        this.timeoutMs = timeoutMs;
    }

    /// <summary>
    /// Transforms a schema to vLLM format.
    /// </summary>
    /// <param name="schema">The original JSON Schema.</param>
    /// <returns>The transformed schema.</returns>
    /// <exception cref="SchemaTooComplexException">Thrown when schema exceeds limits.</exception>
    public JsonElement Transform(JsonElement schema)
    {
        // Check size limit
        var schemaJson = schema.GetRawText();
        if (schemaJson.Length > this.maxSize)
        {
            throw new SchemaTooComplexException(
                $"Schema exceeds size limit ({schemaJson.Length} > {this.maxSize} bytes)",
                "ACODE-VLM-SO-001")
            {
                ActualSize = schemaJson.Length,
                MaxSize = this.maxSize,
            };
        }

        using var cts = new CancellationTokenSource(this.timeoutMs);

        try
        {
            // Resolve $refs and transform
            var resolved = this.ResolveRefs(schema, schema, new HashSet<string>());

            // Check depth limit
            var (depth, deepestPath) = CalculateDepthWithPath(resolved);
            if (depth > this.maxDepth)
            {
                throw new SchemaTooComplexException(
                    $"Schema exceeds depth limit ({depth} > {this.maxDepth} levels)",
                    "ACODE-VLM-SO-001")
                {
                    ActualDepth = depth,
                    MaxDepth = this.maxDepth,
                    DeepestPath = deepestPath,
                };
            }

            return resolved;
        }
        catch (OperationCanceledException)
        {
            throw new SchemaTooComplexException(
                $"Schema processing timeout ({this.timeoutMs}ms exceeded)",
                "ACODE-VLM-SO-003");
        }
    }

    /// <summary>
    /// Validates a schema without transforming it.
    /// </summary>
    /// <param name="schema">The schema to validate.</param>
    /// <returns>A validation result.</returns>
    public SchemaValidationResult Validate(JsonElement schema)
    {
        var errors = new List<string>();
        var warnings = new List<string>();

        var schemaJson = schema.GetRawText();

        // Check size
        if (schemaJson.Length > this.maxSize)
        {
            errors.Add($"Schema exceeds size limit ({schemaJson.Length} > {this.maxSize} bytes)");
        }

        // Check depth
        var (depth, deepestPath) = CalculateDepthWithPath(schema);
        if (depth > this.maxDepth)
        {
            errors.Add($"Schema exceeds depth limit ({depth} > {this.maxDepth} levels) at path: {deepestPath}");
        }

        // Check for unsupported constructs
        if (ContainsRef(schema))
        {
            warnings.Add("Schema contains $ref which will be resolved during transformation");
        }

        return new SchemaValidationResult
        {
            IsValid = errors.Count == 0,
            Errors = errors,
            Warnings = warnings,
            Depth = depth,
            SizeBytes = schemaJson.Length,
        };
    }

    private static JsonElement ResolveRefPath(JsonElement root, string refPath)
    {
        // Parse path like "#/$defs/User"
        var parts = refPath.TrimStart('#', '/').Split('/');
        var current = root;

        foreach (var part in parts)
        {
            if (!current.TryGetProperty(part, out current))
            {
                throw new SchemaTooComplexException(
                    $"Cannot resolve $ref: {refPath}",
                    "ACODE-VLM-SO-002");
            }
        }

        return current;
    }

    private static (int Depth, string Path) CalculateDepthWithPath(JsonElement element, int currentDepth = 0, string currentPath = "")
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            return (currentDepth, currentPath);
        }

        var maxChildDepth = currentDepth;
        var deepestPath = currentPath;

        if (element.TryGetProperty("properties", out var props))
        {
            foreach (var prop in props.EnumerateObject())
            {
                var propPath = string.IsNullOrEmpty(currentPath) ? prop.Name : $"{currentPath}.{prop.Name}";
                var (childDepth, childPath) = CalculateDepthWithPath(prop.Value, currentDepth + 1, propPath);
                if (childDepth > maxChildDepth)
                {
                    maxChildDepth = childDepth;
                    deepestPath = childPath;
                }
            }
        }

        if (element.TryGetProperty("items", out var items))
        {
            var itemPath = string.IsNullOrEmpty(currentPath) ? "[*]" : $"{currentPath}[*]";
            var (itemDepth, itemChildPath) = CalculateDepthWithPath(items, currentDepth + 1, itemPath);
            if (itemDepth > maxChildDepth)
            {
                maxChildDepth = itemDepth;
                deepestPath = itemChildPath;
            }
        }

        return (maxChildDepth, deepestPath);
    }

    private static bool ContainsRef(JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            return false;
        }

        if (element.TryGetProperty("$ref", out _))
        {
            return true;
        }

        foreach (var prop in element.EnumerateObject())
        {
            if (ContainsRef(prop.Value))
            {
                return true;
            }
        }

        return false;
    }

    private JsonElement ResolveRefs(
        JsonElement element,
        JsonElement rootSchema,
        HashSet<string> visited)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            return element;
        }

        // Check for $ref
        if (element.TryGetProperty("$ref", out var refProp))
        {
            var refPath = refProp.GetString();
            if (refPath is null || !refPath.StartsWith("#/", StringComparison.Ordinal))
            {
                throw new SchemaTooComplexException(
                    $"Only local $ref supported: {refPath}",
                    "ACODE-VLM-SO-002");
            }

            if (visited.Contains(refPath))
            {
                throw new SchemaTooComplexException(
                    $"Circular $ref detected: {refPath}",
                    "ACODE-VLM-SO-002");
            }

            visited.Add(refPath);
            var resolved = ResolveRefPath(rootSchema, refPath);
            return this.ResolveRefs(resolved, rootSchema, visited);
        }

        // Transform properties recursively
        using var stream = new MemoryStream();
        using var writer = new Utf8JsonWriter(stream);

        writer.WriteStartObject();

        foreach (var prop in element.EnumerateObject())
        {
            // Skip $defs and definitions - they're only used for references
            if (prop.Name == "$defs" || prop.Name == "definitions")
            {
                continue;
            }

            writer.WritePropertyName(prop.Name);

            if (prop.Name == "properties" && prop.Value.ValueKind == JsonValueKind.Object)
            {
                // Transform nested property schemas
                writer.WriteStartObject();
                foreach (var propSchema in prop.Value.EnumerateObject())
                {
                    writer.WritePropertyName(propSchema.Name);
                    var transformed = this.ResolveRefs(propSchema.Value, rootSchema, new HashSet<string>(visited));
                    transformed.WriteTo(writer);
                }

                writer.WriteEndObject();
            }
            else if (prop.Name == "items" && prop.Value.ValueKind == JsonValueKind.Object)
            {
                // Transform array item schema
                var transformed = this.ResolveRefs(prop.Value, rootSchema, new HashSet<string>(visited));
                transformed.WriteTo(writer);
            }
            else
            {
                prop.Value.WriteTo(writer);
            }
        }

        writer.WriteEndObject();
        writer.Flush();

        return JsonDocument.Parse(stream.ToArray()).RootElement.Clone();
    }
}
