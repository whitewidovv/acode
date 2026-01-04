namespace Acode.Infrastructure.Vllm.Exceptions;

/// <summary>
/// Exception thrown when unable to parse a vLLM response.
/// </summary>
public sealed class VllmParseException : VllmException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="VllmParseException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    public VllmParseException(string message)
        : base("ACODE-VLM-006", message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="VllmParseException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception.</param>
    public VllmParseException(string message, Exception innerException)
        : base("ACODE-VLM-006", message, innerException)
    {
    }

    /// <inheritdoc/>
    public override bool IsTransient => false;
}
