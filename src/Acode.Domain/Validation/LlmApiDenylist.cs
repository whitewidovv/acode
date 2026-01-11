using System.Collections.Frozen;

namespace Acode.Domain.Validation;

/// <summary>
/// Denylist of external LLM API endpoints using pattern matching.
/// Enforces HC-01: No external LLM APIs in LocalOnly/Airgapped modes.
/// </summary>
/// <remarks>
/// This list is immutable and cannot be bypassed. Per Task 001.b.
/// Supports exact, wildcard, and regex patterns for comprehensive matching.
/// </remarks>
public static class LlmApiDenylist
{
    private static readonly FrozenSet<EndpointPattern> _deniedPatterns;

    static LlmApiDenylist()
    {
        _deniedPatterns = new EndpointPattern[]
        {
            // OpenAI - exact API endpoint
            new() { Pattern = "api.openai.com", Type = PatternType.Exact, Description = "OpenAI API" },

            // OpenAI - wildcard for all subdomains (chat, platform, beta, etc.)
            new() { Pattern = "*.openai.com", Type = PatternType.Wildcard, Description = "OpenAI subdomains" },

            // Azure OpenAI - regex pattern covers both root domain (openai.azure.com) and custom instances (*.openai.azure.com)
            new() { Pattern = @".*\.openai\.azure\.com", Type = PatternType.Regex, Description = "Azure OpenAI (root and custom instances)" },

            // Anthropic - exact API endpoint
            new() { Pattern = "api.anthropic.com", Type = PatternType.Exact, Description = "Anthropic API" },

            // Anthropic - wildcard for all subdomains
            new() { Pattern = "*.anthropic.com", Type = PatternType.Wildcard, Description = "Anthropic subdomains" },

            // Google AI - exact endpoints
            new() { Pattern = "generativelanguage.googleapis.com", Type = PatternType.Exact, Description = "Google Generative Language API" },
            new() { Pattern = "ai.googleapis.com", Type = PatternType.Exact, Description = "Google AI API" },

            // Cohere - exact endpoints
            new() { Pattern = "api.cohere.ai", Type = PatternType.Exact, Description = "Cohere API (.ai)" },
            new() { Pattern = "api.cohere.com", Type = PatternType.Exact, Description = "Cohere API (.com)" },

            // AI21 Labs - exact endpoint
            new() { Pattern = "api.ai21.com", Type = PatternType.Exact, Description = "AI21 Labs API" },

            // Hugging Face - exact endpoint
            new() { Pattern = "api-inference.huggingface.co", Type = PatternType.Exact, Description = "Hugging Face Inference API" },

            // Together.ai - exact endpoint
            new() { Pattern = "api.together.xyz", Type = PatternType.Exact, Description = "Together.ai API" },

            // Replicate - exact endpoint
            new() { Pattern = "api.replicate.com", Type = PatternType.Exact, Description = "Replicate API" },

            // AWS Bedrock - regex pattern for all regions
            new() { Pattern = @"bedrock.*\.amazonaws\.com", Type = PatternType.Regex, Description = "AWS Bedrock (all regions)" },
        }.ToFrozenSet();
    }

    /// <summary>
    /// Check if a URI is denied by any pattern in the denylist.
    /// </summary>
    /// <param name="uri">URI to check.</param>
    /// <returns>True if denied, false otherwise.</returns>
    public static bool IsDenied(Uri uri)
    {
        ArgumentNullException.ThrowIfNull(uri);

        // Check each pattern to see if URI matches
        return _deniedPatterns.Any(pattern => pattern.Matches(uri));
    }

    /// <summary>
    /// Get all denied patterns for documentation/debugging.
    /// </summary>
    /// <returns>Immutable set of endpoint patterns.</returns>
    public static IReadOnlySet<EndpointPattern> GetDeniedPatterns()
    {
        return _deniedPatterns;
    }
}
