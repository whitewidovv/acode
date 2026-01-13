namespace Acode.Infrastructure.Ollama.SmokeTest.Output;

/// <summary>
/// Formats test results as human-readable text output.
/// </summary>
/// <remarks>
/// FR-071 to FR-075: Displays test name, result, timing, and diagnostic information.
/// FR-077: Supports quiet mode for CI environments.
/// </remarks>
public sealed class TextTestReporter : ITestReporter
{
    private readonly bool verbose;

    /// <summary>
    /// Initializes a new instance of the <see cref="TextTestReporter"/> class.
    /// </summary>
    /// <param name="verbose">Whether to show verbose output.</param>
    public TextTestReporter(bool verbose = false)
    {
        this.verbose = verbose;
    }

    /// <inheritdoc/>
    public void Report(SmokeTestResults results, TextWriter output)
    {
        ArgumentNullException.ThrowIfNull(results);
        ArgumentNullException.ThrowIfNull(output);

        // Header
        output.WriteLine();
        output.WriteLine("Ollama Provider Smoke Test");
        output.WriteLine("===========================");
        output.WriteLine();

        if (this.verbose)
        {
            output.WriteLine($"Started: {results.CheckedAt:yyyy-MM-dd HH:mm:ss} UTC");
            output.WriteLine();
        }

        // Individual test results
        for (int i = 0; i < results.Results.Count; i++)
        {
            var test = results.Results[i];
            var number = i + 1;
            var total = results.Results.Count;

            if (test.Passed)
            {
                output.WriteLine($"[{number}/{total}] {test.TestName,-25} PASS ({FormatDuration(test.ElapsedTime)})");
            }
            else
            {
                output.WriteLine($"[{number}/{total}] {test.TestName,-25} FAIL");
                if (!string.IsNullOrEmpty(test.ErrorMessage))
                {
                    output.WriteLine($"      Error: {test.ErrorMessage}");
                }

                if (!string.IsNullOrEmpty(test.DiagnosticHint))
                {
                    output.WriteLine();
                    output.WriteLine("      Possible solution:");
                    output.WriteLine($"      {test.DiagnosticHint}");
                }

                output.WriteLine();
            }
        }

        // Summary
        output.WriteLine("===========================");

        if (results.AllPassed)
        {
            output.WriteLine($"All tests passed ({results.PassedCount}/{results.Results.Count})");
        }
        else
        {
            output.WriteLine($"Tests failed ({results.PassedCount}/{results.Results.Count}, {results.FailedCount} failed)");
        }

        output.WriteLine($"Total time: {FormatDuration(results.TotalDuration)}");
        output.WriteLine();
    }

    private static string FormatDuration(TimeSpan duration)
    {
        if (duration.TotalSeconds >= 1)
        {
            return $"{duration.TotalSeconds:F1}s";
        }

        return $"{duration.TotalMilliseconds:F0}ms";
    }
}
