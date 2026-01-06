#pragma warning disable CA2007 // Consider calling ConfigureAwait on awaited task - xUnit tests should use ConfigureAwait(true)

using Acode.Application.Database;
using Acode.Infrastructure.Database.Sqlite;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
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
    public void Constructor_WithNullConnectionString_ThrowsArgumentNullException()
    {
        // Act & Assert
        var act = () => new SqliteConnectionFactory(null!, NullLogger<SqliteConnectionFactory>.Instance);
        act.Should().Throw<ArgumentNullException>().WithParameterName("databasePath");
    }

    [Fact]
    public void ProviderType_ReturnsSQLite()
    {
        // Arrange
        var factory = new SqliteConnectionFactory(_testDbPath, NullLogger<SqliteConnectionFactory>.Instance);

        // Assert
        factory.ProviderType.Should().Be(DbProviderType.SQLite);
    }

    [Fact]
    public void ConnectionString_ReturnsExpectedValue()
    {
        // Arrange
        var factory = new SqliteConnectionFactory(_testDbPath, NullLogger<SqliteConnectionFactory>.Instance);

        // Assert
        factory.ConnectionString.Should().Contain(_testDbPath);
    }

    [Fact]
    public async Task CreateAsync_CreatesDirectoryIfNotExists()
    {
        // Arrange
        var factory = new SqliteConnectionFactory(_testDbPath, NullLogger<SqliteConnectionFactory>.Instance);
        Directory.Exists(_testDbDir).Should().BeFalse("directory should not exist before test");

        // Act
        await using var connection = await factory.CreateAsync(CancellationToken.None).ConfigureAwait(true);

        // Assert
        Directory.Exists(_testDbDir).Should().BeTrue("directory should be created");
    }

    [Fact]
    public async Task CreateAsync_CreatesDatabaseFile()
    {
        // Arrange
        var factory = new SqliteConnectionFactory(_testDbPath, NullLogger<SqliteConnectionFactory>.Instance);
        File.Exists(_testDbPath).Should().BeFalse("database file should not exist before test");

        // Act
        await using var connection = await factory.CreateAsync(CancellationToken.None).ConfigureAwait(true);

        // Assert
        File.Exists(_testDbPath).Should().BeTrue("database file should be created");
    }

    [Fact]
    public async Task CreateAsync_ReturnsOpenConnection()
    {
        // Arrange
        var factory = new SqliteConnectionFactory(_testDbPath, NullLogger<SqliteConnectionFactory>.Instance);

        // Act
        await using var connection = await factory.CreateAsync(CancellationToken.None).ConfigureAwait(true);

        // Assert
        connection.State.Should().Be(System.Data.ConnectionState.Open);
    }

    [Fact]
    public async Task CreateAsync_EnablesWALMode()
    {
        // Arrange
        var factory = new SqliteConnectionFactory(_testDbPath, NullLogger<SqliteConnectionFactory>.Instance);

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
        var factory = new SqliteConnectionFactory(_testDbPath, NullLogger<SqliteConnectionFactory>.Instance, busyTimeoutMs: 3000);

        // Act
        await using var connection = await factory.CreateAsync(CancellationToken.None).ConfigureAwait(true);

        // Assert
        var busyTimeout = await connection.QuerySingleAsync<int>("PRAGMA busy_timeout;").ConfigureAwait(true);
        busyTimeout.Should().Be(3000);
    }

    [Fact]
    public async Task CreateAsync_RespectsCancellation()
    {
        // Arrange
        var factory = new SqliteConnectionFactory(_testDbPath, NullLogger<SqliteConnectionFactory>.Instance);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        var act = async () => await factory.CreateAsync(cts.Token).ConfigureAwait(true);
        await act.Should().ThrowAsync<OperationCanceledException>().ConfigureAwait(true);
    }
}
