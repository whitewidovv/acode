#pragma warning disable CA2007 // xUnit tests should use ConfigureAwait(true)

using Acode.Application.Database;
using Acode.Infrastructure.Database;
using Acode.Infrastructure.Database.Sqlite;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Acode.Infrastructure.Tests.Database;

/// <summary>
/// Tests for <see cref="SqliteConnectionFactory"/>.
/// </summary>
public sealed class SqliteConnectionFactoryTests : IDisposable
{
    private readonly string _testDbPath;
    private readonly string _testDbDir;

    public SqliteConnectionFactoryTests()
    {
        _testDbDir = Path.Combine(Path.GetTempPath(), $"acode-test-{Guid.NewGuid():N}");
        _testDbPath = Path.Combine(_testDbDir, "workspace.db");
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDbDir))
        {
            Directory.Delete(_testDbDir, recursive: true);
        }
    }

    [Fact]
    public void Constructor_WithNullOptions_ThrowsArgumentNullException()
    {
        // Act & Assert
        var act = () => new SqliteConnectionFactory(null!, NullLogger<SqliteConnectionFactory>.Instance);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Provider_ReturnsSQLite()
    {
        // Arrange
        var options = CreateOptions(_testDbPath);
        var factory = new SqliteConnectionFactory(options, NullLogger<SqliteConnectionFactory>.Instance);

        // Assert
        factory.Provider.Should().Be(DatabaseProvider.SQLite);
    }

    [Fact]
    public void Constructor_CreatesDirectoryIfNotExists()
    {
        // Arrange - directory doesn't exist initially
        Directory.Exists(_testDbDir).Should().BeFalse("directory should not exist before constructor");

        // Act - constructor creates directory
        var options = CreateOptions(_testDbPath);
        using var factory = new SqliteConnectionFactory(options, NullLogger<SqliteConnectionFactory>.Instance);

        // Assert
        Directory.Exists(_testDbDir).Should().BeTrue("constructor should create directory");
    }

    [Fact]
    public async Task CreateAsync_CreatesDatabaseFile()
    {
        // Arrange
        var options = CreateOptions(_testDbPath);
        var factory = new SqliteConnectionFactory(options, NullLogger<SqliteConnectionFactory>.Instance);
        File.Exists(_testDbPath).Should().BeFalse("database file should not exist before test");

        // Act
        await using var connection = await factory.CreateAsync(CancellationToken.None).ConfigureAwait(true);

        // Assert
        File.Exists(_testDbPath).Should().BeTrue("database file should be created");
    }

    [Fact]
    public async Task CreateAsync_EnablesWALMode()
    {
        // Arrange
        var options = CreateOptions(_testDbPath);
        var factory = new SqliteConnectionFactory(options, NullLogger<SqliteConnectionFactory>.Instance);

        // Act
        await using var connection = await factory.CreateAsync(CancellationToken.None).ConfigureAwait(true);

        // Assert
        var journalMode = await connection.QuerySingleAsync<string>("PRAGMA journal_mode;").ConfigureAwait(true);
        journalMode.Should().BeEquivalentTo("wal");
    }

    [Fact]
    public async Task CreateAsync_SetsBusyTimeout()
    {
        // Arrange
        var options = CreateOptions(_testDbPath, busyTimeoutMs: 3000);
        var factory = new SqliteConnectionFactory(options, NullLogger<SqliteConnectionFactory>.Instance);

        // Act
        await using var connection = await factory.CreateAsync(CancellationToken.None).ConfigureAwait(true);

        // Assert
        var busyTimeout = await connection.QuerySingleAsync<int>("PRAGMA busy_timeout;").ConfigureAwait(true);
        busyTimeout.Should().Be(3000);
    }

    [Fact]
    public async Task CreateAsync_EnablesForeignKeys()
    {
        // Arrange
        var options = CreateOptions(_testDbPath);
        var factory = new SqliteConnectionFactory(options, NullLogger<SqliteConnectionFactory>.Instance);

        // Act
        await using var connection = await factory.CreateAsync(CancellationToken.None).ConfigureAwait(true);

        // Assert
        var foreignKeys = await connection.QuerySingleAsync<int>("PRAGMA foreign_keys;").ConfigureAwait(true);
        foreignKeys.Should().Be(1, "foreign_keys should be enabled");
    }

    [Fact]
    public async Task CreateAsync_RespectsCancellation()
    {
        // Arrange
        var options = CreateOptions(_testDbPath);
        using var factory = new SqliteConnectionFactory(options, NullLogger<SqliteConnectionFactory>.Instance);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert - TaskCanceledException inherits from OperationCanceledException
        var act = async () => await factory.CreateAsync(cts.Token).ConfigureAwait(true);
        await act.Should().ThrowAsync<OperationCanceledException>().ConfigureAwait(true);
    }

    [Fact]
    public async Task CheckHealthAsync_ReturnsHealthy_WhenDatabaseExists()
    {
        // Arrange
        var options = CreateOptions(_testDbPath);
        var factory = new SqliteConnectionFactory(options, NullLogger<SqliteConnectionFactory>.Instance);

        // Create database first
        await using var connection = await factory.CreateAsync().ConfigureAwait(true);

        // Act
        var result = await factory.CheckHealthAsync().ConfigureAwait(true);

        // Assert
        result.Status.Should().Be(HealthStatus.Healthy);
        result.Data.Should().NotBeNull();
        result.Data.Should().ContainKey("path");
        result.Data.Should().ContainKey("size_bytes");
    }

    [Fact]
    public async Task CheckHealthAsync_ReturnsUnhealthy_WhenDatabaseMissing()
    {
        // Arrange
        var options = CreateOptions(_testDbPath);
        var factory = new SqliteConnectionFactory(options, NullLogger<SqliteConnectionFactory>.Instance);

        // Act (database doesn't exist)
        var result = await factory.CheckHealthAsync().ConfigureAwait(true);

        // Assert
        result.Status.Should().Be(HealthStatus.Unhealthy);
        result.Description.Should().Contain("not found");
    }

    private static IOptions<DatabaseOptions> CreateOptions(string dbPath, int busyTimeoutMs = 5000)
    {
        var options = new DatabaseOptions
        {
            Local = new LocalDatabaseOptions
            {
                Path = dbPath,
                BusyTimeoutMs = busyTimeoutMs,
            },
        };
        return Options.Create(options);
    }
}
