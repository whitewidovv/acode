using System.Text.Json;
using Acode.Infrastructure.Vllm.Models;
using FluentAssertions;

namespace Acode.Infrastructure.Tests.Vllm.Models;

public class VllmRequestTests
{
    [Fact]
    public void Constructor_Should_InitializeProperties()
    {
        // Arrange & Act
        var request = new VllmRequest
        {
            Model = "meta-llama/Llama-3.2-8B-Instruct",
            Messages = new List<VllmMessage>
            {
                new() { Role = "user", Content = "Hello" }
            },
            Temperature = 0.7,
            MaxTokens = 2048,
            Stream = false
        };

        // Assert
        request.Model.Should().Be("meta-llama/Llama-3.2-8B-Instruct");
        request.Messages.Should().HaveCount(1);
        request.Temperature.Should().Be(0.7);
        request.MaxTokens.Should().Be(2048);
        request.Stream.Should().BeFalse();
    }

    [Fact]
    public void Serialization_Should_UseCamelCase()
    {
        // Arrange
        var request = new VllmRequest
        {
            Model = "test-model",
            Messages = new List<VllmMessage>(),
            MaxTokens = 100
        };

        // Act
        var json = JsonSerializer.Serialize(request);

        // Assert
        json.Should().Contain("\"model\":");
        json.Should().Contain("\"max_tokens\":");
        json.Should().NotContain("\"Model\":");
    }

    [Fact]
    public void Tools_Should_BeOptional()
    {
        // Arrange & Act
        var request = new VllmRequest
        {
            Model = "test-model",
            Messages = new List<VllmMessage>()
        };

        // Assert
        request.Tools.Should().BeNull();
    }

    [Fact]
    public void ResponseFormat_Should_BeOptional()
    {
        // Arrange & Act
        var request = new VllmRequest
        {
            Model = "test-model",
            Messages = new List<VllmMessage>()
        };

        // Assert
        request.ResponseFormat.Should().BeNull();
    }
}
