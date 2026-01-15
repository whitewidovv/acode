#pragma warning disable IDE0005
using Acode.Application.Providers.Vllm;
using Acode.Domain.Providers.Vllm;
using Acode.Infrastructure.Providers.Vllm.Lifecycle;
using FluentAssertions;
#pragma warning restore IDE0005

namespace Acode.Integration.Tests.Providers.Vllm.Lifecycle;

/// <summary>
/// Integration tests for vLLM lifecycle management end-to-end scenarios.
/// </summary>
public class VllmLifecycleIntegrationTests
{
    /// <summary>
    /// IT-001: End-to-end startup flow (not running -> startup -> ready/failed).
    /// </summary>
    [Fact]
    public async Task IT001_EndToEndStartup_TransitionsStates()
    {
        // Arrange
        using var orchestrator = CreateOrchestrator();
        var initialStatus = await orchestrator.GetStatusAsync();
        initialStatus.State.Should().Be(VllmServiceState.Unknown, "Should start in Unknown state");

        // Act - Try to start (will fail in test environment, but tests the flow)
        try
        {
            await orchestrator.StartAsync("microsoft/phi-2");
        }
        catch
        {
            // Expected in test environment (no vLLM binary)
        }

        // Assert
        var finalStatus = await orchestrator.GetStatusAsync();
        finalStatus.State.Should().BeOneOf(
            VllmServiceState.Starting,
            VllmServiceState.Failed,
            VllmServiceState.Running);
        finalStatus.CurrentModel.Should().Be("microsoft/phi-2");
    }

    /// <summary>
    /// IT-002: Model switching with service restart.
    /// </summary>
    [Fact]
    public async Task IT002_ModelSwitching_RestartsWithNewModel()
    {
        // Arrange - Start with first model
        using var orchestrator = CreateOrchestrator();
        try
        {
            await orchestrator.StartAsync("microsoft/phi-2");
        }
        catch
        {
            // Expected
        }

        var statusAfterFirst = await orchestrator.GetStatusAsync();
        statusAfterFirst.CurrentModel.Should().Be("microsoft/phi-2");

        // Act - Restart with different model
        try
        {
            await orchestrator.RestartAsync("google/gemma-2b");
        }
        catch
        {
            // Expected
        }

        // Assert
        var finalStatus = await orchestrator.GetStatusAsync();
        finalStatus.CurrentModel.Should().Be("google/gemma-2b", "Should switch to new model");
    }

    /// <summary>
    /// IT-003: Crash recovery and auto-restart respects rate limits.
    /// </summary>
    [Fact]
    public async Task IT003_RestartRateLimiting_EnforcesPolicy()
    {
        // Arrange - Do 3 restarts (max allowed)
        using var orchestrator = CreateOrchestrator();
        for (int i = 0; i < 3; i++)
        {
            try
            {
                await orchestrator.RestartAsync("microsoft/phi-2");
            }
            catch (InvalidOperationException)
            {
                // Rate limit hit - this is fine
                break;
            }
            catch
            {
                // vLLM not installed - expected
            }
        }

        // Act - 4th restart should hit rate limit
        Func<Task> act = async () => await orchestrator.RestartAsync("microsoft/phi-2");

        // Assert - Should throw rate limit exception
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*rate limit*");
    }

    /// <summary>
    /// IT-004: GPU detection returns valid structure.
    /// </summary>
    [Fact]
    public async Task IT004_GpuDetection_ReturnsValidList()
    {
        // Arrange
        using var orchestrator = CreateOrchestrator();

        // Act
        var gpus = await orchestrator.GetAvailableGpusAsync();

        // Assert
        gpus.Should().NotBeNull("Should return a list");

        // If GPUs available, verify structure
        foreach (var gpu in gpus)
        {
            gpu.DeviceId.Should().BeGreaterThanOrEqualTo(0);
            gpu.Name.Should().NotBeNullOrEmpty();
            gpu.TotalMemoryMb.Should().BeGreaterThan(0);
        }
    }

    /// <summary>
    /// IT-005: EnsureHealthy starts service if not running.
    /// </summary>
    [Fact]
    public async Task IT005_EnsureHealthy_StartsIfNotRunning()
    {
        // Arrange
        using var orchestrator = CreateOrchestrator();
        var initialStatus = await orchestrator.GetStatusAsync();
        initialStatus.State.Should().Be(VllmServiceState.Unknown);

        // Act
        try
        {
            await orchestrator.EnsureHealthyAsync("microsoft/phi-2");
        }
        catch
        {
            // Expected in test environment
        }

        // Assert - Should have attempted to start
        var finalStatus = await orchestrator.GetStatusAsync();
        finalStatus.State.Should().NotBe(
            VllmServiceState.Unknown,
            "Should have transitioned from Unknown");
    }

    /// <summary>
    /// IT-006: Status reporting includes all components.
    /// </summary>
    [Fact]
    public async Task IT006_StatusReporting_IncludesAllInfo()
    {
        // Arrange - Start a service first
        using var orchestrator = CreateOrchestrator();
        try
        {
            await orchestrator.StartAsync("microsoft/phi-2");
        }
        catch
        {
            // Expected
        }

        // Act
        var status = await orchestrator.GetStatusAsync();

        // Assert - All fields populated
        status.Should().NotBeNull();
        status.State.Should().BeDefined();
        status.CurrentModel.Should().NotBeNull();
        status.GpuDevices.Should().NotBeNull();
        status.ErrorMessage.Should().NotBeNull();

        // LastHealthCheckHealthy should be false (no actual health checks ran)
        status.LastHealthCheckHealthy.Should().BeFalse();
    }

    /// <summary>
    /// IT-007: Model validation rejects invalid formats.
    /// </summary>
    [Fact]
    public async Task IT007_InvalidModelFormat_RejectedWithGuidance()
    {
        // Arrange
        using var orchestrator = CreateOrchestrator();

        // Act
        Func<Task> act = async () => await orchestrator.StartAsync("invalid-model-format");

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*org/model-name*");
    }

    private VllmServiceOrchestrator CreateOrchestrator()
    {
        var options = new VllmLifecycleOptions
        {
            Mode = VllmLifecycleMode.Managed,
            HealthCheckIntervalSeconds = 60,
            StartTimeoutSeconds = 30,
            Port = 8000
        };
        return new VllmServiceOrchestrator(options);
    }
}
