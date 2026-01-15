namespace Acode.Application.Tests.ToolSchemas.Retry;

using Acode.Application.ToolSchemas.Retry;
using FluentAssertions;

/// <summary>
/// Unit tests for ErrorSeverity enum.
/// </summary>
/// <remarks>
/// Spec Reference: Implementation Prompt lines 3212-3242.
/// ErrorSeverity defines severity levels for validation errors.
/// Values must be: Info=0, Warning=1, Error=2 for proper sorting (lowest severity first).
/// </remarks>
public sealed class ErrorSeverityTests
{
    [Fact]
    public void Info_Should_Have_Value_0()
    {
        // Arrange & Act
        var value = (int)ErrorSeverity.Info;

        // Assert
        value.Should().Be(0, "Info is the lowest severity and should be 0");
    }

    [Fact]
    public void Warning_Should_Have_Value_1()
    {
        // Arrange & Act
        var value = (int)ErrorSeverity.Warning;

        // Assert
        value.Should().Be(1, "Warning is medium severity and should be 1");
    }

    [Fact]
    public void Error_Should_Have_Value_2()
    {
        // Arrange & Act
        var value = (int)ErrorSeverity.Error;

        // Assert
        value.Should().Be(2, "Error is the highest severity and should be 2");
    }

    [Fact]
    public void Should_Have_Exactly_Three_Values()
    {
        // Arrange & Act
        var values = Enum.GetValues<ErrorSeverity>();

        // Assert
        values.Should().HaveCount(3, "ErrorSeverity should have exactly 3 values: Info, Warning, Error");
    }

    [Fact]
    public void Should_Sort_By_Severity_Ascending()
    {
        // Arrange
        var severities = new[] { ErrorSeverity.Error, ErrorSeverity.Info, ErrorSeverity.Warning };

        // Act
        var sorted = severities.OrderBy(s => s).ToArray();

        // Assert
        sorted.Should().ContainInOrder(
            ErrorSeverity.Info,
            ErrorSeverity.Warning,
            ErrorSeverity.Error);
    }

    [Fact]
    public void Should_Sort_By_Severity_Descending()
    {
        // Arrange
        var severities = new[] { ErrorSeverity.Info, ErrorSeverity.Warning, ErrorSeverity.Error };

        // Act
        var sorted = severities.OrderByDescending(s => s).ToArray();

        // Assert
        sorted.Should().ContainInOrder(
            ErrorSeverity.Error,
            ErrorSeverity.Warning,
            ErrorSeverity.Info);
    }
}
