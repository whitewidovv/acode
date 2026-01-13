namespace Acode.Infrastructure.Tests.Common;

using Acode.Infrastructure.Common;
using FluentAssertions;

/// <summary>
/// Tests for SnakeCaseNamingPolicy following TDD (RED phase).
/// Ensures property names are converted to snake_case for JSON serialization.
/// </summary>
public class SnakeCaseNamingPolicyTests
{
    [Theory]
    [InlineData("PropertyName", "property_name")]
    [InlineData("SupportsStreaming", "supports_streaming")]
    [InlineData("MaxContextLength", "max_context_length")]
    [InlineData("ID", "id")]
    [InlineData("IOError", "io_error")]
    [InlineData("XMLParser", "xml_parser")]
    [InlineData("simpleTest", "simple_test")]
    [InlineData("alreadysnakecase", "alreadysnakecase")]
    [InlineData("PascalCase", "pascal_case")]
    [InlineData("camelCase", "camel_case")]
    [InlineData("ABC", "abc")]
    [InlineData("ABCDef", "abc_def")]
    [InlineData("A", "a")]
    [InlineData("AB", "ab")]
    public void ConvertName_ConvertsToSnakeCase(string input, string expected)
    {
        // Arrange
        var policy = new SnakeCaseNamingPolicy();

        // Act
        var result = policy.ConvertName(input);

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void ConvertName_HandlesNullInput()
    {
        // Arrange
        var policy = new SnakeCaseNamingPolicy();

        // Act
        var result = policy.ConvertName(null!);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void ConvertName_HandlesEmptyString()
    {
        // Arrange
        var policy = new SnakeCaseNamingPolicy();

        // Act
        var result = policy.ConvertName(string.Empty);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void ConvertName_PreservesUnderscores()
    {
        // Arrange
        var policy = new SnakeCaseNamingPolicy();

        // Act
        var result = policy.ConvertName("Already_Has_Underscores");

        // Assert
        result.Should().Be("already_has_underscores");
    }

    [Fact]
    public void ConvertName_HandlesConsecutiveUpperCase()
    {
        // Arrange
        var policy = new SnakeCaseNamingPolicy();

        // Act
        var result = policy.ConvertName("HTTPSConnection");

        // Assert
        result.Should().Be("https_connection");
    }

    [Fact]
    public void ConvertName_HandlesNumbersInName()
    {
        // Arrange
        var policy = new SnakeCaseNamingPolicy();

        // Act
        var result = policy.ConvertName("Property123Name");

        // Assert
        result.Should().Be("property123_name");
    }
}
