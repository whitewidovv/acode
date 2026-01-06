using Acode.Domain.PromptPacks;
using Acode.Infrastructure.PromptPacks;
using FluentAssertions;
using Xunit;

namespace Acode.Infrastructure.Tests.PromptPacks;

/// <summary>
/// Tests for <see cref="PathNormalizer"/>.
/// </summary>
public class PathNormalizerTests
{
    [Fact]
    public void Normalize_BackslashesToForwardSlashes_ShouldConvert()
    {
        // Arrange
        var path = "roles\\coder.md";

        // Act
        var normalized = PathNormalizer.Normalize(path);

        // Assert
        normalized.Should().Be("roles/coder.md");
    }

    [Fact]
    public void Normalize_MixedSlashes_ShouldConvertAll()
    {
        // Arrange
        var path = "languages\\typescript/patterns.md";

        // Act
        var normalized = PathNormalizer.Normalize(path);

        // Assert
        normalized.Should().Be("languages/typescript/patterns.md");
    }

    [Fact]
    public void Normalize_AlreadyForwardSlashes_ShouldRemainUnchanged()
    {
        // Arrange
        var path = "frameworks/react.md";

        // Act
        var normalized = PathNormalizer.Normalize(path);

        // Assert
        normalized.Should().Be("frameworks/react.md");
    }

    [Fact]
    public void Validate_ParentDirectoryTraversal_ShouldThrow()
    {
        // Arrange
        var path = "../../../etc/passwd";

        // Act
        var act = () => PathNormalizer.Validate(path);

        // Assert
        act.Should().Throw<PathTraversalException>()
            .WithMessage("*path traversal*");
    }

    [Fact]
    public void Validate_DotDotInMiddle_ShouldThrow()
    {
        // Arrange
        var path = "roles/../../../secrets.md";

        // Act
        var act = () => PathNormalizer.Validate(path);

        // Assert
        act.Should().Throw<PathTraversalException>();
    }

    [Fact]
    public void Validate_AbsolutePath_ShouldThrow()
    {
        // Arrange
        var path = "/etc/passwd";

        // Act
        var act = () => PathNormalizer.Validate(path);

        // Assert
        act.Should().Throw<PathTraversalException>()
            .WithMessage("*absolute path*");
    }

    [Fact]
    public void Validate_WindowsAbsolutePath_ShouldThrow()
    {
        // Arrange
        var path = "C:\\Windows\\System32\\config";

        // Act
        var act = () => PathNormalizer.Validate(path);

        // Assert
        act.Should().Throw<PathTraversalException>();
    }

    [Fact]
    public void Validate_ValidRelativePath_ShouldNotThrow()
    {
        // Arrange
        var path = "roles/coder.md";

        // Act
        var act = () => PathNormalizer.Validate(path);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void Validate_ValidNestedPath_ShouldNotThrow()
    {
        // Arrange
        var path = "languages/typescript/patterns.md";

        // Act
        var act = () => PathNormalizer.Validate(path);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void NormalizeAndValidate_ValidPath_ShouldReturnNormalized()
    {
        // Arrange
        var path = "roles\\coder.md";

        // Act
        var result = PathNormalizer.NormalizeAndValidate(path);

        // Assert
        result.Should().Be("roles/coder.md");
    }

    [Fact]
    public void NormalizeAndValidate_TraversalPath_ShouldThrow()
    {
        // Arrange
        var path = "..\\..\\secrets.md";

        // Act
        var act = () => PathNormalizer.NormalizeAndValidate(path);

        // Assert
        act.Should().Throw<PathTraversalException>();
    }
}
