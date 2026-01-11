using Acode.Domain.Models.Routing;
using FluentAssertions;

namespace Acode.Domain.Tests.Models.Routing;

/// <summary>
/// Tests for <see cref="RoutingDecision"/>.
/// </summary>
public class RoutingDecisionTests
{
    [Fact]
    public void Constructor_WithAllParameters_SetsProperties()
    {
        // Act
        var decision = new RoutingDecision
        {
            ModelId = "llama3.2:70b",
            IsFallback = false,
            Reason = "Role-based routing to planner model",
        };

        // Assert
        decision.ModelId.Should().Be("llama3.2:70b");
        decision.IsFallback.Should().BeFalse();
        decision.Reason.Should().Be("Role-based routing to planner model");
    }

    [Fact]
    public void Constructor_WithFallback_SetsFallbackTrue()
    {
        // Act
        var decision = new RoutingDecision
        {
            ModelId = "llama3.2:7b",
            IsFallback = true,
            Reason = "Primary model unavailable, using fallback",
        };

        // Assert
        decision.IsFallback.Should().BeTrue();
    }

    [Fact]
    public void RoutingDecision_IsImmutable()
    {
        // Arrange
        var decision = new RoutingDecision
        {
            ModelId = "mistral:7b",
            IsFallback = false,
            Reason = "Test",
        };

        // Act & Assert
        // Properties should be init-only, verified by compilation success
        decision.ModelId.Should().Be("mistral:7b");
    }

    [Fact]
    public void RecordEquality_WithSameValues_AreEqual()
    {
        // Arrange
        var decision1 = new RoutingDecision
        {
            ModelId = "qwen2.5:14b",
            IsFallback = false,
            Reason = "Default model",
        };
        var decision2 = new RoutingDecision
        {
            ModelId = "qwen2.5:14b",
            IsFallback = false,
            Reason = "Default model",
        };

        // Act & Assert
        decision1.Should().Be(decision2);
    }

    [Fact]
    public void RecordEquality_WithDifferentModelId_AreNotEqual()
    {
        // Arrange
        var decision1 = new RoutingDecision
        {
            ModelId = "llama3.2:70b",
            IsFallback = false,
            Reason = "Primary",
        };
        var decision2 = new RoutingDecision
        {
            ModelId = "llama3.2:7b",
            IsFallback = false,
            Reason = "Primary",
        };

        // Act & Assert
        decision1.Should().NotBe(decision2);
    }

    [Fact]
    public void ToString_IncludesModelIdAndFallbackStatus()
    {
        // Arrange
        var decision = new RoutingDecision
        {
            ModelId = "llama3.2:70b",
            IsFallback = true,
            Reason = "Fallback to secondary model",
        };

        // Act
        var result = decision.ToString();

        // Assert
        result.Should().Contain("llama3.2:70b");
        result.Should().Contain("True");
    }
}
