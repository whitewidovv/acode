namespace Acode.Domain.Tests.Risks;

using Acode.Domain.Risks;
using FluentAssertions;
using Xunit;

public class SeverityTests
{
    [Fact]
    public void Severity_ShouldHaveLowValue()
    {
        // Arrange & Act
        var severity = Severity.Low;

        // Assert
        severity.Should().Be(Severity.Low);
        ((int)severity).Should().Be(0);
    }

    [Fact]
    public void Severity_ShouldHaveMediumValue()
    {
        // Arrange & Act
        var severity = Severity.Medium;

        // Assert
        severity.Should().Be(Severity.Medium);
        ((int)severity).Should().Be(1);
    }

    [Fact]
    public void Severity_ShouldHaveHighValue()
    {
        // Arrange & Act
        var severity = Severity.High;

        // Assert
        severity.Should().Be(Severity.High);
        ((int)severity).Should().Be(2);
    }

    [Fact]
    public void Severity_ShouldHaveCriticalValue()
    {
        // Arrange & Act
        var severity = Severity.Critical;

        // Assert
        severity.Should().Be(Severity.Critical);
        ((int)severity).Should().Be(3);
    }

    [Fact]
    public void Severity_ShouldHaveExactlyFourValues()
    {
        // Arrange & Act
        var values = Enum.GetValues<Severity>();

        // Assert
        values.Should().HaveCount(4);
    }

    [Fact]
    public void Severity_ShouldBeOrderedBySeverityAscending()
    {
        // Arrange & Act
        var low = (int)Severity.Low;
        var medium = (int)Severity.Medium;
        var high = (int)Severity.High;
        var critical = (int)Severity.Critical;

        // Assert
        low.Should().BeLessThan(medium);
        medium.Should().BeLessThan(high);
        high.Should().BeLessThan(critical);
    }

    [Fact]
    public void Severity_AllValuesShouldBeDistinct()
    {
        // Arrange & Act
        var values = Enum.GetValues<Severity>();

        // Assert
        values.Should().OnlyHaveUniqueItems();
    }
}
