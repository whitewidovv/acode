using Acode.Infrastructure.PromptPacks;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;

namespace Acode.Infrastructure.Tests.PromptPacks;

/// <summary>
/// Tests for PackConfiguration.
/// </summary>
public class PackConfigurationTests : IDisposable
{
    private readonly PackConfiguration _config;
    private string? _originalEnvValue;

    /// <summary>
    /// Initializes a new instance of the <see cref="PackConfigurationTests"/> class.
    /// </summary>
    public PackConfigurationTests()
    {
        _originalEnvValue = Environment.GetEnvironmentVariable("ACODE_PROMPT_PACK");
        Environment.SetEnvironmentVariable("ACODE_PROMPT_PACK", null);

        _config = new PackConfiguration(NullLogger<PackConfiguration>.Instance);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        Environment.SetEnvironmentVariable("ACODE_PROMPT_PACK", _originalEnvValue);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Test that default pack ID is returned when no config.
    /// </summary>
    [Fact]
    public void Should_Return_Default_PackId()
    {
        // Act
        var packId = _config.GetActivePackId();

        // Assert
        packId.Should().Be("acode-standard");
    }

    /// <summary>
    /// Test that environment variable overrides default.
    /// </summary>
    [Fact]
    public void Should_Use_Environment_Variable_Override()
    {
        // Arrange
        Environment.SetEnvironmentVariable("ACODE_PROMPT_PACK", "custom-pack");
        _config.ClearCache();

        // Act
        var packId = _config.GetActivePackId();

        // Assert
        packId.Should().Be("custom-pack");
    }

    /// <summary>
    /// Test that empty environment variable falls back to default.
    /// </summary>
    [Fact]
    public void Should_Ignore_Empty_Environment_Variable()
    {
        // Arrange
        Environment.SetEnvironmentVariable("ACODE_PROMPT_PACK", string.Empty);
        _config.ClearCache();

        // Act
        var packId = _config.GetActivePackId();

        // Assert
        packId.Should().Be("acode-standard");
    }

    /// <summary>
    /// Test that whitespace environment variable falls back to default.
    /// </summary>
    [Fact]
    public void Should_Ignore_Whitespace_Environment_Variable()
    {
        // Arrange
        Environment.SetEnvironmentVariable("ACODE_PROMPT_PACK", "   ");
        _config.ClearCache();

        // Act
        var packId = _config.GetActivePackId();

        // Assert
        packId.Should().Be("acode-standard");
    }

    /// <summary>
    /// Test that result is cached.
    /// </summary>
    [Fact]
    public void Should_Cache_Result()
    {
        // Arrange
        var firstResult = _config.GetActivePackId();

        // Change env var but don't clear cache
        Environment.SetEnvironmentVariable("ACODE_PROMPT_PACK", "new-pack");

        // Act
        var secondResult = _config.GetActivePackId();

        // Assert - Should still return cached value
        secondResult.Should().Be(firstResult);
    }

    /// <summary>
    /// Test that ClearCache invalidates cache.
    /// </summary>
    [Fact]
    public void ClearCache_Should_Invalidate_Cache()
    {
        // Arrange
        _ = _config.GetActivePackId();
        Environment.SetEnvironmentVariable("ACODE_PROMPT_PACK", "after-clear");

        // Act
        _config.ClearCache();
        var result = _config.GetActivePackId();

        // Assert
        result.Should().Be("after-clear");
    }

    /// <summary>
    /// Test that DefaultPack property returns default ID.
    /// </summary>
    [Fact]
    public void DefaultPack_Should_Return_Default_Id()
    {
        // Act
        var defaultPack = PackConfiguration.DefaultPack;

        // Assert
        defaultPack.Should().Be("acode-standard");
    }
}
