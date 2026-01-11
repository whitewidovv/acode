namespace Acode.Domain.PromptPacks;

/// <summary>
/// Represents a loaded prompt pack with component content.
/// </summary>
/// <param name="Id">Gets the pack ID.</param>
/// <param name="Version">Gets the pack version.</param>
/// <param name="Name">Gets the pack name.</param>
/// <param name="Description">Gets the pack description.</param>
/// <param name="Source">Gets the pack source (built-in or user).</param>
/// <param name="PackPath">Gets the pack directory path.</param>
/// <param name="ContentHash">Gets the content hash for integrity verification.</param>
/// <param name="Components">Gets the loaded components with content.</param>
public sealed record PromptPack(
    string Id,
    PackVersion Version,
    string Name,
    string? Description,
    PackSource Source,
    string PackPath,
    ContentHash? ContentHash,
    IReadOnlyList<LoadedComponent> Components)
{
    /// <summary>
    /// Gets a component by path.
    /// </summary>
    /// <param name="path">The component path.</param>
    /// <returns>The component if found; otherwise, <c>null</c>.</returns>
    public LoadedComponent? GetComponent(string path)
    {
        return Components.FirstOrDefault(c =>
            string.Equals(c.Path, path, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Gets components by type.
    /// </summary>
    /// <param name="type">The component type.</param>
    /// <returns>The matching components.</returns>
    public IEnumerable<LoadedComponent> GetComponentsByType(ComponentType type)
    {
        return Components.Where(c => c.Type == type);
    }

    /// <summary>
    /// Gets the system prompt component if present.
    /// </summary>
    /// <returns>The system component content; otherwise, <c>null</c>.</returns>
    public string? GetSystemPrompt()
    {
        return Components.FirstOrDefault(c => c.Type == ComponentType.System)?.Content;
    }
}

/// <summary>
/// Represents a loaded component with its content.
/// </summary>
/// <param name="Path">Gets the component path.</param>
/// <param name="Type">Gets the component type.</param>
/// <param name="Content">Gets the component content.</param>
/// <param name="Metadata">Gets the component metadata.</param>
public sealed record LoadedComponent(
    string Path,
    ComponentType Type,
    string Content,
    IReadOnlyDictionary<string, string>? Metadata);
