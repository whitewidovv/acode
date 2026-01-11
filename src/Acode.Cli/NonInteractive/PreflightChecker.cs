// <copyright file="PreflightChecker.cs" company="Acode">
// Copyright (c) Acode. All rights reserved.
// </copyright>

using Microsoft.Extensions.Logging;

namespace Acode.Cli.NonInteractive;

/// <summary>
/// Runs pre-flight checks before command execution.
/// </summary>
/// <remarks>
/// FR-071 through FR-076: Pre-flight check requirements.
/// </remarks>
public sealed class PreflightChecker : IPreflightChecker
{
    private readonly List<IPreflightCheck> _checks = [];
    private readonly ILogger<PreflightChecker>? _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="PreflightChecker"/> class.
    /// </summary>
    /// <param name="logger">Optional logger for check events.</param>
    public PreflightChecker(ILogger<PreflightChecker>? logger = null)
    {
        _logger = logger;
    }

    /// <inheritdoc/>
    public void AddCheck(IPreflightCheck check)
    {
        ArgumentNullException.ThrowIfNull(check);
        _checks.Add(check);
    }

    /// <inheritdoc/>
    /// <remarks>
    /// FR-075: Pre-flight MUST list all failures at once.
    /// </remarks>
    public async Task<PreflightResult> RunAllChecksAsync(
        CancellationToken cancellationToken = default
    )
    {
        var result = new PreflightResult();

        if (_checks.Count == 0)
        {
            _logger?.LogDebug("No pre-flight checks registered");
            return result;
        }

        _logger?.LogInformation("Running {Count} pre-flight check(s)", _checks.Count);

        foreach (var check in _checks)
        {
            cancellationToken.ThrowIfCancellationRequested();

            _logger?.LogDebug("Running pre-flight check: {CheckName}", check.Name);

            try
            {
                var checkResult = await check.RunAsync(cancellationToken).ConfigureAwait(false);
                result.AddResult(checkResult);

                if (checkResult.Passed)
                {
                    _logger?.LogDebug("Pre-flight check passed: {CheckName}", check.Name);
                }
                else
                {
                    _logger?.LogWarning(
                        "Pre-flight check failed: {CheckName} - {Message}",
                        check.Name,
                        checkResult.Message
                    );
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger?.LogError(ex, "Pre-flight check threw exception: {CheckName}", check.Name);

                result.AddResult(
                    new PreflightCheckResult(
                        check.Name,
                        Passed: false,
                        $"Check threw exception: {ex.Message}"
                    )
                );
            }
        }

        if (result.AllPassed)
        {
            _logger?.LogInformation("All pre-flight checks passed");
        }
        else
        {
            _logger?.LogError(
                "Pre-flight checks failed: {FailureCount} of {TotalCount} checks failed",
                result.Failures.Count,
                result.AllResults.Count
            );
        }

        return result;
    }
}
