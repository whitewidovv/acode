using System.Collections.Frozen;

namespace Acode.Domain.Validation;

/// <summary>
/// Denylist of external LLM API endpoints.
/// Enforces HC-01: No external LLM APIs in LocalOnly/Airgapped modes.
/// </summary>
/// <remarks>
/// This list is immutable and cannot be bypassed. Per Task 001.b.
/// </remarks>
public static class LlmApiDenylist
{
    private static readonly FrozenSet<string> _deniedHosts;

    static LlmApiDenylist()
    {
        _deniedHosts = new[]
        {
            // OpenAI
            "api.openai.com",
            "openai.azure.com",

            // Anthropic
            "api.anthropic.com",

            // Google AI
            "generativelanguage.googleapis.com",
            "ai.googleapis.com",

            // Cohere
            "api.cohere.ai",
            "api.cohere.com",

            // AI21 Labs
            "api.ai21.com",

            // Hugging Face
            "api-inference.huggingface.co",

            // Together.ai
            "api.together.xyz",

            // Replicate
            "api.replicate.com",

            // AWS Bedrock (common endpoints)
            "bedrock-runtime.us-east-1.amazonaws.com",
            "bedrock-runtime.us-west-2.amazonaws.com",
            "bedrock-runtime.eu-west-1.amazonaws.com",

            // Azure OpenAI (pattern matching)
            // Note: Azure OpenAI uses *.openai.azure.com which is covered above
        }.ToFrozenSet(StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Check if a URI is denied.
    /// </summary>
    /// <param name="uri">URI to check.</param>
    /// <returns>True if denied, false otherwise.</returns>
    public static bool IsDenied(Uri uri)
    {
        ArgumentNullException.ThrowIfNull(uri);

        var host = uri.Host.ToLowerInvariant();

        // Exact match
        if (_deniedHosts.Contains(host))
        {
            return true;
        }

        // Subdomain match (e.g., xxx.openai.azure.com)
        foreach (var deniedHost in _deniedHosts)
        {
            if (host.EndsWith("." + deniedHost, StringComparison.OrdinalIgnoreCase) ||
                host.Equals(deniedHost, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Get all denied hosts for documentation/debugging.
    /// </summary>
    /// <returns>Immutable set of denied hosts.</returns>
    public static IReadOnlySet<string> GetDeniedHosts()
    {
        return _deniedHosts;
    }
}
