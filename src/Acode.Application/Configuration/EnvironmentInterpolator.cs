using System.Text;
using System.Text.RegularExpressions;

namespace Acode.Application.Configuration;

/// <summary>
/// Interpolates environment variables in configuration strings.
/// Supports ${VAR}, ${VAR:-default}, and ${VAR:?error} syntax.
/// </summary>
/// <remarks>
/// Per FR-002b-106 through FR-002b-120.
/// Interpolation occurs after parsing, before validation.
/// Not recursive - single pass only.
/// </remarks>
public sealed partial class EnvironmentInterpolator
{
    private const int MaxReplacements = 100;

    /// <summary>
    /// Interpolates environment variables in a string.
    /// </summary>
    /// <param name="input">Input string with ${VAR} references.</param>
    /// <returns>String with environment variables expanded.</returns>
    /// <exception cref="InvalidOperationException">If required variable (${VAR:?}) is not set.</exception>
    public string? Interpolate(string? input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return input;
        }

        var result = new StringBuilder(input);
        var replacements = 0;
        var regex = VariablePattern();

        var match = regex.Match(input);
        var offset = 0;

        while (match.Success && replacements < MaxReplacements)
        {
            var fullMatch = match.Value;

            // Handle $$ → $
            if (fullMatch == "$$")
            {
                result.Remove(match.Index + offset, 2);
                result.Insert(match.Index + offset, "$");
                offset -= 1;
            }
            else
            {
                var varName = match.Groups[1].Value;
                var defaultValue = match.Groups[2].Success ? match.Groups[2].Value : null;
                var errorMessage = match.Groups[3].Success ? match.Groups[3].Value : null;

                var envValue = Environment.GetEnvironmentVariable(varName);

                if (envValue == null)
                {
                    if (errorMessage != null)
                    {
                        throw new InvalidOperationException(
                            $"Required environment variable '{varName}' is not set. {errorMessage}");
                    }

                    envValue = defaultValue ?? string.Empty;
                }

                var replacement = envValue;
                result.Remove(match.Index + offset, fullMatch.Length);
                result.Insert(match.Index + offset, replacement);
                offset += replacement.Length - fullMatch.Length;
            }

            replacements++;
            match = match.NextMatch();
        }

        return result.ToString();
    }

    // Regex for ${VAR}, ${VAR:-default}, ${VAR:?error}
    // Variable names: [A-Za-z_][A-Za-z0-9_]* (alphanumeric+underscore, can't start with digit)
    [GeneratedRegex(@"\$\{([A-Za-z_][A-Za-z0-9_]*?)(?::-(.*?))?(?::\?(.*?))?\}|\$\$", RegexOptions.Compiled)]
    private static partial Regex VariablePattern();
}
