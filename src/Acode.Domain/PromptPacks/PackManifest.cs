namespace Acode.Domain.PromptPacks;

/// <summary>
/// Represents the parsed manifest.yml metadata for a prompt pack.
/// </summary>
/// <param name="FormatVersion">Gets the manifest format version.</param>
/// <param name="Id">Gets the unique pack identifier (kebab-case).</param>
/// <param name="Version">Gets the pack version.</param>
/// <param name="Name">Gets the human-readable pack name.</param>
/// <param name="Description">Gets the optional pack description.</param>
/// <param name="ContentHash">Gets the content hash for integrity verification.</param>
/// <param name="CreatedAt">Gets the pack creation timestamp.</param>
/// <param name="Components">Gets the list of component files in this pack.</param>
/// <param name="Source">Gets the pack source (built-in or user).</param>
/// <param name="PackPath">Gets the file system path to the pack directory.</param>
public sealed record PackManifest(
    string FormatVersion,
    string Id,
    PackVersion Version,
    string Name,
    string? Description,
    ContentHash? ContentHash,
    DateTimeOffset CreatedAt,
    IReadOnlyList<PackComponent> Components,
    PackSource Source,
    string PackPath)
{
    /// <summary>
    /// Validates that a pack ID conforms to the expected format.
    /// </summary>
    /// <param name="id">The pack ID to validate.</param>
    /// <returns><c>true</c> if the ID is valid; otherwise, <c>false</c>.</returns>
    public static bool IsValidPackId(string? id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return false;
        }

        // Must be kebab-case: lowercase letters, numbers, and hyphens
        // Must start with a letter
        // Must not start or end with hyphen
        // Must not have consecutive hyphens
        // Minimum 3 characters, maximum 64
        if (id.Length < 3 || id.Length > 64)
        {
            return false;
        }

        if (!char.IsLetter(id[0]) || !char.IsLower(id[0]))
        {
            return false;
        }

        if (id[^1] == '-')
        {
            return false;
        }

        for (var i = 0; i < id.Length; i++)
        {
            var c = id[i];
            if (!char.IsLetterOrDigit(c) && c != '-')
            {
                return false;
            }

            if (char.IsLetter(c) && !char.IsLower(c))
            {
                return false;
            }

            if (c == '-' && i > 0 && id[i - 1] == '-')
            {
                return false;
            }
        }

        return true;
    }
}
