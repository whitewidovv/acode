namespace Acode.Integration.Tests.Providers;

using System;
using System.Threading;
using System.Threading.Tasks;
using Acode.Application.Inference;
using Acode.Application.Providers;
using Acode.Application.Providers.Selection;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

/// <summary>
/// Integration tests for provider health check functionality.
/// Gap #26 from task-004c completion checklist.
/// </summary>
public sealed class ProviderHealthCheckTests
{
    [Fact]
    public async Task Should_Check_Provider_Health()
    {
        // Arrange
        var healthyProvider = Substitute.For<IModelProvider>();
        healthyProvider.ProviderName.Returns("healthy");
        healthyProvider.IsHealthyAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(true));

        var unhealthyProvider = Substitute.For<IModelProvider>();
        unhealthyProvider.ProviderName.Returns("unhealthy");
        unhealthyProvider.IsHealthyAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(false));

        var providerFactory = new Func<ProviderDescriptor, IModelProvider?>(desc =>
        {
            if (desc.Id == "healthy")
            {
                return healthyProvider;
            }

            if (desc.Id == "unhealthy")
            {
                return unhealthyProvider;
            }

            return null;
        });

        var logger = NullLogger<ProviderRegistry>.Instance;
        var selector = new CapabilityProviderSelector(_ => null);
        var registry = new ProviderRegistry(logger, selector, null, providerFactory);

        registry.Register(new ProviderDescriptor
        {
            Id = "healthy",
            Name = "Healthy Provider",
            Type = ProviderType.Local,
            Capabilities = new ProviderCapabilities(supportsStreaming: false, supportsTools: false),
            Endpoint = new ProviderEndpoint(new Uri("http://localhost:8000"))
        });

        registry.Register(new ProviderDescriptor
        {
            Id = "unhealthy",
            Name = "Unhealthy Provider",
            Type = ProviderType.Local,
            Capabilities = new ProviderCapabilities(supportsStreaming: false, supportsTools: false),
            Endpoint = new ProviderEndpoint(new Uri("http://localhost:9000"))
        });

        // Act
        var healthResults = await registry.CheckAllHealthAsync(CancellationToken.None);

        // Assert
        healthResults.Should().HaveCount(2);
        healthResults["healthy"].Status.Should().Be(HealthStatus.Healthy);
        healthResults["unhealthy"].Status.Should().Be(HealthStatus.Unhealthy);
        healthResults["healthy"].ConsecutiveFailures.Should().Be(0);
        healthResults["unhealthy"].ConsecutiveFailures.Should().Be(1);
    }

    [Fact]
    public async Task Should_Timeout_Appropriately()
    {
        // Arrange
        var slowProvider = Substitute.For<IModelProvider>();
        slowProvider.ProviderName.Returns("slow");
        slowProvider.IsHealthyAsync(Arg.Any<CancellationToken>())
            .Returns(async callInfo =>
            {
                var cancellationToken = callInfo.Arg<CancellationToken>();
                await Task.Delay(TimeSpan.FromSeconds(10), cancellationToken); // Simulate slow response
                return true;
            });

        var providerFactory = new Func<ProviderDescriptor, IModelProvider?>(desc =>
        {
            if (desc.Id == "slow")
            {
                return slowProvider;
            }

            return null;
        });

        var logger = NullLogger<ProviderRegistry>.Instance;
        var selector = new CapabilityProviderSelector(_ => null);
        var registry = new ProviderRegistry(logger, selector, null, providerFactory);

        registry.Register(new ProviderDescriptor
        {
            Id = "slow",
            Name = "Slow Provider",
            Type = ProviderType.Local,
            Capabilities = new ProviderCapabilities(supportsStreaming: false, supportsTools: false),
            Endpoint = new ProviderEndpoint(new Uri("http://localhost:8000"))
        });

        // Act - Use a short timeout to simulate timeout scenario
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));
        var healthResults = await registry.CheckAllHealthAsync(cts.Token);

        // Assert - Provider marked unhealthy due to timeout
        healthResults.Should().HaveCount(1);
        healthResults["slow"].Status.Should().Be(HealthStatus.Unhealthy);
        healthResults["slow"].LastError.Should().NotBeNull();
    }

    [Fact]
    public async Task Should_Update_Health_Status()
    {
        // Arrange - Provider that fails then recovers
        var flipFlopProvider = Substitute.For<IModelProvider>();
        flipFlopProvider.ProviderName.Returns("flipflop");
        var callCount = 0;
        flipFlopProvider.IsHealthyAsync(Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                callCount++;
                return Task.FromResult(callCount > 2); // Fail first 2 calls, then succeed
            });

        var providerFactory = new Func<ProviderDescriptor, IModelProvider?>(desc =>
        {
            if (desc.Id == "flipflop")
            {
                return flipFlopProvider;
            }

            return null;
        });

        var logger = NullLogger<ProviderRegistry>.Instance;
        var selector = new CapabilityProviderSelector(_ => null);
        var registry = new ProviderRegistry(logger, selector, null, providerFactory);

        registry.Register(new ProviderDescriptor
        {
            Id = "flipflop",
            Name = "FlipFlop Provider",
            Type = ProviderType.Local,
            Capabilities = new ProviderCapabilities(supportsStreaming: false, supportsTools: false),
            Endpoint = new ProviderEndpoint(new Uri("http://localhost:8000"))
        });

        // Act - First check: should fail
        var firstCheck = await registry.CheckAllHealthAsync(CancellationToken.None);

        // Assert first check
        firstCheck["flipflop"].Status.Should().Be(HealthStatus.Unhealthy);
        firstCheck["flipflop"].ConsecutiveFailures.Should().Be(1);

        // Act - Second check: should still fail
        var secondCheck = await registry.CheckAllHealthAsync(CancellationToken.None);

        // Assert second check
        secondCheck["flipflop"].Status.Should().Be(HealthStatus.Unhealthy);
        secondCheck["flipflop"].ConsecutiveFailures.Should().Be(2);

        // Act - Third check: should succeed and reset failures
        var thirdCheck = await registry.CheckAllHealthAsync(CancellationToken.None);

        // Assert third check
        thirdCheck["flipflop"].Status.Should().Be(HealthStatus.Healthy);
        thirdCheck["flipflop"].ConsecutiveFailures.Should().Be(0);
    }
}
