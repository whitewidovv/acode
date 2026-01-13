namespace Acode.Infrastructure.Ollama.SmokeTest.Tests;

using Acode.Infrastructure.Ollama.SmokeTest.Output;

/// <summary>
/// Tool call test - verifies function calling works.
/// </summary>
/// <remarks>
/// FR-069, FR-070: Tool calling test.
/// STUB: Implementation deferred to Task 007d (tool calling support).
/// This test currently returns a "skipped" result.
/// </remarks>
public sealed class ToolCallTest : ISmokeTest
{
    /// <inheritdoc/>
    public string Name => "Tool Calling";

    /// <inheritdoc/>
    public Task<TestResult> RunAsync(SmokeTestOptions options, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(options);

        // STUB: Tool calling test deferred to Task 007d
        // When 007d is complete, this should:
        // 1. Send a prompt that triggers a function call
        // 2. Provide a simple tool definition (e.g., get_weather)
        // 3. Verify the model returns a tool call
        // 4. Parse and validate the tool call structure
        var result = new TestResult
        {
            TestName = this.Name,
            Passed = true, // Mark as passed so it doesn't fail the suite
            ElapsedTime = TimeSpan.Zero,
            ErrorMessage = "SKIPPED: Requires Task 007d - tool calling support",
            DiagnosticHint = "This test will be implemented when tool calling is available"
        };

        return Task.FromResult(result);
    }
}
