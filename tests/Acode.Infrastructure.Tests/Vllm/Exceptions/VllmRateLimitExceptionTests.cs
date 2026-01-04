using Acode.Infrastructure.Vllm.Exceptions;
using FluentAssertions;
using Xunit;

namespace Acode.Infrastructure.Tests.Vllm.Exceptions;

public class VllmRateLimitExceptionTests
{
    [Fact]
    public void Constructor_Should_SetErrorCode()
    {
        // Arrange & Act
        var exception = new VllmRateLimitException("Rate limit exceeded");

        // Assert
        exception.ErrorCode.Should().Be("ACODE-VLM-012");
        exception.Message.Should().Be("Rate limit exceeded");
    }

    [Fact]
    public void IsTransient_Should_BeTrue()
    {
        // Arrange & Act
        var exception = new VllmRateLimitException("Rate limit exceeded");

        // Assert
        exception.IsTransient.Should().BeTrue();
    }

    [Fact]
    public void RetryAfter_Should_BeSettable()
    {
        // Arrange & Act
        var exception = new VllmRateLimitException("Rate limit exceeded")
        {
            RetryAfter = TimeSpan.FromSeconds(30)
        };

        // Assert
        exception.RetryAfter.Should().Be(TimeSpan.FromSeconds(30));
    }
}
