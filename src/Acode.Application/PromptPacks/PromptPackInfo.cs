using Acode.Domain.PromptPacks;

namespace Acode.Application.PromptPacks;

/// <summary>
/// Metadata about an available prompt pack.
/// </summary>
/// <param name="Id">Unique identifier for the pack.</param>
/// <param name="Version">Semantic version of the pack.</param>
/// <param name="Name">Human-readable name.</param>
/// <param name="Description">Brief description of the pack's purpose.</param>
/// <param name="Source">Where the pack was loaded from (BuiltIn or User).</param>
/// <param name="Author">Optional author name.</param>
public sealed record PromptPackInfo(
    string Id,
    PackVersion Version,
    string Name,
    string Description,
    PackSource Source,
    string? Author = null);
