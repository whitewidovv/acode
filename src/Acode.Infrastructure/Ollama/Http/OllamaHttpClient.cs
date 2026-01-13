using System.Diagnostics;
using System.Net.Http.Json;
using System.Text.Json;
using Acode.Infrastructure.Ollama.Models;
using Microsoft.Extensions.Logging;

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
    private readonly ILogger<OllamaHttpClient>? _logger;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="OllamaHttpClient"/> class.
    /// </summary>
    /// <param name="httpClient">The HTTP client instance.</param>
    /// <param name="baseAddress">The Ollama API base address.</param>
    /// <param name="ownsHttpClient">Whether this instance owns the HttpClient and should dispose it.</param>
    /// <param name="logger">Optional logger for observability.</param>
    public OllamaHttpClient(
        HttpClient httpClient,
        string baseAddress,
        bool ownsHttpClient = false,
        ILogger<OllamaHttpClient>? logger = null)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        ArgumentNullException.ThrowIfNull(baseAddress);

        // FR-002: Accept configuration via constructor
        this._httpClient = httpClient;
        this._baseAddress = baseAddress;
        this._ownsHttpClient = ownsHttpClient;
        this._logger = logger;

        // FR-004: Configure base address from configuration
        if (this._httpClient.BaseAddress is null)
        {
            this._httpClient.BaseAddress = new Uri(this._baseAddress);
        }

        // FR-007: Expose correlation ID for request tracing
        this.CorrelationId = Guid.NewGuid().ToString();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="OllamaHttpClient"/> class using IHttpClientFactory.
    /// </summary>
    /// <param name="httpClientFactory">The HTTP client factory.</param>
    /// <param name="configuration">The Ollama configuration.</param>
    /// <param name="logger">Optional logger for observability.</param>
    /// <remarks>
    /// FR-003: OllamaHttpClient MUST use IHttpClientFactory for HttpClient creation.
    /// FR-005: Configure timeout from configuration.
    /// FR-040: PostAsync MUST log request and response timing.
    /// </remarks>
    public OllamaHttpClient(
        IHttpClientFactory httpClientFactory,
        OllamaConfiguration configuration,
        ILogger<OllamaHttpClient>? logger = null)
    {
        ArgumentNullException.ThrowIfNull(httpClientFactory);
        ArgumentNullException.ThrowIfNull(configuration);

        // FR-003: Create HttpClient from factory
        this._httpClient = httpClientFactory.CreateClient("Ollama");

        // FR-004: Configure base address from configuration
        this._baseAddress = configuration.BaseUrl;
        if (this._httpClient.BaseAddress is null)
        {
            this._httpClient.BaseAddress = new Uri(this._baseAddress);
        }

        // FR-005: Configure timeout from configuration
        this._httpClient.Timeout = configuration.RequestTimeout;

        // FR-040: Store logger for observability
        this._logger = logger;

        // FR-007: Expose correlation ID for request tracing
        this.CorrelationId = Guid.NewGuid().ToString();

        // Factory-created HttpClient should be disposed
        this._ownsHttpClient = true;
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
    /// Sends a generic HTTP POST request to the specified endpoint.
    /// </summary>
    /// <typeparam name="TResponse">The expected response type.</typeparam>
    /// <param name="endpoint">The API endpoint (e.g., "/api/chat", "/api/tags").</param>
    /// <param name="request">The request object to serialize.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The deserialized response.</returns>
    /// <remarks>
    /// Gap #5: Generic PostAsync method supporting any endpoint and type.
    /// Includes logging with correlation ID and request timing.
    /// </remarks>
    public async Task<TResponse> PostAsync<TResponse>(
        string endpoint,
        object request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(endpoint);
        ArgumentNullException.ThrowIfNull(request);

        // FR-040 + NFR-019-022: Begin logging scope with correlation ID
        using var scope = this._logger?.BeginScope(new { CorrelationId = this.CorrelationId });

        // FR-040: Start timing for observability
        var stopwatch = Stopwatch.StartNew();

        // Request serialization with camelCase naming
        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };

        // Send HTTP POST to specified endpoint
        var response = await this._httpClient.PostAsJsonAsync(
            endpoint,
            request,
            jsonOptions,
            cancellationToken).ConfigureAwait(false);

        // FR-040: Log request timing and status
        this._logger?.LogDebug(
            "POST {Endpoint} completed in {ElapsedMs}ms with status {StatusCode}",
            endpoint,
            stopwatch.ElapsedMilliseconds,
            (int)response.StatusCode);

        response.EnsureSuccessStatusCode();

        // Response deserialization
        var result = await response.Content.ReadFromJsonAsync<TResponse>(
            jsonOptions,
            cancellationToken).ConfigureAwait(false);

        if (result is null)
        {
            throw new InvalidOperationException($"Failed to deserialize response from {endpoint}.");
        }

        return result;
    }

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

        // FR-040 + NFR-019-022: Begin logging scope with correlation ID
        using var scope = this._logger?.BeginScope(new { CorrelationId = this.CorrelationId });

        // FR-040: Start timing for observability
        var stopwatch = Stopwatch.StartNew();

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

        // FR-040: Log request timing and status
        this._logger?.LogDebug(
            "POST /api/chat completed in {ElapsedMs}ms with status {StatusCode}",
            stopwatch.ElapsedMilliseconds,
            (int)response.StatusCode);

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
