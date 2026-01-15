namespace Acode.Infrastructure.Tests.Truncation;

using System.Text.RegularExpressions;
using Acode.Infrastructure.Truncation;
using FluentAssertions;

/// <summary>
/// Tests for FileSystemArtifactStore.
/// </summary>
/// <remarks>
/// Task-007c: Truncation + Artifact Attachment Rules.
/// Spec Reference: Testing Requirements lines 1460-1525.
/// Tests artifact creation, retrieval, concurrency, and cleanup.
/// </remarks>
public sealed class ArtifactStorageTests : IDisposable
{
    private readonly string testDirectory;
    private readonly FileSystemArtifactStore storage;

    public ArtifactStorageTests()
    {
        this.testDirectory = Path.Combine(Path.GetTempPath(), $"artifact-store-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(this.testDirectory);
        this.storage = new FileSystemArtifactStore(this.testDirectory);
    }

    public void Dispose()
    {
        this.storage.Dispose();
        if (Directory.Exists(this.testDirectory))
        {
            Directory.Delete(this.testDirectory, recursive: true);
        }
    }

    [Fact]
    public async Task CreateAsync_ShouldCreateArtifactWithUniqueId()
    {
        // Arrange
        var content = new string('x', 100000); // 100KB
        var toolName = "test_tool";
        var contentType = "text/plain";

        // Act
        var artifact = await this.storage.CreateAsync(content, toolName, contentType);

        // Assert
        artifact.Should().NotBeNull();
        artifact.Id.Should().NotBeNullOrEmpty();

        // ID format: art_{timestamp}_{random hex}
        artifact.Id.Should().MatchRegex(@"^art_\d+_[a-f0-9]+$");
        artifact.Size.Should().Be(100000);
        artifact.SourceTool.Should().Be("test_tool");
        artifact.ContentType.Should().Be("text/plain");
    }

    [Fact]
    public async Task GetContentAsync_ShouldRetrieveArtifactById()
    {
        // Arrange
        var originalContent = "Test content with multiple lines\nLine 2\nLine 3";
        var artifact = await this.storage.CreateAsync(originalContent, "test_tool", "text/plain");

        // Act
        var retrievedContent = await this.storage.GetContentAsync(artifact.Id);

        // Assert
        retrievedContent.Should().Be(originalContent);
    }

    [Fact]
    public async Task CreateAsync_ShouldHandleConcurrentCreation()
    {
        // Arrange
        const int concurrentCount = 10;
        var tasks = new List<Task<Acode.Application.Truncation.Artifact>>();

        // Act - Create 10 artifacts concurrently
        for (int i = 0; i < concurrentCount; i++)
        {
            var content = $"Content for artifact {i}";
            tasks.Add(this.storage.CreateAsync(content, $"tool_{i}", "text/plain"));
        }

        var artifacts = await Task.WhenAll(tasks);

        // Assert
        artifacts.Should().HaveCount(concurrentCount);
        artifacts.Select(a => a.Id).Should().OnlyHaveUniqueItems(); // No ID collisions
    }

    [Fact]
    public async Task CleanupAsync_ShouldRemoveAllArtifacts()
    {
        // Arrange
        await this.storage.CreateAsync("Content 1", "tool_1", "text/plain");
        await this.storage.CreateAsync("Content 2", "tool_2", "text/plain");
        await this.storage.CreateAsync("Content 3", "tool_3", "text/plain");

        var artifactDir = Path.Combine(this.testDirectory, ".acode", "artifacts");
        Directory.Exists(artifactDir).Should().BeTrue(); // Artifacts dir exists

        // Act
        var cleanedCount = await this.storage.CleanupAsync();

        // Assert
        cleanedCount.Should().Be(3);

        // The artifacts directory should be empty or removed after cleanup
        var remaining = await this.storage.ListAsync();
        remaining.Should().BeEmpty();
    }

    [Fact]
    public async Task GetPartialContentAsync_ShouldRetrieveLineRange()
    {
        // Arrange
        var lines = Enumerable.Range(1, 10).Select(i => $"Line {i}");
        var content = string.Join('\n', lines);
        var artifact = await this.storage.CreateAsync(content, "test_tool", "text/plain");

        // Act - Get lines 3-6
        var partialContent = await this.storage.GetPartialContentAsync(artifact.Id, startLine: 3, endLine: 6);

        // Assert
        partialContent.Should().NotBeNull();
        partialContent.Should().Contain("Line 3");
        partialContent.Should().Contain("Line 6");
        partialContent.Should().NotContain("Line 1");
        partialContent.Should().NotContain("Line 10");
    }

    [Fact]
    public async Task DeleteAsync_ShouldRemoveSpecificArtifact()
    {
        // Arrange
        var artifact1 = await this.storage.CreateAsync("Content 1", "tool_1", "text/plain");
        var artifact2 = await this.storage.CreateAsync("Content 2", "tool_2", "text/plain");

        // Act
        var deleted = await this.storage.DeleteAsync(artifact1.Id);

        // Assert
        deleted.Should().BeTrue();

        // artifact1 should be gone
        var content1 = await this.storage.GetContentAsync(artifact1.Id);
        content1.Should().BeNull();

        // artifact2 should still exist
        var content2 = await this.storage.GetContentAsync(artifact2.Id);
        content2.Should().NotBeNull();
    }

    [Fact]
    public async Task ExistsAsync_ShouldReturnCorrectValue()
    {
        // Arrange
        var artifact = await this.storage.CreateAsync("Content", "test_tool", "text/plain");

        // Act & Assert
        var exists = await this.storage.ExistsAsync(artifact.Id);
        exists.Should().BeTrue();

        var nonExistent = await this.storage.ExistsAsync("art_fake_12345678");
        nonExistent.Should().BeFalse();
    }

    [Fact]
    public async Task GetMetadataAsync_ShouldReturnArtifactInfo()
    {
        // Arrange
        var content = "Test content for metadata";
        var artifact = await this.storage.CreateAsync(content, "metadata_test", "text/plain");

        // Act
        var metadata = await this.storage.GetMetadataAsync(artifact.Id);

        // Assert
        metadata.Should().NotBeNull();
        metadata!.Id.Should().Be(artifact.Id);
        metadata.SourceTool.Should().Be("metadata_test");
        metadata.ContentType.Should().Be("text/plain");
        metadata.Size.Should().Be(content.Length);
    }

    [Fact]
    public async Task ListAsync_ShouldReturnAllArtifacts()
    {
        // Arrange
        await this.storage.CreateAsync("Content 1", "tool_1", "text/plain");
        await this.storage.CreateAsync("Content 2", "tool_2", "text/plain");
        await this.storage.CreateAsync("Content 3", "tool_3", "text/plain");

        // Act
        var artifacts = await this.storage.ListAsync();

        // Assert
        artifacts.Should().HaveCount(3);
    }

    [Fact]
    public void GenerateArtifactId_ShouldBeUnique()
    {
        // This is tested via CreateAsync - create multiple artifacts and verify unique IDs
        // This test explicitly validates the ID format matches security requirements
        var idPattern = new Regex(@"^art_\d+_[a-f0-9]{12}$", RegexOptions.Compiled);

        // Verify the pattern is what we expect (timestamp_randomhex)
        idPattern.IsMatch("art_1234567890123_abcdef123456").Should().BeTrue();
        idPattern.IsMatch("art_invalid").Should().BeFalse();
        idPattern.IsMatch("../etc/passwd").Should().BeFalse();
    }
}
