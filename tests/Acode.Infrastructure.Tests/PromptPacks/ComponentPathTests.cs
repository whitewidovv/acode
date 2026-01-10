using Acode.Domain.PromptPacks.Exceptions;
using Acode.Infrastructure.PromptPacks;
using FluentAssertions;
using Xunit;

namespace Acode.Infrastructure.Tests.PromptPacks;

/// <summary>
/// Unit tests for path normalization and security validation.
/// </summary>
public class ComponentPathTests
{
    [Theory]
    [InlineData(@"roles\coder.md", "roles/coder.md")]
    [InlineData(@"roles\\coder.md", "roles/coder.md")]
    [InlineData("roles//coder.md", "roles/coder.md")]
    [InlineData("roles/./coder.md", "roles/coder.md")]
    [InlineData("roles/coder.md/", "roles/coder.md")]
    public void Should_Normalize_Paths(string input, string expected)
    {
        // Act
        var result = PathNormalizer.Normalize(input);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("../etc/passwd")]
    [InlineData("roles/../../../etc/passwd")]
    [InlineData("roles/..")]
    [InlineData("..")]
    public void Should_Reject_Traversal_Paths(string path)
    {
        // Act
        var act = () => PathNormalizer.Normalize(path);

        // Assert
        act.Should().Throw<PathTraversalException>()
            .Where(e => e.ErrorCode == "ACODE-PKL-007");
    }

    [Theory]
    [InlineData("/roles/coder.md")]
    [InlineData(@"C:\Users\test\file.md")]
    [InlineData("C:/Users/test/file.md")]
    public void Should_Reject_Absolute_Paths(string path)
    {
        // Act
        var act = () => PathNormalizer.Normalize(path);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*absolute*");
    }

    [Fact]
    public void Should_Handle_Unicode_Paths()
    {
        // Arrange
        var unicodePath = "languages/日本語.md";

        // Act
        var result = PathNormalizer.Normalize(unicodePath);

        // Assert
        result.Should().Be(unicodePath);
    }

    [Fact]
    public void Should_Validate_Path_Is_Under_Root()
    {
        // Arrange
        var root = "/pack";
        var safePath = "roles/coder.md";
        var unsafePath = "../other/file.md";

        // Act
        var isSafe = PathNormalizer.IsPathSafe(root, safePath);
        var isUnsafe = !PathNormalizer.IsPathSafe(root, unsafePath);

        // Assert
        isSafe.Should().BeTrue();
        isUnsafe.Should().BeTrue();
    }

    [Fact]
    public void Should_Return_True_For_Safe_Paths()
    {
        // Arrange
        var safePath = "roles/coder.md";

        // Act
        var result = PathNormalizer.IsPathSafe(safePath);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void Should_Return_False_For_Null_Path()
    {
        // Act
        var result = PathNormalizer.IsPathSafe(null);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void Should_Return_False_For_Whitespace_Path()
    {
        // Act
        var result = PathNormalizer.IsPathSafe("  ");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void EnsurePathSafe_Should_Throw_For_Traversal()
    {
        // Arrange
        var unsafePath = "../etc/passwd";

        // Act
        var act = () => PathNormalizer.EnsurePathSafe(unsafePath);

        // Assert
        act.Should().Throw<PathTraversalException>()
            .Where(e => e.ErrorCode == "ACODE-PKL-007");
    }

    [Fact]
    public void EnsurePathSafe_Should_Throw_For_Null()
    {
        // Act
        var act = () => PathNormalizer.EnsurePathSafe(null);

        // Assert
        act.Should().Throw<PathTraversalException>();
    }
}
