using System.Text.RegularExpressions;

namespace Acode.Infrastructure.Providers.Vllm.Lifecycle;

/// <summary>
/// Validates and manages Huggingface model loading for vLLM.
/// Handles model ID validation, airgapped mode detection, and authentication.
/// </summary>
public sealed partial class VllmModelLoader
{
    private bool _isAirgappedMode;
    private string? _hfToken;

    /// <summary>
    /// Gets a value indicating whether airgapped mode is enabled.
    /// In airgapped mode, models must be pre-cached locally.
    /// </summary>
    public bool IsAirgappedMode => _isAirgappedMode;

    /// <summary>
    /// Gets the Huggingface token for authentication.
    /// </summary>
    public string? HfToken => _hfToken;

    /// <summary>
    /// Gets a value indicating whether a Huggingface token is set.
    /// </summary>
    public bool HasHfToken => !string.IsNullOrWhiteSpace(_hfToken);

    /// <summary>
    /// Sets the airgapped mode flag.
    /// </summary>
    /// <param name="isAirgapped">True to enable airgapped mode.</param>
    public void SetAirgappedMode(bool isAirgapped)
    {
        _isAirgappedMode = isAirgapped;
    }

    /// <summary>
    /// Sets the Huggingface token for authentication.
    /// </summary>
    /// <param name="token">The HF token, or null/empty to clear.</param>
    public void SetHfToken(string? token)
    {
        _hfToken = string.IsNullOrWhiteSpace(token) ? null : token;
    }

    /// <summary>
    /// Validates that a model ID is in the correct Huggingface format (org/model-name).
    /// </summary>
    /// <param name="modelId">The model ID to validate.</param>
    /// <returns>True if valid format, false otherwise.</returns>
    public bool IsValidModelIdFormat(string modelId)
    {
        if (string.IsNullOrWhiteSpace(modelId))
        {
            return false;
        }

        // Valid format: exactly one slash, with non-empty parts before and after
        // Example: meta-llama/Llama-2-7b-hf
        return ModelIdFormatRegex().IsMatch(modelId);
    }

    /// <summary>
    /// Validates a model ID and throws if invalid.
    /// </summary>
    /// <param name="modelId">The model ID to validate.</param>
    /// <returns>A completed task if validation succeeds.</returns>
    /// <exception cref="ArgumentException">Thrown if model ID is invalid.</exception>
    public Task ValidateModelIdAsync(string modelId)
    {
        if (string.IsNullOrWhiteSpace(modelId))
        {
            throw new ArgumentException("Model ID cannot be empty. Provide a valid Huggingface model ID.", nameof(modelId));
        }

        if (!IsValidModelIdFormat(modelId))
        {
            throw new ArgumentException(
                $"Invalid model ID format: '{modelId}'. " +
                "Use Huggingface format: org/model-name (e.g., meta-llama/Llama-2-7b-hf)",
                nameof(modelId));
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Checks if a model can be loaded given current settings (airgapped mode, format, etc.).
    /// </summary>
    /// <param name="modelId">The model ID to check.</param>
    /// <returns>True if model can be loaded, false otherwise.</returns>
    public Task<bool> CanLoadModelAsync(string modelId)
    {
        // First check format
        if (!IsValidModelIdFormat(modelId))
        {
            return Task.FromResult(false);
        }

        // In airgapped mode, we cannot load from HF
        // (would need to check local cache, but for now assume not cached)
        if (_isAirgappedMode)
        {
            return Task.FromResult(false);
        }

        return Task.FromResult(true);
    }

    /// <summary>
    /// Gets an error message explaining why a model cannot be loaded.
    /// </summary>
    /// <param name="modelId">The model ID.</param>
    /// <returns>Error message, or null if model can be loaded.</returns>
    public string? GetModelLoadError(string modelId)
    {
        if (!IsValidModelIdFormat(modelId))
        {
            return $"Invalid model ID format: '{modelId}'. " +
                   "Use Huggingface format: org/model-name (e.g., meta-llama/Llama-2-7b-hf)";
        }

        if (_isAirgappedMode)
        {
            return $"Cannot load model '{modelId}' in airgapped mode. " +
                   "Models must be pre-downloaded before enabling airgapped mode. " +
                   $"To pre-download, run: huggingface-cli download {modelId}";
        }

        return null;
    }

    /// <summary>
    /// Gets guidance for Huggingface authentication.
    /// </summary>
    /// <returns>Authentication guidance message.</returns>
    public string GetAuthenticationGuidance()
    {
        return "To authenticate with Huggingface for private/gated models:\n" +
               "1. Create a token at https://huggingface.co/settings/tokens\n" +
               "2. Set the HF_TOKEN environment variable: export HF_TOKEN=your_token\n" +
               "3. Or configure in .agent/config.yml: vllm.hf_token: your_token";
    }

    /// <summary>
    /// Parses a model ID into its organization and model name components.
    /// </summary>
    /// <param name="modelId">The model ID to parse.</param>
    /// <returns>Tuple of (organization, modelName), or (null, null) if invalid.</returns>
    public (string? Organization, string? ModelName) ParseModelId(string modelId)
    {
        if (!IsValidModelIdFormat(modelId))
        {
            return (null, null);
        }

        var parts = modelId.Split('/');
        return (parts[0], parts[1]);
    }

    /// <summary>
    /// Regex pattern for valid Huggingface model ID format.
    /// Format: org/model-name where both parts are non-empty and contain valid characters.
    /// </summary>
    [GeneratedRegex(@"^[a-zA-Z0-9_-]+/[a-zA-Z0-9_.-]+$", RegexOptions.Compiled)]
    private static partial Regex ModelIdFormatRegex();
}
