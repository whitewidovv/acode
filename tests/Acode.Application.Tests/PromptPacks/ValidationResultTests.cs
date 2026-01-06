using Acode.Domain.PromptPacks;
using FluentAssertions;

namespace Acode.Application.Tests.PromptPacks;

/// <summary>
/// Tests for <see cref="ValidationResult"/>.
/// </summary>
public class ValidationResultTests
{
    [Fact]
    public void Constructor_WithValid_ShouldSetIsValidTrue()
    {
        // Arrange & Act
        var result = new ValidationResult(true, new List<ValidationError>());

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void Constructor_WithInvalid_ShouldSetIsValidFalse()
    {
        // Arrange
        var errors = new List<ValidationError>
        {
            new("ERR001", "Test error"),
        };

        // Act
        var result = new ValidationResult(false, errors);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().HaveCount(1);
        result.Errors.First().Code.Should().Be("ERR001");
    }

    [Fact]
    public void Success_ShouldReturnValidResult()
    {
        // Act
        var result = ValidationResult.Success();

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void Failure_WithErrors_ShouldReturnInvalidResult()
    {
        // Arrange
        var errors = new List<ValidationError>
        {
            new("ERR001", "Error 1"),
            new("ERR002", "Error 2"),
        };

        // Act
        var result = ValidationResult.Failure(errors);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().HaveCount(2);
    }

    [Fact]
    public void Failure_WithSingleError_ShouldReturnInvalidResult()
    {
        // Arrange
        var error = new ValidationError("ERR001", "Single error");

        // Act
        var result = ValidationResult.Failure(error);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().HaveCount(1);
        result.Errors.First().Should().Be(error);
    }

    [Fact]
    public void Errors_ShouldBeImmutable()
    {
        // Arrange
        var result = ValidationResult.Success();

        // Act & Assert
        result.Errors.Should().BeAssignableTo<IReadOnlyList<ValidationError>>();
    }
}
