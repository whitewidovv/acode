namespace Acode.Infrastructure.Ollama.SmokeTest.Tests;

using System.Diagnostics;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using Acode.Infrastructure.Ollama.SmokeTest.Output;

/// <summary>
/// Completion test - verifies non-streaming inference works.
/// </summary>
/// <remarks>
/// FR-064, FR-065, FR-066: Sends simple prompt, verifies response and finish reason.
/// </remarks>
public sealed class CompletionTest : ISmokeTest
{
    private readonly HttpClient httpClient;

    /// <summary>
    /// Initializes a new instance of the <see cref="CompletionTest"/> class.
    /// </summary>
    /// <param name="httpClient">HTTP client for making requests.</param>
    public CompletionTest(HttpClient? httpClient = null)
    {
        this.httpClient = httpClient ?? new HttpClient();
    }

    /// <inheritdoc/>
    public string Name => "Non-Streaming Completion";

    /// <inheritdoc/>
    public async Task<TestResult> RunAsync(SmokeTestOptions options, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(options);

        var stopwatch = Stopwatch.StartNew();

        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(options.Timeout);

            var endpoint = $"{options.Endpoint.TrimEnd('/')}/api/generate";

            // FR-064: Send simple prompt
            var requestBody = new
            {
                model = options.Model,
                prompt = "What is 2+2? Answer with just the number.",
                stream = false
            };

            var json = JsonSerializer.Serialize(requestBody);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await this.httpClient.PostAsync(endpoint, content, cts.Token).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                stopwatch.Stop();
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                return new TestResult
                {
                    TestName = this.Name,
                    Passed = false,
                    ElapsedTime = stopwatch.Elapsed,
                    ErrorMessage = $"HTTP {(int)response.StatusCode}: {errorContent}",
                    DiagnosticHint = response.StatusCode == System.Net.HttpStatusCode.NotFound
                        ? $"Model '{options.Model}' not found. Pull it with: ollama pull {options.Model}"
                        : "Check Ollama logs for details"
                };
            }

            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            using var responseJson = JsonDocument.Parse(responseContent);

            stopwatch.Stop();

            // FR-065: Verify non-empty response
            if (!responseJson.RootElement.TryGetProperty("response", out var responseText))
            {
                return new TestResult
                {
                    TestName = this.Name,
                    Passed = false,
                    ElapsedTime = stopwatch.Elapsed,
                    ErrorMessage = "Response missing 'response' property",
                    DiagnosticHint = "Ollama API response format unexpected"
                };
            }

            var text = responseText.GetString();
            if (string.IsNullOrWhiteSpace(text))
            {
                return new TestResult
                {
                    TestName = this.Name,
                    Passed = false,
                    ElapsedTime = stopwatch.Elapsed,
                    ErrorMessage = "Model returned empty response",
                    DiagnosticHint = "Model may not be loaded properly"
                };
            }

            // FR-066: Verify finish reason (done field should be true)
            if (!responseJson.RootElement.TryGetProperty("done", out var doneElement) || !doneElement.GetBoolean())
            {
                return new TestResult
                {
                    TestName = this.Name,
                    Passed = false,
                    ElapsedTime = stopwatch.Elapsed,
                    ErrorMessage = "Response indicates generation not complete",
                    DiagnosticHint = "Generation may have been interrupted"
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
                DiagnosticHint = "Model may be loading. Increase timeout or wait for model to warm up"
            };
        }
    }
}
