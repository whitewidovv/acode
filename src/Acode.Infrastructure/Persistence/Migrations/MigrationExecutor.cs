// src/Acode.Infrastructure/Persistence/Migrations/MigrationExecutor.cs
namespace Acode.Infrastructure.Persistence.Migrations;

using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Acode.Application.Database;
using Microsoft.Extensions.Logging;

/// <summary>
/// Executes database migrations with transaction support.
/// </summary>
public sealed class MigrationExecutor : IMigrationExecutor
{
    private readonly System.Data.IDbConnection _connection;
    private readonly IMigrationRepository _repository;
    private readonly ILogger<MigrationExecutor> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="MigrationExecutor"/> class.
    /// </summary>
    /// <param name="connection">Database connection.</param>
    /// <param name="repository">Migration repository.</param>
    /// <param name="logger">Logger instance.</param>
    public MigrationExecutor(
        System.Data.IDbConnection connection,
        IMigrationRepository repository,
        ILogger<MigrationExecutor> logger)
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentNullException.ThrowIfNull(repository);
        ArgumentNullException.ThrowIfNull(logger);

        _connection = connection;
        _repository = repository;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<MigrationExecutionResult> ApplyAsync(MigrationFile migration, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(migration);

        var stopwatch = Stopwatch.StartNew();

        using var transaction = _connection.BeginTransaction();
        try
        {
            _logger.LogInformation("Applying migration {Version}", migration.Version);

            // Execute the up script
            using var command = _connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = migration.UpContent;
            command.ExecuteNonQuery();

            stopwatch.Stop();

            // Record the migration in the history table
            var appliedMigration = new AppliedMigration
            {
                Version = migration.Version,
                Checksum = migration.Checksum,
                AppliedAt = DateTime.UtcNow,
                Duration = stopwatch.Elapsed,
                Status = MigrationStatus.Applied
            };

            await _repository.RecordMigrationAsync(appliedMigration, cancellationToken).ConfigureAwait(false);

            // Commit the transaction
            transaction.Commit();

            _logger.LogInformation("Successfully applied migration {Version} in {Duration}ms", migration.Version, stopwatch.ElapsedMilliseconds);

            return new MigrationExecutionResult
            {
                Success = true,
                Version = migration.Version,
                Duration = stopwatch.Elapsed
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Failed to apply migration {Version}", migration.Version);

            try
            {
                transaction.Rollback();
            }
            catch (Exception rollbackEx)
            {
                _logger.LogError(rollbackEx, "Failed to rollback transaction for migration {Version}", migration.Version);
            }

            return new MigrationExecutionResult
            {
                Success = false,
                Version = migration.Version,
                Duration = stopwatch.Elapsed,
                ErrorMessage = ex.Message,
                Exception = ex
            };
        }
    }

    /// <inheritdoc/>
    public async Task<MigrationExecutionResult> RollbackAsync(MigrationFile migration, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(migration);

        if (string.IsNullOrWhiteSpace(migration.DownContent))
        {
            _logger.LogWarning("Migration {Version} does not have a down script for rollback", migration.Version);
            return new MigrationExecutionResult
            {
                Success = false,
                Version = migration.Version,
                Duration = TimeSpan.Zero,
                ErrorMessage = $"Migration {migration.Version} does not have a down script for rollback"
            };
        }

        var stopwatch = Stopwatch.StartNew();

        using var transaction = _connection.BeginTransaction();
        try
        {
            _logger.LogInformation("Rolling back migration {Version}", migration.Version);

            // Execute the down script
            using var command = _connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = migration.DownContent;
            command.ExecuteNonQuery();

            stopwatch.Stop();

            // Remove the migration from the history table
            await _repository.RemoveMigrationAsync(migration.Version, cancellationToken).ConfigureAwait(false);

            // Commit the transaction
            transaction.Commit();

            _logger.LogInformation("Successfully rolled back migration {Version} in {Duration}ms", migration.Version, stopwatch.ElapsedMilliseconds);

            return new MigrationExecutionResult
            {
                Success = true,
                Version = migration.Version,
                Duration = stopwatch.Elapsed
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Failed to rollback migration {Version}", migration.Version);

            try
            {
                transaction.Rollback();
            }
            catch (Exception rollbackEx)
            {
                _logger.LogError(rollbackEx, "Failed to rollback transaction for migration {Version}", migration.Version);
            }

            return new MigrationExecutionResult
            {
                Success = false,
                Version = migration.Version,
                Duration = stopwatch.Elapsed,
                ErrorMessage = ex.Message,
                Exception = ex
            };
        }
    }
}
