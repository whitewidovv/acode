namespace Acode.Application.Providers.Selection;

using System.Collections.Generic;
using Acode.Application.Inference;

/// <summary>
/// Strategy interface for selecting a provider based on request requirements.
/// </summary>
/// <remarks>
/// FR-081 to FR-085 from task-004c spec.
/// Gap #9 from task-004c completion checklist.
/// </remarks>
public interface IProviderSelector
{
    /// <summary>
    /// Selects the most suitable provider for a given request.
    /// </summary>
    /// <param name="providers">Available provider descriptors.</param>
    /// <param name="request">Chat request with requirements.</param>
    /// <param name="healthStatus">Current health status of all providers.</param>
    /// <returns>Selected provider instance, or null if no suitable provider found.</returns>
    /// <remarks>
    /// FR-081: SelectProvider method accepting providers, request, and health status.
    /// FR-082: Must return null if no provider can handle the request.
    /// FR-083: Must consider provider capabilities when selecting.
    /// FR-084: Must consider provider health status when selecting.
    /// FR-085: Implementation strategy determines selection algorithm.
    /// </remarks>
    IModelProvider? SelectProvider(
        IReadOnlyList<ProviderDescriptor> providers,
        ChatRequest request,
        IReadOnlyDictionary<string, ProviderHealth> healthStatus);
}
