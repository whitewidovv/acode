using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using Acode.Infrastructure.Common;
using Acode.Infrastructure.Vllm.Exceptions;
using Acode.Infrastructure.Vllm.Models;
using Acode.Infrastructure.Vllm.Serialization;
using Microsoft.Extensions.Logging;

namespace Acode.Infrastructure.Vllm.Client;

/// <summary>
/// HTTP client for vLLM /v1/chat/completions endpoint with SSE streaming support.
/// </summary>
public sealed class VllmHttpClient : IAsyncDisposable
{
    private readonly VllmClientConfiguration _config;
    private readonly HttpClient _httpClient;
    private readonly ILogger<VllmHttpClient> _logger;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="VllmHttpClient"/> class.
    /// </summary>
    /// <param name="config">Client configuration.</param>
    /// <param name="logger">Logger for diagnostic output.</param>
    public VllmHttpClient(VllmClientConfiguration config, ILogger<VllmHttpClient> logger)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _config.Validate();

        var handler = new SocketsHttpHandler
        {
            MaxConnectionsPerServer = _config.MaxConnections,
            PooledConnectionIdleTimeout = TimeSpan.FromSeconds(_config.IdleTimeoutSeconds),
            PooledConnectionLifetime = TimeSpan.FromSeconds(_config.ConnectionLifetimeSeconds),
            ConnectTimeout = TimeSpan.FromSeconds(_config.ConnectTimeoutSeconds),

            // FR-014: TCP keep-alive is enabled by default in SocketsHttpHandler
            UseProxy = false
        };

        _httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri(_config.Endpoint),
            Timeout = TimeSpan.FromSeconds(_config.RequestTimeoutSeconds)
        };

        if (!string.IsNullOrEmpty(_config.ApiKey))
        {
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", _config.ApiKey);
        }

        _httpClient.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/json"));

        // FR-015: Explicitly disable Expect: 100-continue header for HTTP/1.1
        _httpClient.DefaultRequestHeaders.ExpectContinue = false;
    }

    /// <summary>
    /// Sends a generic POST request to vLLM.
    /// </summary>
    /// <typeparam name="TResponse">The response type to deserialize to.</typeparam>
    /// <param name="path">The endpoint path (e.g., "/v1/chat/completions").</param>
    /// <param name="request">The request payload.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The deserialized response.</returns>
    /// <exception cref="VllmConnectionException">Connection failed.</exception>
    /// <exception cref="VllmTimeoutException">Request timed out.</exception>
    /// <exception cref="VllmRequestException">Invalid request (4xx).</exception>
    /// <exception cref="VllmServerException">Server error (5xx).</exception>
    public async Task<TResponse> PostAsync<TResponse>(
        string path,
        object request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(path);
        ArgumentNullException.ThrowIfNull(request);

        try
        {
            // FR-027: Generate correlation ID for request tracing
            var correlationId = Guid.NewGuid().ToString();

            var requestOptions = new System.Text.Json.JsonSerializerOptions
            {
                PropertyNamingPolicy = new SnakeCaseNamingPolicy(),
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            };
            var json = System.Text.Json.JsonSerializer.Serialize(request, requestOptions);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

            // FR-027: Create request message with correlation ID header
            var requestMessage = new HttpRequestMessage(HttpMethod.Post, path)
            {
                Content = content
            };
            requestMessage.Headers.Add("X-Request-ID", correlationId);

            var response = await _httpClient.SendAsync(
                requestMessage,
                cancellationToken).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken)
                    .ConfigureAwait(false);
                ThrowForStatusCode(response.StatusCode, errorContent);
            }

            var responseJson = await response.Content.ReadAsStringAsync(cancellationToken)
                .ConfigureAwait(false);

            var responseOptions = new System.Text.Json.JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            return System.Text.Json.JsonSerializer.Deserialize<TResponse>(responseJson, responseOptions)
                ?? throw new VllmParseException("Failed to deserialize response");
        }
        catch (HttpRequestException ex)
        {
            throw new VllmConnectionException(
                $"Failed to connect to vLLM at {_config.Endpoint}: {ex.Message}",
                ex);
        }
        catch (TaskCanceledException ex) when (ex.CancellationToken == cancellationToken)
        {
            throw;
        }
        catch (TaskCanceledException ex) when (IsConnectionTimeout(ex))
        {
            throw new VllmConnectionException(
                $"Failed to connect to vLLM at {_config.Endpoint}: connection timed out",
                ex);
        }
        catch (TaskCanceledException ex)
        {
            throw new VllmTimeoutException(
                $"Request to vLLM timed out after {_config.RequestTimeoutSeconds}s",
                ex);
        }
    }

    /// <summary>
    /// Sends a non-streaming request to vLLM.
    /// </summary>
    /// <param name="request">The request payload.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The response.</returns>
    /// <exception cref="VllmConnectionException">Connection failed.</exception>
    /// <exception cref="VllmTimeoutException">Request timed out.</exception>
    /// <exception cref="VllmRequestException">Invalid request (4xx).</exception>
    /// <exception cref="VllmServerException">Server error (5xx).</exception>
    public async Task<VllmResponse> SendRequestAsync(
        VllmRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            // FR-027: Generate correlation ID for request tracing
            var correlationId = Guid.NewGuid().ToString();

            var json = VllmRequestSerializer.Serialize(request);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

            var requestMessage = new HttpRequestMessage(HttpMethod.Post, "/v1/chat/completions")
            {
                Content = content
            };

            // FR-027: Add correlation ID header for request tracing
            requestMessage.Headers.Add("X-Request-ID", correlationId);

            var response = await _httpClient.SendAsync(
                requestMessage,
                cancellationToken).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken)
                    .ConfigureAwait(false);
                ThrowForStatusCode(response.StatusCode, errorContent);
            }

            var responseJson = await response.Content.ReadAsStringAsync(cancellationToken)
                .ConfigureAwait(false);
            return VllmRequestSerializer.DeserializeResponse(responseJson);
        }
        catch (HttpRequestException ex)
        {
            throw new VllmConnectionException(
                $"Failed to connect to vLLM at {_config.Endpoint}: {ex.Message}",
                ex);
        }
        catch (TaskCanceledException ex) when (ex.CancellationToken == cancellationToken)
        {
            throw;
        }
        catch (TaskCanceledException ex) when (IsConnectionTimeout(ex))
        {
            // Connection timeout should be treated as a connection error, not a request timeout
            throw new VllmConnectionException(
                $"Failed to connect to vLLM at {_config.Endpoint}: connection timed out",
                ex);
        }
        catch (TaskCanceledException ex)
        {
            throw new VllmTimeoutException(
                $"Request to vLLM timed out after {_config.RequestTimeoutSeconds}s",
                ex);
        }
    }

    /// <summary>
    /// Streams a generic request to vLLM using Server-Sent Events (SSE).
    /// </summary>
    /// <param name="path">The endpoint path (e.g., "/v1/chat/completions").</param>
    /// <param name="request">The request payload (can be any object).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Async enumerable of stream chunks.</returns>
    /// <exception cref="VllmConnectionException">Connection failed.</exception>
    /// <exception cref="VllmTimeoutException">Request timed out.</exception>
    public async IAsyncEnumerable<VllmStreamChunk> PostStreamingAsync(
        string path,
        object request,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(path);
        ArgumentNullException.ThrowIfNull(request);

        // FR-043: If request is VllmRequest, automatically set Stream flag
        if (request is VllmRequest vllmRequest)
        {
            vllmRequest.Stream = true;
        }

        // FR-027: Generate correlation ID for request tracing
        var correlationId = Guid.NewGuid().ToString();

        var requestOptions = new System.Text.Json.JsonSerializerOptions
        {
            PropertyNamingPolicy = new SnakeCaseNamingPolicy(),
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };
        var json = System.Text.Json.JsonSerializer.Serialize(request, requestOptions);
        var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

        var requestMessage = new HttpRequestMessage(HttpMethod.Post, path)
        {
            Content = content
        };

        // FR-027: Add correlation ID header for request tracing
        requestMessage.Headers.Add("X-Request-ID", correlationId);

        HttpResponseMessage? response = null;
        Stream? stream = null;
        StreamReader? reader = null;

        try
        {
            response = await _httpClient.SendAsync(
                requestMessage,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken).ConfigureAwait(false);
        }
        catch (HttpRequestException ex)
        {
            throw new VllmConnectionException(
                $"Failed to connect to vLLM at {_config.Endpoint}: {ex.Message}",
                ex);
        }
        catch (TaskCanceledException ex) when (ex.CancellationToken == cancellationToken)
        {
            throw;
        }
        catch (TaskCanceledException ex)
        {
            throw new VllmTimeoutException(
                $"Streaming request to vLLM timed out after {_config.StreamingReadTimeoutSeconds}s",
                ex);
        }

        try
        {
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken)
                    .ConfigureAwait(false);
                ThrowForStatusCode(response.StatusCode, errorContent);
            }

            stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            reader = new StreamReader(stream, System.Text.Encoding.UTF8);

            string? line;
            while ((line = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false)) != null)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }

                // FR-051: Parse SSE format (lines starting with "data: ")
                if (line.StartsWith("data: ", StringComparison.Ordinal))
                {
                    var data = line.Substring(6);

                    // FR-052: Handle [DONE] marker to end stream
                    if (data == "[DONE]")
                    {
                        break;
                    }

                    var chunk = VllmRequestSerializer.DeserializeStreamChunk(data);
                    yield return chunk;
                }
            }
        }
        finally
        {
            reader?.Dispose();
            stream?.Dispose();
            response?.Dispose();
        }
    }

    /// <summary>
    /// Streams a request to vLLM using Server-Sent Events (SSE).
    /// </summary>
    /// <param name="request">The request payload.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Async enumerable of stream chunks.</returns>
    /// <exception cref="VllmConnectionException">Connection failed.</exception>
    /// <exception cref="VllmTimeoutException">Request timed out.</exception>
    public async IAsyncEnumerable<VllmStreamChunk> StreamRequestAsync(
        VllmRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        request.Stream = true;

        // FR-027: Generate correlation ID for request tracing
        var correlationId = Guid.NewGuid().ToString();

        var json = VllmRequestSerializer.Serialize(request);
        var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

        var requestMessage = new HttpRequestMessage(HttpMethod.Post, "/v1/chat/completions")
        {
            Content = content
        };

        // FR-027: Add correlation ID header for request tracing
        requestMessage.Headers.Add("X-Request-ID", correlationId);

        HttpResponseMessage? response = null;
        Stream? stream = null;
        StreamReader? reader = null;

        try
        {
            response = await _httpClient.SendAsync(
                requestMessage,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken).ConfigureAwait(false);
        }
        catch (HttpRequestException ex)
        {
            throw new VllmConnectionException(
                $"Failed to connect to vLLM at {_config.Endpoint}: {ex.Message}",
                ex);
        }
        catch (TaskCanceledException ex) when (ex.CancellationToken == cancellationToken)
        {
            throw;
        }
        catch (TaskCanceledException ex)
        {
            throw new VllmTimeoutException(
                $"Streaming request to vLLM timed out after {_config.StreamingReadTimeoutSeconds}s",
                ex);
        }

        try
        {
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken)
                    .ConfigureAwait(false);
                ThrowForStatusCode(response.StatusCode, errorContent);
            }

            stream = await response.Content.ReadAsStreamAsync(cancellationToken)
                .ConfigureAwait(false);
            reader = new StreamReader(stream);

            while (!reader.EndOfStream && !cancellationToken.IsCancellationRequested)
            {
                var line = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false);

                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                if (line.StartsWith("data: ", StringComparison.Ordinal))
                {
                    var data = line["data: ".Length..];

                    if (data == "[DONE]")
                    {
                        break;
                    }

                    var chunk = VllmRequestSerializer.DeserializeStreamChunk(data);
                    yield return chunk;
                }
            }
        }
        finally
        {
            reader?.Dispose();
            stream?.Dispose();
            response?.Dispose();
        }
    }

    /// <summary>
    /// Asynchronously disposes the HTTP client.
    /// </summary>
    /// <returns>A ValueTask representing the asynchronous disposal operation.</returns>
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _httpClient.Dispose();
        _disposed = true;
        await ValueTask.CompletedTask.ConfigureAwait(false);
    }

    private static void ThrowForStatusCode(System.Net.HttpStatusCode statusCode, string errorContent)
    {
        var message = $"vLLM returned {(int)statusCode}: {errorContent}";

        switch ((int)statusCode)
        {
            case 401:
            case 403:
                throw new VllmAuthException(message);
            case 404:
                throw new VllmModelNotFoundException(message);
            case 429:
                throw new VllmRateLimitException(message);
            case >= 400 and < 500:
                throw new VllmRequestException(message);
            case >= 500:
                throw new VllmServerException(message);
            default:
                throw new VllmException("ACODE-VLM-999", message);
        }
    }

    private static bool IsConnectionTimeout(TaskCanceledException ex)
    {
        // Check if this is a connection timeout (vs request timeout)
        // Connection timeouts have a TimeoutException in the chain
        var current = ex.InnerException;
        while (current != null)
        {
            if (current is TimeoutException)
            {
                return true;
            }

            current = current.InnerException;
        }

        return false;
    }
}
