using Acode.Domain.Modes;
using FluentAssertions;
using Xunit;

namespace Acode.Domain.Tests.Modes;

/// <summary>
/// Tests for the Permission enum.
/// Verifies permission levels per Task 001.a mode matrix specification.
/// </summary>
public class PermissionTests
{
    [Fact]
    public void Permission_ShouldHaveAllowedValue()
    {
        // Arrange & Act
        var allowed = Permission.Allowed;

        // Assert
        allowed.Should().BeDefined("Permission.Allowed is required per mode matrix");
    }

    [Fact]
    public void Permission_ShouldHaveDeniedValue()
    {
        // Arrange & Act
        var denied = Permission.Denied;

        // Assert
        denied.Should().BeDefined("Permission.Denied is required per mode matrix");
    }

    [Fact]
    public void Permission_ShouldHaveConditionalOnConsentValue()
    {
        // Arrange & Act
        var conditional = Permission.ConditionalOnConsent;

        // Assert
        conditional.Should().BeDefined(
            "Burst mode requires consent-based conditional permissions");
    }

    [Fact]
    public void Permission_ShouldHaveConditionalOnConfigValue()
    {
        // Arrange & Act
        var conditional = Permission.ConditionalOnConfig;

        // Assert
        conditional.Should().BeDefined(
            "Some capabilities require configuration to enable");
    }

    [Fact]
    public void Permission_ShouldHaveLimitedScopeValue()
    {
        // Arrange & Act
        var limited = Permission.LimitedScope;

        // Assert
        limited.Should().BeDefined(
            "Some capabilities are allowed but with limited scope");
    }

    [Fact]
    public void Permission_ShouldHaveAtLeastFiveValues()
    {
        // Arrange & Act
        var values = Enum.GetValues<Permission>();

        // Assert
        values.Should().HaveCountGreaterOrEqualTo(
            5,
            "mode matrix uses at least 5 permission levels");
    }

    [Fact]
    public void Permission_ToString_ShouldReturnMeaningfulNames()
    {
        // Arrange & Act & Assert
        Permission.Allowed.ToString().Should().Be("Allowed");
        Permission.Denied.ToString().Should().Be("Denied");
        Permission.ConditionalOnConsent.ToString().Should().Be("ConditionalOnConsent");
        Permission.ConditionalOnConfig.ToString().Should().Be("ConditionalOnConfig");
        Permission.LimitedScope.ToString().Should().Be("LimitedScope");
    }
}
