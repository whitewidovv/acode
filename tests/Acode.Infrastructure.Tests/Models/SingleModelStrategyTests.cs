using Acode.Domain.Models.Routing;
using Acode.Infrastructure.Models;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Acode.Infrastructure.Tests.Models;

/// <summary>
/// Tests for <see cref="SingleModelStrategy"/>.
/// </summary>
public class SingleModelStrategyTests
{
    private readonly ILogger<SingleModelStrategy> _logger;
    private readonly RoutingConfig _config;

    public SingleModelStrategyTests()
    {
        _logger = Substitute.For<ILogger<SingleModelStrategy>>();
        _config = new RoutingConfig
        {
            Strategy = "single",
            DefaultModel = "llama3.2:7b",
        };
    }

    [Fact]
    public void GetModel_WithPlannerRole_ReturnsDefaultModel()
    {
        // Arrange
        var strategy = new SingleModelStrategy(_logger, _config);
        var request = new RoutingRequest
        {
            Role = AgentRole.Planner,
        };

        // Act
        var decision = strategy.GetModel(request);

        // Assert
        decision.ModelId.Should().Be("llama3.2:7b");
        decision.IsFallback.Should().BeFalse();
        decision.Reason.Should().Contain("single");
        decision.Reason.Should().Contain("llama3.2:7b");
    }

    [Fact]
    public void GetModel_WithCoderRole_ReturnsDefaultModel()
    {
        // Arrange
        var strategy = new SingleModelStrategy(_logger, _config);
        var request = new RoutingRequest
        {
            Role = AgentRole.Coder,
        };

        // Act
        var decision = strategy.GetModel(request);

        // Assert
        decision.ModelId.Should().Be("llama3.2:7b");
        decision.IsFallback.Should().BeFalse();
    }

    [Fact]
    public void GetModel_WithReviewerRole_ReturnsDefaultModel()
    {
        // Arrange
        var strategy = new SingleModelStrategy(_logger, _config);
        var request = new RoutingRequest
        {
            Role = AgentRole.Reviewer,
        };

        // Act
        var decision = strategy.GetModel(request);

        // Assert
        decision.ModelId.Should().Be("llama3.2:7b");
        decision.IsFallback.Should().BeFalse();
    }

    [Fact]
    public void GetModel_WithDefaultRole_ReturnsDefaultModel()
    {
        // Arrange
        var strategy = new SingleModelStrategy(_logger, _config);
        var request = new RoutingRequest
        {
            Role = AgentRole.Default,
        };

        // Act
        var decision = strategy.GetModel(request);

        // Assert
        decision.ModelId.Should().Be("llama3.2:7b");
        decision.IsFallback.Should().BeFalse();
    }

    [Fact]
    public void GetModel_LogsRoutingDecision()
    {
        // Arrange
        var strategy = new SingleModelStrategy(_logger, _config);
        var request = new RoutingRequest
        {
            Role = AgentRole.Planner,
        };

        // Act
        var decision = strategy.GetModel(request);

        // Assert
        _logger.Received(1).Log(
            LogLevel.Information,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString()!.Contains("llama3.2:7b")),
            null,
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public void GetModel_WithDifferentDefaultModel_UsesConfiguredModel()
    {
        // Arrange
        var customConfig = new RoutingConfig
        {
            Strategy = "single",
            DefaultModel = "qwen2.5:14b",
        };
        var strategy = new SingleModelStrategy(_logger, customConfig);
        var request = new RoutingRequest
        {
            Role = AgentRole.Coder,
        };

        // Act
        var decision = strategy.GetModel(request);

        // Assert
        decision.ModelId.Should().Be("qwen2.5:14b");
        decision.Reason.Should().Contain("qwen2.5:14b");
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new SingleModelStrategy(null!, _config);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    [Fact]
    public void Constructor_WithNullConfig_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new SingleModelStrategy(_logger, null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("config");
    }

    [Fact]
    public void GetModel_WithNullRequest_ThrowsArgumentNullException()
    {
        // Arrange
        var strategy = new SingleModelStrategy(_logger, _config);

        // Act
        var act = () => strategy.GetModel(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("request");
    }
}
