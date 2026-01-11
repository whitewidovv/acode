using Acode.Domain.Validation;
using FluentAssertions;

namespace Acode.Domain.Tests.Validation;

/// <summary>
/// Tests for PatternType enum.
/// Verifies pattern type definition per Task 001.b.
/// </summary>
public class PatternTypeTests
{
    [Fact]
    public void PatternType_ShouldHaveExactValue()
    {
        // Assert
        PatternType.Exact.Should().BeDefined();
        ((int)PatternType.Exact).Should().Be(0);
    }

    [Fact]
    public void PatternType_ShouldHaveWildcardValue()
    {
        // Assert
        PatternType.Wildcard.Should().BeDefined();
        ((int)PatternType.Wildcard).Should().Be(1);
    }

    [Fact]
    public void PatternType_ShouldHaveRegexValue()
    {
        // Assert
        PatternType.Regex.Should().BeDefined();
        ((int)PatternType.Regex).Should().Be(2);
    }

    [Fact]
    public void PatternType_ShouldBeComparable()
    {
        // Arrange
        var type1 = PatternType.Exact;
        var type2 = PatternType.Exact;
        var type3 = PatternType.Wildcard;

        // Assert
        type1.Should().Be(type2);
        type1.Should().NotBe(type3);
    }

    [Fact]
    public void PatternType_ShouldSupportSwitchExpression()
    {
        // Arrange & Act
        var result = PatternType.Regex switch
        {
            PatternType.Exact => "exact",
            PatternType.Wildcard => "wildcard",
            PatternType.Regex => "regex",
            _ => "unknown"
        };

        // Assert
        result.Should().Be("regex");
    }
}
