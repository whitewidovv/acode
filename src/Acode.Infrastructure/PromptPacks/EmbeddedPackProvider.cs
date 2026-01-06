using System.Reflection;
using Acode.Application.PromptPacks;
using Acode.Domain.PromptPacks;

namespace Acode.Infrastructure.PromptPacks;

/// <summary>
/// Provides access to built-in prompt packs embedded as resources.
/// </summary>
public sealed class EmbeddedPackProvider
{
    private const string ResourcePrefix = "Acode.Infrastructure.Resources.PromptPacks";
    private static readonly string[] BuiltInPackIds = { "acode-standard", "acode-dotnet", "acode-react" };

    private readonly IPromptPackLoader _loader;
    private readonly IContentHasher _hasher;
    private readonly Assembly _assembly;

    /// <summary>
    /// Initializes a new instance of the <see cref="EmbeddedPackProvider"/> class.
    /// </summary>
    /// <param name="loader">Pack loader for parsing pack files.</param>
    /// <param name="hasher">Content hasher for verification.</param>
    public EmbeddedPackProvider(IPromptPackLoader loader, IContentHasher hasher)
    {
        ArgumentNullException.ThrowIfNull(loader);
        ArgumentNullException.ThrowIfNull(hasher);

        _loader = loader;
        _hasher = hasher;
        _assembly = typeof(EmbeddedPackProvider).Assembly;
    }

    /// <summary>
    /// Gets the list of available built-in pack IDs.
    /// </summary>
    /// <returns>Array of pack IDs.</returns>
    public string[] GetAvailablePackIds()
    {
        return BuiltInPackIds.ToArray();
    }

    /// <summary>
    /// Checks if a pack ID is a built-in pack.
    /// </summary>
    /// <param name="packId">Pack ID to check.</param>
    /// <returns>True if the pack is built-in, false otherwise.</returns>
    public bool IsBuiltInPack(string packId)
    {
        return BuiltInPackIds.Contains(packId);
    }

    /// <summary>
    /// Extracts an embedded pack to a temporary directory and loads it.
    /// </summary>
    /// <param name="packId">Pack ID to load.</param>
    /// <returns>Loaded prompt pack.</returns>
    /// <exception cref="PackNotFoundException">Thrown when pack is not found in embedded resources.</exception>
    public PromptPack LoadPack(string packId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(packId);

        if (!IsBuiltInPack(packId))
        {
            throw new PackNotFoundException(packId);
        }

        // Extract pack to temporary directory
        var tempPackDir = ExtractPackToTemp(packId);

        try
        {
            // Load pack using standard loader
            var pack = _loader.LoadPack(tempPackDir);

            // Ensure source is marked as BuiltIn
            return pack with { Source = PackSource.BuiltIn };
        }
        finally
        {
            // Clean up temporary files
            if (Directory.Exists(tempPackDir))
            {
                try
                {
                    Directory.Delete(tempPackDir, recursive: true);
                }
                catch
                {
                    // Ignore cleanup errors
                }
            }
        }
    }

    private string ExtractPackToTemp(string packId)
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "acode", "packs", packId, Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);

        // MSBuild converts hyphens to underscores in embedded resource names
        // e.g., "acode-dotnet" directory becomes "acode_dotnet" in resource names
        var resourcePackId = packId.Replace("-", "_", StringComparison.Ordinal);
        var packResourcePrefix = $"{ResourcePrefix}.{resourcePackId}";

        // Get all resource names for this pack
        var resourceNames = _assembly.GetManifestResourceNames()
            .Where(name => name.StartsWith(packResourcePrefix, StringComparison.Ordinal))
            .ToList();

        if (resourceNames.Count == 0)
        {
            throw new PackNotFoundException(packId);
        }

        foreach (var resourceName in resourceNames)
        {
            // Extract relative path from resource name
            // e.g., "Acode.Infrastructure.Resources.PromptPacks.acode-standard.manifest.yml"
            //    -> "manifest.yml"
            // e.g., "Acode.Infrastructure.Resources.PromptPacks.acode-standard.roles.coder.md"
            //    -> "roles/coder.md"
            var relativePath = resourceName
                .Substring(packResourcePrefix.Length + 1) // +1 for the dot
                .Replace('.', Path.DirectorySeparatorChar);

            // Special handling for file extensions - restore the last dot
            // Path.DirectorySeparatorChar + "md" = "/md" (3 characters)
            // Path.DirectorySeparatorChar + "yml" = "/yml" (4 characters)
            if (relativePath.EndsWith(Path.DirectorySeparatorChar + "md", StringComparison.Ordinal))
            {
                relativePath = relativePath.Substring(0, relativePath.Length - 3) + ".md";
            }
            else if (relativePath.EndsWith(Path.DirectorySeparatorChar + "yml", StringComparison.Ordinal))
            {
                relativePath = relativePath.Substring(0, relativePath.Length - 4) + ".yml";
            }

            var targetPath = Path.Combine(tempDir, relativePath);

            // Ensure directory exists
            var targetDir = Path.GetDirectoryName(targetPath);
            if (!string.IsNullOrEmpty(targetDir))
            {
                Directory.CreateDirectory(targetDir);
            }

            // Extract resource to file
            using var resourceStream = _assembly.GetManifestResourceStream(resourceName);
            if (resourceStream == null)
            {
                continue;
            }

            using var fileStream = File.Create(targetPath);
            resourceStream.CopyTo(fileStream);
        }

        return tempDir;
    }
}
