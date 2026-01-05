namespace Acode.Application.Tools.Retry;

/// <summary>
/// Defines validation error codes for the retry contract.
/// </summary>
/// <remarks>
/// FR-007b: Validation error retry contract.
/// FR-009 to FR-017: Error code definitions.
/// All codes follow the VAL-XXX format for model comprehension.
/// </remarks>
public static class ValidationErrorCode
{
    /// <summary>
    /// Required field is missing.
    /// </summary>
    public const string RequiredMissing = "VAL-001";

    /// <summary>
    /// Type mismatch between expected and actual value.
    /// </summary>
    public const string TypeMismatch = "VAL-002";

    /// <summary>
    /// Value violates a constraint (min, max, range).
    /// </summary>
    public const string ConstraintViolation = "VAL-003";

    /// <summary>
    /// Invalid JSON syntax.
    /// </summary>
    public const string InvalidJson = "VAL-004";

    /// <summary>
    /// Unknown field in strict mode.
    /// </summary>
    public const string UnknownField = "VAL-005";

    /// <summary>
    /// Array length violation (too few or too many items).
    /// </summary>
    public const string ArrayLengthViolation = "VAL-006";

    /// <summary>
    /// Value does not match required pattern.
    /// </summary>
    public const string PatternMismatch = "VAL-007";

    /// <summary>
    /// Value is not in the allowed enum values.
    /// </summary>
    public const string InvalidEnumValue = "VAL-008";

    /// <summary>
    /// String length exceeds maximum or is below minimum.
    /// </summary>
    public const string StringLengthViolation = "VAL-009";

    /// <summary>
    /// Value does not match required format.
    /// </summary>
    public const string FormatViolation = "VAL-010";
}
