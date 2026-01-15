using System.Net;
using System.Net.Sockets;
using Acode.Infrastructure.Vllm.Exceptions;

namespace Acode.Infrastructure.Vllm.Health.Errors;

/// <summary>
/// Maps vLLM errors to exception types.
/// </summary>
public sealed class VllmExceptionMapper
{
    /// <summary>
    /// Maps an HTTP status code and error info to an appropriate exception.
    /// </summary>
    /// <param name="statusCode">The HTTP status code.</param>
    /// <param name="errorInfo">Parsed error information.</param>
    /// <param name="requestId">The request ID (optional).</param>
    /// <returns>The appropriate exception.</returns>
    public VllmException MapException(
        HttpStatusCode statusCode,
        VllmErrorInfo errorInfo,
        string? requestId = null)
    {
        ArgumentNullException.ThrowIfNull(errorInfo);

        VllmException exception = statusCode switch
        {
            HttpStatusCode.Unauthorized => new VllmAuthException(errorInfo.Message),
            HttpStatusCode.TooManyRequests => new VllmRateLimitException(errorInfo.Message),

            HttpStatusCode.BadRequest when errorInfo.Code == "model_not_found" =>
                new VllmModelNotFoundException(errorInfo.Message),

            HttpStatusCode.NotFound =>
                new VllmModelNotFoundException(errorInfo.Message),

            HttpStatusCode.BadRequest =>
                new VllmRequestException(errorInfo.Message),

            _ when (int)statusCode >= 500 && (int)statusCode < 600 =>
                new VllmServerException(errorInfo.Message),

            _ => new VllmRequestException(errorInfo.Message)
        };

        exception.RequestId = requestId;
        return exception;
    }

    /// <summary>
    /// Maps an exception to a vLLM exception.
    /// </summary>
    /// <param name="exception">The original exception.</param>
    /// <param name="requestId">The request ID (optional).</param>
    /// <returns>The appropriate vLLM exception.</returns>
    public VllmException MapException(Exception exception, string? requestId = null)
    {
        ArgumentNullException.ThrowIfNull(exception);

        VllmException vllmException = exception switch
        {
            TimeoutException => new VllmTimeoutException(exception.Message, exception),
            HttpRequestException => new VllmConnectionException(exception.Message, exception),
            SocketException => new VllmConnectionException(exception.Message, exception),

            _ when exception.Message.Contains("CUDA out of memory", StringComparison.OrdinalIgnoreCase) =>
                new VllmOutOfMemoryException(exception.Message, exception),

            _ => new VllmServerException(exception.Message, exception)
        };

        vllmException.RequestId = requestId;
        return vllmException;
    }
}
