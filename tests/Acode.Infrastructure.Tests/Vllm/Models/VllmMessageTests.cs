using Acode.Infrastructure.Vllm.Models;
using FluentAssertions;
using Xunit;

namespace Acode.Infrastructure.Tests.Vllm.Models;

public class VllmMessageTests
{
    [Fact]
    public void Constructor_Should_SetRoleAndContent()
    {
        // Arrange & Act
        var message = new VllmMessage
        {
            Role = "user",
            Content = "Hello, world!"
        };

        // Assert
        message.Role.Should().Be("user");
        message.Content.Should().Be("Hello, world!");
    }

    [Fact]
    public void ToolCalls_Should_BeOptional()
    {
        // Arrange & Act
        var message = new VllmMessage
        {
            Role = "assistant",
            Content = "Response"
        };

        // Assert
        message.ToolCalls.Should().BeNull();
    }

    [Fact]
    public void ToolCallId_Should_BeOptional()
    {
        // Arrange & Act
        var message = new VllmMessage
        {
            Role = "tool",
            Content = "Result"
        };

        // Assert
        message.ToolCallId.Should().BeNull();
    }
}
