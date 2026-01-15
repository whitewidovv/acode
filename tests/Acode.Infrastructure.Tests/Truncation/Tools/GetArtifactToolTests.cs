namespace Acode.Infrastructure.Tests.Truncation.Tools;

using Acode.Infrastructure.Truncation;
using Acode.Infrastructure.Truncation.Tools;
using FluentAssertions;

/// <summary>
/// Tests for GetArtifactTool functionality.
/// </summary>
/// <remarks>
/// Task-007c: Truncation + Artifact Attachment Rules.
/// Tests artifact retrieval via get_artifact tool.
/// </remarks>
public sealed class GetArtifactToolTests : IDisposable
{
    private readonly string testDirectory;
    private readonly FileSystemArtifactStore artifactStore;
    private readonly GetArtifactTool tool;

    public GetArtifactToolTests()
    {
        this.testDirectory = Path.Combine(Path.GetTempPath(), $"artifact-tool-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(this.testDirectory);
        this.artifactStore = new FileSystemArtifactStore(this.testDirectory);
        this.tool = new GetArtifactTool(this.artifactStore);
    }

    public void Dispose()
    {
        this.artifactStore.Dispose();
        if (Directory.Exists(this.testDirectory))
        {
            Directory.Delete(this.testDirectory, recursive: true);
        }
    }

    [Fact]
    public async Task GetAsync_WithValidArtifactId_ReturnsFullContent()
    {
        // Arrange
        var content = "Line 1\nLine 2\nLine 3\nLine 4\nLine 5";
        var artifact = await this.artifactStore.CreateAsync(content, "test_tool", "text/plain");

        // Act
        var result = await this.tool.GetAsync(artifact.Id);

        // Assert
        result.Should().NotBeNull();
        result!.IsSuccess.Should().BeTrue();
        result.Content.Should().Be(content);
    }

    [Fact]
    public async Task GetAsync_WithInvalidArtifactId_ReturnsError()
    {
        // Arrange
        var invalidId = "art_invalid_12345678abcd";

        // Act
        var result = await this.tool.GetAsync(invalidId);

        // Assert
        result.Should().NotBeNull();
        result!.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("not found");
    }

    [Fact]
    public async Task GetAsync_WithLineRange_ReturnsPartialContent()
    {
        // Arrange
        var content = "Line 1\nLine 2\nLine 3\nLine 4\nLine 5";
        var artifact = await this.artifactStore.CreateAsync(content, "test_tool", "text/plain");

        // Act
        var result = await this.tool.GetAsync(artifact.Id, startLine: 2, endLine: 4);

        // Assert
        result.Should().NotBeNull();
        result!.IsSuccess.Should().BeTrue();
        result.Content.Should().Contain("Line 2");
        result.Content.Should().Contain("Line 3");
        result.Content.Should().Contain("Line 4");
        result.Content.Should().NotContain("Line 1");
        result.Content.Should().NotContain("Line 5");
    }

    [Fact]
    public async Task GetAsync_WithStartLineOnly_ReturnsFromStartToEnd()
    {
        // Arrange
        var content = "Line 1\nLine 2\nLine 3\nLine 4\nLine 5";
        var artifact = await this.artifactStore.CreateAsync(content, "test_tool", "text/plain");

        // Act
        var result = await this.tool.GetAsync(artifact.Id, startLine: 3);

        // Assert
        result.Should().NotBeNull();
        result!.IsSuccess.Should().BeTrue();
        result.Content.Should().Contain("Line 3");
        result.Content.Should().Contain("Line 4");
        result.Content.Should().Contain("Line 5");
        result.Content.Should().NotContain("Line 1");
        result.Content.Should().NotContain("Line 2");
    }

    [Fact]
    public async Task GetAsync_WithEndLineOnly_ReturnsFromBeginningToEnd()
    {
        // Arrange
        var content = "Line 1\nLine 2\nLine 3\nLine 4\nLine 5";
        var artifact = await this.artifactStore.CreateAsync(content, "test_tool", "text/plain");

        // Act
        var result = await this.tool.GetAsync(artifact.Id, endLine: 3);

        // Assert
        result.Should().NotBeNull();
        result!.IsSuccess.Should().BeTrue();
        result.Content.Should().Contain("Line 1");
        result.Content.Should().Contain("Line 2");
        result.Content.Should().Contain("Line 3");
        result.Content.Should().NotContain("Line 4");
        result.Content.Should().NotContain("Line 5");
    }

    [Fact]
    public async Task GetAsync_WithOutOfRangeLines_ReturnsAvailableContent()
    {
        // Arrange
        var content = "Line 1\nLine 2\nLine 3";
        var artifact = await this.artifactStore.CreateAsync(content, "test_tool", "text/plain");

        // Act
        var result = await this.tool.GetAsync(artifact.Id, startLine: 1, endLine: 100);

        // Assert
        result.Should().NotBeNull();
        result!.IsSuccess.Should().BeTrue();
        result.Content.Should().Be(content);
    }

    [Fact]
    public async Task GetAsync_WithPathTraversalAttempt_ReturnsError()
    {
        // Arrange
        var maliciousId = "art_../../etc/passwd";

        // Act
        var result = await this.tool.GetAsync(maliciousId);

        // Assert
        result.Should().NotBeNull();
        result!.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Invalid artifact ID");
    }

    [Fact]
    public async Task GetAsync_WithUrlEncodedPathTraversal_ReturnsError()
    {
        // Arrange - URL-encoded ".." (%2e%2e)
        var maliciousId = "art_%2e%2e%2fetc%2fpasswd";

        // Act
        var result = await this.tool.GetAsync(maliciousId);

        // Assert
        result.Should().NotBeNull();
        result!.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Invalid artifact ID");
    }

    [Fact]
    public async Task GetAsync_WithInvalidCharacters_ReturnsError()
    {
        // Arrange - artifact ID with invalid characters (spaces, special chars)
        var invalidId = "art_test id with spaces!@#";

        // Act
        var result = await this.tool.GetAsync(invalidId);

        // Assert
        result.Should().NotBeNull();
        result!.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Invalid artifact ID");
    }

    [Fact]
    public async Task GetAsync_WithNullId_ReturnsError()
    {
        // Act
        var result = await this.tool.GetAsync(null!);

        // Assert
        result.Should().NotBeNull();
        result!.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("required");
    }

    [Fact]
    public async Task GetAsync_WithEmptyId_ReturnsError()
    {
        // Act
        var result = await this.tool.GetAsync(string.Empty);

        // Assert
        result.Should().NotBeNull();
        result!.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("required");
    }

    [Fact]
    public void ToolName_ShouldBeGetArtifact()
    {
        // Assert
        GetArtifactTool.ToolName.Should().Be("get_artifact");
    }
}
