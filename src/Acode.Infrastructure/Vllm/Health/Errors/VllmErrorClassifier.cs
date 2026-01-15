using System.Net;

namespace Acode.Infrastructure.Vllm.Health.Errors;

/// <summary>
/// Classifies vLLM errors as transient or permanent.
/// </summary>
public sealed class VllmErrorClassifier
{
    /// <summary>
    /// Determines if an HTTP status code represents a transient error.
    /// </summary>
    /// <param name="statusCode">The HTTP status code.</param>
    /// <returns>True if transient (should retry), false if permanent.</returns>
    public bool IsTransient(HttpStatusCode statusCode)
    {
        return statusCode switch
        {
            // Transient: should retry
            HttpStatusCode.TooManyRequests => true,        // 429
            HttpStatusCode.InternalServerError => true,    // 500
            HttpStatusCode.BadGateway => true,             // 502
            HttpStatusCode.ServiceUnavailable => true,     // 503
            HttpStatusCode.GatewayTimeout => true,         // 504

            // Permanent: should not retry
            HttpStatusCode.BadRequest => false,            // 400
            HttpStatusCode.Unauthorized => false,          // 401
            HttpStatusCode.Forbidden => false,             // 403
            HttpStatusCode.NotFound => false,              // 404

            // Other errors: permanent by default
            _ => false
        };
    }
}
