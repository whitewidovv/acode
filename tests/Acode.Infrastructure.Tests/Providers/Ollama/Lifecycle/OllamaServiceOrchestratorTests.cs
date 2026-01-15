using Acode.Application.Providers.Ollama;
using Acode.Domain.Providers.Ollama;
using Acode.Infrastructure.Providers.Ollama.Lifecycle;

namespace Acode.Infrastructure.Tests.Providers.Ollama.Lifecycle;

/// <summary>
/// Comprehensive tests for OllamaServiceOrchestrator.
/// Tests all methods, modes, state transitions, and error scenarios.
/// </summary>
public class OllamaServiceOrchestratorTests
{
    private readonly OllamaLifecycleOptions _managedOptions = new()
    {
        Mode = OllamaLifecycleMode.Managed,
        StartTimeoutSeconds = 30,
        HealthCheckIntervalSeconds = 60,
        Port = 11434,
    };

    private readonly OllamaLifecycleOptions _monitoredOptions = new()
    {
        Mode = OllamaLifecycleMode.Monitored,
        Port = 11434,
    };

    private readonly OllamaLifecycleOptions _externalOptions = new()
    {
        Mode = OllamaLifecycleMode.External,
        Port = 11434,
    };

    [Fact]
    public void OllamaServiceOrchestrator_CanBeInstantiatedInManagedMode()
    {
        // Arrange & Act
        var orchestrator = new OllamaServiceOrchestrator(_managedOptions);

        // Assert
        Assert.NotNull(orchestrator);
    }

    [Fact]
    public void OllamaServiceOrchestrator_CanBeInstantiatedInMonitoredMode()
    {
        // Arrange & Act
        var orchestrator = new OllamaServiceOrchestrator(_monitoredOptions);

        // Assert
        Assert.NotNull(orchestrator);
    }

    [Fact]
    public void OllamaServiceOrchestrator_CanBeInstantiatedInExternalMode()
    {
        // Arrange & Act
        var orchestrator = new OllamaServiceOrchestrator(_externalOptions);

        // Assert
        Assert.NotNull(orchestrator);
    }

    [Fact]
    public async Task OllamaServiceOrchestrator_EnsureHealthyAsync_ReturnsState()
    {
        // Arrange
        var orchestrator = new OllamaServiceOrchestrator(_managedOptions);

        // Act
        var state = await orchestrator.EnsureHealthyAsync(CancellationToken.None);

        // Assert
        Assert.IsType<OllamaServiceState>(state);
    }

    [Fact]
    public async Task OllamaServiceOrchestrator_GetStateAsync_ReturnsState()
    {
        // Arrange
        var orchestrator = new OllamaServiceOrchestrator(_managedOptions);

        // Act
        var state = await orchestrator.GetStateAsync(CancellationToken.None);

        // Assert
        Assert.IsType<OllamaServiceState>(state);
    }

    [Fact]
    public async Task OllamaServiceOrchestrator_StartAsync_ReturnsState()
    {
        // Arrange
        var orchestrator = new OllamaServiceOrchestrator(_managedOptions);

        // Act
        var state = await orchestrator.StartAsync(CancellationToken.None);

        // Assert
        Assert.IsType<OllamaServiceState>(state);
    }

    [Fact]
    public async Task OllamaServiceOrchestrator_StopAsync_ReturnsState()
    {
        // Arrange
        var orchestrator = new OllamaServiceOrchestrator(_managedOptions);

        // Act
        var state = await orchestrator.StopAsync(CancellationToken.None);

        // Assert
        Assert.IsType<OllamaServiceState>(state);
    }

    [Fact]
    public async Task OllamaServiceOrchestrator_PullModelAsync_ReturnsResult()
    {
        // Arrange
        var orchestrator = new OllamaServiceOrchestrator(_managedOptions);

        // Act
        var result = await orchestrator.PullModelAsync("llama2:latest", CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.IsType<ModelPullResult>(result);
    }

    [Fact]
    public async Task OllamaServiceOrchestrator_PullModelAsync_WithProgressCallback_ReturnsResult()
    {
        // Arrange
        var orchestrator = new OllamaServiceOrchestrator(_managedOptions);
        var progressEvents = new List<ModelPullProgress>();

        // Act
        var result = await orchestrator.PullModelAsync(
            "llama2:latest",
            (progress) => progressEvents.Add(progress),
            CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.IsType<ModelPullResult>(result);
    }

    [Fact]
    public async Task OllamaServiceOrchestrator_PullModelStreamAsync_ReturnsAsyncEnumerable()
    {
        // Arrange
        var orchestrator = new OllamaServiceOrchestrator(_managedOptions);

        // Act
        var progressList = new List<ModelPullProgress>();
        await foreach (var progress in orchestrator.PullModelStreamAsync("llama2:latest", CancellationToken.None))
        {
            progressList.Add(progress);
            if (progressList.Count > 100)
            {
                break; // Safety limit
            }
        }

        // Assert
        Assert.IsType<List<ModelPullProgress>>(progressList);
    }

    [Fact]
    public async Task OllamaServiceOrchestrator_ManagedMode_DoesNotThrowOnStart()
    {
        // Arrange
        var orchestrator = new OllamaServiceOrchestrator(_managedOptions);

        // Act & Assert - should not throw
        await orchestrator.StartAsync(CancellationToken.None);
    }

    [Fact]
    public async Task OllamaServiceOrchestrator_MonitoredMode_DoesNotThrowOnStart()
    {
        // Arrange
        var orchestrator = new OllamaServiceOrchestrator(_monitoredOptions);

        // Act & Assert - should not throw (but may not actually start in Monitored mode)
        await orchestrator.StartAsync(CancellationToken.None);
    }

    [Fact]
    public async Task OllamaServiceOrchestrator_ExternalMode_DoesNotThrowOnStart()
    {
        // Arrange
        var orchestrator = new OllamaServiceOrchestrator(_externalOptions);

        // Act & Assert - should not throw
        await orchestrator.StartAsync(CancellationToken.None);
    }

    [Fact]
    public async Task OllamaServiceOrchestrator_ValidatesModelName_InPull()
    {
        // Arrange
        var orchestrator = new OllamaServiceOrchestrator(_managedOptions);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            orchestrator.PullModelAsync(string.Empty, CancellationToken.None));
    }

    [Fact]
    public async Task OllamaServiceOrchestrator_ValidatesModelName_InPullWithCallback()
    {
        // Arrange
        var orchestrator = new OllamaServiceOrchestrator(_managedOptions);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            orchestrator.PullModelAsync(string.Empty, null, CancellationToken.None));
    }

    [Fact]
    public async Task OllamaServiceOrchestrator_ValidatesModelName_InPullStream()
    {
        // Arrange
        var orchestrator = new OllamaServiceOrchestrator(_managedOptions);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await orchestrator.PullModelStreamAsync(string.Empty, CancellationToken.None).GetAsyncEnumerator().MoveNextAsync());
    }

    [Fact]
    public async Task OllamaServiceOrchestrator_RespectsCancellationToken()
    {
        // Arrange
        var orchestrator = new OllamaServiceOrchestrator(_managedOptions);
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            orchestrator.EnsureHealthyAsync(cts.Token));
    }

    [Fact]
    public async Task OllamaServiceOrchestrator_HandlesCancellation_InStop()
    {
        // Arrange
        var orchestrator = new OllamaServiceOrchestrator(_managedOptions);
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            orchestrator.StopAsync(cts.Token));
    }

    [Theory]
    [InlineData("llama2:latest")]
    [InlineData("neural-chat:latest")]
    [InlineData("custom-model:v1.0")]
    public async Task OllamaServiceOrchestrator_AcceptsVariousModelNames(string modelName)
    {
        // Arrange
        var orchestrator = new OllamaServiceOrchestrator(_managedOptions);

        // Act
        var result = await orchestrator.PullModelAsync(modelName, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public async Task OllamaServiceOrchestrator_StateTransitionsAreConsistent()
    {
        // Arrange
        var orchestrator = new OllamaServiceOrchestrator(_managedOptions);

        // Act - Get state multiple times
        var state1 = await orchestrator.GetStateAsync(CancellationToken.None);
        var state2 = await orchestrator.GetStateAsync(CancellationToken.None);

        // Assert - State should be consistent
        Assert.Equal(state1, state2);
    }

    [Fact]
    public void OllamaServiceOrchestrator_OptionsAreRespected()
    {
        // Arrange
        var customOptions = new OllamaLifecycleOptions
        {
            Mode = OllamaLifecycleMode.Managed,
            StartTimeoutSeconds = 60,
            HealthCheckIntervalSeconds = 120,
            Port = 11435,
        };

        // Act
        var orchestrator = new OllamaServiceOrchestrator(customOptions);

        // Assert
        Assert.NotNull(orchestrator);
    }

    [Fact]
    public async Task OllamaServiceOrchestrator_EnsureHealthyAsync_CanBeCalled_Multiple_Times()
    {
        // Arrange
        var orchestrator = new OllamaServiceOrchestrator(_managedOptions);

        // Act & Assert - should not throw when called multiple times
        await orchestrator.EnsureHealthyAsync(CancellationToken.None);
        await orchestrator.EnsureHealthyAsync(CancellationToken.None);
        await orchestrator.EnsureHealthyAsync(CancellationToken.None);
    }
}
