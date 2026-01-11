namespace Acode.Application.Configuration;

/// <summary>
/// Standard error codes for configuration validation.
/// </summary>
public static class ConfigErrorCodes
{
    // File errors (CFG001-CFG009)
    public const string FileNotFound = "CFG001";
    public const string FileUnreadable = "CFG002";
    public const string FileTooBig = "CFG003";
    public const string EncodingError = "CFG004";

    // Parse errors (CFG010-CFG019)
    public const string YamlParseError = "CFG010";
    public const string InvalidYamlSyntax = "CFG011";
    public const string CircularReference = "CFG012";
    public const string TooManyAnchors = "CFG013";

    // Schema errors (CFG020-CFG029)
    public const string SchemaViolation = "CFG020";
    public const string MissingRequiredField = "CFG021";
    public const string InvalidFieldType = "CFG022";
    public const string InvalidFieldValue = "CFG023";
    public const string UnknownField = "CFG024";

    // Semantic errors (CFG030-CFG049)
    public const string UnsupportedVersion = "CFG030";
    public const string InvalidMode = "CFG031";
    public const string InvalidProvider = "CFG032";
    public const string InvalidEndpoint = "CFG033";
    public const string PathTraversal = "CFG034";
    public const string PathNotFound = "CFG035";
    public const string InvalidCommand = "CFG036";
    public const string CircularDependency = "CFG037";
    public const string InvalidNetworkAllowlist = "CFG038";

    // Interpolation errors (CFG050-CFG059)
    public const string UndefinedVariable = "CFG050";
    public const string InvalidVariableSyntax = "CFG051";
    public const string CircularVariableReference = "CFG052";

    // Security errors (CFG060-CFG069)
    public const string SecretInPlaintext = "CFG060";
    public const string InsecureEndpoint = "CFG061";
    public const string DeniedHost = "CFG062";
}
