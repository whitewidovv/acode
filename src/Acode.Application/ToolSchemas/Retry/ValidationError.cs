using System.Diagnostics.CodeAnalysis;

namespace Acode.Application.ToolSchemas.Retry;

/// <summary>
/// Represents a validation error that occurred during tool argument validation.
/// Designed for model comprehension with clear field paths and actionable messages.
/// </summary>
/// <remarks>
/// Spec Reference: Implementation Prompt lines 3159-3210.
/// This is an immutable sealed class with required init-only properties.
/// </remarks>
public sealed class ValidationError
{
    /// <summary>
    /// Gets the error code in VAL-XXX format (e.g., VAL-001, VAL-002).
    /// </summary>
    [NotNull]
    public required string ErrorCode { get; init; }

    /// <summary>
    /// Gets the JSON Pointer path to the field that failed validation (e.g., /path, /config/timeout).
    /// Format: RFC 6901 JSON Pointer notation.
    /// </summary>
    [NotNull]
    public required string FieldPath { get; init; }

    /// <summary>
    /// Gets the human-readable error message describing the validation failure.
    /// </summary>
    [NotNull]
    public required string Message { get; init; }

    /// <summary>
    /// Gets the severity level: Error (must fix), Warning (should fix), or Info (advisory).
    /// </summary>
    public required ErrorSeverity Severity { get; init; }

    /// <summary>
    /// Gets the expected value or type description from the schema.
    /// Example: "string", "integer between 1 and 100", "one of: utf-8, ascii".
    /// </summary>
    public string? ExpectedValue { get; init; }

    /// <summary>
    /// Gets the actual value provided by the model (sanitized to prevent secret leakage).
    /// May be truncated if very long.
    /// </summary>
    public string? ActualValue { get; init; }
}
