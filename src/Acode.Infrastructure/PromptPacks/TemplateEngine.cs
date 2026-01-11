using System.Text.RegularExpressions;
using Acode.Application.PromptPacks;
using Acode.Domain.PromptPacks;
using Acode.Domain.PromptPacks.Exceptions;

namespace Acode.Infrastructure.PromptPacks;

/// <summary>
/// Processes Mustache-style {{variable}} templates.
/// </summary>
public sealed partial class TemplateEngine : ITemplateEngine
{
    private readonly int _maxVariableLength;
    private readonly int _maxExpansionDepth;

    /// <summary>
    /// Initializes a new instance of the <see cref="TemplateEngine"/> class.
    /// </summary>
    /// <param name="maxVariableLength">Maximum allowed length for variable values.</param>
    /// <param name="maxExpansionDepth">Maximum recursion depth for nested variable expansion.</param>
    public TemplateEngine(
        int maxVariableLength = 1024,
        int maxExpansionDepth = 3)
    {
        _maxVariableLength = maxVariableLength;
        _maxExpansionDepth = maxExpansionDepth;
    }

    /// <inheritdoc />
    public string Substitute(string content, CompositionContext context)
    {
        if (string.IsNullOrEmpty(content))
        {
            return content;
        }

        ArgumentNullException.ThrowIfNull(context);

        var variables = BuildVariableMap(context);
        return SubstituteRecursive(content, variables, depth: 0);
    }

    [GeneratedRegex(@"\{\{(?<name>[a-zA-Z_][a-zA-Z0-9_]*)\}\}", RegexOptions.Compiled)]
    private static partial Regex VariablePattern();

    private static string EscapeValue(string value)
    {
        // Escape HTML entities to prevent injection
        return value
            .Replace("&", "&amp;", StringComparison.Ordinal)
            .Replace("<", "&lt;", StringComparison.Ordinal)
            .Replace(">", "&gt;", StringComparison.Ordinal)
            .Replace("\"", "&quot;", StringComparison.Ordinal)
            .Replace("'", "&#39;", StringComparison.Ordinal);
    }

    private IReadOnlyDictionary<string, string> BuildVariableMap(CompositionContext context)
    {
        // Priority: config > environment > context > defaults > variables
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        // 1. Variables (lowest priority when using multi-source)
        foreach (var kvp in context.Variables)
        {
            map[kvp.Key] = kvp.Value;
        }

        // 2. Defaults
        foreach (var kvp in context.DefaultVariables)
        {
            map[kvp.Key] = kvp.Value;
        }

        // 3. Context variables
        foreach (var kvp in context.ContextVariables)
        {
            map[kvp.Key] = kvp.Value;
        }

        // 4. Environment variables
        foreach (var kvp in context.EnvironmentVariables)
        {
            map[kvp.Key] = kvp.Value;
        }

        // 5. Config variables (highest priority)
        foreach (var kvp in context.ConfigVariables)
        {
            map[kvp.Key] = kvp.Value;
        }

        return map;
    }

    private string SubstituteRecursive(
        string content,
        IReadOnlyDictionary<string, string> variables,
        int depth)
    {
        if (depth > _maxExpansionDepth)
        {
            throw new TemplateVariableException(
                $"Template variable expansion depth limit ({_maxExpansionDepth}) exceeded. Possible circular reference.",
                "ACODE-PRM-008",
                null);
        }

        return VariablePattern().Replace(content, match =>
        {
            var variableName = match.Groups["name"].Value;

            if (!variables.TryGetValue(variableName, out var value))
            {
                // Missing variable replaced with empty string
                return string.Empty;
            }

            ValidateVariableValue(value, variableName);
            var escaped = EscapeValue(value);

            // Check if value contains more variables (nested expansion)
            if (VariablePattern().IsMatch(escaped))
            {
                return SubstituteRecursive(escaped, variables, depth + 1);
            }

            return escaped;
        });
    }

    private void ValidateVariableValue(string value, string variableName)
    {
        if (value.Length > _maxVariableLength)
        {
            throw new TemplateVariableException(
                $"Variable '{variableName}' value exceeds maximum length ({_maxVariableLength} characters).",
                variableName);
        }
    }
}
