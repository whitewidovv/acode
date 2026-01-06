namespace Acode.Infrastructure.Ollama;

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Acode.Application.Inference;
using Acode.Domain.Models.Inference;
using Acode.Infrastructure.Ollama.Exceptions;
using Acode.Infrastructure.Ollama.Health;
using Acode.Infrastructure.Ollama.Http;
using Acode.Infrastructure.Ollama.Mapping;

/// <summary>
/// Ollama provider implementation of <see cref="IModelProvider"/>.
/// </summary>
/// <remarks>
/// FR-005-062 to FR-005-095: Complete Ollama provider implementation.
/// </remarks>
public sealed class OllamaProvider : IModelProvider
{
    private readonly HttpClient _httpClient;
    private readonly OllamaConfiguration _config;
    private readonly OllamaHealthChecker _healthChecker;

    /// <summary>
    /// Initializes a new instance of the <see cref="OllamaProvider"/> class.
    /// </summary>
    /// <param name="httpClient">HTTP client for requests.</param>
    /// <param name="config">Ollama configuration.</param>
    public OllamaProvider(HttpClient httpClient, OllamaConfiguration config)
    {
        this._httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        this._config = config ?? throw new ArgumentNullException(nameof(config));
        this._healthChecker = new OllamaHealthChecker(httpClient, config.BaseUrl);
    }

    /// <summary>
    /// Gets the provider name.
    /// </summary>
    /// <remarks>
    /// FR-005-062: ProviderName returns "ollama".
    /// </remarks>
    public string ProviderName => "ollama";

    /// <summary>
    /// Gets the provider capabilities.
    /// </summary>
    /// <remarks>
    /// FR-005-063 to FR-005-068: Capabilities configuration.
    /// </remarks>
    public ProviderCapabilities Capabilities => new ProviderCapabilities(
        supportsStreaming: true,
        supportsTools: true,
        supportsSystemMessages: true,
        supportsVision: false, // Ollama vision support varies by model
        maxContextLength: null, // Model-dependent
        supportedModels: this.GetSupportedModels(),
        defaultModel: this._config.DefaultModel);

    /// <summary>
    /// Sends a chat completion request.
    /// </summary>
    /// <param name="request">Chat request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Chat response.</returns>
    /// <remarks>
    /// FR-005-069 to FR-005-079: ChatAsync implementation.
    /// </remarks>
    public async Task<ChatResponse> ChatAsync(ChatRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request, nameof(request));

        try
        {
            var ollamaHttpClient = new OllamaHttpClient(this._httpClient, this._config.BaseUrl);
            var ollamaRequest = OllamaRequestMapper.Map(request, this._config.DefaultModel);

            var ollamaResponse = await ollamaHttpClient.PostChatAsync(ollamaRequest, cancellationToken).ConfigureAwait(false);

            return OllamaResponseMapper.Map(ollamaResponse);
        }
        catch (HttpRequestException ex)
        {
            // Check if this is an HTTP status code error (5xx = server error)
            if (ex.StatusCode.HasValue && (int)ex.StatusCode.Value >= 500)
            {
                throw new OllamaServerException($"Ollama server returned error: {ex.Message}", ex, (int)ex.StatusCode.Value);
            }

            throw new OllamaConnectionException($"Failed to connect to Ollama server at {this._config.BaseUrl}", ex);
        }
        catch (TaskCanceledException ex)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                throw;
            }

            throw new OllamaTimeoutException($"Request to Ollama server timed out after {this._config.RequestTimeoutSeconds}s", ex);
        }
    }

    /// <summary>
    /// Streams a chat completion request.
    /// </summary>
    /// <param name="request">Chat request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Async stream of response deltas.</returns>
    /// <remarks>
    /// FR-005-080 to FR-005-087: StreamChatAsync implementation.
    /// </remarks>
    public async IAsyncEnumerable<ResponseDelta> StreamChatAsync(
        ChatRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request, nameof(request));

        // Build Ollama request with stream=true
        var ollamaRequest = OllamaRequestMapper.Map(request, this._config.DefaultModel);
        var streamingRequest = ollamaRequest with { Stream = true };

        // Make HTTP POST request (exception handling here, before yield)
        HttpResponseMessage response;
        try
        {
            var requestUri = $"{this._config.BaseUrl}/api/chat";
            var content = new System.Net.Http.StringContent(
                System.Text.Json.JsonSerializer.Serialize(streamingRequest),
                System.Text.Encoding.UTF8,
                "application/json");

            response = await this._httpClient.PostAsync(requestUri, content, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
        }
        catch (HttpRequestException ex)
        {
            if (ex.StatusCode.HasValue && (int)ex.StatusCode.Value >= 500)
            {
                throw new OllamaServerException($"Ollama server returned error: {ex.Message}", ex, (int)ex.StatusCode.Value);
            }

            throw new OllamaConnectionException($"Failed to connect to Ollama server at {this._config.BaseUrl}", ex);
        }
        catch (TaskCanceledException ex)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                throw;
            }

            throw new OllamaTimeoutException($"Request to Ollama server timed out after {this._config.RequestTimeoutSeconds}s", ex);
        }

        // Read NDJSON stream and yield deltas (no try-catch here to allow yield)
        using (response)
        {
            var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            var index = 0;

            await foreach (var chunk in Streaming.OllamaStreamReader.ReadAsync(stream, cancellationToken).ConfigureAwait(false))
            {
                var delta = Mapping.OllamaDeltaMapper.MapToDelta(chunk, index);
                index++;
                yield return delta;

                if (chunk.Done)
                {
                    yield break;
                }
            }
        }
    }

    /// <summary>
    /// Checks if the provider is healthy.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if healthy, false otherwise.</returns>
    /// <remarks>
    /// FR-005-088: IsHealthyAsync delegates to OllamaHealthChecker.
    /// </remarks>
    public async Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default)
    {
        return await this._healthChecker.CheckHealthAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Gets supported model names.
    /// </summary>
    /// <returns>Array of supported model names.</returns>
    /// <remarks>
    /// FR-005-089 to FR-005-095: GetSupportedModels returns common Ollama models.
    /// </remarks>
    public string[] GetSupportedModels()
    {
        return new[]
        {
            "llama3.2:latest",
            "llama3.2:1b",
            "llama3.2:3b",
            "llama3.1:latest",
            "llama3.1:8b",
            "llama3.1:70b",
            "llama3:latest",
            "llama3:8b",
            "llama3:70b",
            "qwen2.5:latest",
            "qwen2.5:0.5b",
            "qwen2.5:1.5b",
            "qwen2.5:3b",
            "qwen2.5:7b",
            "qwen2.5:14b",
            "qwen2.5:32b",
            "qwen2.5:72b",
            "mistral:latest",
            "mixtral:latest",
            "gemma2:latest",
            "gemma2:2b",
            "gemma2:9b",
            "gemma2:27b",
        };
    }

    /// <summary>
    /// Gets metadata for a specific model.
    /// </summary>
    /// <param name="modelId">The model identifier.</param>
    /// <returns>Model metadata.</returns>
    /// <remarks>
    /// FR-004-19: Returns model metadata.
    /// All Ollama models are local and do not require network access since Ollama
    /// runs models locally on the user's machine.
    /// </remarks>
    public ModelInfo GetModelInfo(string modelId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(modelId, nameof(modelId));

        // All Ollama models are local and don't require network
        return new ModelInfo
        {
            ModelId = modelId,
            IsLocal = true,
            RequiresNetwork = false,
        };
    }
}
