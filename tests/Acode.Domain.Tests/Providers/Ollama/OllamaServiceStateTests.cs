using Acode.Domain.Providers.Ollama;

namespace Acode.Domain.Tests.Providers.Ollama;

/// <summary>
/// Tests for OllamaServiceState enum.
/// Validates that all required service states are defined correctly.
/// </summary>
public class OllamaServiceStateTests
{
    [Fact]
    public void OllamaServiceState_HasRunningValue()
    {
        // Arrange & Act
        var hasRunning = Enum.GetNames(typeof(OllamaServiceState)).Contains("Running");

        // Assert
        Assert.True(hasRunning, "OllamaServiceState should have Running value");
    }

    [Fact]
    public void OllamaServiceState_HasStartingValue()
    {
        // Arrange & Act
        var hasStarting = Enum.GetNames(typeof(OllamaServiceState)).Contains("Starting");

        // Assert
        Assert.True(hasStarting, "OllamaServiceState should have Starting value");
    }

    [Fact]
    public void OllamaServiceState_HasStoppingValue()
    {
        // Arrange & Act
        var hasStopping = Enum.GetNames(typeof(OllamaServiceState)).Contains("Stopping");

        // Assert
        Assert.True(hasStopping, "OllamaServiceState should have Stopping value");
    }

    [Fact]
    public void OllamaServiceState_HasStoppedValue()
    {
        // Arrange & Act
        var hasStopped = Enum.GetNames(typeof(OllamaServiceState)).Contains("Stopped");

        // Assert
        Assert.True(hasStopped, "OllamaServiceState should have Stopped value");
    }

    [Fact]
    public void OllamaServiceState_HasFailedValue()
    {
        // Arrange & Act
        var hasFailed = Enum.GetNames(typeof(OllamaServiceState)).Contains("Failed");

        // Assert
        Assert.True(hasFailed, "OllamaServiceState should have Failed value");
    }

    [Fact]
    public void OllamaServiceState_HasCrashedValue()
    {
        // Arrange & Act
        var hasCrashed = Enum.GetNames(typeof(OllamaServiceState)).Contains("Crashed");

        // Assert
        Assert.True(hasCrashed, "OllamaServiceState should have Crashed value");
    }

    [Fact]
    public void OllamaServiceState_HasUnknownValue()
    {
        // Arrange & Act
        var hasUnknown = Enum.GetNames(typeof(OllamaServiceState)).Contains("Unknown");

        // Assert
        Assert.True(hasUnknown, "OllamaServiceState should have Unknown value");
    }

    [Fact]
    public void OllamaServiceState_HasExactlySevenValues()
    {
        // Arrange & Act
        var values = Enum.GetNames(typeof(OllamaServiceState));

        // Assert
        Assert.Equal(7, values.Length);
    }

    [Fact]
    public void OllamaServiceState_AllValuesAreIntBased()
    {
        // Arrange & Act
        var underlyingType = Enum.GetUnderlyingType(typeof(OllamaServiceState));

        // Assert
        Assert.Equal(typeof(int), underlyingType);
    }

    [Fact]
    public void OllamaServiceState_CanCastToInt()
    {
        // Arrange
        var running = OllamaServiceState.Running;

        // Act
        var intValue = (int)running;

        // Assert
        Assert.IsType<int>(intValue);
    }

    [Theory]
    [InlineData(OllamaServiceState.Running, "Running")]
    [InlineData(OllamaServiceState.Starting, "Starting")]
    [InlineData(OllamaServiceState.Stopping, "Stopping")]
    [InlineData(OllamaServiceState.Stopped, "Stopped")]
    [InlineData(OllamaServiceState.Failed, "Failed")]
    [InlineData(OllamaServiceState.Crashed, "Crashed")]
    [InlineData(OllamaServiceState.Unknown, "Unknown")]
    public void OllamaServiceState_ValueNameMatches(OllamaServiceState state, string expectedName)
    {
        // Arrange & Act
        var name = state.ToString();

        // Assert
        Assert.Equal(expectedName, name);
    }
}
