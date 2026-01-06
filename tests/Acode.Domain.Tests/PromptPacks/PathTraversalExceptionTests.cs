using Acode.Domain.PromptPacks;
using FluentAssertions;
using Xunit;

namespace Acode.Domain.Tests.PromptPacks;

/// <summary>
/// Tests for <see cref="PathTraversalException"/>.
/// </summary>
public class PathTraversalExceptionTests
{
    [Fact]
    public void Constructor_WithAttemptedPath_ShouldSetProperties()
    {
        // Arrange
        var attemptedPath = "../../../etc/passwd";

        // Act
        var exception = new PathTraversalException(attemptedPath);

        // Assert
        exception.AttemptedPath.Should().Be(attemptedPath);
        exception.Message.Should().Contain(attemptedPath);
        exception.Message.Should().Contain("Path traversal");
    }

    [Fact]
    public void Constructor_WithMessage_ShouldIncludeCustomMessage()
    {
        // Arrange
        var attemptedPath = "../sensitive.md";
        var customMessage = "Custom security message";

        // Act
        var exception = new PathTraversalException(attemptedPath, customMessage);

        // Assert
        exception.AttemptedPath.Should().Be(attemptedPath);
        exception.Message.Should().Contain(customMessage);
    }
}
