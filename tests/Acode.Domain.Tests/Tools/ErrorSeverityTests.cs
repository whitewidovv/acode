namespace Acode.Domain.Tests.Tools;

using Acode.Domain.Tools;
using FluentAssertions;

/// <summary>
/// Tests for ErrorSeverity enum.
/// FR-007b: Validation error retry contract.
/// </summary>
public sealed class ErrorSeverityTests
{
    [Fact]
    public void ErrorSeverity_ShouldHaveErrorValue()
    {
        // Assert
        ErrorSeverity.Error.Should().BeDefined();
    }

    [Fact]
    public void ErrorSeverity_ShouldHaveWarningValue()
    {
        // Assert
        ErrorSeverity.Warning.Should().BeDefined();
    }

    [Fact]
    public void ErrorSeverity_ShouldHaveInfoValue()
    {
        // Assert
        ErrorSeverity.Info.Should().BeDefined();
    }

    [Fact]
    public void ErrorSeverity_ShouldHaveThreeValues()
    {
        // Act
        var values = Enum.GetValues<ErrorSeverity>();

        // Assert
        values.Should().HaveCount(3);
    }

    [Fact]
    public void Error_ShouldBeHighestSeverity()
    {
        // Assert - Error should have lowest numeric value (highest priority)
        ((int)ErrorSeverity.Error).Should().BeLessThan((int)ErrorSeverity.Warning);
        ((int)ErrorSeverity.Warning).Should().BeLessThan((int)ErrorSeverity.Info);
    }
}
