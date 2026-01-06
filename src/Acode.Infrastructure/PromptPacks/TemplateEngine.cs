using System.Text;
using System.Text.RegularExpressions;
using Acode.Application.PromptPacks;
using Acode.Domain.PromptPacks;

namespace Acode.Infrastructure.PromptPacks;

/// <summary>
/// Template engine for substituting variables in prompt pack components.
/// </summary>
public sealed partial class TemplateEngine : ITemplateEngine
{
    private const int MaxVariableLength = 1024;
    private const int MaxRecursionDepth = 3;

    /// <inheritdoc/>
    public string Substitute(string templateText, Dictionary<string, string> variables)
    {
        ArgumentNullException.ThrowIfNull(templateText);
        ArgumentNullException.ThrowIfNull(variables);

        if (string.IsNullOrEmpty(templateText))
        {
            return string.Empty;
        }

        // Validate variable values don't exceed max length
        var oversizedVariable = variables
            .Where(kvp => kvp.Value.Length > MaxVariableLength)
            .Select(kvp => kvp.Key)
            .FirstOrDefault();

        if (oversizedVariable != null)
        {
            throw new ArgumentException(
                $"Variable '{oversizedVariable}' value exceeds maximum length of {MaxVariableLength} characters.",
                nameof(variables));
        }

        // Perform substitution with recursion detection
        return SubstituteRecursive(templateText, variables, 0);
    }

    /// <inheritdoc/>
    public ValidationResult ValidateTemplate(string templateText)
    {
        ArgumentNullException.ThrowIfNull(templateText);

        var errors = new List<ValidationError>();
        var regex = GetVariablePattern();
        var matches = regex.Matches(templateText);

        // Check for unclosed braces
        var openBraces = templateText.Count(c => c == '{');
        var closeBraces = templateText.Count(c => c == '}');

        if (openBraces != closeBraces)
        {
            errors.Add(new ValidationError(
                "TEMPLATE_UNCLOSED_BRACES",
                "Template contains unclosed braces",
                null,
                ValidationSeverity.Error));
        }

        // Validate each variable name
        var variableNames = matches.Cast<Match>()
            .Select(match => match.Groups[1].Value)
            .ToList();

        foreach (var variableName in variableNames.Where(string.IsNullOrWhiteSpace))
        {
            errors.Add(new ValidationError(
                "TEMPLATE_EMPTY_VARIABLE",
                "Template contains empty variable name {{}}",
                null,
                ValidationSeverity.Error));
        }

        foreach (var variableName in variableNames
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Where(name => !GetVariableNamePattern().IsMatch(name)))
        {
            errors.Add(new ValidationError(
                "TEMPLATE_INVALID_VARIABLE_NAME",
                $"Template contains invalid variable name '{variableName}'. Variable names must contain only alphanumeric characters and underscores.",
                null,
                ValidationSeverity.Error));
        }

        // Check for empty variable names like {{}}
        if (templateText.Contains("{{}}", StringComparison.Ordinal))
        {
            errors.Add(new ValidationError(
                "TEMPLATE_EMPTY_VARIABLE",
                "Template contains empty variable name {{}}",
                null,
                ValidationSeverity.Error));
        }

        // Check for potential malformed patterns like {{name with spaces}}
        if (templateText.Contains("{{", StringComparison.Ordinal))
        {
            var malformedPattern = new Regex(@"\{\{[^}]*\s+[^}]*\}\}", RegexOptions.Compiled);
            if (malformedPattern.IsMatch(templateText))
            {
                errors.Add(new ValidationError(
                    "TEMPLATE_INVALID_VARIABLE_NAME",
                    "Template contains invalid variable name with spaces or special characters",
                    null,
                    ValidationSeverity.Error));
            }
        }

        return errors.Count == 0
            ? ValidationResult.Success()
            : ValidationResult.Failure(errors);
    }

    private static string SubstituteRecursive(
        string templateText,
        Dictionary<string, string> variables,
        int depth)
    {
        if (depth >= MaxRecursionDepth)
        {
            throw new InvalidOperationException(
                $"Recursive variable expansion detected. Maximum depth of {MaxRecursionDepth} exceeded.");
        }

        var regex = GetVariablePattern();
        var result = new StringBuilder(templateText);
        var matches = regex.Matches(templateText);

        // Track if any substitution was made
        var substitutionMade = false;

        // Replace from end to start to preserve match indices
        for (var i = matches.Count - 1; i >= 0; i--)
        {
            var match = matches[i];
            var variableName = match.Groups[1].Value;

            // Get variable value (empty string if missing)
            var value = variables.GetValueOrDefault(variableName) ?? string.Empty;

            // Replace the match
            result.Remove(match.Index, match.Length);
            result.Insert(match.Index, value);

            if (!string.IsNullOrEmpty(value))
            {
                substitutionMade = true;
            }
        }

        var substituted = result.ToString();

        // Check if result contains more variables (indicating recursion)
        if (substitutionMade && regex.IsMatch(substituted))
        {
            // Recursively substitute
            return SubstituteRecursive(substituted, variables, depth + 1);
        }

        return substituted;
    }

    /// <summary>
    /// Regular expression for matching {{variable_name}} patterns.
    /// Variable names can contain alphanumeric characters and underscores.
    /// </summary>
    [GeneratedRegex(@"\{\{([a-zA-Z0-9_]+)\}\}", RegexOptions.Compiled)]
    private static partial Regex GetVariablePattern();

    /// <summary>
    /// Regular expression for validating variable names.
    /// Variable names must be non-empty and contain only alphanumeric characters and underscores.
    /// </summary>
    [GeneratedRegex("^[a-zA-Z0-9_]+$", RegexOptions.Compiled)]
    private static partial Regex GetVariableNamePattern();
}
