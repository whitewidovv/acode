using System.Text;
using Acode.Domain.Risks;

namespace Acode.Application.Security;

/// <summary>
/// Generates markdown documentation from risk register.
/// </summary>
public sealed class RiskRegisterMarkdownGenerator
{
    private readonly IRiskRegister _riskRegister;

    /// <summary>
    /// Initializes a new instance of the <see cref="RiskRegisterMarkdownGenerator"/> class.
    /// </summary>
    /// <param name="riskRegister">Risk register to generate documentation from.</param>
    public RiskRegisterMarkdownGenerator(IRiskRegister riskRegister)
    {
        _riskRegister = riskRegister ?? throw new ArgumentNullException(nameof(riskRegister));
    }

    /// <summary>
    /// Generates markdown documentation for all risks and mitigations.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Markdown content as string.</returns>
    public async Task<string> GenerateAsync(CancellationToken cancellationToken = default)
    {
        var sb = new StringBuilder();

        // Header
        sb.AppendLine("# Risk Register");
        sb.AppendLine();
        sb.AppendLine($"**Version**: {_riskRegister.Version}");
        sb.AppendLine($"**Last Updated**: {_riskRegister.LastUpdated:yyyy-MM-dd}");
        sb.AppendLine();
        sb.AppendLine("This document provides a comprehensive enumeration of security risks and their mitigations for Acode.");
        sb.AppendLine();
        sb.AppendLine("## Table of Contents");
        sb.AppendLine();
        sb.AppendLine("- [Risks by STRIDE Category](#risks-by-stride-category)");
        sb.AppendLine("  - [Spoofing](#spoofing)");
        sb.AppendLine("  - [Tampering](#tampering)");
        sb.AppendLine("  - [Repudiation](#repudiation)");
        sb.AppendLine("  - [Information Disclosure](#information-disclosure)");
        sb.AppendLine("  - [Denial of Service](#denial-of-service)");
        sb.AppendLine("  - [Elevation of Privilege](#elevation-of-privilege)");
        sb.AppendLine("- [Mitigations](#mitigations)");
        sb.AppendLine();

        // Risks by STRIDE category
        sb.AppendLine("## Risks by STRIDE Category");
        sb.AppendLine();

        await AppendRisksByCategoryAsync(sb, RiskCategory.Spoofing, "Spoofing", cancellationToken).ConfigureAwait(false);
        await AppendRisksByCategoryAsync(sb, RiskCategory.Tampering, "Tampering", cancellationToken).ConfigureAwait(false);
        await AppendRisksByCategoryAsync(sb, RiskCategory.Repudiation, "Repudiation", cancellationToken).ConfigureAwait(false);
        await AppendRisksByCategoryAsync(sb, RiskCategory.InformationDisclosure, "Information Disclosure", cancellationToken).ConfigureAwait(false);
        await AppendRisksByCategoryAsync(sb, RiskCategory.DenialOfService, "Denial of Service", cancellationToken).ConfigureAwait(false);
        await AppendRisksByCategoryAsync(sb, RiskCategory.ElevationOfPrivilege, "Elevation of Privilege", cancellationToken).ConfigureAwait(false);

        // Mitigations
        await AppendMitigationsAsync(sb, cancellationToken).ConfigureAwait(false);

        return sb.ToString();
    }

    private async Task AppendRisksByCategoryAsync(
        StringBuilder sb,
        RiskCategory category,
        string categoryName,
        CancellationToken cancellationToken)
    {
        var risks = await _riskRegister.GetRisksByCategoryAsync(category, cancellationToken).ConfigureAwait(false);

        sb.AppendLine($"### {categoryName}");
        sb.AppendLine();
        sb.AppendLine($"**Risks in Category**: {risks.Count}");
        sb.AppendLine();

        if (!risks.Any())
        {
            sb.AppendLine("*No risks defined in this category.*");
            sb.AppendLine();
            return;
        }

        sb.AppendLine("| ID | Title | Severity | DREAD | Status | Mitigations |");
        sb.AppendLine("|----|-------|----------|-------|--------|-------------|");

        foreach (var risk in risks.OrderBy(r => r.RiskId.Value))
        {
            var mitigationIds = risk.Mitigations.Any()
                ? string.Join(", ", risk.Mitigations.Select(m => m.Id.Value))
                : "None";

            sb.AppendLine($"| {risk.RiskId.Value} | {risk.Title} | {risk.Severity} | {risk.DreadScore.Average:F1} | {risk.Status} | {mitigationIds} |");
        }

        sb.AppendLine();

        // Detailed risk information
        sb.AppendLine("#### Detailed Risk Information");
        sb.AppendLine();

        foreach (var risk in risks.OrderBy(r => r.RiskId.Value))
        {
            sb.AppendLine($"##### {risk.RiskId.Value}: {risk.Title}");
            sb.AppendLine();
            sb.AppendLine($"- **Description**: {risk.Description}");
            sb.AppendLine($"- **Severity**: {risk.Severity}");
            sb.AppendLine($"- **Status**: {risk.Status}");
            sb.AppendLine($"- **Owner**: {risk.Owner}");
            sb.AppendLine($"- **Created**: {risk.Created:yyyy-MM-dd}");
            sb.AppendLine($"- **Last Review**: {risk.LastReview:yyyy-MM-dd}");
            sb.AppendLine();
            sb.AppendLine("**DREAD Score**:");
            sb.AppendLine($"- Damage: {risk.DreadScore.Damage}/10");
            sb.AppendLine($"- Reproducibility: {risk.DreadScore.Reproducibility}/10");
            sb.AppendLine($"- Exploitability: {risk.DreadScore.Exploitability}/10");
            sb.AppendLine($"- Affected Users: {risk.DreadScore.AffectedUsers}/10");
            sb.AppendLine($"- Discoverability: {risk.DreadScore.Discoverability}/10");
            sb.AppendLine($"- **Average**: {risk.DreadScore.Average:F1}");
            sb.AppendLine();

            if (risk.AttackVectors?.Any() == true)
            {
                sb.AppendLine("**Attack Vectors**:");
                foreach (var vector in risk.AttackVectors)
                {
                    sb.AppendLine($"- {vector}");
                }

                sb.AppendLine();
            }

            if (risk.Mitigations.Any())
            {
                sb.AppendLine("**Mitigations**:");
                foreach (var mitigation in risk.Mitigations)
                {
                    sb.AppendLine($"- [{mitigation.Id.Value}](#mit-{mitigation.Id.Value.ToLowerInvariant()}): {mitigation.Title}");
                }

                sb.AppendLine();
            }

            if (!string.IsNullOrWhiteSpace(risk.ResidualRisk))
            {
                sb.AppendLine($"**Residual Risk**: {risk.ResidualRisk}");
                sb.AppendLine();
            }

            sb.AppendLine("---");
            sb.AppendLine();
        }
    }

    private async Task AppendMitigationsAsync(StringBuilder sb, CancellationToken cancellationToken)
    {
        var mitigations = await _riskRegister.GetAllMitigationsAsync(cancellationToken).ConfigureAwait(false);

        sb.AppendLine("## Mitigations");
        sb.AppendLine();
        sb.AppendLine($"**Total Mitigations**: {mitigations.Count}");
        sb.AppendLine();

        if (!mitigations.Any())
        {
            sb.AppendLine("*No mitigations defined.*");
            sb.AppendLine();
            return;
        }

        sb.AppendLine("| ID | Title | Status | Last Verified |");
        sb.AppendLine("|----|-------|--------|---------------|");

        foreach (var mitigation in mitigations.OrderBy(m => m.Id.Value))
        {
            var lastVerified = mitigation.LastVerified.Year > 1
                ? mitigation.LastVerified.ToString("yyyy-MM-dd")
                : "Never";

            sb.AppendLine($"| {mitigation.Id.Value} | {mitigation.Title} | {mitigation.Status} | {lastVerified} |");
        }

        sb.AppendLine();

        // Detailed mitigation information
        sb.AppendLine("### Detailed Mitigation Information");
        sb.AppendLine();

        foreach (var mitigation in mitigations.OrderBy(m => m.Id.Value))
        {
            sb.AppendLine($"#### <a name=\"mit-{mitigation.Id.Value.ToLowerInvariant()}\"></a>{mitigation.Id.Value}: {mitigation.Title}");
            sb.AppendLine();
            sb.AppendLine($"- **Description**: {mitigation.Description}");
            sb.AppendLine($"- **Status**: {mitigation.Status}");
            sb.AppendLine($"- **Implementation**: {mitigation.Implementation}");

            if (!string.IsNullOrWhiteSpace(mitigation.VerificationTest))
            {
                sb.AppendLine($"- **Verification Test**: {mitigation.VerificationTest}");
            }

            if (mitigation.LastVerified.Year > 1)
            {
                sb.AppendLine($"- **Last Verified**: {mitigation.LastVerified:yyyy-MM-dd}");
            }

            sb.AppendLine();
        }
    }
}
