// tests/Acode.Infrastructure.Tests/Persistence/Migrations/MigrationValidatorTests.cs
#pragma warning disable CA2007 // ConfigureAwait not required in test code

namespace Acode.Infrastructure.Tests.Persistence.Migrations;

using Acode.Application.Database;
using Acode.Infrastructure.Persistence.Migrations;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

/// <summary>
/// Tests for MigrationValidator class.
/// Verifies checksum validation, version gap detection, and pending migration identification.
/// </summary>
public sealed class MigrationValidatorTests
{
    private readonly MigrationValidator _sut;
    private readonly ILogger<MigrationValidator> _loggerMock;

    public MigrationValidatorTests()
    {
        _loggerMock = Substitute.For<ILogger<MigrationValidator>>();
        _sut = new MigrationValidator(_loggerMock);
    }

    [Fact]
    public async Task ValidateAsync_WithNoAppliedMigrations_ReturnsAllAsPending()
    {
        // Arrange
        var discovered = new[]
        {
            new MigrationFile
            {
                Version = "001",
                UpContent = "CREATE TABLE test;",
                Checksum = "abc123",
                Source = MigrationSource.Embedded
            },
            new MigrationFile
            {
                Version = "002",
                UpContent = "ALTER TABLE test;",
                Checksum = "def456",
                Source = MigrationSource.Embedded
            }
        };
        var applied = Array.Empty<AppliedMigration>();

        // Act
        var result = await _sut.ValidateAsync(discovered, applied, CancellationToken.None);

        // Assert
        result.PendingMigrations.Should().HaveCount(2);
        result.PendingMigrations.Select(m => m.Version).Should().BeEquivalentTo(new[] { "001", "002" });
        result.ChecksumMismatches.Should().BeEmpty();
        result.VersionGaps.Should().BeEmpty();
    }

    [Fact]
    public async Task ValidateAsync_WithMatchingChecksums_ReturnsNoPending()
    {
        // Arrange
        var discovered = new[]
        {
            new MigrationFile
            {
                Version = "001",
                UpContent = "CREATE TABLE test;",
                Checksum = "abc123",
                Source = MigrationSource.Embedded
            }
        };
        var applied = new[]
        {
            new AppliedMigration
            {
                Version = "001",
                Checksum = "abc123",
                AppliedAt = DateTime.UtcNow,
                Duration = TimeSpan.FromSeconds(1),
                Status = MigrationStatus.Applied
            }
        };

        // Act
        var result = await _sut.ValidateAsync(discovered, applied, CancellationToken.None);

        // Assert
        result.PendingMigrations.Should().BeEmpty();
        result.ChecksumMismatches.Should().BeEmpty();
        result.VersionGaps.Should().BeEmpty();
    }

    [Fact]
    public async Task ValidateAsync_WithChecksumMismatch_DetectsTampering()
    {
        // Arrange
        var discovered = new[]
        {
            new MigrationFile
            {
                Version = "001",
                UpContent = "CREATE TABLE test_modified;",
                Checksum = "xyz789",
                Source = MigrationSource.File
            }
        };
        var applied = new[]
        {
            new AppliedMigration
            {
                Version = "001",
                Checksum = "abc123",
                AppliedAt = DateTime.UtcNow,
                Duration = TimeSpan.FromSeconds(1),
                Status = MigrationStatus.Applied
            }
        };

        // Act
        var result = await _sut.ValidateAsync(discovered, applied, CancellationToken.None);

        // Assert
        result.ChecksumMismatches.Should().HaveCount(1);
        var mismatch = result.ChecksumMismatches.First();
        mismatch.Version.Should().Be("001");
        mismatch.ExpectedChecksum.Should().Be("abc123");
        mismatch.ActualChecksum.Should().Be("xyz789");
    }

    [Fact]
    public async Task ValidateAsync_WithChecksumMismatch_LogsWarning()
    {
        // Arrange
        var discovered = new[]
        {
            new MigrationFile
            {
                Version = "001",
                UpContent = "CREATE TABLE test_modified;",
                Checksum = "xyz789",
                Source = MigrationSource.File
            }
        };
        var applied = new[]
        {
            new AppliedMigration
            {
                Version = "001",
                Checksum = "abc123",
                AppliedAt = DateTime.UtcNow,
                Duration = TimeSpan.FromSeconds(1),
                Status = MigrationStatus.Applied
            }
        };

        // Act
        await _sut.ValidateAsync(discovered, applied, CancellationToken.None);

        // Assert
        _loggerMock.Received(1).Log(
            LogLevel.Warning,
            Arg.Any<EventId>(),
            Arg.Is<object>(v => v.ToString()!.Contains("001") && v.ToString()!.Contains("checksum")),
            null,
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public async Task ValidateAsync_WithVersionGap_DetectsMissingMigration()
    {
        // Arrange
        var discovered = new[]
        {
            new MigrationFile
            {
                Version = "001",
                UpContent = "CREATE TABLE test;",
                Checksum = "abc123",
                Source = MigrationSource.Embedded
            },
            new MigrationFile
            {
                Version = "003",
                UpContent = "ALTER TABLE test;",
                Checksum = "ghi789",
                Source = MigrationSource.Embedded
            }
        };
        var applied = new[]
        {
            new AppliedMigration
            {
                Version = "001",
                Checksum = "abc123",
                AppliedAt = DateTime.UtcNow,
                Duration = TimeSpan.FromSeconds(1),
                Status = MigrationStatus.Applied
            }
        };

        // Act
        var result = await _sut.ValidateAsync(discovered, applied, CancellationToken.None);

        // Assert
        result.VersionGaps.Should().HaveCount(1);
        result.VersionGaps.First().MissingVersion.Should().Be("002");
        result.VersionGaps.First().BeforeVersion.Should().Be("001");
        result.VersionGaps.First().AfterVersion.Should().Be("003");
    }

    [Fact]
    public async Task ValidateAsync_WithMixedState_IdentifiesCorrectly()
    {
        // Arrange
        var discovered = new[]
        {
            new MigrationFile
            {
                Version = "001",
                UpContent = "CREATE TABLE test;",
                Checksum = "abc123",
                Source = MigrationSource.Embedded
            },
            new MigrationFile
            {
                Version = "002",
                UpContent = "ALTER TABLE test;",
                Checksum = "def456",
                Source = MigrationSource.Embedded
            },
            new MigrationFile
            {
                Version = "003",
                UpContent = "CREATE INDEX;",
                Checksum = "ghi789",
                Source = MigrationSource.File
            }
        };
        var applied = new[]
        {
            new AppliedMigration
            {
                Version = "001",
                Checksum = "abc123",
                AppliedAt = DateTime.UtcNow,
                Duration = TimeSpan.FromSeconds(1),
                Status = MigrationStatus.Applied
            }
        };

        // Act
        var result = await _sut.ValidateAsync(discovered, applied, CancellationToken.None);

        // Assert
        result.PendingMigrations.Should().HaveCount(2);
        result.PendingMigrations.Select(m => m.Version).Should().BeEquivalentTo(new[] { "002", "003" });
        result.ChecksumMismatches.Should().BeEmpty();
        result.VersionGaps.Should().BeEmpty();
    }
}
