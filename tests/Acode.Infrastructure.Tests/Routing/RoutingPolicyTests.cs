namespace Acode.Infrastructure.Tests.Routing;

using System;
using System.Collections.Generic;
using Acode.Application.Inference;
using Acode.Application.Routing;
using Acode.Domain.Modes;
using Acode.Infrastructure.Routing;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

/// <summary>
/// Unit tests for the RoutingPolicy implementation.
/// Tests FR-009 routing policy requirements.
/// </summary>
public class RoutingPolicyTests
{
    private readonly ILogger<RoutingPolicy> _logger = NullLogger<RoutingPolicy>.Instance;
    private readonly ILogger<ModelRegistry> _registryLogger = NullLogger<ModelRegistry>.Instance;

    /// <summary>
    /// Test 1: Should route planner role to configured large model.
    /// AC-013, AC-026: Planner role uses role_models configuration.
    /// </summary>
    [Fact]
    public void Should_Route_Planner_Role_To_Configured_Large_Model()
    {
        // Arrange - Create routing configuration with role-based strategy
        var configuration = new RoutingConfiguration
        {
            Strategy = RoutingStrategy.RoleBased,
            DefaultModel = "llama3.2:7b",
            RoleModels = new Dictionary<AgentRole, string>
            {
                { AgentRole.Planner, "llama3.2:70b" },
                { AgentRole.Coder, "llama3.2:7b" },
                { AgentRole.Reviewer, "llama3.2:70b" },
            },
        };

        var mockProvider = CreateMockProvider("llama3.2:70b", "llama3.2:7b");
        mockProvider.IsHealthyAsync(Arg.Any<CancellationToken>()).Returns(true);

        var modelRegistry = new ModelRegistry(new[] { mockProvider }, _registryLogger);
        var policy = new RoutingPolicy(configuration, modelRegistry, _logger);

        var context = new RoutingContext
        {
            OperatingMode = OperatingMode.LocalOnly,
            TaskComplexity = TaskComplexity.High,
        };

        // Act - Request model for planner role
        var decision = policy.GetModel(AgentRole.Planner, context);

        // Assert - Should select large model for planning
        decision.ModelId.Should().Be("llama3.2:70b");
        decision.IsFallback.Should().BeFalse();
        decision.SelectionReason.Should().Contain("RoleBased");
        decision.SelectedProvider.Should().Be("ollama");
    }

    /// <summary>
    /// Test 2: Should route coder role to configured small model.
    /// AC-014, AC-026: Coder role uses role_models configuration.
    /// </summary>
    [Fact]
    public void Should_Route_Coder_Role_To_Configured_Small_Model()
    {
        // Arrange
        var configuration = new RoutingConfiguration
        {
            Strategy = RoutingStrategy.RoleBased,
            DefaultModel = "llama3.2:7b",
            RoleModels = new Dictionary<AgentRole, string>
            {
                { AgentRole.Planner, "llama3.2:70b" },
                { AgentRole.Coder, "llama3.2:7b" },
                { AgentRole.Reviewer, "llama3.2:70b" },
            },
        };

        var mockProvider = CreateMockProvider("llama3.2:70b", "llama3.2:7b");
        mockProvider.IsHealthyAsync(Arg.Any<CancellationToken>()).Returns(true);

        var modelRegistry = new ModelRegistry(new[] { mockProvider }, _registryLogger);
        var policy = new RoutingPolicy(configuration, modelRegistry, _logger);

        var context = new RoutingContext
        {
            OperatingMode = OperatingMode.LocalOnly,
            TaskComplexity = TaskComplexity.Medium,
        };

        // Act - Request model for coder role
        var decision = policy.GetModel(AgentRole.Coder, context);

        // Assert - Should select small efficient model for coding
        decision.ModelId.Should().Be("llama3.2:7b");
        decision.IsFallback.Should().BeFalse();
        decision.DecisionTimeMs.Should().BeGreaterOrEqualTo(0);
    }

    /// <summary>
    /// Test 3: Should use single model strategy when configured.
    /// AC-023 to AC-025: Single strategy uses same model for all roles.
    /// </summary>
    [Fact]
    public void Should_Use_Single_Model_Strategy_When_Configured()
    {
        // Arrange - Single model strategy uses same model for all roles
        var configuration = new RoutingConfiguration
        {
            Strategy = RoutingStrategy.SingleModel,
            DefaultModel = "llama3.2:70b",
        };

        var mockProvider = CreateMockProvider("llama3.2:70b");
        mockProvider.IsHealthyAsync(Arg.Any<CancellationToken>()).Returns(true);

        var modelRegistry = new ModelRegistry(new[] { mockProvider }, _registryLogger);
        var policy = new RoutingPolicy(configuration, modelRegistry, _logger);

        var context = new RoutingContext { OperatingMode = OperatingMode.LocalOnly };

        // Act - Request models for all three roles
        var plannerDecision = policy.GetModel(AgentRole.Planner, context);
        var coderDecision = policy.GetModel(AgentRole.Coder, context);
        var reviewerDecision = policy.GetModel(AgentRole.Reviewer, context);

        // Assert - All roles should get same model
        plannerDecision.ModelId.Should().Be("llama3.2:70b");
        coderDecision.ModelId.Should().Be("llama3.2:70b");
        reviewerDecision.ModelId.Should().Be("llama3.2:70b");

        // All should indicate single model strategy
        plannerDecision.SelectionReason.Should().Contain("SingleModel");
        coderDecision.SelectionReason.Should().Contain("SingleModel");
        reviewerDecision.SelectionReason.Should().Contain("SingleModel");
    }

    /// <summary>
    /// Test 4: Should fallback to secondary model when primary unavailable.
    /// AC-030 to AC-034: Fallback chain traversal.
    /// </summary>
    [Fact]
    public void Should_Fallback_To_Secondary_Model_When_Primary_Unavailable()
    {
        // Arrange - Configure fallback chain
        var configuration = new RoutingConfiguration
        {
            Strategy = RoutingStrategy.RoleBased,
            DefaultModel = "llama3.2:7b",
            RoleModels = new Dictionary<AgentRole, string>
            {
                { AgentRole.Planner, "llama3.2:70b" },
            },
            FallbackChain = new List<string> { "llama3.2:70b", "llama3.2:13b", "llama3.2:7b" },
        };

        // Primary model (70b) unavailable, fallback (13b) available
        var mockProvider = Substitute.For<IModelProvider>();
        mockProvider.ProviderName.Returns("ollama");
        mockProvider
            .GetSupportedModels()
            .Returns(new[] { "llama3.2:70b", "llama3.2:13b", "llama3.2:7b" });
        mockProvider.Capabilities.Returns(new ProviderCapabilities(supportsTools: true));

        // First call returns false (70b unavailable), second returns true (13b available)
        mockProvider.IsHealthyAsync(Arg.Any<CancellationToken>()).Returns(false, true, true);

        var modelRegistry = new ModelRegistry(new[] { mockProvider }, _registryLogger);
        var policy = new RoutingPolicy(configuration, modelRegistry, _logger);

        var context = new RoutingContext { OperatingMode = OperatingMode.LocalOnly };

        // Act - Request model for planner role
        var decision = policy.GetModel(AgentRole.Planner, context);

        // Assert - Should fall back to second model in chain
        decision.ModelId.Should().Be("llama3.2:13b");
        decision.IsFallback.Should().BeTrue();
        decision.FallbackReason.Should().Be("primary_unavailable");
    }

    /// <summary>
    /// Test 5: Should respect operating mode constraints.
    /// AC-035 to AC-038: Operating mode enforcement.
    /// </summary>
    [Fact]
    public void Should_Respect_Operating_Mode_Constraints()
    {
        // Arrange - Configure with cloud model (should be rejected in local-only mode)
        var configuration = new RoutingConfiguration
        {
            Strategy = RoutingStrategy.SingleModel,
            DefaultModel = "gpt-4:latest",
        };

        var mockCloudProvider = Substitute.For<IModelProvider>();
        mockCloudProvider.ProviderName.Returns("openai"); // Cloud provider
        mockCloudProvider.GetSupportedModels().Returns(new[] { "gpt-4:latest" });
        mockCloudProvider.Capabilities.Returns(new ProviderCapabilities(supportsTools: true));
        mockCloudProvider.IsHealthyAsync(Arg.Any<CancellationToken>()).Returns(true);

        var modelRegistry = new ModelRegistry(new[] { mockCloudProvider }, _registryLogger);
        var policy = new RoutingPolicy(configuration, modelRegistry, _logger);

        var context = new RoutingContext
        {
            OperatingMode = OperatingMode.LocalOnly, // Requires local models only
        };

        // Act & Assert - Should throw exception for mode constraint violation
        var exception = Assert.Throws<RoutingException>(() =>
            policy.GetModel(AgentRole.Coder, context)
        );

        exception.ErrorCode.Should().Be("ACODE-RTE-003");
        exception.Message.Should().Contain("LocalOnly");
    }

    /// <summary>
    /// Test 6: Should throw when all fallback models unavailable.
    /// AC-033: All unavailable fails gracefully.
    /// </summary>
    [Fact]
    public void Should_Throw_When_All_Fallback_Models_Unavailable()
    {
        // Arrange - Configure fallback chain where all models are unavailable
        var configuration = new RoutingConfiguration
        {
            Strategy = RoutingStrategy.SingleModel,
            DefaultModel = "llama3.2:70b",
            FallbackChain = new List<string> { "llama3.2:70b", "llama3.2:13b", "llama3.2:7b" },
        };

        var mockProvider = Substitute.For<IModelProvider>();
        mockProvider.ProviderName.Returns("ollama");
        mockProvider
            .GetSupportedModels()
            .Returns(new[] { "llama3.2:70b", "llama3.2:13b", "llama3.2:7b" });
        mockProvider.Capabilities.Returns(new ProviderCapabilities(supportsTools: true));
        mockProvider.IsHealthyAsync(Arg.Any<CancellationToken>()).Returns(false); // All unavailable

        var modelRegistry = new ModelRegistry(new[] { mockProvider }, _registryLogger);
        var policy = new RoutingPolicy(configuration, modelRegistry, _logger);

        var context = new RoutingContext { OperatingMode = OperatingMode.LocalOnly };

        // Act & Assert - Should throw with helpful error message
        var exception = Assert.Throws<RoutingException>(() =>
            policy.GetModel(AgentRole.Coder, context)
        );

        exception.ErrorCode.Should().Be("ACODE-RTE-004");
        exception.Message.Should().Contain("exhausted");
        exception.Suggestion.Should().Contain("ollama run");
    }

    /// <summary>
    /// Test 7: Should use default model when role not configured.
    /// AC-028: Missing role uses default_model.
    /// </summary>
    [Fact]
    public void Should_Use_Default_Model_When_Role_Not_Configured()
    {
        // Arrange - Configure role-based strategy but leave reviewer unconfigured
        var configuration = new RoutingConfiguration
        {
            Strategy = RoutingStrategy.RoleBased,
            DefaultModel = "llama3.2:7b",
            RoleModels = new Dictionary<AgentRole, string>
            {
                { AgentRole.Planner, "llama3.2:70b" },
                { AgentRole.Coder, "llama3.2:13b" },

                // Reviewer not configured, should use default
            },
        };

        var mockProvider = CreateMockProvider("llama3.2:7b", "llama3.2:70b", "llama3.2:13b");
        mockProvider.IsHealthyAsync(Arg.Any<CancellationToken>()).Returns(true);

        var modelRegistry = new ModelRegistry(new[] { mockProvider }, _registryLogger);
        var policy = new RoutingPolicy(configuration, modelRegistry, _logger);

        var context = new RoutingContext { OperatingMode = OperatingMode.LocalOnly };

        // Act - Request model for unconfigured reviewer role
        var decision = policy.GetModel(AgentRole.Reviewer, context);

        // Assert - Should fall back to default model
        decision.ModelId.Should().Be("llama3.2:7b");
    }

    /// <summary>
    /// Test 8: Should honor user override in routing context.
    /// AC-029: User override bypasses strategy.
    /// </summary>
    [Fact]
    public void Should_Honor_User_Override_In_Routing_Context()
    {
        // Arrange - Configure role-based routing
        var configuration = new RoutingConfiguration
        {
            Strategy = RoutingStrategy.RoleBased,
            DefaultModel = "llama3.2:7b",
            RoleModels = new Dictionary<AgentRole, string> { { AgentRole.Coder, "llama3.2:7b" } },
        };

        var mockProvider = CreateMockProvider("llama3.2:70b", "llama3.2:7b");
        mockProvider.IsHealthyAsync(Arg.Any<CancellationToken>()).Returns(true);

        var modelRegistry = new ModelRegistry(new[] { mockProvider }, _registryLogger);
        var policy = new RoutingPolicy(configuration, modelRegistry, _logger);

        var context = new RoutingContext
        {
            OperatingMode = OperatingMode.LocalOnly,
            UserOverride = "llama3.2:70b", // User explicitly requests large model
        };

        // Act - Request with override
        var decision = policy.GetModel(AgentRole.Coder, context);

        // Assert - Should use override model, not configured model
        decision.ModelId.Should().Be("llama3.2:70b");
        decision.SelectionReason.Should().Contain("user override");
    }

    /// <summary>
    /// Test 9: Should cache availability checks within TTL window.
    /// AC-041: 5 second cache TTL.
    /// </summary>
    [Fact]
    public void Should_Cache_Availability_Checks_Within_TTL_Window()
    {
        // Arrange - Configure routing
        var configuration = new RoutingConfiguration
        {
            Strategy = RoutingStrategy.SingleModel,
            DefaultModel = "llama3.2:7b",
            AvailabilityCacheTtlSeconds = 5,
        };

        var mockProvider = CreateMockProvider("llama3.2:7b");
        mockProvider.IsHealthyAsync(Arg.Any<CancellationToken>()).Returns(true);

        var modelRegistry = new ModelRegistry(new[] { mockProvider }, _registryLogger, 5);
        var policy = new RoutingPolicy(configuration, modelRegistry, _logger);

        var context = new RoutingContext { OperatingMode = OperatingMode.LocalOnly };

        // Act - Make multiple routing requests within cache TTL
        var decision1 = policy.GetModel(AgentRole.Coder, context);
        var decision2 = policy.GetModel(AgentRole.Coder, context);
        var decision3 = policy.GetModel(AgentRole.Coder, context);

        // Assert - All decisions should return the same model
        decision1.ModelId.Should().Be("llama3.2:7b");
        decision2.ModelId.Should().Be("llama3.2:7b");
        decision3.ModelId.Should().Be("llama3.2:7b");

        // Availability should be checked only once (cached for subsequent requests)
        // Note: Due to implementation, health check is called once on first access
        mockProvider.Received(1).IsHealthyAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Test 10: Should validate model ID format before selection.
    /// AC-002: GetModel validates model ID format.
    /// </summary>
    [Fact]
    public void Should_Validate_Model_ID_Format_Before_Selection()
    {
        // Arrange - Configure with invalid model ID
        var configuration = new RoutingConfiguration
        {
            Strategy = RoutingStrategy.SingleModel,
            DefaultModel = "invalid-model-id-no-tag", // Invalid format (no :tag)
        };

        var mockProvider = CreateMockProvider("invalid-model-id-no-tag");
        mockProvider.IsHealthyAsync(Arg.Any<CancellationToken>()).Returns(true);

        var modelRegistry = new ModelRegistry(new[] { mockProvider }, _registryLogger);
        var policy = new RoutingPolicy(configuration, modelRegistry, _logger);

        var context = new RoutingContext { OperatingMode = OperatingMode.LocalOnly };

        // Act & Assert - Should throw for invalid model ID format
        var exception = Assert.Throws<RoutingException>(() =>
            policy.GetModel(AgentRole.Coder, context)
        );

        exception.ErrorCode.Should().Be("ACODE-RTE-002");
        exception.Message.Should().Contain("Invalid model ID");
        exception.Message.Should().Contain("name:tag");
    }

    /// <summary>
    /// Additional test: Should return decision with timestamp.
    /// </summary>
    [Fact]
    public void Should_Return_Decision_With_Timestamp()
    {
        // Arrange
        var configuration = new RoutingConfiguration
        {
            Strategy = RoutingStrategy.SingleModel,
            DefaultModel = "llama3.2:7b",
        };

        var mockProvider = CreateMockProvider("llama3.2:7b");
        mockProvider.IsHealthyAsync(Arg.Any<CancellationToken>()).Returns(true);

        var modelRegistry = new ModelRegistry(new[] { mockProvider }, _registryLogger);
        var policy = new RoutingPolicy(configuration, modelRegistry, _logger);

        var context = new RoutingContext { OperatingMode = OperatingMode.LocalOnly };
        var beforeCall = DateTime.UtcNow;

        // Act
        var decision = policy.GetModel(AgentRole.Coder, context);

        // Assert
        decision.Timestamp.Should().BeOnOrAfter(beforeCall);
        decision.Timestamp.Should().BeOnOrBefore(DateTime.UtcNow);
    }

    /// <summary>
    /// Additional test: Should list available models.
    /// AC-006: ListAvailableModels method exists.
    /// </summary>
    [Fact]
    public void Should_List_Available_Models()
    {
        // Arrange
        var configuration = new RoutingConfiguration
        {
            Strategy = RoutingStrategy.SingleModel,
            DefaultModel = "llama3.2:7b",
        };

        var mockProvider = CreateMockProvider("llama3.2:7b", "llama3.2:70b");
        mockProvider.IsHealthyAsync(Arg.Any<CancellationToken>()).Returns(true);

        var modelRegistry = new ModelRegistry(new[] { mockProvider }, _registryLogger);
        var policy = new RoutingPolicy(configuration, modelRegistry, _logger);

        // Act
        var models = policy.ListAvailableModels();

        // Assert
        models.Should().HaveCount(2);
        models.Should().Contain(m => m.ModelId == "llama3.2:7b");
        models.Should().Contain(m => m.ModelId == "llama3.2:70b");
    }

    /// <summary>
    /// Additional test: Should check model availability.
    /// AC-005: IsModelAvailable method exists.
    /// </summary>
    [Fact]
    public void Should_Check_Model_Availability()
    {
        // Arrange
        var configuration = new RoutingConfiguration
        {
            Strategy = RoutingStrategy.SingleModel,
            DefaultModel = "llama3.2:7b",
        };

        var mockProvider = CreateMockProvider("llama3.2:7b");
        mockProvider.IsHealthyAsync(Arg.Any<CancellationToken>()).Returns(true);

        var modelRegistry = new ModelRegistry(new[] { mockProvider }, _registryLogger);
        var policy = new RoutingPolicy(configuration, modelRegistry, _logger);

        // Act
        var isAvailable = policy.IsModelAvailable("llama3.2:7b");
        var isNotAvailable = policy.IsModelAvailable("nonexistent:model");

        // Assert
        isAvailable.Should().BeTrue();
        isNotAvailable.Should().BeFalse();
    }

    /// <summary>
    /// Additional test: Should get fallback model when requested.
    /// AC-004: GetFallbackModel method exists.
    /// </summary>
    [Fact]
    public void Should_Get_Fallback_Model_When_Requested()
    {
        // Arrange
        var configuration = new RoutingConfiguration
        {
            Strategy = RoutingStrategy.SingleModel,
            DefaultModel = "llama3.2:70b",
            FallbackChain = new List<string> { "llama3.2:70b", "llama3.2:7b" },
        };

        var mockProvider = CreateMockProvider("llama3.2:70b", "llama3.2:7b");
        mockProvider.IsHealthyAsync(Arg.Any<CancellationToken>()).Returns(true);

        var modelRegistry = new ModelRegistry(new[] { mockProvider }, _registryLogger);
        var policy = new RoutingPolicy(configuration, modelRegistry, _logger);

        var context = new RoutingContext { OperatingMode = OperatingMode.LocalOnly };

        // Act
        var fallback = policy.GetFallbackModel(AgentRole.Coder, context);

        // Assert
        fallback.Should().NotBeNull();
        fallback!.IsFallback.Should().BeTrue();
    }

    private static IModelProvider CreateMockProvider(params string[] models)
    {
        var mockProvider = Substitute.For<IModelProvider>();
        mockProvider.ProviderName.Returns("ollama");
        mockProvider.GetSupportedModels().Returns(models);
        mockProvider.Capabilities.Returns(new ProviderCapabilities(supportsTools: true));
        return mockProvider;
    }
}
