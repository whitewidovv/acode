namespace Acode.Domain.Tests.Security;

using Acode.Domain.Security;
using FluentAssertions;
using Xunit;

public class SecuritySeverityTests
{
    [Fact]
    public void SecuritySeverity_ShouldHaveDebugValue()
    {
        // Arrange & Act
        var severity = SecuritySeverity.Debug;

        // Assert
        severity.Should().Be(SecuritySeverity.Debug);
        ((int)severity).Should().Be(0);
    }

    [Fact]
    public void SecuritySeverity_ShouldHaveInfoValue()
    {
        // Arrange & Act
        var severity = SecuritySeverity.Info;

        // Assert
        severity.Should().Be(SecuritySeverity.Info);
        ((int)severity).Should().Be(1);
    }

    [Fact]
    public void SecuritySeverity_ShouldHaveWarningValue()
    {
        // Arrange & Act
        var severity = SecuritySeverity.Warning;

        // Assert
        severity.Should().Be(SecuritySeverity.Warning);
        ((int)severity).Should().Be(2);
    }

    [Fact]
    public void SecuritySeverity_ShouldHaveErrorValue()
    {
        // Arrange & Act
        var severity = SecuritySeverity.Error;

        // Assert
        severity.Should().Be(SecuritySeverity.Error);
        ((int)severity).Should().Be(3);
    }

    [Fact]
    public void SecuritySeverity_ShouldHaveCriticalValue()
    {
        // Arrange & Act
        var severity = SecuritySeverity.Critical;

        // Assert
        severity.Should().Be(SecuritySeverity.Critical);
        ((int)severity).Should().Be(4);
    }

    [Fact]
    public void SecuritySeverity_ShouldHaveExactlyFiveValues()
    {
        // Arrange & Act
        var values = Enum.GetValues<SecuritySeverity>();

        // Assert
        values.Should().HaveCount(5);
    }

    [Fact]
    public void SecuritySeverity_ShouldBeOrderedBySeverityAscending()
    {
        // Arrange & Act
        var debug = (int)SecuritySeverity.Debug;
        var info = (int)SecuritySeverity.Info;
        var warning = (int)SecuritySeverity.Warning;
        var error = (int)SecuritySeverity.Error;
        var critical = (int)SecuritySeverity.Critical;

        // Assert
        debug.Should().BeLessThan(info);
        info.Should().BeLessThan(warning);
        warning.Should().BeLessThan(error);
        error.Should().BeLessThan(critical);
    }
}
