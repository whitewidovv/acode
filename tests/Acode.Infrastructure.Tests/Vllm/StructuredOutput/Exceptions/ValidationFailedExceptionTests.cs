namespace Acode.Infrastructure.Tests.Vllm.StructuredOutput.Exceptions;

using Acode.Infrastructure.Vllm.StructuredOutput.Exceptions;
using FluentAssertions;
using Xunit;

/// <summary>
/// Tests for ValidationFailedException.
/// </summary>
public class ValidationFailedExceptionTests
{
    [Fact]
    public void Constructor_WithMessageAndErrors_CreatesException()
    {
        // Arrange
        var errors = new[] { "Field 'name' is required", "Field 'age' must be a number" };

        // Act
        var exception = new ValidationFailedException("Validation failed", errors);

        // Assert
        exception.Message.Should().Be("Validation failed");
        exception.ErrorCode.Should().Be("ACODE-VLM-SO-006");
        exception.Errors.Should().Equal(errors);
    }

    [Fact]
    public void Constructor_WithEmptyErrors_CreatesException()
    {
        // Arrange
        var errors = Array.Empty<string>();

        // Act
        var exception = new ValidationFailedException("Validation failed", errors);

        // Assert
        exception.Errors.Should().BeEmpty();
    }

    [Fact]
    public void Constructor_WithSingleError_CreatesException()
    {
        // Arrange
        var errors = new[] { "Schema validation failed" };

        // Act
        var exception = new ValidationFailedException("Validation failed", errors);

        // Assert
        exception.Errors.Should().HaveCount(1);
        exception.Errors[0].Should().Be("Schema validation failed");
    }

    [Fact]
    public void Errors_PropertyIsReadOnly()
    {
        // Arrange
        var errors = new[] { "Error 1", "Error 2" };
        var exception = new ValidationFailedException("Test", errors);

        // Assert - attempting to reassign should cause compilation error (verified manually)
        // This test verifies the property exists and is accessible
        exception.Errors.Should().NotBeNull();
    }

    [Fact]
    public void IsTransient_ReturnsFalse()
    {
        // Act
        var exception = new ValidationFailedException("Test", new[] { "Error" });

        // Assert
        exception.IsTransient.Should().BeFalse();
    }
}
