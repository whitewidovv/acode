using Acode.Infrastructure.Vllm.Exceptions;
using FluentAssertions;

namespace Acode.Infrastructure.Tests.Vllm.Exceptions;

public class VllmExceptionTests
{
    [Fact]
    public void Constructor_Should_SetMessage()
    {
        // Arrange & Act
        var exception = new VllmException("ACODE-VLM-001", "Test error message");

        // Assert
        exception.Message.Should().Be("Test error message");
        exception.ErrorCode.Should().Be("ACODE-VLM-001");
    }

    [Fact]
    public void Constructor_Should_SetInnerException()
    {
        // Arrange
        var innerException = new InvalidOperationException("Inner error");

        // Act
        var exception = new VllmException("ACODE-VLM-001", "Test error message", innerException);

        // Assert
        exception.InnerException.Should().BeSameAs(innerException);
    }

    [Fact]
    public void ErrorCode_Should_BeAccessible()
    {
        // Arrange & Act
        var exception = new VllmException("ACODE-VLM-999", "Test");

        // Assert
        exception.ErrorCode.Should().Be("ACODE-VLM-999");
    }

    [Fact]
    public void RequestId_Should_BeSettable()
    {
        // Arrange & Act
        var exception = new VllmException("ACODE-VLM-001", "Test")
        {
            RequestId = "req-12345"
        };

        // Assert
        exception.RequestId.Should().Be("req-12345");
    }

    [Fact]
    public void Timestamp_Should_BeSet()
    {
        // Arrange
        var before = DateTime.UtcNow;

        // Act
        var exception = new VllmException("ACODE-VLM-001", "Test");
        var after = DateTime.UtcNow;

        // Assert
        exception.Timestamp.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
    }
}
