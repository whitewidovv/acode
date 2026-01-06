using Acode.Application.Security;
using Acode.Domain.Security.PathProtection;
using FluentAssertions;

namespace Acode.Application.Tests.Security;

/// <summary>
/// Tests for IProtectedPathValidator and PathValidationResult.
/// </summary>
public sealed class ProtectedPathValidatorTests
{
    [Fact]
    public void PathValidationResult_Allowed_SetsIsProtectedFalse()
    {
        // Act
        var result = PathValidationResult.Allowed();

        // Assert
        result.IsProtected.Should().BeFalse();
        result.MatchedPattern.Should().BeNull();
        result.Reason.Should().BeNull();
        result.RiskId.Should().BeNull();
        result.Category.Should().BeNull();
    }

    [Fact]
    public void PathValidationResult_Blocked_SetsIsProtectedTrue()
    {
        // Arrange
        var entry = new DenylistEntry
        {
            Pattern = "~/.ssh/",
            Reason = "SSH directory",
            RiskId = "RISK-I-003",
            Category = PathCategory.SshKeys,
            Platforms = new[] { Platform.Linux }
        };

        // Act
        var result = PathValidationResult.Blocked(entry);

        // Assert
        result.IsProtected.Should().BeTrue();
        result.MatchedPattern.Should().Be("~/.ssh/");
        result.Reason.Should().Be("SSH directory");
        result.RiskId.Should().Be("RISK-I-003");
        result.Category.Should().Be(PathCategory.SshKeys);
    }

    [Fact]
    public void PathValidationResult_Record_SupportsValueEquality()
    {
        // Arrange
        var result1 = PathValidationResult.Allowed();
        var result2 = PathValidationResult.Allowed();

        // Act & Assert
        result1.Should().Be(result2);
    }

    [Fact]
    public void FileOperation_HasExpectedValues()
    {
        // Assert
        Enum.IsDefined(typeof(FileOperation), FileOperation.Read).Should().BeTrue();
        Enum.IsDefined(typeof(FileOperation), FileOperation.Write).Should().BeTrue();
        Enum.IsDefined(typeof(FileOperation), FileOperation.Delete).Should().BeTrue();
        Enum.IsDefined(typeof(FileOperation), FileOperation.List).Should().BeTrue();
    }
}
