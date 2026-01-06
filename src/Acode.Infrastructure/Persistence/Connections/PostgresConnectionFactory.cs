// src/Acode.Infrastructure/Persistence/Connections/PostgresConnectionFactory.cs
namespace Acode.Infrastructure.Persistence.Connections;

using System.Data;
using System.Diagnostics;
using Acode.Application.Interfaces.Persistence;
using Acode.Domain.Enums;
using Acode.Domain.Exceptions;
using Acode.Infrastructure.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;

/// <summary>
/// Connection factory for PostgreSQL database with connection pooling.
/// Handles connection string building from config and environment variables.
/// </summary>
public sealed class PostgresConnectionFactory : IConnectionFactory
{
    private readonly NpgsqlDataSource _dataSource;
    private readonly ILogger<PostgresConnectionFactory> _logger;
    private readonly string _maskedConnectionString;

    /// <summary>
    /// Initializes a new instance of the <see cref="PostgresConnectionFactory"/> class.
    /// </summary>
    /// <param name="options">Database configuration options.</param>
    /// <param name="logger">Logger instance.</param>
    public PostgresConnectionFactory(
        IOptions<DatabaseOptions> options,
        ILogger<PostgresConnectionFactory> logger)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        _logger = logger;

        var remoteOptions = options.Value.Remote;
        var connectionString = BuildConnectionString(remoteOptions);
        _maskedConnectionString = MaskPassword(connectionString);

        var builder = new NpgsqlDataSourceBuilder(connectionString);

        _dataSource = builder.Build();

        _logger.LogInformation(
            "PostgreSQL data source initialized. Host={Host}, Database={Database}, Pool=[{Min}-{Max}]",
            remoteOptions.Host,
            remoteOptions.Database,
            remoteOptions.Pool.MinSize,
            remoteOptions.Pool.MaxSize);
    }

    /// <inheritdoc/>
    public DatabaseType DatabaseType => DatabaseType.Postgres;

    /// <inheritdoc/>
    public async Task<IDbConnection> CreateAsync(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var sw = Stopwatch.StartNew();

        try
        {
            var connection = await _dataSource.OpenConnectionAsync(ct).ConfigureAwait(false);

            sw.Stop();

            if (sw.ElapsedMilliseconds > 100)
            {
                _logger.LogWarning(
                    "Slow PostgreSQL pool acquisition. Duration={Duration}ms, Threshold=100ms",
                    sw.ElapsedMilliseconds);
            }
            else
            {
                _logger.LogDebug(
                    "PostgreSQL connection acquired from pool. Duration={Duration}ms",
                    sw.ElapsedMilliseconds);
            }

            return connection;
        }
        catch (NpgsqlException ex) when (IsPoolExhausted(ex))
        {
            sw.Stop();
            _logger.LogError(
                ex,
                "PostgreSQL pool exhausted. Duration={Duration}ms",
                sw.ElapsedMilliseconds);

            throw DatabaseException.PoolExhausted(sw.Elapsed);
        }
        catch (NpgsqlException ex)
        {
            sw.Stop();
            _logger.LogError(
                ex,
                "PostgreSQL connection failed. ConnectionString={ConnectionString}, Duration={Duration}ms",
                _maskedConnectionString,
                sw.ElapsedMilliseconds);

            throw DatabaseException.ConnectionFailed(ex.Message, ex);
        }
    }

    private static string BuildConnectionString(RemoteDatabaseOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (!string.IsNullOrEmpty(options.ConnectionString))
        {
            return options.ConnectionString;
        }

        // Check environment variables (highest priority)
        var envConnectionString = Environment.GetEnvironmentVariable("ACODE_PG_CONNECTION");
        if (!string.IsNullOrEmpty(envConnectionString))
        {
            return envConnectionString;
        }

        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = Environment.GetEnvironmentVariable("ACODE_PG_HOST") ?? options.Host,
            Port = int.TryParse(Environment.GetEnvironmentVariable("ACODE_PG_PORT"), out var port)
                ? port
                : options.Port,
            Database = Environment.GetEnvironmentVariable("ACODE_PG_DATABASE") ?? options.Database,
            Username = Environment.GetEnvironmentVariable("ACODE_PG_USER") ?? options.Username,
            Password = Environment.GetEnvironmentVariable("ACODE_PG_PASSWORD") ?? options.Password,
            SslMode = ParseSslMode(options.SslMode),
            CommandTimeout = options.CommandTimeoutSeconds,
            MinPoolSize = options.Pool.MinSize,
            MaxPoolSize = options.Pool.MaxSize,
            ConnectionLifetime = options.Pool.ConnectionLifetimeSeconds,
            Timeout = options.Pool.AcquisitionTimeoutSeconds,
            ApplicationName = "Acode",
        };

        return builder.ToString();
    }

    private static SslMode ParseSslMode(string mode) => mode.ToLowerInvariant() switch
    {
        "disable" => SslMode.Disable,
        "prefer" => SslMode.Prefer,
        "require" => SslMode.Require,
        "verify-ca" => SslMode.VerifyCA,
        "verify-full" => SslMode.VerifyFull,
        _ => SslMode.Prefer,
    };

    private static bool IsPoolExhausted(NpgsqlException ex) =>
        ex.Message.Contains("pool", StringComparison.OrdinalIgnoreCase) &&
        ex.Message.Contains("exhaust", StringComparison.OrdinalIgnoreCase);

    private static string MaskPassword(string connectionString)
    {
        var builder = new NpgsqlConnectionStringBuilder(connectionString);
        if (!string.IsNullOrEmpty(builder.Password))
        {
            builder.Password = "***MASKED***";
        }

        return builder.ToString();
    }
}
