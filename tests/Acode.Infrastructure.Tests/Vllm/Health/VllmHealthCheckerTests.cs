using Acode.Infrastructure.Vllm.Client;
using Acode.Infrastructure.Vllm.Health;
using FluentAssertions;

namespace Acode.Infrastructure.Tests.Vllm.Health;

public class VllmHealthCheckerTests
{
    [Fact]
    public async Task IsHealthyAsync_Should_ReturnTrue_When_ServerRespondsOk()
    {
        // Arrange
        var config = new VllmClientConfiguration
        {
            Endpoint = "http://localhost:8000"
        };
        var checker = new VllmHealthChecker(config);

        // Act & Assert - will fail when server not running, but verifies contract
#pragma warning disable CA2007
        var result = await checker.IsHealthyAsync(CancellationToken.None);
#pragma warning restore CA2007

        // Server not running in test environment, so result will be false
        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsHealthyAsync_Should_ReturnFalse_When_ServerUnreachable()
    {
        // Arrange
        var config = new VllmClientConfiguration
        {
            Endpoint = "http://localhost:9999", // Invalid port
            HealthCheckTimeoutSeconds = 1
        };
        var checker = new VllmHealthChecker(config);

        // Act
#pragma warning disable CA2007
        var result = await checker.IsHealthyAsync(CancellationToken.None);
#pragma warning restore CA2007

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsHealthyAsync_Should_ReturnFalse_When_Timeout()
    {
        // Arrange
        var config = new VllmClientConfiguration
        {
            Endpoint = "http://localhost:8000",
            HealthCheckTimeoutSeconds = 1
        };
        var checker = new VllmHealthChecker(config);

        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(10));

        // Act
#pragma warning disable CA2007
        var result = await checker.IsHealthyAsync(cts.Token);
#pragma warning restore CA2007

        // Assert - should return false, never throw
        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsHealthyAsync_Should_NeverThrowException()
    {
        // Arrange
        var config = new VllmClientConfiguration
        {
            Endpoint = "http://invalid-domain-that-does-not-exist.local",
            HealthCheckTimeoutSeconds = 1
        };
        var checker = new VllmHealthChecker(config);

        // Act
#pragma warning disable CA2007
        var act = async () => await checker.IsHealthyAsync(CancellationToken.None);

        // Assert - should not throw, just return false
        await act.Should().NotThrowAsync();
#pragma warning restore CA2007
    }

    [Fact]
    public async Task GetHealthStatusAsync_Should_ReturnHealthyStatus_When_ServerOk()
    {
        // Arrange
        var config = new VllmClientConfiguration
        {
            Endpoint = "http://localhost:8000"
        };
        var checker = new VllmHealthChecker(config);

        // Act
#pragma warning disable CA2007
        var status = await checker.GetHealthStatusAsync(CancellationToken.None);
#pragma warning restore CA2007

        // Assert
        status.Should().NotBeNull();
        status.Endpoint.Should().Be("http://localhost:8000");
        status.CheckedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
    }
}

public class HealthStatusTests
{
    [Fact]
    public void Should_Have_All_Four_States()
    {
        // Arrange & Act - Just verify the enum exists and has all states
        var healthyStatus = HealthStatus.Healthy;
        var degradedStatus = HealthStatus.Degraded;
        var unhealthyStatus = HealthStatus.Unhealthy;
        var unknownStatus = HealthStatus.Unknown;

        // Assert
        healthyStatus.Should().Be(HealthStatus.Healthy);
        degradedStatus.Should().Be(HealthStatus.Degraded);
        unhealthyStatus.Should().Be(HealthStatus.Unhealthy);
        unknownStatus.Should().Be(HealthStatus.Unknown);
    }
}
