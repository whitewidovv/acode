using System.Net.Http.Json;
using System.Text.Json;
using Acode.Infrastructure.Ollama.Models;

namespace Acode.Infrastructure.Ollama.Http;

/// <summary>
/// HTTP client for communicating with the Ollama API.
/// </summary>
/// <remarks>
/// FR-001 to FR-007 from Task 005.a.
/// This class handles all HTTP communication with the Ollama /api/chat endpoint.
/// </remarks>
public sealed class OllamaHttpClient : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly string _baseAddress;
    private readonly bool _ownsHttpClient;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="OllamaHttpClient"/> class.
    /// </summary>
    /// <param name="httpClient">The HTTP client instance.</param>
    /// <param name="baseAddress">The Ollama API base address.</param>
    /// <param name="ownsHttpClient">Whether this instance owns the HttpClient and should dispose it.</param>
    public OllamaHttpClient(HttpClient httpClient, string baseAddress, bool ownsHttpClient = false)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        ArgumentNullException.ThrowIfNull(baseAddress);

        // FR-002: Accept configuration via constructor
        this._httpClient = httpClient;
        this._baseAddress = baseAddress;
        this._ownsHttpClient = ownsHttpClient;

        // FR-004: Configure base address from configuration
        if (this._httpClient.BaseAddress is null)
        {
            this._httpClient.BaseAddress = new Uri(this._baseAddress);
        }

        // FR-007: Expose correlation ID for request tracing
        this.CorrelationId = Guid.NewGuid().ToString();
    }

    /// <summary>
    /// Gets the base address of the Ollama API.
    /// </summary>
    public string BaseAddress => this._baseAddress;

    /// <summary>
    /// Gets the correlation ID for this client instance.
    /// </summary>
    /// <remarks>
    /// FR-007: Exposes correlation ID for request tracing.
    /// </remarks>
    public string CorrelationId { get; }

    /// <summary>
    /// Sends a chat completion request to the Ollama API.
    /// </summary>
    /// <param name="request">The Ollama request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The Ollama response.</returns>
    public async Task<OllamaResponse> PostChatAsync(
        OllamaRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        // FR-008 to FR-014: Request serialization (handled by System.Text.Json)
        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };

        // Send HTTP POST to /api/chat endpoint
        var response = await this._httpClient.PostAsJsonAsync(
            "/api/chat",
            request,
            jsonOptions,
            cancellationToken).ConfigureAwait(false);

        response.EnsureSuccessStatusCode();

        // FR-015 to FR-033: Response parsing (handled by System.Text.Json)
        var ollamaResponse = await response.Content.ReadFromJsonAsync<OllamaResponse>(
            jsonOptions,
            cancellationToken).ConfigureAwait(false);

        if (ollamaResponse is null)
        {
            throw new InvalidOperationException("Failed to deserialize Ollama response.");
        }

        return ollamaResponse;
    }

    /// <summary>
    /// Disposes the HTTP client resources.
    /// </summary>
    /// <remarks>
    /// FR-006: Implements IDisposable for cleanup.
    /// </remarks>
    public void Dispose()
    {
        if (this._disposed)
        {
            return;
        }

        if (this._ownsHttpClient)
        {
            this._httpClient?.Dispose();
        }

        this._disposed = true;
    }
}
