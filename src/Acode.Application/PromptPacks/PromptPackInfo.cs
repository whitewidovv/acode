using Acode.Domain.PromptPacks;

namespace Acode.Application.PromptPacks;

/// <summary>
/// Summary information about a prompt pack for listing.
/// </summary>
/// <param name="Id">The pack ID.</param>
/// <param name="Version">The pack version.</param>
/// <param name="Name">The pack name.</param>
/// <param name="Source">The pack source (built-in or user).</param>
/// <param name="IsActive">Whether this is the active pack.</param>
/// <param name="Path">The pack path.</param>
public sealed record PromptPackInfo(
    string Id,
    PackVersion Version,
    string Name,
    PackSource Source,
    bool IsActive,
    string Path);
