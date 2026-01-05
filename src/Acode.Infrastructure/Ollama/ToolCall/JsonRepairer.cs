namespace Acode.Infrastructure.Ollama.ToolCall;

using System.Text;
using System.Text.Json;
using Acode.Infrastructure.Ollama.ToolCall.Models;

/// <summary>
/// Attempts to repair common JSON syntax errors produced by language models.
/// All repairs are deterministic and idempotent.
/// </summary>
public sealed class JsonRepairer
{
    private readonly int timeoutMs;

    /// <summary>
    /// Initializes a new instance of the <see cref="JsonRepairer"/> class.
    /// </summary>
    /// <param name="timeoutMs">The timeout for repair attempts in milliseconds.</param>
    public JsonRepairer(int timeoutMs = 100)
    {
        this.timeoutMs = timeoutMs;
    }

    /// <summary>
    /// Attempt to repair malformed JSON.
    /// Returns immediately if JSON is already valid.
    /// </summary>
    /// <param name="json">The JSON string to repair.</param>
    /// <returns>The repair result.</returns>
    public RepairResult TryRepair(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return RepairResult.Fail(json ?? string.Empty, "Input is empty or whitespace");
        }

        // Check if already valid
        if (IsValidJson(json))
        {
            return RepairResult.AlreadyValid(json);
        }

        // Use cancellation token for timeout
        using var cts = new CancellationTokenSource(timeoutMs);

        try
        {
            var (repaired, repairs) = ApplyRepairs(json, cts.Token);

            if (IsValidJson(repaired))
            {
                return RepairResult.Ok(json, repaired, repairs);
            }

            return RepairResult.Fail(json, "Unable to repair JSON after applying heuristics");
        }
        catch (OperationCanceledException)
        {
            return RepairResult.Fail(json, "Repair timed out");
        }
    }

    /// <summary>
    /// Checks if a string is valid JSON.
    /// </summary>
    private static bool IsValidJson(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    /// <summary>
    /// Applies repair heuristics in order of frequency.
    /// </summary>
    private static (string Result, List<string> Repairs) ApplyRepairs(string json, CancellationToken ct)
    {
        var result = json;
        var repairs = new List<string>();

        // 1. Remove trailing commas (42% of errors)
        ct.ThrowIfCancellationRequested();
        var (newResult, wasApplied) = RemoveTrailingCommas(result);
        if (wasApplied)
        {
            result = newResult;
            repairs.Add("removed_trailing_comma");
        }

        // 2. Balance braces (23% of errors)
        ct.ThrowIfCancellationRequested();
        (newResult, wasApplied) = BalanceBraces(result);
        if (wasApplied)
        {
            result = newResult;
            repairs.Add("balanced_braces");
        }

        // 3. Balance brackets (11% of errors)
        ct.ThrowIfCancellationRequested();
        (newResult, wasApplied) = BalanceBrackets(result);
        if (wasApplied)
        {
            result = newResult;
            repairs.Add("balanced_brackets");
        }

        // 4. Replace single quotes with double quotes (8% of errors)
        ct.ThrowIfCancellationRequested();
        (newResult, wasApplied) = ReplaceSingleQuotes(result);
        if (wasApplied)
        {
            result = newResult;
            repairs.Add("replaced_single_quotes");
        }

        // 5. Quote unquoted keys (7% of errors)
        ct.ThrowIfCancellationRequested();
        (newResult, wasApplied) = QuoteUnquotedKeys(result);
        if (wasApplied)
        {
            result = newResult;
            repairs.Add("quoted_unquoted_keys");
        }

        // 6. Close unclosed strings (5% of errors)
        ct.ThrowIfCancellationRequested();
        (newResult, wasApplied) = CloseUnclosedStrings(result);
        if (wasApplied)
        {
            result = newResult;
            repairs.Add("closed_unclosed_strings");
        }

        return (result, repairs);
    }

    /// <summary>
    /// Removes trailing commas before closing braces/brackets.
    /// </summary>
    private static (string Result, bool Applied) RemoveTrailingCommas(string json)
    {
        var original = json;

        // Remove comma followed by } or ]
        var sb = new StringBuilder(json.Length);
        var i = 0;
        while (i < json.Length)
        {
            if (json[i] == ',' && i + 1 < json.Length)
            {
                // Look ahead for whitespace followed by } or ]
                var j = i + 1;
                while (j < json.Length && char.IsWhiteSpace(json[j]))
                {
                    j++;
                }

                if (j < json.Length && (json[j] == '}' || json[j] == ']'))
                {
                    // Skip the comma
                    i++;
                    continue;
                }
            }

            sb.Append(json[i]);
            i++;
        }

        var result = sb.ToString();
        return (result, result != original);
    }

    /// <summary>
    /// Balances opening and closing braces.
    /// </summary>
    private static (string Result, bool Applied) BalanceBraces(string json)
    {
        var openCount = 0;
        var closeCount = 0;
        var inString = false;
        var escaped = false;

        for (var i = 0; i < json.Length; i++)
        {
            var c = json[i];

            if (escaped)
            {
                escaped = false;
                continue;
            }

            if (c == '\\' && inString)
            {
                escaped = true;
                continue;
            }

            if (c == '"')
            {
                inString = !inString;
                continue;
            }

            if (!inString)
            {
                if (c == '{')
                {
                    openCount++;
                }
                else if (c == '}')
                {
                    closeCount++;
                }
            }
        }

        if (openCount > closeCount)
        {
            var missingBraces = new string('}', openCount - closeCount);
            return (json + missingBraces, true);
        }

        return (json, false);
    }

    /// <summary>
    /// Balances opening and closing brackets.
    /// </summary>
    private static (string Result, bool Applied) BalanceBrackets(string json)
    {
        var openCount = 0;
        var closeCount = 0;
        var inString = false;
        var escaped = false;

        for (var i = 0; i < json.Length; i++)
        {
            var c = json[i];

            if (escaped)
            {
                escaped = false;
                continue;
            }

            if (c == '\\' && inString)
            {
                escaped = true;
                continue;
            }

            if (c == '"')
            {
                inString = !inString;
                continue;
            }

            if (!inString)
            {
                if (c == '[')
                {
                    openCount++;
                }
                else if (c == ']')
                {
                    closeCount++;
                }
            }
        }

        if (openCount > closeCount)
        {
            var missingBrackets = new string(']', openCount - closeCount);
            return (json + missingBrackets, true);
        }

        return (json, false);
    }

    /// <summary>
    /// Replaces single quotes with double quotes (outside existing double-quoted strings).
    /// </summary>
    private static (string Result, bool Applied) ReplaceSingleQuotes(string json)
    {
        var original = json;
        var sb = new StringBuilder(json.Length);
        var inDoubleString = false;
        var escaped = false;

        for (var i = 0; i < json.Length; i++)
        {
            var c = json[i];

            if (escaped)
            {
                escaped = false;
                sb.Append(c);
                continue;
            }

            if (c == '\\')
            {
                escaped = true;
                sb.Append(c);
                continue;
            }

            if (c == '"')
            {
                inDoubleString = !inDoubleString;
                sb.Append(c);
                continue;
            }

            if (c == '\'' && !inDoubleString)
            {
                sb.Append('"');
            }
            else
            {
                sb.Append(c);
            }
        }

        var result = sb.ToString();
        return (result, result != original);
    }

    /// <summary>
    /// Quotes unquoted property keys.
    /// </summary>
    private static (string Result, bool Applied) QuoteUnquotedKeys(string json)
    {
        // Simple heuristic: look for pattern like  key: or  key : before a value
        var original = json;
        var sb = new StringBuilder(json.Length * 2);
        var i = 0;
        var inString = false;
        var escaped = false;

        while (i < json.Length)
        {
            var c = json[i];

            if (escaped)
            {
                escaped = false;
                sb.Append(c);
                i++;
                continue;
            }

            if (c == '\\' && inString)
            {
                escaped = true;
                sb.Append(c);
                i++;
                continue;
            }

            if (c == '"')
            {
                inString = !inString;
                sb.Append(c);
                i++;
                continue;
            }

            if (!inString && (c == '{' || c == ','))
            {
                sb.Append(c);
                i++;

                // Skip whitespace
                while (i < json.Length && char.IsWhiteSpace(json[i]))
                {
                    sb.Append(json[i]);
                    i++;
                }

                // Check if next is an unquoted key (letter or underscore followed by colon)
                if (i < json.Length && (char.IsLetter(json[i]) || json[i] == '_'))
                {
                    var keyStart = i;
                    while (i < json.Length && (char.IsLetterOrDigit(json[i]) || json[i] == '_'))
                    {
                        i++;
                    }

                    var key = json[keyStart..i];

                    // Skip whitespace after key
                    while (i < json.Length && char.IsWhiteSpace(json[i]))
                    {
                        i++;
                    }

                    // Check for colon
                    if (i < json.Length && json[i] == ':')
                    {
                        // This was an unquoted key, quote it
                        sb.Append('"');
                        sb.Append(key);
                        sb.Append('"');
                    }
                    else
                    {
                        // Not a key, put it back as-is
                        sb.Append(key);
                    }
                }

                continue;
            }

            sb.Append(c);
            i++;
        }

        var result = sb.ToString();
        return (result, result != original);
    }

    /// <summary>
    /// Closes unclosed string literals.
    /// </summary>
    private static (string Result, bool Applied) CloseUnclosedStrings(string json)
    {
        var quoteCount = 0;
        var escaped = false;

        for (var i = 0; i < json.Length; i++)
        {
            var c = json[i];

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

            if (c == '"')
            {
                quoteCount++;
            }
        }

        // Odd number of quotes means unclosed string
        if (quoteCount % 2 == 1)
        {
            return (json + "\"", true);
        }

        return (json, false);
    }
}
