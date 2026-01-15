namespace Acode.Infrastructure.Vllm.Client.Retry;

/// <summary>
/// Policy for retrying vLLM requests on transient failures.
/// </summary>
/// <remarks>
/// FR-075 to FR-084: VllmRetryPolicy implementation.
/// </remarks>
public interface IVllmRetryPolicy
{
    /// <summary>
    /// Executes an operation with retry logic.
    /// </summary>
    /// <typeparam name="T">Return type of operation.</typeparam>
    /// <param name="operation">The operation to execute.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result of operation.</returns>
    /// <remarks>
    /// FR-076: Retries on SocketException.
    /// FR-077: Retries on HttpRequestException.
    /// FR-078: Retries on 503 Service Unavailable.
    /// FR-079: Retries on 429 Rate Limit.
    /// FR-080: Does NOT retry on 400 Bad Request.
    /// FR-081: Applies exponential backoff between retries.
    /// FR-082: Does NOT retry on 401/403/404 errors.
    /// FR-083: Respects cancellation token.
    /// FR-084: Throws after max retries exceeded.
    /// </remarks>
    Task<T> ExecuteAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        CancellationToken cancellationToken);
}
