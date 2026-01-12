using Acode.Application.Security;
using Acode.Domain.Risks;
using FluentAssertions;
using NSubstitute;

namespace Acode.Cli.Tests.Commands;

/// <summary>
/// Tests for SecurityCommand.
/// </summary>
public sealed class SecurityCommandTests
{
    private readonly global::Acode.Cli.Commands.SecurityCommand _command;

    public SecurityCommandTests()
    {
        _command = new global::Acode.Cli.Commands.SecurityCommand();
    }

    [Fact]
    public void ShowStatus_ReturnsSuccess()
    {
        // Arrange
        using var output = new StringWriter();
        Console.SetOut(output);

        // Act
        var result = _command.ShowStatus();

        // Assert
        result.Should().Be(0);
        output.ToString().Should().Contain("Security Status");
    }

    [Fact]
    public void ShowDenylist_ReturnsSuccess()
    {
        // Arrange
        using var output = new StringWriter();
        Console.SetOut(output);

        // Act
        var result = _command.ShowDenylist();

        // Assert
        result.Should().Be(0);
        output.ToString().Should().Contain("Protected Paths Denylist");
    }

    [Fact]
    public void CheckPath_AllowedPath_ReturnsSuccess()
    {
        // Arrange
        using var output = new StringWriter();
        Console.SetOut(output);

        // Act
        var result = _command.CheckPath("src/Program.cs");

        // Assert
        result.Should().Be(0);
        output.ToString().Should().Contain("ALLOWED");
    }

    [Fact]
    public void CheckPath_ProtectedPath_ReturnsFailure()
    {
        // Arrange
        using var output = new StringWriter();
        Console.SetOut(output);

        // Act
        var result = _command.CheckPath("~/.ssh/id_rsa");

        // Assert
        result.Should().Be(1);
        output.ToString().Should().Contain("BLOCKED");
    }

    [Fact]
    public async Task ShowRisksAsync_WithNoRiskRegister_ReturnsError()
    {
        // Arrange
        using var output = new StringWriter();
        Console.SetOut(output);
        var command = new global::Acode.Cli.Commands.SecurityCommand(riskRegister: null);

        // Act
        var result = await command.ShowRisksAsync();

        // Assert
        result.Should().Be(1);
        output.ToString().Should().Contain("Risk register not available");
    }

    [Fact]
    public async Task ShowRisksAsync_WithRiskRegister_DisplaysAllRisks()
    {
        // Arrange
        using var output = new StringWriter();
        Console.SetOut(output);

        var riskRegister = CreateTestRiskRegister();
        var command = new global::Acode.Cli.Commands.SecurityCommand(riskRegister);

        // Act
        var result = await command.ShowRisksAsync();

        // Assert
        result.Should().Be(0);
        output.ToString().Should().Contain("Risk Register");
        output.ToString().Should().Contain("RISK-I-001");
        output.ToString().Should().Contain("Total Risks: 2");
    }

    [Fact]
    public async Task ShowRisksAsync_FilterByCategory_DisplaysOnlyMatchingRisks()
    {
        // Arrange
        using var output = new StringWriter();
        Console.SetOut(output);

        var riskRegister = CreateTestRiskRegister();
        var command = new global::Acode.Cli.Commands.SecurityCommand(riskRegister);

        // Act
        var result = await command.ShowRisksAsync(RiskCategory.InformationDisclosure);

        // Assert
        result.Should().Be(0);
        output.ToString().Should().Contain("RISK-I-001");
        output.ToString().Should().NotContain("RISK-T-001");
    }

    [Fact]
    public async Task ShowRiskDetailAsync_WithNoRiskRegister_ReturnsError()
    {
        // Arrange
        using var output = new StringWriter();
        Console.SetOut(output);
        var command = new global::Acode.Cli.Commands.SecurityCommand(riskRegister: null);

        // Act
        var result = await command.ShowRiskDetailAsync("RISK-I-001");

        // Assert
        result.Should().Be(1);
        output.ToString().Should().Contain("Risk register not available");
    }

    [Fact]
    public async Task ShowRiskDetailAsync_RiskExists_DisplaysFullDetails()
    {
        // Arrange
        using var output = new StringWriter();
        Console.SetOut(output);

        var riskRegister = CreateTestRiskRegister();
        var command = new global::Acode.Cli.Commands.SecurityCommand(riskRegister);

        // Act
        var result = await command.ShowRiskDetailAsync("RISK-I-001");

        // Assert
        result.Should().Be(0);
        output.ToString().Should().Contain("RISK-I-001");
        output.ToString().Should().Contain("Test risk");
        output.ToString().Should().Contain("DREAD Score");
        output.ToString().Should().Contain("Damage:");
        output.ToString().Should().Contain("MIT-001");
    }

    [Fact]
    public async Task ShowRiskDetailAsync_RiskNotFound_ReturnsError()
    {
        // Arrange
        using var output = new StringWriter();
        Console.SetOut(output);

        var riskRegister = CreateTestRiskRegister();
        var command = new global::Acode.Cli.Commands.SecurityCommand(riskRegister);

        // Act
        var result = await command.ShowRiskDetailAsync("RISK-I-999");

        // Assert
        result.Should().Be(1);
        output.ToString().Should().Contain("Risk not found");
    }

    [Fact]
    public async Task ShowMitigationsAsync_WithNoRiskRegister_ReturnsError()
    {
        // Arrange
        using var output = new StringWriter();
        Console.SetOut(output);
        var command = new global::Acode.Cli.Commands.SecurityCommand(riskRegister: null);

        // Act
        var result = await command.ShowMitigationsAsync();

        // Assert
        result.Should().Be(1);
        output.ToString().Should().Contain("Risk register not available");
    }

    [Fact]
    public async Task ShowMitigationsAsync_WithRiskRegister_DisplaysAllMitigations()
    {
        // Arrange
        using var output = new StringWriter();
        Console.SetOut(output);

        var riskRegister = CreateTestRiskRegister();
        var command = new global::Acode.Cli.Commands.SecurityCommand(riskRegister);

        // Act
        var result = await command.ShowMitigationsAsync();

        // Assert
        result.Should().Be(0);
        output.ToString().Should().Contain("Mitigations");
        output.ToString().Should().Contain("MIT-001");
        output.ToString().Should().Contain("Test mitigation");
        output.ToString().Should().Contain("Status Summary");
    }

    [Fact]
    public async Task VerifyMitigationsAsync_WithNoRiskRegister_ReturnsError()
    {
        // Arrange
        using var output = new StringWriter();
        Console.SetOut(output);
        var command = new global::Acode.Cli.Commands.SecurityCommand(riskRegister: null);

        // Act
        var result = await command.VerifyMitigationsAsync();

        // Assert
        result.Should().Be(1);
        output.ToString().Should().Contain("Risk register not available");
    }

    [Fact]
    public async Task VerifyMitigationsAsync_AllImplemented_ReturnsSuccess()
    {
        // Arrange
        using var output = new StringWriter();
        Console.SetOut(output);

        var riskRegister = CreateTestRiskRegister();
        var command = new global::Acode.Cli.Commands.SecurityCommand(riskRegister);

        // Act
        var result = await command.VerifyMitigationsAsync();

        // Assert
        result.Should().Be(0);
        output.ToString().Should().Contain("Mitigation Verification Report");
        output.ToString().Should().Contain("Implemented:");
        output.ToString().Should().Contain("All mitigations verified");
    }

    [Fact]
    public async Task VerifyMitigationsAsync_WithPendingMitigations_ReturnsFailure()
    {
        // Arrange
        using var output = new StringWriter();
        Console.SetOut(output);

        var riskRegister = CreateTestRiskRegisterWithPending();
        var command = new global::Acode.Cli.Commands.SecurityCommand(riskRegister);

        // Act
        var result = await command.VerifyMitigationsAsync();

        // Assert
        result.Should().Be(1);
        output.ToString().Should().Contain("need attention");
    }

    private static IRiskRegister CreateTestRiskRegister()
    {
        var riskRegister = Substitute.For<IRiskRegister>();

        riskRegister.Version.Returns("1.0.0");
        riskRegister.LastUpdated.Returns(DateTimeOffset.Parse("2025-01-01T00:00:00Z"));

        var mitigation1 = new Mitigation
        {
            Id = new MitigationId("MIT-001"),
            Title = "Test mitigation",
            Description = "Test description",
            Implementation = "Test implementation",
            VerificationTest = "Test verification",
            Status = MitigationStatus.Implemented,
            LastVerified = DateTimeOffset.Parse("2025-01-01T00:00:00Z"),
        };

        var risk1 = new Risk
        {
            RiskId = new RiskId("RISK-I-001"),
            Title = "Test risk",
            Description = "Test description",
            Category = RiskCategory.InformationDisclosure,
            DreadScore = new DreadScore(8, 7, 6, 5, 4),
            Mitigations = new List<Mitigation> { mitigation1 },
            AttackVectors = new List<string> { "Test vector" },
            ResidualRisk = "Low",
            Owner = "test-team",
            Status = RiskStatus.Active,
            Created = DateTimeOffset.Parse("2025-01-01T00:00:00Z"),
            LastReview = DateTimeOffset.Parse("2025-01-01T00:00:00Z"),
        };

        var risk2 = new Risk
        {
            RiskId = new RiskId("RISK-T-001"),
            Title = "Tampering risk",
            Description = "Test description",
            Category = RiskCategory.Tampering,
            DreadScore = new DreadScore(5, 5, 5, 5, 5),
            Mitigations = new List<Mitigation>(),
            Owner = "test-team",
            Status = RiskStatus.Active,
            Created = DateTimeOffset.Parse("2025-01-01T00:00:00Z"),
            LastReview = DateTimeOffset.Parse("2025-01-01T00:00:00Z"),
        };

        var allRisks = new List<Risk> { risk1, risk2 };
        var allMitigations = new List<Mitigation> { mitigation1 };

        riskRegister.GetAllRisksAsync(Arg.Any<CancellationToken>()).Returns(allRisks);
        riskRegister.GetRiskAsync(new RiskId("RISK-I-001"), Arg.Any<CancellationToken>()).Returns(risk1);
        riskRegister.GetRiskAsync(new RiskId("RISK-I-999"), Arg.Any<CancellationToken>()).Returns((Risk?)null);
        riskRegister.GetRisksByCategoryAsync(RiskCategory.InformationDisclosure, Arg.Any<CancellationToken>())
            .Returns(new List<Risk> { risk1 });
        riskRegister.GetAllMitigationsAsync(Arg.Any<CancellationToken>()).Returns(allMitigations);
        riskRegister.GetMitigationsForRiskAsync(new RiskId("RISK-I-001"), Arg.Any<CancellationToken>())
            .Returns(new List<Mitigation> { mitigation1 });

        return riskRegister;
    }

    private static IRiskRegister CreateTestRiskRegisterWithPending()
    {
        var riskRegister = Substitute.For<IRiskRegister>();

        riskRegister.Version.Returns("1.0.0");
        riskRegister.LastUpdated.Returns(DateTimeOffset.Parse("2025-01-01T00:00:00Z"));

        var mitigation1 = new Mitigation
        {
            Id = new MitigationId("MIT-001"),
            Title = "Implemented mitigation",
            Description = "Test description",
            Implementation = "Test implementation",
            VerificationTest = "Test verification",
            Status = MitigationStatus.Implemented,
            LastVerified = DateTimeOffset.Parse("2025-01-01T00:00:00Z"),
        };

        var mitigation2 = new Mitigation
        {
            Id = new MitigationId("MIT-002"),
            Title = "Pending mitigation",
            Description = "Test description",
            Implementation = "Test implementation",
            Status = MitigationStatus.Pending,
            LastVerified = DateTimeOffset.Parse("2025-01-01T00:00:00Z"),
        };

        var allMitigations = new List<Mitigation> { mitigation1, mitigation2 };

        riskRegister.GetAllMitigationsAsync(Arg.Any<CancellationToken>()).Returns(allMitigations);

        return riskRegister;
    }
}
