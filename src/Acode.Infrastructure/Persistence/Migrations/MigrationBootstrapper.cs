// src/Acode.Infrastructure/Persistence/Migrations/MigrationBootstrapper.cs
namespace Acode.Infrastructure.Persistence.Migrations;

using System;
using System.Threading;
using System.Threading.Tasks;
using Acode.Application.Database;
using Microsoft.Extensions.Logging;

/// <summary>
/// Orchestrates database migration checking and application during startup.
/// </summary>
public sealed class MigrationBootstrapper : IMigrationBootstrapper
{
    private readonly IMigrationLock _lock;
    private readonly IMigrationDiscovery _discovery;
    private readonly IMigrationValidator _validator;
    private readonly IMigrationExecutor _executor;
    private readonly IMigrationRepository _repository;
    private readonly MigrationBootstrapperOptions _options;
    private readonly ILogger<MigrationBootstrapper> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="MigrationBootstrapper"/> class.
    /// </summary>
    /// <param name="migrationLock">Migration lock for preventing concurrent migrations.</param>
    /// <param name="discovery">Migration discovery service.</param>
    /// <param name="validator">Migration validator.</param>
    /// <param name="executor">Migration executor.</param>
    /// <param name="repository">Migration repository.</param>
    /// <param name="options">Bootstrapper options.</param>
    /// <param name="logger">Logger instance.</param>
    public MigrationBootstrapper(
        IMigrationLock migrationLock,
        IMigrationDiscovery discovery,
        IMigrationValidator validator,
        IMigrationExecutor executor,
        IMigrationRepository repository,
        MigrationBootstrapperOptions options,
        ILogger<MigrationBootstrapper> logger)
    {
        ArgumentNullException.ThrowIfNull(migrationLock);
        ArgumentNullException.ThrowIfNull(discovery);
        ArgumentNullException.ThrowIfNull(validator);
        ArgumentNullException.ThrowIfNull(executor);
        ArgumentNullException.ThrowIfNull(repository);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        _lock = migrationLock;
        _discovery = discovery;
        _validator = validator;
        _executor = executor;
        _repository = repository;
        _options = options;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<BootstrapResult> BootstrapAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting database bootstrap");

        try
        {
            // Step 1: Acquire migration lock
            _logger.LogDebug("Attempting to acquire migration lock with timeout {Timeout}", _options.LockTimeout);
            var lockAcquired = await _lock.TryAcquireAsync(cancellationToken).ConfigureAwait(false);

            if (!lockAcquired)
            {
                var errorMessage = $"Failed to acquire migration lock within {_options.LockTimeout}";
                _logger.LogError(errorMessage);
                return new BootstrapResult
                {
                    Success = false,
                    PendingMigrationsCount = 0,
                    AppliedMigrationsCount = 0,
                    ErrorMessage = errorMessage
                };
            }

            try
            {
                _logger.LogDebug("Migration lock acquired successfully");

                // Step 2: Discover migrations
                _logger.LogDebug("Discovering migrations");
                var discoveredMigrations = await _discovery.DiscoverAsync(cancellationToken).ConfigureAwait(false);
                _logger.LogInformation("Discovered {Count} migrations", discoveredMigrations.Count);

                // Step 3: Get applied migrations
                _logger.LogDebug("Fetching applied migrations from repository");
                var appliedMigrations = await _repository.GetAppliedMigrationsAsync(cancellationToken).ConfigureAwait(false);
                _logger.LogInformation("Found {Count} applied migrations", appliedMigrations.Count);

                // Step 4: Validate migrations
                _logger.LogDebug("Validating migrations");
                var validationResult = await _validator.ValidateAsync(
                    discoveredMigrations,
                    appliedMigrations,
                    cancellationToken).ConfigureAwait(false);

                // Step 5: Check for validation errors
                if (validationResult.ChecksumMismatches.Count > 0)
                {
                    var errorMessage = $"Migration validation failed: {validationResult.ChecksumMismatches.Count} checksum mismatch(es) detected";
                    _logger.LogError(errorMessage);

                    foreach (var mismatch in validationResult.ChecksumMismatches)
                    {
                        _logger.LogError(
                            "Checksum mismatch for migration {Version}: expected {Expected}, actual {Actual}",
                            mismatch.Version,
                            mismatch.ExpectedChecksum,
                            mismatch.ActualChecksum);
                    }

                    return new BootstrapResult
                    {
                        Success = false,
                        PendingMigrationsCount = validationResult.PendingMigrations.Count,
                        AppliedMigrationsCount = 0,
                        ErrorMessage = errorMessage
                    };
                }

                if (validationResult.VersionGaps.Count > 0)
                {
                    var errorMessage = $"Migration validation failed: {validationResult.VersionGaps.Count} version gap(s) detected";
                    _logger.LogError(errorMessage);

                    foreach (var gap in validationResult.VersionGaps)
                    {
                        _logger.LogError(
                            "Version gap detected: missing version {Missing} between {Before} and {After}",
                            gap.MissingVersion,
                            gap.BeforeVersion,
                            gap.AfterVersion);
                    }

                    return new BootstrapResult
                    {
                        Success = false,
                        PendingMigrationsCount = validationResult.PendingMigrations.Count,
                        AppliedMigrationsCount = 0,
                        ErrorMessage = errorMessage
                    };
                }

                // Step 6: Apply pending migrations if AutoMigrate is enabled
                var pendingCount = validationResult.PendingMigrations.Count;
                _logger.LogInformation("Found {Count} pending migration(s)", pendingCount);

                if (pendingCount > 0 && _options.AutoMigrate)
                {
                    _logger.LogInformation("AutoMigrate is enabled. Applying {Count} pending migration(s)", pendingCount);

                    var appliedCount = 0;
                    foreach (var migration in validationResult.PendingMigrations)
                    {
                        _logger.LogInformation("Applying migration {Version}", migration.Version);
                        var executionResult = await _executor.ApplyAsync(migration, cancellationToken).ConfigureAwait(false);

                        if (!executionResult.Success)
                        {
                            var errorMessage = $"Failed to apply migration {migration.Version}: {executionResult.ErrorMessage}";
                            _logger.LogError(errorMessage);

                            return new BootstrapResult
                            {
                                Success = false,
                                PendingMigrationsCount = pendingCount,
                                AppliedMigrationsCount = appliedCount,
                                ErrorMessage = errorMessage,
                                Exception = executionResult.Exception
                            };
                        }

                        appliedCount++;
                        _logger.LogInformation(
                            "Successfully applied migration {Version} in {Duration}ms",
                            migration.Version,
                            executionResult.Duration.TotalMilliseconds);
                    }

                    _logger.LogInformation("All pending migrations applied successfully");

                    return new BootstrapResult
                    {
                        Success = true,
                        PendingMigrationsCount = pendingCount,
                        AppliedMigrationsCount = appliedCount
                    };
                }
                else if (pendingCount > 0)
                {
                    _logger.LogWarning("AutoMigrate is disabled. {Count} pending migration(s) were not applied", pendingCount);
                }
                else
                {
                    _logger.LogInformation("No pending migrations. Database is up to date");
                }

                return new BootstrapResult
                {
                    Success = true,
                    PendingMigrationsCount = pendingCount,
                    AppliedMigrationsCount = 0
                };
            }
            finally
            {
                // Step 7: Always release lock
                _logger.LogDebug("Releasing migration lock");
                await _lock.DisposeAsync().ConfigureAwait(false);
                _logger.LogDebug("Migration lock released");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during database bootstrap");
            return new BootstrapResult
            {
                Success = false,
                PendingMigrationsCount = 0,
                AppliedMigrationsCount = 0,
                ErrorMessage = $"Unexpected error: {ex.Message}",
                Exception = ex
            };
        }
    }
}
