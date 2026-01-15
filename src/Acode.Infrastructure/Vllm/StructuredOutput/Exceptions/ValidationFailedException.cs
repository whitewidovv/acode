namespace Acode.Infrastructure.Vllm.StructuredOutput.Exceptions;

/// <summary>
/// Exception thrown when output validation fails.
/// </summary>
public sealed class ValidationFailedException : StructuredOutputException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ValidationFailedException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="errors">The validation errors.</param>
    public ValidationFailedException(string message, string[] errors)
        : base(message, "ACODE-VLM-SO-006")
    {
        this.Errors = errors;
    }

    /// <summary>
    /// Gets the validation errors.
    /// </summary>
    public string[] Errors { get; }
}
