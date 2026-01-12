namespace Acode.Application.Security;

using Acode.Domain.Risks;

/// <summary>
/// Data transfer object containing parsed risk register data.
/// </summary>
/// <param name="Version">The risk register version.</param>
/// <param name="LastUpdated">The last update timestamp.</param>
/// <param name="Risks">The list of risks.</param>
/// <param name="Mitigations">The list of mitigations.</param>
public record RiskRegisterData(
    string Version,
    DateTimeOffset LastUpdated,
    IReadOnlyList<Risk> Risks,
    IReadOnlyList<Mitigation> Mitigations);
