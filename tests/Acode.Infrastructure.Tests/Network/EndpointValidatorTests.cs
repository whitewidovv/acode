using System.Net;
using Acode.Infrastructure.Network;
using FluentAssertions;

namespace Acode.Infrastructure.Tests.Network;

/// <summary>
/// Tests for EndpointValidator implementation.
/// Verifies endpoint validation logic per Task 001.b.
/// </summary>
public class EndpointValidatorTests
{
    [Fact]
    public void EndpointValidator_LocalOnlyMode_ShouldDenyOpenAiApi()
    {
        // Arrange
        var validator = new EndpointValidator();
        var uri = new Uri("https://api.openai.com/v1/chat/completions");

        // Act
        var result = validator.Validate(uri, Domain.Modes.OperatingMode.LocalOnly);

        // Assert
        result.IsAllowed.Should().BeFalse();
        result.Reason.Should().Contain("denied");
        result.ViolatedConstraint.Should().Be("HC-01");
    }

    [Fact]
    public void EndpointValidator_LocalOnlyMode_ShouldAllowLocalhost()
    {
        // Arrange
        var validator = new EndpointValidator();
        var uri = new Uri("http://localhost:11434/api/generate");

        // Act
        var result = validator.Validate(uri, Domain.Modes.OperatingMode.LocalOnly);

        // Assert
        result.IsAllowed.Should().BeTrue();
        result.Reason.Should().Contain("allowlist");
    }

    [Fact]
    public void EndpointValidator_LocalOnlyMode_ShouldAllow127001()
    {
        // Arrange
        var validator = new EndpointValidator();
        var uri = new Uri("http://127.0.0.1:11434/api/generate");

        // Act
        var result = validator.Validate(uri, Domain.Modes.OperatingMode.LocalOnly);

        // Assert
        result.IsAllowed.Should().BeTrue();
    }

    [Fact]
    public void EndpointValidator_BurstMode_ShouldAllowOpenAiApi()
    {
        // Arrange
        var validator = new EndpointValidator();
        var uri = new Uri("https://api.openai.com/v1/chat/completions");

        // Act
        var result = validator.Validate(uri, Domain.Modes.OperatingMode.Burst);

        // Assert
        result.IsAllowed.Should().BeTrue();
        result.Reason.Should().Contain("Burst mode");
    }

    [Fact]
    public void EndpointValidator_AirgappedMode_ShouldDenyEverything()
    {
        // Arrange
        var validator = new EndpointValidator();
        var uris = new[]
        {
            new Uri("http://localhost:11434/"),
            new Uri("http://127.0.0.1:11434/"),
            new Uri("https://api.openai.com/"),
        };

        // Act & Assert
        foreach (var uri in uris)
        {
            var result = validator.Validate(uri, Domain.Modes.OperatingMode.Airgapped);
            result.IsAllowed.Should().BeFalse($"{uri} should be denied in Airgapped mode");
            result.ViolatedConstraint.Should().Be("HC-02");
        }
    }

    [Fact]
    public void EndpointValidator_ShouldCheckAllowlistBeforeDenylist()
    {
        // Arrange - localhost is in both allowlist and could match denylist patterns
        var validator = new EndpointValidator();
        var uri = new Uri("http://localhost:11434/");

        // Act
        var result = validator.Validate(uri, Domain.Modes.OperatingMode.LocalOnly);

        // Assert - allowlist wins
        result.IsAllowed.Should().BeTrue();
    }

    [Fact]
    public void EndpointValidator_WithNullUri_ShouldBlock()
    {
        // Arrange
        var validator = new EndpointValidator();

        // Act
        var act = () => validator.Validate(null!, Domain.Modes.OperatingMode.LocalOnly);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void EndpointValidator_DeniedResult_ShouldIncludeRemediation()
    {
        // Arrange
        var validator = new EndpointValidator();
        var uri = new Uri("https://api.anthropic.com/v1/messages");

        // Act
        var result = validator.Validate(uri, Domain.Modes.OperatingMode.LocalOnly);

        // Assert
        result.IsAllowed.Should().BeFalse();
        result.Reason.Should().Contain("LocalOnly");
        result.ViolatedConstraint.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void EndpointValidator_ValidateIp_LoopbackShouldBeAllowedInLocalOnly()
    {
        // Arrange
        var validator = new EndpointValidator();
        var loopback = IPAddress.Loopback; // 127.0.0.1

        // Act
        var result = validator.ValidateIp(loopback, Domain.Modes.OperatingMode.LocalOnly);

        // Assert
        result.IsAllowed.Should().BeTrue();
    }

    [Fact]
    public void EndpointValidator_ValidateIp_ExternalIpShouldBeDeniedInLocalOnly()
    {
        // Arrange
        var validator = new EndpointValidator();
        var externalIp = IPAddress.Parse("8.8.8.8"); // Google DNS

        // Act
        var result = validator.ValidateIp(externalIp, Domain.Modes.OperatingMode.LocalOnly);

        // Assert
        result.IsAllowed.Should().BeFalse();
        result.ViolatedConstraint.Should().Be("HC-01");
    }

    [Fact]
    public void EndpointValidator_ValidateIp_Ipv6LoopbackShouldBeAllowed()
    {
        // Arrange
        var validator = new EndpointValidator();
        var ipv6Loopback = IPAddress.IPv6Loopback; // ::1

        // Act
        var result = validator.ValidateIp(ipv6Loopback, Domain.Modes.OperatingMode.LocalOnly);

        // Assert
        result.IsAllowed.Should().BeTrue();
    }

    [Fact]
    public void EndpointValidator_LocalOnlyMode_ShouldDenyAzureOpenAi()
    {
        // Arrange
        var validator = new EndpointValidator();
        var uri = new Uri("https://mycompany.openai.azure.com/openai/deployments/gpt-4");

        // Act
        var result = validator.Validate(uri, Domain.Modes.OperatingMode.LocalOnly);

        // Assert
        result.IsAllowed.Should().BeFalse();
        result.ViolatedConstraint.Should().Be("HC-01");
    }

    [Fact]
    public void EndpointValidator_LocalOnlyMode_ShouldDenyAwsBedrock()
    {
        // Arrange
        var validator = new EndpointValidator();
        var uri = new Uri("https://bedrock-runtime.us-east-1.amazonaws.com/model/invoke");

        // Act
        var result = validator.Validate(uri, Domain.Modes.OperatingMode.LocalOnly);

        // Assert
        result.IsAllowed.Should().BeFalse();
        result.ViolatedConstraint.Should().Be("HC-01");
    }

    [Fact]
    public void EndpointValidator_LocalOnlyMode_ShouldDenyOpenAiSubdomains()
    {
        // Arrange
        var validator = new EndpointValidator();
        var uri = new Uri("https://chat.openai.com/");

        // Act
        var result = validator.Validate(uri, Domain.Modes.OperatingMode.LocalOnly);

        // Assert
        result.IsAllowed.Should().BeFalse();
        result.ViolatedConstraint.Should().Be("HC-01");
    }
}
