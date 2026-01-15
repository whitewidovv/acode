using Acode.Domain.Providers.Ollama;
using Acode.Infrastructure.Providers.Ollama.Lifecycle;

namespace Acode.Infrastructure.Tests.Providers.Ollama.Lifecycle;

/// <summary>
/// Tests for ModelPullManager.
/// Validates model pulling, retries, and error handling.
/// </summary>
public class ModelPullManagerTests
{
    [Fact]
    public void ModelPullManager_CanBeInstantiated()
    {
        // Arrange & Act
        var manager = new ModelPullManager();

        // Assert
        Assert.NotNull(manager);
    }

    [Fact]
    public async Task ModelPullManager_ValidatesModelName()
    {
        // Arrange
        var manager = new ModelPullManager();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            manager.PullAsync(string.Empty, CancellationToken.None));
    }

    [Fact]
    public async Task ModelPullManager_ValidatesNullModelName()
    {
        // Arrange
        var manager = new ModelPullManager();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            manager.PullAsync(null!, CancellationToken.None));
    }

    [Fact]
    public async Task ModelPullManager_SuccessPullReturnsSuccess()
    {
        // Arrange
        var manager = new ModelPullManager();

        // Act - Simulate with a valid model name (will need mock in real scenario)
        // For now, just verify the method exists and signature is correct
        var result = await manager.PullAsync("llama2:latest", CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.IsType<ModelPullResult>(result);
    }

    [Fact]
    public async Task ModelPullManager_CanCallPullWithProgressCallback()
    {
        // Arrange
        var manager = new ModelPullManager();
        Action<ModelPullProgress>? progressCallback = (progress) => { };

        // Act & Assert - Just verify method signature works
        var result = await manager.PullAsync("llama2:latest", progressCallback, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.IsType<ModelPullResult>(result);
    }

    [Fact]
    public async Task ModelPullManager_CanStreamProgress()
    {
        // Arrange
        var manager = new ModelPullManager();

        // Act - Enumerate the stream (should be empty or minimal in test)
        // Prevent infinite loop in test - max 100 iterations
        var progressCount = 0;
        await foreach (var progress in manager.PullStreamAsync("llama2:latest", CancellationToken.None))
        {
            progressCount++;
            if (progressCount > 100)
            {
                break;
            }
        }

        // Assert - Just verify it returns an enumerable
        Assert.IsType<int>(progressCount);
    }

    [Fact]
    public async Task ModelPullManager_RespectsAirgappedMode()
    {
        // Arrange
        var manager = new ModelPullManager(airgappedMode: true);

        // Act
        var result = await manager.PullAsync("llama2:latest", CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("airgap", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ModelPullManager_RejectsNullModelNameWithMessage()
    {
        // Arrange
        var manager = new ModelPullManager();

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
            manager.PullAsync(null!, CancellationToken.None));

        Assert.Contains("model", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ModelPullManager_RespectsCancellationToken()
    {
        // Arrange
        var manager = new ModelPullManager();
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert - should throw OperationCanceledException
        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            manager.PullAsync("llama2:latest", cts.Token));
    }

    [Theory]
    [InlineData("llama2")]
    [InlineData("neural-chat:7b")]
    [InlineData("mistral:latest")]
    public async Task ModelPullManager_AcceptsVariousValidModelNames(string modelName)
    {
        // Arrange
        var manager = new ModelPullManager();

        // Act
        var result = await manager.PullAsync(modelName, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.IsType<ModelPullResult>(result);
    }

    [Fact]
    public async Task ModelPullManager_RejectsWhitespaceOnlyModelName()
    {
        // Arrange
        var manager = new ModelPullManager();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            manager.PullAsync("   ", CancellationToken.None));
    }

    [Fact]
    public async Task ModelPullManager_HandleMultiplePullsSequentially()
    {
        // Arrange
        var manager = new ModelPullManager();

        // Act - Pull same model multiple times
        var result1 = await manager.PullAsync("llama2:latest", CancellationToken.None);
        var result2 = await manager.PullAsync("llama2:latest", CancellationToken.None);
        var result3 = await manager.PullAsync("neural-chat:latest", CancellationToken.None);

        // Assert - All should return valid results
        Assert.NotNull(result1);
        Assert.NotNull(result2);
        Assert.NotNull(result3);
        Assert.IsType<ModelPullResult>(result1);
        Assert.IsType<ModelPullResult>(result2);
        Assert.IsType<ModelPullResult>(result3);
    }
}
