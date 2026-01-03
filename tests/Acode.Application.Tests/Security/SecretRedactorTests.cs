using Acode.Application.Security;
using FluentAssertions;
using Xunit;

namespace Acode.Application.Tests.Security;

/// <summary>
/// Tests for ISecretRedactor interface.
/// </summary>
public sealed class SecretRedactorTests
{
    [Fact]
    public void ISecretRedactor_Interface_Exists()
    {
        // Assert
        typeof(ISecretRedactor).Should().NotBeNull();
        typeof(ISecretRedactor).IsInterface.Should().BeTrue();
    }

    [Fact]
    public void ISecretRedactor_HasRedactMethods()
    {
        // Act
        var methods = typeof(ISecretRedactor).GetMethods()
            .Where(m => m.Name == "Redact")
            .ToArray();

        // Assert
        methods.Should().HaveCount(2);
        methods.Should().AllSatisfy(m => m.ReturnType.Should().Be(typeof(RedactedContent)));
    }

    [Fact]
    public void RedactedContent_Record_HasRequiredProperties()
    {
        // Arrange & Act
        var redacted = new RedactedContent
        {
            Content = "[REDACTED]",
            RedactionCount = 2,
            SecretTypesFound = new[] { "api_key", "password" }
        };

        // Assert
        redacted.Content.Should().Be("[REDACTED]");
        redacted.RedactionCount.Should().Be(2);
        redacted.SecretTypesFound.Should().HaveCount(2);
    }

    [Fact]
    public void RedactedContent_Record_SupportsValueEquality()
    {
        // Arrange
        var redacted1 = new RedactedContent
        {
            Content = "test",
            RedactionCount = 1,
            SecretTypesFound = Array.Empty<string>()
        };

        var redacted2 = new RedactedContent
        {
            Content = "test",
            RedactionCount = 1,
            SecretTypesFound = Array.Empty<string>()
        };

        // Act & Assert
        redacted1.Should().Be(redacted2);
    }
}
