using Acode.Domain.PromptPacks;
using Acode.Infrastructure.PromptPacks;
using FluentAssertions;
using Xunit;

namespace Acode.Infrastructure.Tests.PromptPacks;

/// <summary>
/// Tests for <see cref="ContentHasher"/>.
/// </summary>
public class ContentHasherTests
{
    [Fact]
    public void Compute_EmptyComponents_ShouldReturnValidHash()
    {
        // Arrange
        var hasher = new ContentHasher();
        var components = new Dictionary<string, string>();

        // Act
        var hash = hasher.Compute(components);

        // Assert
        hash.Should().NotBeNull();
        hash.Value.Should().HaveLength(64);
    }

    [Fact]
    public void Compute_SingleComponent_ShouldReturnValidHash()
    {
        // Arrange
        var hasher = new ContentHasher();
        var components = new Dictionary<string, string>
        {
            ["roles/coder.md"] = "You are a coding assistant.",
        };

        // Act
        var hash = hasher.Compute(components);

        // Assert
        hash.Should().NotBeNull();
        hash.Value.Should().HaveLength(64);
        hash.Value.Should().MatchRegex("^[0-9a-f]{64}$");
    }

    [Fact]
    public void Compute_MultipleComponents_ShouldReturnValidHash()
    {
        // Arrange
        var hasher = new ContentHasher();
        var components = new Dictionary<string, string>
        {
            ["roles/coder.md"] = "You are a coding assistant.",
            ["languages/csharp.md"] = "C# best practices.",
            ["frameworks/dotnet.md"] = ".NET guidelines.",
        };

        // Act
        var hash = hasher.Compute(components);

        // Assert
        hash.Should().NotBeNull();
        hash.Value.Should().HaveLength(64);
    }

    [Fact]
    public void Compute_SameInputTwice_ShouldReturnSameHash()
    {
        // Arrange
        var hasher = new ContentHasher();
        var components = new Dictionary<string, string>
        {
            ["roles/coder.md"] = "You are a coding assistant.",
            ["languages/csharp.md"] = "C# best practices.",
        };

        // Act
        var hash1 = hasher.Compute(components);
        var hash2 = hasher.Compute(components);

        // Assert
        hash1.Should().Be(hash2);
    }

    [Fact]
    public void Compute_DifferentOrder_ShouldReturnSameHash()
    {
        // Arrange
        var hasher = new ContentHasher();
        var components1 = new Dictionary<string, string>
        {
            ["a.md"] = "Content A",
            ["b.md"] = "Content B",
        };
        var components2 = new Dictionary<string, string>
        {
            ["b.md"] = "Content B",
            ["a.md"] = "Content A",
        };

        // Act
        var hash1 = hasher.Compute(components1);
        var hash2 = hasher.Compute(components2);

        // Assert
        hash1.Should().Be(hash2, "hashing should be deterministic regardless of input order");
    }

    [Fact]
    public void Compute_DifferentContent_ShouldReturnDifferentHash()
    {
        // Arrange
        var hasher = new ContentHasher();
        var components1 = new Dictionary<string, string>
        {
            ["roles/coder.md"] = "You are a coding assistant.",
        };
        var components2 = new Dictionary<string, string>
        {
            ["roles/coder.md"] = "You are a helpful assistant.",
        };

        // Act
        var hash1 = hasher.Compute(components1);
        var hash2 = hasher.Compute(components2);

        // Assert
        hash1.Should().NotBe(hash2);
    }

    [Fact]
    public void Compute_WindowsLineEndings_ShouldNormalizeToLF()
    {
        // Arrange
        var hasher = new ContentHasher();
        var componentsUnix = new Dictionary<string, string>
        {
            ["test.md"] = "Line 1\nLine 2\nLine 3",
        };
        var componentsWindows = new Dictionary<string, string>
        {
            ["test.md"] = "Line 1\r\nLine 2\r\nLine 3",
        };

        // Act
        var hashUnix = hasher.Compute(componentsUnix);
        var hashWindows = hasher.Compute(componentsWindows);

        // Assert
        hashUnix.Should().Be(hashWindows, "line endings should be normalized to LF for cross-platform stability");
    }

    [Fact]
    public void Verify_MatchingHash_ShouldReturnTrue()
    {
        // Arrange
        var hasher = new ContentHasher();
        var components = new Dictionary<string, string>
        {
            ["roles/coder.md"] = "You are a coding assistant.",
        };
        var expectedHash = hasher.Compute(components);

        // Act
        var result = hasher.Verify(components, expectedHash);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void Verify_MismatchedHash_ShouldReturnFalse()
    {
        // Arrange
        var hasher = new ContentHasher();
        var components = new Dictionary<string, string>
        {
            ["roles/coder.md"] = "You are a coding assistant.",
        };
        var wrongHash = new ContentHash("a".PadRight(64, '1'));

        // Act
        var result = hasher.Verify(components, wrongHash);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void Verify_ModifiedContent_ShouldReturnFalse()
    {
        // Arrange
        var hasher = new ContentHasher();
        var originalComponents = new Dictionary<string, string>
        {
            ["roles/coder.md"] = "You are a coding assistant.",
        };
        var expectedHash = hasher.Compute(originalComponents);

        var modifiedComponents = new Dictionary<string, string>
        {
            ["roles/coder.md"] = "You are a helpful assistant.",
        };

        // Act
        var result = hasher.Verify(modifiedComponents, expectedHash);

        // Assert
        result.Should().BeFalse();
    }
}
