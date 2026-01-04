namespace Acode.Infrastructure.Ollama.Health;

using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Checks health of Ollama server.
/// </summary>
/// <remarks>
/// FR-005-054: OllamaHealthChecker calls /api/tags endpoint.
/// FR-005-055: Returns true if server responds with 200 OK.
/// FR-005-056: Returns false on any error (connection, timeout, non-200 status).
/// FR-005-057: Never throws exceptions.
/// </remarks>
public sealed class OllamaHealthChecker
{
    private readonly HttpClient _httpClient;
    private readonly string _baseAddress;

    /// <summary>
    /// Initializes a new instance of the <see cref="OllamaHealthChecker"/> class.
    /// </summary>
    /// <param name="httpClient">HTTP client for requests.</param>
    /// <param name="baseAddress">Ollama server base address.</param>
    public OllamaHealthChecker(HttpClient httpClient, string baseAddress)
    {
        this._httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        this._baseAddress = baseAddress ?? throw new ArgumentNullException(nameof(baseAddress));
    }

    /// <summary>
    /// Checks if Ollama server is healthy.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if healthy, false otherwise.</returns>
    /// <remarks>
    /// FR-005-054: Calls /api/tags endpoint.
    /// FR-005-055: Returns true only on 200 OK.
    /// FR-005-056: Returns false on any error.
    /// FR-005-057: Never throws exceptions.
    /// </remarks>
    public async Task<bool> CheckHealthAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var requestUri = $"{this._baseAddress}/api/tags";
            var response = await this._httpClient.GetAsync(requestUri, cancellationToken).ConfigureAwait(false);

            return response.IsSuccessStatusCode;
        }
        catch (Exception)
        {
            // FR-005-057: Never throw, always return false on any error
            return false;
        }
    }
}
