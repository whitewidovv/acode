using Acode.Application.Configuration;
using Acode.Domain.Configuration;
using Acode.Infrastructure.PromptPacks;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace Acode.Infrastructure.Tests.PromptPacks;

/// <summary>
/// Tests for PackConfiguration.
/// </summary>
public class PackConfigurationTests : IDisposable
{
    private readonly PackConfiguration _config;
    private readonly IConfigLoader _mockConfigLoader;
    private string? _originalEnvValue;

    /// <summary>
    /// Initializes a new instance of the <see cref="PackConfigurationTests"/> class.
    /// </summary>
    public PackConfigurationTests()
    {
        _originalEnvValue = Environment.GetEnvironmentVariable("ACODE_PROMPT_PACK");
        Environment.SetEnvironmentVariable("ACODE_PROMPT_PACK", null);

        _mockConfigLoader = Substitute.For<IConfigLoader>();
        _config = new PackConfiguration(_mockConfigLoader, NullLogger<PackConfiguration>.Instance);
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

    /// <summary>
    /// Test that config file pack_id is used when present.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task Should_Use_Config_File_PackId()
    {
        // Arrange
        var configWithPrompts = new AcodeConfig
        {
            SchemaVersion = "1.0.0",
            Prompts = new PromptsConfig { PackId = "config-file-pack" },
        };

        _mockConfigLoader.LoadAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(configWithPrompts));

        var config = new PackConfiguration(_mockConfigLoader, NullLogger<PackConfiguration>.Instance, "/repo");

        // Act
        var packId = await config.GetActivePackIdAsync();

        // Assert
        packId.Should().Be("config-file-pack");
    }

    /// <summary>
    /// Test that environment variable overrides config file.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task Should_Prefer_EnvVar_Over_Config_File()
    {
        // Arrange
        Environment.SetEnvironmentVariable("ACODE_PROMPT_PACK", "env-var-pack");

        var configWithPrompts = new AcodeConfig
        {
            SchemaVersion = "1.0.0",
            Prompts = new PromptsConfig { PackId = "config-file-pack" },
        };

        _mockConfigLoader.LoadAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(configWithPrompts));

        var config = new PackConfiguration(_mockConfigLoader, NullLogger<PackConfiguration>.Instance, "/repo");

        // Act
        var packId = await config.GetActivePackIdAsync();

        // Assert
        packId.Should().Be("env-var-pack");
    }

    /// <summary>
    /// Test that missing config file falls back to default.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task Should_Fallback_When_Config_File_Missing()
    {
        // Arrange
        _mockConfigLoader.LoadAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new FileNotFoundException("Config not found"));

        var config = new PackConfiguration(_mockConfigLoader, NullLogger<PackConfiguration>.Instance, "/repo");

        // Act
        var packId = await config.GetActivePackIdAsync();

        // Assert
        packId.Should().Be("acode-standard");
    }

    /// <summary>
    /// Test that missing prompts section falls back to default.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task Should_Fallback_When_Prompts_Section_Missing()
    {
        // Arrange
        var configWithoutPrompts = new AcodeConfig
        {
            SchemaVersion = "1.0.0",
            Prompts = null,
        };

        _mockConfigLoader.LoadAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(configWithoutPrompts));

        var config = new PackConfiguration(_mockConfigLoader, NullLogger<PackConfiguration>.Instance, "/repo");

        // Act
        var packId = await config.GetActivePackIdAsync();

        // Assert
        packId.Should().Be("acode-standard");
    }

    /// <summary>
    /// Test that config is only loaded once (cached).
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task Should_Cache_Config_File_Result()
    {
        // Arrange
        var configWithPrompts = new AcodeConfig
        {
            SchemaVersion = "1.0.0",
            Prompts = new PromptsConfig { PackId = "cached-pack" },
        };

        _mockConfigLoader.LoadAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(configWithPrompts));

        var config = new PackConfiguration(_mockConfigLoader, NullLogger<PackConfiguration>.Instance, "/repo");

        // Act
        await config.GetActivePackIdAsync();
        await config.GetActivePackIdAsync();

        // Assert - Config loader should only be called once due to caching
        await _mockConfigLoader.Received(1).LoadAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Test that ClearCache forces re-reading of config file.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task ClearCache_Should_Force_Config_Reload()
    {
        // Arrange
        var config1 = new AcodeConfig
        {
            SchemaVersion = "1.0.0",
            Prompts = new PromptsConfig { PackId = "first-pack" },
        };
        var config2 = new AcodeConfig
        {
            SchemaVersion = "1.0.0",
            Prompts = new PromptsConfig { PackId = "second-pack" },
        };

        _mockConfigLoader.LoadAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(config1), Task.FromResult(config2));

        var config = new PackConfiguration(_mockConfigLoader, NullLogger<PackConfiguration>.Instance, "/repo");

        // Act
        var first = await config.GetActivePackIdAsync();
        config.ClearCache();
        var second = await config.GetActivePackIdAsync();

        // Assert
        first.Should().Be("first-pack");
        second.Should().Be("second-pack");
    }

    /// <summary>
    /// Test that validation errors in config file fall back to default.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task Should_Fallback_When_Config_Validation_Fails()
    {
        // Arrange
        _mockConfigLoader.LoadAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("Configuration validation failed"));

        var config = new PackConfiguration(_mockConfigLoader, NullLogger<PackConfiguration>.Instance, "/repo");

        // Act
        var packId = await config.GetActivePackIdAsync();

        // Assert
        packId.Should().Be("acode-standard");
    }
}
