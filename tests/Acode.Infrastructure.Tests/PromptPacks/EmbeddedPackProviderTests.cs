using Acode.Domain.PromptPacks;
using Acode.Infrastructure.PromptPacks;
using FluentAssertions;

namespace Acode.Infrastructure.Tests.PromptPacks;

/// <summary>
/// Tests for <see cref="EmbeddedPackProvider"/>.
/// </summary>
public class EmbeddedPackProviderTests
{
    [Fact]
    public void GetAvailablePackIds_ShouldReturnBuiltInPacks()
    {
        // Arrange
        var loader = new PromptPackLoader(new ContentHasher());
        var provider = new EmbeddedPackProvider(loader, new ContentHasher());

        // Act
        var packIds = provider.GetAvailablePackIds();

        // Assert
        packIds.Should().Contain("acode-standard");
        packIds.Should().Contain("acode-dotnet");
        packIds.Should().Contain("acode-react");
        packIds.Should().HaveCount(3);
    }

    [Theory]
    [InlineData("acode-standard")]
    [InlineData("acode-dotnet")]
    [InlineData("acode-react")]
    public void IsBuiltInPack_WithBuiltInPackId_ShouldReturnTrue(string packId)
    {
        // Arrange
        var loader = new PromptPackLoader(new ContentHasher());
        var provider = new EmbeddedPackProvider(loader, new ContentHasher());

        // Act
        var result = provider.IsBuiltInPack(packId);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsBuiltInPack_WithNonBuiltInPackId_ShouldReturnFalse()
    {
        // Arrange
        var loader = new PromptPackLoader(new ContentHasher());
        var provider = new EmbeddedPackProvider(loader, new ContentHasher());

        // Act
        var result = provider.IsBuiltInPack("custom-pack");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void LoadPack_StandardPack_ShouldLoadSuccessfully()
    {
        // Arrange
        var loader = new PromptPackLoader(new ContentHasher());
        var provider = new EmbeddedPackProvider(loader, new ContentHasher());

        // Act
        var pack = provider.LoadPack("acode-standard");

        // Assert
        pack.Should().NotBeNull();
        pack.Manifest.Id.Should().Be("acode-standard");
        pack.Manifest.Version.ToString().Should().Be("1.0.0");
        pack.Source.Should().Be(PackSource.BuiltIn);

        pack.Components.Should().ContainKey("system.md");
        pack.Components.Should().ContainKey("roles/planner.md");
        pack.Components.Should().ContainKey("roles/coder.md");
        pack.Components.Should().ContainKey("roles/reviewer.md");

        // Verify content is loaded
        pack.Components["system.md"].Content.Should().NotBeEmpty();
        pack.Components["system.md"].Content.Should().Contain("Strict Minimal Diff");
    }

    [Fact]
    public void LoadPack_DotnetPack_ShouldLoadSuccessfully()
    {
        // Arrange
        var loader = new PromptPackLoader(new ContentHasher());
        var provider = new EmbeddedPackProvider(loader, new ContentHasher());

        // Act
        var pack = provider.LoadPack("acode-dotnet");

        // Assert
        pack.Should().NotBeNull();
        pack.Manifest.Id.Should().Be("acode-dotnet");
        pack.Source.Should().Be(PackSource.BuiltIn);

        pack.Components.Should().ContainKey("languages/csharp.md");
        pack.Components.Should().ContainKey("frameworks/aspnetcore.md");

        // Verify C# content
        pack.Components["languages/csharp.md"].Content.Should().Contain("PascalCase");
        pack.Components["languages/csharp.md"].Content.Should().Contain("async");
    }

    [Fact]
    public void LoadPack_ReactPack_ShouldLoadSuccessfully()
    {
        // Arrange
        var loader = new PromptPackLoader(new ContentHasher());
        var provider = new EmbeddedPackProvider(loader, new ContentHasher());

        // Act
        var pack = provider.LoadPack("acode-react");

        // Assert
        pack.Should().NotBeNull();
        pack.Manifest.Id.Should().Be("acode-react");
        pack.Source.Should().Be(PackSource.BuiltIn);

        pack.Components.Should().ContainKey("languages/typescript.md");
        pack.Components.Should().ContainKey("frameworks/react.md");

        // Verify TypeScript content
        pack.Components["languages/typescript.md"].Content.Should().Contain("interface");
        pack.Components["frameworks/react.md"].Content.Should().Contain("useState");
    }

    [Fact]
    public void LoadPack_NonExistentPack_ShouldThrowPackNotFoundException()
    {
        // Arrange
        var loader = new PromptPackLoader(new ContentHasher());
        var provider = new EmbeddedPackProvider(loader, new ContentHasher());

        // Act
        var act = () => provider.LoadPack("non-existent-pack");

        // Assert
        act.Should().Throw<PackNotFoundException>()
            .WithMessage("*non-existent-pack*");
    }

    [Fact]
    public void LoadPack_AllPacks_ContentHashesShouldMatch()
    {
        // This test helps us compute and verify the correct content hashes
        var loader = new PromptPackLoader(new ContentHasher());
        var hasher = new ContentHasher();
        var provider = new EmbeddedPackProvider(loader, hasher);

        var packIds = new[] { "acode-standard", "acode-dotnet", "acode-react" };

        foreach (var packId in packIds)
        {
            // Act
            var pack = provider.LoadPack(packId);

            // Compute expected hash
            var components = new Dictionary<string, string>();
            foreach (var component in pack.Components.Values)
            {
                components[component.Path] = component.Content ?? string.Empty;
            }

            var expectedHash = hasher.Compute(components);

            // Output for manual manifest update (useful during development)
            Console.WriteLine($"{packId} content_hash: {expectedHash.Value}");

            // Assert - Note: This will fail initially with placeholder_will_compute
            // but we'll update the manifests with the computed hashes
            pack.Manifest.ContentHash.Should().Be(
                expectedHash,
                $"Pack {packId} hash should match computed hash. Expected: {expectedHash.Value}");
        }
    }
}
