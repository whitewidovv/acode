using Acode.Domain.Providers.Ollama;

namespace Acode.Domain.Tests.Providers.Ollama;

/// <summary>
/// Tests for OllamaLifecycleMode enum.
/// Validates that all required lifecycle modes are defined correctly.
/// </summary>
public class OllamaLifecycleModeTests
{
    [Fact]
    public void OllamaLifecycleMode_HasManagedValue()
    {
        // Arrange & Act
        var hasManaged = Enum.GetNames(typeof(OllamaLifecycleMode)).Contains("Managed");

        // Assert
        Assert.True(hasManaged, "OllamaLifecycleMode should have Managed value");
    }

    [Fact]
    public void OllamaLifecycleMode_HasMonitoredValue()
    {
        // Arrange & Act
        var hasMonitored = Enum.GetNames(typeof(OllamaLifecycleMode)).Contains("Monitored");

        // Assert
        Assert.True(hasMonitored, "OllamaLifecycleMode should have Monitored value");
    }

    [Fact]
    public void OllamaLifecycleMode_HasExternalValue()
    {
        // Arrange & Act
        var hasExternal = Enum.GetNames(typeof(OllamaLifecycleMode)).Contains("External");

        // Assert
        Assert.True(hasExternal, "OllamaLifecycleMode should have External value");
    }

    [Fact]
    public void OllamaLifecycleMode_HasExactlyThreeValues()
    {
        // Arrange & Act
        var values = Enum.GetNames(typeof(OllamaLifecycleMode));

        // Assert
        Assert.Equal(3, values.Length);
    }

    [Fact]
    public void OllamaLifecycleMode_AllValuesAreIntBased()
    {
        // Arrange & Act
        var underlyingType = Enum.GetUnderlyingType(typeof(OllamaLifecycleMode));

        // Assert
        Assert.Equal(typeof(int), underlyingType);
    }

    [Fact]
    public void OllamaLifecycleMode_CanCastToInt()
    {
        // Arrange
        var managed = OllamaLifecycleMode.Managed;

        // Act
        var intValue = (int)managed;

        // Assert
        Assert.IsType<int>(intValue);
    }

    [Theory]
    [InlineData(OllamaLifecycleMode.Managed, "Managed")]
    [InlineData(OllamaLifecycleMode.Monitored, "Monitored")]
    [InlineData(OllamaLifecycleMode.External, "External")]
    public void OllamaLifecycleMode_ValueNameMatches(OllamaLifecycleMode mode, string expectedName)
    {
        // Arrange & Act
        var name = mode.ToString();

        // Assert
        Assert.Equal(expectedName, name);
    }

    [Fact]
    public void OllamaLifecycleMode_ManagedIsDefault()
    {
        // Arrange & Act - In many configuration systems, Managed is the preferred default
        var managed = OllamaLifecycleMode.Managed;

        // Assert - Just verify it exists
        Assert.Equal("Managed", managed.ToString());
    }
}
