using Acode.Infrastructure.Vllm.Exceptions;
using FluentAssertions;

namespace Acode.Infrastructure.Tests.Vllm.Exceptions;

public class VllmOutOfMemoryExceptionTests
{
    [Fact]
    public void Constructor_Should_SetMessage()
    {
        // Arrange & Act
        var exception = new VllmOutOfMemoryException("CUDA out of memory");

        // Assert
        exception.Message.Should().Be("CUDA out of memory");
    }

    [Fact]
    public void Should_Have_ErrorCode_ACODE_VLM_013()
    {
        // Arrange & Act
        var exception = new VllmOutOfMemoryException("Test error");

        // Assert
        exception.ErrorCode.Should().Be("ACODE-VLM-013");
    }

    [Fact]
    public void Should_Be_Transient()
    {
        // Arrange & Act
        var exception = new VllmOutOfMemoryException("Test error");

        // Assert
        exception.IsTransient.Should().BeTrue();
    }

    [Fact]
    public void Constructor_Should_Accept_InnerException()
    {
        // Arrange
        var innerException = new OutOfMemoryException("System out of memory");

        // Act
        var exception = new VllmOutOfMemoryException("CUDA out of memory", innerException);

        // Assert
        exception.InnerException.Should().BeSameAs(innerException);
        exception.ErrorCode.Should().Be("ACODE-VLM-013");
    }

    [Fact]
    public void Should_Implement_IVllmException()
    {
        // Arrange & Act
        var exception = new VllmOutOfMemoryException("Test error");

        // Assert
        exception.Should().BeAssignableTo<IVllmException>();
    }

    [Fact]
    public void IVllmException_ErrorCode_Should_Be_ACODE_VLM_013()
    {
        // Arrange
        IVllmException exception = new VllmOutOfMemoryException("Test error");

        // Act & Assert
        exception.ErrorCode.Should().Be("ACODE-VLM-013");
    }

    [Fact]
    public void IVllmException_IsTransient_Should_BeTrue()
    {
        // Arrange
        IVllmException exception = new VllmOutOfMemoryException("Test error");

        // Act & Assert
        exception.IsTransient.Should().BeTrue();
    }
}
