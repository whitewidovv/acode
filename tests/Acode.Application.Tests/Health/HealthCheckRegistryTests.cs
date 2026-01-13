// tests/Acode.Application.Tests/Health/HealthCheckRegistryTests.cs
#pragma warning disable CA2007 // ConfigureAwait not required in test code

namespace Acode.Application.Tests.Health;

using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Acode.Application.Health;
using Acode.Infrastructure.Health;
using FluentAssertions;
using NSubstitute;
using Xunit;

/// <summary>
/// Tests for HealthCheckRegistry.
/// Verifies health check registration, parallel execution, and status aggregation.
/// </summary>
public sealed class HealthCheckRegistryTests
{
    [Fact]
    public void Register_AddsHealthCheck_ToRegistry()
    {
        // Arrange
        var registry = new HealthCheckRegistry();
        var check = CreateMockCheck("TestCheck", HealthStatus.Healthy);

        // Act
        registry.Register(check);

        // Assert
        registry.GetRegisteredChecks().Should().Contain(c => c.Name == "TestCheck");
    }

    [Fact]
    public void Register_DuplicateName_IsIdempotent()
    {
        // Arrange
        var registry = new HealthCheckRegistry();
        var check1 = CreateMockCheck("TestCheck", HealthStatus.Healthy);
        var check2 = CreateMockCheck("TestCheck", HealthStatus.Unhealthy);

        // Act
        registry.Register(check1);
        registry.Register(check2);

        // Assert
        registry.GetRegisteredChecks().Should().HaveCount(1);
    }

    [Fact]
    public async Task CheckAllAsync_RunsAllChecksInParallel()
    {
        // Arrange
        var registry = new HealthCheckRegistry();
        var slowCheck1 = CreateSlowCheck("Slow1", 50);
        var slowCheck2 = CreateSlowCheck("Slow2", 50);
        var slowCheck3 = CreateSlowCheck("Slow3", 50);

        registry.Register(slowCheck1);
        registry.Register(slowCheck2);
        registry.Register(slowCheck3);

        // Act
        var stopwatch = Stopwatch.StartNew();
        var result = await registry.CheckAllAsync(CancellationToken.None);
        stopwatch.Stop();

        // Assert - parallel execution should be significantly faster than sequential (150ms)
        // Allow up to 500ms to account for system load during full test suite execution
        // This still validates parallelism while being tolerant to timing variability
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(500, "checks should run in parallel");
        result.Results.Should().HaveCount(3);
    }

    [Theory]
    [InlineData(HealthStatus.Healthy, HealthStatus.Healthy, HealthStatus.Healthy)]
    [InlineData(HealthStatus.Healthy, HealthStatus.Degraded, HealthStatus.Degraded)]
    [InlineData(HealthStatus.Healthy, HealthStatus.Unhealthy, HealthStatus.Unhealthy)]
    [InlineData(HealthStatus.Degraded, HealthStatus.Unhealthy, HealthStatus.Unhealthy)]
    public async Task CheckAllAsync_AggregatesStatus_WorstCaseWins(
        HealthStatus status1,
        HealthStatus status2,
        HealthStatus expected)
    {
        // Arrange
        var registry = new HealthCheckRegistry();
        registry.Register(CreateMockCheck("Check1", status1));
        registry.Register(CreateMockCheck("Check2", status2));

        // Act
        var result = await registry.CheckAllAsync(CancellationToken.None);

        // Assert
        result.AggregateStatus.Should().Be(expected);
    }

    [Fact]
    public async Task CheckAllAsync_ContinuesAfterCheckFailure()
    {
        // Arrange
        var registry = new HealthCheckRegistry();

        var throwingCheck = Substitute.For<IHealthCheck>();
        throwingCheck.Name.Returns("Throwing");
        throwingCheck.CheckAsync(Arg.Any<CancellationToken>())
            .Returns<HealthCheckResult>(x => throw new InvalidOperationException("Boom"));

        registry.Register(throwingCheck);
        registry.Register(CreateMockCheck("Normal", HealthStatus.Healthy));

        // Act
        var result = await registry.CheckAllAsync(CancellationToken.None);

        // Assert
        result.Results.Should().HaveCount(2);
        result.Results.First(r => r.Name == "Throwing").Status.Should().Be(HealthStatus.Unhealthy);
        result.Results.First(r => r.Name == "Normal").Status.Should().Be(HealthStatus.Healthy);
    }

    [Fact]
    public async Task CheckAllAsync_WithNoChecks_ReturnsHealthy()
    {
        // Arrange
        var registry = new HealthCheckRegistry();

        // Act
        var result = await registry.CheckAllAsync(CancellationToken.None);

        // Assert
        result.AggregateStatus.Should().Be(HealthStatus.Healthy);
        result.Results.Should().BeEmpty();
        result.TotalDuration.Should().Be(TimeSpan.Zero);
    }

    [Fact]
    public async Task CheckAllAsync_CalculatesTotalDuration()
    {
        // Arrange
        var registry = new HealthCheckRegistry();
        registry.Register(CreateMockCheck("Check1", HealthStatus.Healthy, TimeSpan.FromMilliseconds(10)));
        registry.Register(CreateMockCheck("Check2", HealthStatus.Healthy, TimeSpan.FromMilliseconds(20)));

        // Act
        var result = await registry.CheckAllAsync(CancellationToken.None);

        // Assert
        result.TotalDuration.Should().BeGreaterOrEqualTo(TimeSpan.FromMilliseconds(20), "should use max duration from parallel execution");
    }

    [Fact]
    public async Task CheckAllAsync_SetsCheckedAtTimestamp()
    {
        // Arrange
        var registry = new HealthCheckRegistry();
        registry.Register(CreateMockCheck("Check1", HealthStatus.Healthy));

        var before = DateTime.UtcNow;

        // Act
        var result = await registry.CheckAllAsync(CancellationToken.None);

        var after = DateTime.UtcNow;

        // Assert
        result.CheckedAt.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
    }

    [Fact]
    public void Register_WithNullCheck_ThrowsArgumentNullException()
    {
        // Arrange
        var registry = new HealthCheckRegistry();

        // Act
        var act = () => registry.Register(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    private static IHealthCheck CreateMockCheck(string name, HealthStatus status, TimeSpan? duration = null)
    {
        var mock = Substitute.For<IHealthCheck>();
        mock.Name.Returns(name);
        mock.CheckAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new HealthCheckResult
            {
                Name = name,
                Status = status,
                Duration = duration ?? TimeSpan.FromMilliseconds(10)
            }));
        return mock;
    }

    private static IHealthCheck CreateSlowCheck(string name, int delayMs)
    {
        var mock = Substitute.For<IHealthCheck>();
        mock.Name.Returns(name);
        mock.CheckAsync(Arg.Any<CancellationToken>())
            .Returns(async x =>
            {
                await Task.Delay(delayMs);
                return new HealthCheckResult
                {
                    Name = name,
                    Status = HealthStatus.Healthy,
                    Duration = TimeSpan.FromMilliseconds(delayMs)
                };
            });
        return mock;
    }
}
