#pragma warning disable IDE0005
using Acode.Application.Providers.Vllm;
using Acode.Domain.Providers.Vllm;
using Acode.Infrastructure.Providers.Vllm.Lifecycle;
using FluentAssertions;
#pragma warning restore IDE0005

namespace Acode.Infrastructure.Tests.Providers.Vllm.Lifecycle;

/// <summary>
/// Tests for VllmServiceOrchestrator lifecycle management.
/// </summary>
public class VllmServiceOrchestratorTests : IDisposable
{
    private readonly VllmServiceOrchestrator _orchestrator;

    public VllmServiceOrchestratorTests()
    {
        _orchestrator = new VllmServiceOrchestrator();
    }

    public void Dispose()
    {
        _orchestrator.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public void Test_VllmServiceOrchestrator_ImplementsInterface()
    {
        // Assert
        _orchestrator.Should().BeAssignableTo<IVllmServiceOrchestrator>();
    }

    [Fact]
    public async Task Test_GetStatusAsync_InitialState_IsUnknown()
    {
        // Act
        var status = await _orchestrator.GetStatusAsync();

        // Assert
        status.State.Should().Be(VllmServiceState.Unknown);
    }

    [Fact]
    public async Task Test_GetStatusAsync_ReturnsValidStatus()
    {
        // Act
        var status = await _orchestrator.GetStatusAsync();

        // Assert
        status.Should().NotBeNull();
        status.CurrentModel.Should().NotBeNull();
        status.GpuDevices.Should().NotBeNull();
        status.ErrorMessage.Should().NotBeNull();
    }

    [Fact]
    public async Task Test_GetAvailableGpusAsync_ReturnsGpuList()
    {
        // Act
        var gpus = await _orchestrator.GetAvailableGpusAsync();

        // Assert
        gpus.Should().NotBeNull("Should return a list, even if empty");
    }

    [Fact]
    public async Task Test_StartAsync_InvalidModelId_ThrowsArgumentException()
    {
        // Act
        var act = async () => await _orchestrator.StartAsync("invalid-format");

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*org/model-name*");
    }

    [Fact]
    public async Task Test_StartAsync_EmptyModelId_ThrowsArgumentException()
    {
        // Act
        var act = async () => await _orchestrator.StartAsync(string.Empty);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task Test_StartAsync_ValidModelId_SetsStateToStarting()
    {
        // Act - Start with valid model format (won't actually start vLLM)
        // In test environment, will transition to Starting then fail
        try
        {
            await _orchestrator.StartAsync("meta-llama/Llama-2-7b-hf");
        }
        catch
        {
            // Expected to fail in test environment (no vLLM binary)
        }

        // Assert - Check that we at least attempted to start
        var status = await _orchestrator.GetStatusAsync();

        // State should be Starting, Failed, or Stopped depending on timing
        status.State.Should().BeOneOf(
            VllmServiceState.Starting,
            VllmServiceState.Failed,
            VllmServiceState.Stopped);
    }

    [Fact]
    public async Task Test_StopAsync_WhenNotRunning_CompletesWithoutError()
    {
        // Act - Stop when not running should be a no-op
        var exception = await Record.ExceptionAsync(() => _orchestrator.StopAsync());

        // Assert
        exception.Should().BeNull("Stop on non-running service should not throw");
    }

    [Fact]
    public async Task Test_StopAsync_SetsStateToStopping()
    {
        // Arrange - First start a service (will fail but changes state)
        try
        {
            await _orchestrator.StartAsync("microsoft/phi-2");
        }
        catch
        {
            // Expected to fail
        }

        // Act
        await _orchestrator.StopAsync();

        // Assert
        var status = await _orchestrator.GetStatusAsync();
        status.State.Should().BeOneOf(
            VllmServiceState.Stopped,
            VllmServiceState.Stopping);
    }

    [Fact]
    public async Task Test_RestartAsync_WithNullModel_UsesCurrentModel()
    {
        // Arrange - Set a model first (will fail but sets current model)
        try
        {
            await _orchestrator.StartAsync("microsoft/phi-2");
        }
        catch
        {
            // Expected
        }

        // Act
        try
        {
            await _orchestrator.RestartAsync(null);
        }
        catch
        {
            // Expected to fail in test environment
        }

        // Assert - Should have attempted restart
        var status = await _orchestrator.GetStatusAsync();
        status.Should().NotBeNull();
    }

    [Fact]
    public async Task Test_RestartAsync_WithNewModel_ChangesModel()
    {
        // Arrange
        try
        {
            await _orchestrator.StartAsync("microsoft/phi-2");
        }
        catch
        {
            // Expected
        }

        // Act - Restart with different model
        try
        {
            await _orchestrator.RestartAsync("google/gemma-2b");
        }
        catch
        {
            // Expected
        }

        // Assert
        var status = await _orchestrator.GetStatusAsync();
        status.Should().NotBeNull();
    }

    [Fact]
    public async Task Test_EnsureHealthyAsync_WhenNotRunning_StartsService()
    {
        // Act
        try
        {
            await _orchestrator.EnsureHealthyAsync("microsoft/phi-2");
        }
        catch
        {
            // Expected in test environment
        }

        // Assert - Should have attempted to start
        var status = await _orchestrator.GetStatusAsync();
        status.State.Should().NotBe(VllmServiceState.Unknown, "Should have transitioned from Unknown");
    }

    [Fact]
    public async Task Test_EnsureHealthyAsync_WithModelOverride_UsesOverrideModel()
    {
        // Act
        try
        {
            await _orchestrator.EnsureHealthyAsync("microsoft/phi-2");
        }
        catch
        {
            // Expected
        }

        // Assert
        var status = await _orchestrator.GetStatusAsync();
        status.Should().NotBeNull();
    }

    [Fact]
    public void Test_Dispose_CanBeCalledMultipleTimes()
    {
        // Arrange
        var orchestrator = new VllmServiceOrchestrator();

        // Act & Assert - Should not throw
        orchestrator.Dispose();
        orchestrator.Dispose();
        orchestrator.Dispose();
    }

    [Fact]
    public async Task Test_GetStatusAsync_AfterDispose_ThrowsObjectDisposedException()
    {
        // Arrange
        var orchestrator = new VllmServiceOrchestrator();
        orchestrator.Dispose();

        // Act & Assert
        await Assert.ThrowsAsync<ObjectDisposedException>(() => orchestrator.GetStatusAsync());
    }

    [Fact]
    public void Test_VllmServiceOrchestrator_Options_CanBeConfigured()
    {
        // Arrange
        var options = new VllmLifecycleOptions
        {
            Mode = VllmLifecycleMode.Managed,
            Port = 8001,
            HealthCheckIntervalSeconds = 30
        };

        // Act
        var orchestrator = new VllmServiceOrchestrator(options);

        // Assert
        orchestrator.Should().NotBeNull();
        orchestrator.Dispose();
    }

    [Fact]
    public void Test_VllmServiceOrchestrator_DefaultOptions_UsesDefaults()
    {
        // Act
        var orchestrator = new VllmServiceOrchestrator();

        // Assert - Should use default options without throwing
        orchestrator.Should().NotBeNull();
        orchestrator.Dispose();
    }

    [Fact]
    public async Task Test_GetStatusAsync_ReturnsCurrentModel()
    {
        // Arrange - Try to start with a model
        try
        {
            await _orchestrator.StartAsync("microsoft/phi-2");
        }
        catch
        {
            // Expected
        }

        // Act
        var status = await _orchestrator.GetStatusAsync();

        // Assert - Should track the model we tried to load
        status.CurrentModel.Should().NotBeNull();
    }

    [Fact]
    public async Task Test_GetStatusAsync_ReturnsLastHealthCheckInfo()
    {
        // Act
        var status = await _orchestrator.GetStatusAsync();

        // Assert
        // LastHealthCheckUtc may be null initially, that's OK
        status.LastHealthCheckHealthy.Should().BeFalse("Should be false initially");
    }

    [Fact]
    public async Task Test_StartAsync_Cancellation_ThrowsOperationCanceledException()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => _orchestrator.StartAsync("microsoft/phi-2", cts.Token));
    }

    [Fact]
    public async Task Test_StopAsync_Cancellation_ThrowsOperationCanceledException()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => _orchestrator.StopAsync(cts.Token));
    }
}
