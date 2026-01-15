namespace Acode.Infrastructure.Tests.Vllm.StructuredOutput.Exceptions;

using Acode.Infrastructure.Vllm.StructuredOutput.Exceptions;
using FluentAssertions;
using Xunit;

/// <summary>
/// Tests for StructuredOutputException.
/// </summary>
public class StructuredOutputExceptionTests
{
    [Fact]
    public void Constructor_WithMessageAndErrorCode_CreatesException()
    {
        // Act
        var exception = new StructuredOutputException("Test message", "ACODE-VLM-SO-001");

        // Assert
        exception.Message.Should().Be("Test message");
        exception.ErrorCode.Should().Be("ACODE-VLM-SO-001");
        exception.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Constructor_WithMessageErrorCodeAndInnerException_CreatesException()
    {
        // Arrange
        var innerEx = new InvalidOperationException("Inner error");

        // Act
        var exception = new StructuredOutputException("Test message", "ACODE-VLM-SO-002", innerEx);

        // Assert
        exception.Message.Should().Be("Test message");
        exception.ErrorCode.Should().Be("ACODE-VLM-SO-002");
        exception.InnerException.Should().Be(innerEx);
    }

    [Fact]
    public void IsTransient_ReturnsFalse()
    {
        // Act
        var exception = new StructuredOutputException("Test", "ACODE-VLM-SO-001");

        // Assert
        exception.IsTransient.Should().BeFalse();
    }

    [Fact]
    public void RequestId_CanBeSet()
    {
        // Arrange
        var exception = new StructuredOutputException("Test", "ACODE-VLM-SO-001");

        // Act
        exception.RequestId = "req-123";

        // Assert
        exception.RequestId.Should().Be("req-123");
    }
}
