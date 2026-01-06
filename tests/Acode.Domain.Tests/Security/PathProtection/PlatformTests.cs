namespace Acode.Domain.Tests.Security.PathProtection;

using Acode.Domain.Security.PathProtection;
using FluentAssertions;

public class PlatformTests
{
    [Fact]
    public void Platform_ShouldHaveWindowsValue()
    {
        // Arrange & Act
        var platform = Platform.Windows;

        // Assert
        platform.Should().Be(Platform.Windows);
    }

    [Fact]
    public void Platform_ShouldHaveLinuxValue()
    {
        // Arrange & Act
        var platform = Platform.Linux;

        // Assert
        platform.Should().Be(Platform.Linux);
    }

    [Fact]
    public void Platform_ShouldHaveMacOSValue()
    {
        // Arrange & Act
        var platform = Platform.MacOS;

        // Assert
        platform.Should().Be(Platform.MacOS);
    }

    [Fact]
    public void Platform_ShouldHaveAllValue()
    {
        // Arrange & Act
        var platform = Platform.All;

        // Assert
        platform.Should().Be(Platform.All);
    }

    [Fact]
    public void Platform_ShouldHaveExactlyFourValues()
    {
        // Arrange & Act
        var values = Enum.GetValues<Platform>();

        // Assert
        values.Should().HaveCount(4);
    }

    [Fact]
    public void Platform_AllValuesShouldBeDistinct()
    {
        // Arrange & Act
        var values = Enum.GetValues<Platform>();

        // Assert
        values.Should().OnlyHaveUniqueItems();
    }
}
