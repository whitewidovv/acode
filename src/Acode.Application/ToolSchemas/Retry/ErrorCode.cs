namespace Acode.Application.ToolSchemas.Retry;

/// <summary>
/// Standard error codes for validation failures.
/// All codes follow VAL-XXX format for consistency and model comprehension.
/// </summary>
/// <remarks>
/// Spec Reference: Implementation Prompt lines 3244-3331.
/// These codes are used in ValidationError.ErrorCode to identify the type of validation failure.
/// </remarks>
public static class ErrorCode
{
    /// <summary>
    /// A required field is missing from the input.
    /// </summary>
    public const string RequiredFieldMissing = "VAL-001";

    /// <summary>
    /// The type of a field does not match the expected type.
    /// </summary>
    public const string TypeMismatch = "VAL-002";

    /// <summary>
    /// A value violates a constraint (min, max, range, etc.).
    /// </summary>
    public const string ConstraintViolation = "VAL-003";

    /// <summary>
    /// The input is not valid JSON syntax.
    /// </summary>
    public const string InvalidJsonSyntax = "VAL-004";

    /// <summary>
    /// An unknown/unexpected field was provided (strict mode).
    /// </summary>
    public const string UnknownField = "VAL-005";

    /// <summary>
    /// An array has too few or too many items.
    /// </summary>
    public const string ArrayLengthViolation = "VAL-006";

    /// <summary>
    /// A string value does not match the required pattern/regex.
    /// </summary>
    public const string PatternMismatch = "VAL-007";

    /// <summary>
    /// A value is not one of the allowed enum values.
    /// </summary>
    public const string InvalidEnumValue = "VAL-008";

    /// <summary>
    /// A string's length exceeds maximum or is below minimum.
    /// </summary>
    public const string StringLengthViolation = "VAL-009";

    /// <summary>
    /// A value does not match the required format (date, uri, email, etc.).
    /// </summary>
    public const string FormatViolation = "VAL-010";

    /// <summary>
    /// A number is outside the allowed range.
    /// </summary>
    public const string NumberRangeViolation = "VAL-011";

    /// <summary>
    /// A uniqueness constraint was violated (duplicate items).
    /// </summary>
    public const string UniqueConstraintViolation = "VAL-012";

    /// <summary>
    /// A dependency constraint was violated (dependent fields).
    /// </summary>
    public const string DependencyViolation = "VAL-013";

    /// <summary>
    /// Mutually exclusive fields were both provided.
    /// </summary>
    public const string MutualExclusivityViolation = "VAL-014";

    /// <summary>
    /// The object does not conform to the expected schema structure.
    /// </summary>
    public const string ObjectSchemaViolation = "VAL-015";
}
