#pragma warning disable CA2007 // xUnit tests should use ConfigureAwait(true)

using Acode.Domain.Database;
using FluentAssertions;

namespace Acode.Domain.Tests.Database;

/// <summary>
/// Tests for <see cref="ValidationResult"/> domain model.
/// </summary>
public sealed class ValidationResultTests
{
    [Fact]
    public void Success_ShouldReturnValidResult()
    {
        // Act
        var result = ValidationResult.Success();

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
        result.Warnings.Should().BeEmpty();
    }

    [Fact]
    public void Failure_WithSingleError_ShouldReturnInvalidResult()
    {
        // Act
        var result = ValidationResult.Failure("Test error");

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle();
        result.Errors.Should().Contain("Test error");
        result.Warnings.Should().BeEmpty();
    }

    [Fact]
    public void Failure_WithMultipleErrors_ShouldReturnInvalidResult()
    {
        // Act
        var result = ValidationResult.Failure("Error 1", "Error 2", "Error 3");

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().HaveCount(3);
        result.Errors.Should().Contain(new[] { "Error 1", "Error 2", "Error 3" });
        result.Warnings.Should().BeEmpty();
    }

    [Fact]
    public void WithWarnings_ShouldReturnValidResultWithWarnings()
    {
        // Act
        var result = ValidationResult.WithWarnings("Warning 1", "Warning 2");

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
        result.Warnings.Should().HaveCount(2);
        result.Warnings.Should().Contain(new[] { "Warning 1", "Warning 2" });
    }

    [Fact]
    public void Combine_WithTwoSuccessResults_ShouldReturnSuccess()
    {
        // Arrange
        var result1 = ValidationResult.Success();
        var result2 = ValidationResult.Success();

        // Act
        var combined = result1.Combine(result2);

        // Assert
        combined.IsValid.Should().BeTrue();
        combined.Errors.Should().BeEmpty();
        combined.Warnings.Should().BeEmpty();
    }

    [Fact]
    public void Combine_WithSuccessAndFailure_ShouldReturnFailure()
    {
        // Arrange
        var result1 = ValidationResult.Success();
        var result2 = ValidationResult.Failure("Error from result2");

        // Act
        var combined = result1.Combine(result2);

        // Assert
        combined.IsValid.Should().BeFalse();
        combined.Errors.Should().ContainSingle();
        combined.Errors.Should().Contain("Error from result2");
    }

    [Fact]
    public void Combine_WithTwoFailures_ShouldCombineErrors()
    {
        // Arrange
        var result1 = ValidationResult.Failure("Error 1");
        var result2 = ValidationResult.Failure("Error 2");

        // Act
        var combined = result1.Combine(result2);

        // Assert
        combined.IsValid.Should().BeFalse();
        combined.Errors.Should().HaveCount(2);
        combined.Errors.Should().Contain(new[] { "Error 1", "Error 2" });
    }

    [Fact]
    public void Combine_ShouldCombineWarnings()
    {
        // Arrange
        var result1 = ValidationResult.WithWarnings("Warning 1");
        var result2 = ValidationResult.WithWarnings("Warning 2");

        // Act
        var combined = result1.Combine(result2);

        // Assert
        combined.IsValid.Should().BeTrue();
        combined.Warnings.Should().HaveCount(2);
        combined.Warnings.Should().Contain(new[] { "Warning 1", "Warning 2" });
    }

    [Fact]
    public void Combine_WithErrorsAndWarnings_ShouldCombineBoth()
    {
        // Arrange
        var result1 = new ValidationResult
        {
            IsValid = false,
            Errors = new[] { "Error 1" },
            Warnings = new[] { "Warning 1" }
        };
        var result2 = new ValidationResult
        {
            IsValid = false,
            Errors = new[] { "Error 2" },
            Warnings = new[] { "Warning 2" }
        };

        // Act
        var combined = result1.Combine(result2);

        // Assert
        combined.IsValid.Should().BeFalse();
        combined.Errors.Should().HaveCount(2);
        combined.Warnings.Should().HaveCount(2);
    }

    [Fact]
    public void ErrorsList_ShouldBeImmutable()
    {
        // Arrange
        var result = ValidationResult.Failure("Error 1");

        // Act & Assert
        result.Errors.Should().BeAssignableTo<IReadOnlyList<string>>();
    }

    [Fact]
    public void WarningsList_ShouldBeImmutable()
    {
        // Arrange
        var result = ValidationResult.WithWarnings("Warning 1");

        // Act & Assert
        result.Warnings.Should().BeAssignableTo<IReadOnlyList<string>>();
    }
}
