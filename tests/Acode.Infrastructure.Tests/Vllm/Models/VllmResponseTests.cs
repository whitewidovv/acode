using Acode.Infrastructure.Vllm.Models;
using FluentAssertions;
using Xunit;

namespace Acode.Infrastructure.Tests.Vllm.Models;

public class VllmResponseTests
{
    [Fact]
    public void Response_Should_ContainChoices()
    {
        // Arrange & Act
        var response = new VllmResponse
        {
            Id = "chatcmpl-123",
            Model = "test-model",
            Choices = new List<VllmChoice>
            {
                new()
                {
                    Index = 0,
                    Message = new VllmMessage { Role = "assistant", Content = "Hello" },
                    FinishReason = "stop"
                }
            }
        };

        // Assert
        response.Id.Should().Be("chatcmpl-123");
        response.Choices.Should().HaveCount(1);
        response.Choices[0].Message.Content.Should().Be("Hello");
    }

    [Fact]
    public void Usage_Should_BeOptional()
    {
        // Arrange & Act
        var response = new VllmResponse
        {
            Id = "chatcmpl-123",
            Model = "test-model",
            Choices = new List<VllmChoice>()
        };

        // Assert
        response.Usage.Should().BeNull();
    }
}
