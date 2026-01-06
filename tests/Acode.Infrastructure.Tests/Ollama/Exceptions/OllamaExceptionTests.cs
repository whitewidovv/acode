namespace Acode.Infrastructure.Tests.Ollama.Exceptions;

using System;
using Acode.Infrastructure.Ollama.Exceptions;
using FluentAssertions;

/// <summary>
/// Tests for Ollama exception hierarchy.
/// </summary>
/// <remarks>
/// FR-005-026 to FR-005-042: Exception types and error codes.
/// </remarks>
public sealed class OllamaExceptionTests
{
    [Fact]
    public void OllamaException_WithMessage_CreatesInstance()
    {
        // Arrange & Act
        var exception = new OllamaException("Test message");

        // Assert
        exception.Message.Should().Be("Test message");
        exception.ErrorCode.Should().Be("ACODE-OLM-000");
        exception.InnerException.Should().BeNull();
    }

    [Fact]
    public void OllamaException_WithMessageAndInnerException_CreatesInstance()
    {
        // Arrange
        var innerException = new InvalidOperationException("Inner");

        // Act
        var exception = new OllamaException("Test message", innerException);

        // Assert
        exception.Message.Should().Be("Test message");
        exception.ErrorCode.Should().Be("ACODE-OLM-000");
        exception.InnerException.Should().BeSameAs(innerException);
    }

    [Fact]
    public void OllamaException_WithMessageAndErrorCode_CreatesInstance()
    {
        // Arrange & Act
        var exception = new OllamaException("Test message", "CUSTOM-001");

        // Assert
        exception.Message.Should().Be("Test message");
        exception.ErrorCode.Should().Be("CUSTOM-001");
    }

    [Fact]
    public void OllamaConnectionException_HasCorrectErrorCode()
    {
        // Arrange & Act
        var exception = new OllamaConnectionException("Connection failed");

        // Assert
        exception.ErrorCode.Should().Be("ACODE-OLM-001");
        exception.Message.Should().Be("Connection failed");
    }

    [Fact]
    public void OllamaConnectionException_WithInnerException_CreatesInstance()
    {
        // Arrange
        var innerException = new System.Net.Http.HttpRequestException("Network error");

        // Act
        var exception = new OllamaConnectionException("Connection failed", innerException);

        // Assert
        exception.ErrorCode.Should().Be("ACODE-OLM-001");
        exception.InnerException.Should().BeSameAs(innerException);
    }

    [Fact]
    public void OllamaTimeoutException_HasCorrectErrorCode()
    {
        // Arrange & Act
        var exception = new OllamaTimeoutException("Request timed out");

        // Assert
        exception.ErrorCode.Should().Be("ACODE-OLM-002");
        exception.Message.Should().Be("Request timed out");
    }

    [Fact]
    public void OllamaRequestException_HasCorrectErrorCode()
    {
        // Arrange & Act
        var exception = new OllamaRequestException("Invalid request");

        // Assert
        exception.ErrorCode.Should().Be("ACODE-OLM-003");
        exception.Message.Should().Be("Invalid request");
    }

    [Fact]
    public void OllamaServerException_HasCorrectErrorCode()
    {
        // Arrange & Act
        var exception = new OllamaServerException("Server error");

        // Assert
        exception.ErrorCode.Should().Be("ACODE-OLM-004");
        exception.Message.Should().Be("Server error");
    }

    [Fact]
    public void OllamaServerException_WithStatusCode_StoresStatusCode()
    {
        // Arrange & Act
        var exception = new OllamaServerException("Server error", statusCode: 500);

        // Assert
        exception.ErrorCode.Should().Be("ACODE-OLM-004");
        exception.StatusCode.Should().Be(500);
    }

    [Fact]
    public void OllamaParseException_HasCorrectErrorCode()
    {
        // Arrange & Act
        var exception = new OllamaParseException("Parse error");

        // Assert
        exception.ErrorCode.Should().Be("ACODE-OLM-005");
        exception.Message.Should().Be("Parse error");
    }

    [Fact]
    public void OllamaParseException_WithInvalidJson_StoresInvalidJson()
    {
        // Arrange
        var invalidJson = "{broken json}";

        // Act
        var exception = new OllamaParseException("Parse error", invalidJson);

        // Assert
        exception.ErrorCode.Should().Be("ACODE-OLM-005");
        exception.InvalidJson.Should().Be(invalidJson);
    }

    [Fact]
    public void ExceptionHierarchy_AllInheritFromOllamaException()
    {
        // Assert
        typeof(OllamaConnectionException).Should().BeDerivedFrom<OllamaException>();
        typeof(OllamaTimeoutException).Should().BeDerivedFrom<OllamaException>();
        typeof(OllamaRequestException).Should().BeDerivedFrom<OllamaException>();
        typeof(OllamaServerException).Should().BeDerivedFrom<OllamaException>();
        typeof(OllamaParseException).Should().BeDerivedFrom<OllamaException>();
    }

    [Fact]
    public void ExceptionHierarchy_AllInheritFromSystemException()
    {
        // Assert
        typeof(OllamaException).Should().BeDerivedFrom<Exception>();
        typeof(OllamaConnectionException).Should().BeDerivedFrom<Exception>();
        typeof(OllamaTimeoutException).Should().BeDerivedFrom<Exception>();
        typeof(OllamaRequestException).Should().BeDerivedFrom<Exception>();
        typeof(OllamaServerException).Should().BeDerivedFrom<Exception>();
        typeof(OllamaParseException).Should().BeDerivedFrom<Exception>();
    }
}
