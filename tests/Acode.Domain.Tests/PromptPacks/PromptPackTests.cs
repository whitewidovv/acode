using Acode.Domain.PromptPacks;
using FluentAssertions;
using Xunit;

namespace Acode.Domain.Tests.PromptPacks;

/// <summary>
/// Tests for <see cref="PromptPack"/> record.
/// </summary>
public class PromptPackTests
{
    [Fact]
    public void Constructor_WithManifest_ShouldSucceed()
    {
        // Arrange
        var manifest = new PackManifest
        {
            FormatVersion = "1.0",
            Id = "test-pack",
            Version = PackVersion.Parse("1.0.0"),
            Name = "Test Pack",
            Description = "Test description",
            ContentHash = new ContentHash("a".PadRight(64, 'b')),
            CreatedAt = DateTime.UtcNow,
            Components = new List<PackComponent>()
        };

        // Act
        var pack = new PromptPack
        {
            Manifest = manifest
        };

        // Assert
        pack.Manifest.Should().Be(manifest);
        pack.Components.Should().BeEmpty();
        pack.Source.Should().Be(PackSource.User);
    }

    [Fact]
    public void Constructor_WithComponentsDictionary_ShouldSucceed()
    {
        // Arrange
        var manifest = new PackManifest
        {
            FormatVersion = "1.0",
            Id = "test-pack",
            Version = PackVersion.Parse("1.0.0"),
            Name = "Test Pack",
            Description = "Test",
            ContentHash = new ContentHash("c".PadRight(64, 'd')),
            CreatedAt = DateTime.UtcNow,
            Components = new List<PackComponent>()
        };

        var components = new Dictionary<string, PackComponent>
        {
            ["system.md"] = new PackComponent
            {
                Path = "system.md",
                Type = ComponentType.System,
                Content = "System prompt content"
            }
        };

        // Act
        var pack = new PromptPack
        {
            Manifest = manifest,
            Components = components
        };

        // Assert
        pack.Components.Should().ContainKey("system.md");
        pack.Components["system.md"].Content.Should().Be("System prompt content");
    }

    [Fact]
    public void Source_BuiltIn_ShouldWork()
    {
        // Arrange
        var manifest = new PackManifest
        {
            FormatVersion = "1.0",
            Id = "acode-standard",
            Version = PackVersion.Parse("1.0.0"),
            Name = "Acode Standard",
            Description = "Built-in pack",
            ContentHash = new ContentHash("e".PadRight(64, 'f')),
            CreatedAt = DateTime.UtcNow,
            Components = new List<PackComponent>()
        };

        // Act
        var pack = new PromptPack
        {
            Manifest = manifest,
            Source = PackSource.BuiltIn
        };

        // Assert
        pack.Source.Should().Be(PackSource.BuiltIn);
    }

    [Fact]
    public void Components_ImmutableDictionary_ShouldWork()
    {
        // Arrange
        var manifest = new PackManifest
        {
            FormatVersion = "1.0",
            Id = "test",
            Version = PackVersion.Parse("1.0.0"),
            Name = "Test",
            Description = "Test",
            ContentHash = new ContentHash("1".PadRight(64, '2')),
            CreatedAt = DateTime.UtcNow,
            Components = new List<PackComponent>()
        };

        var components1 = new Dictionary<string, PackComponent>
        {
            ["first.md"] = new PackComponent { Path = "first.md", Type = ComponentType.System }
        };

        var pack1 = new PromptPack
        {
            Manifest = manifest,
            Components = components1
        };

        // Act
        var components2 = new Dictionary<string, PackComponent>
        {
            ["second.md"] = new PackComponent { Path = "second.md", Type = ComponentType.Custom }
        };

        var pack2 = pack1 with { Components = components2 };

        // Assert
        pack1.Components.Should().ContainKey("first.md");
        pack1.Components.Should().NotContainKey("second.md");
        pack2.Components.Should().ContainKey("second.md");
        pack2.Components.Should().NotContainKey("first.md");
    }

    [Fact]
    public void Equality_SameManifest_ShouldBeEqual()
    {
        // Arrange
        var manifest = new PackManifest
        {
            FormatVersion = "1.0",
            Id = "test-pack",
            Version = PackVersion.Parse("1.0.0"),
            Name = "Test Pack",
            Description = "Description",
            ContentHash = new ContentHash("3".PadRight(64, '4')),
            CreatedAt = new DateTime(2024, 1, 15, 10, 30, 0, DateTimeKind.Utc),
            Components = new List<PackComponent>()
        };

        var components = new Dictionary<string, PackComponent>();

        var pack1 = new PromptPack
        {
            Manifest = manifest,
            Components = components
        };

        var pack2 = new PromptPack
        {
            Manifest = manifest,
            Components = components
        };

        // Act & Assert
        pack1.Should().Be(pack2);
    }

    [Fact]
    public void Equality_DifferentManifest_ShouldNotBeEqual()
    {
        // Arrange
        var manifest1 = new PackManifest
        {
            FormatVersion = "1.0",
            Id = "pack-one",
            Version = PackVersion.Parse("1.0.0"),
            Name = "Pack One",
            Description = "Description",
            ContentHash = new ContentHash("5".PadRight(64, '6')),
            CreatedAt = DateTime.UtcNow,
            Components = new List<PackComponent>()
        };

        var manifest2 = new PackManifest
        {
            FormatVersion = "1.0",
            Id = "pack-two",
            Version = PackVersion.Parse("1.0.0"),
            Name = "Pack Two",
            Description = "Description",
            ContentHash = new ContentHash("7".PadRight(64, '8')),
            CreatedAt = DateTime.UtcNow,
            Components = new List<PackComponent>()
        };

        var pack1 = new PromptPack { Manifest = manifest1 };
        var pack2 = new PromptPack { Manifest = manifest2 };

        // Act & Assert
        pack1.Should().NotBe(pack2);
    }
}
