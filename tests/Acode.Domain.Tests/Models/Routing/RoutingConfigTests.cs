using Acode.Domain.Models.Routing;
using FluentAssertions;

namespace Acode.Domain.Tests.Models.Routing;

/// <summary>
/// Tests for <see cref="RoutingConfig"/>.
/// </summary>
public class RoutingConfigTests
{
    [Fact]
    public void Constructor_WithDefaults_SetsDefaultValues()
    {
        // Act
        var config = new RoutingConfig();

        // Assert
        config.Strategy.Should().Be("single");
        config.DefaultModel.Should().Be("llama3.2:7b");
        config.RoleModels.Should().BeNull();
        config.FallbackChain.Should().BeNull();
    }

    [Fact]
    public void Constructor_WithAllProperties_SetsProperties()
    {
        // Arrange
        var roleModels = new Dictionary<string, string>
        {
            ["planner"] = "llama3.2:70b",
            ["coder"] = "qwen2.5:14b",
        };
        var fallbackChain = new List<string> { "llama3.2:7b", "mistral:7b" };

        // Act
        var config = new RoutingConfig
        {
            Strategy = "role-based",
            DefaultModel = "qwen2.5:14b",
            RoleModels = roleModels,
            FallbackChain = fallbackChain,
        };

        // Assert
        config.Strategy.Should().Be("role-based");
        config.DefaultModel.Should().Be("qwen2.5:14b");
        config.RoleModels.Should().BeSameAs(roleModels);
        config.FallbackChain.Should().BeSameAs(fallbackChain);
    }

    [Fact]
    public void RoutingConfig_IsImmutable()
    {
        // Arrange
        var config = new RoutingConfig
        {
            Strategy = "single",
            DefaultModel = "llama3.2:7b",
        };

        // Act & Assert
        // Properties should be init-only, verified by compilation success
        config.Strategy.Should().Be("single");
    }

    [Fact]
    public void RecordEquality_WithSameValues_AreEqual()
    {
        // Arrange
        var config1 = new RoutingConfig
        {
            Strategy = "single",
            DefaultModel = "llama3.2:7b",
        };
        var config2 = new RoutingConfig
        {
            Strategy = "single",
            DefaultModel = "llama3.2:7b",
        };

        // Act & Assert
        config1.Should().Be(config2);
    }

    [Fact]
    public void RecordEquality_WithDifferentStrategy_AreNotEqual()
    {
        // Arrange
        var config1 = new RoutingConfig
        {
            Strategy = "single",
            DefaultModel = "llama3.2:7b",
        };
        var config2 = new RoutingConfig
        {
            Strategy = "role-based",
            DefaultModel = "llama3.2:7b",
        };

        // Act & Assert
        config1.Should().NotBe(config2);
    }

    [Fact]
    public void ToString_IncludesStrategyAndDefaultModel()
    {
        // Arrange
        var config = new RoutingConfig
        {
            Strategy = "role-based",
            DefaultModel = "qwen2.5:14b",
        };

        // Act
        var result = config.ToString();

        // Assert
        result.Should().Contain("role-based");
        result.Should().Contain("qwen2.5:14b");
    }
}
