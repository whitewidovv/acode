namespace Acode.Application.Providers.Exceptions;

using System;

/// <summary>
/// Exception thrown when a requested provider is not found in the registry.
/// </summary>
/// <remarks>
/// FR-101 to FR-104 from task-004c spec.
/// Gap #13 from task-004c completion checklist.
/// </remarks>
public sealed class ProviderNotFoundException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ProviderNotFoundException"/> class.
    /// </summary>
    /// <param name="providerId">The ID of the provider that was not found.</param>
    public ProviderNotFoundException(string providerId)
        : base($"Provider '{providerId}' not found in registry")
    {
        ProviderId = providerId ?? throw new ArgumentNullException(nameof(providerId));
        ErrorCode = "ACODE-PRV-003";
    }

    /// <summary>
    /// Gets the ID of the provider that was not found.
    /// </summary>
    public string ProviderId { get; }

    /// <summary>
    /// Gets the error code for this exception.
    /// </summary>
    public string ErrorCode { get; }
}
