namespace Acode.Infrastructure.Ollama.SmokeTest.Output;

using System.Text.Json;

/// <summary>
/// Formats test results as JSON for machine parsing.
/// </summary>
/// <remarks>
/// FR-076: JSON output option for parsing in CI/CD pipelines.
/// </remarks>
public sealed class JsonTestReporter : ITestReporter
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    /// <inheritdoc/>
    public void Report(SmokeTestResults results, TextWriter output)
    {
        ArgumentNullException.ThrowIfNull(results);
        ArgumentNullException.ThrowIfNull(output);

        // Convert to a serializable format
        var jsonOutput = new
        {
            checkedAt = results.CheckedAt,
            totalDurationMs = results.TotalDuration.TotalMilliseconds,
            allPassed = results.AllPassed,
            passedCount = results.PassedCount,
            failedCount = results.FailedCount,
            results = results.Results.Select(r => new
            {
                testName = r.TestName,
                passed = r.Passed,
                elapsedTimeMs = r.ElapsedTime.TotalMilliseconds,
                errorMessage = r.ErrorMessage,
                diagnosticHint = r.DiagnosticHint
            }).ToList()
        };

        var json = JsonSerializer.Serialize(jsonOutput, SerializerOptions);
        output.WriteLine(json);
    }
}
