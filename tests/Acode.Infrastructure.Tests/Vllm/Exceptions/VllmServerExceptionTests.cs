using Acode.Infrastructure.Vllm.Exceptions;
using FluentAssertions;
using Xunit;

namespace Acode.Infrastructure.Tests.Vllm.Exceptions;

public class VllmServerExceptionTests
{
    [Fact]
    public void Constructor_Should_SetErrorCode()
    {
        // Arrange & Act
        var exception = new VllmServerException("Server error");

        // Assert
        exception.ErrorCode.Should().Be("ACODE-VLM-005");
        exception.Message.Should().Be("Server error");
    }

    [Fact]
    public void IsTransient_Should_BeTrue()
    {
        // Arrange & Act
        var exception = new VllmServerException("Server error");

        // Assert
        exception.IsTransient.Should().BeTrue();
    }
}
