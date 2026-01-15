using System.Text;
using Acode.Application.ToolSchemas.Retry;

namespace Acode.Infrastructure.ToolSchemas.Retry;

/// <summary>
/// Formats validation errors into model-comprehensible messages.
/// </summary>
/// <remarks>
/// Spec Reference: Implementation Prompt lines 3511-3676.
/// Implements IErrorFormatter with error aggregation, sanitization, and hint generation.
/// </remarks>
public sealed class ErrorFormatter : IErrorFormatter
{
    private readonly RetryConfiguration config;
    private readonly ValueSanitizer sanitizer;
    private readonly ErrorAggregator aggregator;

    /// <summary>
    /// Initializes a new instance of the <see cref="ErrorFormatter"/> class.
    /// </summary>
    /// <param name="config">Retry configuration options.</param>
    public ErrorFormatter(RetryConfiguration config)
    {
        ArgumentNullException.ThrowIfNull(config);

        this.config = config;
        this.sanitizer = new ValueSanitizer(config.MaxValuePreview, config.RedactSecrets, config.RelativizePaths);
        this.aggregator = new ErrorAggregator(config.MaxErrorsShown);
    }

    /// <inheritdoc/>
    public string FormatErrors(string toolName, IEnumerable<ValidationError> errors, int attemptNumber, int maxAttempts)
    {
        ArgumentException.ThrowIfNullOrEmpty(toolName);

        var errorList = errors?.ToList() ?? new List<ValidationError>();
        if (errorList.Count == 0)
        {
            return string.Empty;
        }

        // Aggregate errors (deduplicate, sort, limit)
        var aggregatedErrors = this.aggregator.Aggregate(errorList);
        var totalErrors = errorList.Count;
        var shownErrors = aggregatedErrors.Count;

        var sb = new StringBuilder();

        // Header line
        sb.AppendLine($"Validation failed for tool '{toolName}' (attempt {attemptNumber}/{maxAttempts}):");
        sb.AppendLine();

        if (aggregatedErrors.Count == 1)
        {
            // Single error format (no "Errors:" header)
            var error = aggregatedErrors[0];
            FormatSingleError(sb, error);
        }
        else
        {
            // Multiple error format
            sb.AppendLine("Errors:");
            foreach (var error in aggregatedErrors)
            {
                FormatErrorBullet(sb, error);
            }
        }

        // Show count of additional errors if truncated
        if (totalErrors > shownErrors)
        {
            sb.AppendLine();
            sb.AppendLine($"...and {totalErrors - shownErrors} more error(s).");
        }

        // Include correction hints if enabled
        if (this.config.IncludeHints)
        {
            AppendCorrectionHints(sb, aggregatedErrors);
        }

        var result = sb.ToString();

        // Truncate if too long
        if (result.Length > this.config.MaxMessageLength)
        {
            result = result[..(this.config.MaxMessageLength - 3)] + "...";
        }

        return result;
    }

    private static void AppendCorrectionHints(StringBuilder sb, IReadOnlyList<ValidationError> errors)
    {
        sb.AppendLine();
        sb.AppendLine("Hints:");

        var hintsAdded = new HashSet<string>();

        foreach (var error in errors)
        {
            var hint = GenerateHintForError(error);
            if (hint is not null && hintsAdded.Add(hint))
            {
                sb.AppendLine($"  - {hint}");
            }
        }
    }

    private static string? GenerateHintForError(ValidationError error)
    {
        return error.ErrorCode switch
        {
            ErrorCode.RequiredFieldMissing => $"Add the required field '{ExtractFieldName(error.FieldPath)}'",
            ErrorCode.TypeMismatch when error.ExpectedValue is not null => $"Change {ExtractFieldName(error.FieldPath)} to type {error.ExpectedValue}",
            ErrorCode.TypeMismatch => $"Check the type of {ExtractFieldName(error.FieldPath)}",
            ErrorCode.InvalidEnumValue when error.ExpectedValue is not null => $"Use one of: {error.ExpectedValue}",
            ErrorCode.PatternMismatch when error.ExpectedValue is not null => $"Match pattern: {error.ExpectedValue}",
            ErrorCode.StringLengthViolation => "Adjust string length to meet constraints",
            ErrorCode.NumberRangeViolation when error.ExpectedValue is not null => $"Use value in range: {error.ExpectedValue}",
            ErrorCode.ArrayLengthViolation => "Adjust array length to meet constraints",
            ErrorCode.UnknownField => $"Remove unrecognized field '{ExtractFieldName(error.FieldPath)}'",
            _ => null,
        };
    }

    private static string ExtractFieldName(string fieldPath)
    {
        var lastSlash = fieldPath.LastIndexOf('/');
        return lastSlash >= 0 ? fieldPath[(lastSlash + 1)..] : fieldPath;
    }

    private void FormatSingleError(StringBuilder sb, ValidationError error)
    {
        sb.AppendLine($"[{error.ErrorCode}] {error.FieldPath}: {error.Message}");

        if (this.config.IncludeActualValues)
        {
            if (error.ExpectedValue is not null)
            {
                sb.AppendLine($"  Expected: {error.ExpectedValue}");
            }

            if (error.ActualValue is not null)
            {
                var sanitizedValue = this.sanitizer.Sanitize(error.ActualValue, error.FieldPath);
                sb.AppendLine($"  Actual: {sanitizedValue}");
            }
        }
    }

    private void FormatErrorBullet(StringBuilder sb, ValidationError error)
    {
        var severityPrefix = error.Severity switch
        {
            ErrorSeverity.Error => "❌",
            ErrorSeverity.Warning => "⚠️",
            ErrorSeverity.Info => "ℹ️",
            _ => "•",
        };

        sb.AppendLine($"  {severityPrefix} [{error.ErrorCode}] {error.FieldPath}: {error.Message}");

        if (this.config.IncludeActualValues && error.ActualValue is not null)
        {
            var sanitizedValue = this.sanitizer.Sanitize(error.ActualValue, error.FieldPath);
            sb.AppendLine($"      Actual: {sanitizedValue}");
        }
    }
}
