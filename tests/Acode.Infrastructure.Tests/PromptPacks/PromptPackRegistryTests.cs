using Acode.Domain.PromptPacks;
using Acode.Infrastructure.PromptPacks;
using FluentAssertions;

namespace Acode.Infrastructure.Tests.PromptPacks;

/// <summary>
/// Tests for <see cref="PromptPackRegistry"/>.
/// </summary>
public class PromptPackRegistryTests : IDisposable
{
    private readonly string _testWorkspaceRoot;
    private readonly string _testPacksDir;

    public PromptPackRegistryTests()
    {
        _testWorkspaceRoot = Path.Combine(Path.GetTempPath(), $"acode-test-workspace-{Guid.NewGuid():N}");
        _testPacksDir = Path.Combine(_testWorkspaceRoot, ".acode", "prompts");
        Directory.CreateDirectory(_testPacksDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testWorkspaceRoot))
        {
            Directory.Delete(_testWorkspaceRoot, recursive: true);
        }
    }

    [Fact]
    public void Initialize_WithNoPacks_ShouldSucceed()
    {
        // Arrange
        var loader = new PromptPackLoader(new ContentHasher());
        var registry = new PromptPackRegistry(loader, _testWorkspaceRoot);

        // Act
        registry.Initialize();
        var packs = registry.ListPacks();

        // Assert
        packs.Should().NotBeNull();
        packs.Should().BeEmpty();
    }

    [Fact]
    public void Initialize_WithUserPacks_ShouldDiscoverPacks()
    {
        // Arrange
        CreateTestPack("user-pack-1", "1.0.0");
        CreateTestPack("user-pack-2", "2.5.0");

        var loader = new PromptPackLoader(new ContentHasher());
        var registry = new PromptPackRegistry(loader, _testWorkspaceRoot);

        // Act
        registry.Initialize();
        var packs = registry.ListPacks();

        // Assert
        packs.Should().HaveCount(2);
        packs.Should().Contain(p => p.Id == "user-pack-1" && p.Version.ToString() == "1.0.0");
        packs.Should().Contain(p => p.Id == "user-pack-2" && p.Version.ToString() == "2.5.0");
    }

    [Fact]
    public void ListPacks_AfterInitialize_ShouldReturnPackMetadata()
    {
        // Arrange
        CreateTestPack("test-pack", "1.2.3", "Test Pack", "A test pack", "Test Author");

        var loader = new PromptPackLoader(new ContentHasher());
        var registry = new PromptPackRegistry(loader, _testWorkspaceRoot);
        registry.Initialize();

        // Act
        var packs = registry.ListPacks();

        // Assert
        packs.Should().ContainSingle();
        var packInfo = packs[0];
        packInfo.Id.Should().Be("test-pack");
        packInfo.Version.ToString().Should().Be("1.2.3");
        packInfo.Name.Should().Be("Test Pack");
        packInfo.Description.Should().Be("A test pack");
        packInfo.Source.Should().Be(PackSource.User);
        packInfo.Author.Should().Be("Test Author");
    }

    [Fact]
    public void GetPack_ExistingPack_ShouldReturnPack()
    {
        // Arrange
        CreateTestPack("existing-pack", "1.0.0");

        var loader = new PromptPackLoader(new ContentHasher());
        var registry = new PromptPackRegistry(loader, _testWorkspaceRoot);
        registry.Initialize();

        // Act
        var pack = registry.GetPack("existing-pack");

        // Assert
        pack.Should().NotBeNull();
        pack.Manifest.Id.Should().Be("existing-pack");
        pack.Source.Should().Be(PackSource.User);
    }

    [Fact]
    public void GetPack_NonExistentPack_ShouldThrowPackNotFoundException()
    {
        // Arrange
        var loader = new PromptPackLoader(new ContentHasher());
        var registry = new PromptPackRegistry(loader, _testWorkspaceRoot);
        registry.Initialize();

        // Act
        var act = () => registry.GetPack("non-existent-pack");

        // Assert
        act.Should().Throw<PackNotFoundException>()
            .WithMessage("*non-existent-pack*");
    }

    [Fact]
    public void GetActivePack_WithDefaultStandard_ShouldReturnStandardPack()
    {
        // Arrange
        CreateTestPack("acode-standard", "1.0.0");

        var loader = new PromptPackLoader(new ContentHasher());
        var registry = new PromptPackRegistry(loader, _testWorkspaceRoot);
        registry.Initialize();

        // Act
        var activePack = registry.GetActivePack();

        // Assert
        activePack.Should().NotBeNull();
        activePack.Manifest.Id.Should().Be("acode-standard");
    }

    [Fact]
    public void GetActivePack_WithEnvironmentVariable_ShouldUseEnvVarPack()
    {
        // Arrange
        CreateTestPack("acode-standard", "1.0.0");
        CreateTestPack("custom-pack", "2.0.0");

        Environment.SetEnvironmentVariable("ACODE_PROMPT_PACK", "custom-pack");
        try
        {
            var loader = new PromptPackLoader(new ContentHasher());
            var registry = new PromptPackRegistry(loader, _testWorkspaceRoot);
            registry.Initialize();

            // Act
            var activePack = registry.GetActivePack();

            // Assert
            activePack.Should().NotBeNull();
            activePack.Manifest.Id.Should().Be("custom-pack");
        }
        finally
        {
            Environment.SetEnvironmentVariable("ACODE_PROMPT_PACK", null);
        }
    }

    [Fact]
    public void GetActivePack_EnvVarNotFound_ShouldFallbackToDefault()
    {
        // Arrange
        CreateTestPack("acode-standard", "1.0.0");

        Environment.SetEnvironmentVariable("ACODE_PROMPT_PACK", "non-existent-pack");
        try
        {
            var loader = new PromptPackLoader(new ContentHasher());
            var registry = new PromptPackRegistry(loader, _testWorkspaceRoot);
            registry.Initialize();

            // Act
            var activePack = registry.GetActivePack();

            // Assert
            activePack.Should().NotBeNull();
            activePack.Manifest.Id.Should().Be("acode-standard");
        }
        finally
        {
            Environment.SetEnvironmentVariable("ACODE_PROMPT_PACK", null);
        }
    }

    [Fact]
    public void Refresh_AfterAddingNewPack_ShouldDiscoverNewPack()
    {
        // Arrange
        CreateTestPack("pack-1", "1.0.0");

        var loader = new PromptPackLoader(new ContentHasher());
        var registry = new PromptPackRegistry(loader, _testWorkspaceRoot);
        registry.Initialize();

        var packsBeforeRefresh = registry.ListPacks();
        packsBeforeRefresh.Should().ContainSingle();

        // Add a new pack after initialization
        CreateTestPack("pack-2", "2.0.0");

        // Act
        registry.Refresh();
        var packsAfterRefresh = registry.ListPacks();

        // Assert
        packsAfterRefresh.Should().HaveCount(2);
        packsAfterRefresh.Should().Contain(p => p.Id == "pack-1");
        packsAfterRefresh.Should().Contain(p => p.Id == "pack-2");
    }

    [Fact]
    public void Refresh_AfterModifyingPack_ShouldReloadPack()
    {
        // Arrange
        CreateTestPack("test-pack", "1.0.0", "Original Name");

        var loader = new PromptPackLoader(new ContentHasher());
        var registry = new PromptPackRegistry(loader, _testWorkspaceRoot);
        registry.Initialize();

        var originalPack = registry.GetPack("test-pack");
        originalPack.Manifest.Name.Should().Be("Original Name");

        // Modify the pack
        CreateTestPack("test-pack", "1.0.0", "Modified Name");

        // Act
        registry.Refresh();
        var refreshedPack = registry.GetPack("test-pack");

        // Assert
        refreshedPack.Manifest.Name.Should().Be("Modified Name");
    }

    [Fact]
    public async Task Registry_ShouldBeThreadSafe()
    {
        // Arrange
        for (int i = 0; i < 10; i++)
        {
            CreateTestPack($"pack-{i}", "1.0.0");
        }

        var loader = new PromptPackLoader(new ContentHasher());
        var registry = new PromptPackRegistry(loader, _testWorkspaceRoot);
        registry.Initialize();

        var exceptions = new System.Collections.Concurrent.ConcurrentBag<Exception>();

        // Act - Concurrent read access
        var tasks = Enumerable.Range(0, 50).Select(i => Task.Run(() =>
        {
            try
            {
                var packs = registry.ListPacks();
                packs.Should().HaveCount(10);

                var packId = $"pack-{i % 10}";
                var pack = registry.GetPack(packId);
                pack.Should().NotBeNull();
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);
            }
        })).ToArray();

        await Task.WhenAll(tasks).ConfigureAwait(true);

        // Assert
        exceptions.Should().BeEmpty("registry operations should be thread-safe");
    }

    private void CreateTestPack(
        string id,
        string version,
        string name = "Test Pack",
        string description = "A test pack",
        string? author = null)
    {
        var packPath = Path.Combine(_testPacksDir, id);
        Directory.CreateDirectory(packPath);
        Directory.CreateDirectory(Path.Combine(packPath, "roles"));

        // Create component file
        var componentContent = "You are a coding assistant.";
        File.WriteAllText(Path.Combine(packPath, "roles", "coder.md"), componentContent);

        // Compute content hash
        var hasher = new ContentHasher();
        var components = new Dictionary<string, string>
        {
            ["roles/coder.md"] = componentContent,
        };
        var contentHash = hasher.Compute(components);

        // Create manifest
        var manifestContent = $@"
format_version: '1.0'
id: {id}
version: {version}
name: {name}
description: {description}
{(author != null ? $"author: {author}" : string.Empty)}
content_hash: {contentHash.Value}
created_at: 2024-01-15T10:30:00Z
components:
  - path: roles/coder.md
    type: role
    role: coder
";

        File.WriteAllText(Path.Combine(packPath, "manifest.yml"), manifestContent);
    }
}
