namespace Acode.Infrastructure.Vllm.StructuredOutput.Schema;

/// <summary>
/// Result of schema validation.
/// </summary>
public sealed class SchemaValidationResult
{
    /// <summary>
    /// Gets a value indicating whether the schema is valid for vLLM.
    /// </summary>
    public bool IsValid { get; init; }

    /// <summary>
    /// Gets the validation errors.
    /// </summary>
    public IReadOnlyList<string> Errors { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Gets the validation warnings.
    /// </summary>
    public IReadOnlyList<string> Warnings { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Gets the calculated schema depth.
    /// </summary>
    public int Depth { get; init; }

    /// <summary>
    /// Gets the schema size in bytes.
    /// </summary>
    public int SizeBytes { get; init; }
}
