using Acode.Infrastructure.Vllm.Exceptions;
using FluentAssertions;
using Xunit;

namespace Acode.Infrastructure.Tests.Vllm.Exceptions;

public class VllmRequestExceptionTests
{
    [Fact]
    public void Constructor_Should_SetErrorCode()
    {
        // Arrange & Act
        var exception = new VllmRequestException("Invalid request");

        // Assert
        exception.ErrorCode.Should().Be("ACODE-VLM-004");
        exception.Message.Should().Be("Invalid request");
    }

    [Fact]
    public void IsTransient_Should_BeFalse()
    {
        // Arrange & Act
        var exception = new VllmRequestException("Invalid request");

        // Assert
        exception.IsTransient.Should().BeFalse();
    }
}
