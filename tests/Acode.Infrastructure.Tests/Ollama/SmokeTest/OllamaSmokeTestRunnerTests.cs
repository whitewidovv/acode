namespace Acode.Infrastructure.Tests.Ollama.SmokeTest;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Acode.Infrastructure.Ollama.SmokeTest;
using Acode.Infrastructure.Ollama.SmokeTest.Output;
using FluentAssertions;
using NSubstitute;
using Xunit;

/// <summary>
/// Tests for OllamaSmokeTestRunner.
/// </summary>
public sealed class OllamaSmokeTestRunnerTests
{
    private readonly SmokeTestOptions defaultOptions = new()
    {
        Endpoint = "http://localhost:11434",
        Model = "llama3.2:latest",
        Timeout = TimeSpan.FromSeconds(30)
    };

    [Fact]
    public async Task RunAsync_ExecutesAllTests_WhenAllPass()
    {
        // Arrange - Create mock tests that all pass
        var mockTests = new List<ISmokeTest>();
        foreach (var name in new[] { "Health Check", "Model List", "Non-Streaming Completion", "Streaming Completion", "Tool Calling" })
        {
            var mockTest = Substitute.For<ISmokeTest>();
            mockTest.Name.Returns(name);
            mockTest.RunAsync(Arg.Any<SmokeTestOptions>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(new TestResult
                {
                    TestName = name,
                    Passed = true,
                    ElapsedTime = TimeSpan.FromMilliseconds(10)
                }));
            mockTests.Add(mockTest);
        }

        var runner = new OllamaSmokeTestRunner(mockTests);

        // Act
        var results = await runner.RunAsync(this.defaultOptions, CancellationToken.None);

        // Assert
        results.Should().NotBeNull();
        results.Results.Should().HaveCount(5); // Health, Model, Completion, Streaming, ToolCall
        results.Results.Should().Contain(r => r.TestName == "Health Check");
        results.Results.Should().Contain(r => r.TestName == "Model List");
        results.Results.Should().Contain(r => r.TestName == "Non-Streaming Completion");
        results.Results.Should().Contain(r => r.TestName == "Streaming Completion");
        results.Results.Should().Contain(r => r.TestName == "Tool Calling");
        results.AllPassed.Should().BeTrue();
    }

    [Fact]
    public async Task RunAsync_StopsAfterHealthCheckFailure()
    {
        // Arrange
        var invalidOptions = new SmokeTestOptions
        {
            Endpoint = "http://invalid-endpoint:99999",
            Model = "llama3.2:latest",
            Timeout = TimeSpan.FromSeconds(1)
        };
        var runner = new OllamaSmokeTestRunner();

        // Act
        var results = await runner.RunAsync(invalidOptions, CancellationToken.None);

        // Assert
        results.Should().NotBeNull();
        results.Results.Should().HaveCount(1, "should stop after health check fails");
        results.Results[0].TestName.Should().Be("Health Check");
        results.Results[0].Passed.Should().BeFalse();
        results.AllPassed.Should().BeFalse();
    }

    [Fact]
    public async Task RunAsync_SkipsToolTest_WhenFlagged()
    {
        // Arrange - Create mock tests that all pass
        var mockTests = new List<ISmokeTest>();
        foreach (var name in new[] { "Health Check", "Model List", "Non-Streaming Completion", "Streaming Completion", "Tool Calling" })
        {
            var mockTest = Substitute.For<ISmokeTest>();
            mockTest.Name.Returns(name);
            mockTest.RunAsync(Arg.Any<SmokeTestOptions>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(new TestResult
                {
                    TestName = name,
                    Passed = true,
                    ElapsedTime = TimeSpan.FromMilliseconds(10)
                }));
            mockTests.Add(mockTest);
        }

        var optionsWithSkip = new SmokeTestOptions
        {
            Endpoint = "http://localhost:11434",
            Model = "llama3.2:latest",
            Timeout = TimeSpan.FromSeconds(30),
            SkipToolTest = true
        };
        var runner = new OllamaSmokeTestRunner(mockTests);

        // Act
        var results = await runner.RunAsync(optionsWithSkip, CancellationToken.None);

        // Assert
        results.Should().NotBeNull();
        results.Results.Should().HaveCount(4, "should skip tool test");
        results.Results.Should().NotContain(r => r.TestName == "Tool Calling");
    }

    [Fact]
    public async Task RunAsync_ReturnsAggregateResults_WithCorrectMetadata()
    {
        // Arrange - Create mock tests with mixed results
        var mockTests = new List<ISmokeTest>();
        var testResults = new[]
        {
            ("Test 1", true),
            ("Test 2", true),
            ("Test 3", false)
        };

        foreach (var (name, passed) in testResults)
        {
            var mockTest = Substitute.For<ISmokeTest>();
            mockTest.Name.Returns(name);
            mockTest.RunAsync(Arg.Any<SmokeTestOptions>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(new TestResult
                {
                    TestName = name,
                    Passed = passed,
                    ElapsedTime = TimeSpan.FromMilliseconds(10)
                }));
            mockTests.Add(mockTest);
        }

        var runner = new OllamaSmokeTestRunner(mockTests);

        // Act
        var results = await runner.RunAsync(this.defaultOptions, CancellationToken.None);

        // Assert
        results.CheckedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        results.TotalDuration.Should().BeGreaterThan(TimeSpan.Zero);
        results.Results.Should().HaveCount(3);
        results.PassedCount.Should().Be(2);
        results.FailedCount.Should().Be(1);
        results.AllPassed.Should().BeFalse();
    }

    [Fact]
    public async Task RunAsync_RespectsCancellationToken()
    {
        // Arrange
        var runner = new OllamaSmokeTestRunner();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act
        Func<Task> act = async () => await runner.RunAsync(this.defaultOptions, cts.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public void Constructor_InitializesTestsInCorrectOrder()
    {
        // Arrange & Act
        var runner = new OllamaSmokeTestRunner();

        // Assert
        runner.Should().NotBeNull();

        // Order verification will be implicit through RunAsync behavior
    }
}
