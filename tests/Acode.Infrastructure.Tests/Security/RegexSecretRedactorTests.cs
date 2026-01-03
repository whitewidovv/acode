using Acode.Infrastructure.Security;
using FluentAssertions;
using Xunit;

namespace Acode.Infrastructure.Tests.Security;

/// <summary>
/// Tests for RegexSecretRedactor implementation.
/// </summary>
public sealed class RegexSecretRedactorTests
{
    private readonly RegexSecretRedactor _redactor;

    public RegexSecretRedactorTests()
    {
        _redactor = new RegexSecretRedactor();
    }

    [Theory]
    [InlineData("password = 'secret123'")]
    [InlineData("PASSWORD=\"MyP@ssw0rd\"")]
    [InlineData("pwd: admin123")]
    public void Redact_PasswordPattern_RedactsSecret(string input)
    {
        // Act
        var result = _redactor.Redact(input);

        // Assert
        result.Content.Should().Contain("[REDACTED]");
        result.RedactionCount.Should().BeGreaterThan(0);
        result.SecretTypesFound.Should().Contain("password");
    }

    [Theory]
    [InlineData("API_KEY=sk-1234567890abcdef")]
    [InlineData("apiKey: 'abcd1234567890'")]
    [InlineData("api-key = token123456")]
    public void Redact_ApiKeyPattern_RedactsSecret(string input)
    {
        // Act
        var result = _redactor.Redact(input);

        // Assert
        result.Content.Should().Contain("[REDACTED]");
        result.RedactionCount.Should().BeGreaterThan(0);
        result.SecretTypesFound.Should().Contain("api_key");
    }

    [Theory]
    [InlineData("Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9")]
    [InlineData("token: eyJ0eXAiOiJKV1QiLCJhbGciOiJIUzI1NiJ9")]
    public void Redact_TokenPattern_RedactsSecret(string input)
    {
        // Act
        var result = _redactor.Redact(input);

        // Assert
        result.Content.Should().Contain("[REDACTED]");
        result.RedactionCount.Should().BeGreaterThan(0);
        result.SecretTypesFound.Should().Contain("token");
    }

    [Fact]
    public void Redact_NoSecrets_ReturnsOriginal()
    {
        // Arrange
        var input = "This is normal text with no secrets.";

        // Act
        var result = _redactor.Redact(input);

        // Assert
        result.Content.Should().Be(input);
        result.RedactionCount.Should().Be(0);
        result.SecretTypesFound.Should().BeEmpty();
    }

    [Fact]
    public void Redact_MultipleSecrets_RedactsAll()
    {
        // Arrange
        var input = "password=secret123 and api_key=sk-1234567890abcdef and token=abc123def456ghi789jkl012mno345pqr";

        // Act
        var result = _redactor.Redact(input);

        // Assert
        result.Content.Should().Contain("[REDACTED]");
        result.RedactionCount.Should().BeGreaterThanOrEqualTo(2);
        result.SecretTypesFound.Should().HaveCountGreaterThanOrEqualTo(2);
    }

    [Fact]
    public void Redact_WithFilePath_IncludesContext()
    {
        // Arrange
        var input = "password=secret123";
        var filePath = ".env";

        // Act
        var result = _redactor.Redact(input, filePath);

        // Assert
        result.Content.Should().Contain("[REDACTED]");
        result.RedactionCount.Should().BeGreaterThan(0);
    }
}
