using Acode.Domain.PromptPacks;
using Acode.Domain.PromptPacks.Exceptions;
using Acode.Infrastructure.PromptPacks;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;

namespace Acode.Infrastructure.Tests.PromptPacks;

/// <summary>
/// Tests for PromptPackLoader.
/// </summary>
public class PromptPackLoaderTests : IDisposable
{
    private readonly string _tempDir;
    private readonly PromptPackLoader _loader;

    /// <summary>
    /// Initializes a new instance of the <see cref="PromptPackLoaderTests"/> class.
    /// </summary>
    public PromptPackLoaderTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"acode-test-{Guid.NewGuid()}");
        Directory.CreateDirectory(_tempDir);

        var parser = new ManifestParser();
        var hasher = new ContentHasher();
        var embeddedProvider = new EmbeddedPackProvider(parser, NullLogger<EmbeddedPackProvider>.Instance);
        _loader = new PromptPackLoader(parser, hasher, embeddedProvider, NullLogger<PromptPackLoader>.Instance);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, true);
        }

        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Test that a valid pack is loaded correctly.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Fact]
    public async Task Should_Load_Valid_Pack()
    {
        // Arrange
        var packPath = CreatePackDirectory("test-pack");
        var manifest = @"
format_version: '1.0'
id: test-pack
version: 1.0.0
name: Test Pack
description: Test prompt pack
created_at: 2025-01-01T00:00:00Z
components:
  - path: system.md
    type: system
";
        File.WriteAllText(Path.Combine(packPath, "manifest.yml"), manifest);
        File.WriteAllText(Path.Combine(packPath, "system.md"), "You are a coding assistant.");

        // Act
        var result = await _loader.LoadPackAsync(packPath);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be("test-pack");
        result.Version.ToString().Should().Be("1.0.0");
        result.Name.Should().Be("Test Pack");
        result.Description.Should().Be("Test prompt pack");
        result.Components.Should().HaveCount(1);
        result.Components[0].Path.Should().Be("system.md");
        result.Components[0].Type.Should().Be(ComponentType.System);
        result.Components[0].Content.Should().Be("You are a coding assistant.");
    }

    /// <summary>
    /// Test that missing manifest throws appropriate exception.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Fact]
    public async Task Should_Fail_On_Missing_Manifest()
    {
        // Arrange
        var packPath = CreatePackDirectory("missing-manifest");

        // Act
        var act = () => _loader.LoadPackAsync(packPath);

        // Assert
        await act.Should().ThrowAsync<PackLoadException>()
            .Where(ex => ex.ErrorCode == "ACODE-PKL-001")
            .Where(ex => ex.PackPath == packPath);
    }

    /// <summary>
    /// Test that invalid YAML throws appropriate exception.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Fact]
    public async Task Should_Fail_On_Invalid_YAML()
    {
        // Arrange
        var packPath = CreatePackDirectory("bad-yaml");
        var invalidYaml = "id: test\nversion: [this is not valid";
        File.WriteAllText(Path.Combine(packPath, "manifest.yml"), invalidYaml);

        // Act
        var act = () => _loader.LoadPackAsync(packPath);

        // Assert
        await act.Should().ThrowAsync<PackLoadException>()
            .Where(ex => ex.ErrorCode == "ACODE-PKL-002");
    }

    /// <summary>
    /// Test that missing component file throws appropriate exception.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Fact]
    public async Task Should_Fail_On_Missing_Component()
    {
        // Arrange
        var packPath = CreatePackDirectory("missing-component");
        var manifest = @"
format_version: '1.0'
id: test
version: 1.0.0
name: Test
description: Test
created_at: 2025-01-01T00:00:00Z
components:
  - path: missing.md
    type: system
";
        File.WriteAllText(Path.Combine(packPath, "manifest.yml"), manifest);

        // Act
        var act = () => _loader.LoadPackAsync(packPath);

        // Assert
        await act.Should().ThrowAsync<PackLoadException>()
            .Where(ex => ex.ErrorCode == "ACODE-PKL-003")
            .Where(ex => ex.Message.Contains("missing.md"));
    }

    /// <summary>
    /// Test that multiple components are loaded correctly.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Fact]
    public async Task Should_Load_All_Components()
    {
        // Arrange
        var packPath = CreatePackDirectory("multi-component");
        Directory.CreateDirectory(Path.Combine(packPath, "roles"));

        var manifest = @"
format_version: '1.0'
id: multi
version: 1.0.0
name: Multi Component
description: Multiple components
created_at: 2025-01-01T00:00:00Z
components:
  - path: system.md
    type: system
  - path: roles/planner.md
    type: role
  - path: roles/coder.md
    type: role
";
        File.WriteAllText(Path.Combine(packPath, "manifest.yml"), manifest);
        File.WriteAllText(Path.Combine(packPath, "system.md"), "System prompt");
        File.WriteAllText(Path.Combine(packPath, "roles", "planner.md"), "Planner prompt");
        File.WriteAllText(Path.Combine(packPath, "roles", "coder.md"), "Coder prompt");

        // Act
        var result = await _loader.LoadPackAsync(packPath);

        // Assert
        result.Components.Should().HaveCount(3);
        result.Components.Should().Contain(c => c.Path == "system.md");
        result.Components.Should().Contain(c => c.Path == "roles/planner.md");
        result.Components.Should().Contain(c => c.Path == "roles/coder.md");
    }

    /// <summary>
    /// Test that path traversal attempts are blocked.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Fact]
    public async Task Should_Block_Path_Traversal()
    {
        // Arrange
        var packPath = CreatePackDirectory("attack-pack");
        var manifest = @"
format_version: '1.0'
id: attack
version: 1.0.0
name: Attack Pack
description: Path traversal attempt
created_at: 2025-01-01T00:00:00Z
components:
  - path: ../../../etc/passwd
    type: system
";
        File.WriteAllText(Path.Combine(packPath, "manifest.yml"), manifest);

        // Act
        var act = () => _loader.LoadPackAsync(packPath);

        // Assert
        await act.Should().ThrowAsync<PathTraversalException>();
    }

    /// <summary>
    /// Test that TryLoadPack returns false on error.
    /// </summary>
    [Fact]
    public void TryLoadPack_Should_Return_False_On_Error()
    {
        // Arrange
        var packPath = CreatePackDirectory("error-pack");

        // Act
        var result = _loader.TryLoadPack(packPath, out var pack, out var errorMessage);

        // Assert
        result.Should().BeFalse();
        pack.Should().BeNull();
        errorMessage.Should().NotBeNullOrEmpty();
    }

    /// <summary>
    /// Test that TryLoadPack returns true on success.
    /// </summary>
    [Fact]
    public void TryLoadPack_Should_Return_True_On_Success()
    {
        // Arrange
        var packPath = CreatePackDirectory("success-pack");
        var manifest = @"
format_version: '1.0'
id: success
version: 1.0.0
name: Success Pack
description: Should load
created_at: 2025-01-01T00:00:00Z
components:
  - path: system.md
    type: system
";
        File.WriteAllText(Path.Combine(packPath, "manifest.yml"), manifest);
        File.WriteAllText(Path.Combine(packPath, "system.md"), "System prompt");

        // Act
        var result = _loader.TryLoadPack(packPath, out var pack, out var errorMessage);

        // Assert
        result.Should().BeTrue();
        pack.Should().NotBeNull();
        pack!.Id.Should().Be("success");
        errorMessage.Should().BeNull();
    }

    /// <summary>
    /// Test that pack source is set correctly.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Fact]
    public async Task Should_Set_PackSource_To_User()
    {
        // Arrange
        var packPath = CreatePackDirectory("user-pack");
        var manifest = @"
format_version: '1.0'
id: user-pack
version: 1.0.0
name: User Pack
description: User pack test
created_at: 2025-01-01T00:00:00Z
components:
  - path: system.md
    type: system
";
        File.WriteAllText(Path.Combine(packPath, "manifest.yml"), manifest);
        File.WriteAllText(Path.Combine(packPath, "system.md"), "System");

        // Act
        var result = await _loader.LoadUserPackAsync(packPath);

        // Assert
        result.Source.Should().Be(PackSource.User);
    }

    /// <summary>
    /// Test that LoadBuiltInPackAsync throws for non-existent pack.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Fact]
    public async Task LoadBuiltInPackAsync_Should_Throw_PackNotFoundException()
    {
        // Act
        var act = () => _loader.LoadBuiltInPackAsync("nonexistent-pack");

        // Assert
        await act.Should().ThrowAsync<PackNotFoundException>();
    }

    private string CreatePackDirectory(string name)
    {
        var packPath = Path.Combine(_tempDir, name);
        Directory.CreateDirectory(packPath);
        return packPath;
    }
}
