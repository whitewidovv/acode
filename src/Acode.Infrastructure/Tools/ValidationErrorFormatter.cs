namespace Acode.Infrastructure.Tools;

using System.Text;
using Acode.Application.Tools.Retry;
using Acode.Domain.Tools;

/// <summary>
/// Formats validation errors into model-comprehensible messages.
/// </summary>
/// <remarks>
/// FR-007b: Validation error retry contract.
/// FR-018 to FR-025: Error formatting requirements.
/// </remarks>
public sealed class ValidationErrorFormatter : IValidationErrorFormatter
{
    private readonly RetryConfiguration config;

    /// <summary>
    /// Initializes a new instance of the <see cref="ValidationErrorFormatter"/> class.
    /// </summary>
    /// <param name="config">The retry configuration.</param>
    public ValidationErrorFormatter(RetryConfiguration config)
    {
        this.config = config ?? throw new ArgumentNullException(nameof(config));
    }

    /// <inheritdoc />
    public string FormatErrors(
        string toolName,
        IReadOnlyCollection<SchemaValidationError> errors,
        int attemptNumber,
        int maxAttempts)
    {
        ArgumentNullException.ThrowIfNull(errors);
        var sb = new StringBuilder();

        // Header
        sb.AppendLine($"Tool validation failed for '{toolName}' - Attempt {attemptNumber} of {maxAttempts}");
        sb.AppendLine();

        if (errors.Count == 0)
        {
            sb.AppendLine("No specific errors reported. Please verify the input format.");
            return this.TruncateIfNeeded(sb.ToString());
        }

        // Format errors up to max
        var errorsToShow = errors.Take(this.config.MaxErrorsShown).ToList();
        var remainingCount = errors.Count - errorsToShow.Count;

        sb.AppendLine("Errors:");
        foreach (var error in errorsToShow)
        {
            var severityStr = error.Severity == ErrorSeverity.Warning ? "[Warning]" : "[Error]";
            sb.AppendLine($"  {severityStr} {error.Code} at '{error.Path}': {error.Message}");
        }

        if (remainingCount > 0)
        {
            var errorWord = remainingCount == 1 ? "error" : "errors";
            sb.AppendLine($"  ... and {remainingCount} additional {errorWord} not shown.");
        }

        sb.AppendLine();
        sb.AppendLine("Please fix the errors above and retry.");

        return this.TruncateIfNeeded(sb.ToString());
    }

    /// <inheritdoc />
    public string FormatEscalation(string toolName, IReadOnlyList<ValidationAttempt> history)
    {
        ArgumentNullException.ThrowIfNull(history);
        var sb = new StringBuilder();

        // Header
        sb.AppendLine($"Validation failed for tool '{toolName}' after {history.Count} attempts - escalating to user.");
        sb.AppendLine();
        sb.AppendLine("The model was unable to provide valid arguments within the allowed retry limit.");
        sb.AppendLine();

        // History summary
        sb.AppendLine("Attempt History:");
        foreach (var attempt in history)
        {
            sb.AppendLine($"  Attempt {attempt.AttemptNumber} ({attempt.Timestamp:HH:mm:ss}):");
            foreach (var error in attempt.Errors.Take(3))
            {
                sb.AppendLine($"    - {error.Code}: {error.Message}");
            }

            if (attempt.Errors.Count > 3)
            {
                sb.AppendLine($"    ... and {attempt.Errors.Count - 3} more errors");
            }
        }

        sb.AppendLine();
        sb.AppendLine("User intervention required to resolve this issue.");

        return this.TruncateIfNeeded(sb.ToString());
    }

    private string TruncateIfNeeded(string message)
    {
        if (message.Length <= this.config.MaxMessageLength)
        {
            return message;
        }

        var truncated = message[..this.config.MaxMessageLength];
        var lastNewline = truncated.LastIndexOf('\n');
        if (lastNewline > this.config.MaxMessageLength / 2)
        {
            truncated = truncated[..lastNewline];
        }

        return truncated + "\n... (message truncated)";
    }
}
