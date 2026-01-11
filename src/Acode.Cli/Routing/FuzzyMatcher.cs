namespace Acode.Cli.Routing;

/// <summary>
/// Provides fuzzy string matching for command suggestions.
/// </summary>
/// <remarks>
/// Uses Levenshtein distance algorithm to find similar commands
/// when the user types an unknown command name.
/// </remarks>
public sealed class FuzzyMatcher
{
    private readonly double _threshold;
    private readonly int _maxResults;

    /// <summary>
    /// Initializes a new instance of the <see cref="FuzzyMatcher"/> class.
    /// </summary>
    /// <param name="threshold">Minimum similarity threshold (0.0-1.0). Default is 0.6.</param>
    /// <param name="maxResults">Maximum number of suggestions to return. Default is 3.</param>
    public FuzzyMatcher(double threshold = 0.6, int maxResults = 3)
    {
        if (threshold < 0 || threshold > 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(threshold),
                "Threshold must be between 0.0 and 1.0."
            );
        }

        if (maxResults < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maxResults),
                "MaxResults must be non-negative."
            );
        }

        _threshold = threshold;
        _maxResults = maxResults;
    }

    /// <summary>
    /// Calculates the Levenshtein distance between two strings.
    /// </summary>
    /// <param name="source">First string.</param>
    /// <param name="target">Second string.</param>
    /// <returns>Number of single-character edits (insertions, deletions, substitutions).</returns>
    public static int LevenshteinDistance(string source, string target)
    {
        if (string.IsNullOrEmpty(source))
        {
            return target?.Length ?? 0;
        }

        if (string.IsNullOrEmpty(target))
        {
            return source.Length;
        }

        var distance = new int[source.Length + 1, target.Length + 1];

        for (int i = 0; i <= source.Length; i++)
        {
            distance[i, 0] = i;
        }

        for (int j = 0; j <= target.Length; j++)
        {
            distance[0, j] = j;
        }

        for (int i = 1; i <= source.Length; i++)
        {
            for (int j = 1; j <= target.Length; j++)
            {
                var cost = source[i - 1] == target[j - 1] ? 0 : 1;
                distance[i, j] = Math.Min(
                    Math.Min(distance[i - 1, j] + 1, distance[i, j - 1] + 1),
                    distance[i - 1, j - 1] + cost
                );
            }
        }

        return distance[source.Length, target.Length];
    }

    /// <summary>
    /// Finds candidates similar to the input string.
    /// </summary>
    /// <param name="input">The input string to match.</param>
    /// <param name="candidates">Collection of candidate strings to search.</param>
    /// <param name="maxResults">Optional override for maximum results.</param>
    /// <param name="threshold">Optional override for similarity threshold.</param>
    /// <returns>A list of similar strings, ordered by similarity (best first).</returns>
    public IReadOnlyList<string> FindSimilar(
        string input,
        IEnumerable<string> candidates,
        int? maxResults = null,
        double? threshold = null
    )
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(candidates);

        var effectiveMaxResults = maxResults ?? _maxResults;
        var effectiveThreshold = threshold ?? _threshold;

        if (effectiveMaxResults <= 0)
        {
            return Array.Empty<string>();
        }

        var inputLower = input.ToLowerInvariant();

        return candidates
            .Select(c => new
            {
                Candidate = c,
                Similarity = CalculateSimilarity(inputLower, c.ToLowerInvariant()),
            })
            .Where(x => x.Similarity >= effectiveThreshold)
            .OrderByDescending(x => x.Similarity)
            .Take(effectiveMaxResults)
            .Select(x => x.Candidate)
            .ToList();
    }

    /// <summary>
    /// Calculates the similarity between two strings.
    /// </summary>
    /// <param name="a">First string.</param>
    /// <param name="b">Second string.</param>
    /// <returns>Similarity score from 0.0 (completely different) to 1.0 (identical).</returns>
    /// <remarks>
    /// Uses Levenshtein distance normalized by the maximum string length.
    /// </remarks>
    public double CalculateSimilarity(string a, string b)
    {
        if (string.IsNullOrEmpty(a) && string.IsNullOrEmpty(b))
        {
            return 1.0;
        }

        if (string.IsNullOrEmpty(a) || string.IsNullOrEmpty(b))
        {
            return 0.0;
        }

        var distance = LevenshteinDistance(a, b);
        var maxLength = Math.Max(a.Length, b.Length);

        return 1.0 - ((double)distance / maxLength);
    }
}
