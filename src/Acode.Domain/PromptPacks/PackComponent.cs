namespace Acode.Domain.PromptPacks;

/// <summary>
/// Represents a single component file within a prompt pack.
/// </summary>
/// <param name="Path">Gets the relative file path within the pack directory.</param>
/// <param name="Type">Gets the component type classification.</param>
/// <param name="Metadata">Gets optional key-value metadata associated with this component.</param>
/// <param name="Description">Gets an optional human-readable description.</param>
public sealed record PackComponent(
    string Path,
    ComponentType Type,
    IReadOnlyDictionary<string, string>? Metadata,
    string? Description);
