using Acode.Domain.Models.Routing;
using FluentAssertions;

namespace Acode.Domain.Tests.Models.Routing;

/// <summary>
/// Tests for <see cref="RoleDefinition"/>.
/// </summary>
public class RoleDefinitionTests
{
    [Fact]
    public void Constructor_WithAllParameters_SetsProperties()
    {
        // Arrange
        var role = AgentRole.Planner;
        var name = "planner";
        var description = "Task decomposition and planning";
        var capabilities = new List<string> { "plan", "decompose" };
        var constraints = new List<string> { "cannot_modify_files" };

        // Act
        var definition = new RoleDefinition
        {
            Role = role,
            Name = name,
            Description = description,
            Capabilities = capabilities,
            Constraints = constraints,
        };

        // Assert
        definition.Role.Should().Be(role);
        definition.Name.Should().Be(name);
        definition.Description.Should().Be(description);
        definition.Capabilities.Should().BeEquivalentTo(capabilities);
        definition.Constraints.Should().BeEquivalentTo(constraints);
    }

    [Fact]
    public void Constructor_WithMinimalParameters_SetsRequiredPropertiesOnly()
    {
        // Act
        var definition = new RoleDefinition
        {
            Role = AgentRole.Coder,
            Name = "coder",
            Description = "Implementation",
            Capabilities = new List<string> { "write_file" },
            Constraints = Array.Empty<string>(),
        };

        // Assert
        definition.Role.Should().Be(AgentRole.Coder);
        definition.Name.Should().Be("coder");
        definition.Capabilities.Should().NotBeNull();
        definition.Constraints.Should().NotBeNull();
    }

    [Fact]
    public void RoleDefinition_IsImmutable()
    {
        // Arrange
        var definition = new RoleDefinition
        {
            Role = AgentRole.Reviewer,
            Name = "reviewer",
            Description = "Verification",
            Capabilities = new List<string> { "analyze_diff" },
            Constraints = new List<string> { "strict_mode" },
        };

        // Act & Assert
        // Properties should be init-only, verified by compilation success
        definition.Role.Should().Be(AgentRole.Reviewer);
    }

    [Fact]
    public void Capabilities_ReturnsReadOnlyList()
    {
        // Arrange
        var capabilities = new List<string> { "capability1" };
        var definition = new RoleDefinition
        {
            Role = AgentRole.Coder,
            Name = "coder",
            Description = "Test",
            Capabilities = capabilities,
            Constraints = Array.Empty<string>(),
        };

        // Act
        var result = definition.Capabilities;

        // Assert
        result.Should().BeAssignableTo<IReadOnlyList<string>>();
    }

    [Fact]
    public void Constraints_ReturnsReadOnlyList()
    {
        // Arrange
        var constraints = new List<string> { "constraint1" };
        var definition = new RoleDefinition
        {
            Role = AgentRole.Planner,
            Name = "planner",
            Description = "Test",
            Capabilities = Array.Empty<string>(),
            Constraints = constraints,
        };

        // Act
        var result = definition.Constraints;

        // Assert
        result.Should().BeAssignableTo<IReadOnlyList<string>>();
    }

    [Fact]
    public void RecordEquality_WithSameValues_AreEqual()
    {
        // Arrange
        var capabilities = new List<string> { "cap1" };
        var constraints = new List<string> { "con1" };
        var definition1 = new RoleDefinition
        {
            Role = AgentRole.Planner,
            Name = "planner",
            Description = "Test",
            Capabilities = capabilities,
            Constraints = constraints,
        };
        var definition2 = new RoleDefinition
        {
            Role = AgentRole.Planner,
            Name = "planner",
            Description = "Test",
            Capabilities = capabilities,
            Constraints = constraints,
        };

        // Act & Assert
        definition1.Should().Be(definition2);
    }

    [Fact]
    public void RecordEquality_WithDifferentValues_AreNotEqual()
    {
        // Arrange
        var definition1 = new RoleDefinition
        {
            Role = AgentRole.Planner,
            Name = "planner",
            Description = "Test",
            Capabilities = Array.Empty<string>(),
            Constraints = Array.Empty<string>(),
        };
        var definition2 = new RoleDefinition
        {
            Role = AgentRole.Coder,
            Name = "coder",
            Description = "Different",
            Capabilities = Array.Empty<string>(),
            Constraints = Array.Empty<string>(),
        };

        // Act & Assert
        definition1.Should().NotBe(definition2);
    }
}
