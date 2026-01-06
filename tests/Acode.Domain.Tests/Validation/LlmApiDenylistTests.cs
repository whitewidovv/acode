using Acode.Domain.Validation;
using FluentAssertions;

namespace Acode.Domain.Tests.Validation;

/// <summary>
/// Tests for LlmApiDenylist.
/// Verifies HC-01 enforcement: no external LLM APIs in LocalOnly mode.
/// </summary>
public class LlmApiDenylistTests
{
    [Theory]
    [InlineData("https://api.openai.com/v1/chat/completions")]
    [InlineData("https://api.anthropic.com/v1/messages")]
    [InlineData("https://api.cohere.ai/v1/generate")]
    [InlineData("https://generativelanguage.googleapis.com/v1/models")]
    [InlineData("https://api.ai21.com/studio/v1/chat")]
    public void LlmApiDenylist_ShouldDenyMajorLlmApis(string url)
    {
        // Arrange
        var uri = new Uri(url);

        // Act
        var isDenied = LlmApiDenylist.IsDenied(uri);

        // Assert
        isDenied.Should().BeTrue($"{url} should be denied per HC-01");
    }

    [Theory]
    [InlineData("https://localhost:11434/api/generate")]
    [InlineData("http://127.0.0.1:11434/api/generate")]
    [InlineData("https://example.com/myapi")]
    [InlineData("https://github.com/user/repo")]
    public void LlmApiDenylist_ShouldAllowNonLlmEndpoints(string url)
    {
        // Arrange
        var uri = new Uri(url);

        // Act
        var isDenied = LlmApiDenylist.IsDenied(uri);

        // Assert
        isDenied.Should().BeFalse($"{url} should not be denied");
    }

    [Fact]
    public void LlmApiDenylist_ShouldIncludeOpenAiDomains()
    {
        // Assert
        LlmApiDenylist.IsDenied(new Uri("https://api.openai.com/any/path")).Should().BeTrue();
        LlmApiDenylist.IsDenied(new Uri("https://openai.azure.com/any/path")).Should().BeTrue();
    }

    [Fact]
    public void LlmApiDenylist_ShouldIncludeAnthropicDomains()
    {
        // Assert
        LlmApiDenylist.IsDenied(new Uri("https://api.anthropic.com/v1/messages")).Should().BeTrue();
    }

    [Fact]
    public void LlmApiDenylist_ShouldIncludeGoogleAiDomains()
    {
        // Assert
        LlmApiDenylist.IsDenied(new Uri("https://generativelanguage.googleapis.com/v1/models")).Should().BeTrue();
    }

    [Fact]
    public void LlmApiDenylist_ShouldIncludeCohereDomains()
    {
        // Assert
        LlmApiDenylist.IsDenied(new Uri("https://api.cohere.ai/v1/generate")).Should().BeTrue();
    }
}
