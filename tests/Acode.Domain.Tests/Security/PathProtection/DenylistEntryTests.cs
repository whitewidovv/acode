namespace Acode.Domain.Tests.Security.PathProtection;

using Acode.Domain.Security.PathProtection;
using FluentAssertions;

public class DenylistEntryTests
{
    [Fact]
    public void DenylistEntry_ShouldBeCreatableWithRequiredFields()
    {
        // Arrange & Act
        var entry = new DenylistEntry
        {
            Pattern = "~/.ssh/",
            Reason = "SSH directory containing private keys",
            RiskId = "RISK-E-003",
            Category = PathCategory.SshKeys,
            Platforms = new[] { Platform.Linux, Platform.MacOS },
            IsDefault = true
        };

        // Assert
        entry.Pattern.Should().Be("~/.ssh/");
        entry.Reason.Should().Be("SSH directory containing private keys");
        entry.RiskId.Should().Be("RISK-E-003");
        entry.Category.Should().Be(PathCategory.SshKeys);
        entry.Platforms.Should().BeEquivalentTo(new[] { Platform.Linux, Platform.MacOS });
        entry.IsDefault.Should().BeTrue();
    }

    [Fact]
    public void DenylistEntry_IsDefault_ShouldDefaultToTrue()
    {
        // Arrange & Act
        var entry = new DenylistEntry
        {
            Pattern = ".env",
            Reason = "Environment file may contain secrets",
            RiskId = "RISK-I-002",
            Category = PathCategory.EnvironmentFiles,
            Platforms = new[] { Platform.All }
        };

        // Assert - IsDefault should be true by default
        entry.IsDefault.Should().BeTrue();
    }

    [Fact]
    public void DenylistEntry_ShouldBeImmutable()
    {
        // Arrange
        var entry = new DenylistEntry
        {
            Pattern = "*.pem",
            Reason = "Certificate files",
            RiskId = "RISK-I-003",
            Category = PathCategory.SecretFiles,
            Platforms = new[] { Platform.All }
        };

        // Act & Assert
        var modified = entry with { Reason = "Modified reason" };

        entry.Reason.Should().Be("Certificate files");
        modified.Reason.Should().Be("Modified reason");
        modified.Pattern.Should().Be(entry.Pattern);
    }

    [Fact]
    public void DenylistEntry_ShouldSupportValueEqualityForPrimitiveFields()
    {
        // Arrange
        var platforms = new[] { Platform.All };
        var entry1 = new DenylistEntry
        {
            Pattern = "~/.aws/",
            Reason = "AWS credentials",
            RiskId = "RISK-I-003",
            Category = PathCategory.CloudCredentials,
            Platforms = platforms
        };

        var entry2 = new DenylistEntry
        {
            Pattern = "~/.aws/",
            Reason = "AWS credentials",
            RiskId = "RISK-I-003",
            Category = PathCategory.CloudCredentials,
            Platforms = platforms // Same reference
        };

        // Act & Assert - Records support value equality for same references
        entry1.Should().Be(entry2);
        (entry1 == entry2).Should().BeTrue();
    }

    [Fact]
    public void DenylistEntry_WithDifferentPattern_ShouldNotBeEqual()
    {
        // Arrange
        var entry1 = new DenylistEntry
        {
            Pattern = "~/.ssh/id_rsa",
            Reason = "SSH private key",
            RiskId = "RISK-E-003",
            Category = PathCategory.SshKeys,
            Platforms = new[] { Platform.All }
        };

        var entry2 = new DenylistEntry
        {
            Pattern = "~/.ssh/id_ed25519",
            Reason = "SSH private key",
            RiskId = "RISK-E-003",
            Category = PathCategory.SshKeys,
            Platforms = new[] { Platform.All }
        };

        // Act & Assert
        entry1.Should().NotBe(entry2);
    }

    [Fact]
    public void DenylistEntry_UserDefined_ShouldAllowIsDefaultFalse()
    {
        // Arrange & Act
        var entry = new DenylistEntry
        {
            Pattern = "company-secrets/",
            Reason = "Internal documentation",
            RiskId = "RISK-USER-001",
            Category = PathCategory.UserDefined,
            Platforms = new[] { Platform.All },
            IsDefault = false
        };

        // Assert
        entry.IsDefault.Should().BeFalse();
        entry.Category.Should().Be(PathCategory.UserDefined);
    }
}
