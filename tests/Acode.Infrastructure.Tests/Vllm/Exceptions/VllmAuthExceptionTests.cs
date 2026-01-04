using Acode.Infrastructure.Vllm.Exceptions;
using FluentAssertions;
using Xunit;

namespace Acode.Infrastructure.Tests.Vllm.Exceptions;

public class VllmAuthExceptionTests
{
    [Fact]
    public void Constructor_Should_SetErrorCode()
    {
        // Arrange & Act
        var exception = new VllmAuthException("Authentication failed");

        // Assert
        exception.ErrorCode.Should().Be("ACODE-VLM-011");
        exception.Message.Should().Be("Authentication failed");
    }

    [Fact]
    public void IsTransient_Should_BeFalse()
    {
        // Arrange & Act
        var exception = new VllmAuthException("Authentication failed");

        // Assert
        exception.IsTransient.Should().BeFalse();
    }
}
