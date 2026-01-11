// tests/Acode.Domain.Tests/Common/EntityTests.cs
#pragma warning disable SA1201 // Elements should appear in the correct order
namespace Acode.Domain.Tests.Common;

using Acode.Domain.Common;
using FluentAssertions;
using Xunit;

/// <summary>
/// Tests for Entity base class.
/// Verifies identity-based equality and proper inheritance behavior.
/// </summary>
public sealed class EntityTests
{
    [Fact]
    public void Equality_WithSameId_ReturnsTrue()
    {
        // Arrange
        var id = TestEntityId.From("test-123");
        var entity1 = new TestEntity { Id = id };
        var entity2 = new TestEntity { Id = id };

        // Act & Assert
        entity1.Equals(entity2).Should().BeTrue();
        (entity1 == entity2).Should().BeTrue();
        entity1.GetHashCode().Should().Be(entity2.GetHashCode());
    }

    [Fact]
    public void Equality_WithDifferentId_ReturnsFalse()
    {
        // Arrange
        var entity1 = new TestEntity { Id = TestEntityId.From("test-123") };
        var entity2 = new TestEntity { Id = TestEntityId.From("test-456") };

        // Act & Assert
        entity1.Equals(entity2).Should().BeFalse();
        (entity1 == entity2).Should().BeFalse();
    }

    [Fact]
    public void Equality_WithNull_ReturnsFalse()
    {
        // Arrange
        var entity = new TestEntity { Id = TestEntityId.From("test-123") };

        // Act & Assert
        entity.Equals(null).Should().BeFalse();
        (entity == null).Should().BeFalse();
    }

    [Fact]
    public void Equality_WithDifferentType_ReturnsFalse()
    {
        // Arrange
        var entity1 = new TestEntity { Id = TestEntityId.From("test-123") };
        var entity2 = new AnotherTestEntity { Id = TestEntityId.From("test-123") };

        // Act & Assert
        entity1.Equals(entity2).Should().BeFalse();
    }

    [Fact]
    public void GetHashCode_UsesIdHashCode()
    {
        // Arrange
        var id = TestEntityId.From("test-123");
        var entity = new TestEntity { Id = id };

        // Act
        var entityHash = entity.GetHashCode();
        var idHash = id.GetHashCode();

        // Assert
        entityHash.Should().Be(idHash);
    }

    /// <summary>
    /// Test entity for verifying Entity behavior.
    /// </summary>
    private sealed class TestEntity : Entity<TestEntityId>
    {
    }

    /// <summary>
    /// Another test entity for verifying type-based equality.
    /// </summary>
    private sealed class AnotherTestEntity : Entity<TestEntityId>
    {
    }

    /// <summary>
    /// Test entity ID value object.
    /// </summary>
    private readonly record struct TestEntityId(string Value)
    {
        public static TestEntityId From(string value) => new(value);
    }
}
