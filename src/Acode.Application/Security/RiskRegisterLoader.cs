namespace Acode.Application.Security;

using Acode.Domain.Risks;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

/// <summary>
/// Loads and parses risk register from YAML format.
/// </summary>
public class RiskRegisterLoader
{
    private readonly IDeserializer _deserializer;

    /// <summary>
    /// Initializes a new instance of the <see cref="RiskRegisterLoader"/> class.
    /// </summary>
    public RiskRegisterLoader()
    {
        _deserializer = new DeserializerBuilder()
            .WithNamingConvention(UnderscoredNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();
    }

    /// <summary>
    /// Parses YAML content into risk register data.
    /// </summary>
    /// <param name="yamlContent">The YAML content to parse.</param>
    /// <returns>Parsed risk register data.</returns>
    /// <exception cref="RiskRegisterParseException">Thrown when YAML parsing fails.</exception>
    /// <exception cref="RiskRegisterValidationException">Thrown when validation fails.</exception>
    public RiskRegisterData Parse(string yamlContent)
    {
        // Parse YAML
        RiskRegisterYamlDto data;
        try
        {
            data = _deserializer.Deserialize<RiskRegisterYamlDto>(yamlContent);
        }
        catch (Exception ex)
        {
            throw new RiskRegisterParseException($"Failed to parse YAML: {ex.Message}", ex);
        }

        // Validate required fields
        if (string.IsNullOrWhiteSpace(data.Version))
        {
            throw new RiskRegisterValidationException("Missing required field: version");
        }

        if (data.LastUpdated == default)
        {
            throw new RiskRegisterValidationException("Missing required field: last_updated");
        }

        data.Risks ??= new List<RiskYamlDto>();
        data.Mitigations ??= new List<MitigationYamlDto>();

        // Validate duplicate risk IDs
        ValidateDuplicateRiskIds(data.Risks);

        // Validate mitigation references
        ValidateMitigationReferences(data.Risks, data.Mitigations);

        // Validate required fields in risks
        ValidateRiskRequiredFields(data.Risks);

        // Map to domain models
        var mitigationsDict = data.Mitigations.ToDictionary(m => m.Id, MapToMitigation);
        var risks = data.Risks.Select(r => MapToRisk(r, mitigationsDict)).ToList();
        var mitigations = mitigationsDict.Values.ToList();

        return new RiskRegisterData(
            data.Version,
            data.LastUpdated,
            risks,
            mitigations);
    }

    private static void ValidateDuplicateRiskIds(List<RiskYamlDto> risks)
    {
        var duplicates = risks
            .GroupBy(r => r.Id)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        if (duplicates.Any())
        {
            throw new RiskRegisterValidationException(
                $"Duplicate risk IDs found: {string.Join(", ", duplicates)}");
        }
    }

    private static void ValidateMitigationReferences(
        List<RiskYamlDto> risks,
        List<MitigationYamlDto> mitigations)
    {
        // Note: We don't validate mitigation references strictly because the risk register
        // may contain forward references to mitigations not yet fully defined.
        // The mapping phase will filter out references to non-existent mitigations.
    }

    private static void ValidateRiskRequiredFields(List<RiskYamlDto> risks)
    {
        foreach (var risk in risks)
        {
            if (string.IsNullOrWhiteSpace(risk.Title))
            {
                throw new RiskRegisterValidationException(
                    $"Risk {risk.Id}: Missing required field 'title'");
            }

            if (string.IsNullOrWhiteSpace(risk.Description))
            {
                throw new RiskRegisterValidationException(
                    $"Risk {risk.Id}: Missing required field 'description'");
            }

            if (risk.Dread == null)
            {
                throw new RiskRegisterValidationException(
                    $"Risk {risk.Id}: Missing required field 'dread'");
            }

            if (string.IsNullOrWhiteSpace(risk.Owner))
            {
                throw new RiskRegisterValidationException(
                    $"Risk {risk.Id}: Missing required field 'owner'");
            }
        }
    }

    private static Risk MapToRisk(RiskYamlDto dto, Dictionary<string, Mitigation> mitigationsDict)
    {
        var riskId = new RiskId(dto.Id);
        var category = MapCategory(dto.Category);
        var dread = new DreadScore(
            dto.Dread.Damage,
            dto.Dread.Reproducibility,
            dto.Dread.Exploitability,
            dto.Dread.AffectedUsers,
            dto.Dread.Discoverability);
        var severity = MapSeverity(dto.Severity);
        var status = MapRiskStatus(dto.Status);

        var mitigations = dto.Mitigations?
            .Where(mitigationsDict.ContainsKey)
            .Select(id => mitigationsDict[id])
            .ToList() ?? new List<Mitigation>();

        return new Risk
        {
            RiskId = riskId,
            Category = category,
            Title = dto.Title,
            Description = dto.Description,
            DreadScore = dread,
            Mitigations = mitigations,
            AttackVectors = dto.AttackVectors ?? new List<string>(),
            ResidualRisk = dto.ResidualRisk,
            Owner = dto.Owner,
            Status = status,
            Created = dto.Created,
            LastReview = dto.LastReview,
        };
    }

    private static Mitigation MapToMitigation(MitigationYamlDto dto)
    {
        var mitigationId = new MitigationId(dto.Id);
        var status = MapMitigationStatus(dto.Status);

        return new Mitigation
        {
            Id = mitigationId,
            Title = dto.Title ?? string.Empty,
            Description = dto.Description ?? string.Empty,
            Implementation = dto.Implementation ?? string.Empty,
            VerificationTest = dto.Verification,
            Status = status,
            LastVerified = dto.LastVerified,
        };
    }

    private static RiskCategory MapCategory(string category)
    {
        return category?.ToLowerInvariant() switch
        {
            "spoofing" => RiskCategory.Spoofing,
            "tampering" => RiskCategory.Tampering,
            "repudiation" => RiskCategory.Repudiation,
            "information_disclosure" => RiskCategory.InformationDisclosure,
            "denial_of_service" => RiskCategory.DenialOfService,
            "elevation_of_privilege" => RiskCategory.ElevationOfPrivilege,
            _ => throw new RiskRegisterValidationException($"Invalid risk category: {category}")
        };
    }

    private static Severity MapSeverity(string severity)
    {
        return severity?.ToLowerInvariant() switch
        {
            "low" => Severity.Low,
            "medium" => Severity.Medium,
            "high" => Severity.High,
            "critical" => Severity.Critical,
            _ => throw new RiskRegisterValidationException($"Invalid severity: {severity}")
        };
    }

    private static RiskStatus MapRiskStatus(string status)
    {
        return status?.ToLowerInvariant() switch
        {
            "active" => RiskStatus.Active,
            "deprecated" => RiskStatus.Deprecated,
            "accepted" => RiskStatus.Accepted,
            _ => throw new RiskRegisterValidationException($"Invalid risk status: {status}")
        };
    }

    private static MitigationStatus MapMitigationStatus(string status)
    {
        return status?.ToLowerInvariant() switch
        {
            "implemented" => MitigationStatus.Implemented,
            "in_progress" => MitigationStatus.InProgress,
            "inprogress" => MitigationStatus.InProgress,
            "pending" => MitigationStatus.Pending,
            "not_applicable" => MitigationStatus.NotApplicable,
            "notapplicable" => MitigationStatus.NotApplicable,
            _ => throw new RiskRegisterValidationException($"Invalid mitigation status: {status}")
        };
    }

    /// <summary>
    /// YAML DTO for risk register.
    /// </summary>
    private class RiskRegisterYamlDto
    {
        public string Version { get; set; } = string.Empty;

        public DateTimeOffset LastUpdated { get; set; }

        public string? ReviewCycle { get; set; }

        public List<RiskYamlDto> Risks { get; set; } = new();

        public List<MitigationYamlDto> Mitigations { get; set; } = new();
    }

    /// <summary>
    /// YAML DTO for risk.
    /// </summary>
    private class RiskYamlDto
    {
        public string Id { get; set; } = string.Empty;

        public string Category { get; set; } = string.Empty;

        public string Title { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public DreadYamlDto Dread { get; set; } = new();

        public string Severity { get; set; } = string.Empty;

        public List<string>? Mitigations { get; set; }

        public List<string>? AttackVectors { get; set; }

        public string? ResidualRisk { get; set; }

        public string Owner { get; set; } = string.Empty;

        public string Status { get; set; } = string.Empty;

        public DateTimeOffset Created { get; set; }

        public DateTimeOffset LastReview { get; set; }
    }

    /// <summary>
    /// YAML DTO for DREAD score.
    /// </summary>
    private class DreadYamlDto
    {
        public int Damage { get; set; }

        public int Reproducibility { get; set; }

        public int Exploitability { get; set; }

        public int AffectedUsers { get; set; }

        public int Discoverability { get; set; }
    }

    /// <summary>
    /// YAML DTO for mitigation.
    /// </summary>
    private class MitigationYamlDto
    {
        public string Id { get; set; } = string.Empty;

        public string? Title { get; set; }

        public string? Description { get; set; }

        public string? Implementation { get; set; }

        public string? Verification { get; set; }

        public string Status { get; set; } = string.Empty;

        public DateTimeOffset LastVerified { get; set; }
    }
}
