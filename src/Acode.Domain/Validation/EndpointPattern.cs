using System.Text.RegularExpressions;

namespace Acode.Domain.Validation;

/// <summary>
/// Pattern for matching endpoints in denylist or allowlist.
/// Supports exact matching, wildcard subdomain matching, and regex matching.
/// </summary>
/// <remarks>
/// Per Task 001.b FR-001b-13, FR-001b-34, FR-001b-35:
/// Patterns can be exact (api.openai.com), wildcard (*.openai.com),
/// or regex (bedrock.*\.amazonaws\.com) for flexible endpoint matching.
/// Regex patterns are pre-compiled for performance (NFR-001b-23).
/// </remarks>
public record EndpointPattern
{
    [System.Runtime.CompilerServices.CompilerGenerated]
    private readonly Lazy<Regex?> _compiledRegex;

    /// <summary>
    /// Initializes a new instance of the <see cref="EndpointPattern"/> class.
    /// </summary>
    /// <remarks>
    /// Pre-compiles regex patterns if Type is Regex for performance (NFR-001b-23).
    /// Validates regex patterns eagerly for fail-fast behavior.
    /// </remarks>
    public EndpointPattern()
    {
        // Lazy compilation ensures regex is compiled on first use.
        // Invalid patterns will throw ArgumentException when first accessed.
        _compiledRegex = new Lazy<Regex?>(() =>
        {
            if (Type == PatternType.Regex)
            {
                ArgumentException.ThrowIfNullOrEmpty(Pattern);
                try
                {
                    return new Regex(
                        Pattern,
                        RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.CultureInvariant,
                        TimeSpan.FromMilliseconds(100));
                }
                catch (ArgumentException ex)
                {
                    throw new ArgumentException(
                        $"Invalid regex pattern '{Pattern}': {ex.Message}",
                        nameof(Pattern),
                        ex);
                }
            }

            return null;
        });
    }

    /// <summary>
    /// Gets the pattern string to match against.
    /// </summary>
    public required string Pattern { get; init; }

    /// <summary>
    /// Gets the type of pattern matching to use.
    /// </summary>
    public required PatternType Type { get; init; }

    /// <summary>
    /// Gets an optional description of what this pattern matches.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Determines whether the specified object is equal to the current object.
    /// </summary>
    /// <param name="other">The object to compare with the current object.</param>
    /// <returns>True if objects are equal, false otherwise.</returns>
    /// <remarks>
    /// Excludes _compiledRegex from equality comparison as it's a cached compilation artifact.
    /// </remarks>
    public virtual bool Equals(EndpointPattern? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return Pattern == other.Pattern
            && Type == other.Type
            && Description == other.Description;
    }

    /// <summary>
    /// Returns the hash code for this instance.
    /// </summary>
    /// <returns>Hash code.</returns>
    public override int GetHashCode()
    {
        return HashCode.Combine(Pattern, Type, Description);
    }

    /// <summary>
    /// Checks if the given URI matches this pattern.
    /// </summary>
    /// <param name="uri">URI to check.</param>
    /// <returns>True if URI matches the pattern, false otherwise.</returns>
    /// <exception cref="ArgumentNullException">If uri is null.</exception>
    public bool Matches(Uri uri)
    {
        ArgumentNullException.ThrowIfNull(uri);

        return Type switch
        {
            PatternType.Exact => MatchExact(uri),
            PatternType.Wildcard => MatchWildcard(uri),
            PatternType.Regex => MatchRegex(uri),
            _ => false
        };
    }

    private bool MatchExact(Uri uri)
    {
        return uri.Host.Equals(Pattern, StringComparison.OrdinalIgnoreCase);
    }

    private bool MatchWildcard(Uri uri)
    {
        // Wildcard pattern: *.openai.com
        // Should match: api.openai.com, chat.openai.com
        // Should NOT match: openai.com (no subdomain)
        if (Pattern.StartsWith("*."))
        {
            var domain = Pattern[2..]; // Remove "*."

            // Check if host ends with the domain and has a subdomain prefix
            // e.g., "api.openai.com" ends with "openai.com" and has length > domain.Length
            // but "openai.com" does NOT have a subdomain (length == domain.Length)
            if (uri.Host.EndsWith(domain, StringComparison.OrdinalIgnoreCase)
                && uri.Host.Length > domain.Length)
            {
                return true;
            }

            return false;
        }

        // No wildcard, treat as exact match
        return uri.Host.Equals(Pattern, StringComparison.OrdinalIgnoreCase);
    }

    private bool MatchRegex(Uri uri)
    {
        var regex = _compiledRegex.Value;
        return regex?.IsMatch(uri.Host) ?? false;
    }
}
