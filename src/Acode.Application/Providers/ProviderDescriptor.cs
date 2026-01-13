namespace Acode.Application.Providers;

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Acode.Application.Inference;

/// <summary>
/// Complete descriptor for a model provider including capabilities, endpoint, and configuration.
/// </summary>
/// <remarks>
/// FR-001 to FR-026 from task-004c spec.
/// Gap #1 from task-004c completion checklist.
/// </remarks>
public sealed record ProviderDescriptor
{
    private string? _id;
    private string? _name;
    private ProviderCapabilities? _capabilities;
    private ProviderEndpoint? _endpoint;

    /// <summary>
    /// Gets or initializes the unique identifier for this provider.
    /// </summary>
    /// <remarks>
    /// Must be lowercase alphanumeric with hyphens only (e.g., "ollama-local", "vllm-remote").
    /// </remarks>
    public required string Id
    {
        get => _id!;
        init
        {
            ArgumentNullException.ThrowIfNull(value, nameof(Id));

            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("Id cannot be empty", nameof(Id));
            }

            if (!Regex.IsMatch(value, "^[a-z0-9-]+$"))
            {
                throw new ArgumentException(
                    "Id must contain only lowercase alphanumeric characters and hyphens",
                    nameof(Id));
            }

            _id = value;
        }
    }

    /// <summary>
    /// Gets or initializes the display name for this provider.
    /// </summary>
    public required string Name
    {
        get => _name!;
        init
        {
            ArgumentNullException.ThrowIfNull(value, nameof(Name));
            _name = value;
        }
    }

    /// <summary>
    /// Gets or initializes the provider type (local vs remote).
    /// </summary>
    public required ProviderType Type { get; init; }

    /// <summary>
    /// Gets or initializes the provider capabilities.
    /// </summary>
    public required ProviderCapabilities Capabilities
    {
        get => _capabilities!;
        init
        {
            ArgumentNullException.ThrowIfNull(value, nameof(Capabilities));
            _capabilities = value;
        }
    }

    /// <summary>
    /// Gets or initializes the provider endpoint configuration.
    /// </summary>
    public required ProviderEndpoint Endpoint
    {
        get => _endpoint!;
        init
        {
            ArgumentNullException.ThrowIfNull(value, nameof(Endpoint));
            _endpoint = value;
        }
    }

    /// <summary>
    /// Gets or initializes the provider-specific configuration.
    /// </summary>
    public ProviderConfig? Config { get; init; }

    /// <summary>
    /// Gets or initializes the retry policy for this provider.
    /// </summary>
    public RetryPolicy? RetryPolicy { get; init; }

    /// <summary>
    /// Gets or initializes the fallback provider ID to use if this provider fails.
    /// </summary>
    public string? FallbackProviderId { get; init; }

    /// <summary>
    /// Gets or initializes model name mappings (e.g., map "gpt-4" to "llama3").
    /// </summary>
    public Dictionary<string, string>? ModelMappings { get; init; }

    /// <summary>
    /// Gets or initializes the priority for fallback ordering (lower = preferred).
    /// </summary>
    /// <remarks>
    /// FR-020: Priority property for fallback ordering.
    /// Used when multiple fallback providers available.
    /// Lower priority values are preferred (0 is highest priority).
    /// </remarks>
    public int Priority { get; init; } = 0;

    /// <summary>
    /// Gets a value indicating whether this provider is enabled.
    /// </summary>
    /// <remarks>
    /// FR-021: Enabled property to enable/disable providers.
    /// Disabled providers (Enabled=false) are not available for selection.
    /// </remarks>
    public bool Enabled { get; init; } = true;
}
