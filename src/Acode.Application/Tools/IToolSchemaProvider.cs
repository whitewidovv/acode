namespace Acode.Application.Tools;

using Acode.Domain.Models.Inference;

/// <summary>
/// Interface for components that provide tool schema definitions.
/// </summary>
/// <remarks>
/// FR-007: Tool Schema Registry requirements.
/// AC-066 to AC-070: Provider registration requirements.
/// Providers are loaded in order of their Order property (lowest first).
/// </remarks>
public interface IToolSchemaProvider
{
    /// <summary>
    /// Gets the name of this provider.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the version of this provider following semver format.
    /// </summary>
    string Version { get; }

    /// <summary>
    /// Gets the loading order. Lower values are loaded first.
    /// </summary>
    /// <remarks>
    /// Core tools should use 0, plugins should use 100+.
    /// </remarks>
    int Order { get; }

    /// <summary>
    /// Gets all tool definitions provided by this provider.
    /// </summary>
    /// <returns>An enumerable of tool definitions.</returns>
    IEnumerable<ToolDefinition> GetToolDefinitions();
}
