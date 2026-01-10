using Acode.Domain.PromptPacks;
using Acode.Infrastructure.PromptPacks;
using FluentAssertions;
using Xunit;

namespace Acode.Infrastructure.Tests.PromptPacks;

/// <summary>
/// Tests for PackCache.
/// </summary>
public class PackCacheTests
{
    /// <summary>
    /// Test that Set and Get work correctly.
    /// </summary>
    [Fact]
    public void Should_Store_And_Retrieve_Pack()
    {
        // Arrange
        var cache = new PackCache();
        var pack = CreateTestPack("test-pack");

        // Act
        cache.Set("test-key", pack);
        var result = cache.Get("test-key");

        // Assert
        result.Should().NotBeNull();
        result.Should().BeSameAs(pack);
    }

    /// <summary>
    /// Test that Get returns null for missing key.
    /// </summary>
    [Fact]
    public void Should_Return_Null_For_Missing_Key()
    {
        // Arrange
        var cache = new PackCache();

        // Act
        var result = cache.Get("nonexistent");

        // Assert
        result.Should().BeNull();
    }

    /// <summary>
    /// Test that Set with pack only uses ID and hash as key.
    /// </summary>
    [Fact]
    public void Should_Set_Using_Pack_Id_And_Hash()
    {
        // Arrange
        var cache = new PackCache();
        var hash = ContentHash.Compute(new[] { ("path.md", "test-content") });
        var pack = CreateTestPackWithHash("test-pack", hash);

        // Act
        cache.Set(pack);
        var key = PackCache.BuildCacheKey("test-pack", hash);
        var result = cache.Get(key);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeSameAs(pack);
    }

    /// <summary>
    /// Test that GetByPackId retrieves pack ignoring hash.
    /// </summary>
    [Fact]
    public void Should_GetByPackId_Ignoring_Hash()
    {
        // Arrange
        var cache = new PackCache();
        var hash = ContentHash.Compute(new[] { ("path.md", "content") });
        var pack = CreateTestPackWithHash("my-pack", hash);
        cache.Set(pack);

        // Act
        var result = cache.GetByPackId("my-pack");

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be("my-pack");
    }

    /// <summary>
    /// Test that GetByPackId is case-insensitive.
    /// </summary>
    [Fact]
    public void GetByPackId_Should_Be_Case_Insensitive()
    {
        // Arrange
        var cache = new PackCache();
        var pack = CreateTestPack("my-pack");
        cache.Set("key", pack);

        // Act
        var result = cache.GetByPackId("MY-PACK");

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be("my-pack");
    }

    /// <summary>
    /// Test that Remove removes the pack.
    /// </summary>
    [Fact]
    public void Should_Remove_Pack()
    {
        // Arrange
        var cache = new PackCache();
        var pack = CreateTestPack("test");
        cache.Set("key", pack);

        // Act
        var removed = cache.Remove("key");

        // Assert
        removed.Should().BeTrue();
        cache.Get("key").Should().BeNull();
    }

    /// <summary>
    /// Test that Remove returns false for missing key.
    /// </summary>
    [Fact]
    public void Remove_Should_Return_False_For_Missing_Key()
    {
        // Arrange
        var cache = new PackCache();

        // Act
        var removed = cache.Remove("nonexistent");

        // Assert
        removed.Should().BeFalse();
    }

    /// <summary>
    /// Test that Clear removes all entries.
    /// </summary>
    [Fact]
    public void Should_Clear_All_Entries()
    {
        // Arrange
        var cache = new PackCache();
        cache.Set("key1", CreateTestPack("pack1"));
        cache.Set("key2", CreateTestPack("pack2"));
        cache.Count.Should().Be(2);

        // Act
        cache.Clear();

        // Assert
        cache.Count.Should().Be(0);
        cache.Get("key1").Should().BeNull();
        cache.Get("key2").Should().BeNull();
    }

    /// <summary>
    /// Test that Count returns correct number.
    /// </summary>
    [Fact]
    public void Should_Return_Correct_Count()
    {
        // Arrange
        var cache = new PackCache();

        // Act & Assert
        cache.Count.Should().Be(0);
        cache.Set("key1", CreateTestPack("pack1"));
        cache.Count.Should().Be(1);
        cache.Set("key2", CreateTestPack("pack2"));
        cache.Count.Should().Be(2);
    }

    /// <summary>
    /// Test that BuildCacheKey handles null hash.
    /// </summary>
    [Fact]
    public void BuildCacheKey_Should_Handle_Null_Hash()
    {
        // Act
        var key = PackCache.BuildCacheKey("test", null);

        // Assert
        key.Should().Be("test");
    }

    /// <summary>
    /// Test that BuildCacheKey includes hash when present.
    /// </summary>
    [Fact]
    public void BuildCacheKey_Should_Include_Hash()
    {
        // Arrange
        var hash = ContentHash.Compute(new[] { ("path.md", "content") });

        // Act
        var key = PackCache.BuildCacheKey("test", hash);

        // Assert
        key.Should().StartWith("test:");
        key.Should().Contain(hash.ToString());
    }

    private static PromptPack CreateTestPack(string id)
    {
        return CreateTestPackWithHash(id, null);
    }

    private static PromptPack CreateTestPackWithHash(string id, ContentHash? hash)
    {
        var components = new[]
        {
            new LoadedComponent("system.md", ComponentType.System, "content", null),
        };

        return new PromptPack(
            id,
            PackVersion.Parse("1.0.0"),
            $"{id} Name",
            "Description",
            PackSource.User,
            "/path",
            hash,
            components);
    }
}
