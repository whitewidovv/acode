namespace Acode.Infrastructure.Vllm.StructuredOutput.Fallback;

/// <summary>
/// Handles fallback logic for structured output failures.
/// </summary>
/// <remarks>
/// FR-062 through FR-071: Fallback handling orchestration.
/// </remarks>
public sealed class FallbackHandler
{
    private readonly OutputValidator _validator;

    /// <summary>
    /// Initializes a new instance of the <see cref="FallbackHandler"/> class.
    /// </summary>
    /// <param name="validator">The output validator to use for fallback validation.</param>
    public FallbackHandler(OutputValidator validator)
    {
        this._validator = validator ?? throw new ArgumentNullException(nameof(validator));
    }

    /// <summary>
    /// Handles a validation failure with fallback logic.
    /// </summary>
    /// <param name="context">The fallback context containing failure information.</param>
    /// <param name="schema">The JSON schema as string for validation.</param>
    /// <returns>A result indicating fallback handling outcome.</returns>
    public FallbackResult Handle(FallbackContext context, string schema)
    {
        if (context == null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        if (string.IsNullOrEmpty(schema))
        {
            throw new ArgumentException("Schema must not be empty", nameof(schema));
        }

        // Check if we've exceeded max attempts
        if (context.FallbackAttempts >= context.MaxFallbackAttempts)
        {
            return new FallbackResult
            {
                Success = false,
                Reason = FallbackReason.MaxAttemptsExceeded,
                Message = $"Maximum fallback attempts ({context.MaxFallbackAttempts}) exceeded",
            };
        }

        context.FallbackAttempts++;

        // If we have invalid output, try extraction first
        if (!string.IsNullOrEmpty(context.InvalidOutput))
        {
            var extracted = this._validator.TryExtractValidJson(context.InvalidOutput);
            if (!string.IsNullOrEmpty(extracted))
            {
                var validationResult = this._validator.Validate(extracted, schema);
                if (validationResult.IsValid)
                {
                    return new FallbackResult
                    {
                        Success = true,
                        Reason = FallbackReason.ExtractionSucceeded,
                        Message = "Successfully extracted valid JSON from invalid output",
                        CorrectedOutput = extracted,
                    };
                }
            }
        }

        // Determine next action
        if (context.ShouldRegenerateOutput)
        {
            return new FallbackResult
            {
                Success = false,
                Reason = FallbackReason.RegenerationRequired,
                Message = "Output requires regeneration",
                ShouldRetry = true,
            };
        }

        // Final fallback: mark as handled but unsuccessful
        return new FallbackResult
        {
            Success = false,
            Reason = FallbackReason.Unrecoverable,
            Message = "Unable to recover from structured output failure",
        };
    }

    /// <summary>
    /// Validates if output meets fallback requirements.
    /// </summary>
    /// <param name="output">The output to validate.</param>
    /// <param name="schema">The schema for validation.</param>
    /// <returns>True if output is valid, false otherwise.</returns>
    public bool Validate(string output, string schema)
    {
        if (string.IsNullOrEmpty(output) || string.IsNullOrEmpty(schema))
        {
            return false;
        }

        var result = this._validator.Validate(output, schema);
        return result.IsValid;
    }
}

/// <summary>
/// Result of a fallback handling operation.
/// </summary>
public sealed class FallbackResult
{
    /// <summary>
    /// Gets or sets a value indicating whether fallback handling was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the reason for the fallback result.
    /// </summary>
    public FallbackReason Reason { get; set; }

    /// <summary>
    /// Gets or sets a message describing the result.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the corrected output (if extraction succeeded).
    /// </summary>
    public string? CorrectedOutput { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the client should retry the request.
    /// </summary>
    public bool ShouldRetry { get; set; }
}
