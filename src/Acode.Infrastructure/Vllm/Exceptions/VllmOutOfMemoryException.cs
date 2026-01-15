namespace Acode.Infrastructure.Vllm.Exceptions;

/// <summary>
/// Exception thrown when vLLM encounters CUDA out of memory error.
/// </summary>
public sealed class VllmOutOfMemoryException : VllmException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="VllmOutOfMemoryException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    public VllmOutOfMemoryException(string message)
        : base("ACODE-VLM-013", message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="VllmOutOfMemoryException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception.</param>
    public VllmOutOfMemoryException(string message, Exception innerException)
        : base("ACODE-VLM-013", message, innerException)
    {
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Transient because may succeed after memory is freed or smaller request is made.
    /// </remarks>
    public override bool IsTransient => true;
}
