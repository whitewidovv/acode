namespace Acode.Application.Configuration;

/// <summary>
/// Standard error codes for configuration validation.
/// Per Task 002.b FR-002b-48, spec lines 401-429.
/// Error code format: ACODE-CFG-NNN (3-digit number).
/// </summary>
public static class ConfigErrorCodes
{
    /// <summary>
    /// ACODE-CFG-001: Configuration file does not exist.
    /// </summary>
    public const string FileNotFound = "ACODE-CFG-001";

    /// <summary>
    /// ACODE-CFG-002: Cannot read configuration file.
    /// </summary>
    public const string FileReadError = "ACODE-CFG-002";

    /// <summary>
    /// ACODE-CFG-003: File is not valid UTF-8.
    /// </summary>
    public const string EncodingError = "ACODE-CFG-003";

    /// <summary>
    /// ACODE-CFG-004: Invalid YAML syntax.
    /// </summary>
    public const string YamlSyntaxError = "ACODE-CFG-004";

    /// <summary>
    /// ACODE-CFG-005: YAML structure not allowed.
    /// </summary>
    public const string YamlStructureError = "ACODE-CFG-005";

    /// <summary>
    /// ACODE-CFG-006: Config exceeds 1MB limit.
    /// </summary>
    public const string FileTooLarge = "ACODE-CFG-006";

    /// <summary>
    /// ACODE-CFG-007: YAML nesting exceeds 20 levels.
    /// </summary>
    public const string NestingTooDeep = "ACODE-CFG-007";

    /// <summary>
    /// ACODE-CFG-008: Config exceeds 1000 keys.
    /// </summary>
    public const string TooManyKeys = "ACODE-CFG-008";

    /// <summary>
    /// ACODE-CFG-009: YAML anchor creates cycle.
    /// </summary>
    public const string CircularReference = "ACODE-CFG-009";

    /// <summary>
    /// ACODE-CFG-010: Required field not present.
    /// </summary>
    public const string RequiredFieldMissing = "ACODE-CFG-010";

    /// <summary>
    /// ACODE-CFG-011: Field has wrong type.
    /// </summary>
    public const string TypeMismatch = "ACODE-CFG-011";

    /// <summary>
    /// ACODE-CFG-012: Value not in allowed set.
    /// </summary>
    public const string EnumViolation = "ACODE-CFG-012";

    /// <summary>
    /// ACODE-CFG-013: Value doesn't match pattern.
    /// </summary>
    public const string PatternViolation = "ACODE-CFG-013";

    /// <summary>
    /// ACODE-CFG-014: Value outside allowed range.
    /// </summary>
    public const string RangeViolation = "ACODE-CFG-014";

    /// <summary>
    /// ACODE-CFG-015: Field not in schema (warning).
    /// </summary>
    public const string UnknownField = "ACODE-CFG-015";

    /// <summary>
    /// ACODE-CFG-016: Field is deprecated (warning).
    /// </summary>
    public const string DeprecatedField = "ACODE-CFG-016";

    /// <summary>
    /// ACODE-CFG-017: Environment variable not set.
    /// </summary>
    public const string EnvVarMissing = "ACODE-CFG-017";

    /// <summary>
    /// ACODE-CFG-018: Environment variable syntax error.
    /// </summary>
    public const string EnvVarError = "ACODE-CFG-018";

    /// <summary>
    /// ACODE-CFG-019: Path attempts directory escape.
    /// </summary>
    public const string PathTraversal = "ACODE-CFG-019";

    /// <summary>
    /// ACODE-CFG-020: Glob pattern is malformed.
    /// </summary>
    public const string InvalidGlob = "ACODE-CFG-020";

    /// <summary>
    /// ACODE-CFG-021: Mode configuration conflict.
    /// </summary>
    public const string ModeViolation = "ACODE-CFG-021";

    /// <summary>
    /// ACODE-CFG-022: Provider not allowed in mode.
    /// </summary>
    public const string ProviderViolation = "ACODE-CFG-022";

    /// <summary>
    /// ACODE-CFG-023: Schema version not recognized.
    /// </summary>
    public const string SchemaVersionUnsupported = "ACODE-CFG-023";

    /// <summary>
    /// ACODE-CFG-024: Cross-field validation failed.
    /// </summary>
    public const string SemanticViolation = "ACODE-CFG-024";

    /// <summary>
    /// ACODE-CFG-025: Potentially dangerous config.
    /// </summary>
    public const string SecurityViolation = "ACODE-CFG-025";
}
