// <copyright file="ApprovalPolicyTests.cs" company="Acode">
// Copyright (c) Acode. All rights reserved.
// </copyright>

using Acode.Cli.NonInteractive;
using FluentAssertions;

namespace Acode.Cli.Tests.NonInteractive;

/// <summary>
/// Unit tests for approval policies.
/// </summary>
public sealed class ApprovalPolicyTests
{
    /// <summary>
    /// FR-020: Policy "none" MUST reject all approvals.
    /// </summary>
    [Fact]
    public void NonePolicy_Should_Reject_All()
    {
        // Arrange
        var policy = ApprovalPolicyFactory.Create("none");
        var request = new ApprovalRequest(
            ActionType: "delete_file",
            RiskLevel: RiskLevel.Medium,
            Context: new Dictionary<string, object>()
        );

        // Act
        var decision = policy.Evaluate(request);

        // Assert
        decision.Should().Be(ApprovalDecision.Reject, "none policy rejects all approval requests");
    }

    /// <summary>
    /// FR-021: Policy "low-risk" MUST approve low-risk only.
    /// </summary>
    [Fact]
    public void LowRiskPolicy_Should_Approve_Low_Risk()
    {
        // Arrange
        var policy = ApprovalPolicyFactory.Create("low-risk");
        var lowRiskRequest = new ApprovalRequest(
            ActionType: "read_file",
            RiskLevel: RiskLevel.Low,
            Context: new Dictionary<string, object>()
        );

        // Act
        var decision = policy.Evaluate(lowRiskRequest);

        // Assert
        decision
            .Should()
            .Be(ApprovalDecision.Approve, "low-risk policy approves low-risk actions");
    }

    /// <summary>
    /// FR-021: Policy "low-risk" MUST reject high-risk.
    /// </summary>
    /// <param name="riskLevel">The risk level to test.</param>
    [Theory]
    [InlineData(RiskLevel.Medium)]
    [InlineData(RiskLevel.High)]
    [InlineData(RiskLevel.Critical)]
    public void LowRiskPolicy_Should_Reject_HigherRisk(RiskLevel riskLevel)
    {
        // Arrange
        var policy = ApprovalPolicyFactory.Create("low-risk");
        var request = new ApprovalRequest(
            ActionType: "test_action",
            RiskLevel: riskLevel,
            Context: new Dictionary<string, object>()
        );

        // Act
        var decision = policy.Evaluate(request);

        // Assert
        decision
            .Should()
            .Be(ApprovalDecision.Reject, $"low-risk policy rejects {riskLevel} actions");
    }

    /// <summary>
    /// FR-022: Policy "all" MUST approve all actions.
    /// </summary>
    /// <param name="riskLevel">The risk level to test.</param>
    [Theory]
    [InlineData(RiskLevel.Low)]
    [InlineData(RiskLevel.Medium)]
    [InlineData(RiskLevel.High)]
    [InlineData(RiskLevel.Critical)]
    public void AllPolicy_Should_Approve_All(RiskLevel riskLevel)
    {
        // Arrange
        var policy = ApprovalPolicyFactory.Create("all");
        var request = new ApprovalRequest(
            ActionType: "test_action",
            RiskLevel: riskLevel,
            Context: new Dictionary<string, object>()
        );

        // Act
        var decision = policy.Evaluate(request);

        // Assert
        decision.Should().Be(ApprovalDecision.Approve, $"all policy approves {riskLevel} actions");
    }

    /// <summary>
    /// Factory should be case-insensitive.
    /// </summary>
    /// <param name="policyName">The policy name to test.</param>
    [Theory]
    [InlineData("none")]
    [InlineData("None")]
    [InlineData("NONE")]
    public void Factory_Should_Be_CaseInsensitive(string policyName)
    {
        // Act
        var policy = ApprovalPolicyFactory.Create(policyName);

        // Assert
        policy.Name.Should().Be("none");
    }

    /// <summary>
    /// Factory should throw on unknown policy.
    /// </summary>
    [Fact]
    public void Factory_Should_Throw_On_Unknown_Policy()
    {
        // Act
        var act = () => ApprovalPolicyFactory.Create("unknown");

        // Assert
        act.Should().Throw<ArgumentException>().WithMessage("*Unknown approval policy*unknown*");
    }

    /// <summary>
    /// Factory should list valid policy names.
    /// </summary>
    [Fact]
    public void Factory_Should_List_Valid_Policies()
    {
        // Act
        var validNames = ApprovalPolicyFactory.GetValidPolicyNames();

        // Assert
        validNames.Should().Contain("none");
        validNames.Should().Contain("low-risk");
        validNames.Should().Contain("all");
    }

    /// <summary>
    /// Policies should validate null request.
    /// </summary>
    [Fact]
    public void Policies_Should_Throw_On_Null_Request()
    {
        // Arrange
        var nonePolicy = ApprovalPolicyFactory.Create("none");
        var lowRiskPolicy = ApprovalPolicyFactory.Create("low-risk");
        var allPolicy = ApprovalPolicyFactory.Create("all");

        // Act & Assert
        var actNone = () => nonePolicy.Evaluate(null!);
        var actLowRisk = () => lowRiskPolicy.Evaluate(null!);
        var actAll = () => allPolicy.Evaluate(null!);

        actNone.Should().Throw<ArgumentNullException>();
        actLowRisk.Should().Throw<ArgumentNullException>();
        actAll.Should().Throw<ArgumentNullException>();
    }
}
