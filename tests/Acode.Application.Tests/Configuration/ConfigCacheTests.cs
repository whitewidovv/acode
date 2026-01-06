using Acode.Application.Configuration;
using Acode.Domain.Configuration;
using FluentAssertions;

namespace Acode.Application.Tests.Configuration;

/// <summary>
/// Tests for ConfigCache.
/// Verifies in-memory caching behavior.
/// </summary>
public class ConfigCacheTests
{
    [Fact]
    public void TryGet_WithEmptyCache_ShouldReturnFalse()
    {
        // Arrange
        var cache = new ConfigCache();

        // Act
        var result = cache.TryGet("/repo", out var config);

        // Assert
        result.Should().BeFalse();
        config.Should().BeNull();
    }

    [Fact]
    public void Set_ThenTryGet_ShouldReturnTrue()
    {
        // Arrange
        var cache = new ConfigCache();
        var testConfig = new AcodeConfig { SchemaVersion = "1.0.0" };

        // Act
        cache.Store("/repo", testConfig);
        var result = cache.TryGet("/repo", out var config);

        // Assert
        result.Should().BeTrue();
        config.Should().NotBeNull();
        config!.SchemaVersion.Should().Be("1.0.0");
    }

    [Fact]
    public void TryGet_WithDifferentKey_ShouldReturnFalse()
    {
        // Arrange
        var cache = new ConfigCache();
        var testConfig = new AcodeConfig { SchemaVersion = "1.0.0" };
        cache.Store("/repo1", testConfig);

        // Act
        var result = cache.TryGet("/repo2", out var config);

        // Assert
        result.Should().BeFalse();
        config.Should().BeNull();
    }

    [Fact]
    public void Set_WithSameKey_ShouldOverwrite()
    {
        // Arrange
        var cache = new ConfigCache();
        var config1 = new AcodeConfig { SchemaVersion = "1.0.0" };
        var config2 = new AcodeConfig { SchemaVersion = "2.0.0" };

        // Act
        cache.Store("/repo", config1);
        cache.Store("/repo", config2);
        cache.TryGet("/repo", out var result);

        // Assert
        result.Should().NotBeNull();
        result!.SchemaVersion.Should().Be("2.0.0");
    }

    [Fact]
    public void Invalidate_ShouldRemoveEntry()
    {
        // Arrange
        var cache = new ConfigCache();
        var testConfig = new AcodeConfig { SchemaVersion = "1.0.0" };
        cache.Store("/repo", testConfig);

        // Act
        cache.Invalidate("/repo");
        var result = cache.TryGet("/repo", out var config);

        // Assert
        result.Should().BeFalse();
        config.Should().BeNull();
    }

    [Fact]
    public void Invalidate_WithNonExistentKey_ShouldNotThrow()
    {
        // Arrange
        var cache = new ConfigCache();

        // Act
        var action = () => cache.Invalidate("/nonexistent");

        // Assert
        action.Should().NotThrow();
    }

    [Fact]
    public void InvalidateAll_ShouldClearAllEntries()
    {
        // Arrange
        var cache = new ConfigCache();
        var config1 = new AcodeConfig { SchemaVersion = "1.0.0" };
        var config2 = new AcodeConfig { SchemaVersion = "2.0.0" };
        cache.Store("/repo1", config1);
        cache.Store("/repo2", config2);

        // Act
        cache.InvalidateAll();

        // Assert
        cache.TryGet("/repo1", out _).Should().BeFalse();
        cache.TryGet("/repo2", out _).Should().BeFalse();
    }

    [Fact]
    public async Task Cache_ShouldBeThreadSafe()
    {
        // Arrange
        var cache = new ConfigCache();
        var config = new AcodeConfig { SchemaVersion = "1.0.0" };

        // Act - concurrent operations
        var tasks = Enumerable.Range(0, 100).Select(i => Task.Run(() =>
        {
            cache.Store($"/repo{i}", config);
            cache.TryGet($"/repo{i}", out _);
        }));

        // Assert - should complete without exception
#pragma warning disable CA2007 // Test code doesn't need ConfigureAwait
        await Task.WhenAll(tasks);
#pragma warning restore CA2007
    }
}
