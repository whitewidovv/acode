// src/Acode.Infrastructure/Health/Checks/DatabaseConnectivityCheck.cs
namespace Acode.Infrastructure.Health.Checks;

using System;
using System.Data;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Acode.Application.Health;

/// <summary>
/// Health check for database connectivity.
/// Executes a simple SELECT 1 query to verify the database responds.
/// </summary>
public sealed class DatabaseConnectivityCheck : IHealthCheck
{
    private readonly IDbConnection _connection;

    /// <summary>
    /// Initializes a new instance of the <see cref="DatabaseConnectivityCheck"/> class.
    /// </summary>
    /// <param name="connection">The database connection to check.</param>
    public DatabaseConnectivityCheck(IDbConnection connection)
    {
        ArgumentNullException.ThrowIfNull(connection);
        _connection = connection;
    }

    /// <inheritdoc/>
    public string Name => "Database Connectivity";

    /// <inheritdoc/>
    public async Task<HealthCheckResult> CheckAsync(CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            // Ensure connection is open
            if (_connection.State != ConnectionState.Open)
            {
                _connection.Open();
            }

            // Execute simple query
            using var command = _connection.CreateCommand();
            command.CommandText = "SELECT 1";
            var result = await Task.Run(() => command.ExecuteScalar(), cancellationToken).ConfigureAwait(false);

            stopwatch.Stop();

            if (result == null || (!result.Equals(1) && !result.Equals(1L)))
            {
                return HealthCheckResult.Unhealthy(
                    Name,
                    stopwatch.Elapsed,
                    "Database query returned unexpected result",
                    "DB_UNEXPECTED_RESULT",
                    "Verify database server is running and accessible");
            }

            return HealthCheckResult.Healthy(
                Name,
                stopwatch.Elapsed,
                "Database is responding to queries");
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            return HealthCheckResult.Unhealthy(
                Name,
                stopwatch.Elapsed,
                $"Database connectivity check failed: {ex.Message}",
                "DB_CONNECTION_FAILED",
                "Check database server status and network connectivity");
        }
    }
}
