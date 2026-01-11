using Acode.Domain.Validation;
using FluentAssertions;

namespace Acode.Domain.Tests.Validation;

/// <summary>
/// Tests for EndpointPattern record.
/// Verifies pattern matching logic per Task 001.b.
/// </summary>
public class EndpointPatternTests
{
    [Fact]
    public void EndpointPattern_ExactMatch_ShouldMatchExactHost()
    {
        // Arrange
        var pattern = new EndpointPattern
        {
            Pattern = "api.openai.com",
            Type = PatternType.Exact,
            Description = "OpenAI API"
        };
        var uri = new Uri("https://api.openai.com/v1/chat/completions");

        // Act
        var matches = pattern.Matches(uri);

        // Assert
        matches.Should().BeTrue();
    }

    [Fact]
    public void EndpointPattern_ExactMatch_ShouldNotMatchDifferentHost()
    {
        // Arrange
        var pattern = new EndpointPattern
        {
            Pattern = "api.openai.com",
            Type = PatternType.Exact
        };
        var uri = new Uri("https://chat.openai.com/");

        // Act
        var matches = pattern.Matches(uri);

        // Assert
        matches.Should().BeFalse();
    }

    [Fact]
    public void EndpointPattern_ExactMatch_ShouldBeCaseInsensitive()
    {
        // Arrange
        var pattern = new EndpointPattern
        {
            Pattern = "API.OpenAI.com",
            Type = PatternType.Exact
        };
        var uri = new Uri("https://api.openai.com/v1/models");

        // Act
        var matches = pattern.Matches(uri);

        // Assert
        matches.Should().BeTrue();
    }

    [Fact]
    public void EndpointPattern_WildcardMatch_ShouldMatchSubdomain()
    {
        // Arrange
        var pattern = new EndpointPattern
        {
            Pattern = "*.openai.com",
            Type = PatternType.Wildcard
        };
        var uri = new Uri("https://chat.openai.com/");

        // Act
        var matches = pattern.Matches(uri);

        // Assert
        matches.Should().BeTrue();
    }

    [Fact]
    public void EndpointPattern_WildcardMatch_ShouldMatchApiSubdomain()
    {
        // Arrange
        var pattern = new EndpointPattern
        {
            Pattern = "*.openai.com",
            Type = PatternType.Wildcard
        };
        var uri = new Uri("https://api.openai.com/v1/models");

        // Act
        var matches = pattern.Matches(uri);

        // Assert
        matches.Should().BeTrue();
    }

    [Fact]
    public void EndpointPattern_WildcardMatch_ShouldNotMatchRootDomain()
    {
        // Arrange
        var pattern = new EndpointPattern
        {
            Pattern = "*.openai.com",
            Type = PatternType.Wildcard
        };
        var uri = new Uri("https://openai.com/");

        // Act
        var matches = pattern.Matches(uri);

        // Assert - per spec, *.foo.com should NOT match foo.com (no subdomain)
        matches.Should().BeFalse();
    }

    [Fact]
    public void EndpointPattern_WildcardMatch_ShouldBeCaseInsensitive()
    {
        // Arrange
        var pattern = new EndpointPattern
        {
            Pattern = "*.OpenAI.COM",
            Type = PatternType.Wildcard
        };
        var uri = new Uri("https://chat.openai.com/");

        // Act
        var matches = pattern.Matches(uri);

        // Assert
        matches.Should().BeTrue();
    }

    [Fact]
    public void EndpointPattern_RegexMatch_ShouldMatchAzurePattern()
    {
        // Arrange
        var pattern = new EndpointPattern
        {
            Pattern = @".*\.openai\.azure\.com",
            Type = PatternType.Regex
        };
        var uri = new Uri("https://myinstance.openai.azure.com/");

        // Act
        var matches = pattern.Matches(uri);

        // Assert
        matches.Should().BeTrue();
    }

    [Fact]
    public void EndpointPattern_RegexMatch_ShouldMatchBedrockPattern()
    {
        // Arrange
        var pattern = new EndpointPattern
        {
            Pattern = @"bedrock.*\.amazonaws\.com",
            Type = PatternType.Regex
        };
        var uri = new Uri("https://bedrock-runtime.us-east-1.amazonaws.com/");

        // Act
        var matches = pattern.Matches(uri);

        // Assert
        matches.Should().BeTrue();
    }

    [Fact]
    public void EndpointPattern_RegexMatch_ShouldBeCaseInsensitive()
    {
        // Arrange
        var pattern = new EndpointPattern
        {
            Pattern = @"bedrock.*\.amazonaws\.com",
            Type = PatternType.Regex
        };
        var uri = new Uri("https://BEDROCK-RUNTIME.US-WEST-2.AMAZONAWS.COM/");

        // Act
        var matches = pattern.Matches(uri);

        // Assert
        matches.Should().BeTrue();
    }

    [Fact]
    public void EndpointPattern_RegexMatch_WithInvalidPattern_ShouldThrowOnFirstUse()
    {
        // Arrange
        var pattern = new EndpointPattern
        {
            Pattern = "[invalid(regex",
            Type = PatternType.Regex
        };
        var uri = new Uri("https://test.com/");

        // Act
        var act = () => pattern.Matches(uri);

        // Assert - exception thrown on first use (lazy evaluation)
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Invalid regex pattern*");
    }

    [Fact]
    public void EndpointPattern_ShouldSupportRecordEquality()
    {
        // Arrange
        var pattern1 = new EndpointPattern
        {
            Pattern = "api.openai.com",
            Type = PatternType.Exact,
            Description = "Test"
        };
        var pattern2 = new EndpointPattern
        {
            Pattern = "api.openai.com",
            Type = PatternType.Exact,
            Description = "Test"
        };

        // Assert
        pattern1.Should().Be(pattern2);
    }

    [Fact]
    public void EndpointPattern_RegexShouldBePreCompiled()
    {
        // Arrange
        var pattern = new EndpointPattern
        {
            Pattern = @"test\.com",
            Type = PatternType.Regex
        };

        // Act
        var uri1 = new Uri("https://test.com/");
        var uri2 = new Uri("https://test.com/other");

        var result1 = pattern.Matches(uri1);
        var result2 = pattern.Matches(uri2);

        // Assert - both should use same compiled regex (performance)
        result1.Should().BeTrue();
        result2.Should().BeTrue();
    }

    [Fact]
    public void EndpointPattern_UnknownPatternType_ShouldReturnFalse()
    {
        // Arrange
        var pattern = new EndpointPattern
        {
            Pattern = "test.com",
            Type = (PatternType)999 // Invalid type
        };
        var uri = new Uri("https://test.com/");

        // Act
        var matches = pattern.Matches(uri);

        // Assert
        matches.Should().BeFalse();
    }
}
