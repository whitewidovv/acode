// tests/Acode.Infrastructure.Tests/Persistence/SqliteConnectionFactoryTests.cs
#pragma warning disable CA2007 // ConfigureAwait not required in test code

namespace Acode.Infrastructure.Tests.Persistence;

using Acode.Domain.Enums;
using Acode.Infrastructure.Configuration;
using Acode.Infrastructure.Persistence.Connections;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

/// <summary>
/// Tests for SqliteConnectionFactory directory creation, PRAGMA configuration, and connection pooling.
/// Verifies WAL mode, foreign keys, and error handling.
/// </summary>
public sealed class SqliteConnectionFactoryTests : IDisposable
{
    private readonly string _testDirectory;

    public SqliteConnectionFactoryTests()
    {
        // Create unique test directory for this test run
        _testDirectory = Path.Combine(
            Path.GetTempPath(),
            "acode-tests",
            $"sqlite-{Guid.NewGuid():N}");
    }

    public void Dispose()
    {
        // Cleanup test directory after tests
        if (Directory.Exists(_testDirectory))
        {
            try
            {
                Directory.Delete(_testDirectory, recursive: true);
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }

    [Fact]
    public void Constructor_ShouldCreateDirectory_WhenItDoesNotExist()
    {
        // Arrange
        var dbPath = Path.Combine(_testDirectory, "data", "workspace.db");
        var options = CreateOptions(dbPath);

        // Act
        _ = new SqliteConnectionFactory(options, NullLogger<SqliteConnectionFactory>.Instance);

        // Assert
        var directory = Path.GetDirectoryName(dbPath);
        Directory.Exists(directory).Should().BeTrue("directory should be created by constructor");
    }

    [Fact]
    public async Task CreateAsync_ShouldReturnOpenConnection()
    {
        // Arrange
        var dbPath = Path.Combine(_testDirectory, "test.db");
        var options = CreateOptions(dbPath);
        var factory = new SqliteConnectionFactory(options, NullLogger<SqliteConnectionFactory>.Instance);

        // Act
        var connection = await factory.CreateAsync(CancellationToken.None);
        using (connection)
        {
            // Assert
            connection.Should().NotBeNull();
            connection.State.Should().Be(System.Data.ConnectionState.Open);
            connection.Should().BeOfType<SqliteConnection>();
        }
    }

    [Fact]
    public async Task CreateAsync_ShouldConfigureWalMode_WhenEnabled()
    {
        // Arrange
        var dbPath = Path.Combine(_testDirectory, "wal-test.db");
        var options = CreateOptions(dbPath, walMode: true);
        var factory = new SqliteConnectionFactory(options, NullLogger<SqliteConnectionFactory>.Instance);

        // Act
        var connection = await factory.CreateAsync(CancellationToken.None);
        using (connection)
        {
            // Assert - Query PRAGMA journal_mode
            var cmd = ((SqliteConnection)connection).CreateCommand();
            await using (cmd.ConfigureAwait(false))
            {
                cmd.CommandText = "PRAGMA journal_mode;";
                var result = await cmd.ExecuteScalarAsync();
                result.Should().NotBeNull();
                result!.ToString()!.ToUpperInvariant().Should().Be("WAL");
            }
        }
    }

    [Fact]
    public async Task CreateAsync_ShouldConfigureDeleteMode_WhenWalDisabled()
    {
        // Arrange
        var dbPath = Path.Combine(_testDirectory, "delete-test.db");
        var options = CreateOptions(dbPath, walMode: false);
        var factory = new SqliteConnectionFactory(options, NullLogger<SqliteConnectionFactory>.Instance);

        // Act
        var connection = await factory.CreateAsync(CancellationToken.None);
        using (connection)
        {
            // Assert - Query PRAGMA journal_mode
            var cmd = ((SqliteConnection)connection).CreateCommand();
            await using (cmd.ConfigureAwait(false))
            {
                cmd.CommandText = "PRAGMA journal_mode;";
                var result = await cmd.ExecuteScalarAsync();
                result.Should().NotBeNull();
                result!.ToString()!.ToUpperInvariant().Should().Be("DELETE");
            }
        }
    }

    [Fact]
    public async Task CreateAsync_ShouldEnableForeignKeys()
    {
        // Arrange
        var dbPath = Path.Combine(_testDirectory, "fk-test.db");
        var options = CreateOptions(dbPath);
        var factory = new SqliteConnectionFactory(options, NullLogger<SqliteConnectionFactory>.Instance);

        // Act
        var connection = await factory.CreateAsync(CancellationToken.None);
        using (connection)
        {
            // Assert - Query PRAGMA foreign_keys
            var cmd = ((SqliteConnection)connection).CreateCommand();
            await using (cmd.ConfigureAwait(false))
            {
                cmd.CommandText = "PRAGMA foreign_keys;";
                var result = await cmd.ExecuteScalarAsync();
                result.Should().NotBeNull();
                Convert.ToInt64(result).Should().Be(1, "foreign keys should be enabled");
            }
        }
    }

    [Fact]
    public async Task CreateAsync_ShouldConfigureBusyTimeout()
    {
        // Arrange
        var dbPath = Path.Combine(_testDirectory, "timeout-test.db");
        var options = CreateOptions(dbPath, busyTimeoutMs: 5000);
        var factory = new SqliteConnectionFactory(options, NullLogger<SqliteConnectionFactory>.Instance);

        // Act
        var connection = await factory.CreateAsync(CancellationToken.None);
        using (connection)
        {
            // Assert - Query PRAGMA busy_timeout
            var cmd = ((SqliteConnection)connection).CreateCommand();
            await using (cmd.ConfigureAwait(false))
            {
                cmd.CommandText = "PRAGMA busy_timeout;";
                var result = await cmd.ExecuteScalarAsync();
                result.Should().NotBeNull();
                Convert.ToInt64(result).Should().Be(5000);
            }
        }
    }

    [Fact]
    public async Task CreateAsync_ShouldConfigureSynchronousNormal()
    {
        // Arrange
        var dbPath = Path.Combine(_testDirectory, "sync-test.db");
        var options = CreateOptions(dbPath);
        var factory = new SqliteConnectionFactory(options, NullLogger<SqliteConnectionFactory>.Instance);

        // Act
        var connection = await factory.CreateAsync(CancellationToken.None);
        using (connection)
        {
            // Assert - Query PRAGMA synchronous
            var cmd = ((SqliteConnection)connection).CreateCommand();
            await using (cmd.ConfigureAwait(false))
            {
                cmd.CommandText = "PRAGMA synchronous;";
                var result = await cmd.ExecuteScalarAsync();
                result.Should().NotBeNull();
                Convert.ToInt64(result).Should().Be(1, "synchronous=NORMAL is 1");
            }
        }
    }

    [Fact]
    public async Task CreateAsync_ShouldRespectCancellation()
    {
        // Arrange
        var dbPath = Path.Combine(_testDirectory, "cancel-test.db");
        var options = CreateOptions(dbPath);
        var factory = new SqliteConnectionFactory(options, NullLogger<SqliteConnectionFactory>.Instance);
        var cts = new CancellationTokenSource();
        cts.Cancel(); // Cancel immediately

        // Act
        var act = async () => await factory.CreateAsync(cts.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public void DatabaseType_ShouldReturnSqlite()
    {
        // Arrange
        var dbPath = Path.Combine(_testDirectory, "type-test.db");
        var options = CreateOptions(dbPath);
        var factory = new SqliteConnectionFactory(options, NullLogger<SqliteConnectionFactory>.Instance);

        // Act
        var databaseType = factory.DatabaseType;

        // Assert
        databaseType.Should().Be(DatabaseType.Sqlite);
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentNullException_WhenOptionsIsNull()
    {
        // Arrange & Act
        var act = () => new SqliteConnectionFactory(null!, NullLogger<SqliteConnectionFactory>.Instance);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("options");
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentNullException_WhenLoggerIsNull()
    {
        // Arrange
        var dbPath = Path.Combine(_testDirectory, "logger-test.db");
        var options = CreateOptions(dbPath);

        // Act
        var act = () => new SqliteConnectionFactory(options, null!);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    [Fact]
    public async Task CreateAsync_ShouldCreateDatabaseFile_WhenItDoesNotExist()
    {
        // Arrange
        var dbPath = Path.Combine(_testDirectory, "new-file.db");
        var options = CreateOptions(dbPath);
        var factory = new SqliteConnectionFactory(options, NullLogger<SqliteConnectionFactory>.Instance);

        File.Exists(dbPath).Should().BeFalse("database file should not exist yet");

        // Act
        var connection = await factory.CreateAsync(CancellationToken.None);
        using (connection)
        {
            // Assert
            File.Exists(dbPath).Should().BeTrue("database file should be created");
        }
    }

    private static IOptions<DatabaseOptions> CreateOptions(
        string path,
        bool walMode = true,
        int busyTimeoutMs = 3000)
    {
        return Options.Create(new DatabaseOptions
        {
            Provider = "sqlite",
            Local = new LocalDatabaseOptions
            {
                Path = path,
                WalMode = walMode,
                BusyTimeoutMs = busyTimeoutMs,
            },
        });
    }
}
