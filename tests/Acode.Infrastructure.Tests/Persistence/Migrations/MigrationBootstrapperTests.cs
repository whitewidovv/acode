// tests/Acode.Infrastructure.Tests/Persistence/Migrations/MigrationBootstrapperTests.cs
#pragma warning disable CA2007 // ConfigureAwait not required in test code

namespace Acode.Infrastructure.Tests.Persistence.Migrations;

using Acode.Application.Database;
using Acode.Infrastructure.Persistence.Migrations;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

/// <summary>
/// Tests for MigrationBootstrapper class.
/// Verifies startup migration orchestration with lock management and auto-migrate logic.
/// </summary>
public sealed class MigrationBootstrapperTests
{
    private readonly MigrationBootstrapper _sut;
    private readonly IMigrationLock _lockMock;
    private readonly IMigrationDiscovery _discoveryMock;
    private readonly IMigrationValidator _validatorMock;
    private readonly IMigrationExecutor _executorMock;
    private readonly IMigrationRepository _repositoryMock;
    private readonly ILogger<MigrationBootstrapper> _loggerMock;
    private readonly MigrationBootstrapperOptions _options;

    public MigrationBootstrapperTests()
    {
        _lockMock = Substitute.For<IMigrationLock>();
        _discoveryMock = Substitute.For<IMigrationDiscovery>();
        _validatorMock = Substitute.For<IMigrationValidator>();
        _executorMock = Substitute.For<IMigrationExecutor>();
        _repositoryMock = Substitute.For<IMigrationRepository>();
        _loggerMock = Substitute.For<ILogger<MigrationBootstrapper>>();

        _options = new MigrationBootstrapperOptions
        {
            AutoMigrate = false,
            LockTimeout = TimeSpan.FromSeconds(30)
        };

        _sut = new MigrationBootstrapper(
            _lockMock,
            _discoveryMock,
            _validatorMock,
            _executorMock,
            _repositoryMock,
            _options,
            _loggerMock);
    }

    [Fact]
    public async Task BootstrapAsync_WithNoPendingMigrations_Succeeds()
    {
        // Arrange
        _lockMock.TryAcquireAsync(Arg.Any<CancellationToken>()).Returns(true);

        var discoveredMigrations = new List<MigrationFile>
        {
            new MigrationFile
            {
                Version = "001",
                UpContent = "CREATE TABLE test (id INTEGER);",
                Checksum = "abc123",
                Source = MigrationSource.Embedded
            }
        };

        var appliedMigrations = new List<AppliedMigration>
        {
            new AppliedMigration
            {
                Version = "001",
                Checksum = "abc123",
                AppliedAt = DateTime.UtcNow.AddHours(-1),
                Duration = TimeSpan.FromMilliseconds(50),
                Status = MigrationStatus.Applied
            }
        };

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
        var result = await _sut.BootstrapAsync();

        // Assert
        result.Success.Should().BeTrue();
        result.PendingMigrationsCount.Should().Be(0);
        result.AppliedMigrationsCount.Should().Be(0);

        await _lockMock.Received(1).TryAcquireAsync(Arg.Any<CancellationToken>());
        await _lockMock.Received(1).DisposeAsync();
    }

    [Fact]
    public async Task BootstrapAsync_WithAutoMigrateEnabled_AppliesPendingMigrations()
    {
        // Arrange
        _options.AutoMigrate = true;
        _lockMock.TryAcquireAsync(Arg.Any<CancellationToken>()).Returns(true);

        var pendingMigration = new MigrationFile
        {
            Version = "002",
            UpContent = "CREATE TABLE users (id INTEGER);",
            Checksum = "def456",
            Source = MigrationSource.File
        };

        var discoveredMigrations = new List<MigrationFile> { pendingMigration };
        var appliedMigrations = new List<AppliedMigration>();

        var validationResult = new ValidationResult
        {
            PendingMigrations = new List<MigrationFile> { pendingMigration },
            ChecksumMismatches = new List<ChecksumMismatch>(),
            VersionGaps = new List<VersionGap>()
        };

        _discoveryMock.DiscoverAsync(Arg.Any<CancellationToken>()).Returns(discoveredMigrations);
        _repositoryMock.GetAppliedMigrationsAsync(Arg.Any<CancellationToken>()).Returns(appliedMigrations);
        _validatorMock.ValidateAsync(
            Arg.Any<IReadOnlyList<MigrationFile>>(),
            Arg.Any<IReadOnlyList<AppliedMigration>>(),
            Arg.Any<CancellationToken>()).Returns(validationResult);

        _executorMock.ApplyAsync(Arg.Any<MigrationFile>(), Arg.Any<CancellationToken>())
            .Returns(new MigrationExecutionResult
            {
                Success = true,
                Version = "002",
                Duration = TimeSpan.FromMilliseconds(100)
            });

        // Act
        var result = await _sut.BootstrapAsync();

        // Assert
        result.Success.Should().BeTrue();
        result.PendingMigrationsCount.Should().Be(1);
        result.AppliedMigrationsCount.Should().Be(1);

        await _executorMock.Received(1).ApplyAsync(
            Arg.Is<MigrationFile>(m => m.Version == "002"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task BootstrapAsync_WithAutoMigrateDisabled_DoesNotApplyMigrations()
    {
        // Arrange
        _options.AutoMigrate = false;
        _lockMock.TryAcquireAsync(Arg.Any<CancellationToken>()).Returns(true);

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

        _discoveryMock.DiscoverAsync(Arg.Any<CancellationToken>()).Returns(new List<MigrationFile> { pendingMigration });
        _repositoryMock.GetAppliedMigrationsAsync(Arg.Any<CancellationToken>()).Returns(new List<AppliedMigration>());
        _validatorMock.ValidateAsync(
            Arg.Any<IReadOnlyList<MigrationFile>>(),
            Arg.Any<IReadOnlyList<AppliedMigration>>(),
            Arg.Any<CancellationToken>()).Returns(validationResult);

        // Act
        var result = await _sut.BootstrapAsync();

        // Assert
        result.Success.Should().BeTrue();
        result.PendingMigrationsCount.Should().Be(1);
        result.AppliedMigrationsCount.Should().Be(0);

        await _executorMock.DidNotReceive().ApplyAsync(
            Arg.Any<MigrationFile>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task BootstrapAsync_WhenLockCannotBeAcquired_ReturnsFailure()
    {
        // Arrange
        _lockMock.TryAcquireAsync(Arg.Any<CancellationToken>()).Returns(false);

        // Act
        var result = await _sut.BootstrapAsync();

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("lock");

        await _discoveryMock.DidNotReceive().DiscoverAsync(Arg.Any<CancellationToken>());
        await _executorMock.DidNotReceive().ApplyAsync(Arg.Any<MigrationFile>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task BootstrapAsync_WithChecksumMismatch_ReturnsFailure()
    {
        // Arrange
        _lockMock.TryAcquireAsync(Arg.Any<CancellationToken>()).Returns(true);

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
        var result = await _sut.BootstrapAsync();

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("checksum mismatch");

        await _executorMock.DidNotReceive().ApplyAsync(Arg.Any<MigrationFile>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task BootstrapAsync_WithVersionGaps_ReturnsFailure()
    {
        // Arrange
        _lockMock.TryAcquireAsync(Arg.Any<CancellationToken>()).Returns(true);

        var versionGap = new VersionGap
        {
            MissingVersion = "002",
            BeforeVersion = "001",
            AfterVersion = "003"
        };

        var validationResult = new ValidationResult
        {
            PendingMigrations = new List<MigrationFile>(),
            ChecksumMismatches = new List<ChecksumMismatch>(),
            VersionGaps = new List<VersionGap> { versionGap }
        };

        _discoveryMock.DiscoverAsync(Arg.Any<CancellationToken>()).Returns(new List<MigrationFile>());
        _repositoryMock.GetAppliedMigrationsAsync(Arg.Any<CancellationToken>()).Returns(new List<AppliedMigration>());
        _validatorMock.ValidateAsync(
            Arg.Any<IReadOnlyList<MigrationFile>>(),
            Arg.Any<IReadOnlyList<AppliedMigration>>(),
            Arg.Any<CancellationToken>()).Returns(validationResult);

        // Act
        var result = await _sut.BootstrapAsync();

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("version gap");

        await _executorMock.DidNotReceive().ApplyAsync(Arg.Any<MigrationFile>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task BootstrapAsync_ReleasesLockEvenWhenMigrationFails()
    {
        // Arrange
        _options.AutoMigrate = true;
        _lockMock.TryAcquireAsync(Arg.Any<CancellationToken>()).Returns(true);

        var pendingMigration = new MigrationFile
        {
            Version = "002",
            UpContent = "INVALID SQL;",
            Checksum = "def456",
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
                Success = false,
                Version = "002",
                Duration = TimeSpan.FromMilliseconds(50),
                ErrorMessage = "SQL syntax error"
            });

        // Act
        var result = await _sut.BootstrapAsync();

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("SQL syntax error");

        await _lockMock.Received(1).DisposeAsync();
    }

    [Fact]
    public async Task BootstrapAsync_AppliesMultiplePendingMigrationsInOrder()
    {
        // Arrange
        _options.AutoMigrate = true;
        _lockMock.TryAcquireAsync(Arg.Any<CancellationToken>()).Returns(true);

        var migration1 = new MigrationFile
        {
            Version = "001",
            UpContent = "CREATE TABLE test (id INTEGER);",
            Checksum = "abc123",
            Source = MigrationSource.File
        };

        var migration2 = new MigrationFile
        {
            Version = "002",
            UpContent = "CREATE TABLE users (id INTEGER);",
            Checksum = "def456",
            Source = MigrationSource.File
        };

        var validationResult = new ValidationResult
        {
            PendingMigrations = new List<MigrationFile> { migration1, migration2 },
            ChecksumMismatches = new List<ChecksumMismatch>(),
            VersionGaps = new List<VersionGap>()
        };

        _discoveryMock.DiscoverAsync(Arg.Any<CancellationToken>()).Returns(new List<MigrationFile> { migration1, migration2 });
        _repositoryMock.GetAppliedMigrationsAsync(Arg.Any<CancellationToken>()).Returns(new List<AppliedMigration>());
        _validatorMock.ValidateAsync(
            Arg.Any<IReadOnlyList<MigrationFile>>(),
            Arg.Any<IReadOnlyList<AppliedMigration>>(),
            Arg.Any<CancellationToken>()).Returns(validationResult);

        _executorMock.ApplyAsync(Arg.Any<MigrationFile>(), Arg.Any<CancellationToken>())
            .Returns(
                new MigrationExecutionResult { Success = true, Version = "001", Duration = TimeSpan.FromMilliseconds(50) },
                new MigrationExecutionResult { Success = true, Version = "002", Duration = TimeSpan.FromMilliseconds(60) });

        // Act
        var result = await _sut.BootstrapAsync();

        // Assert
        result.Success.Should().BeTrue();
        result.AppliedMigrationsCount.Should().Be(2);

        await _executorMock.Received(1).ApplyAsync(
            Arg.Is<MigrationFile>(m => m.Version == "001"),
            Arg.Any<CancellationToken>());

        await _executorMock.Received(1).ApplyAsync(
            Arg.Is<MigrationFile>(m => m.Version == "002"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task BootstrapAsync_StopsOnFirstMigrationFailure()
    {
        // Arrange
        _options.AutoMigrate = true;
        _lockMock.TryAcquireAsync(Arg.Any<CancellationToken>()).Returns(true);

        var migration1 = new MigrationFile
        {
            Version = "001",
            UpContent = "INVALID SQL;",
            Checksum = "abc123",
            Source = MigrationSource.File
        };

        var migration2 = new MigrationFile
        {
            Version = "002",
            UpContent = "CREATE TABLE users (id INTEGER);",
            Checksum = "def456",
            Source = MigrationSource.File
        };

        var validationResult = new ValidationResult
        {
            PendingMigrations = new List<MigrationFile> { migration1, migration2 },
            ChecksumMismatches = new List<ChecksumMismatch>(),
            VersionGaps = new List<VersionGap>()
        };

        _discoveryMock.DiscoverAsync(Arg.Any<CancellationToken>()).Returns(new List<MigrationFile> { migration1, migration2 });
        _repositoryMock.GetAppliedMigrationsAsync(Arg.Any<CancellationToken>()).Returns(new List<AppliedMigration>());
        _validatorMock.ValidateAsync(
            Arg.Any<IReadOnlyList<MigrationFile>>(),
            Arg.Any<IReadOnlyList<AppliedMigration>>(),
            Arg.Any<CancellationToken>()).Returns(validationResult);

        _executorMock.ApplyAsync(Arg.Is<MigrationFile>(m => m.Version == "001"), Arg.Any<CancellationToken>())
            .Returns(new MigrationExecutionResult
            {
                Success = false,
                Version = "001",
                Duration = TimeSpan.FromMilliseconds(50),
                ErrorMessage = "SQL syntax error"
            });

        // Act
        var result = await _sut.BootstrapAsync();

        // Assert
        result.Success.Should().BeFalse();
        result.AppliedMigrationsCount.Should().Be(0);

        await _executorMock.Received(1).ApplyAsync(
            Arg.Is<MigrationFile>(m => m.Version == "001"),
            Arg.Any<CancellationToken>());

        await _executorMock.DidNotReceive().ApplyAsync(
            Arg.Is<MigrationFile>(m => m.Version == "002"),
            Arg.Any<CancellationToken>());
    }
}
