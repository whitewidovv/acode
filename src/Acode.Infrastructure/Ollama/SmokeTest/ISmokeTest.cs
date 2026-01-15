namespace Acode.Infrastructure.Ollama.SmokeTest;

using Acode.Infrastructure.Ollama.SmokeTest.Output;

/// <summary>
/// Interface for individual smoke tests.
/// </summary>
public interface ISmokeTest
{
    /// <summary>
    /// Gets the name of the test.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Runs the smoke test asynchronously.
    /// </summary>
    /// <param name="options">Test configuration options.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The test result.</returns>
    Task<TestResult> RunAsync(SmokeTestOptions options, CancellationToken cancellationToken);
}

/// <summary>
/// Configuration options for smoke tests.
/// </summary>
/// <remarks>
/// FR-082 to FR-087: Test configuration supports endpoint, model, timeout, and test skipping.
/// </remarks>
public sealed record SmokeTestOptions
{
    /// <summary>
    /// Gets the Ollama endpoint URL.
    /// </summary>
    public required string Endpoint { get; init; }

    /// <summary>
    /// Gets the model to use for testing.
    /// </summary>
    public required string Model { get; init; }

    /// <summary>
    /// Gets the timeout for requests.
    /// </summary>
    public TimeSpan Timeout { get; init; } = TimeSpan.FromSeconds(60);

    /// <summary>
    /// Gets a value indicating whether to skip the tool calling test.
    /// </summary>
    public bool SkipToolTest { get; init; } = false;
}
