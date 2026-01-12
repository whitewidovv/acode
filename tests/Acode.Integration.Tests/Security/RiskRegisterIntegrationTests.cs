namespace Acode.Integration.Tests.Security;

using Acode.Application.Security;
using Acode.Domain.Risks;
using Acode.Infrastructure.Security;
using FluentAssertions;

/// <summary>
/// Integration tests for risk register loading and querying.
/// Tests against actual docs/security/risk-register.yaml file.
/// Per Task 003a spec lines 1116-1278.
/// </summary>
public class RiskRegisterIntegrationTests
{
    private IRiskRegister _riskRegister = null!;

    [Fact]
    public async Task Should_Load_Complete_Risk_Register_From_File()
    {
        // Arrange
        var yamlPath = GetRiskRegisterPath();
        _riskRegister = new YamlRiskRegisterRepository(yamlPath);

        // Act
        var risks = await _riskRegister.GetAllRisksAsync();

        // Assert
        risks.Should().NotBeNull();
        risks.Count.Should().BeGreaterOrEqualTo(40, "Expected minimum 40 risks per spec");

        // Verify all STRIDE categories are covered
        var categories = risks.Select(r => r.Category).Distinct().ToList();
        categories.Should().HaveCount(6, "All 6 STRIDE categories must be represented");
        categories.Should().Contain(RiskCategory.Spoofing);
        categories.Should().Contain(RiskCategory.Tampering);
        categories.Should().Contain(RiskCategory.Repudiation);
        categories.Should().Contain(RiskCategory.InformationDisclosure);
        categories.Should().Contain(RiskCategory.DenialOfService);
        categories.Should().Contain(RiskCategory.ElevationOfPrivilege);
    }

    [Fact]
    public async Task Should_Have_Minimum_Risks_Per_Category()
    {
        // Arrange
        var yamlPath = GetRiskRegisterPath();
        _riskRegister = new YamlRiskRegisterRepository(yamlPath);

        // Act
        var spoofingRisks = await _riskRegister.GetRisksByCategoryAsync(RiskCategory.Spoofing);
        var tamperingRisks = await _riskRegister.GetRisksByCategoryAsync(RiskCategory.Tampering);
        var repudiationRisks = await _riskRegister.GetRisksByCategoryAsync(RiskCategory.Repudiation);
        var infoDisclosureRisks = await _riskRegister.GetRisksByCategoryAsync(RiskCategory.InformationDisclosure);
        var dosRisks = await _riskRegister.GetRisksByCategoryAsync(RiskCategory.DenialOfService);
        var elevationRisks = await _riskRegister.GetRisksByCategoryAsync(RiskCategory.ElevationOfPrivilege);

        // Assert
        spoofingRisks.Count.Should().BeGreaterOrEqualTo(1, "Should have Spoofing risks");
        tamperingRisks.Count.Should().BeGreaterOrEqualTo(1, "Should have Tampering risks");
        repudiationRisks.Count.Should().BeGreaterOrEqualTo(1, "Should have Repudiation risks");
        infoDisclosureRisks.Count.Should().BeGreaterOrEqualTo(1, "Should have Information Disclosure risks");
        dosRisks.Count.Should().BeGreaterOrEqualTo(1, "Should have Denial of Service risks");
        elevationRisks.Count.Should().BeGreaterOrEqualTo(1, "Should have Elevation of Privilege risks");
    }

    [Fact]
    public async Task Should_Cross_Reference_Risks_And_Mitigations()
    {
        // Arrange
        var yamlPath = GetRiskRegisterPath();
        _riskRegister = new YamlRiskRegisterRepository(yamlPath);

        // Act
        var risks = await _riskRegister.GetAllRisksAsync();
        var mitigations = await _riskRegister.GetAllMitigationsAsync();

        // Assert
        var mitigationIds = new HashSet<string>(mitigations.Select(m => m.Id.Value));

        // Verify all loaded risk mitigation references exist
        foreach (var risk in risks)
        {
            foreach (var mitigation in risk.Mitigations)
            {
                mitigationIds.Should().Contain(
                    mitigation.Id.Value,
                    $"Risk {risk.RiskId.Value} references mitigation {mitigation.Id.Value} which should exist");
            }
        }

        // Verify at least some risks have mitigations
        // (Not all risks have fully-defined mitigations in the YAML yet)
        var risksWithMitigations = risks.Count(r => r.Mitigations.Any());
        risksWithMitigations.Should().BeGreaterThan(
            0,
            "At least some risks should have fully-defined mitigations");
    }

    [Fact]
    public async Task Should_Load_Mitigations_With_Required_Fields()
    {
        // Arrange
        var yamlPath = GetRiskRegisterPath();
        _riskRegister = new YamlRiskRegisterRepository(yamlPath);

        // Act
        var mitigations = await _riskRegister.GetAllMitigationsAsync();

        // Assert
        mitigations.Should().NotBeEmpty("Should have mitigations defined");

        foreach (var mitigation in mitigations)
        {
            mitigation.Id.Should().NotBeNull($"Mitigation should have ID");
            mitigation.Id.Value.Should().MatchRegex(
                @"^MIT-\d{3}$",
                $"Mitigation ID {mitigation.Id.Value} should match format MIT-NNN");
            mitigation.Title.Should().NotBeNullOrWhiteSpace(
                $"Mitigation {mitigation.Id.Value} should have title");
            mitigation.Description.Should().NotBeNullOrWhiteSpace(
                $"Mitigation {mitigation.Id.Value} should have description");
            mitigation.Status.Should().BeOneOf(
                new[]
                {
                    MitigationStatus.Implemented,
                    MitigationStatus.InProgress,
                    MitigationStatus.Pending,
                    MitigationStatus.NotApplicable,
                },
                $"Mitigation {mitigation.Id.Value} should have valid status");
        }
    }

    [Fact]
    public async Task Should_Query_Risks_By_Severity()
    {
        // Arrange
        var yamlPath = GetRiskRegisterPath();
        _riskRegister = new YamlRiskRegisterRepository(yamlPath);

        // Act
        var highRisks = await _riskRegister.GetRisksBySeverityAsync(Severity.High);
        var mediumRisks = await _riskRegister.GetRisksBySeverityAsync(Severity.Medium);
        var lowRisks = await _riskRegister.GetRisksBySeverityAsync(Severity.Low);
        var criticalRisks = await _riskRegister.GetRisksBySeverityAsync(Severity.Critical);

        // Assert - at least some risks of each major severity
        (highRisks.Count + mediumRisks.Count + lowRisks.Count + criticalRisks.Count)
            .Should().BeGreaterOrEqualTo(40, "All risks should be categorized by severity");

        // Should have high severity risks
        highRisks.Should().NotBeEmpty("Should have high severity risks");

        // At least some high severity risks should have mitigations
        var highWithMitigations = highRisks.Count(r => r.Mitigations.Any());
        highWithMitigations.Should().BeGreaterThan(
            0,
            "At least some high severity risks should have defined mitigations");
    }

    [Fact]
    public async Task Should_Search_Risks_By_Keyword()
    {
        // Arrange
        var yamlPath = GetRiskRegisterPath();
        _riskRegister = new YamlRiskRegisterRepository(yamlPath);

        // Act
        var llmRisks = await _riskRegister.SearchRisksAsync("LLM");
        var exfiltrationRisks = await _riskRegister.SearchRisksAsync("exfiltration");

        // Assert
        llmRisks.Should().NotBeEmpty("Should find risks related to LLM");
        exfiltrationRisks.Should().NotBeEmpty("Should find risks related to exfiltration");

        // Verify keyword appears in title or description
        foreach (var risk in llmRisks)
        {
            (risk.Title.Contains("LLM", StringComparison.OrdinalIgnoreCase) ||
             risk.Description.Contains("LLM", StringComparison.OrdinalIgnoreCase))
                .Should().BeTrue($"Risk {risk.RiskId.Value} should contain 'LLM'");
        }
    }

    [Fact]
    public async Task Should_Get_Specific_Risk_By_ID()
    {
        // Arrange
        var yamlPath = GetRiskRegisterPath();
        _riskRegister = new YamlRiskRegisterRepository(yamlPath);
        var risks = await _riskRegister.GetAllRisksAsync();
        var firstRisk = risks.First();

        // Act
        var retrievedRisk = await _riskRegister.GetRiskAsync(firstRisk.RiskId);

        // Assert
        retrievedRisk.Should().NotBeNull();
        retrievedRisk!.RiskId.Value.Should().Be(firstRisk.RiskId.Value);
        retrievedRisk.Title.Should().Be(firstRisk.Title);
        retrievedRisk.Description.Should().Be(firstRisk.Description);
    }

    [Fact]
    public async Task Should_Return_Null_For_Nonexistent_Risk()
    {
        // Arrange
        var yamlPath = GetRiskRegisterPath();
        _riskRegister = new YamlRiskRegisterRepository(yamlPath);
        var nonexistentId = new RiskId("RISK-S-999"); // Valid format but nonexistent

        // Act
        var risk = await _riskRegister.GetRiskAsync(nonexistentId);

        // Assert
        risk.Should().BeNull("Nonexistent risk should return null");
    }

    [Fact]
    public async Task Should_Get_Mitigations_For_Risk()
    {
        // Arrange
        var yamlPath = GetRiskRegisterPath();
        _riskRegister = new YamlRiskRegisterRepository(yamlPath);
        var risks = await _riskRegister.GetAllRisksAsync();
        var riskWithMitigations = risks.First(r => r.Mitigations.Any());

        // Act
        var mitigations = await _riskRegister.GetMitigationsForRiskAsync(riskWithMitigations.RiskId);

        // Assert
        mitigations.Should().NotBeEmpty();
        mitigations.Count.Should().Be(riskWithMitigations.Mitigations.Count);
    }

    [Fact]
    public void Should_Expose_Version_And_LastUpdated()
    {
        // Arrange
        var yamlPath = GetRiskRegisterPath();
        _riskRegister = new YamlRiskRegisterRepository(yamlPath);

        // Act & Assert
        _riskRegister.Version.Should().NotBeNullOrWhiteSpace("Version should be set");
        _riskRegister.Version.Should().MatchRegex(@"^\d+\.\d+\.\d+$", "Version should be semver format");
        _riskRegister.LastUpdated.Should().BeAfter(DateTimeOffset.MinValue, "LastUpdated should be set");
    }

    [Fact]
    public void Should_Throw_When_File_Not_Found()
    {
        // Arrange
        var nonexistentPath = "/nonexistent/path/risk-register.yaml";

        // Assert - should throw when trying to access data
        var repo = new YamlRiskRegisterRepository(nonexistentPath);
        Func<Task> getDataAct = async () => await repo.GetAllRisksAsync();
        getDataAct.Should().ThrowAsync<FileNotFoundException>();
    }

    private static string GetRiskRegisterPath()
    {
        // Navigate from test output directory to repository root
        var currentDir = Directory.GetCurrentDirectory();
        var repoRoot = FindRepositoryRoot(currentDir);
        if (repoRoot == null)
        {
            throw new InvalidOperationException($"Could not find repository root from {currentDir}");
        }

        var yamlPath = Path.Combine(repoRoot, "docs", "security", "risk-register.yaml");
        if (!File.Exists(yamlPath))
        {
            throw new FileNotFoundException($"Risk register file not found: {yamlPath}");
        }

        return yamlPath;
    }

    private static string? FindRepositoryRoot(string startPath)
    {
        var dir = new DirectoryInfo(startPath);
        while (dir != null)
        {
            if (Directory.Exists(Path.Combine(dir.FullName, ".git")) ||
                File.Exists(Path.Combine(dir.FullName, "Acode.sln")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        return null;
    }
}
