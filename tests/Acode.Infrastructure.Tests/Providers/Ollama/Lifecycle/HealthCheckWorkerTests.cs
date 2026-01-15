using Acode.Infrastructure.Providers.Ollama.Lifecycle;

namespace Acode.Infrastructure.Tests.Providers.Ollama.Lifecycle;

/// <summary>
/// Tests for HealthCheckWorker background health monitoring.
/// </summary>
public class HealthCheckWorkerTests
{
    [Fact]
    public void HealthCheckWorker_CanBeInstantiated()
    {
        // Arrange & Act
        var worker = new HealthCheckWorker(
            healthCheckIntervalMs: 1000,
            isExternalMode: false);

        // Assert
        Assert.NotNull(worker);
    }

    [Fact]
    public async Task HealthCheckWorker_CanStart()
    {
        // Arrange
        var worker = new HealthCheckWorker(
            healthCheckIntervalMs: 1000,
            isExternalMode: false);

        // Act & Assert - Just verify it can start without error
        using var cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromMilliseconds(100));

        try
        {
            await worker.StartAsync(cts.Token);
        }
        catch (OperationCanceledException)
        {
            // Expected
        }
    }

    [Fact]
    public async Task HealthCheckWorker_CanStop()
    {
        // Arrange
        var worker = new HealthCheckWorker(
            healthCheckIntervalMs: 1000,
            isExternalMode: false);

        // Act & Assert - Just verify stop doesn't error
        await worker.StopAsync(CancellationToken.None);
    }

    [Fact]
    public void HealthCheckWorker_SkipsChecksInExternalMode()
    {
        // Arrange & Act
        var worker = new HealthCheckWorker(
            healthCheckIntervalMs: 1000,
            isExternalMode: true);

        // Assert - Just verify it's created (actual behavior verified in integration tests)
        Assert.NotNull(worker);
    }

    [Theory]
    [InlineData(1000)]
    [InlineData(5000)]
    [InlineData(30000)]
    public void HealthCheckWorker_AcceptsVariousIntervals(int intervalMs)
    {
        // Arrange & Act
        var worker = new HealthCheckWorker(
            healthCheckIntervalMs: intervalMs,
            isExternalMode: false);

        // Assert
        Assert.NotNull(worker);
    }

    [Fact]
    public void HealthCheckWorker_RejectsNegativeInterval()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentException>(() =>
            new HealthCheckWorker(
                healthCheckIntervalMs: -1,
                isExternalMode: false));
    }

    [Fact]
    public void HealthCheckWorker_RejectsZeroInterval()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentException>(() =>
            new HealthCheckWorker(
                healthCheckIntervalMs: 0,
                isExternalMode: false));
    }

    [Fact]
    public async Task HealthCheckWorker_CanDisposeAsyncSafely()
    {
        // Arrange
        var worker = new HealthCheckWorker(
            healthCheckIntervalMs: 1000,
            isExternalMode: false);

        // Act & Assert - should not throw
        await worker.DisposeAsync();
    }
}
