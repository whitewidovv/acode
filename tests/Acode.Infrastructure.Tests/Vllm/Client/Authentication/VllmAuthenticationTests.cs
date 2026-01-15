using Acode.Infrastructure.Vllm.Client.Authentication;
using FluentAssertions;

namespace Acode.Infrastructure.Tests.Vllm.Client.Authentication;

public class VllmAuthenticationTests
{
    [Fact]
    public void Should_Include_Bearer_Header()
    {
        // Arrange (FR-087, AC-087): MUST format as "Bearer {key}"
        var handler = new VllmAuthHandler("test-key-123");

        // Act
        var headerValue = handler.GetAuthorizationHeaderValue();

        // Assert
        headerValue.Should().Be("Bearer test-key-123");
    }

    [Fact]
    public void Should_Read_From_Environment()
    {
        // Arrange (FR-086, AC-086): Environment variable overrides config
        var envVarName = $"VLLM_API_KEY_{Guid.NewGuid()}";
        var envKey = "env-key-456";
        Environment.SetEnvironmentVariable(envVarName, envKey);

        try
        {
            // Act
            var handler = new VllmAuthHandler("config-key", envVarName);
            var headerValue = handler.GetAuthorizationHeaderValue();

            // Assert - environment variable should override config
            headerValue.Should().Be($"Bearer {envKey}");
        }
        finally
        {
            Environment.SetEnvironmentVariable(envVarName, null);
        }
    }

    [Fact]
    public void Should_Not_Log_Key()
    {
        // Arrange (FR-088, FR-089, AC-088, AC-089): MUST redact key in logs
        var handler = new VllmAuthHandler("secret-key-789");

        // Act
        var redactedKey = handler.GetRedactedKey();

        // Assert
        redactedKey.Should().Be("[REDACTED]");
        redactedKey.Should().NotContain("secret-key-789");
    }

    [Fact]
    public void Should_Work_Without_Key()
    {
        // Arrange (FR-090, AC-090): MUST work without API key
        var handler = new VllmAuthHandler(null, "NONEXISTENT_VAR");

        // Act
        var headerValue = handler.GetAuthorizationHeaderValue();
        var hasKey = handler.HasApiKey;

        // Assert
        headerValue.Should().BeNull();
        hasKey.Should().BeFalse();
    }

    [Fact]
    public void Should_Return_No_Key_String_When_No_Key_Configured()
    {
        // Arrange (FR-088, AC-088): Redaction also works when no key
        var handler = new VllmAuthHandler(null, "NONEXISTENT_VAR");

        // Act
        var redactedKey = handler.GetRedactedKey();

        // Assert
        redactedKey.Should().Be("(no key)");
    }

    [Fact]
    public void Should_Use_Default_Environment_Variable_Name()
    {
        // Arrange (FR-086, AC-086): Default env var is VLLM_API_KEY
        var handler = new VllmAuthHandler("config-key");  // No env var name specified

        // Act
        var headerValue = handler.GetAuthorizationHeaderValue();

        // Assert - should use config key since VLLM_API_KEY is not set
        headerValue.Should().Be("Bearer config-key");
    }

    [Fact]
    public void Should_Ignore_Whitespace_Only_Environment_Variable()
    {
        // Arrange (FR-086, AC-086): Whitespace-only env var should be ignored
        var envVarName = $"VLLM_API_KEY_{Guid.NewGuid()}";
        Environment.SetEnvironmentVariable(envVarName, "   ");

        try
        {
            // Act
            var handler = new VllmAuthHandler("config-key", envVarName);
            var headerValue = handler.GetAuthorizationHeaderValue();

            // Assert - should use config key, not whitespace env var
            headerValue.Should().Be("Bearer config-key");
        }
        finally
        {
            Environment.SetEnvironmentVariable(envVarName, null);
        }
    }
}
