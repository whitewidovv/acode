namespace Acode.Integration.Tests.Ollama.SmokeTest;

using System;
using System.Threading;
using System.Threading.Tasks;
using Acode.Infrastructure.Ollama.SmokeTest;
using FluentAssertions;
using Xunit;

/// <summary>
/// Integration tests for Ollama smoke tests end-to-end.
/// </summary>
/// <remarks>
/// These tests require a running Ollama instance and may be skipped in CI environments.
/// To run these tests, ensure Ollama is running at http://localhost:11434.
/// </remarks>
public sealed class SmokeTestIntegrationTests
{
    private readonly SmokeTestOptions defaultOptions = new()
    {
        Endpoint = "http://localhost:11434",
        Model = "llama3.2:latest",
        Timeout = TimeSpan.FromSeconds(30)
    };

    [Fact(Skip = "Requires live Ollama instance - enable manually for integration testing")]
    public async Task SmokeTest_CompleteAllTests_WhenOllamaRunning()
    {
        // Arrange
        var runner = new OllamaSmokeTestRunner();

        // Act
        var results = await runner.RunAsync(this.defaultOptions, CancellationToken.None);

        // Assert - If Ollama is running and properly configured, all tests should pass
        results.Should().NotBeNull();
        results.Results.Should().HaveCount(5); // Health, Model, Completion, Streaming, ToolCall
        results.CheckedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(10));
        results.TotalDuration.Should().BeGreaterThan(TimeSpan.Zero);

        // Individual test results
        var healthCheck = results.Results.Should().ContainSingle(r => r.TestName == "Health Check").Subject;
        healthCheck.Passed.Should().BeTrue("health check should pass when Ollama is running");

        var modelList = results.Results.Should().ContainSingle(r => r.TestName == "Model List").Subject;
        modelList.Passed.Should().BeTrue("model list should pass when Ollama is running");

        // Note: Other tests may fail if model isn't available - that's expected
        results.PassedCount.Should().BeGreaterOrEqualTo(2, "at least health and model list should pass");
    }

    [Fact]
    public async Task SmokeTest_FailsGracefully_WhenOllamaNotRunning()
    {
        // Arrange - Use invalid endpoint that won't resolve
        var invalidOptions = new SmokeTestOptions
        {
            Endpoint = "http://invalid-endpoint-that-does-not-exist:99999",
            Model = "llama3.2:latest",
            Timeout = TimeSpan.FromSeconds(2)
        };
        var runner = new OllamaSmokeTestRunner();

        // Act
        var results = await runner.RunAsync(invalidOptions, CancellationToken.None);

        // Assert - Should fail gracefully without throwing exceptions
        results.Should().NotBeNull();
        results.Results.Should().HaveCount(1, "should stop after health check fails");

        var healthCheck = results.Results[0];
        healthCheck.TestName.Should().Be("Health Check");
        healthCheck.Passed.Should().BeFalse();
        healthCheck.ErrorMessage.Should().NotBeNullOrEmpty();
        healthCheck.DiagnosticHint.Should().NotBeNullOrEmpty();

        // Overall results
        results.AllPassed.Should().BeFalse();
        results.FailedCount.Should().Be(1);
        results.PassedCount.Should().Be(0);
    }

    [Fact]
    public async Task SmokeTest_SkipsToolTest_WhenFlagSet()
    {
        // Arrange - Tool test skip flag should work regardless of Ollama status
        var optionsWithSkip = new SmokeTestOptions
        {
            Endpoint = "http://invalid-endpoint:99999",
            Model = "llama3.2:latest",
            Timeout = TimeSpan.FromSeconds(2),
            SkipToolTest = true
        };
        var runner = new OllamaSmokeTestRunner();

        // Act
        var results = await runner.RunAsync(optionsWithSkip, CancellationToken.None);

        // Assert
        results.Should().NotBeNull();
        results.Results.Should().NotContain(r => r.TestName == "Tool Calling", "tool test should be skipped");
    }

    [Fact]
    public async Task SmokeTest_RespectsTimeout()
    {
        // Arrange - Very short timeout should cause timeout errors
        var shortTimeoutOptions = new SmokeTestOptions
        {
            Endpoint = "http://localhost:11434",
            Model = "llama3.2:latest",
            Timeout = TimeSpan.FromMilliseconds(1) // Impossibly short
        };
        var runner = new OllamaSmokeTestRunner();

        // Act
        var results = await runner.RunAsync(shortTimeoutOptions, CancellationToken.None);

        // Assert - Should complete (not hang) even with short timeout
        results.Should().NotBeNull();
        results.TotalDuration.Should().BeLessThan(TimeSpan.FromSeconds(5), "should not hang indefinitely");
    }

    [Fact]
    public async Task SmokeTest_HandlesCancellation()
    {
        // Arrange
        var runner = new OllamaSmokeTestRunner();
        using var cts = new CancellationTokenSource();
        cts.Cancel(); // Cancel immediately

        // Act
        Func<Task> act = async () => await runner.RunAsync(this.defaultOptions, cts.Token);

        // Assert - Should throw OperationCanceledException
        await act.Should().ThrowAsync<OperationCanceledException>();
    }
}
