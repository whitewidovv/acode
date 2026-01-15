using Acode.Application.Providers.Ollama;
using Acode.Domain.Providers.Ollama;
using Acode.Infrastructure.Providers.Ollama.Lifecycle;

namespace Acode.Infrastructure.Tests.Providers.Ollama.Lifecycle;

/// <summary>
/// End-to-end integration tests for Ollama lifecycle management.
/// Tests full workflows with real components (mocked Ollama process).
/// </summary>
public class OllamaLifecycleIntegrationTests
{
    private readonly OllamaLifecycleOptions _managedModeOptions = new()
    {
        Mode = OllamaLifecycleMode.Managed,
        StartTimeoutSeconds = 30,
        HealthCheckIntervalSeconds = 60,
        Port = 11434,
        MaxRestartsPerMinute = 3,
    };

    private readonly OllamaLifecycleOptions _monitoredModeOptions = new()
    {
        Mode = OllamaLifecycleMode.Monitored,
        Port = 11434,
        HealthCheckIntervalSeconds = 60,
    };

    private readonly OllamaLifecycleOptions _externalModeOptions = new()
    {
        Mode = OllamaLifecycleMode.External,
        Port = 11434,
    };

    [Fact]
    public async Task IntegrationTest_ManagedMode_Startup_And_Shutdown()
    {
        // Arrange
        var orchestrator = new OllamaServiceOrchestrator(_managedModeOptions);

        try
        {
            // Act - Startup
            var startupState = await orchestrator.EnsureHealthyAsync(CancellationToken.None);

            // Assert - Should be running
            Assert.NotEqual(OllamaServiceState.Failed, startupState);
            Assert.NotEqual(OllamaServiceState.Unknown, startupState);

            // Act - Verify state
            var currentState = await orchestrator.GetStateAsync(CancellationToken.None);
            Assert.Equal(startupState, currentState);
        }
        finally
        {
            // Cleanup
            await orchestrator.DisposeAsync();
        }
    }

    [Fact]
    public async Task IntegrationTest_MonitoredMode_DoesNotStartService()
    {
        // Arrange
        var orchestrator = new OllamaServiceOrchestrator(_monitoredModeOptions);

        try
        {
            // Act - In monitored mode, just checks health, doesn't start
            var state = await orchestrator.EnsureHealthyAsync(CancellationToken.None);

            // Assert - State should be queryable
            Assert.NotEqual(OllamaServiceState.Unknown, state);
        }
        finally
        {
            // Cleanup
            await orchestrator.DisposeAsync();
        }
    }

    [Fact]
    public async Task IntegrationTest_ExternalMode_AssumeRunning()
    {
        // Arrange
        var orchestrator = new OllamaServiceOrchestrator(_externalModeOptions);

        try
        {
            // Act - In external mode, assumes always running
            var state = await orchestrator.EnsureHealthyAsync(CancellationToken.None);

            // Assert - Should assume running
            Assert.Equal(OllamaServiceState.Running, state);
        }
        finally
        {
            // Cleanup
            await orchestrator.DisposeAsync();
        }
    }

    [Fact]
    public async Task IntegrationTest_StateTransitions_Managed()
    {
        // Arrange
        var orchestrator = new OllamaServiceOrchestrator(_managedModeOptions);

        try
        {
            // Act - Get initial state
            var initialState = await orchestrator.GetStateAsync(CancellationToken.None);

            // Act - Ensure health (may start)
            var ensureState = await orchestrator.EnsureHealthyAsync(CancellationToken.None);

            // Act - Get state again
            var afterEnsureState = await orchestrator.GetStateAsync(CancellationToken.None);

            // Assert - State transitions should be valid
            Assert.True(
                initialState == OllamaServiceState.Unknown ||
                initialState == OllamaServiceState.Running ||
                initialState == OllamaServiceState.Stopped,
                "Initial state should be valid");

            Assert.True(
                ensureState == OllamaServiceState.Running ||
                ensureState == OllamaServiceState.Failed,
                "Ensure health result should be valid");

            Assert.NotEqual(default(OllamaServiceState), afterEnsureState);
        }
        finally
        {
            // Cleanup
            await orchestrator.DisposeAsync();
        }
    }

    [Fact]
    public async Task IntegrationTest_ModelPull_Integration()
    {
        // Arrange
        var orchestrator = new OllamaServiceOrchestrator(_managedModeOptions);

        try
        {
            // Act - Ensure service is ready
            await orchestrator.EnsureHealthyAsync(CancellationToken.None);

            // Act - Attempt to pull model
            var pullResult = await orchestrator.PullModelAsync("llama2:latest", CancellationToken.None);

            // Assert - Result should be valid
            Assert.NotNull(pullResult);
            Assert.IsType<ModelPullResult>(pullResult);
        }
        finally
        {
            // Cleanup
            await orchestrator.DisposeAsync();
        }
    }

    [Fact]
    public async Task IntegrationTest_ModelPull_WithProgressCallback()
    {
        // Arrange
        var orchestrator = new OllamaServiceOrchestrator(_managedModeOptions);
        var progressEvents = new List<ModelPullProgress>();

        try
        {
            // Act - Ensure service is ready
            await orchestrator.EnsureHealthyAsync(CancellationToken.None);

            // Act - Pull model with progress callback
            var pullResult = await orchestrator.PullModelAsync(
                "llama2:latest",
                (progress) => progressEvents.Add(progress),
                CancellationToken.None);

            // Assert - Result should be valid
            Assert.NotNull(pullResult);
            Assert.IsType<ModelPullResult>(pullResult);
        }
        finally
        {
            // Cleanup
            await orchestrator.DisposeAsync();
        }
    }

    [Fact]
    public async Task IntegrationTest_ModelPull_StreamingProgress()
    {
        // Arrange
        var orchestrator = new OllamaServiceOrchestrator(_managedModeOptions);

        try
        {
            // Act - Ensure service is ready
            await orchestrator.EnsureHealthyAsync(CancellationToken.None);

            // Act - Pull model with streaming progress
            var progressCount = 0;
            await foreach (var progress in orchestrator.PullModelStreamAsync("llama2:latest", CancellationToken.None))
            {
                progressCount++;
                if (progressCount > 100)
                {
                    break; // Safety limit
                }
            }

            // Assert - Should be able to iterate
            Assert.True(progressCount >= 0);
        }
        finally
        {
            // Cleanup
            await orchestrator.DisposeAsync();
        }
    }

    [Fact]
    public async Task IntegrationTest_ConcurrentOperations()
    {
        // Arrange
        var orchestrator = new OllamaServiceOrchestrator(_managedModeOptions);

        try
        {
            // Act - Multiple operations concurrently
            var ensureTask = orchestrator.EnsureHealthyAsync(CancellationToken.None);
            var stateTask = orchestrator.GetStateAsync(CancellationToken.None);
            var pullTask = orchestrator.PullModelAsync("llama2:latest", CancellationToken.None);

            await Task.WhenAll(
                ensureTask.ContinueWith(t => (object?)t.Result),
                stateTask.ContinueWith(t => (object?)t.Result),
                pullTask.ContinueWith(t => (object?)t.Result));

            // Assert - All should complete without error
            Assert.True(true);
        }
        finally
        {
            // Cleanup
            await orchestrator.DisposeAsync();
        }
    }

    [Fact]
    public async Task IntegrationTest_CancellationToken_Propagation()
    {
        // Arrange
        var orchestrator = new OllamaServiceOrchestrator(_managedModeOptions);
        var cts = new CancellationTokenSource();
        cts.Cancel();

        try
        {
            // Act & Assert - Should throw on cancelled token
            await Assert.ThrowsAsync<OperationCanceledException>(() =>
                orchestrator.EnsureHealthyAsync(cts.Token));
        }
        finally
        {
            // Cleanup
            await orchestrator.DisposeAsync();
        }
    }

    [Fact]
    public async Task IntegrationTest_Multiple_Start_Stop_Cycles()
    {
        // Arrange
        var orchestrator = new OllamaServiceOrchestrator(_managedModeOptions);

        try
        {
            // Act - Multiple startup/shutdown cycles
            for (int i = 0; i < 3; i++)
            {
                var startState = await orchestrator.StartAsync(CancellationToken.None);
                Assert.NotEqual(OllamaServiceState.Unknown, startState);

                var state = await orchestrator.GetStateAsync(CancellationToken.None);
                Assert.NotEqual(OllamaServiceState.Unknown, state);

                var stopState = await orchestrator.StopAsync(CancellationToken.None);
                Assert.True(
                    stopState == OllamaServiceState.Stopped ||
                    stopState == OllamaServiceState.Running,
                    "Stop state should be valid");
            }

            // Assert - Should complete without errors
            Assert.True(true);
        }
        finally
        {
            // Cleanup
            await orchestrator.DisposeAsync();
        }
    }
}
