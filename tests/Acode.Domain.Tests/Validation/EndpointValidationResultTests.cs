using Acode.Domain.Validation;
using FluentAssertions;
using Xunit;

namespace Acode.Domain.Tests.Validation;

/// <summary>
/// Tests for EndpointValidationResult record.
/// Verifies validation result structure per Task 001.b.
/// </summary>
public class EndpointValidationResultTests
{
    [Fact]
    public void EndpointValidationResult_Allowed_ShouldHaveIsAllowedTrue()
    {
        // Act
        var result = EndpointValidationResult.Allowed("Valid endpoint");

        // Assert
        result.IsAllowed.Should().BeTrue();
        result.Reason.Should().Be("Valid endpoint");
        result.ViolatedConstraint.Should().BeNull();
    }

    [Fact]
    public void EndpointValidationResult_Denied_ShouldHaveIsAllowedFalse()
    {
        // Act
        var result = EndpointValidationResult.Denied("HC-01", "External LLM API denied");

        // Assert
        result.IsAllowed.Should().BeFalse();
        result.Reason.Should().Be("External LLM API denied");
        result.ViolatedConstraint.Should().Be("HC-01");
    }

    [Fact]
    public void EndpointValidationResult_ShouldSupportValueEquality()
    {
        // Arrange
        var result1 = EndpointValidationResult.Allowed("Test");
        var result2 = EndpointValidationResult.Allowed("Test");

        // Act & Assert
        result1.Should().Be(result2);
    }

    [Fact]
    public void EndpointValidationResult_ImplicitBoolConversion_ShouldWorkForAllowed()
    {
        // Arrange
        var result = EndpointValidationResult.Allowed("Valid");

        // Act
        bool isValid = result;

        // Assert
        isValid.Should().BeTrue();
    }

    [Fact]
    public void EndpointValidationResult_ImplicitBoolConversion_ShouldWorkForDenied()
    {
        // Arrange
        var result = EndpointValidationResult.Denied("HC-01", "Invalid");

        // Act
        bool isValid = result;

        // Assert
        isValid.Should().BeFalse();
    }
}
