namespace Acode.Application.Inference;

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Acode.Domain.Models.Inference;

/// <summary>
/// Interface for model inference providers (Ollama, vLLM, etc.).
/// </summary>
/// <remarks>
/// FR-004-81: IModelProvider interface defined.
/// FR-004-82 to FR-004-90: Methods and properties for chat completion.
/// </remarks>
public interface IModelProvider
{
    /// <summary>
    /// Gets the provider name/identifier.
    /// </summary>
    /// <remarks>
    /// FR-004-81: ProviderName property (string, unique identifier like "ollama", "vllm").
    /// </remarks>
    string ProviderName { get; }

    /// <summary>
    /// Gets the capabilities of this provider.
    /// </summary>
    /// <remarks>
    /// FR-004-82: Capabilities property (ProviderCapabilities, describes features).
    /// </remarks>
    ProviderCapabilities Capabilities { get; }

    /// <summary>
    /// Performs non-streaming chat completion.
    /// </summary>
    /// <param name="request">Chat request with messages and parameters.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Complete chat response.</returns>
    /// <remarks>
    /// FR-004-83, FR-004-84: ChatAsync method accepting ChatRequest and CancellationToken.
    /// </remarks>
    Task<ChatResponse> ChatAsync(ChatRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs streaming chat completion.
    /// </summary>
    /// <param name="request">Chat request with stream=true.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Async stream of response deltas.</returns>
    /// <remarks>
    /// FR-004-85, FR-004-86, FR-004-87: StreamChatAsync method returning IAsyncEnumerable with CancellationToken.
    /// </remarks>
    IAsyncEnumerable<ResponseDelta> StreamChatAsync(ChatRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if the provider is healthy and reachable.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if healthy, false otherwise.</returns>
    /// <remarks>
    /// FR-004-88, FR-004-89: IsHealthyAsync method with CancellationToken.
    /// </remarks>
    Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the list of supported model identifiers.
    /// </summary>
    /// <returns>Array of model identifiers.</returns>
    /// <remarks>
    /// FR-004-90: GetSupportedModels method returning string array.
    /// </remarks>
    string[] GetSupportedModels();
}
