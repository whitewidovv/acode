using Acode.Domain.PromptPacks;
using FluentAssertions;

namespace Acode.Application.Tests.PromptPacks;

/// <summary>
/// Tests for <see cref="ValidationError"/>.
/// </summary>
public class ValidationErrorTests
{
    [Fact]
    public void Constructor_WithCodeAndMessage_ShouldSetProperties()
    {
        // Act
        var error = new ValidationError("ERR001", "Test error message");

        // Assert
        error.Code.Should().Be("ERR001");
        error.Message.Should().Be("Test error message");
        error.Path.Should().BeNull();
        error.Severity.Should().Be(ValidationSeverity.Error);
    }

    [Fact]
    public void Constructor_WithAllParameters_ShouldSetAllProperties()
    {
        // Act
        var error = new ValidationError(
            "WARN001",
            "Warning message",
            "roles/coder.md",
            ValidationSeverity.Warning);

        // Assert
        error.Code.Should().Be("WARN001");
        error.Message.Should().Be("Warning message");
        error.Path.Should().Be("roles/coder.md");
        error.Severity.Should().Be(ValidationSeverity.Warning);
    }

    [Fact]
    public void Severity_ShouldDefaultToError()
    {
        // Act
        var error = new ValidationError("CODE", "Message");

        // Assert
        error.Severity.Should().Be(ValidationSeverity.Error);
    }

    [Fact]
    public void ValidationSeverity_ShouldHaveThreeLevels()
    {
        // Assert
        Enum.GetValues<ValidationSeverity>().Should().HaveCount(3);
        Enum.GetValues<ValidationSeverity>().Should().Contain(ValidationSeverity.Error);
        Enum.GetValues<ValidationSeverity>().Should().Contain(ValidationSeverity.Warning);
        Enum.GetValues<ValidationSeverity>().Should().Contain(ValidationSeverity.Info);
    }

    [Fact]
    public void Equality_SameCodeAndMessage_ShouldBeEqual()
    {
        // Arrange
        var error1 = new ValidationError("ERR001", "Error message");
        var error2 = new ValidationError("ERR001", "Error message");

        // Act & Assert
        error1.Should().Be(error2);
    }

    [Fact]
    public void Equality_DifferentCode_ShouldNotBeEqual()
    {
        // Arrange
        var error1 = new ValidationError("ERR001", "Error message");
        var error2 = new ValidationError("ERR002", "Error message");

        // Act & Assert
        error1.Should().NotBe(error2);
    }
}
