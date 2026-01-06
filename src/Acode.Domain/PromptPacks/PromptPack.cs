namespace Acode.Domain.PromptPacks;

/// <summary>
/// Represents a complete prompt pack with manifest and loaded components.
/// </summary>
/// <remarks>
/// PromptPack combines the manifest metadata with the actual loaded component content.
/// Components are stored in a dictionary keyed by their relative path for efficient lookup.
/// </remarks>
public sealed record PromptPack
{
    /// <summary>
    /// Gets the pack manifest containing metadata.
    /// </summary>
    public required PackManifest Manifest { get; init; }

    /// <summary>
    /// Gets the loaded pack components keyed by their relative path.
    /// </summary>
    /// <remarks>
    /// Dictionary keys match the Path property of each PackComponent.
    /// Used for efficient component lookup during prompt composition.
    /// </remarks>
    public IReadOnlyDictionary<string, PackComponent> Components { get; init; } =
        new Dictionary<string, PackComponent>();

    /// <summary>
    /// Gets the pack source (built-in or user-provided).
    /// </summary>
    /// <remarks>
    /// Defaults to User. Built-in packs are loaded from embedded resources.
    /// </remarks>
    public PackSource Source { get; init; } = PackSource.User;
}
