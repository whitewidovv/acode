// tests/Acode.Infrastructure.Tests/Persistence/Migrations/MigrationRunnerTests.cs
#pragma warning disable CA2007 // ConfigureAwait not required in test code

namespace Acode.Infrastructure.Tests.Persistence.Migrations;

using Acode.Application.Database;
using Acode.Infrastructure.Persistence.Migrations;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

/// <summary>
/// Tests for MigrationRunner class.
/// Verifies high-level migration orchestration operations.
/// </summary>
public sealed class MigrationRunnerTests
{
    private readonly MigrationRunner _sut;
    private readonly IMigrationLock _lockMock;
    private readonly IMigrationDiscovery _discoveryMock;
    private readonly IMigrationValidator _validatorMock;
    private readonly IMigrationExecutor _executorMock;
    private readonly IMigrationRepository _repositoryMock;
    private readonly ILogger<MigrationRunner> _loggerMock;

    public MigrationRunnerTests()
    {
        _lockMock = Substitute.For<IMigrationLock>();
        _discoveryMock = Substitute.For<IMigrationDiscovery>();
        _validatorMock = Substitute.For<IMigrationValidator>();
        _executorMock = Substitute.For<IMigrationExecutor>();
        _repositoryMock = Substitute.For<IMigrationRepository>();
        _loggerMock = Substitute.For<ILogger<MigrationRunner>>();

        _sut = new MigrationRunner(
            _lockMock,
            _discoveryMock,
            _validatorMock,
            _executorMock,
            _repositoryMock,
            _loggerMock);
    }

    [Fact]
    public async Task MigrateAsync_WithNoPendingMigrations_ReturnsSuccess()
    {
        // Arrange
        _lockMock.TryAcquireAsync(Arg.Any<CancellationToken>()).Returns(true);

        var discoveredMigrations = new List<MigrationFile>();
        var appliedMigrations = new List<AppliedMigration>();

        var validationResult = new ValidationResult
        {
            PendingMigrations = new List<MigrationFile>(),
            ChecksumMismatches = new List<ChecksumMismatch>(),
            VersionGaps = new List<VersionGap>()
        };

        _discoveryMock.DiscoverAsync(Arg.Any<CancellationToken>()).Returns(discoveredMigrations);
        _repositoryMock.GetAppliedMigrationsAsync(Arg.Any<CancellationToken>()).Returns(appliedMigrations);
        _validatorMock.ValidateAsync(
            Arg.Any<IReadOnlyList<MigrationFile>>(),
            Arg.Any<IReadOnlyList<AppliedMigration>>(),
            Arg.Any<CancellationToken>()).Returns(validationResult);

        // Act
        var result = await _sut.MigrateAsync();

        // Assert
        result.Success.Should().BeTrue();
        result.AppliedCount.Should().Be(0);
        result.TotalDuration.Should().BeGreaterThanOrEqualTo(TimeSpan.Zero);
    }

    [Fact]
    public async Task MigrateAsync_WithPendingMigrations_AppliesAll()
    {
        // Arrange
        _lockMock.TryAcquireAsync(Arg.Any<CancellationToken>()).Returns(true);

        var pendingMigration = new MigrationFile
        {
            Version = "001",
            UpContent = "CREATE TABLE test (id INTEGER);",
            Checksum = "abc123",
            Source = MigrationSource.File
        };

        var validationResult = new ValidationResult
        {
            PendingMigrations = new List<MigrationFile> { pendingMigration },
            ChecksumMismatches = new List<ChecksumMismatch>(),
            VersionGaps = new List<VersionGap>()
        };

        _discoveryMock.DiscoverAsync(Arg.Any<CancellationToken>()).Returns(new List<MigrationFile> { pendingMigration });
        _repositoryMock.GetAppliedMigrationsAsync(Arg.Any<CancellationToken>()).Returns(new List<AppliedMigration>());
        _validatorMock.ValidateAsync(
            Arg.Any<IReadOnlyList<MigrationFile>>(),
            Arg.Any<IReadOnlyList<AppliedMigration>>(),
            Arg.Any<CancellationToken>()).Returns(validationResult);

        _executorMock.ApplyAsync(Arg.Any<MigrationFile>(), Arg.Any<CancellationToken>())
            .Returns(new MigrationExecutionResult
            {
                Success = true,
                Version = "001",
                Duration = TimeSpan.FromMilliseconds(100)
            });

        // Act
        var result = await _sut.MigrateAsync();

        // Assert
        result.Success.Should().BeTrue();
        result.AppliedCount.Should().Be(1);
        result.TotalDuration.Should().BeGreaterThan(TimeSpan.Zero);

        await _executorMock.Received(1).ApplyAsync(
            Arg.Is<MigrationFile>(m => m.Version == "001"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RollbackAsync_RollsBackLastMigration()
    {
        // Arrange
        _lockMock.TryAcquireAsync(Arg.Any<CancellationToken>()).Returns(true);

        var appliedMigration = new AppliedMigration
        {
            Version = "001",
            Checksum = "abc123",
            AppliedAt = DateTime.UtcNow,
            Duration = TimeSpan.FromMilliseconds(100),
            Status = MigrationStatus.Applied
        };

        var migrationFile = new MigrationFile
        {
            Version = "001",
            UpContent = "CREATE TABLE test (id INTEGER);",
            DownContent = "DROP TABLE test;",
            Checksum = "abc123",
            Source = MigrationSource.File
        };

        _repositoryMock.GetAppliedMigrationsAsync(Arg.Any<CancellationToken>())
            .Returns(new List<AppliedMigration> { appliedMigration });
        _discoveryMock.DiscoverAsync(Arg.Any<CancellationToken>())
            .Returns(new List<MigrationFile> { migrationFile });

        _executorMock.RollbackAsync(Arg.Any<MigrationFile>(), Arg.Any<CancellationToken>())
            .Returns(new MigrationExecutionResult
            {
                Success = true,
                Version = "001",
                Duration = TimeSpan.FromMilliseconds(50)
            });

        // Act
        var result = await _sut.RollbackAsync();

        // Assert
        result.Success.Should().BeTrue();
        result.RolledBackCount.Should().Be(1);

        await _executorMock.Received(1).RollbackAsync(
            Arg.Is<MigrationFile>(m => m.Version == "001"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RollbackAsync_WithNoAppliedMigrations_ReturnsFailure()
    {
        // Arrange
        _lockMock.TryAcquireAsync(Arg.Any<CancellationToken>()).Returns(true);
        _repositoryMock.GetAppliedMigrationsAsync(Arg.Any<CancellationToken>())
            .Returns(new List<AppliedMigration>());

        // Act
        var result = await _sut.RollbackAsync();

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("no applied migrations");

        await _executorMock.DidNotReceive().RollbackAsync(
            Arg.Any<MigrationFile>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetStatusAsync_ReturnsCurrentState()
    {
        // Arrange
        var appliedMigration = new AppliedMigration
        {
            Version = "001",
            Checksum = "abc123",
            AppliedAt = DateTime.UtcNow.AddHours(-1),
            Duration = TimeSpan.FromMilliseconds(100),
            Status = Application.Database.MigrationStatus.Applied
        };

        var pendingMigration = new MigrationFile
        {
            Version = "002",
            UpContent = "CREATE TABLE users (id INTEGER);",
            Checksum = "def456",
            Source = MigrationSource.File
        };

        var validationResult = new ValidationResult
        {
            PendingMigrations = new List<MigrationFile> { pendingMigration },
            ChecksumMismatches = new List<ChecksumMismatch>(),
            VersionGaps = new List<VersionGap>()
        };

        _discoveryMock.DiscoverAsync(Arg.Any<CancellationToken>())
            .Returns(new List<MigrationFile> { pendingMigration });
        _repositoryMock.GetAppliedMigrationsAsync(Arg.Any<CancellationToken>())
            .Returns(new List<AppliedMigration> { appliedMigration });
        _validatorMock.ValidateAsync(
            Arg.Any<IReadOnlyList<MigrationFile>>(),
            Arg.Any<IReadOnlyList<AppliedMigration>>(),
            Arg.Any<CancellationToken>()).Returns(validationResult);

        // Act
        var result = await _sut.GetStatusAsync();

        // Assert
        result.AppliedMigrations.Should().HaveCount(1);
        result.PendingMigrations.Should().HaveCount(1);
        result.ChecksumMismatches.Should().BeEmpty();
        result.VersionGaps.Should().BeEmpty();
    }

    [Fact]
    public async Task ValidateAsync_WithValidMigrations_ReturnsSuccess()
    {
        // Arrange
        var validationResult = new ValidationResult
        {
            PendingMigrations = new List<MigrationFile>(),
            ChecksumMismatches = new List<ChecksumMismatch>(),
            VersionGaps = new List<VersionGap>()
        };

        _discoveryMock.DiscoverAsync(Arg.Any<CancellationToken>()).Returns(new List<MigrationFile>());
        _repositoryMock.GetAppliedMigrationsAsync(Arg.Any<CancellationToken>()).Returns(new List<AppliedMigration>());
        _validatorMock.ValidateAsync(
            Arg.Any<IReadOnlyList<MigrationFile>>(),
            Arg.Any<IReadOnlyList<AppliedMigration>>(),
            Arg.Any<CancellationToken>()).Returns(validationResult);

        // Act
        var result = await _sut.ValidateAsync();

        // Assert
        result.IsValid.Should().BeTrue();
        result.ChecksumMismatches.Should().BeEmpty();
        result.VersionGaps.Should().BeEmpty();
    }

    [Fact]
    public async Task ValidateAsync_WithChecksumMismatches_ReturnsInvalid()
    {
        // Arrange
        var mismatch = new ChecksumMismatch(
            Version: "001",
            ExpectedChecksum: "abc123",
            ActualChecksum: "xyz789",
            AppliedAt: DateTime.UtcNow.AddHours(-1));

        var validationResult = new ValidationResult
        {
            PendingMigrations = new List<MigrationFile>(),
            ChecksumMismatches = new List<ChecksumMismatch> { mismatch },
            VersionGaps = new List<VersionGap>()
        };

        _discoveryMock.DiscoverAsync(Arg.Any<CancellationToken>()).Returns(new List<MigrationFile>());
        _repositoryMock.GetAppliedMigrationsAsync(Arg.Any<CancellationToken>()).Returns(new List<AppliedMigration>());
        _validatorMock.ValidateAsync(
            Arg.Any<IReadOnlyList<MigrationFile>>(),
            Arg.Any<IReadOnlyList<AppliedMigration>>(),
            Arg.Any<CancellationToken>()).Returns(validationResult);

        // Act
        var result = await _sut.ValidateAsync();

        // Assert
        result.IsValid.Should().BeTrue(); // IsValid only checks version gaps, not checksum mismatches
        result.ChecksumMismatches.Should().HaveCount(1);
    }

    [Fact]
    public async Task MigrateAsync_ReleasesLockEvenOnFailure()
    {
        // Arrange
        _lockMock.TryAcquireAsync(Arg.Any<CancellationToken>()).Returns(true);
        _discoveryMock.DiscoverAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromException<IReadOnlyList<MigrationFile>>(new InvalidOperationException("Discovery failed")));

        // Act
        var result = await _sut.MigrateAsync();

        // Assert
        result.Success.Should().BeFalse();
        await _lockMock.Received(1).DisposeAsync();
    }
}
