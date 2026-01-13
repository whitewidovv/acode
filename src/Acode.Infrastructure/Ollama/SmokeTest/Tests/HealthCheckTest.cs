namespace Acode.Infrastructure.Ollama.SmokeTest.Tests;

using System.Diagnostics;
using System.Net.Http;
using Acode.Infrastructure.Ollama.SmokeTest.Output;

/// <summary>
/// Health check test - verifies Ollama connectivity.
/// </summary>
/// <remarks>
/// FR-060, FR-061: Calls /api/tags endpoint with 5 second timeout.
/// </remarks>
public sealed class HealthCheckTest : ISmokeTest
{
    private readonly HttpClient httpClient;

    /// <summary>
    /// Initializes a new instance of the <see cref="HealthCheckTest"/> class.
    /// </summary>
    /// <param name="httpClient">HTTP client for making requests.</param>
    public HealthCheckTest(HttpClient? httpClient = null)
    {
        this.httpClient = httpClient ?? new HttpClient();
    }

    /// <inheritdoc/>
    public string Name => "Health Check";

    /// <inheritdoc/>
    public async Task<TestResult> RunAsync(SmokeTestOptions options, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(options);

        var stopwatch = Stopwatch.StartNew();

        try
        {
            // FR-061: 5 second timeout for health check
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(5));

            var endpoint = $"{options.Endpoint.TrimEnd('/')}/api/tags";
            var response = await this.httpClient.GetAsync(endpoint, cts.Token).ConfigureAwait(false);

            stopwatch.Stop();

            if (response.IsSuccessStatusCode)
            {
                return new TestResult
                {
                    TestName = this.Name,
                    Passed = true,
                    ElapsedTime = stopwatch.Elapsed
                };
            }

            return new TestResult
            {
                TestName = this.Name,
                Passed = false,
                ElapsedTime = stopwatch.Elapsed,
                ErrorMessage = $"HTTP {(int)response.StatusCode}: {response.ReasonPhrase}",
                DiagnosticHint = "Verify Ollama is running with 'ollama serve'"
            };
        }
        catch (HttpRequestException ex)
        {
            stopwatch.Stop();
            return new TestResult
            {
                TestName = this.Name,
                Passed = false,
                ElapsedTime = stopwatch.Elapsed,
                ErrorMessage = $"Connection failed: {ex.Message}",
                DiagnosticHint = $"1. Start Ollama: ollama serve\n" +
                                $"      2. Check endpoint: {options.Endpoint}\n" +
                                $"      3. Test manually: curl {options.Endpoint}/api/tags"
            };
        }
        catch (TaskCanceledException)
        {
            stopwatch.Stop();
            return new TestResult
            {
                TestName = this.Name,
                Passed = false,
                ElapsedTime = stopwatch.Elapsed,
                ErrorMessage = "Request timed out after 5 seconds",
                DiagnosticHint = "Ollama server may be overloaded or unreachable"
            };
        }
    }
}
