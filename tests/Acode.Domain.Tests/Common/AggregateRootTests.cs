// tests/Acode.Domain.Tests/Common/AggregateRootTests.cs
#pragma warning disable SA1201 // Elements should appear in the correct order
namespace Acode.Domain.Tests.Common;

using Acode.Domain.Common;
using FluentAssertions;
using Xunit;

/// <summary>
/// Tests for AggregateRoot base class.
/// Verifies aggregate root behavior and domain event tracking.
/// </summary>
public sealed class AggregateRootTests
{
    [Fact]
    public void Constructor_InitializesEmptyEventCollection()
    {
        // Arrange & Act
        var aggregate = new TestAggregate { Id = TestAggregateId.From("test-123") };

        // Assert
        aggregate.Id.Should().Be(TestAggregateId.From("test-123"));
    }

    [Fact]
    public void Inheritance_InheritsFromEntity()
    {
        // Arrange
        var aggregate = new TestAggregate { Id = TestAggregateId.From("test-123") };

        // Act & Assert
        aggregate.Should().BeAssignableTo<Entity<TestAggregateId>>();
    }

    [Fact]
    public void Equality_WorksWithAggregateRoots()
    {
        // Arrange
        var id = TestAggregateId.From("test-123");
        var aggregate1 = new TestAggregate { Id = id };
        var aggregate2 = new TestAggregate { Id = id };

        // Act & Assert
        aggregate1.Equals(aggregate2).Should().BeTrue();
        (aggregate1 == aggregate2).Should().BeTrue();
    }

    /// <summary>
    /// Test aggregate root for verifying AggregateRoot behavior.
    /// </summary>
    private sealed class TestAggregate : AggregateRoot<TestAggregateId>
    {
    }

    /// <summary>
    /// Test aggregate ID value object.
    /// </summary>
    private readonly record struct TestAggregateId(string Value)
    {
        public static TestAggregateId From(string value) => new(value);
    }
}
