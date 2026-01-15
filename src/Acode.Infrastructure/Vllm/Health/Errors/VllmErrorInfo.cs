namespace Acode.Infrastructure.Vllm.Health.Errors;

/// <summary>
/// Parsed vLLM error information.
/// </summary>
public sealed class VllmErrorInfo
{
    /// <summary>
    /// Gets the error message.
    /// </summary>
    public string Message { get; init; } = string.Empty;

    /// <summary>
    /// Gets the error type.
    /// </summary>
    public string? Type { get; init; }

    /// <summary>
    /// Gets the error code.
    /// </summary>
    public string? Code { get; init; }

    /// <summary>
    /// Gets the parameter that caused the error.
    /// </summary>
    public string? Param { get; init; }
}
