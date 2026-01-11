namespace Acode.Application.Tests.Security;

using Acode.Application.Security;
using FluentAssertions;

/// <summary>
/// Tests for RiskRegisterLoader YAML parsing.
/// Per Task 003a spec lines 857-946.
/// </summary>
public class RiskRegisterLoaderTests
{
    [Fact]
    public void Should_Parse_Valid_Risk_Register_YAML()
    {
        // Arrange
        var yaml = """
            version: "1.0.0"
            last_updated: "2025-01-03T10:00:00Z"
            risks:
              - id: RISK-I-001
                category: information_disclosure
                title: Source code exfiltration via LLM
                description: Code sent to external LLM API
                dread:
                  damage: 9
                  reproducibility: 10
                  exploitability: 3
                  affected_users: 10
                  discoverability: 7
                severity: high
                mitigations:
                  - MIT-001
                attack_vectors:
                  - "User configures external LLM provider"
                residual_risk: "Low - requires explicit user configuration"
                owner: security-team
                status: active
                created: "2025-01-01T00:00:00Z"
                last_review: "2025-01-03T00:00:00Z"
            mitigations:
              - id: MIT-001
                title: LocalOnly mode default
                description: Deny external LLM APIs by default
                implementation: "ModeMatrix.cs, LlmApiDenylist.cs"
                verification_test: "ModeMatrixTests.LocalOnly_Should_Deny_External_LLM_APIs"
                status: implemented
                last_verified: "2025-01-03T00:00:00Z"
            """;
        var loader = new RiskRegisterLoader();

        // Act
        var register = loader.Parse(yaml);

        // Assert
        register.Should().NotBeNull();
        register.Version.Should().Be("1.0.0");
        register.LastUpdated.Should().Be(DateTimeOffset.Parse("2025-01-03T10:00:00Z"));
        register.Risks.Should().HaveCount(1);
        register.Mitigations.Should().HaveCount(1);

        var risk = register.Risks[0];
        risk.RiskId.Value.Should().Be("RISK-I-001");
        risk.Category.Should().Be(Domain.Risks.RiskCategory.InformationDisclosure);
        risk.Title.Should().Be("Source code exfiltration via LLM");
        risk.Description.Should().Be("Code sent to external LLM API");
        risk.DreadScore.Damage.Should().Be(9);
        risk.DreadScore.Reproducibility.Should().Be(10);
        risk.DreadScore.Exploitability.Should().Be(3);
        risk.DreadScore.AffectedUsers.Should().Be(10);
        risk.DreadScore.Discoverability.Should().Be(7);
        risk.Severity.Should().Be(Domain.Risks.Severity.High);
        risk.Mitigations.Should().HaveCount(1);
        risk.Mitigations[0].Id.Value.Should().Be("MIT-001");
        risk.AttackVectors.Should().Contain("User configures external LLM provider");
        risk.ResidualRisk.Should().Be("Low - requires explicit user configuration");
        risk.Owner.Should().Be("security-team");
        risk.Status.Should().Be(Domain.Risks.RiskStatus.Active);

        var mitigation = register.Mitigations[0];
        mitigation.Id.Value.Should().Be("MIT-001");
        mitigation.Title.Should().Be("LocalOnly mode default");
        mitigation.Description.Should().Be("Deny external LLM APIs by default");
        mitigation.Implementation.Should().Be("ModeMatrix.cs, LlmApiDenylist.cs");
        mitigation.VerificationTest.Should().Be("ModeMatrixTests.LocalOnly_Should_Deny_External_LLM_APIs");
        mitigation.Status.Should().Be(Domain.Risks.MitigationStatus.Implemented);
    }

    [Fact]
    public void Should_Detect_Duplicate_Risk_IDs()
    {
        // Arrange
        var yaml = """
            version: "1.0.0"
            last_updated: "2025-01-03T10:00:00Z"
            risks:
              - id: RISK-I-001
                category: information_disclosure
                title: First risk
                description: First description
                dread:
                  damage: 5
                  reproducibility: 5
                  exploitability: 5
                  affected_users: 5
                  discoverability: 5
                severity: medium
                mitigations: []
                owner: team-a
                status: active
                created: "2025-01-01T00:00:00Z"
                last_review: "2025-01-03T00:00:00Z"
              - id: RISK-I-001
                category: tampering
                title: Duplicate risk
                description: Duplicate description
                dread:
                  damage: 5
                  reproducibility: 5
                  exploitability: 5
                  affected_users: 5
                  discoverability: 5
                severity: medium
                mitigations: []
                owner: team-b
                status: active
                created: "2025-01-01T00:00:00Z"
                last_review: "2025-01-03T00:00:00Z"
            mitigations: []
            """;
        var loader = new RiskRegisterLoader();

        // Act
        Action act = () => loader.Parse(yaml);

        // Assert
        act.Should().Throw<RiskRegisterValidationException>()
            .WithMessage("*Duplicate risk IDs*RISK-I-001*");
    }

    [Fact]
    public void Should_Validate_Mitigation_References_Exist()
    {
        // Arrange
        var yaml = """
            version: "1.0.0"
            last_updated: "2025-01-03T10:00:00Z"
            risks:
              - id: RISK-I-001
                category: information_disclosure
                title: Test risk
                description: Test description
                dread:
                  damage: 5
                  reproducibility: 5
                  exploitability: 5
                  affected_users: 5
                  discoverability: 5
                severity: medium
                mitigations:
                  - MIT-999
                owner: team-a
                status: active
                created: "2025-01-01T00:00:00Z"
                last_review: "2025-01-03T00:00:00Z"
            mitigations: []
            """;
        var loader = new RiskRegisterLoader();

        // Act
        Action act = () => loader.Parse(yaml);

        // Assert
        act.Should().Throw<RiskRegisterValidationException>()
            .WithMessage("*Mitigation reference*MIT-999*not found*");
    }

    [Fact]
    public void Should_Reject_Invalid_YAML_Syntax()
    {
        // Arrange
        var yaml = """
            version: "1.0.0"
            last_updated: "2025-01-03T10:00:00Z"
            risks:
              - id: RISK-I-001
                category: [invalid: unclosed: bracket
            """;
        var loader = new RiskRegisterLoader();

        // Act
        Action act = () => loader.Parse(yaml);

        // Assert
        act.Should().Throw<RiskRegisterParseException>()
            .WithMessage("*YAML*");
    }

    [Fact]
    public void Should_Reject_Missing_Required_Fields()
    {
        // Arrange
        var yaml = """
            version: "1.0.0"
            last_updated: "2025-01-03T10:00:00Z"
            risks:
              - id: RISK-I-001
                category: information_disclosure
            mitigations: []
            """;
        var loader = new RiskRegisterLoader();

        // Act
        Action act = () => loader.Parse(yaml);

        // Assert
        act.Should().Throw<RiskRegisterValidationException>()
            .WithMessage("*required field*");
    }
}
