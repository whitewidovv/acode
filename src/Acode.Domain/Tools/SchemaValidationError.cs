namespace Acode.Domain.Tools;

/// <summary>
/// Represents a single validation error from JSON Schema validation of tool arguments.
/// </summary>
/// <remarks>
/// FR-007: Tool Schema Registry requires structured validation errors.
/// FR-007b: Validation error retry contract.
/// This is a model-friendly format that enables LLM retry on invalid arguments.
/// </remarks>
public sealed record SchemaValidationError
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SchemaValidationError"/> class.
    /// </summary>
    /// <param name="path">JSON Pointer path to the error location.</param>
    /// <param name="code">Error code following VAL-XXX pattern.</param>
    /// <param name="message">Human-readable error message.</param>
    /// <param name="severity">The severity level of the error.</param>
    /// <param name="expectedType">Expected type or format (null if not applicable).</param>
    /// <param name="actualValue">Sanitized actual value (null if not applicable).</param>
    /// <exception cref="ArgumentException">
    /// Thrown when path, code, or message is null or empty.
    /// </exception>
    public SchemaValidationError(
        string path,
        string code,
        string message,
        ErrorSeverity severity = ErrorSeverity.Error,
        string? expectedType = null,
        string? actualValue = null)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("Path must not be null or empty.", nameof(path));
        }

        if (string.IsNullOrWhiteSpace(code))
        {
            throw new ArgumentException("Code must not be null or empty.", nameof(code));
        }

        if (string.IsNullOrWhiteSpace(message))
        {
            throw new ArgumentException("Message must not be null or empty.", nameof(message));
        }

        this.Path = path;
        this.Code = code;
        this.Message = message;
        this.Severity = severity;
        this.ExpectedType = expectedType;
        this.ActualValue = actualValue;
    }

    /// <summary>
    /// Gets the JSON Pointer path to the error location.
    /// </summary>
    /// <example>Example paths: /path, /items/0, /properties/name.</example>
    public string Path { get; init; }

    /// <summary>
    /// Gets the error code following VAL-XXX pattern.
    /// </summary>
    /// <example>Example: VAL-001.</example>
    public string Code { get; init; }

    /// <summary>
    /// Gets the human-readable error message.
    /// </summary>
    public string Message { get; init; }

    /// <summary>
    /// Gets the severity level of the error.
    /// </summary>
    public ErrorSeverity Severity { get; init; }

    /// <summary>
    /// Gets the expected type or format, or null if not applicable.
    /// </summary>
    public string? ExpectedType { get; init; }

    /// <summary>
    /// Gets the sanitized actual value, or null if not applicable.
    /// </summary>
    /// <remarks>
    /// SECURITY: This value MUST be sanitized before storage to prevent
    /// sensitive data exposure in logs and error messages.
    /// </remarks>
    public string? ActualValue { get; init; }

    /// <summary>
    /// Returns a model-friendly string representation of the error.
    /// </summary>
    /// <returns>A formatted string containing path, code, and message.</returns>
    public override string ToString()
    {
        var result = $"[{this.Code}] {this.Path}: {this.Message}";

        if (this.ExpectedType is not null)
        {
            result += $" (expected: {this.ExpectedType})";
        }

        if (this.ActualValue is not null)
        {
            result += $" (actual: {this.ActualValue})";
        }

        return result;
    }
}
