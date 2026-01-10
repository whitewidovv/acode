namespace Acode.Domain.Tests.Roles;

using Acode.Domain.Roles;
using FluentAssertions;
using Xunit;

/// <summary>
/// Unit tests for the RoleDefinition value object.
/// Tests AC-008 to AC-014 requirements.
/// </summary>
public class RoleDefinitionTests
{
    /// <summary>
    /// Test: Should define Planner role correctly.
    /// AC-020 to AC-024: Planner definition requirements.
    /// </summary>
    [Fact]
    public void Should_Define_Planner_Role_Correctly()
    {
        // Arrange & Act
        var planner = new RoleDefinition
        {
            Role = AgentRole.Planner,
            Name = "Planner",
            Description = "Task decomposition and planning",
            Capabilities = new[]
            {
                "read_file",
                "list_directory",
                "grep_search",
                "semantic_search",
            },
            Constraints = new[] { "Cannot modify files", "Cannot execute commands" },
            PromptKey = "roles/planner.md",
            ContextStrategy = ContextStrategy.Broad,
        };

        // Assert
        planner.Role.Should().Be(AgentRole.Planner);
        planner.Name.Should().Be("Planner");
        planner.Capabilities.Should().Contain("read_file");
        planner.Capabilities.Should().NotContain("write_file");
        planner.Constraints.Should().Contain("Cannot modify files");
        planner.PromptKey.Should().Be("roles/planner.md");
        planner.ContextStrategy.Should().Be(ContextStrategy.Broad);
    }

    /// <summary>
    /// Test: Should define Coder role correctly.
    /// AC-025 to AC-029: Coder definition requirements.
    /// </summary>
    [Fact]
    public void Should_Define_Coder_Role_Correctly()
    {
        // Arrange & Act
        var coder = new RoleDefinition
        {
            Role = AgentRole.Coder,
            Name = "Coder",
            Description = "Implementation and code changes",
            Capabilities = new[]
            {
                "read_file",
                "write_file",
                "create_file",
                "delete_file",
                "execute_command",
                "run_tests",
            },
            Constraints = new[] { "Must follow plan", "Strict minimal diff" },
            PromptKey = "roles/coder.md",
            ContextStrategy = ContextStrategy.Focused,
        };

        // Assert
        coder.Role.Should().Be(AgentRole.Coder);
        coder.Name.Should().Be("Coder");
        coder.Capabilities.Should().Contain("write_file");
        coder.Capabilities.Should().Contain("execute_command");
        coder.Constraints.Should().Contain("Must follow plan");
        coder.PromptKey.Should().Be("roles/coder.md");
        coder.ContextStrategy.Should().Be(ContextStrategy.Focused);
    }

    /// <summary>
    /// Test: Should define Reviewer role correctly.
    /// AC-030 to AC-034: Reviewer definition requirements.
    /// </summary>
    [Fact]
    public void Should_Define_Reviewer_Role_Correctly()
    {
        // Arrange & Act
        var reviewer = new RoleDefinition
        {
            Role = AgentRole.Reviewer,
            Name = "Reviewer",
            Description = "Verification and quality assurance",
            Capabilities = new[] { "read_file", "list_directory", "analyze_diff", "grep_search" },
            Constraints = new[] { "Cannot modify files", "Cannot execute commands" },
            PromptKey = "roles/reviewer.md",
            ContextStrategy = ContextStrategy.ChangeFocused,
        };

        // Assert
        reviewer.Role.Should().Be(AgentRole.Reviewer);
        reviewer.Name.Should().Be("Reviewer");
        reviewer.Capabilities.Should().Contain("analyze_diff");
        reviewer.Capabilities.Should().NotContain("write_file");
        reviewer.Constraints.Should().Contain("Cannot modify files");
        reviewer.PromptKey.Should().Be("roles/reviewer.md");
        reviewer.ContextStrategy.Should().Be(ContextStrategy.ChangeFocused);
    }

    /// <summary>
    /// Test: Should define Default role correctly.
    /// </summary>
    [Fact]
    public void Should_Define_Default_Role_Correctly()
    {
        // Arrange & Act
        var defaultRole = new RoleDefinition
        {
            Role = AgentRole.Default,
            Name = "Default",
            Description = "General-purpose, no specialization",
            Capabilities = new[] { "all" },
            Constraints = Array.Empty<string>(),
            PromptKey = "system.md",
            ContextStrategy = ContextStrategy.Adaptive,
        };

        // Assert
        defaultRole.Role.Should().Be(AgentRole.Default);
        defaultRole.Name.Should().Be("Default");
        defaultRole.Capabilities.Should().Contain("all");
        defaultRole.Constraints.Should().BeEmpty();
        defaultRole.PromptKey.Should().Be("system.md");
        defaultRole.ContextStrategy.Should().Be(ContextStrategy.Adaptive);
    }

    /// <summary>
    /// Test: Validate should throw for null capabilities.
    /// </summary>
    [Fact]
    public void Validate_Should_Throw_When_Capabilities_Is_Null()
    {
        // Arrange
        var roleDefinition = new RoleDefinition
        {
            Role = AgentRole.Planner,
            Name = "Planner",
            Description = "Planning",
            Capabilities = null!,
            Constraints = Array.Empty<string>(),
            PromptKey = "roles/planner.md",
            ContextStrategy = ContextStrategy.Broad,
        };

        // Act
        Action act = () => roleDefinition.Validate();

        // Assert
        act.Should().Throw<ArgumentException>().WithParameterName("Capabilities");
    }

    /// <summary>
    /// Test: Validate should throw for empty name.
    /// </summary>
    [Fact]
    public void Validate_Should_Throw_When_Name_Is_Empty()
    {
        // Arrange
        var roleDefinition = new RoleDefinition
        {
            Role = AgentRole.Planner,
            Name = string.Empty,
            Description = "Planning",
            Capabilities = new[] { "read_file" },
            Constraints = Array.Empty<string>(),
            PromptKey = "roles/planner.md",
            ContextStrategy = ContextStrategy.Broad,
        };

        // Act
        Action act = () => roleDefinition.Validate();

        // Assert
        act.Should().Throw<ArgumentException>().WithParameterName("Name");
    }

    /// <summary>
    /// Test: Validate should succeed for valid role definition.
    /// </summary>
    [Fact]
    public void Validate_Should_Succeed_For_Valid_Definition()
    {
        // Arrange
        var roleDefinition = new RoleDefinition
        {
            Role = AgentRole.Planner,
            Name = "Planner",
            Description = "Task decomposition and planning",
            Capabilities = new[] { "read_file" },
            Constraints = Array.Empty<string>(),
            PromptKey = "roles/planner.md",
            ContextStrategy = ContextStrategy.Broad,
        };

        // Act
        Action act = () => roleDefinition.Validate();

        // Assert
        act.Should().NotThrow();
    }
}
