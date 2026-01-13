namespace Acode.Infrastructure.Ollama.SmokeTest;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Acode.Infrastructure.Ollama.SmokeTest.Output;
using Acode.Infrastructure.Ollama.SmokeTest.Tests;

/// <summary>
/// Orchestrates execution of all Ollama smoke tests.
/// </summary>
/// <remarks>
/// FR-039 to FR-051: Runs health check, model list, completion, streaming, and tool call tests.
/// Stops execution if health check fails since subsequent tests require connectivity.
/// </remarks>
public sealed class OllamaSmokeTestRunner
{
    private readonly List<ISmokeTest> tests;

    /// <summary>
    /// Initializes a new instance of the <see cref="OllamaSmokeTestRunner"/> class.
    /// </summary>
    public OllamaSmokeTestRunner()
        : this(new List<ISmokeTest>
        {
            new HealthCheckTest(),
            new ModelListTest(),
            new CompletionTest(),
            new StreamingTest(),
            new ToolCallTest()
        })
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="OllamaSmokeTestRunner"/> class.
    /// </summary>
    /// <param name="tests">Test instances to run.</param>
    internal OllamaSmokeTestRunner(List<ISmokeTest> tests)
    {
        ArgumentNullException.ThrowIfNull(tests);
        this.tests = tests;
    }

    /// <summary>
    /// Runs all smoke tests sequentially.
    /// </summary>
    /// <param name="options">Test configuration options.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Aggregate test results.</returns>
    public async Task<SmokeTestResults> RunAsync(SmokeTestOptions options, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(options);

        var startTime = DateTime.UtcNow;
        var stopwatch = Stopwatch.StartNew();
        var results = new List<TestResult>();

        foreach (var test in this.tests)
        {
            // Skip tool test if requested
            if (test.Name == "Tool Calling" && options.SkipToolTest)
            {
                continue;
            }

            cancellationToken.ThrowIfCancellationRequested();

            var result = await test.RunAsync(options, cancellationToken).ConfigureAwait(false);
            results.Add(result);

            // Stop after health check failure since subsequent tests require connectivity
            if (test.Name == "Health Check" && !result.Passed)
            {
                break;
            }
        }

        stopwatch.Stop();

        return new SmokeTestResults
        {
            Results = results,
            CheckedAt = startTime,
            TotalDuration = stopwatch.Elapsed
        };
    }
}
