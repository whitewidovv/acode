using Acode.Application.Models;
using Acode.Domain.Models.Routing;
using Acode.Infrastructure.Models;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Acode.Infrastructure.Tests.Models;

/// <summary>
/// Tests for <see cref="RoutingPolicy"/>.
/// </summary>
public class RoutingPolicyTests
{
    private readonly ILogger<RoutingPolicy> _logger;
    private readonly ILogger<SingleModelStrategy> _singleStrategyLogger;
    private readonly ILogger<RoleBasedStrategy> _roleStrategyLogger;
    private readonly ILogger<FallbackHandler> _fallbackLogger;
    private readonly IModelAvailabilityChecker _availabilityChecker;
    private readonly RoutingConfig _singleConfig;
    private readonly RoutingConfig _roleBasedConfig;

    public RoutingPolicyTests()
    {
        _logger = Substitute.For<ILogger<RoutingPolicy>>();
        _singleStrategyLogger = Substitute.For<ILogger<SingleModelStrategy>>();
        _roleStrategyLogger = Substitute.For<ILogger<RoleBasedStrategy>>();
        _fallbackLogger = Substitute.For<ILogger<FallbackHandler>>();
        _availabilityChecker = Substitute.For<IModelAvailabilityChecker>();

        _singleConfig = new RoutingConfig
        {
            Strategy = "single",
            DefaultModel = "llama3.2:7b",
        };

        _roleBasedConfig = new RoutingConfig
        {
            Strategy = "role-based",
            DefaultModel = "llama3.2:7b",
            RoleModels = new Dictionary<string, string>
            {
                ["planner"] = "llama3.2:70b",
                ["coder"] = "qwen2.5:14b",
            },
        };
    }

    [Fact]
    public void GetModel_WithSingleStrategy_UsesSingleModelStrategy()
    {
        // Arrange
        _availabilityChecker.IsModelAvailable("llama3.2:7b").Returns(true);

        var strategy = new SingleModelStrategy(_singleStrategyLogger, _singleConfig);
        var fallbackHandler = new FallbackHandler(_fallbackLogger, _availabilityChecker);
        var policy = new RoutingPolicy(_logger, _availabilityChecker, _singleConfig, strategy, fallbackHandler);
        var request = new RoutingRequest { Role = AgentRole.Planner };

        // Act
        var decision = policy.GetModel(request);

        // Assert
        decision.ModelId.Should().Be("llama3.2:7b");
        decision.IsFallback.Should().BeFalse();
    }

    [Fact]
    public void GetModel_WithRoleBasedStrategy_UsesRoleBasedStrategy()
    {
        // Arrange
        _availabilityChecker.IsModelAvailable("llama3.2:70b").Returns(true);

        var strategy = new RoleBasedStrategy(_roleStrategyLogger, _roleBasedConfig);
        var fallbackHandler = new FallbackHandler(_fallbackLogger, _availabilityChecker);
        var policy = new RoutingPolicy(_logger, _availabilityChecker, _roleBasedConfig, strategy, fallbackHandler);
        var request = new RoutingRequest { Role = AgentRole.Planner };

        // Act
        var decision = policy.GetModel(request);

        // Assert
        decision.ModelId.Should().Be("llama3.2:70b");
    }

    [Fact]
    public void GetModel_WithUnavailablePrimaryModel_UsesFallbackChain()
    {
        // Arrange
        var configWithFallback = new RoutingConfig
        {
            Strategy = "single",
            DefaultModel = "llama3.2:7b",
            FallbackChain = new List<string> { "llama3.2:70b", "mistral:7b" },
        };

        _availabilityChecker.IsModelAvailable("llama3.2:7b").Returns(false);
        _availabilityChecker.IsModelAvailable("llama3.2:70b").Returns(false);
        _availabilityChecker.IsModelAvailable("mistral:7b").Returns(true);

        var strategy = new SingleModelStrategy(_singleStrategyLogger, configWithFallback);
        var fallbackHandler = new FallbackHandler(_fallbackLogger, _availabilityChecker);
        var policy = new RoutingPolicy(_logger, _availabilityChecker, configWithFallback, strategy, fallbackHandler);
        var request = new RoutingRequest { Role = AgentRole.Coder };

        // Act
        var decision = policy.GetModel(request);

        // Assert
        decision.ModelId.Should().Be("mistral:7b");
        decision.IsFallback.Should().BeTrue();
    }

    [Fact]
    public void GetModel_WhenAllModelsUnavailable_ThrowsInvalidOperationException()
    {
        // Arrange
        _availabilityChecker.IsModelAvailable(Arg.Any<string>()).Returns(false);
        _availabilityChecker.ListAvailableModels().Returns(new List<string>());

        var strategy = new SingleModelStrategy(_singleStrategyLogger, _singleConfig);
        var fallbackHandler = new FallbackHandler(_fallbackLogger, _availabilityChecker);
        var policy = new RoutingPolicy(_logger, _availabilityChecker, _singleConfig, strategy, fallbackHandler);
        var request = new RoutingRequest { Role = AgentRole.Default };

        // Act
        var act = () => policy.GetModel(request);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*no suitable model*");
    }

    [Fact]
    public void GetModel_WithUserOverride_BypassesStrategy()
    {
        // Arrange
        _availabilityChecker.IsModelAvailable("qwen2.5:32b").Returns(true);

        var strategy = new RoleBasedStrategy(_roleStrategyLogger, _roleBasedConfig);
        var fallbackHandler = new FallbackHandler(_fallbackLogger, _availabilityChecker);
        var policy = new RoutingPolicy(_logger, _availabilityChecker, _roleBasedConfig, strategy, fallbackHandler);
        var request = new RoutingRequest
        {
            Role = AgentRole.Planner,
            UserOverride = "qwen2.5:32b",
        };

        // Act
        var decision = policy.GetModel(request);

        // Assert - Should use override, not role-based planner model
        decision.ModelId.Should().Be("qwen2.5:32b");
        decision.ModelId.Should().NotBe("llama3.2:70b");
    }

    [Fact]
    public void IsModelAvailable_DelegatesToAvailabilityChecker()
    {
        // Arrange
        _availabilityChecker.IsModelAvailable("llama3.2:7b").Returns(true);
        _availabilityChecker.IsModelAvailable("mistral:7b").Returns(false);

        var strategy = new SingleModelStrategy(_singleStrategyLogger, _singleConfig);
        var fallbackHandler = new FallbackHandler(_fallbackLogger, _availabilityChecker);
        var policy = new RoutingPolicy(_logger, _availabilityChecker, _singleConfig, strategy, fallbackHandler);

        // Act & Assert
        policy.IsModelAvailable("llama3.2:7b").Should().BeTrue();
        policy.IsModelAvailable("mistral:7b").Should().BeFalse();
    }

    [Fact]
    public void ListAvailableModels_DelegatesToAvailabilityChecker()
    {
        // Arrange
        var availableModels = new List<string> { "llama3.2:7b", "mistral:7b", "qwen2.5:14b" };
        _availabilityChecker.ListAvailableModels().Returns(availableModels);

        var strategy = new SingleModelStrategy(_singleStrategyLogger, _singleConfig);
        var fallbackHandler = new FallbackHandler(_fallbackLogger, _availabilityChecker);
        var policy = new RoutingPolicy(_logger, _availabilityChecker, _singleConfig, strategy, fallbackHandler);

        // Act
        var models = policy.ListAvailableModels();

        // Assert
        models.Should().Equal(availableModels);
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Arrange
        var strategy = new SingleModelStrategy(_singleStrategyLogger, _singleConfig);
        var fallbackHandler = new FallbackHandler(_fallbackLogger, _availabilityChecker);

        // Act
        var act = () => new RoutingPolicy(null!, _availabilityChecker, _singleConfig, strategy, fallbackHandler);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    [Fact]
    public void Constructor_WithNullAvailabilityChecker_ThrowsArgumentNullException()
    {
        // Arrange
        var strategy = new SingleModelStrategy(_singleStrategyLogger, _singleConfig);
        var fallbackHandler = new FallbackHandler(_fallbackLogger, _availabilityChecker);

        // Act
        var act = () => new RoutingPolicy(_logger, null!, _singleConfig, strategy, fallbackHandler);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("availabilityChecker");
    }

    [Fact]
    public void Constructor_WithNullConfig_ThrowsArgumentNullException()
    {
        // Arrange
        var strategy = new SingleModelStrategy(_singleStrategyLogger, _singleConfig);
        var fallbackHandler = new FallbackHandler(_fallbackLogger, _availabilityChecker);

        // Act
        var act = () => new RoutingPolicy(_logger, _availabilityChecker, null!, strategy, fallbackHandler);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("config");
    }

    [Fact]
    public void Constructor_WithNullStrategy_ThrowsArgumentNullException()
    {
        // Arrange
        var fallbackHandler = new FallbackHandler(_fallbackLogger, _availabilityChecker);

        // Act
        var act = () => new RoutingPolicy(_logger, _availabilityChecker, _singleConfig, null!, fallbackHandler);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("strategy");
    }

    [Fact]
    public void Constructor_WithNullFallbackHandler_ThrowsArgumentNullException()
    {
        // Arrange
        var strategy = new SingleModelStrategy(_singleStrategyLogger, _singleConfig);

        // Act
        var act = () => new RoutingPolicy(_logger, _availabilityChecker, _singleConfig, strategy, null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("fallbackHandler");
    }
}
