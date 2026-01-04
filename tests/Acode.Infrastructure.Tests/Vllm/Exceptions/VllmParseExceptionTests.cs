using Acode.Infrastructure.Vllm.Exceptions;
using FluentAssertions;
using Xunit;

namespace Acode.Infrastructure.Tests.Vllm.Exceptions;

public class VllmParseExceptionTests
{
    [Fact]
    public void Constructor_Should_SetErrorCode()
    {
        // Arrange & Act
        var exception = new VllmParseException("Failed to parse response");

        // Assert
        exception.ErrorCode.Should().Be("ACODE-VLM-006");
        exception.Message.Should().Be("Failed to parse response");
    }

    [Fact]
    public void IsTransient_Should_BeFalse()
    {
        // Arrange & Act
        var exception = new VllmParseException("Failed to parse response");

        // Assert
        exception.IsTransient.Should().BeFalse();
    }
}
