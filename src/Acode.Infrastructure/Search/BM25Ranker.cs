using Acode.Domain.Configuration;
using Acode.Domain.Search;

namespace Acode.Infrastructure.Search;

/// <summary>
/// Implements BM25 ranking algorithm with recency boost for search results.
/// </summary>
public sealed class BM25Ranker
{
    private const double K1 = 1.2; // Term frequency saturation parameter
    private const double B = 0.75; // Length normalization parameter
    private const double AvgDocLength = 500; // Average document length estimate

    private readonly SearchSettings _settings;

    /// <summary>
    /// Initializes a new instance of the <see cref="BM25Ranker"/> class.
    /// </summary>
    /// <param name="settings">Search settings for configurable ranking behavior.</param>
    public BM25Ranker(SearchSettings? settings = null)
    {
        _settings = settings ?? new SearchSettings();
    }

    /// <summary>
    /// Calculates BM25 score for a document given a query, with recency boost.
    /// </summary>
    /// <param name="query">The search query text.</param>
    /// <param name="content">The document content to score.</param>
    /// <param name="createdAt">The document creation timestamp.</param>
    /// <returns>The BM25 score with recency boost applied.</returns>
    public double CalculateScore(string query, string content, DateTime createdAt)
    {
        if (string.IsNullOrWhiteSpace(query) || string.IsNullOrWhiteSpace(content))
        {
            return 0;
        }

        var queryTerms = Tokenize(query);
        var contentTerms = Tokenize(content);
        var contentLength = contentTerms.Count;

        if (queryTerms.Count == 0 || contentTerms.Count == 0)
        {
            return 0;
        }

        // Calculate term frequencies in content
        var termFrequencies = new Dictionary<string, int>();
        foreach (var term in contentTerms)
        {
            if (termFrequencies.ContainsKey(term))
            {
                termFrequencies[term]++;
            }
            else
            {
                termFrequencies[term] = 1;
            }
        }

        // Calculate BM25 score
        double score = 0;
        foreach (var queryTerm in queryTerms.Distinct())
        {
            if (termFrequencies.TryGetValue(queryTerm, out var termFreq))
            {
                // BM25 formula: IDF * ((f(qi, D) * (k1 + 1)) / (f(qi, D) + k1 * (1 - b + b * |D| / avgdl)))
                // Simplified IDF (assumes query terms are reasonably rare)
                var idf = 1.0; // Simplified - in full implementation would use document frequency

                var numerator = termFreq * (K1 + 1);
                var denominator = termFreq + (K1 * (1 - B + (B * contentLength / AvgDocLength)));

                score += idf * (numerator / denominator);
            }
        }

        // Apply recency boost
        var recencyBoost = CalculateRecencyBoost(createdAt);
        return score * recencyBoost;
    }

    /// <summary>
    /// Ranks a list of search results by score in descending order.
    /// </summary>
    /// <param name="results">The search results to rank.</param>
    /// <returns>The results sorted by score (highest first).</returns>
    public IReadOnlyList<SearchResult> RankResults(IEnumerable<SearchResult> results)
    {
        return results.OrderByDescending(r => r.Score).ToList();
    }

    /// <summary>
    /// Tokenizes text into lowercase terms, splitting on whitespace and punctuation.
    /// </summary>
    /// <param name="text">The text to tokenize.</param>
    /// <returns>List of normalized terms.</returns>
    private static List<string> Tokenize(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return new List<string>();
        }

        // Simple tokenization: split on whitespace and punctuation, convert to lowercase
        var terms = new List<string>();
        var currentTerm = new System.Text.StringBuilder();

        foreach (var c in text)
        {
            if (char.IsLetterOrDigit(c))
            {
                currentTerm.Append(char.ToLowerInvariant(c));
            }
            else if (currentTerm.Length > 0)
            {
                terms.Add(currentTerm.ToString());
                currentTerm.Clear();
            }
        }

        if (currentTerm.Length > 0)
        {
            terms.Add(currentTerm.ToString());
        }

        return terms;
    }

    /// <summary>
    /// Calculates recency boost factor based on message age.
    /// Uses configurable boost multipliers from SearchSettings (AC-054, AC-055).
    /// </summary>
    /// <param name="createdAt">The message creation timestamp.</param>
    /// <returns>The recency boost multiplier.</returns>
    private double CalculateRecencyBoost(DateTime createdAt)
    {
        // AC-055: Check if recency boost is enabled
        if (!_settings.RecencyBoostEnabled)
        {
            return 1.0;
        }

        var age = DateTime.UtcNow - createdAt;

        // AC-054: Use configured boost multipliers
        if (age.TotalHours < 24)
        {
            return _settings.RecencyBoost24Hours;
        }

        if (age.TotalDays <= 7)
        {
            return _settings.RecencyBoost7Days;
        }

        return _settings.RecencyBoostDefault;
    }
}
