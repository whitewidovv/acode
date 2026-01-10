using Acode.Domain.PromptPacks;
using Acode.Infrastructure.PromptPacks;
using FluentAssertions;

namespace Acode.Integration.Tests.PromptPacks;

/// <summary>
/// Integration tests for hash verification functionality.
/// </summary>
public class HashVerificationTests : IDisposable
{
    private readonly string _testDir;
    private readonly ContentHasher _hasher;
    private readonly ManifestParser _parser;

    public HashVerificationTests()
    {
        _testDir = Path.Combine(Path.GetTempPath(), $"hash-test-{Guid.NewGuid()}");
        Directory.CreateDirectory(_testDir);
        _hasher = new ContentHasher();
        _parser = new ManifestParser();
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDir))
        {
            Directory.Delete(_testDir, recursive: true);
        }
    }

    [Fact]
    public async Task Should_Verify_Valid_Hash()
    {
        // Arrange
        var packDir = CreatePackWithHash();
        var manifest = _parser.ParseFile(Path.Combine(packDir, "manifest.yml"), PackSource.User);
        var verifier = new HashVerifier(_hasher);

        // Act
        var result = await verifier.VerifyAsync(manifest, CancellationToken.None);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Should_Detect_Modified_Content()
    {
        // Arrange
        var packDir = CreatePackWithHash();
        await File.AppendAllTextAsync(Path.Combine(packDir, "system.md"), "\nModified!");
        var manifest = _parser.ParseFile(Path.Combine(packDir, "manifest.yml"), PackSource.User);
        var verifier = new HashVerifier(_hasher);

        // Act
        var result = await verifier.VerifyAsync(manifest, CancellationToken.None);

        // Assert
        result.IsValid.Should().BeFalse();
        result.ExpectedHash.Should().NotBe(result.ActualHash);
    }

    [Fact]
    public async Task Should_Handle_Pack_With_No_Content_Hash()
    {
        // Arrange
        var packDir = CreatePackWithoutHash();
        var manifest = _parser.ParseFile(Path.Combine(packDir, "manifest.yml"), PackSource.User);
        var verifier = new HashVerifier(_hasher);

        // Act
        var result = await verifier.VerifyAsync(manifest, CancellationToken.None);

        // Assert - pack without hash should still verify (ContentHash?.Matches returns true)
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void ContentHasher_Should_Compute_Hash_From_Directory()
    {
        // Arrange
        var packDir = CreatePackWithHash();
        File.AppendAllText(Path.Combine(packDir, "system.md"), "\nModified!");

        // Act
        var newHash = _hasher.ComputeHash(packDir, new[] { "system.md" });

        // Assert
        newHash.Should().NotBeNull();
        newHash.ToString().Should().HaveLength(64);
    }

    private string CreatePackWithHash()
    {
        var packDir = Path.Combine(_testDir, "test-pack");
        Directory.CreateDirectory(packDir);

        var systemContent = "You are an AI assistant.";
        File.WriteAllText(Path.Combine(packDir, "system.md"), systemContent);

        var hash = _hasher.ComputeHash(new[] { ("system.md", systemContent) });

        var manifest = $"format_version: \"1.0\"\nid: test-pack\nversion: \"1.0.0\"\nname: Test Pack\ndescription: A test prompt pack\ncontent_hash: {hash}\ncreated_at: 2024-01-15T10:00:00Z\ncomponents:\n  - path: system.md\n    type: system";
        File.WriteAllText(Path.Combine(packDir, "manifest.yml"), manifest);

        return packDir;
    }

    private string CreatePackWithoutHash()
    {
        var packDir = Path.Combine(_testDir, "test-pack-no-hash");
        Directory.CreateDirectory(packDir);

        var systemContent = "You are an AI assistant.";
        File.WriteAllText(Path.Combine(packDir, "system.md"), systemContent);

        var manifest = "format_version: \"1.0\"\nid: test-pack-no-hash\nversion: \"1.0.0\"\nname: Test Pack Without Hash\ndescription: A test prompt pack without content hash\ncreated_at: 2024-01-15T10:00:00Z\ncomponents:\n  - path: system.md\n    type: system";
        File.WriteAllText(Path.Combine(packDir, "manifest.yml"), manifest);

        return packDir;
    }
}
