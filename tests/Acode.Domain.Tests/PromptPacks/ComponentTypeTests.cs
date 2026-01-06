using Acode.Domain.PromptPacks;
using FluentAssertions;
using Xunit;

namespace Acode.Domain.Tests.PromptPacks;

/// <summary>
/// Tests for <see cref="ComponentType"/> enum.
/// </summary>
public class ComponentTypeTests
{
    [Fact]
    public void AllTypes_ShouldBeDefined()
    {
        // Arrange & Act
        var types = Enum.GetValues<ComponentType>();

        // Assert
        types.Should().Contain(ComponentType.System);
        types.Should().Contain(ComponentType.Role);
        types.Should().Contain(ComponentType.Language);
        types.Should().Contain(ComponentType.Framework);
        types.Should().Contain(ComponentType.Custom);
    }

    [Fact]
    public void System_ShouldHaveValue()
    {
        // Act
        var value = (int)ComponentType.System;

        // Assert
        value.Should().Be(0);
    }

    [Fact]
    public void IsValidType_ShouldReturnTrueForDefinedTypes()
    {
        // Arrange
        var validType = ComponentType.Role;

        // Act
        var isValid = Enum.IsDefined(typeof(ComponentType), validType);

        // Assert
        isValid.Should().BeTrue();
    }

    [Fact]
    public void IsValidType_ShouldReturnFalseForInvalidValue()
    {
        // Arrange
        var invalidType = (ComponentType)999;

        // Act
        var isValid = Enum.IsDefined(typeof(ComponentType), invalidType);

        // Assert
        isValid.Should().BeFalse();
    }
}
