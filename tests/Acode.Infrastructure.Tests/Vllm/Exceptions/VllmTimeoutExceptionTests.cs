using Acode.Infrastructure.Vllm.Exceptions;
using FluentAssertions;
using Xunit;

namespace Acode.Infrastructure.Tests.Vllm.Exceptions;

public class VllmTimeoutExceptionTests
{
    [Fact]
    public void Constructor_Should_SetErrorCode()
    {
        // Arrange & Act
        var exception = new VllmTimeoutException("Request timed out");

        // Assert
        exception.ErrorCode.Should().Be("ACODE-VLM-002");
        exception.Message.Should().Be("Request timed out");
    }

    [Fact]
    public void IsTransient_Should_BeTrue()
    {
        // Arrange & Act
        var exception = new VllmTimeoutException("Request timed out");

        // Assert
        exception.IsTransient.Should().BeTrue();
    }
}
