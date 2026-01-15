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

        _discovery = new PackDiscovery(parser, embeddedProvider, Options.Create(discoveryOptions), NullLogger<PackDiscovery>.Instance);
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
    /// Test that ListPacks returns built-in packs when no user packs exist.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task ListPacks_Should_Return_BuiltIn_Packs_When_No_User_Packs()
    {
        // Arrange
        await _registry.InitializeAsync();

        // Act
        var packs = _registry.ListPacks();

        // Assert - should have built-in packs at minimum
        packs.Should().NotBeEmpty();
        packs.Should().Contain(p => p.Id == "acode-standard");
    }

    /// <summary>
    /// Test that ListPacks returns user packs along with built-in packs.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task ListPacks_Should_Include_User_Packs()
    {
        // Arrange
        CreateUserPack("my-pack");
        await _registry.InitializeAsync();

        // Act
        var packs = _registry.ListPacks();

        // Assert - should have both user pack and built-in packs
        packs.Should().Contain(p => p.Id == "my-pack");
        packs.Should().Contain(p => p.Id == "acode-standard");
    }

    /// <summary>
    /// Test that GetPackAsync returns the pack.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task GetPackAsync_Should_Return_Pack()
    {
        // Arrange
        CreateUserPack("test-pack");
        await _registry.InitializeAsync();

        // Act
        var pack = await _registry.GetPackAsync("test-pack");

        // Assert
        pack.Should().NotBeNull();
        pack.Id.Should().Be("test-pack");
    }

    /// <summary>
    /// Test that GetPackAsync throws for unknown pack.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task GetPackAsync_Should_Throw_For_Unknown_Pack()
    {
        // Arrange
        await _registry.InitializeAsync();

        // Act
        var act = async () => await _registry.GetPackAsync("nonexistent");

        // Assert
        await act.Should().ThrowAsync<PackNotFoundException>();
    }

    /// <summary>
    /// Test that TryGetPackAsync returns null for unknown pack.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task TryGetPackAsync_Should_Return_Null_For_Unknown_Pack()
    {
        // Arrange
        await _registry.InitializeAsync();

        // Act
        var pack = await _registry.TryGetPackAsync("nonexistent");

        // Assert
        pack.Should().BeNull();
    }

    /// <summary>
    /// Test that TryGetPackAsync uses cache.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task TryGetPackAsync_Should_Use_Cache()
    {
        // Arrange
        CreateUserPack("cached-pack");
        await _registry.InitializeAsync();

        // Act - Load twice
        var first = await _registry.TryGetPackAsync("cached-pack");
        var second = await _registry.TryGetPackAsync("cached-pack");

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
    /// Test that RefreshAsync clears cache and re-discovers.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task RefreshAsync_Should_Clear_Cache_And_Rediscover()
    {
        // Arrange
        CreateUserPack("initial-pack");
        await _registry.InitializeAsync();
        var initialCount = _registry.ListPacks().Count;
        _registry.ListPacks().Should().Contain(p => p.Id == "initial-pack");

        // Add another pack
        CreateUserPack("second-pack");

        // Act
        await _registry.RefreshAsync();

        // Assert - should have one more pack than before
        _registry.ListPacks().Should().HaveCount(initialCount + 1);
        _registry.ListPacks().Should().Contain(p => p.Id == "second-pack");
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

        // Assert - the active pack should be marked
        var activePack = packs.FirstOrDefault(p => p.Id == "active-test");
        activePack.Should().NotBeNull();
        activePack!.IsActive.Should().BeTrue();

        // Other packs should not be marked as active
        var otherPacks = packs.Where(p => p.Id != "active-test");
        otherPacks.Should().OnlyContain(p => !p.IsActive);
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
        var packIds = packs.Select(p => p.Id).ToList();

        // Assert - should be sorted alphabetically
        packIds.Should().BeInAscendingOrder();
        packIds.Should().Contain("apple-pack");
        packIds.Should().Contain("mango-pack");
        packIds.Should().Contain("zebra-pack");
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
description: A test prompt pack for registry testing purposes
created_at: 2025-01-01T00:00:00Z
components:
  - path: system.md
    type: system
";
        File.WriteAllText(Path.Combine(packPath, "manifest.yml"), manifest);
        File.WriteAllText(Path.Combine(packPath, "system.md"), "System prompt content");
    }
}
