namespace Acode.Infrastructure.Vllm.StructuredOutput.Configuration;

/// <summary>
/// Configuration validation result.
/// </summary>
public sealed class ConfigurationValidationResult
{
    /// <summary>
    /// Gets a value indicating whether the configuration is valid.
    /// </summary>
    public bool IsValid { get; init; }

    /// <summary>
    /// Gets the validation errors.
    /// </summary>
    public IReadOnlyList<string> Errors { get; init; } = Array.Empty<string>();
}
