using Acode.Infrastructure.Vllm.Exceptions;
using FluentAssertions;

namespace Acode.Infrastructure.Tests.Vllm.Exceptions;

public class VllmModelNotFoundExceptionTests
{
    [Fact]
    public void Constructor_Should_SetErrorCode()
    {
        // Arrange & Act
        var exception = new VllmModelNotFoundException("Model not found");

        // Assert
        exception.ErrorCode.Should().Be("ACODE-VLM-003");
        exception.Message.Should().Be("Model not found");
    }

    [Fact]
    public void IsTransient_Should_BeFalse()
    {
        // Arrange & Act
        var exception = new VllmModelNotFoundException("Model not found");

        // Assert
        exception.IsTransient.Should().BeFalse();
    }

    [Fact]
    public void ModelId_Should_BeSettable()
    {
        // Arrange & Act
        var exception = new VllmModelNotFoundException("Model not found")
        {
            ModelId = "meta-llama/Llama-3.2-8B-Instruct"
        };

        // Assert
        exception.ModelId.Should().Be("meta-llama/Llama-3.2-8B-Instruct");
    }
}
