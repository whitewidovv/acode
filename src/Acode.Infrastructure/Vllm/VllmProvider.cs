using System.Diagnostics;
using System.Runtime.CompilerServices;
using Acode.Application.Inference;
using Acode.Domain.Models.Inference;
using Acode.Infrastructure.Vllm.Client;
using Acode.Infrastructure.Vllm.Health;
using Acode.Infrastructure.Vllm.Models;

namespace Acode.Infrastructure.Vllm;

/// <summary>
/// Model provider implementation for vLLM inference engine.
/// </summary>
/// <remarks>
/// FR-006-001 to FR-006-033: VllmProvider implementation.
/// </remarks>
public sealed class VllmProvider : IModelProvider, IDisposable
{
    private readonly VllmClientConfiguration _config;
    private readonly VllmHttpClient _client;
    private readonly VllmHealthChecker _healthChecker;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="VllmProvider"/> class.
    /// </summary>
    /// <param name="config">Client configuration.</param>
    public VllmProvider(VllmClientConfiguration config)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _config.Validate();

        _client = new VllmHttpClient(_config);
        _healthChecker = new VllmHealthChecker(_config);
        _disposed = false;
    }

    /// <inheritdoc/>
    public string ProviderName => "vllm";

    /// <inheritdoc/>
    public ProviderCapabilities Capabilities => new ProviderCapabilities(
        supportsStreaming: true,
        supportsTools: true,
        supportsSystemMessages: true,
        supportsVision: false,
        maxContextLength: null,
        supportedModels: null,
        defaultModel: null);

    /// <inheritdoc/>
    public async Task<ChatResponse> ChatAsync(ChatRequest request, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(request);

        var stopwatch = Stopwatch.StartNew();

        var vllmRequest = MapToVllmRequest(request);
        var vllmResponse = await _client.SendRequestAsync(vllmRequest, cancellationToken).ConfigureAwait(false);

        stopwatch.Stop();

        return MapToChatResponse(vllmResponse, stopwatch.Elapsed);
    }

    /// <inheritdoc/>
    public async IAsyncEnumerable<ResponseDelta> StreamChatAsync(
        ChatRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(request);

        var vllmRequest = MapToVllmRequest(request);
        var index = 0;

        await foreach (var chunk in _client.StreamRequestAsync(vllmRequest, cancellationToken).ConfigureAwait(false))
        {
            var delta = MapToResponseDelta(chunk, index);
            index++;
            yield return delta;
        }
    }

    /// <inheritdoc/>
    public async Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return await _healthChecker.IsHealthyAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public string[] GetSupportedModels()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        // FR-006-031: Return common vLLM model identifiers
        return new[]
        {
            "meta-llama/Llama-2-7b-chat-hf",
            "meta-llama/Llama-2-13b-chat-hf",
            "meta-llama/Llama-2-70b-chat-hf",
            "meta-llama/Meta-Llama-3-8B-Instruct",
            "meta-llama/Meta-Llama-3-70B-Instruct",
            "mistralai/Mistral-7B-Instruct-v0.2",
            "mistralai/Mixtral-8x7B-Instruct-v0.1",
            "codellama/CodeLlama-7b-Instruct-hf",
            "codellama/CodeLlama-13b-Instruct-hf",
            "codellama/CodeLlama-34b-Instruct-hf",
        };
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _client.Dispose();
        _disposed = true;
    }

    private static FinishReason MapFinishReason(string? vllmReason)
    {
        return vllmReason switch
        {
            "stop" => FinishReason.Stop,
            "length" => FinishReason.Length,
            "tool_calls" => FinishReason.ToolCalls,
            "content_filter" => FinishReason.ContentFilter,
            null => FinishReason.Error,
            _ => FinishReason.Error,
        };
    }

    private static VllmRequest MapToVllmRequest(ChatRequest request)
    {
        var messages = request.Messages.Select(m => new VllmMessage
        {
            Role = m.Role.ToString().ToLowerInvariant(),
            Content = m.Content,
        }).ToList();

        var vllmRequest = new VllmRequest
        {
            Model = request.ModelParameters?.Model ?? throw new ArgumentException("Model is required", nameof(request)),
            Messages = messages,
            Temperature = request.ModelParameters?.Temperature,
            MaxTokens = request.ModelParameters?.MaxTokens,
            Stream = request.Stream,
            Tools = null, // Task 007e: Tool schema integration deferred
        };

        return vllmRequest;
    }

    private ChatResponse MapToChatResponse(VllmResponse response, TimeSpan duration)
    {
        var choice = response.Choices.FirstOrDefault()
            ?? throw new InvalidOperationException("vLLM response missing choices");

        var message = ChatMessage.CreateAssistant(
            content: choice.Message.Content,
            toolCalls: null); // Task 007e: Tool call mapping deferred

        var finishReason = MapFinishReason(choice.FinishReason);

        var usage = new UsageInfo(
            PromptTokens: response.Usage?.PromptTokens ?? 0,
            CompletionTokens: response.Usage?.CompletionTokens ?? 0);

        var metadata = new ResponseMetadata(
            ProviderId: ProviderName,
            ModelId: response.Model,
            RequestDuration: duration,
            TimeToFirstToken: null);

        return new ChatResponse(
            Id: response.Id,
            Message: message,
            FinishReason: finishReason,
            Usage: usage,
            Metadata: metadata,
            Created: DateTimeOffset.FromUnixTimeSeconds(response.Created),
            Model: response.Model,
            Refusal: null);
    }

    private ResponseDelta MapToResponseDelta(VllmStreamChunk chunk, int index)
    {
        var choice = chunk.Choices.FirstOrDefault();

        if (choice == null)
        {
            throw new InvalidOperationException("vLLM stream chunk missing choices");
        }

        var contentDelta = choice.Delta?.Content;
        var finishReason = choice.FinishReason != null ? MapFinishReason(choice.FinishReason) : (FinishReason?)null;

        // Final delta with usage info
        if (finishReason.HasValue)
        {
            var usage = chunk.Usage != null
                ? new UsageInfo(
                    PromptTokens: chunk.Usage.PromptTokens,
                    CompletionTokens: chunk.Usage.CompletionTokens)
                : null;

            return new ResponseDelta(
                index: index,
                contentDelta: contentDelta,
                toolCallDelta: null,
                finishReason: finishReason,
                usage: usage);
        }

        // Intermediate delta
        return new ResponseDelta(
            index: index,
            contentDelta: contentDelta,
            toolCallDelta: null);
    }
}
