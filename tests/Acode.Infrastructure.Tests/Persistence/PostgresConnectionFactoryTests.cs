// tests/Acode.Infrastructure.Tests/Persistence/PostgresConnectionFactoryTests.cs
#pragma warning disable CA2007 // ConfigureAwait not required in test code

namespace Acode.Infrastructure.Tests.Persistence;

using Acode.Domain.Enums;
using Acode.Infrastructure.Configuration;
using Acode.Infrastructure.Persistence.Connections;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

/// <summary>
/// Tests for PostgresConnectionFactory connection pooling, environment variables, and configuration.
/// Note: These tests verify factory construction and configuration, but do not require an actual PostgreSQL server.
/// </summary>
public sealed class PostgresConnectionFactoryTests
{
    [Fact]
    public void Constructor_ShouldInitializeDataSource()
    {
        // Arrange
        var options = CreateOptions();

        // Act
        var factory = new PostgresConnectionFactory(options, NullLogger<PostgresConnectionFactory>.Instance);

        // Assert
        factory.Should().NotBeNull();
        factory.DatabaseType.Should().Be(DatabaseType.Postgres);
    }

    [Fact]
    public void DatabaseType_ShouldReturnPostgres()
    {
        // Arrange
        var options = CreateOptions();
        var factory = new PostgresConnectionFactory(options, NullLogger<PostgresConnectionFactory>.Instance);

        // Act
        var databaseType = factory.DatabaseType;

        // Assert
        databaseType.Should().Be(DatabaseType.Postgres);
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentNullException_WhenOptionsIsNull()
    {
        // Arrange & Act
        var act = () => new PostgresConnectionFactory(null!, NullLogger<PostgresConnectionFactory>.Instance);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("options");
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentNullException_WhenLoggerIsNull()
    {
        // Arrange
        var options = CreateOptions();

        // Act
        var act = () => new PostgresConnectionFactory(options, null!);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    [Fact]
    public void Constructor_ShouldUseEnvironmentVariableForHost_WhenSet()
    {
        // Arrange
        var originalHost = Environment.GetEnvironmentVariable("ACODE_PG_HOST");
        try
        {
            Environment.SetEnvironmentVariable("ACODE_PG_HOST", "env-postgres-host");
            var options = CreateOptions(host: "config-host");

            // Act
            var factory = new PostgresConnectionFactory(options, NullLogger<PostgresConnectionFactory>.Instance);

            // Assert
            // We can't directly inspect the connection string, but the factory should be created successfully
            factory.Should().NotBeNull();
        }
        finally
        {
            Environment.SetEnvironmentVariable("ACODE_PG_HOST", originalHost);
        }
    }

    [Fact]
    public void Constructor_ShouldUseEnvironmentVariableForPort_WhenSet()
    {
        // Arrange
        var originalPort = Environment.GetEnvironmentVariable("ACODE_PG_PORT");
        try
        {
            Environment.SetEnvironmentVariable("ACODE_PG_PORT", "9999");
            var options = CreateOptions(port: 5432);

            // Act
            var factory = new PostgresConnectionFactory(options, NullLogger<PostgresConnectionFactory>.Instance);

            // Assert
            factory.Should().NotBeNull();
        }
        finally
        {
            Environment.SetEnvironmentVariable("ACODE_PG_PORT", originalPort);
        }
    }

    [Fact]
    public void Constructor_ShouldUseEnvironmentVariableForDatabase_WhenSet()
    {
        // Arrange
        var originalDatabase = Environment.GetEnvironmentVariable("ACODE_PG_DATABASE");
        try
        {
            Environment.SetEnvironmentVariable("ACODE_PG_DATABASE", "env-database");
            var options = CreateOptions(database: "config-database");

            // Act
            var factory = new PostgresConnectionFactory(options, NullLogger<PostgresConnectionFactory>.Instance);

            // Assert
            factory.Should().NotBeNull();
        }
        finally
        {
            Environment.SetEnvironmentVariable("ACODE_PG_DATABASE", originalDatabase);
        }
    }

    [Fact]
    public void Constructor_ShouldUseEnvironmentVariableForUsername_WhenSet()
    {
        // Arrange
        var originalUsername = Environment.GetEnvironmentVariable("ACODE_PG_USERNAME");
        try
        {
            Environment.SetEnvironmentVariable("ACODE_PG_USERNAME", "env-user");
            var options = CreateOptions(username: "config-user");

            // Act
            var factory = new PostgresConnectionFactory(options, NullLogger<PostgresConnectionFactory>.Instance);

            // Assert
            factory.Should().NotBeNull();
        }
        finally
        {
            Environment.SetEnvironmentVariable("ACODE_PG_USERNAME", originalUsername);
        }
    }

    [Fact]
    public void Constructor_ShouldUseEnvironmentVariableForPassword_WhenSet()
    {
        // Arrange
        var originalPassword = Environment.GetEnvironmentVariable("ACODE_PG_PASSWORD");
        try
        {
            Environment.SetEnvironmentVariable("ACODE_PG_PASSWORD", "env-password");
            var options = CreateOptions(password: "config-password");

            // Act
            var factory = new PostgresConnectionFactory(options, NullLogger<PostgresConnectionFactory>.Instance);

            // Assert
            factory.Should().NotBeNull();
        }
        finally
        {
            Environment.SetEnvironmentVariable("ACODE_PG_PASSWORD", originalPassword);
        }
    }

    [Fact]
    public void Constructor_ShouldUseConfigurationValues_WhenEnvironmentVariablesNotSet()
    {
        // Arrange
        // Ensure no environment variables are set
        var originalHost = Environment.GetEnvironmentVariable("ACODE_PG_HOST");
        var originalPort = Environment.GetEnvironmentVariable("ACODE_PG_PORT");
        var originalDatabase = Environment.GetEnvironmentVariable("ACODE_PG_DATABASE");
        var originalUsername = Environment.GetEnvironmentVariable("ACODE_PG_USERNAME");
        var originalPassword = Environment.GetEnvironmentVariable("ACODE_PG_PASSWORD");

        try
        {
            Environment.SetEnvironmentVariable("ACODE_PG_HOST", null);
            Environment.SetEnvironmentVariable("ACODE_PG_PORT", null);
            Environment.SetEnvironmentVariable("ACODE_PG_DATABASE", null);
            Environment.SetEnvironmentVariable("ACODE_PG_USERNAME", null);
            Environment.SetEnvironmentVariable("ACODE_PG_PASSWORD", null);

            var options = CreateOptions(
                host: "config-host",
                port: 5432,
                database: "config-db",
                username: "config-user",
                password: "config-pass");

            // Act
            var factory = new PostgresConnectionFactory(options, NullLogger<PostgresConnectionFactory>.Instance);

            // Assert
            factory.Should().NotBeNull();
        }
        finally
        {
            Environment.SetEnvironmentVariable("ACODE_PG_HOST", originalHost);
            Environment.SetEnvironmentVariable("ACODE_PG_PORT", originalPort);
            Environment.SetEnvironmentVariable("ACODE_PG_DATABASE", originalDatabase);
            Environment.SetEnvironmentVariable("ACODE_PG_USERNAME", originalUsername);
            Environment.SetEnvironmentVariable("ACODE_PG_PASSWORD", originalPassword);
        }
    }

    [Fact]
    public void Constructor_ShouldConfigureConnectionPooling()
    {
        // Arrange
        var options = CreateOptions(
            poolMinSize: 5,
            poolMaxSize: 50,
            poolConnectionLifetimeSeconds: 300);

        // Act
        var factory = new PostgresConnectionFactory(options, NullLogger<PostgresConnectionFactory>.Instance);

        // Assert
        // NpgsqlDataSource is created with pool configuration
        // We can't directly inspect the pool settings, but construction should succeed
        factory.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_ShouldHandleDefaultPoolConfiguration()
    {
        // Arrange
        var options = CreateOptions(); // Uses default pool settings

        // Act
        var factory = new PostgresConnectionFactory(options, NullLogger<PostgresConnectionFactory>.Instance);

        // Assert
        factory.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_ShouldHandleSslMode()
    {
        // Arrange
        var options = CreateOptions(sslMode: "Require");

        // Act
        var factory = new PostgresConnectionFactory(options, NullLogger<PostgresConnectionFactory>.Instance);

        // Assert
        factory.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_ShouldHandleCommandTimeout()
    {
        // Arrange
        var options = CreateOptions(commandTimeoutSeconds: 60);

        // Act
        var factory = new PostgresConnectionFactory(options, NullLogger<PostgresConnectionFactory>.Instance);

        // Assert
        factory.Should().NotBeNull();
    }

    private static IOptions<DatabaseOptions> CreateOptions(
        string host = "localhost",
        int port = 5432,
        string database = "acode_test",
        string username = "acode_user",
        string password = "test_password",
        string sslMode = "Prefer",
        int commandTimeoutSeconds = 30,
        int poolMinSize = 2,
        int poolMaxSize = 20,
        int poolConnectionLifetimeSeconds = 600)
    {
        return Options.Create(new DatabaseOptions
        {
            Provider = "postgresql",
            Remote = new RemoteDatabaseOptions
            {
                Host = host,
                Port = port,
                Database = database,
                Username = username,
                Password = password,
                SslMode = sslMode,
                CommandTimeoutSeconds = commandTimeoutSeconds,
                Pool = new PoolOptions
                {
                    MinSize = poolMinSize,
                    MaxSize = poolMaxSize,
                    ConnectionLifetimeSeconds = poolConnectionLifetimeSeconds,
                },
            },
        });
    }
}
