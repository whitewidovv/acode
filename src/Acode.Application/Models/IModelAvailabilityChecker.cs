namespace Acode.Application.Models;

/// <summary>
/// Defines the contract for checking model availability.
/// </summary>
/// <remarks>
/// Model availability is determined by querying registered providers
/// to see if a specific model ID is currently loaded and ready for inference.
///
/// Availability checks are cached to reduce overhead when making routing decisions.
/// </remarks>
public interface IModelAvailabilityChecker
{
    /// <summary>
    /// Checks if a specific model is currently available.
    /// </summary>
    /// <param name="modelId">The model identifier to check (e.g., "llama3.2:7b").</param>
    /// <returns>True if the model is loaded and ready; false otherwise.</returns>
    /// <remarks>
    /// This method queries all registered providers to determine if the specified
    /// model is available. Results are cached for performance.
    ///
    /// A model is considered available if:
    /// - It is listed in at least one provider's supported models.
    /// - The provider is healthy and reachable.
    /// </remarks>
    bool IsModelAvailable(string modelId);

    /// <summary>
    /// Lists all models currently available across all providers.
    /// </summary>
    /// <returns>Read-only list of available model IDs.</returns>
    /// <remarks>
    /// Returns a deduplicated list of all models supported by all registered providers.
    /// Results are cached and refreshed every 5 seconds.
    /// </remarks>
    IReadOnlyList<string> ListAvailableModels();
}
