namespace Acode.Application.Security;

/// <summary>
/// Exception thrown when risk register YAML parsing fails.
/// </summary>
public class RiskRegisterParseException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RiskRegisterParseException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    public RiskRegisterParseException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RiskRegisterParseException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception.</param>
    public RiskRegisterParseException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
