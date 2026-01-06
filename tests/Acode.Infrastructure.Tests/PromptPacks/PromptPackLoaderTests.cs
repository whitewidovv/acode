using Acode.Domain.PromptPacks;
using Acode.Infrastructure.PromptPacks;
using FluentAssertions;

namespace Acode.Infrastructure.Tests.PromptPacks;

/// <summary>
/// Tests for <see cref="PromptPackLoader"/>.
/// </summary>
public class PromptPackLoaderTests : IDisposable
{
    private readonly string _testPacksRoot;

    public PromptPackLoaderTests()
    {
        _testPacksRoot = Path.Combine(Path.GetTempPath(), $"acode-test-packs-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_testPacksRoot);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testPacksRoot))
        {
            Directory.Delete(_testPacksRoot, recursive: true);
        }
    }

    [Fact]
    public void LoadPack_ValidPack_ShouldLoadSuccessfully()
    {
        // Arrange
        var packPath = CreateTestPack("test-pack", "1.0.0", new Dictionary<string, string>
        {
            ["roles/coder.md"] = "You are a coding assistant.",
        });

        var hasher = new ContentHasher();
        var loader = new PromptPackLoader(hasher);

        // Act
        var pack = loader.LoadPack(packPath);

        // Assert
        pack.Should().NotBeNull();
        pack.Manifest.Id.Should().Be("test-pack");
        pack.Manifest.Version.Should().Be(PackVersion.Parse("1.0.0"));
        pack.Components.Should().HaveCount(1);
        pack.Components.Should().ContainKey("roles/coder.md");
    }

    [Fact]
    public void LoadPack_MissingManifest_ShouldThrowPackLoadException()
    {
        // Arrange
        var packPath = Path.Combine(_testPacksRoot, "missing-manifest-pack");
        Directory.CreateDirectory(packPath);

        var hasher = new ContentHasher();
        var loader = new PromptPackLoader(hasher);

        // Act
        var act = () => loader.LoadPack(packPath);

        // Assert
        act.Should().Throw<PackLoadException>()
            .WithMessage("*manifest.yml*not found*");
    }

    [Fact]
    public void LoadPack_InvalidYaml_ShouldThrowPackLoadException()
    {
        // Arrange
        var packPath = Path.Combine(_testPacksRoot, "invalid-yaml-pack");
        Directory.CreateDirectory(packPath);
        File.WriteAllText(Path.Combine(packPath, "manifest.yml"), "invalid: yaml: content:");

        var hasher = new ContentHasher();
        var loader = new PromptPackLoader(hasher);

        // Act
        var act = () => loader.LoadPack(packPath);

        // Assert
        act.Should().Throw<PackLoadException>()
            .WithMessage("*parse*manifest.yml*");
    }

    [Fact]
    public void LoadPack_ComponentWithTraversalPath_ShouldThrowPackLoadException()
    {
        // Arrange
        var dummyHash = "a".PadRight(64, '1');
        var manifestContent = $@"
format_version: '1.0'
id: malicious-pack
version: 1.0.0
name: Malicious Pack
description: Pack with path traversal
content_hash: {dummyHash}
created_at: 2024-01-15T10:30:00Z
components:
  - path: ../../../etc/passwd
    type: system
";
        var packPath = Path.Combine(_testPacksRoot, "malicious-pack");
        Directory.CreateDirectory(packPath);
        File.WriteAllText(Path.Combine(packPath, "manifest.yml"), manifestContent);

        var hasher = new ContentHasher();
        var loader = new PromptPackLoader(hasher);

        // Act
        var act = () => loader.LoadPack(packPath);

        // Assert
        act.Should().Throw<PackLoadException>()
            .WithMessage("*path traversal*");
    }

    [Fact]
    public void LoadPack_MissingComponentFile_ShouldThrowPackLoadException()
    {
        // Arrange
        var dummyHash = "b".PadRight(64, '2');
        var manifestContent = $@"
format_version: '1.0'
id: incomplete-pack
version: 1.0.0
name: Incomplete Pack
description: Pack with missing component file
content_hash: {dummyHash}
created_at: 2024-01-15T10:30:00Z
components:
  - path: roles/missing.md
    type: role
    role: missing
";
        var packPath = Path.Combine(_testPacksRoot, "incomplete-pack");
        Directory.CreateDirectory(packPath);
        File.WriteAllText(Path.Combine(packPath, "manifest.yml"), manifestContent);

        var hasher = new ContentHasher();
        var loader = new PromptPackLoader(hasher);

        // Act
        var act = () => loader.LoadPack(packPath);

        // Assert
        act.Should().Throw<PackLoadException>()
            .WithMessage("*component file*not found*");
    }

    [Fact]
    public void LoadPack_MultipleComponents_ShouldLoadAll()
    {
        // Arrange
        var packPath = CreateTestPack("multi-pack", "2.0.0", new Dictionary<string, string>
        {
            ["system/core.md"] = "Core system prompt.",
            ["roles/architect.md"] = "Software architect role.",
            ["languages/python.md"] = "Python guidelines.",
        });

        var hasher = new ContentHasher();
        var loader = new PromptPackLoader(hasher);

        // Act
        var pack = loader.LoadPack(packPath);

        // Assert
        pack.Components.Should().HaveCount(3);
        pack.Components.Should().ContainKey("system/core.md");
        pack.Components.Should().ContainKey("roles/architect.md");
        pack.Components.Should().ContainKey("languages/python.md");
        pack.Components["system/core.md"].Content.Should().Be("Core system prompt.");
    }

    [Fact]
    public void LoadPack_HashMismatch_ShouldLoadWithWarning()
    {
        // Arrange - Create pack with intentionally wrong hash
        var components = new Dictionary<string, string>
        {
            ["roles/coder.md"] = "You are a coding assistant.",
        };
        var packPath = CreateTestPackWithWrongHash("hash-mismatch-pack", "1.0.0", components);

        var hasher = new ContentHasher();
        var loader = new PromptPackLoader(hasher);

        // Act - Should load successfully despite hash mismatch (warning, not error)
        var pack = loader.LoadPack(packPath);

        // Assert
        pack.Should().NotBeNull();
        pack.Manifest.Id.Should().Be("hash-mismatch-pack");

        // Note: In a real implementation, we'd capture warnings via ILogger
        // For now, we verify the pack loads successfully
    }

    [Fact]
    public void LoadPack_NormalizesPaths_ShouldConvertBackslashes()
    {
        // Arrange - Create manifest with backslashes
        var dummyHash = "c".PadRight(64, '3');
        var manifestContent = $@"
format_version: '1.0'
id: backslash-pack
version: 1.0.0
name: Backslash Pack
description: Pack with backslash paths
content_hash: {dummyHash}
created_at: 2024-01-15T10:30:00Z
components:
  - path: roles\coder.md
    type: role
    role: coder
";
        var packPath = Path.Combine(_testPacksRoot, "backslash-pack");
        Directory.CreateDirectory(packPath);
        Directory.CreateDirectory(Path.Combine(packPath, "roles"));
        File.WriteAllText(Path.Combine(packPath, "manifest.yml"), manifestContent);
        File.WriteAllText(Path.Combine(packPath, "roles", "coder.md"), "Content");

        var hasher = new ContentHasher();
        var loader = new PromptPackLoader(hasher);

        // Act
        var pack = loader.LoadPack(packPath);

        // Assert
        pack.Components.Should().ContainKey("roles/coder.md");
    }

    private string CreateTestPack(string id, string version, Dictionary<string, string> components)
    {
        var packPath = Path.Combine(_testPacksRoot, id);
        Directory.CreateDirectory(packPath);

        // Compute correct content hash
        var hasher = new ContentHasher();
        var contentHash = hasher.Compute(components);

        // Create manifest
        var manifestContent = $@"
format_version: '1.0'
id: {id}
version: {version}
name: Test Pack
description: Test pack for unit tests
content_hash: {contentHash.Value}
created_at: 2024-01-15T10:30:00Z
components:
";

        foreach (var (path, content) in components)
        {
            var normalizedPath = path.Replace('\\', '/');
            var parts = normalizedPath.Split('/');
            var componentType = parts[0] switch
            {
                "system" => "system",
                "roles" => "role",
                "languages" => "language",
                "frameworks" => "framework",
                _ => "custom",
            };

            manifestContent += $@"
  - path: {normalizedPath}
    type: {componentType}
";
            if (componentType == "role" && parts.Length > 1)
            {
                manifestContent += $"    role: {Path.GetFileNameWithoutExtension(parts[1])}\n";
            }

            // Create component file
            var componentPath = Path.Combine(packPath, normalizedPath.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(Path.GetDirectoryName(componentPath)!);
            File.WriteAllText(componentPath, content);
        }

        File.WriteAllText(Path.Combine(packPath, "manifest.yml"), manifestContent);
        return packPath;
    }

    private string CreateTestPackWithWrongHash(string id, string version, Dictionary<string, string> components)
    {
        var packPath = CreateTestPack(id, version, components);

        // Overwrite manifest with wrong hash
        var manifestPath = Path.Combine(packPath, "manifest.yml");
        var manifestContent = File.ReadAllText(manifestPath);
        var wrongHash = "a".PadRight(64, '1');
        manifestContent = manifestContent.Replace("content_hash:", $"content_hash: {wrongHash}  # Wrong hash", StringComparison.Ordinal);

        File.WriteAllText(manifestPath, manifestContent);
        return packPath;
    }
}
