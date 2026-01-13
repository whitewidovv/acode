namespace Acode.Infrastructure.Tests.Audit;

using System.Collections.Generic;
using Acode.Infrastructure.Audit;
using FluentAssertions;
using Xunit;

/// <summary>
/// Tests for AuditRedactor.
/// Verifies sensitive data redaction in audit logs.
/// </summary>
public sealed class AuditRedactorTests
{
    private readonly AuditRedactor _redactor;

    public AuditRedactorTests()
    {
        _redactor = new AuditRedactor();
    }

    [Theory]
    [InlineData("apiKey: xyz789token")]
    [InlineData("API_KEY=AKIAIOSFODNN7EXAMPLE")]
    [InlineData("x-api-key: secret123")]
    public void Should_Redact_ApiKeys(string input)
    {
        // Act
        var result = _redactor.Redact(input);

        // Assert
        result.Should().Contain("[REDACTED]");
    }

    [Theory]
    [InlineData("password: my_secret_pw")]
    [InlineData("Password: hunter2")]
    [InlineData("db_password=verysecret")]
    [InlineData("\"password\": \"secret123\"")]
    public void Should_Redact_Passwords(string input)
    {
        // Act
        var result = _redactor.Redact(input);

        // Assert
        result.Should().Contain("[REDACTED]");
        result.Should().NotContain("my_secret_pw");
        result.Should().NotContain("hunter2");
        result.Should().NotContain("verysecret");
        result.Should().NotContain("secret123");
    }

    [Theory]
    [InlineData("token: ghp_abc123def456")]
    [InlineData("access_token: ghp_1234567890abcdef")]
    [InlineData("Bearer eyJhbGciOiJIUzI1NiJ9.xxx.yyy")]
    [InlineData("Authorization: Bearer abc123")]
    public void Should_Redact_Tokens(string input)
    {
        // Act
        var result = _redactor.Redact(input);

        // Assert
        result.Should().Contain("[REDACTED]");
        result.Should().NotContain("ghp_abc123def456");
        result.Should().NotContain("ghp_1234567890abcdef");
        result.Should().NotContain("eyJhbGciOiJIUzI1NiJ9");
    }

    [Theory]
    [InlineData("client_secret: abcdef123456")]
    [InlineData("aws_secret_access_key=wJalrXUtnFEMI/K7MDENG")]
    [InlineData("private_key=-----BEGIN RSA PRIVATE KEY-----")]
    public void Should_Redact_SecretPatterns(string input)
    {
        // Act
        var result = _redactor.Redact(input);

        // Assert
        result.Should().Contain("[REDACTED]");
        result.Should().NotContain("abcdef123456");
        result.Should().NotContain("wJalrXUtnFEMI");
    }

    [Fact]
    public void Should_Redact_Sensitive_Keys_In_Dictionary()
    {
        // Arrange
        var data = new Dictionary<string, object>
        {
            ["username"] = "admin",
            ["password"] = "supersecret",
            ["api_key"] = "sk-12345",
            ["token"] = "jwt.token.here",
            ["credential"] = "some-credential",
            ["normal_field"] = "normal value",
        };

        // Act
        var redacted = _redactor.RedactData(data);

        // Assert
        redacted["username"].Should().Be("admin");
        redacted["password"].Should().Be("[REDACTED]");
        redacted["api_key"].Should().Be("[REDACTED]");
        redacted["token"].Should().Be("[REDACTED]");
        redacted["credential"].Should().Be("[REDACTED]");
        redacted["normal_field"].Should().Be("normal value");
    }

    [Fact]
    public void Should_Handle_Nested_Secrets()
    {
        // Arrange
        var input = "config: {\"password\": \"secret\", \"apiKey\": \"abc123\"}";

        // Act
        var result = _redactor.Redact(input);

        // Assert
        result.Should().NotContain("secret");
        result.Should().NotContain("abc123");
    }

    [Fact]
    public void Should_Preserve_NonSensitive_Data()
    {
        // Arrange
        var input = "username=admin, email=user@example.com, role=editor";

        // Act
        var result = _redactor.Redact(input);

        // Assert
        result.Should().Be(input, because: "no sensitive patterns present");
    }

    [Fact]
    public void Should_Handle_Empty_String()
    {
        // Arrange
        var input = string.Empty;

        // Act
        var result = _redactor.Redact(input);

        // Assert
        result.Should().Be(string.Empty);
    }

    [Fact]
    public void Should_Handle_Empty_Dictionary()
    {
        // Arrange
        var data = new Dictionary<string, object>();

        // Act
        var redacted = _redactor.RedactData(data);

        // Assert
        redacted.Should().BeEmpty();
    }

    [Fact]
    public void Should_Redact_Multiple_Secrets_InSingleString()
    {
        // Arrange
        var input = "password=secret1 token=secret2 api_key=secret3";

        // Act
        var result = _redactor.Redact(input);

        // Assert
        result.Should().NotContain("secret1");
        result.Should().NotContain("secret2");
        result.Should().NotContain("secret3");
        result.Should().Contain("[REDACTED]");
    }

    [Fact]
    public void Should_Redact_StringValues_InDictionary()
    {
        // Arrange
        var data = new Dictionary<string, object>
        {
            ["description"] = "Config: password=hunter2",
            ["count"] = 42,
        };

        // Act
        var redacted = _redactor.RedactData(data);

        // Assert
        ((string)redacted["description"]).Should().NotContain("hunter2");
        ((string)redacted["description"]).Should().Contain("[REDACTED]");
        redacted["count"].Should().Be(42);
    }

    [Fact]
    public void Should_Handle_CaseInsensitive_Keys()
    {
        // Arrange
        var data = new Dictionary<string, object>
        {
            ["PASSWORD"] = "secret",
            ["Api_Key"] = "key123",
            ["Token"] = "token456",
        };

        // Act
        var redacted = _redactor.RedactData(data);

        // Assert
        redacted["PASSWORD"].Should().Be("[REDACTED]");
        redacted["Api_Key"].Should().Be("[REDACTED]");
        redacted["Token"].Should().Be("[REDACTED]");
    }
}
