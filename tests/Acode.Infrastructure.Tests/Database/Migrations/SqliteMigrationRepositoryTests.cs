#pragma warning disable CA2007 // xUnit tests should use ConfigureAwait(true)

using Acode.Application.Database;
using Acode.Infrastructure.Database.Migrations;
using Acode.Infrastructure.Database.Sqlite;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Acode.Infrastructure.Tests.Database.Migrations;

/// <summary>
/// Tests for <see cref="SqliteMigrationRepository"/>.
/// </summary>
public sealed class SqliteMigrationRepositoryTests : IDisposable
{
    private readonly string _testDbPath;
    private readonly string _testDbDir;
    private readonly IConnectionFactory _connectionFactory;

    public SqliteMigrationRepositoryTests()
    {
        _testDbDir = Path.Combine(Path.GetTempPath(), $"acode-migrations-test-{Guid.NewGuid():N}");
        _testDbPath = Path.Combine(_testDbDir, "migrations.db");
        _connectionFactory = new SqliteConnectionFactory(_testDbPath, NullLogger<SqliteConnectionFactory>.Instance);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDbDir))
        {
            Directory.Delete(_testDbDir, recursive: true);
        }
    }

    [Fact]
    public async Task EnsureMigrationsTableExistsAsync_CreatesTableOnFirstCall()
    {
        // Arrange
        var repository = new SqliteMigrationRepository(_connectionFactory, NullLogger<SqliteMigrationRepository>.Instance);

        // Act
        var created = await repository.EnsureMigrationsTableExistsAsync().ConfigureAwait(true);

        // Assert
        created.Should().BeTrue("table should be created on first call");

        // Verify table exists by querying it
        var migrations = await repository.GetAppliedMigrationsAsync().ConfigureAwait(true);
        migrations.Should().NotBeNull();
    }

    [Fact]
    public async Task EnsureMigrationsTableExistsAsync_ReturnsFalseOnSubsequentCalls()
    {
        // Arrange
        var repository = new SqliteMigrationRepository(_connectionFactory, NullLogger<SqliteMigrationRepository>.Instance);
        await repository.EnsureMigrationsTableExistsAsync().ConfigureAwait(true);

        // Act
        var created = await repository.EnsureMigrationsTableExistsAsync().ConfigureAwait(true);

        // Assert
        created.Should().BeFalse("table already exists");
    }

    [Fact]
    public async Task GetAppliedMigrationsAsync_ReturnsEmptyListWhenNoMigrationsApplied()
    {
        // Arrange
        var repository = new SqliteMigrationRepository(_connectionFactory, NullLogger<SqliteMigrationRepository>.Instance);
        await repository.EnsureMigrationsTableExistsAsync().ConfigureAwait(true);

        // Act
        var migrations = await repository.GetAppliedMigrationsAsync().ConfigureAwait(true);

        // Assert
        migrations.Should().BeEmpty();
    }

    [Fact]
    public async Task RecordMigrationAsync_StoresMigrationSuccessfully()
    {
        // Arrange
        var repository = new SqliteMigrationRepository(_connectionFactory, NullLogger<SqliteMigrationRepository>.Instance);
        await repository.EnsureMigrationsTableExistsAsync().ConfigureAwait(true);

        var migration = new AppliedMigration
        {
            Version = "001",
            Checksum = "abc123",
            AppliedAt = DateTime.UtcNow,
            Duration = TimeSpan.FromMilliseconds(250),
            AppliedBy = "test-user",
            Status = MigrationStatus.Applied,
        };

        // Act
        await repository.RecordMigrationAsync(migration).ConfigureAwait(true);

        // Assert
        var retrieved = await repository.GetAppliedMigrationAsync("001").ConfigureAwait(true);
        retrieved.Should().NotBeNull();
        retrieved!.Version.Should().Be("001");
        retrieved.Checksum.Should().Be("abc123");
        retrieved.AppliedBy.Should().Be("test-user");
        retrieved.Status.Should().Be(MigrationStatus.Applied);
    }

    [Fact]
    public async Task GetAppliedMigrationsAsync_ReturnsInVersionOrder()
    {
        // Arrange
        var repository = new SqliteMigrationRepository(_connectionFactory, NullLogger<SqliteMigrationRepository>.Instance);
        await repository.EnsureMigrationsTableExistsAsync().ConfigureAwait(true);

        await repository.RecordMigrationAsync(new AppliedMigration
        {
            Version = "003",
            Checksum = "hash3",
            AppliedAt = DateTime.UtcNow,
            Duration = TimeSpan.FromMilliseconds(100),
        }).ConfigureAwait(true);

        await repository.RecordMigrationAsync(new AppliedMigration
        {
            Version = "001",
            Checksum = "hash1",
            AppliedAt = DateTime.UtcNow,
            Duration = TimeSpan.FromMilliseconds(100),
        }).ConfigureAwait(true);

        await repository.RecordMigrationAsync(new AppliedMigration
        {
            Version = "002",
            Checksum = "hash2",
            AppliedAt = DateTime.UtcNow,
            Duration = TimeSpan.FromMilliseconds(100),
        }).ConfigureAwait(true);

        // Act
        var migrations = await repository.GetAppliedMigrationsAsync().ConfigureAwait(true);

        // Assert
        migrations.Should().HaveCount(3);
        migrations[0].Version.Should().Be("001");
        migrations[1].Version.Should().Be("002");
        migrations[2].Version.Should().Be("003");
    }

    [Fact]
    public async Task GetLatestMigrationAsync_ReturnsHighestVersion()
    {
        // Arrange
        var repository = new SqliteMigrationRepository(_connectionFactory, NullLogger<SqliteMigrationRepository>.Instance);
        await repository.EnsureMigrationsTableExistsAsync().ConfigureAwait(true);

        await repository.RecordMigrationAsync(new AppliedMigration
        {
            Version = "001",
            Checksum = "hash1",
            AppliedAt = DateTime.UtcNow,
            Duration = TimeSpan.FromMilliseconds(100),
        }).ConfigureAwait(true);

        await repository.RecordMigrationAsync(new AppliedMigration
        {
            Version = "005",
            Checksum = "hash5",
            AppliedAt = DateTime.UtcNow,
            Duration = TimeSpan.FromMilliseconds(100),
        }).ConfigureAwait(true);

        // Act
        var latest = await repository.GetLatestMigrationAsync().ConfigureAwait(true);

        // Assert
        latest.Should().NotBeNull();
        latest!.Version.Should().Be("005");
    }

    [Fact]
    public async Task GetLatestMigrationAsync_ReturnsNullWhenNoMigrations()
    {
        // Arrange
        var repository = new SqliteMigrationRepository(_connectionFactory, NullLogger<SqliteMigrationRepository>.Instance);
        await repository.EnsureMigrationsTableExistsAsync().ConfigureAwait(true);

        // Act
        var latest = await repository.GetLatestMigrationAsync().ConfigureAwait(true);

        // Assert
        latest.Should().BeNull();
    }

    [Fact]
    public async Task RemoveMigrationAsync_RemovesMigrationSuccessfully()
    {
        // Arrange
        var repository = new SqliteMigrationRepository(_connectionFactory, NullLogger<SqliteMigrationRepository>.Instance);
        await repository.EnsureMigrationsTableExistsAsync().ConfigureAwait(true);

        await repository.RecordMigrationAsync(new AppliedMigration
        {
            Version = "001",
            Checksum = "hash1",
            AppliedAt = DateTime.UtcNow,
            Duration = TimeSpan.FromMilliseconds(100),
        }).ConfigureAwait(true);

        // Act
        var removed = await repository.RemoveMigrationAsync("001").ConfigureAwait(true);

        // Assert
        removed.Should().BeTrue();
        var retrieved = await repository.GetAppliedMigrationAsync("001").ConfigureAwait(true);
        retrieved.Should().BeNull();
    }

    [Fact]
    public async Task RemoveMigrationAsync_ReturnsFalseWhenMigrationNotFound()
    {
        // Arrange
        var repository = new SqliteMigrationRepository(_connectionFactory, NullLogger<SqliteMigrationRepository>.Instance);
        await repository.EnsureMigrationsTableExistsAsync().ConfigureAwait(true);

        // Act
        var removed = await repository.RemoveMigrationAsync("999").ConfigureAwait(true);

        // Assert
        removed.Should().BeFalse();
    }

    [Fact]
    public async Task IsMigrationAppliedAsync_ReturnsTrueForAppliedMigration()
    {
        // Arrange
        var repository = new SqliteMigrationRepository(_connectionFactory, NullLogger<SqliteMigrationRepository>.Instance);
        await repository.EnsureMigrationsTableExistsAsync().ConfigureAwait(true);

        await repository.RecordMigrationAsync(new AppliedMigration
        {
            Version = "001",
            Checksum = "hash1",
            AppliedAt = DateTime.UtcNow,
            Duration = TimeSpan.FromMilliseconds(100),
        }).ConfigureAwait(true);

        // Act
        var applied = await repository.IsMigrationAppliedAsync("001").ConfigureAwait(true);

        // Assert
        applied.Should().BeTrue();
    }

    [Fact]
    public async Task IsMigrationAppliedAsync_ReturnsFalseForUnappliedMigration()
    {
        // Arrange
        var repository = new SqliteMigrationRepository(_connectionFactory, NullLogger<SqliteMigrationRepository>.Instance);
        await repository.EnsureMigrationsTableExistsAsync().ConfigureAwait(true);

        // Act
        var applied = await repository.IsMigrationAppliedAsync("999").ConfigureAwait(true);

        // Assert
        applied.Should().BeFalse();
    }
}
