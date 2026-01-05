namespace Acode.Infrastructure.Vllm.StructuredOutput.Configuration;

/// <summary>
/// Schema processing configuration.
/// </summary>
public sealed class SchemaConfiguration
{
    /// <summary>
    /// Gets or sets the maximum nesting depth for schemas.
    /// Default: 10.
    /// </summary>
    public int MaxDepth { get; set; } = 10;

    /// <summary>
    /// Gets or sets the maximum schema size in bytes.
    /// Default: 65536 (64KB).
    /// </summary>
    public int MaxSizeBytes { get; set; } = 65536;

    /// <summary>
    /// Gets or sets the maximum number of enum elements.
    /// Default: 100.
    /// </summary>
    public int MaxEnumElements { get; set; } = 100;

    /// <summary>
    /// Gets or sets the schema cache size.
    /// Default: 100.
    /// </summary>
    public int CacheSize { get; set; } = 100;

    /// <summary>
    /// Gets or sets the processing timeout in milliseconds.
    /// Default: 100.
    /// </summary>
    public int ProcessingTimeoutMs { get; set; } = 100;
}
