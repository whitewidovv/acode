namespace Acode.Infrastructure.Tests.ToolSchemas.Retry;

using Acode.Infrastructure.ToolSchemas.Retry;
using FluentAssertions;

/// <summary>
/// Unit tests for ValueSanitizer class.
/// </summary>
/// <remarks>
/// Spec Reference: Implementation Prompt lines 3678-3806.
/// Tests secret redaction, value truncation, and path relativization.
/// </remarks>
public sealed class ValueSanitizerTests
{
    private readonly ValueSanitizer sanitizer;

    public ValueSanitizerTests()
    {
        this.sanitizer = new ValueSanitizer(maxPreviewLength: 100, redactSecrets: true, relativizePaths: true);
    }

    [Fact]
    public void Should_Redact_JWT_Tokens()
    {
        // Arrange
        var jwt = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIn0.dozjgNryP4J3jVmNHl0w5N_XgL0n3I9PlFUP0THsR8U";

        // Act - use a non-sensitive field name to test pattern matching
        var result = this.sanitizer.Sanitize(jwt, "/data");

        // Assert
        result.Should().Be("[REDACTED: JWT]");
    }

    [Fact]
    public void Should_Redact_OpenAI_API_Keys()
    {
        // Arrange
        var apiKey = "sk-abcdefghijklmnopqrstuvwxyz123456";

        // Act - use a non-sensitive field name to test pattern matching
        var result = this.sanitizer.Sanitize(apiKey, "/value");

        // Assert
        result.Should().Be("[REDACTED: API_KEY]");
    }

    [Fact]
    public void Should_Redact_Password_Fields_By_Name()
    {
        // Arrange
        var password = "mysecretpassword123";

        // Act
        var result = this.sanitizer.Sanitize(password, "/password");

        // Assert
        result.Should().Be("[REDACTED: SENSITIVE_FIELD]");
    }

    [Fact]
    public void Should_Redact_AWS_Access_Keys()
    {
        // Arrange
        var awsKey = "AKIAIOSFODNN7EXAMPLE";

        // Act - use a non-sensitive field name to test pattern matching
        var result = this.sanitizer.Sanitize(awsKey, "/key_value");

        // Assert
        result.Should().Be("[REDACTED: AWS_KEY]");
    }

    [Fact]
    public void Should_Truncate_Long_Strings()
    {
        // Arrange
        var longString = new string('x', 200);

        // Act
        var result = this.sanitizer.Sanitize(longString, "/data");

        // Assert
        result.Should().HaveLength(100); // maxPreviewLength
        result.Should().Contain("...");
    }

    [Fact]
    public void Should_Relativize_Absolute_File_Paths()
    {
        // Arrange
        var absolutePath = "/home/user/project/src/file.cs";

        // Act
        var result = this.sanitizer.Sanitize(absolutePath, "/path");

        // Assert
        result.Should().NotStartWith("/home/user");
        result.Should().Contain("file.cs");
    }

    [Fact]
    public void Should_Preserve_Unicode()
    {
        // Arrange
        var unicodeValue = "日本語テスト";

        // Act
        var result = this.sanitizer.Sanitize(unicodeValue, "/message");

        // Assert
        result.Should().Be(unicodeValue);
    }

    [Fact]
    public void Should_Handle_Null_Values()
    {
        // Act
        var result = this.sanitizer.Sanitize(null, "/field");

        // Assert
        result.Should().Be("null");
    }

    [Fact]
    public void Should_Use_Smart_Truncation_Strategy()
    {
        // Arrange - value should show prefix...suffix
        var value = "start_of_string_middle_content_end_of_string";
        var shortSanitizer = new ValueSanitizer(maxPreviewLength: 30, redactSecrets: false, relativizePaths: false);

        // Act
        var result = shortSanitizer.Sanitize(value, "/data");

        // Assert
        result.Should().Contain("...");
        result.Should().StartWith("start");
        result.Should().EndWith("string");
    }

    [Theory]
    [InlineData("/password")]
    [InlineData("/passwd")]
    [InlineData("/pass")]
    [InlineData("/pwd")]
    [InlineData("/secret")]
    [InlineData("/credentials")]
    [InlineData("/api_key")]
    [InlineData("/apiKey")]
    [InlineData("/apikey")]
    [InlineData("/access_key")]
    [InlineData("/accessKey")]
    [InlineData("/token")]
    [InlineData("/auth_token")]
    [InlineData("/authToken")]
    [InlineData("/bearer")]
    [InlineData("/jwt")]
    public void Should_Redact_Sensitive_Field_Names(string fieldPath)
    {
        // Arrange
        var value = "sensitive_value_123";

        // Act
        var result = this.sanitizer.Sanitize(value, fieldPath);

        // Assert
        result.Should().StartWith("[REDACTED:");
    }

    [Fact]
    public void Should_Handle_Empty_Strings()
    {
        // Act
        var result = this.sanitizer.Sanitize(string.Empty, "/field");

        // Assert
        result.Should().Be(string.Empty);
    }

    [Fact]
    public void Should_Not_Redact_When_Disabled()
    {
        // Arrange
        var noRedactSanitizer = new ValueSanitizer(maxPreviewLength: 100, redactSecrets: false, relativizePaths: false);
        var apiKey = "sk-abcdefghijklmnopqrstuvwxyz123456";

        // Act
        var result = noRedactSanitizer.Sanitize(apiKey, "/api_key");

        // Assert
        result.Should().Be(apiKey);
    }

    [Fact]
    public void Should_Handle_Windows_Paths()
    {
        // Arrange
        var windowsPath = @"C:\Users\user\project\src\file.cs";

        // Act
        var result = this.sanitizer.Sanitize(windowsPath, "/path");

        // Assert
        result.Should().Contain("file.cs");
    }
}
