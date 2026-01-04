using Acode.Infrastructure.Vllm.Exceptions;
using FluentAssertions;
using Xunit;

namespace Acode.Infrastructure.Tests.Vllm.Exceptions;

public class VllmConnectionExceptionTests
{
    [Fact]
    public void Constructor_Should_SetErrorCode()
    {
        // Arrange & Act
        var exception = new VllmConnectionException("Connection failed");

        // Assert
        exception.ErrorCode.Should().Be("ACODE-VLM-001");
        exception.Message.Should().Be("Connection failed");
    }

    [Fact]
    public void IsTransient_Should_BeTrue()
    {
        // Arrange & Act
        var exception = new VllmConnectionException("Connection failed");

        // Assert
        exception.IsTransient.Should().BeTrue();
    }

    [Fact]
    public void Constructor_Should_AcceptInnerException()
    {
        // Arrange
        var innerException = new System.Net.Sockets.SocketException();

        // Act
        var exception = new VllmConnectionException("Connection failed", innerException);

        // Assert
        exception.InnerException.Should().BeSameAs(innerException);
    }
}
