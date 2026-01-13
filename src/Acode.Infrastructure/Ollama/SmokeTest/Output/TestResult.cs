namespace Acode.Infrastructure.Ollama.SmokeTest.Output;

/// <summary>
/// Result of a single smoke test.
/// </summary>
/// <remarks>
/// FR-071 to FR-075: Test results include name, pass/fail, timing, and diagnostic info.
/// </remarks>
public sealed record TestResult
{
    /// <summary>
    /// Gets the name of the test.
    /// </summary>
    public required string TestName { get; init; }

    /// <summary>
    /// Gets a value indicating whether the test passed.
    /// </summary>
    public required bool Passed { get; init; }

    /// <summary>
    /// Gets the elapsed time for the test.
    /// </summary>
    public required TimeSpan ElapsedTime { get; init; }

    /// <summary>
    /// Gets the error message if the test failed.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Gets the diagnostic hint to help resolve the failure.
    /// </summary>
    public string? DiagnosticHint { get; init; }
}

/// <summary>
/// Aggregate results of all smoke tests.
/// </summary>
/// <remarks>
/// FR-073: Summary shows total results and timing.
/// </remarks>
public sealed record SmokeTestResults
{
    /// <summary>
    /// Gets the list of individual test results.
    /// </summary>
    public required List<TestResult> Results { get; init; }

    /// <summary>
    /// Gets the timestamp when tests were run.
    /// </summary>
    public required DateTime CheckedAt { get; init; }

    /// <summary>
    /// Gets the total duration of all tests.
    /// </summary>
    public required TimeSpan TotalDuration { get; init; }

    /// <summary>
    /// Gets a value indicating whether all tests passed.
    /// </summary>
    public bool AllPassed => Results.All(r => r.Passed);

    /// <summary>
    /// Gets the count of tests that passed.
    /// </summary>
    public int PassedCount => Results.Count(r => r.Passed);

    /// <summary>
    /// Gets the count of tests that failed.
    /// </summary>
    public int FailedCount => Results.Count(r => !r.Passed);
}
