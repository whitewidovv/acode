namespace Acode.Application.Security;

using Acode.Domain.Risks;

/// <summary>
/// Repository for accessing the risk register.
/// </summary>
public interface IRiskRegister
{
    /// <summary>
    /// Gets the version of the risk register.
    /// </summary>
    string Version { get; }

    /// <summary>
    /// Gets the last update date of the risk register.
    /// </summary>
    DateTimeOffset LastUpdated { get; }

    /// <summary>
    /// Gets all risks in the register.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of all risks in the register.</returns>
    Task<IReadOnlyList<Risk>> GetAllRisksAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a specific risk by ID.
    /// </summary>
    /// <param name="id">The risk identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The risk if found, or null if not found.</returns>
    Task<Risk?> GetRiskAsync(RiskId id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all risks in a specific category.
    /// </summary>
    /// <param name="category">The STRIDE category to filter by.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of risks in the specified category.</returns>
    Task<IReadOnlyList<Risk>> GetRisksByCategoryAsync(
        RiskCategory category,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all risks of a specific severity level.
    /// </summary>
    /// <param name="severity">The severity level to filter by.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of risks at the specified severity level.</returns>
    Task<IReadOnlyList<Risk>> GetRisksBySeverityAsync(
        Severity severity,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches risks by keyword in title or description.
    /// </summary>
    /// <param name="keyword">The search keyword.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of risks matching the keyword.</returns>
    Task<IReadOnlyList<Risk>> SearchRisksAsync(
        string keyword,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all mitigations in the register.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of all mitigations.</returns>
    Task<IReadOnlyList<Mitigation>> GetAllMitigationsAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all mitigations for a specific risk.
    /// </summary>
    /// <param name="riskId">The risk identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of mitigations for the specified risk.</returns>
    Task<IReadOnlyList<Mitigation>> GetMitigationsForRiskAsync(
        RiskId riskId,
        CancellationToken cancellationToken = default);
}
