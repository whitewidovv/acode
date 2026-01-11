using Acode.Infrastructure.PromptPacks;
using FluentAssertions;

namespace Acode.Infrastructure.Tests.PromptPacks;

/// <summary>
/// Unit tests for content hashing functionality.
/// </summary>
public class ContentHasherTests
{
    private readonly ContentHasher _hasher = new();

    [Fact]
    public void Should_Compute_SHA256_Hash()
    {
        // Arrange
        var components = new[]
        {
            ("system.md", "You are an AI assistant."),
        };

        // Act
        var hash = _hasher.ComputeHash(components);

        // Assert
        hash.ToString().Should().HaveLength(64);
        hash.ToString().Should().MatchRegex("^[a-f0-9]+$");
    }

    [Fact]
    public void Should_Sort_Paths_Alphabetically()
    {
        // Arrange
        var components1 = new[]
        {
            ("b.md", "content B"),
            ("a.md", "content A"),
        };
        var components2 = new[]
        {
            ("a.md", "content A"),
            ("b.md", "content B"),
        };

        // Act
        var hash1 = _hasher.ComputeHash(components1);
        var hash2 = _hasher.ComputeHash(components2);

        // Assert
        hash1.Should().Be(hash2, "order should not affect hash");
    }

    [Fact]
    public void Should_Normalize_Line_Endings()
    {
        // Arrange
        var crlfContent = "Line 1\r\nLine 2\r\n";
        var lfContent = "Line 1\nLine 2\n";

        var componentsCrlf = new[] { ("file.md", crlfContent) };
        var componentsLf = new[] { ("file.md", lfContent) };

        // Act
        var hashCrlf = _hasher.ComputeHash(componentsCrlf);
        var hashLf = _hasher.ComputeHash(componentsLf);

        // Assert
        hashCrlf.Should().Be(hashLf, "line endings should be normalized");
    }

    [Fact]
    public void Should_Be_Deterministic()
    {
        // Arrange
        var components = new[]
        {
            ("system.md", "You are an AI."),
            ("roles/coder.md", "Implement code."),
        };

        // Act
        var hash1 = _hasher.ComputeHash(components);
        var hash2 = _hasher.ComputeHash(components);
        var hash3 = _hasher.ComputeHash(components);

        // Assert
        hash1.Should().Be(hash2);
        hash2.Should().Be(hash3);
    }

    [Fact]
    public void Should_Produce_Different_Hash_For_Different_Content()
    {
        // Arrange
        var components1 = new[] { ("file.md", "content A") };
        var components2 = new[] { ("file.md", "content B") };

        // Act
        var hash1 = _hasher.ComputeHash(components1);
        var hash2 = _hasher.ComputeHash(components2);

        // Assert
        hash1.Should().NotBe(hash2);
    }

    [Fact]
    public void Should_Include_Path_In_Hash()
    {
        // Arrange - same content, different paths
        var components1 = new[] { ("path1.md", "same content") };
        var components2 = new[] { ("path2.md", "same content") };

        // Act
        var hash1 = _hasher.ComputeHash(components1);
        var hash2 = _hasher.ComputeHash(components2);

        // Assert
        hash1.Should().NotBe(hash2, "path should affect hash");
    }

    [Fact]
    public void Should_Handle_Empty_Components()
    {
        // Arrange
        var components = Array.Empty<(string, string)>();

        // Act
        var hash = _hasher.ComputeHash(components);

        // Assert
        hash.Should().NotBeNull();
        hash.ToString().Should().HaveLength(64);
    }

    [Fact]
    public void Should_Handle_Unicode_Content()
    {
        // Arrange
        var components = new[]
        {
            ("file.md", "日本語テスト 🚀 émojis"),
        };

        // Act
        var hash = _hasher.ComputeHash(components);

        // Assert
        hash.ToString().Should().HaveLength(64);
    }
}
