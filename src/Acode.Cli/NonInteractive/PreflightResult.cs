// <copyright file="PreflightResult.cs" company="Acode">
// Copyright (c) Acode. All rights reserved.
// </copyright>

namespace Acode.Cli.NonInteractive;

/// <summary>
/// Result of all pre-flight checks.
/// </summary>
public sealed class PreflightResult
{
    private readonly List<PreflightCheckResult> _results = [];

    /// <summary>
    /// Gets a value indicating whether all checks passed.
    /// </summary>
    public bool AllPassed => _results.Count > 0 && _results.All(r => r.Passed);

    /// <summary>
    /// Gets the failed check results.
    /// </summary>
    public IReadOnlyList<PreflightCheckResult> Failures => _results.Where(r => !r.Passed).ToList();

    /// <summary>
    /// Gets all check results.
    /// </summary>
    public IReadOnlyList<PreflightCheckResult> AllResults => _results;

    /// <summary>
    /// Adds a check result.
    /// </summary>
    /// <param name="result">The result to add.</param>
    public void AddResult(PreflightCheckResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        _results.Add(result);
    }

    /// <summary>
    /// Gets a summary message for all failures.
    /// </summary>
    /// <returns>The summary message.</returns>
    public string GetFailureSummary()
    {
        if (AllPassed)
        {
            return "All pre-flight checks passed.";
        }

        var failures = Failures;
        var messages = failures.Select(f => $"- {f.CheckName}: {f.Message}");
        return $"Pre-flight checks failed ({failures.Count} issue(s)):\n{string.Join('\n', messages)}";
    }
}
