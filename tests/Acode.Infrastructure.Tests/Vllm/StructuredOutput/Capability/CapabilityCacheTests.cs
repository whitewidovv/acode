namespace Acode.Infrastructure.Tests.Vllm.StructuredOutput.Capability;

using Acode.Infrastructure.Vllm.StructuredOutput.Capability;
using FluentAssertions;
using Xunit;

/// <summary>
/// Tests for CapabilityCache.
/// </summary>
public class CapabilityCacheTests
{
    [Fact]
    public void Cache_WithCapabilities_StoresInCache()
    {
        // Arrange
        var cache = new CapabilityCache();
        var capabilities = new ModelCapabilities
        {
            ModelId = "llama2",
            SupportsGuidedJson = true,
        };

        // Act
        cache.Cache(capabilities);
        var found = cache.TryGetCached("llama2", out var cached);

        // Assert
        found.Should().BeTrue();
        cached.Should().NotBeNull();
        cached!.ModelId.Should().Be("llama2");
        cached.SupportsGuidedJson.Should().BeTrue();
    }

    [Fact]
    public void TryGetCached_WithNonexistentModel_ReturnsFalse()
    {
        // Arrange
        var cache = new CapabilityCache();

        // Act
        var found = cache.TryGetCached("nonexistent", out var capabilities);

        // Assert
        found.Should().BeFalse();
        capabilities.Should().BeNull();
    }

    [Fact]
    public void TryGetCached_WithEmptyModelId_ReturnsFalse()
    {
        // Arrange
        var cache = new CapabilityCache();

        // Act
        var found = cache.TryGetCached(string.Empty, out var capabilities);

        // Assert
        found.Should().BeFalse();
        capabilities.Should().BeNull();
    }

    [Fact]
    public void Invalidate_WithCachedModel_RemovesFromCache()
    {
        // Arrange
        var cache = new CapabilityCache();
        var capabilities = new ModelCapabilities { ModelId = "llama2" };
        cache.Cache(capabilities);
        cache.TryGetCached("llama2", out _).Should().BeTrue();

        // Act
        cache.Invalidate("llama2");
        var found = cache.TryGetCached("llama2", out _);

        // Assert
        found.Should().BeFalse();
    }

    [Fact]
    public void Clear_WithMultipleCachedModels_ClearsAll()
    {
        // Arrange
        var cache = new CapabilityCache();
        cache.Cache(new ModelCapabilities { ModelId = "llama2" });
        cache.Cache(new ModelCapabilities { ModelId = "llama3" });
        cache.Cache(new ModelCapabilities { ModelId = "mistral" });
        cache.GetCacheSize().Should().Be(3);

        // Act
        cache.Clear();

        // Assert
        cache.GetCacheSize().Should().Be(0);
        cache.TryGetCached("llama2", out _).Should().BeFalse();
    }

    [Fact]
    public void GetCacheSize_WithMultipleCachedModels_ReturnsCorrectCount()
    {
        // Arrange
        var cache = new CapabilityCache();

        // Act
        cache.Cache(new ModelCapabilities { ModelId = "llama2" });
        var size1 = cache.GetCacheSize();

        cache.Cache(new ModelCapabilities { ModelId = "llama3" });
        var size2 = cache.GetCacheSize();

        // Assert
        size1.Should().Be(1);
        size2.Should().Be(2);
    }

    [Fact]
    public void Cache_WithNullCapabilities_DoesNotThrow()
    {
        // Arrange
        var cache = new CapabilityCache();

        // Act
        var action = () => cache.Cache(null!);

        // Assert
        action.Should().NotThrow();
    }

    [Fact]
    public void Cache_WithCaseInsensitiveModelId_RetrievesCorrectly()
    {
        // Arrange
        var cache = new CapabilityCache();
        var capabilities = new ModelCapabilities { ModelId = "Llama2" };
        cache.Cache(capabilities);

        // Act
        var found = cache.TryGetCached("llama2", out var cached);

        // Assert
        found.Should().BeTrue();
        cached!.ModelId.Should().Be("Llama2");
    }
}
