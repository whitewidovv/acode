using System.Text.Json;
using Acode.Domain.Validation;

namespace Acode.Infrastructure.Network;

/// <summary>
/// Provides loadable denylist for endpoint validation.
/// </summary>
/// <remarks>
/// Per Task 001.b FR-001b-36 to FR-001b-37:
/// Loads denylist patterns from JSON file with fallback to built-in patterns.
/// </remarks>
public class DenylistProvider
{
    /// <summary>
    /// Loads denylist patterns from a JSON file.
    /// Falls back to built-in denylist if file not found or invalid.
    /// </summary>
    /// <param name="filePath">Path to denylist JSON file.</param>
    /// <returns>Read-only list of endpoint patterns.</returns>
    public IReadOnlyList<EndpointPattern> LoadFromFile(string filePath)
    {
        try
        {
            if (!File.Exists(filePath))
            {
                return GetBuiltInDenylist();
            }

            var json = File.ReadAllText(filePath);
            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            var denylist = JsonSerializer.Deserialize<DenylistJson>(json, jsonOptions);

            if (denylist?.Patterns == null || denylist.Patterns.Count == 0)
            {
                return GetBuiltInDenylist();
            }

            var patterns = new List<EndpointPattern>();
            foreach (var entry in denylist.Patterns)
            {
                if (!TryParsePatternType(entry.Type, out var patternType))
                {
                    // Skip invalid pattern types
                    continue;
                }

                patterns.Add(new EndpointPattern
                {
                    Pattern = entry.Pattern,
                    Type = patternType,
                    Description = entry.Description
                });
            }

            return patterns.AsReadOnly();
        }
        catch (JsonException)
        {
            // Invalid JSON - fall back to built-in
            return GetBuiltInDenylist();
        }
        catch (IOException)
        {
            // File access error - fall back to built-in
            return GetBuiltInDenylist();
        }
        catch (UnauthorizedAccessException)
        {
            // Permission denied - fall back to built-in
            return GetBuiltInDenylist();
        }
    }

    /// <summary>
    /// Gets the built-in denylist patterns.
    /// Used as fallback when file loading fails.
    /// </summary>
    /// <returns>Read-only list of built-in endpoint patterns.</returns>
    public IReadOnlyList<EndpointPattern> GetBuiltInDenylist()
    {
        var patterns = new EndpointPattern[]
        {
            new() { Pattern = "api.openai.com", Type = PatternType.Exact, Description = "OpenAI API" },
            new() { Pattern = "*.openai.com", Type = PatternType.Wildcard, Description = "OpenAI subdomains" },
            new() { Pattern = "api.anthropic.com", Type = PatternType.Exact, Description = "Anthropic API" },
            new() { Pattern = "*.anthropic.com", Type = PatternType.Wildcard, Description = "Anthropic subdomains" },
            new() { Pattern = @".*\.openai\.azure\.com", Type = PatternType.Regex, Description = "Azure OpenAI endpoints" },
            new() { Pattern = "generativelanguage.googleapis.com", Type = PatternType.Exact, Description = "Google AI API" },
            new() { Pattern = @"bedrock.*\.amazonaws\.com", Type = PatternType.Regex, Description = "AWS Bedrock" },
            new() { Pattern = "api.cohere.ai", Type = PatternType.Exact, Description = "Cohere API" },
            new() { Pattern = "api-inference.huggingface.co", Type = PatternType.Exact, Description = "Hugging Face Inference" },
            new() { Pattern = "api.together.xyz", Type = PatternType.Exact, Description = "Together.ai" },
            new() { Pattern = "api.replicate.com", Type = PatternType.Exact, Description = "Replicate" }
        };

        return patterns.ToList().AsReadOnly();
    }

    private static bool TryParsePatternType(string typeString, out PatternType patternType)
    {
        switch (typeString?.ToLowerInvariant())
        {
            case "exact":
                patternType = PatternType.Exact;
                return true;
            case "wildcard":
                patternType = PatternType.Wildcard;
                return true;
            case "regex":
                patternType = PatternType.Regex;
                return true;
            default:
                patternType = default;
                return false;
        }
    }

    private class DenylistJson
    {
        public required string Version { get; init; }

        public required string Updated { get; init; }

        public required List<PatternEntry> Patterns { get; init; }
    }

    private class PatternEntry
    {
        public required string Pattern { get; init; }

        public required string Type { get; init; }

        public required string Description { get; init; }
    }
}
