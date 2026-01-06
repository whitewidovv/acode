using Acode.Application.PromptPacks;
using Acode.Domain.PromptPacks;
using Acode.Infrastructure.PromptPacks;
using FluentAssertions;

namespace Acode.Integration.Tests.PromptPacks;

/// <summary>
/// Integration tests for Prompt Pack System components working together.
/// </summary>
public class PromptPackSystemIntegrationTests
{
    [Fact]
    public void EndToEnd_CreateManifestValidateAndHash_ShouldSucceed()
    {
        // Arrange - Create components
        var components = new Dictionary<string, string>
        {
            ["roles/coder.md"] = "You are an expert coding assistant.",
            ["languages/csharp.md"] = "C# best practices and conventions.",
            ["frameworks/dotnet.md"] = ".NET framework guidelines.",
        };

        var hasher = new ContentHasher();
        var contentHash = hasher.Compute(components);

        var packComponents = new List<PackComponent>
        {
            new() { Path = "roles/coder.md", Type = ComponentType.Role, Role = "coder" },
            new() { Path = "languages/csharp.md", Type = ComponentType.Language, Language = "csharp" },
            new() { Path = "frameworks/dotnet.md", Type = ComponentType.Framework, Framework = "dotnet" },
        };

        var manifest = new PackManifest
        {
            FormatVersion = "1.0",
            Id = "acode-dotnet",
            Version = PackVersion.Parse("1.0.0"),
            Name = "Acode .NET Pack",
            Description = "Prompts for .NET development with C#",
            ContentHash = contentHash,
            CreatedAt = DateTime.UtcNow,
            Components = packComponents,
        };

        var validator = new ManifestSchemaValidator();

        // Act & Assert - Validate manifest
        var validateAct = () => validator.Validate(manifest);
        validateAct.Should().NotThrow();

        // Act & Assert - Verify hash
        var hashMatches = hasher.Verify(components, manifest.ContentHash);
        hashMatches.Should().BeTrue();
    }

    [Fact]
    public void EndToEnd_ModifiedContent_HashVerificationShouldFail()
    {
        // Arrange - Create original components and manifest
        var originalComponents = new Dictionary<string, string>
        {
            ["roles/coder.md"] = "You are an expert coding assistant.",
        };

        var hasher = new ContentHasher();
        var originalHash = hasher.Compute(originalComponents);

        var manifest = new PackManifest
        {
            FormatVersion = "1.0",
            Id = "acode-test",
            Version = PackVersion.Parse("1.0.0"),
            Name = "Test Pack",
            Description = "Test pack for integrity verification",
            ContentHash = originalHash,
            CreatedAt = DateTime.UtcNow,
            Components = new List<PackComponent>
            {
                new() { Path = "roles/coder.md", Type = ComponentType.Role, Role = "coder" },
            },
        };

        // Act - Modify content after hashing
        var modifiedComponents = new Dictionary<string, string>
        {
            ["roles/coder.md"] = "You are a helpful assistant.", // Changed content
        };

        var hashMatches = hasher.Verify(modifiedComponents, manifest.ContentHash);

        // Assert - Hash verification should fail
        hashMatches.Should().BeFalse("content was modified after hashing");
    }

    [Fact]
    public void EndToEnd_PathTraversalInManifest_ValidationShouldFail()
    {
        // Arrange
        var hasher = new ContentHasher();
        var contentHash = hasher.Compute(new Dictionary<string, string>());

        var manifest = new PackManifest
        {
            FormatVersion = "1.0",
            Id = "malicious-pack",
            Version = PackVersion.Parse("1.0.0"),
            Name = "Malicious Pack",
            Description = "Pack with path traversal attempt",
            ContentHash = contentHash,
            CreatedAt = DateTime.UtcNow,
            Components = new List<PackComponent>
            {
                new() { Path = "../../../etc/passwd", Type = ComponentType.System }, // Path traversal!
            },
        };

        var validator = new ManifestSchemaValidator();

        // Act
        var act = () => validator.Validate(manifest);

        // Assert
        act.Should().Throw<PathTraversalException>()
            .WithMessage("*path traversal*");
    }

    [Fact]
    public void EndToEnd_CrossPlatformPathNormalization_HashShouldBeStable()
    {
        // Arrange - Same content with different line endings
        var hasher = new ContentHasher();

        var unixComponents = new Dictionary<string, string>
        {
            ["test.md"] = "Line 1\nLine 2\nLine 3",
        };

        var windowsComponents = new Dictionary<string, string>
        {
            ["test.md"] = "Line 1\r\nLine 2\r\nLine 3",
        };

        // Act
        var unixHash = hasher.Compute(unixComponents);
        var windowsHash = hasher.Compute(windowsComponents);

        // Assert - Hashes should be identical despite different line endings
        unixHash.Should().Be(windowsHash, "line endings are normalized for cross-platform stability");
    }

    [Fact]
    public void EndToEnd_ComponentOrderIndependence_HashShouldBeStable()
    {
        // Arrange - Same components in different order
        var hasher = new ContentHasher();

        var components1 = new Dictionary<string, string>
        {
            ["a.md"] = "Content A",
            ["b.md"] = "Content B",
            ["c.md"] = "Content C",
        };

        var components2 = new Dictionary<string, string>
        {
            ["c.md"] = "Content C",
            ["a.md"] = "Content A",
            ["b.md"] = "Content B",
        };

        // Act
        var hash1 = hasher.Compute(components1);
        var hash2 = hasher.Compute(components2);

        // Assert - Hashes should be identical regardless of input order
        hash1.Should().Be(hash2, "component paths are sorted for deterministic hashing");
    }

    [Fact]
    public void EndToEnd_InvalidManifestId_ValidationShouldFail()
    {
        // Arrange
        var hasher = new ContentHasher();
        var contentHash = hasher.Compute(new Dictionary<string, string>());

        var manifest = new PackManifest
        {
            FormatVersion = "1.0",
            Id = "Invalid_ID", // Underscores not allowed
            Version = PackVersion.Parse("1.0.0"),
            Name = "Invalid Pack",
            Description = "Pack with invalid ID format",
            ContentHash = contentHash,
            CreatedAt = DateTime.UtcNow,
            Components = new List<PackComponent>(),
        };

        var validator = new ManifestSchemaValidator();

        // Act
        var act = () => validator.Validate(manifest);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*lowercase*hyphens*");
    }

    [Fact]
    public void EndToEnd_ValidCompletePackWorkflow_ShouldSucceed()
    {
        // Arrange - Simulate complete pack creation workflow
        var hasher = new ContentHasher();
        var validator = new ManifestSchemaValidator();

        // Step 1: Define component content
        var components = new Dictionary<string, string>
        {
            ["system/core.md"] = "Core system instructions.",
            ["roles/architect.md"] = "You are a software architect.",
            ["languages/python.md"] = "Python coding standards.",
        };

        // Step 2: Compute content hash
        var contentHash = hasher.Compute(components);

        // Step 3: Create manifest with normalized paths
        var packComponents = new List<PackComponent>
        {
            new() { Path = PathNormalizer.Normalize("system\\core.md"), Type = ComponentType.System },
            new() { Path = PathNormalizer.Normalize("roles/architect.md"), Type = ComponentType.Role, Role = "architect" },
            new() { Path = PathNormalizer.Normalize("languages/python.md"), Type = ComponentType.Language, Language = "python" },
        };

        var manifest = new PackManifest
        {
            FormatVersion = "1.0",
            Id = "acode-python",
            Version = PackVersion.Parse("2.1.0-beta.1"),
            Name = "Acode Python Pack",
            Description = "Comprehensive prompts for Python development",
            ContentHash = contentHash,
            CreatedAt = DateTime.UtcNow,
            Author = "Acode Team",
            Components = packComponents,
        };

        // Step 4: Validate manifest
        var validateAct = () => validator.Validate(manifest);
        validateAct.Should().NotThrow();

        // Step 5: Verify content integrity
        var hashMatches = hasher.Verify(components, manifest.ContentHash);
        hashMatches.Should().BeTrue();

        // Step 6: Create PromptPack domain object
        var promptPack = new PromptPack
        {
            Manifest = manifest,
            Components = packComponents.ToDictionary(c => c.Path, c => c),
            Source = PackSource.BuiltIn,
        };

        // Assert - All steps succeeded
        promptPack.Should().NotBeNull();
        promptPack.Manifest.Id.Should().Be("acode-python");
        promptPack.Components.Should().HaveCount(3);
        promptPack.Source.Should().Be(PackSource.BuiltIn);
    }
}
