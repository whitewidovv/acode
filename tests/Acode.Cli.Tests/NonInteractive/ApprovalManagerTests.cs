// <copyright file="ApprovalManagerTests.cs" company="Acode">
// Copyright (c) Acode. All rights reserved.
// </copyright>

using Acode.Cli.NonInteractive;
using FluentAssertions;

namespace Acode.Cli.Tests.NonInteractive;

/// <summary>
/// Unit tests for <see cref="ApprovalManager"/>.
/// </summary>
public sealed class ApprovalManagerTests
{
    /// <summary>
    /// FR-017: --yes MUST auto-approve all prompts.
    /// </summary>
    [Fact]
    public void Should_Honor_Yes_Flag()
    {
        // Arrange
        var options = new NonInteractiveOptions { Yes = true };
        var manager = new ApprovalManager(options);

        var criticalRequest = new ApprovalRequest(
            ActionType: "git_push",
            RiskLevel: RiskLevel.Critical,
            Context: new Dictionary<string, object>()
        );

        // Act
        var decision = manager.RequestApproval(criticalRequest);

        // Assert
        decision.Should().Be(ApprovalDecision.Approve, "--yes flag auto-approves all actions");
    }

    /// <summary>
    /// FR-018: --no-approve MUST reject all prompts.
    /// </summary>
    [Fact]
    public void Should_Honor_NoApprove_Flag()
    {
        // Arrange
        var options = new NonInteractiveOptions { NoApprove = true };
        var manager = new ApprovalManager(options);

        var lowRiskRequest = new ApprovalRequest(
            ActionType: "read_file",
            RiskLevel: RiskLevel.Low,
            Context: new Dictionary<string, object>()
        );

        // Act
        var decision = manager.RequestApproval(lowRiskRequest);

        // Assert
        decision.Should().Be(ApprovalDecision.Reject, "--no-approve flag rejects all actions");
    }

    /// <summary>
    /// FR-024: Missing approval in non-interactive MUST fail.
    /// </summary>
    [Fact]
    public void Should_Throw_When_Approval_Required_In_NonInteractive_Without_Policy()
    {
        // Arrange
        var options = new NonInteractiveOptions { NonInteractive = true };
        var manager = new ApprovalManager(options, approvalPolicy: null);

        var request = new ApprovalRequest(
            ActionType: "delete_file",
            RiskLevel: RiskLevel.High,
            Context: new Dictionary<string, object>()
        );

        // Act
        var act = () => manager.RequestApproval(request);

        // Assert
        act.Should()
            .Throw<ApprovalRequiredException>()
            .WithMessage("*requires approval but running in non-interactive mode*");
    }

    /// <summary>
    /// Policy decisions should be applied.
    /// </summary>
    [Fact]
    public void Should_Apply_Policy_Decision()
    {
        // Arrange
        var options = new NonInteractiveOptions
        {
            NonInteractive = true,
            ApprovalPolicy = "low-risk",
        };
        var manager = new ApprovalManager(options);

        var lowRiskRequest = new ApprovalRequest(
            ActionType: "read_file",
            RiskLevel: RiskLevel.Low,
            Context: new Dictionary<string, object>()
        );

        var highRiskRequest = new ApprovalRequest(
            ActionType: "delete_file",
            RiskLevel: RiskLevel.High,
            Context: new Dictionary<string, object>()
        );

        // Act
        var lowRiskDecision = manager.RequestApproval(lowRiskRequest);
        var highRiskDecision = manager.RequestApproval(highRiskRequest);

        // Assert
        lowRiskDecision.Should().Be(ApprovalDecision.Approve);
        highRiskDecision.Should().Be(ApprovalDecision.Reject);
    }

    /// <summary>
    /// --yes takes precedence over policy.
    /// </summary>
    [Fact]
    public void YesFlag_Should_Override_Policy()
    {
        // Arrange
        var options = new NonInteractiveOptions { Yes = true, ApprovalPolicy = "none" };
        var manager = new ApprovalManager(options);

        var request = new ApprovalRequest(
            ActionType: "delete_file",
            RiskLevel: RiskLevel.Critical,
            Context: new Dictionary<string, object>()
        );

        // Act
        var decision = manager.RequestApproval(request);

        // Assert
        decision.Should().Be(ApprovalDecision.Approve, "--yes flag takes precedence over policy");
    }

    /// <summary>
    /// Should validate null options.
    /// </summary>
    [Fact]
    public void Should_Throw_On_Null_Options()
    {
        // Act
        var act = () => new ApprovalManager(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("options");
    }

    /// <summary>
    /// Should validate null request.
    /// </summary>
    [Fact]
    public void Should_Throw_On_Null_Request()
    {
        // Arrange
        var options = new NonInteractiveOptions { Yes = true };
        var manager = new ApprovalManager(options);

        // Act
        var act = () => manager.RequestApproval(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("request");
    }

    /// <summary>
    /// Interactive mode should return RequireExplicit when no policy.
    /// </summary>
    [Fact]
    public void Should_Return_RequireExplicit_In_Interactive_Mode()
    {
        // Arrange
        var options = new NonInteractiveOptions { NonInteractive = false };
        var manager = new ApprovalManager(options, approvalPolicy: null);

        var request = new ApprovalRequest(
            ActionType: "delete_file",
            RiskLevel: RiskLevel.High,
            Context: new Dictionary<string, object>()
        );

        // Act
        var decision = manager.RequestApproval(request);

        // Assert
        decision.Should().Be(ApprovalDecision.RequireExplicit);
    }
}
