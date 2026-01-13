namespace Acode.Infrastructure.Ollama.SmokeTest.Tests;

using System.Diagnostics;
using System.Net.Http;
using System.Text.Json;
using Acode.Infrastructure.Ollama.SmokeTest.Output;

/// <summary>
/// Model list test - verifies model enumeration works.
/// </summary>
/// <remarks>
/// FR-062, FR-063: Parses model response and verifies at least one model exists.
/// </remarks>
public sealed class ModelListTest : ISmokeTest
{
    private readonly HttpClient httpClient;

    /// <summary>
    /// Initializes a new instance of the <see cref="ModelListTest"/> class.
    /// </summary>
    /// <param name="httpClient">HTTP client for making requests.</param>
    public ModelListTest(HttpClient? httpClient = null)
    {
        this.httpClient = httpClient ?? new HttpClient();
    }

    /// <inheritdoc/>
    public string Name => "Model List";

    /// <inheritdoc/>
    public async Task<TestResult> RunAsync(SmokeTestOptions options, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(options);

        var stopwatch = Stopwatch.StartNew();

        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(options.Timeout);

            var endpoint = $"{options.Endpoint.TrimEnd('/')}/api/tags";
            var response = await this.httpClient.GetAsync(endpoint, cts.Token).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                stopwatch.Stop();
                return new TestResult
                {
                    TestName = this.Name,
                    Passed = false,
                    ElapsedTime = stopwatch.Elapsed,
                    ErrorMessage = $"HTTP {(int)response.StatusCode}: {response.ReasonPhrase}",
                    DiagnosticHint = "Ollama responded but returned an error"
                };
            }

            var content = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            var json = JsonDocument.Parse(content);

            // Parse models array
            if (!json.RootElement.TryGetProperty("models", out var modelsElement))
            {
                stopwatch.Stop();
                return new TestResult
                {
                    TestName = this.Name,
                    Passed = false,
                    ElapsedTime = stopwatch.Elapsed,
                    ErrorMessage = "Response missing 'models' property",
                    DiagnosticHint = "Ollama API response format unexpected"
                };
            }

            var modelCount = modelsElement.GetArrayLength();

            stopwatch.Stop();

            // FR-063: Verify at least one model exists
            if (modelCount == 0)
            {
                return new TestResult
                {
                    TestName = this.Name,
                    Passed = false,
                    ElapsedTime = stopwatch.Elapsed,
                    ErrorMessage = "No models available",
                    DiagnosticHint = $"Pull a model: ollama pull {options.Model}"
                };
            }

            return new TestResult
            {
                TestName = this.Name,
                Passed = true,
                ElapsedTime = stopwatch.Elapsed
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
                DiagnosticHint = "Verify Ollama is running and accessible"
            };
        }
        catch (JsonException ex)
        {
            stopwatch.Stop();
            return new TestResult
            {
                TestName = this.Name,
                Passed = false,
                ElapsedTime = stopwatch.Elapsed,
                ErrorMessage = $"Failed to parse response: {ex.Message}",
                DiagnosticHint = "Ollama returned invalid JSON"
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
                ErrorMessage = "Request timed out",
                DiagnosticHint = "Increase timeout or check Ollama performance"
            };
        }
    }
}
