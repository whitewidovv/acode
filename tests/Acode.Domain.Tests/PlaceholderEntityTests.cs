using Acode.Domain.Entities;
using FluentAssertions;

namespace Acode.Domain.Tests;

/// <summary>
/// Placeholder tests to verify test infrastructure.
/// </summary>
public class PlaceholderEntityTests
{
    [Fact]
    public void Constructor_ShouldInitializeWithNewGuid_WhenCalled()
    {
        // Arrange & Act
#pragma warning disable CS0618 // Obsolete warning expected for placeholder
        var entity = new PlaceholderEntity();
#pragma warning restore CS0618

        // Assert
        entity.Id.Should().NotBeEmpty();
    }

    [Fact]
    public void Constructor_ShouldInitializeWithEmptyName_WhenCalled()
    {
        // Arrange & Act
#pragma warning disable CS0618
        var entity = new PlaceholderEntity();
#pragma warning restore CS0618

        // Assert
        entity.Name.Should().BeEmpty();
    }

    [Fact]
    public void CreatedAt_ShouldBeRecentUtcTime_WhenEntityCreated()
    {
        // Arrange
        var before = DateTime.UtcNow;

        // Act
#pragma warning disable CS0618
        var entity = new PlaceholderEntity();
#pragma warning restore CS0618
        var after = DateTime.UtcNow;

        // Assert
        entity.CreatedAt.Should().BeOnOrAfter(before);
        entity.CreatedAt.Should().BeOnOrBefore(after);
    }
}
