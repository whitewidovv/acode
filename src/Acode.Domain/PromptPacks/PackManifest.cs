namespace Acode.Domain.PromptPacks;

/// <summary>
/// Represents the manifest metadata for a prompt pack.
/// </summary>
/// <remarks>
/// The manifest (manifest.yml) defines pack metadata including version, components, and content hash.
/// All packs must have a valid manifest to be loaded.
/// </remarks>
public sealed record PackManifest
{
    /// <summary>
    /// Gets the manifest format version.
    /// </summary>
    /// <remarks>
    /// Current version is "1.0". Used for future compatibility and migration.
    /// </remarks>
    public required string FormatVersion { get; init; }

    /// <summary>
    /// Gets the unique pack identifier.
    /// </summary>
    /// <remarks>
    /// Must be lowercase with hyphens only. Examples: "acode-standard", "my-custom-pack".
    /// Pack ID should be stable across versions.
    /// </remarks>
    public required string Id { get; init; }

    /// <summary>
    /// Gets the pack version following Semantic Versioning 2.0.
    /// </summary>
    public required PackVersion Version { get; init; }

    /// <summary>
    /// Gets the human-readable pack name.
    /// </summary>
    /// <remarks>
    /// Display name shown in CLI and UI. Example: "Acode Standard Pack".
    /// </remarks>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the pack description.
    /// </summary>
    /// <remarks>
    /// Brief description of the pack's purpose and use cases.
    /// </remarks>
    public required string Description { get; init; }

    /// <summary>
    /// Gets the SHA-256 content hash of all pack components.
    /// </summary>
    /// <remarks>
    /// Used for integrity verification. Hash mismatch triggers warnings in logs.
    /// </remarks>
    public required ContentHash ContentHash { get; init; }

    /// <summary>
    /// Gets the pack creation timestamp (ISO 8601 UTC).
    /// </summary>
    public required DateTime CreatedAt { get; init; }

    /// <summary>
    /// Gets the pack last updated timestamp (ISO 8601 UTC).
    /// </summary>
    /// <remarks>
    /// Optional. Only present if pack has been updated since creation.
    /// </remarks>
    public DateTime? UpdatedAt { get; init; }

    /// <summary>
    /// Gets the pack author name or email.
    /// </summary>
    /// <remarks>
    /// Optional. Used for attribution and support contacts.
    /// </remarks>
    public string? Author { get; init; }

    /// <summary>
    /// Gets the list of components included in this pack.
    /// </summary>
    /// <remarks>
    /// Each component references a file within the pack directory.
    /// Components are loaded in order during composition.
    /// </remarks>
    public required IReadOnlyList<PackComponent> Components { get; init; }
}
