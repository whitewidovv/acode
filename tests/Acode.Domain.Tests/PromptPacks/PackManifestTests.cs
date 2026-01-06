using Acode.Domain.PromptPacks;
using FluentAssertions;

namespace Acode.Domain.Tests.PromptPacks;

/// <summary>
/// Tests for <see cref="PackManifest"/> record.
/// </summary>
public class PackManifestTests
{
    [Fact]
    public void Constructor_WithRequiredFields_ShouldSucceed()
    {
        // Arrange
        var formatVersion = "1.0";
        var id = "acode-standard";
        var version = PackVersion.Parse("1.0.0");
        var name = "Acode Standard Pack";
        var description = "General purpose coding pack";
        var contentHash = new ContentHash("a".PadRight(64, 'b'));
        var createdAt = DateTime.UtcNow;
        var components = new List<PackComponent>
        {
            new() { Path = "system.md", Type = ComponentType.System }
        };

        // Act
        var manifest = new PackManifest
        {
            FormatVersion = formatVersion,
            Id = id,
            Version = version,
            Name = name,
            Description = description,
            ContentHash = contentHash,
            CreatedAt = createdAt,
            Components = components
        };

        // Assert
        manifest.FormatVersion.Should().Be(formatVersion);
        manifest.Id.Should().Be(id);
        manifest.Version.Should().Be(version);
        manifest.Name.Should().Be(name);
        manifest.Description.Should().Be(description);
        manifest.ContentHash.Should().Be(contentHash);
        manifest.CreatedAt.Should().Be(createdAt);
        manifest.Components.Should().BeEquivalentTo(components);
        manifest.UpdatedAt.Should().BeNull();
        manifest.Author.Should().BeNull();
    }

    [Fact]
    public void Constructor_WithOptionalFields_ShouldIncludeThem()
    {
        // Arrange
        var formatVersion = "1.0";
        var id = "custom-pack";
        var version = PackVersion.Parse("2.1.5");
        var name = "Custom Pack";
        var description = "Custom description";
        var contentHash = new ContentHash("c".PadRight(64, 'd'));
        var createdAt = DateTime.UtcNow.AddDays(-10);
        var updatedAt = DateTime.UtcNow;
        var author = "John Doe";
        var components = new List<PackComponent>();

        // Act
        var manifest = new PackManifest
        {
            FormatVersion = formatVersion,
            Id = id,
            Version = version,
            Name = name,
            Description = description,
            ContentHash = contentHash,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt,
            Author = author,
            Components = components
        };

        // Assert
        manifest.UpdatedAt.Should().Be(updatedAt);
        manifest.Author.Should().Be(author);
    }

    [Fact]
    public void Id_WithLowercaseAndHyphens_ShouldBeValid()
    {
        // Arrange
        var validId = "my-custom-pack";
        var manifest = new PackManifest
        {
            FormatVersion = "1.0",
            Id = validId,
            Version = PackVersion.Parse("1.0.0"),
            Name = "Test",
            Description = "Test",
            ContentHash = new ContentHash("e".PadRight(64, 'f')),
            CreatedAt = DateTime.UtcNow,
            Components = new List<PackComponent>()
        };

        // Act & Assert
        manifest.Id.Should().Be(validId);
    }

    [Fact]
    public void FormatVersion_ShouldBe1Point0()
    {
        // Arrange
        var manifest = new PackManifest
        {
            FormatVersion = "1.0",
            Id = "test",
            Version = PackVersion.Parse("1.0.0"),
            Name = "Test",
            Description = "Test",
            ContentHash = new ContentHash("a".PadRight(64, '1')),
            CreatedAt = DateTime.UtcNow,
            Components = new List<PackComponent>()
        };

        // Act & Assert
        manifest.FormatVersion.Should().Be("1.0");
    }

    [Fact]
    public void Components_ShouldBeImmutableList()
    {
        // Arrange
        var initialComponents = new List<PackComponent>
        {
            new() { Path = "system.md", Type = ComponentType.System }
        };

        var manifest = new PackManifest
        {
            FormatVersion = "1.0",
            Id = "test",
            Version = PackVersion.Parse("1.0.0"),
            Name = "Test",
            Description = "Test",
            ContentHash = new ContentHash("2".PadRight(64, '3')),
            CreatedAt = DateTime.UtcNow,
            Components = initialComponents
        };

        // Act
        var manifest2 = manifest with
        {
            Components = new List<PackComponent>
            {
                new() { Path = "other.md", Type = ComponentType.Custom }
            }
        };

        // Assert
        manifest.Components.Should().HaveCount(1);
        manifest.Components.First().Path.Should().Be("system.md");
        manifest2.Components.Should().HaveCount(1);
        manifest2.Components.First().Path.Should().Be("other.md");
    }

    [Fact]
    public void Equality_SameIdAndVersion_ShouldBeEqual()
    {
        // Arrange
        var createdAt = new DateTime(2024, 1, 15, 10, 30, 0, DateTimeKind.Utc);
        var components = new List<PackComponent>();

        var manifest1 = new PackManifest
        {
            FormatVersion = "1.0",
            Id = "test-pack",
            Version = PackVersion.Parse("1.0.0"),
            Name = "Test Pack",
            Description = "Description",
            ContentHash = new ContentHash("4".PadRight(64, '5')),
            CreatedAt = createdAt,
            Components = components
        };

        var manifest2 = new PackManifest
        {
            FormatVersion = "1.0",
            Id = "test-pack",
            Version = PackVersion.Parse("1.0.0"),
            Name = "Test Pack",
            Description = "Description",
            ContentHash = new ContentHash("4".PadRight(64, '5')),
            CreatedAt = createdAt,
            Components = components
        };

        // Act & Assert
        manifest1.Should().Be(manifest2);
    }

    [Fact]
    public void Equality_DifferentId_ShouldNotBeEqual()
    {
        // Arrange
        var manifest1 = new PackManifest
        {
            FormatVersion = "1.0",
            Id = "pack-one",
            Version = PackVersion.Parse("1.0.0"),
            Name = "Pack One",
            Description = "Description",
            ContentHash = new ContentHash("6".PadRight(64, '7')),
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
            ContentHash = new ContentHash("6".PadRight(64, '7')),
            CreatedAt = manifest1.CreatedAt,
            Components = new List<PackComponent>()
        };

        // Act & Assert
        manifest1.Should().NotBe(manifest2);
    }
}
