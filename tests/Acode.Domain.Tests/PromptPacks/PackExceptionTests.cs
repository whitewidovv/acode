using Acode.Domain.PromptPacks;
using FluentAssertions;

namespace Acode.Domain.Tests.PromptPacks;

/// <summary>
/// Tests for pack exception hierarchy.
/// </summary>
public class PackExceptionTests
{
    [Fact]
    public void PackException_WithMessage_ShouldSetMessage()
    {
        // Act
        var exception = new PackException("Test error");

        // Assert
        exception.Message.Should().Be("Test error");
    }

    [Fact]
    public void PackLoadException_WithPackId_ShouldIncludeInMessage()
    {
        // Act
        var exception = new PackLoadException("acode-standard", "Failed to load manifest");

        // Assert
        exception.PackId.Should().Be("acode-standard");
        exception.Message.Should().Contain("acode-standard");
        exception.Message.Should().Contain("Failed to load manifest");
    }

    [Fact]
    public void PackLoadException_WithInnerException_ShouldPreserveIt()
    {
        // Arrange
        var innerException = new IOException("File not found");

        // Act
        var exception = new PackLoadException("acode-test", "Load failed", innerException);

        // Assert
        exception.InnerException.Should().Be(innerException);
    }

    [Fact]
    public void PackValidationException_WithValidationResult_ShouldIncludeErrors()
    {
        // Arrange
        var validationResult = new ValidationResult(
            false,
            new[] { new ValidationError("ERR001", "Invalid format") });

        // Act
        var exception = new PackValidationException("acode-test", validationResult);

        // Assert
        exception.PackId.Should().Be("acode-test");
        exception.ValidationResult.Should().Be(validationResult);
        exception.Message.Should().Contain("validation failed");
    }

    [Fact]
    public void PackNotFoundException_WithPackId_ShouldFormatMessage()
    {
        // Act
        var exception = new PackNotFoundException("acode-missing");

        // Assert
        exception.PackId.Should().Be("acode-missing");
        exception.Message.Should().Contain("acode-missing");
        exception.Message.Should().Contain("not found");
    }

    [Fact]
    public void ExceptionHierarchy_ShouldBeCorrect()
    {
        // Assert
        typeof(PackLoadException).Should().BeAssignableTo<PackException>();
        typeof(PackValidationException).Should().BeAssignableTo<PackException>();
        typeof(PackNotFoundException).Should().BeAssignableTo<PackException>();
        typeof(PackException).Should().BeAssignableTo<Exception>();
    }
}
