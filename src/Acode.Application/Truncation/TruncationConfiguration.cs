namespace Acode.Application.Truncation;

/// <summary>
/// Configuration for the truncation system including tool-specific overrides.
/// </summary>
public sealed class TruncationConfiguration
{
    /// <summary>
    /// Gets or sets the default truncation limits.
    /// </summary>
    public TruncationLimits DefaultLimits { get; set; } = new();

    /// <summary>
    /// Gets or sets the default truncation strategy.
    /// </summary>
    public TruncationStrategy DefaultStrategy { get; set; } = TruncationStrategy.HeadTail;

    /// <summary>
    /// Gets tool-specific limit overrides.
    /// </summary>
    public Dictionary<string, TruncationLimits> ToolLimits { get; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Gets tool-specific strategy overrides.
    /// </summary>
    public Dictionary<string, TruncationStrategy> ToolStrategies { get; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Gets or sets the artifact storage directory path.
    /// </summary>
    public string ArtifactStoragePath { get; set; } = ".acode/artifacts";

    /// <summary>
    /// Gets or sets a value indicating whether to clean up artifacts on session end.
    /// </summary>
    public bool CleanupOnSessionEnd { get; set; } = true;

    /// <summary>
    /// Creates a default configuration with recommended tool-specific settings.
    /// </summary>
    /// <returns>A new configuration with defaults.</returns>
    public static TruncationConfiguration CreateDefault()
    {
        var config = new TruncationConfiguration();

        // Command execution - use tail strategy (recent output matters most)
        config.ToolStrategies["execute_command"] = TruncationStrategy.Tail;
        config.ToolStrategies["execute_script"] = TruncationStrategy.Tail;

        // File reading - use head+tail (imports and main both matter)
        config.ToolStrategies["read_file"] = TruncationStrategy.HeadTail;

        // Directory/search - use element strategy (structured data)
        config.ToolStrategies["list_directory"] = TruncationStrategy.Element;
        config.ToolStrategies["search_files"] = TruncationStrategy.Element;

        // Git operations - use head+tail
        config.ToolStrategies["git_diff"] = TruncationStrategy.HeadTail;
        config.ToolStrategies["git_log"] = TruncationStrategy.Tail;
        config.ToolStrategies["git_status"] = TruncationStrategy.HeadTail;

        return config;
    }

    /// <summary>
    /// Gets the limits for a specific tool, falling back to defaults if not overridden.
    /// </summary>
    /// <param name="toolName">The tool name.</param>
    /// <returns>The applicable limits.</returns>
    public TruncationLimits GetLimitsForTool(string toolName)
    {
        return ToolLimits.TryGetValue(toolName, out var limits) ? limits : DefaultLimits;
    }

    /// <summary>
    /// Gets the strategy for a specific tool, falling back to defaults if not overridden.
    /// </summary>
    /// <param name="toolName">The tool name.</param>
    /// <returns>The applicable strategy.</returns>
    public TruncationStrategy GetStrategyForTool(string toolName)
    {
        return ToolStrategies.TryGetValue(toolName, out var strategy) ? strategy : DefaultStrategy;
    }
}
