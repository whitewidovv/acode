using System.Text;
using System.Text.RegularExpressions;
using Acode.Domain.Search;

namespace Acode.Infrastructure.Search;

/// <summary>
/// Safely parses and sanitizes user search queries for FTS5 with boolean operator support.
/// </summary>
public sealed class SafeQueryParser
{
    private const int MaxOperatorCount = 5;
    private static readonly string[] BooleanOperators = new[] { "AND", "OR", "NOT" };

    /// <summary>
    /// Parses and validates a user query for FTS5 execution with boolean operator support.
    /// </summary>
    /// <param name="query">The raw user query.</param>
    /// <returns>A parsed FtsQuery with validation results.</returns>
    public FtsQuery ParseQuery(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return new FtsQuery
            {
                Fts5Syntax = string.Empty,
                OperatorCount = 0,
                IsValid = true,
            };
        }

        // Normalize whitespace
        var normalized = Regex.Replace(query, @"\s+", " ").Trim();

        // Tokenize: Split on spaces but preserve quoted phrases
        var tokens = TokenizeQuery(normalized);

        // Validate and transform
        var validationError = ValidateTokens(tokens, out int operatorCount);
        if (validationError != null)
        {
            return new FtsQuery
            {
                Fts5Syntax = string.Empty,
                OperatorCount = operatorCount,
                IsValid = false,
                ErrorMessage = validationError,
            };
        }

        // Build FTS5 syntax
        var fts5Syntax = BuildFts5Syntax(tokens, out operatorCount);

        return new FtsQuery
        {
            Fts5Syntax = fts5Syntax,
            OperatorCount = operatorCount,
            IsValid = true,
        };
    }

    private static List<Token> TokenizeQuery(string query)
    {
        var tokens = new List<Token>();
        var i = 0;

        while (i < query.Length)
        {
            // Skip whitespace
            while (i < query.Length && char.IsWhiteSpace(query[i]))
            {
                i++;
            }

            if (i >= query.Length)
            {
                break;
            }

            // Handle quoted phrases
            if (query[i] == '"')
            {
                var start = i;
                i++; // Skip opening quote
                while (i < query.Length && query[i] != '"')
                {
                    i++;
                }

                if (i < query.Length)
                {
                    i++; // Skip closing quote
                }

                var phrase = query.Substring(start, i - start);
                tokens.Add(new Token { Type = TokenType.Phrase, Value = phrase });
                continue;
            }

            // Handle parentheses
            if (query[i] == '(')
            {
                tokens.Add(new Token { Type = TokenType.LeftParen, Value = "(" });
                i++;
                continue;
            }

            if (query[i] == ')')
            {
                tokens.Add(new Token { Type = TokenType.RightParen, Value = ")" });
                i++;
                continue;
            }

            // Handle words (including potential operators)
            var wordStart = i;
            while (i < query.Length && !char.IsWhiteSpace(query[i]) && query[i] != '(' && query[i] != ')')
            {
                i++;
            }

            var word = query.Substring(wordStart, i - wordStart);

            // Sanitize word (remove special chars except hyphens/underscores)
            var sanitized = SanitizeWord(word);

            if (!string.IsNullOrEmpty(sanitized))
            {
                // Check if it's an operator
                if (IsOperator(sanitized))
                {
                    tokens.Add(new Token { Type = TokenType.Operator, Value = sanitized.ToUpperInvariant() });
                }
                else
                {
                    tokens.Add(new Token { Type = TokenType.Term, Value = sanitized });
                }
            }
        }

        return tokens;
    }

    private static string SanitizeWord(string word)
    {
        var result = new StringBuilder(word.Length);

        foreach (var c in word)
        {
            // Keep alphanumeric, hyphens, underscores
            if (char.IsLetterOrDigit(c) || c == '-' || c == '_')
            {
                result.Append(c);
            }
        }

        return result.ToString();
    }

    private static bool IsOperator(string word)
    {
        return BooleanOperators.Any(op => string.Equals(word, op, StringComparison.OrdinalIgnoreCase));
    }

    private static string? ValidateTokens(List<Token> tokens, out int operatorCount)
    {
        operatorCount = tokens.Count(t => t.Type == TokenType.Operator);

        // Check operator count
        if (operatorCount > MaxOperatorCount)
        {
            return $"Query contains {operatorCount} operators (maximum 5 allowed).";
        }

        // Check for balanced parentheses
        var parenBalance = 0;
        foreach (var token in tokens)
        {
            if (token.Type == TokenType.LeftParen)
            {
                parenBalance++;
            }
            else if (token.Type == TokenType.RightParen)
            {
                parenBalance--;
            }

            if (parenBalance < 0)
            {
                return "Query has unbalanced parentheses (closing before opening).";
            }
        }

        if (parenBalance != 0)
        {
            return "Query has unbalanced parentheses.";
        }

        // Check for leading operator
        var firstContentToken = tokens.FirstOrDefault(t => t.Type != TokenType.LeftParen);
        if (firstContentToken.Type == TokenType.Operator)
        {
            return $"Query cannot start with operator '{firstContentToken.Value}'.";
        }

        // Check for trailing operator
        var lastContentToken = tokens.LastOrDefault(t => t.Type != TokenType.RightParen);
        if (lastContentToken.Type == TokenType.Operator)
        {
            return $"Query cannot end with operator '{lastContentToken.Value}'.";
        }

        return null; // Valid
    }

    private static string BuildFts5Syntax(List<Token> tokens, out int operatorCount)
    {
        var result = new StringBuilder();
        operatorCount = 0;

        for (int i = 0; i < tokens.Count; i++)
        {
            var token = tokens[i];

            switch (token.Type)
            {
                case TokenType.Term:
                case TokenType.Phrase:
                    // Add implicit OR between adjacent terms/phrases
                    if (i > 0)
                    {
                        var prevToken = tokens[i - 1];
                        if (prevToken.Type == TokenType.Term || prevToken.Type == TokenType.Phrase || prevToken.Type == TokenType.RightParen)
                        {
                            result.Append(" OR ");
                            operatorCount++;
                        }
                    }

                    result.Append(token.Value);
                    break;

                case TokenType.Operator:
                    result.Append(' ').Append(token.Value).Append(' ');
                    operatorCount++;
                    break;

                case TokenType.LeftParen:
                    // Add implicit OR before left paren if preceded by term/phrase/right paren
                    if (i > 0)
                    {
                        var prevToken = tokens[i - 1];
                        if (prevToken.Type == TokenType.Term || prevToken.Type == TokenType.Phrase || prevToken.Type == TokenType.RightParen)
                        {
                            result.Append(" OR ");
                            operatorCount++;
                        }
                    }

                    result.Append('(');
                    break;

                case TokenType.RightParen:
                    result.Append(')');
                    break;
            }
        }

        // Clean up extra spaces
        var output = Regex.Replace(result.ToString(), @"\s+", " ").Trim();

        return output;
    }

#pragma warning disable SA1201 // Nested types are placed at end for readability
    private enum TokenType
    {
        Term,
        Phrase,
        Operator,
        LeftParen,
        RightParen,
    }

    private struct Token
    {
        public TokenType Type { get; set; }

        public string Value { get; set; }
    }
#pragma warning restore SA1201
}
