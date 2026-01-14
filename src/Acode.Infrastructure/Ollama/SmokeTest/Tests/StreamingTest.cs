namespace Acode.Infrastructure.Ollama.SmokeTest.Tests;

using System.Diagnostics;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using Acode.Infrastructure.Ollama.SmokeTest.Output;

/// <summary>
/// Streaming test - verifies streaming inference works.
/// </summary>
/// <remarks>
/// FR-067, FR-068: Verifies multiple chunks received and final chunk indicates completion.
/// </remarks>
public sealed class StreamingTest : ISmokeTest
{
    private readonly HttpClient httpClient;

    /// <summary>
    /// Initializes a new instance of the <see cref="StreamingTest"/> class.
    /// </summary>
    /// <param name="httpClient">HTTP client for making requests.</param>
    public StreamingTest(HttpClient? httpClient = null)
    {
        if (httpClient is null)
        {
            // Create our own HttpClient with infinite timeout for streaming
            // (we handle timeouts manually via CancellationTokenSource)
            var client = new HttpClient();
            client.Timeout = Timeout.InfiniteTimeSpan;
            this.httpClient = client;
        }
        else
        {
            // Use injected client without modifying its settings
            this.httpClient = httpClient;
        }
    }

    /// <inheritdoc/>
    public string Name => "Streaming Completion";

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

            var requestBody = new
            {
                model = options.Model,
                prompt = "Count from 1 to 3.",
                stream = true
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            using var request = new HttpRequestMessage(HttpMethod.Post, endpoint) { Content = content };
            var response = await this.httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cts.Token).ConfigureAwait(false);

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

            // Read stream chunks
            var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            using var reader = new StreamReader(stream);

            int chunkCount = 0;
            bool receivedFinalChunk = false;

            while (!reader.EndOfStream)
            {
                var line = await reader.ReadLineAsync(cts.Token).ConfigureAwait(false);
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                chunkCount++;

                // Parse chunk to check for done field
                try
                {
                    using var chunkJson = JsonDocument.Parse(line);
                    if (chunkJson.RootElement.TryGetProperty("done", out var doneElement) && doneElement.GetBoolean())
                    {
                        receivedFinalChunk = true;
                        break;
                    }
                }
                catch (JsonException)
                {
                    // Continue if we can't parse a chunk
                    continue;
                }
            }

            stopwatch.Stop();

            // FR-067: Verify multiple chunks received
            if (chunkCount == 0)
            {
                return new TestResult
                {
                    TestName = this.Name,
                    Passed = false,
                    ElapsedTime = stopwatch.Elapsed,
                    ErrorMessage = "No streaming chunks received",
                    DiagnosticHint = "Streaming may not be properly configured"
                };
            }

            // FR-068: Verify final chunk indicates completion
            if (!receivedFinalChunk)
            {
                return new TestResult
                {
                    TestName = this.Name,
                    Passed = false,
                    ElapsedTime = stopwatch.Elapsed,
                    ErrorMessage = "Did not receive final chunk with done=true",
                    DiagnosticHint = "Stream may have been interrupted"
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
