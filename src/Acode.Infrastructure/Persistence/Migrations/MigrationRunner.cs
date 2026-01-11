// src/Acode.Infrastructure/Persistence/Migrations/MigrationRunner.cs
namespace Acode.Infrastructure.Persistence.Migrations;

using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Acode.Application.Database;
using Microsoft.Extensions.Logging;

/// <summary>
/// High-level migration orchestration service.
/// </summary>
public sealed class MigrationRunner : IMigrationRunner
{
    private readonly IMigrationLock _lock;
    private readonly IMigrationDiscovery _discovery;
    private readonly IMigrationValidator _validator;
    private readonly IMigrationExecutor _executor;
    private readonly IMigrationRepository _repository;
    private readonly ILogger<MigrationRunner> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="MigrationRunner"/> class.
    /// </summary>
    /// <param name="migrationLock">Migration lock.</param>
    /// <param name="discovery">Migration discovery.</param>
    /// <param name="validator">Migration validator.</param>
    /// <param name="executor">Migration executor.</param>
    /// <param name="repository">Migration repository.</param>
    /// <param name="logger">Logger instance.</param>
    public MigrationRunner(
        IMigrationLock migrationLock,
        IMigrationDiscovery discovery,
        IMigrationValidator validator,
        IMigrationExecutor executor,
        IMigrationRepository repository,
        ILogger<MigrationRunner> logger)
    {
        ArgumentNullException.ThrowIfNull(migrationLock);
        ArgumentNullException.ThrowIfNull(discovery);
        ArgumentNullException.ThrowIfNull(validator);
        ArgumentNullException.ThrowIfNull(executor);
        ArgumentNullException.ThrowIfNull(repository);
        ArgumentNullException.ThrowIfNull(logger);

        _lock = migrationLock;
        _discovery = discovery;
        _validator = validator;
        _executor = executor;
        _repository = repository;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<MigrationResult> MigrateAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting migration operation");

        try
        {
            // Acquire lock
            var lockAcquired = await _lock.TryAcquireAsync(cancellationToken).ConfigureAwait(false);
            if (!lockAcquired)
            {
                var errorMessage = "Failed to acquire migration lock";
                _logger.LogError(errorMessage);
                return new MigrationResult
                {
                    Success = false,
                    AppliedCount = 0,
                    TotalDuration = TimeSpan.Zero,
                    ErrorMessage = errorMessage
                };
            }

            try
            {
                // Discover and validate
                var discovered = await _discovery.DiscoverAsync(cancellationToken).ConfigureAwait(false);
                var applied = await _repository.GetAppliedMigrationsAsync(cancellationToken).ConfigureAwait(false);
                var validation = await _validator.ValidateAsync(discovered, applied, cancellationToken).ConfigureAwait(false);

                if (!validation.IsValid)
                {
                    var errorMessage = $"Validation failed: {validation.VersionGaps.Count} version gap(s)";
                    _logger.LogError(errorMessage);
                    return new MigrationResult
                    {
                        Success = false,
                        AppliedCount = 0,
                        TotalDuration = TimeSpan.Zero,
                        ErrorMessage = errorMessage
                    };
                }

                // Apply pending migrations
                var stopwatch = Stopwatch.StartNew();
                var appliedCount = 0;

                foreach (var migration in validation.PendingMigrations)
                {
                    _logger.LogInformation("Applying migration {Version}", migration.Version);
                    var result = await _executor.ApplyAsync(migration, cancellationToken).ConfigureAwait(false);

                    if (!result.Success)
                    {
                        stopwatch.Stop();
                        return new MigrationResult
                        {
                            Success = false,
                            AppliedCount = appliedCount,
                            TotalDuration = stopwatch.Elapsed,
                            ErrorMessage = result.ErrorMessage,
                            Exception = result.Exception
                        };
                    }

                    appliedCount++;
                }

                stopwatch.Stop();
                _logger.LogInformation("Migration completed successfully. Applied {Count} migration(s)", appliedCount);

                return new MigrationResult
                {
                    Success = true,
                    AppliedCount = appliedCount,
                    TotalDuration = stopwatch.Elapsed
                };
            }
            finally
            {
                await _lock.DisposeAsync().ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during migration");
            return new MigrationResult
            {
                Success = false,
                AppliedCount = 0,
                TotalDuration = TimeSpan.Zero,
                ErrorMessage = ex.Message,
                Exception = ex
            };
        }
    }

    /// <inheritdoc/>
    public async Task<RollbackResult> RollbackAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting rollback operation");

        try
        {
            // Acquire lock
            var lockAcquired = await _lock.TryAcquireAsync(cancellationToken).ConfigureAwait(false);
            if (!lockAcquired)
            {
                var errorMessage = "Failed to acquire migration lock";
                _logger.LogError(errorMessage);
                return new RollbackResult
                {
                    Success = false,
                    RolledBackCount = 0,
                    TotalDuration = TimeSpan.Zero,
                    ErrorMessage = errorMessage
                };
            }

            try
            {
                // Get the last applied migration
                var applied = await _repository.GetAppliedMigrationsAsync(cancellationToken).ConfigureAwait(false);
                if (applied.Count == 0)
                {
                    var errorMessage = "No applied migrations to roll back";
                    _logger.LogWarning(errorMessage);
                    return new RollbackResult
                    {
                        Success = false,
                        RolledBackCount = 0,
                        TotalDuration = TimeSpan.Zero,
                        ErrorMessage = errorMessage.ToLowerInvariant()
                    };
                }

                var lastMigration = applied.OrderByDescending(m => m.AppliedAt).First();

                // Find the migration file
                var discovered = await _discovery.DiscoverAsync(cancellationToken).ConfigureAwait(false);
                var migrationFile = discovered.FirstOrDefault(m => m.Version == lastMigration.Version);

                if (migrationFile == null)
                {
                    var errorMessage = $"Migration file for version {lastMigration.Version} not found";
                    _logger.LogError(errorMessage);
                    return new RollbackResult
                    {
                        Success = false,
                        RolledBackCount = 0,
                        TotalDuration = TimeSpan.Zero,
                        ErrorMessage = errorMessage
                    };
                }

                // Rollback
                var stopwatch = Stopwatch.StartNew();
                _logger.LogInformation("Rolling back migration {Version}", migrationFile.Version);
                var result = await _executor.RollbackAsync(migrationFile, cancellationToken).ConfigureAwait(false);
                stopwatch.Stop();

                if (!result.Success)
                {
                    return new RollbackResult
                    {
                        Success = false,
                        RolledBackCount = 0,
                        TotalDuration = stopwatch.Elapsed,
                        ErrorMessage = result.ErrorMessage,
                        Exception = result.Exception
                    };
                }

                _logger.LogInformation("Rollback completed successfully");

                return new RollbackResult
                {
                    Success = true,
                    RolledBackCount = 1,
                    TotalDuration = stopwatch.Elapsed
                };
            }
            finally
            {
                await _lock.DisposeAsync().ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during rollback");
            return new RollbackResult
            {
                Success = false,
                RolledBackCount = 0,
                TotalDuration = TimeSpan.Zero,
                ErrorMessage = ex.Message,
                Exception = ex
            };
        }
    }

    /// <inheritdoc/>
    public async Task<MigrationState> GetStatusAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting migration status");

        var discovered = await _discovery.DiscoverAsync(cancellationToken).ConfigureAwait(false);
        var applied = await _repository.GetAppliedMigrationsAsync(cancellationToken).ConfigureAwait(false);
        var validation = await _validator.ValidateAsync(discovered, applied, cancellationToken).ConfigureAwait(false);

        return new MigrationState
        {
            AppliedMigrations = applied,
            PendingMigrations = validation.PendingMigrations,
            ChecksumMismatches = validation.ChecksumMismatches,
            VersionGaps = validation.VersionGaps
        };
    }

    /// <inheritdoc/>
    public async Task<ValidationResult> ValidateAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Validating migrations");

        var discovered = await _discovery.DiscoverAsync(cancellationToken).ConfigureAwait(false);
        var applied = await _repository.GetAppliedMigrationsAsync(cancellationToken).ConfigureAwait(false);
        var validation = await _validator.ValidateAsync(discovered, applied, cancellationToken).ConfigureAwait(false);

        return validation;
    }
}
