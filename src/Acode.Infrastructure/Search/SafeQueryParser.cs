using System.Text;
using System.Text.RegularExpressions;
using Acode.Domain.Models.Inference;
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
    /// Parses and validates a user query for FTS5 execution with boolean operator support and field filters.
    /// </summary>
    /// <param name="query">The raw user query.</param>
    /// <returns>A parsed FtsQuery with validation results and extracted field filters.</returns>
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

        // Tokenize: Split on spaces but preserve quoted phrases and field prefixes
        var tokens = TokenizeQuery(normalized);

        // Extract field filters
        var fieldFilters = ExtractFieldFilters(tokens, out var filterError);
        if (filterError != null)
        {
            return new FtsQuery
            {
                Fts5Syntax = string.Empty,
                OperatorCount = 0,
                IsValid = false,
                ErrorMessage = filterError,
            };
        }

        // Remove field prefix tokens from main query tokens
        var contentTokens = tokens.Where(t => t.Type != TokenType.FieldPrefix).ToList();

        // Validate and transform remaining content tokens
        var validationError = ValidateTokens(contentTokens, out int operatorCount);
        if (validationError != null)
        {
            return new FtsQuery
            {
                Fts5Syntax = string.Empty,
                OperatorCount = operatorCount,
                IsValid = false,
                ErrorMessage = validationError,
                RoleFilter = fieldFilters.RoleFilter,
                ChatIdFilter = fieldFilters.ChatIdFilter,
                ChatNameFilter = fieldFilters.ChatNameFilter,
                TagFilter = fieldFilters.TagFilter,
                TitleTerms = fieldFilters.TitleTerms,
            };
        }

        // Build FTS5 syntax from content tokens
        var fts5Syntax = BuildFts5Syntax(contentTokens, out operatorCount);

        return new FtsQuery
        {
            Fts5Syntax = fts5Syntax,
            OperatorCount = operatorCount,
            IsValid = true,
            RoleFilter = fieldFilters.RoleFilter,
            ChatIdFilter = fieldFilters.ChatIdFilter,
            ChatNameFilter = fieldFilters.ChatNameFilter,
            TagFilter = fieldFilters.TagFilter,
            TitleTerms = fieldFilters.TitleTerms,
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

            // Handle words (including potential operators and field prefixes)
            var wordStart = i;
            while (i < query.Length && !char.IsWhiteSpace(query[i]) && query[i] != '(' && query[i] != ')')
            {
                i++;
            }

            var word = query.Substring(wordStart, i - wordStart);

            // Check if it's a field prefix (e.g., "role:user", "chat:name", "title:term", "tag:name")
            if (word.Contains(':', StringComparison.Ordinal))
            {
                var parts = word.Split(':', 2);
                var fieldName = parts[0].ToLowerInvariant();
                var fieldValue = parts.Length > 1 ? parts[1] : string.Empty;

                if (fieldName is "role" or "chat" or "title" or "tag")
                {
                    tokens.Add(new Token
                    {
                        Type = TokenType.FieldPrefix,
                        Value = word, // Keep original format for later parsing
                    });
                    continue;
                }
            }

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

    private static FieldFilters ExtractFieldFilters(List<Token> tokens, out string? errorMessage)
    {
        var filters = new FieldFilters();
        errorMessage = null;

        foreach (var token in tokens.Where(t => t.Type == TokenType.FieldPrefix))
        {
            var parts = token.Value.Split(':', 2);
            if (parts.Length != 2)
            {
                continue;
            }

            var fieldName = parts[0].ToLowerInvariant();
            var fieldValue = parts[1];

            switch (fieldName)
            {
                case "role":
                    if (Enum.TryParse<MessageRole>(fieldValue, ignoreCase: true, out var role))
                    {
                        filters.RoleFilter = role;
                    }
                    else
                    {
                        errorMessage = $"invalid role value '{fieldValue}'. Valid values: user, assistant, system, or tool.";
                        return filters;
                    }

                    break;

                case "chat":
                    // Try parsing as GUID first, otherwise treat as chat name
                    if (Guid.TryParse(fieldValue, out var chatId))
                    {
                        filters.ChatIdFilter = chatId;
                    }
                    else
                    {
                        filters.ChatNameFilter = fieldValue;
                    }

                    break;

                case "title":
                    filters.TitleTerms.Add(fieldValue);
                    break;

                case "tag":
                    filters.TagFilter = fieldValue;
                    break;
            }
        }

        return filters;
    }

#pragma warning disable SA1201 // Nested types are placed at end for readability
    private enum TokenType
    {
        Term,
        Phrase,
        Operator,
        LeftParen,
        RightParen,
        FieldPrefix,
    }

    private struct Token
    {
        public TokenType Type { get; set; }

        public string Value { get; set; }
    }

    private struct FieldFilters
    {
        public MessageRole? RoleFilter { get; set; }

        public Guid? ChatIdFilter { get; set; }

        public string? ChatNameFilter { get; set; }

        public string? TagFilter { get; set; }

        public List<string> TitleTerms { get; set; }

        public FieldFilters()
        {
            TitleTerms = new List<string>();
        }
    }
#pragma warning restore SA1201
}
