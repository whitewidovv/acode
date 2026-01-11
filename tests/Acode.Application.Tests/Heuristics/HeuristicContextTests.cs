namespace Acode.Application.Tests.Heuristics;

using Acode.Application.Heuristics;
using Acode.Domain.Roles;
using FluentAssertions;
using Xunit;

/// <summary>
/// Unit tests for <see cref="HeuristicContext"/>.
/// </summary>
public sealed class HeuristicContextTests
{
    /// <summary>
    /// Test that TaskDescription is properly set.
    /// </summary>
    [Fact]
    public void Should_Set_TaskDescription()
    {
        // Arrange & Act
        var context = new HeuristicContext
        {
            TaskDescription = "Test task",
            Files = Array.Empty<string>(),
        };

        // Assert
        context.TaskDescription.Should().Be("Test task");
    }

    /// <summary>
    /// Test that Files can be set with empty array.
    /// </summary>
    [Fact]
    public void Should_Accept_Empty_Files_Array()
    {
        // Arrange & Act
        var context = new HeuristicContext
        {
            TaskDescription = "Test task",
            Files = Array.Empty<string>(),
        };

        // Assert
        context.Files.Should().NotBeNull();
        context.Files.Should().BeEmpty();
    }

    /// <summary>
    /// Test that Files can be set with actual files.
    /// </summary>
    [Fact]
    public void Should_Accept_File_List()
    {
        // Arrange
        var files = new List<string> { "file1.cs", "file2.cs" };

        // Act
        var context = new HeuristicContext { TaskDescription = "Test task", Files = files };

        // Assert
        context.Files.Should().HaveCount(2);
        context.Files.Should().Contain("file1.cs");
        context.Files.Should().Contain("file2.cs");
    }

    /// <summary>
    /// Test that Role defaults to null when not specified.
    /// </summary>
    [Fact]
    public void Role_Should_Default_To_Null()
    {
        // Arrange & Act
        var context = new HeuristicContext
        {
            TaskDescription = "Test task",
            Files = Array.Empty<string>(),
        };

        // Assert
        context.Role.Should().BeNull();
    }

    /// <summary>
    /// Test that Role can be set.
    /// </summary>
    [Fact]
    public void Should_Accept_Role()
    {
        // Arrange & Act
        var context = new HeuristicContext
        {
            TaskDescription = "Test task",
            Files = Array.Empty<string>(),
            Role = AgentRole.Planner,
        };

        // Assert
        context.Role.Should().Be(AgentRole.Planner);
    }

    /// <summary>
    /// Test that Metadata defaults to null when not specified.
    /// </summary>
    [Fact]
    public void Metadata_Should_Default_To_Null()
    {
        // Arrange & Act
        var context = new HeuristicContext
        {
            TaskDescription = "Test task",
            Files = Array.Empty<string>(),
        };

        // Assert
        context.Metadata.Should().BeNull();
    }

    /// <summary>
    /// Test that Metadata can be set.
    /// </summary>
    [Fact]
    public void Should_Accept_Metadata()
    {
        // Arrange
        var metadata = new Dictionary<string, object> { { "key", "value" } };

        // Act
        var context = new HeuristicContext
        {
            TaskDescription = "Test task",
            Files = Array.Empty<string>(),
            Metadata = metadata,
        };

        // Assert
        context.Metadata.Should().ContainKey("key");
        context.Metadata!["key"].Should().Be("value");
    }

    /// <summary>
    /// Test that Files property is read-only.
    /// </summary>
    [Fact]
    public void Files_Should_Be_ReadOnly()
    {
        // Arrange
        var context = new HeuristicContext
        {
            TaskDescription = "Test task",
            Files = new List<string> { "file.cs" },
        };

        // Assert - IReadOnlyList does not expose Add method
        context.Files.Should().BeAssignableTo<IReadOnlyList<string>>();
    }

    /// <summary>
    /// Test that all agent roles can be set.
    /// </summary>
    /// <param name="role">The role to test.</param>
    [Theory]
    [InlineData(AgentRole.Default)]
    [InlineData(AgentRole.Planner)]
    [InlineData(AgentRole.Coder)]
    [InlineData(AgentRole.Reviewer)]
    public void Should_Accept_All_Agent_Roles(AgentRole role)
    {
        // Arrange & Act
        var context = new HeuristicContext
        {
            TaskDescription = "Test task",
            Files = Array.Empty<string>(),
            Role = role,
        };

        // Assert
        context.Role.Should().Be(role);
    }
}
