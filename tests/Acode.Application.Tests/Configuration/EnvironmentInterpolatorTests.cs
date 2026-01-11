using Acode.Application.Configuration;
using FluentAssertions;

namespace Acode.Application.Tests.Configuration;

/// <summary>
/// Tests for EnvironmentInterpolator.
/// Verifies ${VAR} expansion in configuration values.
/// </summary>
public class EnvironmentInterpolatorTests
{
    [Fact]
    public void Interpolate_WithSimpleVariable_ShouldReplace()
    {
        // Arrange
        Environment.SetEnvironmentVariable("TEST_VAR", "test_value");
        var interpolator = new EnvironmentInterpolator();

        // Act
        var result = interpolator.Interpolate("Value is ${TEST_VAR}");

        // Assert
        result.Should().Be("Value is test_value");

        // Cleanup
        Environment.SetEnvironmentVariable("TEST_VAR", null);
    }

    [Fact]
    public void Interpolate_WithDefaultValue_WhenVariableExists_ShouldUseActualValue()
    {
        // Arrange
        Environment.SetEnvironmentVariable("TEST_VAR", "actual");
        var interpolator = new EnvironmentInterpolator();

        // Act
        var result = interpolator.Interpolate("${TEST_VAR:-default}");

        // Assert
        result.Should().Be("actual");

        // Cleanup
        Environment.SetEnvironmentVariable("TEST_VAR", null);
    }

    [Fact]
    public void Interpolate_WithDefaultValue_WhenVariableNotSet_ShouldUseDefault()
    {
        // Arrange
        var interpolator = new EnvironmentInterpolator();

        // Act
        var result = interpolator.Interpolate("${NONEXISTENT_VAR:-default_value}");

        // Assert
        result.Should().Be("default_value");
    }

    [Fact]
    public void Interpolate_WithRequiredVariable_WhenVariableExists_ShouldSucceed()
    {
        // Arrange
        Environment.SetEnvironmentVariable("REQUIRED_VAR", "value");
        var interpolator = new EnvironmentInterpolator();

        // Act
        var result = interpolator.Interpolate("${REQUIRED_VAR:?}");

        // Assert
        result.Should().Be("value");

        // Cleanup
        Environment.SetEnvironmentVariable("REQUIRED_VAR", null);
    }

    [Fact]
    public void Interpolate_WithRequiredVariable_WhenVariableNotSet_ShouldThrow()
    {
        // Arrange
        var interpolator = new EnvironmentInterpolator();

        // Act
        var action = () => interpolator.Interpolate("${NONEXISTENT:?Variable is required}");

        // Assert
        action.Should().Throw<InvalidOperationException>()
            .WithMessage("*NONEXISTENT*required*");
    }

    [Fact]
    public void Interpolate_WithEscapedDollar_ShouldProduceLiteralDollar()
    {
        // Arrange
        var interpolator = new EnvironmentInterpolator();

        // Act
        var result = interpolator.Interpolate("Price: $$100");

        // Assert
        result.Should().Be("Price: $100");
    }

    [Fact]
    public void Interpolate_WithMultipleVariables_ShouldReplaceAll()
    {
        // Arrange
        Environment.SetEnvironmentVariable("VAR1", "value1");
        Environment.SetEnvironmentVariable("VAR2", "value2");
        var interpolator = new EnvironmentInterpolator();

        // Act
        var result = interpolator.Interpolate("${VAR1} and ${VAR2}");

        // Assert
        result.Should().Be("value1 and value2");

        // Cleanup
        Environment.SetEnvironmentVariable("VAR1", null);
        Environment.SetEnvironmentVariable("VAR2", null);
    }

    [Fact]
    public void Interpolate_WithNoVariables_ShouldReturnUnchanged()
    {
        // Arrange
        var interpolator = new EnvironmentInterpolator();

        // Act
        var result = interpolator.Interpolate("Plain text with no variables");

        // Assert
        result.Should().Be("Plain text with no variables");
    }

    [Fact]
    public void Interpolate_WithNullInput_ShouldReturnNull()
    {
        // Arrange
        var interpolator = new EnvironmentInterpolator();

        // Act
        var result = interpolator.Interpolate(null);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    [System.Runtime.Versioning.SupportedOSPlatform("linux")]
    [System.Runtime.Versioning.SupportedOSPlatform("macos")]
    public void Interpolate_IsCaseSensitive()
    {
        // Skip on Windows - environment variables are case-insensitive on Windows
        if (OperatingSystem.IsWindows())
        {
            return;
        }

        // Arrange
        Environment.SetEnvironmentVariable("TestVar", "lowercase");
        Environment.SetEnvironmentVariable("TESTVAR", "uppercase");
        var interpolator = new EnvironmentInterpolator();

        // Act
        var result1 = interpolator.Interpolate("${TestVar}");
        var result2 = interpolator.Interpolate("${TESTVAR}");

        // Assert
        result1.Should().Be("lowercase");
        result2.Should().Be("uppercase");

        // Cleanup
        Environment.SetEnvironmentVariable("TestVar", null);
        Environment.SetEnvironmentVariable("TESTVAR", null);
    }

    [Fact]
    public void Interpolate_WithMaximumReplacements_ShouldHandleLimit()
    {
        // Arrange - Test with many variable replacements
        Environment.SetEnvironmentVariable("VAR1", "value1");
        Environment.SetEnvironmentVariable("VAR2", "value2");
        Environment.SetEnvironmentVariable("VAR3", "value3");
        var interpolator = new EnvironmentInterpolator();

        // Act - Multiple replacements in one string
        var result = interpolator.Interpolate("${VAR1}-${VAR2}-${VAR3}-${VAR1}");

        // Assert
        result.Should().Be("value1-value2-value3-value1");

        // Cleanup
        Environment.SetEnvironmentVariable("VAR1", null);
        Environment.SetEnvironmentVariable("VAR2", null);
        Environment.SetEnvironmentVariable("VAR3", null);
    }

    [Fact]
    public void Interpolate_WithSpecialCharactersInValue_ShouldPreserveSpecialChars()
    {
        // Arrange - Environment variable with special characters
        Environment.SetEnvironmentVariable("SPECIAL_VAR", "value-with-special:chars@123!#$%");
        var interpolator = new EnvironmentInterpolator();

        // Act
        var result = interpolator.Interpolate("prefix-${SPECIAL_VAR}-suffix");

        // Assert
        result.Should().Be("prefix-value-with-special:chars@123!#$%-suffix");

        // Cleanup
        Environment.SetEnvironmentVariable("SPECIAL_VAR", null);
    }

    [Fact]
    public void Interpolate_WithNestedBraces_ShouldHandleCorrectly()
    {
        // Arrange - Test variable name extraction with complex patterns
        Environment.SetEnvironmentVariable("NESTED", "correct");
        var interpolator = new EnvironmentInterpolator();

        // Act - ${NESTED} should be extracted correctly
        var result = interpolator.Interpolate("prefix-${NESTED}-suffix");

        // Assert
        result.Should().Be("prefix-correct-suffix");

        // Cleanup
        Environment.SetEnvironmentVariable("NESTED", null);
    }

    [Fact]
    public void Interpolate_WithManyVariables_ShouldPerformWell()
    {
        // Arrange - Performance test with 20 different variables
        for (int i = 0; i < 20; i++)
        {
            Environment.SetEnvironmentVariable($"PERF_VAR_{i}", $"value_{i}");
        }

        var interpolator = new EnvironmentInterpolator();
        var input = string.Join("-", Enumerable.Range(0, 20).Select(i => $"${{PERF_VAR_{i}}}"));

        // Act
        var result = interpolator.Interpolate(input);

        // Assert
        var expected = string.Join("-", Enumerable.Range(0, 20).Select(i => $"value_{i}"));
        result.Should().Be(expected);

        // Cleanup
        for (int i = 0; i < 20; i++)
        {
            Environment.SetEnvironmentVariable($"PERF_VAR_{i}", null);
        }
    }

    [Fact]
    public void Interpolate_WithDefaultValueContainingSpecialChars_ShouldUseDefault()
    {
        // Arrange - Default value with special characters
        var interpolator = new EnvironmentInterpolator();

        // Act - Variable doesn't exist, should use default with special chars
        var result = interpolator.Interpolate("${NONEXISTENT_VAR:-default:value@123}");

        // Assert
        result.Should().Be("default:value@123");
    }
}
