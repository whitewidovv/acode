using Acode.Domain.Models.Routing;
using Acode.Infrastructure.Models;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Acode.Infrastructure.Tests.Models;

/// <summary>
/// Tests for <see cref="RoleBasedStrategy"/>.
/// </summary>
public class RoleBasedStrategyTests
{
    private readonly ILogger<RoleBasedStrategy> _logger;

    public RoleBasedStrategyTests()
    {
        _logger = Substitute.For<ILogger<RoleBasedStrategy>>();
    }

    [Fact]
    public void GetModel_WithPlannerRole_UsesConfiguredPlannerModel()
    {
        // Arrange
        var config = new RoutingConfig
        {
            Strategy = "role-based",
            DefaultModel = "llama3.2:7b",
            RoleModels = new Dictionary<string, string>
            {
                ["planner"] = "llama3.2:70b",
                ["coder"] = "qwen2.5:14b",
                ["reviewer"] = "llama3.2:70b",
            },
        };
        var strategy = new RoleBasedStrategy(_logger, config);
        var request = new RoutingRequest { Role = AgentRole.Planner };

        // Act
        var decision = strategy.GetModel(request);

        // Assert
        decision.ModelId.Should().Be("llama3.2:70b");
        decision.IsFallback.Should().BeFalse();
        decision.Reason.Should().Contain("planner");
        decision.Reason.Should().Contain("llama3.2:70b");
    }

    [Fact]
    public void GetModel_WithCoderRole_UsesConfiguredCoderModel()
    {
        // Arrange
        var config = new RoutingConfig
        {
            Strategy = "role-based",
            DefaultModel = "llama3.2:7b",
            RoleModels = new Dictionary<string, string>
            {
                ["planner"] = "llama3.2:70b",
                ["coder"] = "qwen2.5:14b",
                ["reviewer"] = "llama3.2:70b",
            },
        };
        var strategy = new RoleBasedStrategy(_logger, config);
        var request = new RoutingRequest { Role = AgentRole.Coder };

        // Act
        var decision = strategy.GetModel(request);

        // Assert
        decision.ModelId.Should().Be("qwen2.5:14b");
        decision.IsFallback.Should().BeFalse();
    }

    [Fact]
    public void GetModel_WithReviewerRole_UsesConfiguredReviewerModel()
    {
        // Arrange
        var config = new RoutingConfig
        {
            Strategy = "role-based",
            DefaultModel = "llama3.2:7b",
            RoleModels = new Dictionary<string, string>
            {
                ["planner"] = "llama3.2:70b",
                ["coder"] = "qwen2.5:14b",
                ["reviewer"] = "mistral:7b",
            },
        };
        var strategy = new RoleBasedStrategy(_logger, config);
        var request = new RoutingRequest { Role = AgentRole.Reviewer };

        // Act
        var decision = strategy.GetModel(request);

        // Assert
        decision.ModelId.Should().Be("mistral:7b");
        decision.IsFallback.Should().BeFalse();
    }

    [Fact]
    public void GetModel_WithUnmappedRole_FallsBackToDefaultModel()
    {
        // Arrange
        var config = new RoutingConfig
        {
            Strategy = "role-based",
            DefaultModel = "llama3.2:7b",
            RoleModels = new Dictionary<string, string>
            {
                ["planner"] = "llama3.2:70b",

                // coder and reviewer not mapped
            },
        };
        var strategy = new RoleBasedStrategy(_logger, config);
        var request = new RoutingRequest { Role = AgentRole.Coder };

        // Act
        var decision = strategy.GetModel(request);

        // Assert
        decision.ModelId.Should().Be("llama3.2:7b");
        decision.IsFallback.Should().BeTrue();
        decision.Reason.Should().Contain("default");
    }

    [Fact]
    public void GetModel_WithDefaultRole_FallsBackToDefaultModel()
    {
        // Arrange
        var config = new RoutingConfig
        {
            Strategy = "role-based",
            DefaultModel = "llama3.2:7b",
            RoleModels = new Dictionary<string, string>
            {
                ["planner"] = "llama3.2:70b",
                ["coder"] = "qwen2.5:14b",
            },
        };
        var strategy = new RoleBasedStrategy(_logger, config);
        var request = new RoutingRequest { Role = AgentRole.Default };

        // Act
        var decision = strategy.GetModel(request);

        // Assert
        decision.ModelId.Should().Be("llama3.2:7b");
        decision.IsFallback.Should().BeTrue();
    }

    [Fact]
    public void GetModel_WithNullRoleModels_FallsBackToDefaultModel()
    {
        // Arrange
        var config = new RoutingConfig
        {
            Strategy = "role-based",
            DefaultModel = "llama3.2:7b",
            RoleModels = null,
        };
        var strategy = new RoleBasedStrategy(_logger, config);
        var request = new RoutingRequest { Role = AgentRole.Planner };

        // Act
        var decision = strategy.GetModel(request);

        // Assert
        decision.ModelId.Should().Be("llama3.2:7b");
        decision.IsFallback.Should().BeTrue();
    }

    [Fact]
    public void GetModel_LogsRoutingDecision()
    {
        // Arrange
        var config = new RoutingConfig
        {
            Strategy = "role-based",
            DefaultModel = "llama3.2:7b",
            RoleModels = new Dictionary<string, string>
            {
                ["planner"] = "llama3.2:70b",
            },
        };
        var strategy = new RoleBasedStrategy(_logger, config);
        var request = new RoutingRequest { Role = AgentRole.Planner };

        // Act
        var decision = strategy.GetModel(request);

        // Assert
        _logger.Received(1).Log(
            LogLevel.Information,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString()!.Contains("llama3.2:70b")),
            null,
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public void GetModel_WithCaseInsensitiveRoleName_MatchesCorrectly()
    {
        // Arrange - config has lowercase "planner"
        var config = new RoutingConfig
        {
            Strategy = "role-based",
            DefaultModel = "llama3.2:7b",
            RoleModels = new Dictionary<string, string>
            {
                ["PlAnNeR"] = "llama3.2:70b", // Mixed case in config
            },
        };
        var strategy = new RoleBasedStrategy(_logger, config);
        var request = new RoutingRequest { Role = AgentRole.Planner };

        // Act
        var decision = strategy.GetModel(request);

        // Assert
        decision.ModelId.Should().Be("llama3.2:70b");
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Arrange
        var config = new RoutingConfig
        {
            Strategy = "role-based",
            DefaultModel = "llama3.2:7b",
        };

        // Act
        var act = () => new RoleBasedStrategy(null!, config);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    [Fact]
    public void Constructor_WithNullConfig_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new RoleBasedStrategy(_logger, null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("config");
    }

    [Fact]
    public void GetModel_WithNullRequest_ThrowsArgumentNullException()
    {
        // Arrange
        var config = new RoutingConfig
        {
            Strategy = "role-based",
            DefaultModel = "llama3.2:7b",
        };
        var strategy = new RoleBasedStrategy(_logger, config);

        // Act
        var act = () => strategy.GetModel(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("request");
    }
}
