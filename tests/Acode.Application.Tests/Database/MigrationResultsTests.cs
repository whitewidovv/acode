// tests/Acode.Application.Tests/Database/MigrationResultsTests.cs
namespace Acode.Application.Tests.Database;

using Acode.Application.Database;
using FluentAssertions;
using Xunit;

/// <summary>
/// Tests for migration result records.
/// Verifies immutability and required properties for all migration result types.
/// </summary>
public sealed class MigrationResultsTests
{
    [Fact]
    public void MigrationStatusReport_ShouldRequireAllRequiredProperties()
    {
        // Arrange & Act
        var report = new MigrationStatusReport
        {
            CurrentVersion = "005",
            AppliedMigrations = new List<AppliedMigration>(),
            PendingMigrations = new List<MigrationFile>(),
            DatabaseProvider = "SQLite",
            ChecksumsValid = true
        };

        // Assert
        report.CurrentVersion.Should().Be("005");
        report.AppliedMigrations.Should().BeEmpty();
        report.PendingMigrations.Should().BeEmpty();
        report.DatabaseProvider.Should().Be("SQLite");
        report.ChecksumsValid.Should().BeTrue();
        report.ChecksumWarnings.Should().BeNull();
    }

    [Fact]
    public void MigrateResult_ShouldRequireAllRequiredProperties()
    {
        // Arrange & Act
        var result = new MigrateResult
        {
            Success = true,
            AppliedCount = 3,
            TotalDuration = TimeSpan.FromSeconds(5)
        };

        // Assert
        result.Success.Should().BeTrue();
        result.AppliedCount.Should().Be(3);
        result.TotalDuration.Should().Be(TimeSpan.FromSeconds(5));
        result.AppliedMigrations.Should().BeNull();
        result.WouldApply.Should().BeNull();
        result.ErrorMessage.Should().BeNull();
        result.ErrorCode.Should().BeNull();
    }

    [Fact]
    public void MigrateResult_ShouldSupportOptionalProperties()
    {
        // Arrange & Act
        var result = new MigrateResult
        {
            Success = false,
            AppliedCount = 0,
            TotalDuration = TimeSpan.Zero,
            ErrorMessage = "Connection failed",
            ErrorCode = "ACODE-MIG-007"
        };

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Be("Connection failed");
        result.ErrorCode.Should().Be("ACODE-MIG-007");
    }

    [Fact]
    public void RollbackResult_ShouldRequireAllRequiredProperties()
    {
        // Arrange & Act
        var result = new RollbackResult
        {
            Success = true,
            RolledBackCount = 2,
            TotalDuration = TimeSpan.FromSeconds(3)
        };

        // Assert
        result.Success.Should().BeTrue();
        result.RolledBackCount.Should().Be(2);
        result.TotalDuration.Should().Be(TimeSpan.FromSeconds(3));
        result.CurrentVersion.Should().BeNull();
        result.RolledBackVersions.Should().BeNull();
        result.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public void RollbackResult_ShouldSupportOptionalProperties()
    {
        // Arrange & Act
        var result = new RollbackResult
        {
            Success = true,
            RolledBackCount = 2,
            TotalDuration = TimeSpan.FromSeconds(3),
            CurrentVersion = "003",
            RolledBackVersions = new List<string> { "005", "004" }
        };

        // Assert
        result.CurrentVersion.Should().Be("003");
        result.RolledBackVersions.Should().HaveCount(2);
        result.RolledBackVersions.Should().Contain(new[] { "005", "004" });
    }

    [Fact]
    public void CreateResult_ShouldRequireAllRequiredProperties()
    {
        // Arrange & Act
        var result = new CreateResult
        {
            Success = true,
            Version = "006",
            UpFilePath = "/migrations/006-up.sql",
            DownFilePath = "/migrations/006-down.sql"
        };

        // Assert
        result.Success.Should().BeTrue();
        result.Version.Should().Be("006");
        result.UpFilePath.Should().Be("/migrations/006-up.sql");
        result.DownFilePath.Should().Be("/migrations/006-down.sql");
    }

    [Fact]
    public void ValidationResult_ShouldRequireAllRequiredProperties()
    {
        // Arrange & Act
        var result = new ValidationResult
        {
            IsValid = true,
            Mismatches = new List<ChecksumMismatch>()
        };

        // Assert
        result.IsValid.Should().BeTrue();
        result.Mismatches.Should().BeEmpty();
    }

    [Fact]
    public void ValidationResult_ShouldSupportMismatches()
    {
        // Arrange
        var mismatch = new ChecksumMismatch(
            "003",
            "abc123",
            "def456",
            DateTime.UtcNow);

        // Act
        var result = new ValidationResult
        {
            IsValid = false,
            Mismatches = new List<ChecksumMismatch> { mismatch }
        };

        // Assert
        result.IsValid.Should().BeFalse();
        result.Mismatches.Should().HaveCount(1);
        result.Mismatches[0].Version.Should().Be("003");
        result.Mismatches[0].ExpectedChecksum.Should().Be("abc123");
        result.Mismatches[0].ActualChecksum.Should().Be("def456");
    }

    [Fact]
    public void ChecksumMismatch_ShouldInitializeAllProperties()
    {
        // Arrange
        var appliedAt = DateTime.UtcNow;

        // Act
        var mismatch = new ChecksumMismatch("005", "hash1", "hash2", appliedAt);

        // Assert
        mismatch.Version.Should().Be("005");
        mismatch.ExpectedChecksum.Should().Be("hash1");
        mismatch.ActualChecksum.Should().Be("hash2");
        mismatch.AppliedAt.Should().Be(appliedAt);
    }

    [Fact]
    public void LockInfo_ShouldInitializeAllProperties()
    {
        // Arrange
        var acquiredAt = DateTime.UtcNow;

        // Act
        var lockInfo = new LockInfo("lock-123", "process-456", acquiredAt, "machine-01");

        // Assert
        lockInfo.LockId.Should().Be("lock-123");
        lockInfo.HolderId.Should().Be("process-456");
        lockInfo.AcquiredAt.Should().Be(acquiredAt);
        lockInfo.MachineName.Should().Be("machine-01");
    }

    [Fact]
    public void LockInfo_ShouldAllowNullMachineName()
    {
        // Arrange
        var acquiredAt = DateTime.UtcNow;

        // Act
        var lockInfo = new LockInfo("lock-123", "process-456", acquiredAt, null);

        // Assert
        lockInfo.MachineName.Should().BeNull();
    }
}
