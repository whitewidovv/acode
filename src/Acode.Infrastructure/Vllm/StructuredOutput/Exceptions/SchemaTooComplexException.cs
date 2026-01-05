namespace Acode.Infrastructure.Vllm.StructuredOutput.Exceptions;

/// <summary>
/// Exception thrown when a schema exceeds complexity limits.
/// </summary>
[Serializable]
public sealed class SchemaTooComplexException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SchemaTooComplexException"/> class.
    /// </summary>
    public SchemaTooComplexException()
        : base("Schema exceeds complexity limits.")
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SchemaTooComplexException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    public SchemaTooComplexException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SchemaTooComplexException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="errorCode">The error code.</param>
    public SchemaTooComplexException(string message, string errorCode)
        : base(message)
    {
        this.ErrorCode = errorCode;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SchemaTooComplexException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception.</param>
    public SchemaTooComplexException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    /// <summary>
    /// Gets or sets the error code (ACODE-VLM-SO-XXX).
    /// </summary>
    public string? ErrorCode { get; set; }

    /// <summary>
    /// Gets or sets the tool name if applicable.
    /// </summary>
    public string? ToolName { get; set; }

    /// <summary>
    /// Gets or sets the actual depth if a depth limit was exceeded.
    /// </summary>
    public int? ActualDepth { get; set; }

    /// <summary>
    /// Gets or sets the maximum allowed depth.
    /// </summary>
    public int? MaxDepth { get; set; }

    /// <summary>
    /// Gets or sets the actual size if a size limit was exceeded.
    /// </summary>
    public int? ActualSize { get; set; }

    /// <summary>
    /// Gets or sets the maximum allowed size.
    /// </summary>
    public int? MaxSize { get; set; }

    /// <summary>
    /// Gets or sets the JSON path to the deepest element if applicable.
    /// </summary>
    public string? DeepestPath { get; set; }
}
