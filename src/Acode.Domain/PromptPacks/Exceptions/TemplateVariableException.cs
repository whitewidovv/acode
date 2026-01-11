namespace Acode.Domain.PromptPacks.Exceptions;

/// <summary>
/// Exception thrown when template variable processing fails.
/// </summary>
/// <remarks>
/// This exception is thrown in the following scenarios:
/// - Variable value exceeds maximum allowed length.
/// - Circular reference detected during recursive expansion.
/// - Invalid variable name format.
/// </remarks>
public sealed class TemplateVariableException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TemplateVariableException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    public TemplateVariableException(string message)
        : base(message)
    {
        ErrorCode = "ACODE-PRM-005";
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TemplateVariableException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="variableName">The variable name that caused the exception.</param>
    public TemplateVariableException(string message, string variableName)
        : base(message)
    {
        ErrorCode = "ACODE-PRM-005";
        VariableName = variableName;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TemplateVariableException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="errorCode">The specific error code.</param>
    /// <param name="variableName">The variable name that caused the exception.</param>
    public TemplateVariableException(string message, string errorCode, string? variableName)
        : base(message)
    {
        ErrorCode = errorCode;
        VariableName = variableName;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TemplateVariableException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception.</param>
    public TemplateVariableException(string message, Exception innerException)
        : base(message, innerException)
    {
        ErrorCode = "ACODE-PRM-005";
    }

    /// <summary>
    /// Gets the error code for this exception.
    /// </summary>
    public string ErrorCode { get; }

    /// <summary>
    /// Gets the variable name that caused the exception, if applicable.
    /// </summary>
    public string? VariableName { get; }
}
