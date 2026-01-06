using Acode.Infrastructure.PromptPacks;
using FluentAssertions;

namespace Acode.Infrastructure.Tests.PromptPacks;

/// <summary>
/// Helper test to list embedded resources.
/// </summary>
public class EmbeddedResourcesTests
{
    [Fact]
    public void ListEmbeddedResources()
    {
        // Arrange
        var assembly = typeof(Infrastructure.PromptPacks.EmbeddedPackProvider).Assembly;

        // Act
        var resources = assembly.GetManifestResourceNames()
            .Where(r => r.Contains("PromptPacks", StringComparison.Ordinal))
            .OrderBy(r => r)
            .ToList();

        // Assert - Output for debugging
        Console.WriteLine($"Found {resources.Count} PromptPack resources:");
        foreach (var resource in resources)
        {
            Console.WriteLine($"  {resource}");
        }

        // Should have at least some resources
        resources.Should().NotBeEmpty("pack files should be embedded as resources");
    }

    [Fact]
    public void DiagnosePathConversion()
    {
        // Arrange
        var assembly = typeof(Infrastructure.PromptPacks.EmbeddedPackProvider).Assembly;
        var packResourcePrefix = "Acode.Infrastructure.Resources.PromptPacks.acode_standard";

        // Act
        var resources = assembly.GetManifestResourceNames()
            .Where(r => r.Contains("acode_standard", StringComparison.Ordinal))
            .OrderBy(r => r)
            .ToList();

        Console.WriteLine($"Found {resources.Count} resources for acode_standard:");
        foreach (var resource in resources)
        {
            Console.WriteLine($"Resource: {resource}");

            if (resource.StartsWith(packResourcePrefix + ".", StringComparison.Ordinal))
            {
                var relativePath = resource
                    .Substring(packResourcePrefix.Length + 1)
                    .Replace('.', Path.DirectorySeparatorChar);

                Console.WriteLine($"  Raw conversion: '{relativePath}'");

                // Apply file extension fix
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

                Console.WriteLine($"  Final path: '{relativePath}'");
            }
        }

        // Should have resources
        resources.Should().NotBeEmpty();
    }

    [Fact]
    public void ComputeContentHashes()
    {
        // This test computes and outputs the correct content hashes for all packs
        var loader = new PromptPackLoader(new ContentHasher());
        var hasher = new ContentHasher();
        var provider = new EmbeddedPackProvider(loader, hasher);

        var packIds = new[] { "acode-standard", "acode-dotnet", "acode-react" };

        Console.WriteLine("=== COMPUTED CONTENT HASHES ===");
        foreach (var packId in packIds)
        {
            try
            {
                var pack = provider.LoadPack(packId);
                var components = new Dictionary<string, string>();
                foreach (var component in pack.Components.Values)
                {
                    components[component.Path] = component.Content ?? string.Empty;
                }

                var expectedHash = hasher.Compute(components);
                Console.WriteLine($"{packId}: {expectedHash.Value}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{packId}: ERROR - {ex.Message}");
            }
        }

        Console.WriteLine("=== Copy these hashes to the manifest.yml files ===");

        // No assertions - this is just for outputting the hashes
    }
}
