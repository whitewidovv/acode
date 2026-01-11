using Acode.Domain.PromptPacks.Exceptions;
using Acode.Infrastructure.PromptPacks;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Acode.Infrastructure.Tests.PromptPacks;

/// <summary>
/// Tests for PromptPackRegistry.
/// </summary>
public class PromptPackRegistryTests : IDisposable
{
    private readonly string _tempDir;
    private readonly PromptPackRegistry _registry;
    private readonly PackDiscovery _discovery;
    private readonly PromptPackLoader _loader;
    private readonly PackValidator _validator;
    private readonly PackCache _cache;
    private readonly PackConfiguration _configuration;
    private string? _originalEnvValue;

    /// <summary>
    /// Initializes a new instance of the <see cref="PromptPackRegistryTests"/> class.
    /// </summary>
    public PromptPackRegistryTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"acode-registry-test-{Guid.NewGuid()}");
        Directory.CreateDirectory(_tempDir);

        _originalEnvValue = Environment.GetEnvironmentVariable("ACODE_PROMPT_PACK");
        Environment.SetEnvironmentVariable("ACODE_PROMPT_PACK", null);

        var parser = new ManifestParser();
        var hasher = new ContentHasher();
        var embeddedProvider = new EmbeddedPackProvider(parser, NullLogger<EmbeddedPackProvider>.Instance);

        var discoveryOptions = new PackDiscoveryOptions
        {
            UserPacksPath = Path.Combine(_tempDir, ".acode", "prompts"),
        };

        _discovery = new PackDiscovery(parser, Options.Create(discoveryOptions), NullLogger<PackDiscovery>.Instance);
        _loader = new PromptPackLoader(parser, hasher, embeddedProvider, NullLogger<PromptPackLoader>.Instance);
        _validator = new PackValidator(parser, NullLogger<PackValidator>.Instance);
        _cache = new PackCache();
        _configuration = new PackConfiguration(NullLogger<PackConfiguration>.Instance);

        _registry = new PromptPackRegistry(
            _discovery,
            _loader,
            _validator,
            _cache,
            _configuration,
            NullLogger<PromptPackRegistry>.Instance);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        Environment.SetEnvironmentVariable("ACODE_PROMPT_PACK", _originalEnvValue);

        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, true);
        }

        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Test that ListPacks returns empty when no packs.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task ListPacks_Should_Return_Empty_When_No_Packs()
    {
        // Arrange
        await _registry.InitializeAsync();

        // Act
        var packs = _registry.ListPacks();

        // Assert
        packs.Should().BeEmpty();
    }

    /// <summary>
    /// Test that ListPacks returns discovered packs.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task ListPacks_Should_Return_Discovered_Packs()
    {
        // Arrange
        CreateUserPack("my-pack");
        await _registry.InitializeAsync();

        // Act
        var packs = _registry.ListPacks();

        // Assert
        packs.Should().HaveCount(1);
        packs[0].Id.Should().Be("my-pack");
    }

    /// <summary>
    /// Test that GetPack returns the pack.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task GetPack_Should_Return_Pack()
    {
        // Arrange
        CreateUserPack("test-pack");
        await _registry.InitializeAsync();

        // Act
        var pack = _registry.GetPack("test-pack");

        // Assert
        pack.Should().NotBeNull();
        pack.Id.Should().Be("test-pack");
    }

    /// <summary>
    /// Test that GetPack throws for unknown pack.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task GetPack_Should_Throw_For_Unknown_Pack()
    {
        // Arrange
        await _registry.InitializeAsync();

        // Act
        var act = () => _registry.GetPack("nonexistent");

        // Assert
        act.Should().Throw<PackNotFoundException>();
    }

    /// <summary>
    /// Test that TryGetPack returns null for unknown pack.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task TryGetPack_Should_Return_Null_For_Unknown_Pack()
    {
        // Arrange
        await _registry.InitializeAsync();

        // Act
        var pack = _registry.TryGetPack("nonexistent");

        // Assert
        pack.Should().BeNull();
    }

    /// <summary>
    /// Test that TryGetPack uses cache.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task TryGetPack_Should_Use_Cache()
    {
        // Arrange
        CreateUserPack("cached-pack");
        await _registry.InitializeAsync();

        // Act - Load twice
        var first = _registry.TryGetPack("cached-pack");
        var second = _registry.TryGetPack("cached-pack");

        // Assert
        first.Should().NotBeNull();
        second.Should().NotBeNull();
        second.Should().BeSameAs(first); // Same instance from cache
    }

    /// <summary>
    /// Test that GetActivePackId returns configuration value.
    /// </summary>
    [Fact]
    public void GetActivePackId_Should_Return_Default()
    {
        // Act
        var packId = _registry.GetActivePackId();

        // Assert
        packId.Should().Be("acode-standard");
    }

    /// <summary>
    /// Test that GetActivePackId respects environment variable.
    /// </summary>
    [Fact]
    public void GetActivePackId_Should_Use_Environment_Variable()
    {
        // Arrange
        Environment.SetEnvironmentVariable("ACODE_PROMPT_PACK", "env-pack");
        _configuration.ClearCache();

        // Act
        var packId = _registry.GetActivePackId();

        // Assert
        packId.Should().Be("env-pack");
    }

    /// <summary>
    /// Test that Refresh clears cache and re-discovers.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task Refresh_Should_Clear_Cache_And_Rediscover()
    {
        // Arrange
        CreateUserPack("initial-pack");
        await _registry.InitializeAsync();
        _registry.ListPacks().Should().HaveCount(1);

        // Add another pack
        CreateUserPack("second-pack");

        // Act
        _registry.Refresh();

        // Assert
        _registry.ListPacks().Should().HaveCount(2);
    }

    /// <summary>
    /// Test that ListPacks marks active pack.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task ListPacks_Should_Mark_Active_Pack()
    {
        // Arrange
        CreateUserPack("active-test");
        Environment.SetEnvironmentVariable("ACODE_PROMPT_PACK", "active-test");
        _configuration.ClearCache();
        await _registry.InitializeAsync();

        // Act
        var packs = _registry.ListPacks();

        // Assert
        packs.Should().HaveCount(1);
        packs[0].IsActive.Should().BeTrue();
    }

    /// <summary>
    /// Test that packs are sorted by ID.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task ListPacks_Should_Be_Sorted_By_Id()
    {
        // Arrange
        CreateUserPack("zebra-pack");
        CreateUserPack("apple-pack");
        CreateUserPack("mango-pack");
        await _registry.InitializeAsync();

        // Act
        var packs = _registry.ListPacks();

        // Assert
        packs.Should().HaveCount(3);
        packs[0].Id.Should().Be("apple-pack");
        packs[1].Id.Should().Be("mango-pack");
        packs[2].Id.Should().Be("zebra-pack");
    }

    private void CreateUserPack(string packId)
    {
        var userPacksPath = Path.Combine(_tempDir, ".acode", "prompts");
        Directory.CreateDirectory(userPacksPath);

        var packPath = Path.Combine(userPacksPath, packId);
        Directory.CreateDirectory(packPath);

        var manifest = $@"
format_version: '1.0'
id: {packId}
version: 1.0.0
name: {packId} Name
description: Test pack
created_at: 2025-01-01T00:00:00Z
components:
  - path: system.md
    type: system
";
        File.WriteAllText(Path.Combine(packPath, "manifest.yml"), manifest);
        File.WriteAllText(Path.Combine(packPath, "system.md"), "System prompt content");
    }
}
