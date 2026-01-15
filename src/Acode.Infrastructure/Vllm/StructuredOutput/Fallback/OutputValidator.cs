namespace Acode.Infrastructure.Vllm.StructuredOutput.Fallback;

using System.Text.Json;
using NJsonSchema;

/// <summary>
/// Validates LLM output against JSON schemas.
/// </summary>
/// <remarks>
/// FR-050 through FR-061: Output validation with schema-based checking.
/// </remarks>
public sealed class OutputValidator
{
    /// <summary>
    /// Validates output against a JSON schema.
    /// </summary>
    /// <param name="output">The output string to validate.</param>
    /// <param name="schemaJson">The JSON schema as a string.</param>
    /// <returns>A validation result indicating whether output is valid.</returns>
    public OutputValidationResult Validate(string output, string schemaJson)
    {
        if (string.IsNullOrWhiteSpace(output) || string.IsNullOrWhiteSpace(schemaJson))
        {
            return new OutputValidationResult
            {
                IsValid = false,
                Errors = new[] { "Output and schema must not be empty" },
            };
        }

        try
        {
            // Parse the output as JSON
            var outputElement = JsonDocument.Parse(output).RootElement;

            // Parse and load schema
            var schema = JsonSchema.FromJsonAsync(schemaJson).ConfigureAwait(false).GetAwaiter().GetResult();

            // Validate output against schema
            var errors = schema.Validate(outputElement.GetRawText());

            if (errors == null || errors.Count == 0)
            {
                return new OutputValidationResult { IsValid = true };
            }

            return new OutputValidationResult
            {
                IsValid = false,
                Errors = errors.Select(e => e.ToString()).ToArray(),
            };
        }
        catch (JsonException ex)
        {
            return new OutputValidationResult
            {
                IsValid = false,
                Errors = new[] { $"Invalid JSON output: {ex.Message}" },
            };
        }
        catch (Exception ex)
        {
            return new OutputValidationResult
            {
                IsValid = false,
                Errors = new[] { $"Schema validation error: {ex.Message}" },
            };
        }
    }

    /// <summary>
    /// Attempts to extract and parse JSON from potentially malformed output.
    /// </summary>
    /// <param name="output">The potentially invalid output.</param>
    /// <returns>Extracted valid JSON, or null if extraction failed.</returns>
    /// <remarks>
    /// Heuristics:
    /// - Try direct parse first.
    /// - Extract JSON-like blocks if present.
    /// - Clean common formatting issues.
    /// </remarks>
    public string? TryExtractValidJson(string output)
    {
        if (string.IsNullOrWhiteSpace(output))
        {
            return null;
        }

        // Try direct parse first
        try
        {
            JsonDocument.Parse(output);
            return output;
        }
        catch
        {
            // Continue to extraction attempts
        }

        // Try to find and extract JSON block
        var trimmed = output.Trim();

        // Look for {...} or [...]
        var openBrace = trimmed.IndexOf('{', StringComparison.Ordinal);
        var openBracket = trimmed.IndexOf('[', StringComparison.Ordinal);
        var startIndex = -1;

        if (openBrace >= 0 && (openBracket < 0 || openBrace < openBracket))
        {
            startIndex = openBrace;
        }
        else if (openBracket >= 0)
        {
            startIndex = openBracket;
        }

        if (startIndex < 0)
        {
            return null;
        }

        // Find corresponding closing brace/bracket
        var closingIndex = this.FindClosingBracket(trimmed, startIndex);
        if (closingIndex < 0)
        {
            return null;
        }

        var extracted = trimmed.Substring(startIndex, closingIndex - startIndex + 1);

        // Verify it's valid JSON
        try
        {
            JsonDocument.Parse(extracted);
            return extracted;
        }
        catch
        {
            return null;
        }
    }

    private int FindClosingBracket(string text, int openIndex)
    {
        var openChar = text[openIndex];
        var closeChar = openChar == '{' ? '}' : ']';
        var depth = 0;
        var inString = false;
        var escaped = false;

        for (int i = openIndex; i < text.Length; i++)
        {
            var c = text[i];

            if (escaped)
            {
                escaped = false;
                continue;
            }

            if (c == '\\')
            {
                escaped = true;
                continue;
            }

            if (c == '"' && !escaped)
            {
                inString = !inString;
                continue;
            }

            if (inString)
            {
                continue;
            }

            if (c == openChar)
            {
                depth++;
            }
            else if (c == closeChar)
            {
                depth--;
                if (depth == 0)
                {
                    return i;
                }
            }
        }

        return -1;
    }
}

/// <summary>
/// Result of output validation operation.
/// </summary>
public sealed class OutputValidationResult
{
    /// <summary>
    /// Gets or sets a value indicating whether the output is valid.
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// Gets or sets validation error messages.
    /// </summary>
    public string[] Errors { get; set; } = Array.Empty<string>();
}
