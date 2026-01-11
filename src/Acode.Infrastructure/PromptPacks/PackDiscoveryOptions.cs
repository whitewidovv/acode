namespace Acode.Infrastructure.PromptPacks;

/// <summary>
/// Configuration options for pack discovery.
/// </summary>
public sealed class PackDiscoveryOptions
{
    /// <summary>
    /// Gets or sets the path to the user packs directory.
    /// Defaults to ~/.acode/packs on Unix or %USERPROFILE%\.acode\packs on Windows.
    /// </summary>
    public string? UserPacksPath { get; set; }

    /// <summary>
    /// Gets the resolved user packs path, using defaults if not configured.
    /// </summary>
    /// <returns>The resolved path to the user packs directory.</returns>
    public string GetResolvedUserPacksPath()
    {
        if (!string.IsNullOrWhiteSpace(UserPacksPath))
        {
            return UserPacksPath;
        }

        var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        return Path.Combine(userProfile, ".acode", "packs");
    }
}
