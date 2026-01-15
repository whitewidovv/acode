namespace Acode.Infrastructure.Ollama.SmokeTest.Output;

/// <summary>
/// Interface for formatting and outputting smoke test results.
/// </summary>
/// <remarks>
/// FR-076, FR-077: Support multiple output formats (text, JSON, quiet mode).
/// </remarks>
public interface ITestReporter
{
    /// <summary>
    /// Reports the test results to the specified output writer.
    /// </summary>
    /// <param name="results">The test results to report.</param>
    /// <param name="output">The output writer.</param>
    void Report(SmokeTestResults results, TextWriter output);
}
