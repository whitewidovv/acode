namespace Acode.Infrastructure.Vllm.Exceptions;

/// <summary>
/// Exception thrown when the requested model is not found on the vLLM server.
/// </summary>
public sealed class VllmModelNotFoundException : VllmException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="VllmModelNotFoundException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    public VllmModelNotFoundException(string message)
        : base("ACODE-VLM-003", message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="VllmModelNotFoundException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception.</param>
    public VllmModelNotFoundException(string message, Exception innerException)
        : base("ACODE-VLM-003", message, innerException)
    {
    }

    /// <inheritdoc/>
    public override bool IsTransient => false;

    /// <summary>
    /// Gets or sets the model ID that was not found.
    /// </summary>
    public string? ModelId { get; set; }
}
