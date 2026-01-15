namespace Acode.Application.Tests.ToolSchemas.Retry;

using Acode.Application.ToolSchemas.Retry;
using FluentAssertions;

/// <summary>
/// Unit tests for ValidationError sealed class.
/// </summary>
/// <remarks>
/// Spec Reference: Implementation Prompt lines 3159-3210.
/// ValidationError is an immutable sealed class representing a validation error.
/// </remarks>
public sealed class ValidationErrorTests
{
    [Fact]
    public void Should_Create_With_All_Fields()
    {
        // Arrange & Act
        var error = new ValidationError
        {
            ErrorCode = ErrorCode.RequiredFieldMissing,
            FieldPath = "/path",
            Message = "Field 'path' is required",
            Severity = ErrorSeverity.Error,
            ExpectedValue = "string",
            ActualValue = "null",
        };

        // Assert
        error.ErrorCode.Should().Be("VAL-001");
        error.FieldPath.Should().Be("/path");
        error.Message.Should().Be("Field 'path' is required");
        error.Severity.Should().Be(ErrorSeverity.Error);
        error.ExpectedValue.Should().Be("string");
        error.ActualValue.Should().Be("null");
    }

    [Fact]
    public void Should_Use_JSON_Pointer_Path_Format()
    {
        // Arrange - JSON Pointer paths per RFC 6901
        var validPaths = new[]
        {
            "/path",
            "/config/timeout",
            "/items/0",
            "/nested/deep/value",
            "/array/0/name",
        };

        // Act & Assert
        foreach (var path in validPaths)
        {
            var error = new ValidationError
            {
                ErrorCode = ErrorCode.RequiredFieldMissing,
                FieldPath = path,
                Message = "Test message",
                Severity = ErrorSeverity.Error,
            };

            // JSON Pointer format validation
            error.FieldPath.Should().StartWith("/", $"Path '{path}' should start with /");
            error.FieldPath.Should().MatchRegex(
                @"^/([a-zA-Z0-9_]+|\d+)(/([a-zA-Z0-9_]+|\d+))*$",
                $"Path '{path}' should follow JSON Pointer format");
        }
    }

    [Theory]
    [InlineData(ErrorSeverity.Info)]
    [InlineData(ErrorSeverity.Warning)]
    [InlineData(ErrorSeverity.Error)]
    public void Should_Support_All_Severity_Levels(ErrorSeverity severity)
    {
        // Arrange & Act
        var error = new ValidationError
        {
            ErrorCode = ErrorCode.TypeMismatch,
            FieldPath = "/field",
            Message = "Type mismatch",
            Severity = severity,
        };

        // Assert
        error.Severity.Should().Be(severity);
    }

    [Theory]
    [InlineData("VAL-001")]
    [InlineData("VAL-002")]
    [InlineData("VAL-003")]
    [InlineData("VAL-010")]
    [InlineData("VAL-015")]
    public void Should_Support_All_Error_Codes(string errorCode)
    {
        // Arrange & Act
        var error = new ValidationError
        {
            ErrorCode = errorCode,
            FieldPath = "/field",
            Message = "Test message",
            Severity = ErrorSeverity.Error,
        };

        // Assert
        error.ErrorCode.Should().Be(errorCode);
        error.ErrorCode.Should().MatchRegex(@"^VAL-\d{3}$");
    }

    [Fact]
    public void Should_Handle_Null_Optional_Fields()
    {
        // Arrange & Act
        var error = new ValidationError
        {
            ErrorCode = ErrorCode.UnknownField,
            FieldPath = "/extra",
            Message = "Unknown field 'extra'",
            Severity = ErrorSeverity.Warning,
            ExpectedValue = null,
            ActualValue = null,
        };

        // Assert
        error.ExpectedValue.Should().BeNull();
        error.ActualValue.Should().BeNull();
    }

    [Fact]
    public void Should_Preserve_Unicode_In_Values()
    {
        // Arrange
        var unicodeMessage = "字段 'name' 类型错误";
        var unicodeValue = "日本語テスト";

        // Act
        var error = new ValidationError
        {
            ErrorCode = ErrorCode.TypeMismatch,
            FieldPath = "/name",
            Message = unicodeMessage,
            Severity = ErrorSeverity.Error,
            ExpectedValue = "string",
            ActualValue = unicodeValue,
        };

        // Assert
        error.Message.Should().Be(unicodeMessage);
        error.ActualValue.Should().Be(unicodeValue);
    }

    [Fact]
    public void Should_Be_Sealed_Class()
    {
        // Assert
        typeof(ValidationError).IsSealed.Should().BeTrue("ValidationError should be sealed");
    }

    [Fact]
    public void Should_Have_Required_Properties_With_Init_Setters()
    {
        // Arrange
        var properties = typeof(ValidationError).GetProperties();

        // Assert - ErrorCode, FieldPath, Message, Severity are required
        var errorCodeProp = properties.First(p => p.Name == "ErrorCode");
        var fieldPathProp = properties.First(p => p.Name == "FieldPath");
        var messageProp = properties.First(p => p.Name == "Message");
        var severityProp = properties.First(p => p.Name == "Severity");

        // All should have getters
        errorCodeProp.CanRead.Should().BeTrue();
        fieldPathProp.CanRead.Should().BeTrue();
        messageProp.CanRead.Should().BeTrue();
        severityProp.CanRead.Should().BeTrue();

        // All should have setters (init)
        errorCodeProp.CanWrite.Should().BeTrue();
        fieldPathProp.CanWrite.Should().BeTrue();
        messageProp.CanWrite.Should().BeTrue();
        severityProp.CanWrite.Should().BeTrue();
    }

    [Fact]
    public void Should_Have_Optional_Properties()
    {
        // Arrange
        var properties = typeof(ValidationError).GetProperties();

        // Assert - ExpectedValue and ActualValue are optional (nullable)
        var expectedValueProp = properties.First(p => p.Name == "ExpectedValue");
        var actualValueProp = properties.First(p => p.Name == "ActualValue");

        expectedValueProp.PropertyType.Should().Be(typeof(string));
        actualValueProp.PropertyType.Should().Be(typeof(string));
    }
}
