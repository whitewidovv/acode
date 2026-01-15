using System.Net;
using System.Net.Sockets;
using Acode.Infrastructure.Vllm.Exceptions;
using Acode.Infrastructure.Vllm.Health.Errors;
using FluentAssertions;

namespace Acode.Infrastructure.Tests.Vllm.Health.Errors;

public class VllmExceptionMapperTests
{
    [Fact]
    public void Should_Map_401_To_VllmAuthException()
    {
        // Arrange
        var mapper = new VllmExceptionMapper();
        var errorInfo = new VllmErrorInfo { Message = "Unauthorized" };

        // Act
        var exception = mapper.MapException(HttpStatusCode.Unauthorized, errorInfo, "req-123");

        // Assert
        exception.Should().BeOfType<VllmAuthException>();
        exception.ErrorCode.Should().Be("ACODE-VLM-011");
        exception.RequestId.Should().Be("req-123");
        exception.Message.Should().Be("Unauthorized");
    }

    [Fact]
    public void Should_Map_404_To_VllmModelNotFoundException()
    {
        // Arrange
        var mapper = new VllmExceptionMapper();
        var errorInfo = new VllmErrorInfo { Message = "Model not found" };

        // Act
        var exception = mapper.MapException(HttpStatusCode.NotFound, errorInfo);

        // Assert
        exception.Should().BeOfType<VllmModelNotFoundException>();
        exception.ErrorCode.Should().Be("ACODE-VLM-003");
    }

    [Fact]
    public void Should_Map_429_To_VllmRateLimitException()
    {
        // Arrange
        var mapper = new VllmExceptionMapper();
        var errorInfo = new VllmErrorInfo { Message = "Too many requests" };

        // Act
        var exception = mapper.MapException(HttpStatusCode.TooManyRequests, errorInfo);

        // Assert
        exception.Should().BeOfType<VllmRateLimitException>();
        exception.ErrorCode.Should().Be("ACODE-VLM-012");
    }

    [Fact]
    public void Should_Map_400_With_ModelNotFound_Code()
    {
        // Arrange
        var mapper = new VllmExceptionMapper();
        var errorInfo = new VllmErrorInfo
        {
            Message = "Model not found",
            Code = "model_not_found"
        };

        // Act
        var exception = mapper.MapException(HttpStatusCode.BadRequest, errorInfo);

        // Assert
        exception.Should().BeOfType<VllmModelNotFoundException>();
        exception.ErrorCode.Should().Be("ACODE-VLM-003");
    }

    [Fact]
    public void Should_Map_400_To_VllmRequestException()
    {
        // Arrange
        var mapper = new VllmExceptionMapper();
        var errorInfo = new VllmErrorInfo { Message = "Bad request" };

        // Act
        var exception = mapper.MapException(HttpStatusCode.BadRequest, errorInfo);

        // Assert
        exception.Should().BeOfType<VllmRequestException>();
        exception.ErrorCode.Should().Be("ACODE-VLM-004");
    }

    [Fact]
    public void Should_Map_500_To_VllmServerException()
    {
        // Arrange
        var mapper = new VllmExceptionMapper();
        var errorInfo = new VllmErrorInfo { Message = "Server error" };

        // Act
        var exception = mapper.MapException(HttpStatusCode.InternalServerError, errorInfo);

        // Assert
        exception.Should().BeOfType<VllmServerException>();
        exception.ErrorCode.Should().Be("ACODE-VLM-005");
    }

    [Fact]
    public void Should_Map_502_To_VllmServerException()
    {
        // Arrange
        var mapper = new VllmExceptionMapper();
        var errorInfo = new VllmErrorInfo { Message = "Bad gateway" };

        // Act
        var exception = mapper.MapException(HttpStatusCode.BadGateway, errorInfo);

        // Assert
        exception.Should().BeOfType<VllmServerException>();
    }

    [Fact]
    public void Should_Map_TimeoutException_To_VllmTimeoutException()
    {
        // Arrange
        var mapper = new VllmExceptionMapper();
        var originalException = new TimeoutException("Request timed out");

        // Act
        var exception = mapper.MapException(originalException, "req-123");

        // Assert
        exception.Should().BeOfType<VllmTimeoutException>();
        exception.ErrorCode.Should().Be("ACODE-VLM-002");
        exception.RequestId.Should().Be("req-123");
        exception.InnerException.Should().BeSameAs(originalException);
    }

    [Fact]
    public void Should_Map_HttpRequestException_To_VllmConnectionException()
    {
        // Arrange
        var mapper = new VllmExceptionMapper();
        var originalException = new HttpRequestException("Connection failed");

        // Act
        var exception = mapper.MapException(originalException);

        // Assert
        exception.Should().BeOfType<VllmConnectionException>();
        exception.ErrorCode.Should().Be("ACODE-VLM-001");
        exception.InnerException.Should().BeSameAs(originalException);
    }

    [Fact]
    public void Should_Map_SocketException_To_VllmConnectionException()
    {
        // Arrange
        var mapper = new VllmExceptionMapper();
        var originalException = new SocketException((int)SocketError.ConnectionRefused);

        // Act
        var exception = mapper.MapException(originalException);

        // Assert
        exception.Should().BeOfType<VllmConnectionException>();
        exception.ErrorCode.Should().Be("ACODE-VLM-001");
    }

    [Fact]
    public void Should_Map_CUDA_OOM_Message_To_VllmOutOfMemoryException()
    {
        // Arrange
        var mapper = new VllmExceptionMapper();
        var originalException = new Exception("CUDA out of memory");

        // Act
        var exception = mapper.MapException(originalException);

        // Assert
        exception.Should().BeOfType<VllmOutOfMemoryException>();
        exception.ErrorCode.Should().Be("ACODE-VLM-013");
    }

    [Fact]
    public void Should_Set_RequestId_On_Mapped_HttpStatus_Exception()
    {
        // Arrange
        var mapper = new VllmExceptionMapper();
        var errorInfo = new VllmErrorInfo { Message = "Test" };

        // Act
        var exception = mapper.MapException(HttpStatusCode.BadRequest, errorInfo, "req-456");

        // Assert
        exception.RequestId.Should().Be("req-456");
    }

    [Fact]
    public void Should_Default_Unknown_Exception_To_VllmServerException()
    {
        // Arrange
        var mapper = new VllmExceptionMapper();
        var originalException = new InvalidOperationException("Unknown error");

        // Act
        var exception = mapper.MapException(originalException);

        // Assert
        exception.Should().BeOfType<VllmServerException>();
    }

    [Fact]
    public void Should_Default_Unknown_Status_Code_To_VllmRequestException()
    {
        // Arrange
        var mapper = new VllmExceptionMapper();
        var errorInfo = new VllmErrorInfo { Message = "Unknown status" };

        // Act
        var exception = mapper.MapException(HttpStatusCode.MethodNotAllowed, errorInfo);

        // Assert
        exception.Should().BeOfType<VllmRequestException>();
    }
}
