namespace Acode.Infrastructure.Heuristics;

/// <summary>
/// Context for override resolution containing potential override values.
/// </summary>
public sealed class OverrideContext
{
    /// <summary>
    /// Gets or sets the request-level override from --model flag.
    /// </summary>
    public string? RequestOverride { get; set; }

    /// <summary>
    /// Gets or sets the session-level override from environment or CLI.
    /// </summary>
    public string? SessionOverride { get; set; }

    /// <summary>
    /// Gets or sets the config-level override.
    /// </summary>
    public string? ConfigOverride { get; set; }
}
