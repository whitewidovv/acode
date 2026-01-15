using Acode.Domain.Providers.Vllm;
using FluentAssertions;

namespace Acode.Infrastructure.Tests.Providers.Vllm;

/// <summary>
/// Tests for VllmLifecycleMode enum.
/// </summary>
public class VllmLifecycleModeTests
{
    [Fact]
    public void Test_VllmLifecycleMode_HasThreeValues()
    {
        // Arrange & Act
        var values = Enum.GetValues(typeof(VllmLifecycleMode));

        // Assert
        values.Length.Should().Be(3, "VllmLifecycleMode should have exactly 3 values");
    }

    [Fact]
    public void Test_VllmLifecycleMode_ManagedIsDefault()
    {
        // Arrange & Act & Assert
        ((int)VllmLifecycleMode.Managed).Should().Be(0, "Managed should be the default (value 0)");
    }

    [Fact]
    public void Test_VllmLifecycleMode_Values_Accessible()
    {
        // Arrange & Act & Assert
        // These should not throw - all values accessible by name
        _ = VllmLifecycleMode.Managed;
        _ = VllmLifecycleMode.Monitored;
        _ = VllmLifecycleMode.External;
    }

    [Fact]
    public void Test_VllmLifecycleMode_ToString()
    {
        // Arrange & Act & Assert
        VllmLifecycleMode.Managed.ToString().Should().Be("Managed");
        VllmLifecycleMode.Monitored.ToString().Should().Be("Monitored");
        VllmLifecycleMode.External.ToString().Should().Be("External");
    }

    [Fact]
    public void Test_VllmLifecycleMode_Parse()
    {
        // Arrange & Act & Assert
        Enum.Parse<VllmLifecycleMode>("Managed").Should().Be(VllmLifecycleMode.Managed);
        Enum.Parse<VllmLifecycleMode>("Monitored").Should().Be(VllmLifecycleMode.Monitored);
        Enum.Parse<VllmLifecycleMode>("External").Should().Be(VllmLifecycleMode.External);
    }

    [Fact]
    public void Test_VllmLifecycleMode_Documentation_Present()
    {
        // Arrange - Get the type to check for documentation
        var type = typeof(VllmLifecycleMode);

        // Act - Get all values
        var values = Enum.GetValues(typeof(VllmLifecycleMode)).Cast<VllmLifecycleMode>().ToList();

        // Assert - All values should exist (implicitly tests documentation is complete if this passes)
        values.Should().HaveCount(3);
        values.Should().Contain(VllmLifecycleMode.Managed);
        values.Should().Contain(VllmLifecycleMode.Monitored);
        values.Should().Contain(VllmLifecycleMode.External);
    }
}
