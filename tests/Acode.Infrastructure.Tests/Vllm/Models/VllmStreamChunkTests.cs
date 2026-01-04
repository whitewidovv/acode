using Acode.Infrastructure.Vllm.Models;
using FluentAssertions;
using Xunit;

namespace Acode.Infrastructure.Tests.Vllm.Models;

public class VllmStreamChunkTests
{
    [Fact]
    public void StreamChunk_Should_ContainDelta()
    {
        // Arrange & Act
        var chunk = new VllmStreamChunk
        {
            Id = "chatcmpl-123",
            Choices = new List<VllmStreamChoice>
            {
                new()
                {
                    Index = 0,
                    Delta = new VllmDelta { Content = "Hello" },
                    FinishReason = null
                }
            }
        };

        // Assert
        chunk.Id.Should().Be("chatcmpl-123");
        chunk.Choices.Should().HaveCount(1);
        chunk.Choices[0].Delta.Content.Should().Be("Hello");
    }

    [Fact]
    public void FinishReason_Should_IndicateCompletion()
    {
        // Arrange & Act
        var chunk = new VllmStreamChunk
        {
            Id = "chatcmpl-123",
            Choices = new List<VllmStreamChoice>
            {
                new()
                {
                    Index = 0,
                    Delta = new VllmDelta { Content = null },
                    FinishReason = "stop"
                }
            }
        };

        // Assert
        chunk.Choices[0].FinishReason.Should().Be("stop");
    }
}
