namespace Acode.Infrastructure.Tests.Fallback;

using Acode.Application.Fallback;
using Acode.Application.Routing;
using Acode.Infrastructure.Fallback;
using FluentAssertions;
using Xunit;

/// <summary>
/// Unit tests for <see cref="FallbackConfiguration"/>.
/// Tests AC-014 to AC-019 and AC-091 to AC-097: Configuration.
/// </summary>
public sealed class FallbackConfigurationTests
{
    /// <summary>
    /// Test that defaults are set correctly.
    /// </summary>
    [Fact]
    public void Should_Have_Correct_Defaults()
    {
        // Arrange & Act
        var config = new FallbackConfiguration();

        // Assert - AC-091 to AC-097
        config.Policy.Should().Be(EscalationPolicy.RetryThenFallback);
        config.RetryCount.Should().Be(2);
        config.RetryDelayMs.Should().Be(1000);
        config.TimeoutMs.Should().Be(60000);
        config.FailureThreshold.Should().Be(5);
        config.CoolingPeriod.Should().Be(TimeSpan.FromSeconds(60));
        config.NotifyUser.Should().BeFalse();
    }

    /// <summary>
    /// Test that global chain is returned.
    /// </summary>
    [Fact]
    public void GetGlobalChain_Should_Return_Configured_Chain()
    {
        // Arrange
        var globalChain = new[] { "llama3.2:7b", "mistral:7b" };
        var config = new FallbackConfiguration(globalChain);

        // Act
        var chain = config.GetGlobalChain();

        // Assert
        chain.Should().BeEquivalentTo(globalChain);
    }

    /// <summary>
    /// Test that role chain takes precedence.
    /// </summary>
    [Fact]
    public void GetRoleChain_Should_Return_Role_Specific_Chain()
    {
        // Arrange
        var globalChain = new[] { "llama3.2:7b" };
        var plannerChain = new List<string> { "llama3.2:70b", "mistral:22b" };
        var roleChains = new Dictionary<AgentRole, IReadOnlyList<string>>
        {
            [AgentRole.Planner] = plannerChain,
        };
        var config = new FallbackConfiguration(globalChain, roleChains);

        // Act
        var chain = config.GetRoleChain(AgentRole.Planner);

        // Assert
        chain.Should().BeEquivalentTo(plannerChain);
    }

    /// <summary>
    /// Test that missing role chain returns empty.
    /// </summary>
    [Fact]
    public void GetRoleChain_Should_Return_Empty_For_Unconfigured_Role()
    {
        // Arrange
        var config = new FallbackConfiguration(new[] { "llama3.2:7b" });

        // Act
        var chain = config.GetRoleChain(AgentRole.Reviewer);

        // Assert
        chain.Should().BeEmpty();
    }

    /// <summary>
    /// Test that custom values are used.
    /// </summary>
    [Fact]
    public void Should_Use_Custom_Values()
    {
        // Arrange
        var globalChain = new[] { "llama3.2:7b" };

        // Act
        var config = new FallbackConfiguration(
            globalChain,
            policy: EscalationPolicy.Immediate,
            retryCount: 5,
            retryDelayMs: 2000,
            timeoutMs: 120000,
            failureThreshold: 3,
            coolingPeriod: TimeSpan.FromSeconds(120),
            notifyUser: true
        );

        // Assert
        config.Policy.Should().Be(EscalationPolicy.Immediate);
        config.RetryCount.Should().Be(5);
        config.RetryDelayMs.Should().Be(2000);
        config.TimeoutMs.Should().Be(120000);
        config.FailureThreshold.Should().Be(3);
        config.CoolingPeriod.Should().Be(TimeSpan.FromSeconds(120));
        config.NotifyUser.Should().BeTrue();
    }

    /// <summary>
    /// Test that invalid retry count throws.
    /// </summary>
    /// <param name="retryCount">The invalid retry count.</param>
    [Theory]
    [InlineData(-1)]
    [InlineData(11)]
    public void Should_Throw_For_Invalid_RetryCount(int retryCount)
    {
        // Act
        var action = () =>
            new FallbackConfiguration(new[] { "llama3.2:7b" }, retryCount: retryCount);

        // Assert
        action.Should().Throw<ArgumentOutOfRangeException>();
    }

    /// <summary>
    /// Test that invalid failure threshold throws.
    /// </summary>
    /// <param name="threshold">The invalid threshold.</param>
    [Theory]
    [InlineData(0)]
    [InlineData(21)]
    public void Should_Throw_For_Invalid_FailureThreshold(int threshold)
    {
        // Act
        var action = () =>
            new FallbackConfiguration(new[] { "llama3.2:7b" }, failureThreshold: threshold);

        // Assert
        action.Should().Throw<ArgumentOutOfRangeException>();
    }

    /// <summary>
    /// Test that null global chain throws.
    /// </summary>
    [Fact]
    public void Should_Throw_For_Null_GlobalChain()
    {
        // Act
        var action = () => new FallbackConfiguration(null!);

        // Assert
        action.Should().Throw<ArgumentNullException>();
    }
}
