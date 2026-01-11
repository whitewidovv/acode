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

    [Theory]
    [InlineData("https://mycompany.openai.azure.com/openai/deployments/gpt-4")]
    [InlineData("https://test-instance.openai.azure.com/v1/chat")]
    [InlineData("https://production.openai.azure.com/")]
    public void LlmApiDenylist_ShouldDenyAzureOpenAiInstances(string url)
    {
        // Arrange
        var uri = new Uri(url);

        // Act
        var isDenied = LlmApiDenylist.IsDenied(uri);

        // Assert
        isDenied.Should().BeTrue($"{url} should be denied (Azure OpenAI regex pattern)");
    }

    [Theory]
    [InlineData("https://bedrock-runtime.us-east-1.amazonaws.com/model/invoke")]
    [InlineData("https://bedrock-runtime.ap-south-1.amazonaws.com/")]
    [InlineData("https://bedrock-runtime.eu-central-1.amazonaws.com/")]
    [InlineData("https://bedrock.us-west-2.amazonaws.com/")]
    public void LlmApiDenylist_ShouldDenyAwsBedrockAllRegions(string url)
    {
        // Arrange
        var uri = new Uri(url);

        // Act
        var isDenied = LlmApiDenylist.IsDenied(uri);

        // Assert
        isDenied.Should().BeTrue($"{url} should be denied (AWS Bedrock regex pattern)");
    }

    [Theory]
    [InlineData("https://chat.openai.com/")]
    [InlineData("https://platform.openai.com/docs")]
    [InlineData("https://beta.openai.com/")]
    public void LlmApiDenylist_ShouldDenyOpenAiSubdomains(string url)
    {
        // Arrange
        var uri = new Uri(url);

        // Act
        var isDenied = LlmApiDenylist.IsDenied(uri);

        // Assert
        isDenied.Should().BeTrue($"{url} should be denied (*.openai.com wildcard pattern)");
    }

    [Theory]
    [InlineData("https://console.anthropic.com/")]
    [InlineData("https://docs.anthropic.com/")]
    public void LlmApiDenylist_ShouldDenyAnthropicSubdomains(string url)
    {
        // Arrange
        var uri = new Uri(url);

        // Act
        var isDenied = LlmApiDenylist.IsDenied(uri);

        // Assert
        isDenied.Should().BeTrue($"{url} should be denied (*.anthropic.com wildcard pattern)");
    }

    [Fact]
    public void LlmApiDenylist_ShouldNotDenyRootDomainsWithoutSubdomain()
    {
        // Arrange - openai.com (no subdomain) should NOT be denied by *.openai.com
        var uri = new Uri("https://openai.com/");

        // Act
        var isDenied = LlmApiDenylist.IsDenied(uri);

        // Assert - wildcards require a subdomain
        isDenied.Should().BeFalse("*.openai.com should not match openai.com (no subdomain)");
    }
}
