using Acode.Domain.Modes;
using Acode.Domain.Validation;
using Acode.Infrastructure.Network;
using FluentAssertions;

namespace Acode.Integration.Tests.Validation;

/// <summary>
/// Integration tests for endpoint validation.
/// Verifies end-to-end validation flow per Task 001.b lines 608-633.
/// </summary>
public class EndpointValidationIntegrationTests
{
    [SkippableFact]
    public void Integration_LoadDenylistFromFile_AndValidateOpenAi()
    {
        // Arrange
        var denylistProvider = new DenylistProvider();
        var denylistPath = Path.Combine(
            Directory.GetCurrentDirectory(),
            "..",
            "..",
            "..",
            "..",
            "..",
            "data",
            "denylist.json");

        // Skip test if denylist file doesn't exist (e.g., in certain CI/CD environments)
        // This provides better visibility in test reports compared to silent return
        Skip.IfNot(File.Exists(denylistPath), $"Denylist file not found at: {denylistPath}");

        var patterns = denylistProvider.LoadFromFile(denylistPath);
        var validator = new EndpointValidator();
        var openAiUri = new Uri("https://api.openai.com/v1/chat/completions");

        // Act
        var result = validator.Validate(openAiUri, OperatingMode.LocalOnly);

        // Assert
        result.IsAllowed.Should().BeFalse();
        result.ViolatedConstraint.Should().Be("HC-01");
        patterns.Should().HaveCountGreaterOrEqualTo(11); // At least 11 from spec
    }

    [Fact]
    public void Integration_OpenAiApi_DeniedInLocalOnlyMode()
    {
        // Arrange
        var validator = new EndpointValidator();
        var openAiUri = new Uri("https://api.openai.com/v1/chat/completions");

        // Act
        var result = validator.Validate(openAiUri, OperatingMode.LocalOnly);

        // Assert
        result.IsAllowed.Should().BeFalse();
        result.ViolatedConstraint.Should().Be("HC-01");
        result.Reason.Should().Contain("LocalOnly");
        result.Reason.Should().Contain("Burst mode");
    }

    [Fact]
    public void Integration_AnthropicApi_DeniedInLocalOnlyMode()
    {
        // Arrange
        var validator = new EndpointValidator();
        var anthropicUri = new Uri("https://api.anthropic.com/v1/messages");

        // Act
        var result = validator.Validate(anthropicUri, OperatingMode.LocalOnly);

        // Assert
        result.IsAllowed.Should().BeFalse();
        result.ViolatedConstraint.Should().Be("HC-01");
        result.Reason.Should().Contain("LocalOnly");
    }

    [Fact]
    public void Integration_OllamaLocalhost_AllowedInLocalOnlyMode()
    {
        // Arrange
        var validator = new EndpointValidator();
        var ollamaUri = new Uri("http://localhost:11434/api/generate");

        // Act
        var result = validator.Validate(ollamaUri, OperatingMode.LocalOnly);

        // Assert
        result.IsAllowed.Should().BeTrue();
        result.Reason.Should().Contain("allowlist");
    }

    [Fact]
    public void Integration_AllApis_AllowedInBurstMode()
    {
        // Arrange
        var validator = new EndpointValidator();
        var uris = new[]
        {
            new Uri("https://api.openai.com/v1/chat/completions"),
            new Uri("https://api.anthropic.com/v1/messages"),
            new Uri("https://generativelanguage.googleapis.com/v1/models"),
            new Uri("http://localhost:11434/api/generate")
        };

        // Act & Assert
        foreach (var uri in uris)
        {
            var result = validator.Validate(uri, OperatingMode.Burst);
            result.IsAllowed.Should().BeTrue($"{uri} should be allowed in Burst mode");

            // Reason can be either "Burst" (for non-allowlisted endpoints) or "allowlist" (for localhost)
            var expectedReason = $"Reason should indicate either Burst mode or allowlist for {uri}";
            result.Reason.Should().Match(
                r => r.Contains("Burst") || r.Contains("allowlist"),
                expectedReason);
        }
    }

    [Fact]
    public void Integration_NoApis_AllowedInAirgappedMode()
    {
        // Arrange
        var validator = new EndpointValidator();
        var uris = new[]
        {
            new Uri("http://localhost:11434/api/generate"),
            new Uri("http://127.0.0.1:11434/api/generate"),
            new Uri("https://api.openai.com/v1/chat/completions"),
            new Uri("https://api.anthropic.com/v1/messages")
        };

        // Act & Assert
        foreach (var uri in uris)
        {
            var result = validator.Validate(uri, OperatingMode.Airgapped);
            result.IsAllowed.Should().BeFalse($"{uri} should be denied in Airgapped mode");
            result.ViolatedConstraint.Should().Be("HC-02");
            result.Reason.Should().Contain("Airgapped");
        }
    }

    [Fact]
    public void Integration_CustomAllowlistEntry_Works()
    {
        // Arrange
        var customAllowlist = new TestAllowlistProvider();
        var validator = new EndpointValidator(customAllowlist);
        var customUri = new Uri("http://custom-llm.internal:8080/api/generate");

        // Act
        var result = validator.Validate(customUri, OperatingMode.LocalOnly);

        // Assert
        result.IsAllowed.Should().BeTrue();
        result.Reason.Should().Contain("allowlist");
    }

    [Fact]
    public void Integration_AllowlistCheckedBeforeDenylist()
    {
        // Arrange - Create allowlist with OpenAI API (normally denied)
        var allowlistWithOpenAi = new TestAllowlistProvider(allowOpenAi: true);
        var validator = new EndpointValidator(allowlistWithOpenAi);
        var openAiUri = new Uri("https://api.openai.com/v1/chat/completions");

        // Act
        var result = validator.Validate(openAiUri, OperatingMode.LocalOnly);

        // Assert
        result.IsAllowed.Should().BeTrue("Allowlist should take precedence over denylist");
        result.Reason.Should().Contain("allowlist");
    }

    [Fact]
    public void Integration_ErrorMessages_IncludeRemediation()
    {
        // Arrange
        var validator = new EndpointValidator();
        var openAiUri = new Uri("https://api.openai.com/v1/chat/completions");

        // Act
        var result = validator.Validate(openAiUri, OperatingMode.LocalOnly);

        // Assert
        result.IsAllowed.Should().BeFalse();
        result.Reason.Should().Contain("Burst mode", "Remediation should mention Burst mode");
        result.Reason.Should().Contain("localhost", "Remediation should mention local alternative");
    }

    [Fact]
    public void Integration_ErrorMessages_IncludeMatchedPattern()
    {
        // Arrange
        var validator = new EndpointValidator();
        var openAiUri = new Uri("https://api.openai.com/v1/chat/completions");

        // Act
        var result = validator.Validate(openAiUri, OperatingMode.LocalOnly);

        // Assert
        result.IsAllowed.Should().BeFalse();
        result.Reason.Should().Contain("api.openai.com", "Error should include the matched host");
    }

    /// <summary>
    /// Test implementation of IAllowlistProvider for integration tests.
    /// </summary>
    private class TestAllowlistProvider : IAllowlistProvider
    {
        private readonly bool _allowOpenAi;

        public TestAllowlistProvider(bool allowOpenAi = false)
        {
            _allowOpenAi = allowOpenAi;
        }

        public IReadOnlyList<AllowlistEntry> GetDefaultAllowlist()
        {
            var entries = new List<AllowlistEntry>
            {
                new() { Host = "custom-llm.internal", Ports = new[] { 8080 }, Reason = "Internal custom LLM" }
            };

            if (_allowOpenAi)
            {
                entries.Add(new AllowlistEntry
                {
                    Host = "api.openai.com",
                    Ports = null, // Any port
                    Reason = "Test: OpenAI explicitly allowed"
                });
            }

            return entries.AsReadOnly();
        }

        public bool IsAllowed(Uri uri)
        {
            return GetDefaultAllowlist().Any(entry => entry.Matches(uri));
        }
    }
}
