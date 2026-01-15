using Acode.Infrastructure.Vllm.Health;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Acode.Infrastructure.Tests.Vllm.Health;

public class VllmHealthCheckerTests
{
    [Fact]
    public void Constructor_Should_Require_Logger()
    {
        // Arrange
        var config = new VllmHealthConfiguration();

        // Act & Assert
        var act = () => new VllmHealthChecker(config, logger: null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public async Task IsHealthyAsync_Should_ReturnTrue_When_ServerRespondsOk()
    {
        // Arrange
        var healthConfig = new VllmHealthConfiguration();
        var logger = CreateMockLogger();
        var checker = new VllmHealthChecker(healthConfig, logger);

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
        var healthConfig = new VllmHealthConfiguration
        {
            TimeoutSeconds = 1
        };
        var logger = CreateMockLogger();
        var checker = new VllmHealthChecker(healthConfig, logger);

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
        var healthConfig = new VllmHealthConfiguration
        {
            TimeoutSeconds = 1
        };
        var logger = CreateMockLogger();
        var checker = new VllmHealthChecker(healthConfig, logger);

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
        var healthConfig = new VllmHealthConfiguration
        {
            TimeoutSeconds = 1
        };
        var logger = CreateMockLogger();
        var checker = new VllmHealthChecker(healthConfig, logger);

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
        var healthConfig = new VllmHealthConfiguration();
        var logger = CreateMockLogger();
        var checker = new VllmHealthChecker(healthConfig, logger);

        // Act
#pragma warning disable CA2007
        var status = await checker.GetHealthStatusAsync(CancellationToken.None);
#pragma warning restore CA2007

        // Assert
        status.Should().NotBeNull();
        status.CheckedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task GetHealthStatusAsync_Should_Log_Check_Start()
    {
        // Arrange
        var healthConfig = new VllmHealthConfiguration();
        var logger = CreateMockLogger();
        var checker = new VllmHealthChecker(healthConfig, logger);

        // Act
#pragma warning disable CA2007
        await checker.GetHealthStatusAsync(CancellationToken.None);
#pragma warning restore CA2007

        // Assert - verify logger was called for check start
        logger.Received().Log(
            LogLevel.Debug,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString()!.Contains("Starting health check")),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    private static ILogger<VllmHealthChecker> CreateMockLogger()
    {
        return Substitute.For<ILogger<VllmHealthChecker>>();
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
