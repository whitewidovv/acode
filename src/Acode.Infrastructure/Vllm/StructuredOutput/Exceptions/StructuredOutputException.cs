namespace Acode.Infrastructure.Vllm.StructuredOutput.Exceptions;

using Acode.Infrastructure.Vllm.Exceptions;

/// <summary>
/// Base exception for structured output enforcement errors.
/// </summary>
public class StructuredOutputException : VllmException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="StructuredOutputException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="errorCode">The error code (ACODE-VLM-SO-XXX).</param>
    public StructuredOutputException(string message, string errorCode)
        : base(errorCode, message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="StructuredOutputException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="errorCode">The error code (ACODE-VLM-SO-XXX).</param>
    /// <param name="innerException">The inner exception.</param>
    public StructuredOutputException(string message, string errorCode, Exception innerException)
        : base(errorCode, message, innerException)
    {
    }
}
