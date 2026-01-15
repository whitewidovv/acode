using Acode.Application.Providers.Ollama;
using Acode.Domain.Providers.Ollama;

namespace Acode.Application.Tests.Providers.Ollama;

/// <summary>
/// Tests for OllamaLifecycleOptions configuration class.
/// </summary>
public class OllamaLifecycleOptionsTests
{
    [Fact]
    public void OllamaLifecycleOptions_CanBeInstantiated()
    {
        // Arrange & Act
        var options = new OllamaLifecycleOptions();

        // Assert
        Assert.NotNull(options);
    }

    [Fact]
    public void OllamaLifecycleOptions_HasModeProperty()
    {
        // Arrange
        var options = new OllamaLifecycleOptions();

        // Act
        var mode = options.Mode;

        // Assert
        Assert.Equal(OllamaLifecycleMode.Managed, mode); // Default should be Managed
    }

    [Fact]
    public void OllamaLifecycleOptions_HasStartTimeoutProperty()
    {
        // Arrange
        var options = new OllamaLifecycleOptions();

        // Act
        var timeout = options.StartTimeoutSeconds;

        // Assert
        Assert.True(timeout > 0, "StartTimeoutSeconds should be positive");
        Assert.True(timeout <= 300, "StartTimeoutSeconds should have a reasonable max");
    }

    [Fact]
    public void OllamaLifecycleOptions_HasHealthCheckIntervalProperty()
    {
        // Arrange
        var options = new OllamaLifecycleOptions();

        // Act
        var interval = options.HealthCheckIntervalSeconds;

        // Assert
        Assert.True(interval > 0, "HealthCheckIntervalSeconds should be positive");
    }

    [Fact]
    public void OllamaLifecycleOptions_HasMaxConsecutiveFailuresProperty()
    {
        // Arrange
        var options = new OllamaLifecycleOptions();

        // Act
        var maxFailures = options.MaxConsecutiveFailures;

        // Assert
        Assert.True(maxFailures > 0, "MaxConsecutiveFailures should be positive");
    }

    [Fact]
    public void OllamaLifecycleOptions_HasMaxRestartsPerMinuteProperty()
    {
        // Arrange
        var options = new OllamaLifecycleOptions();

        // Act
        var maxRestarts = options.MaxRestartsPerMinute;

        // Assert
        Assert.True(maxRestarts > 0, "MaxRestartsPerMinute should be positive");
        Assert.True(maxRestarts <= 10, "MaxRestartsPerMinute should be capped");
    }

    [Fact]
    public void OllamaLifecycleOptions_HasStopOnExitProperty()
    {
        // Arrange
        var options = new OllamaLifecycleOptions();

        // Act
        var stopOnExit = options.StopOnExit;

        // Assert
        Assert.IsType<bool>(stopOnExit);
    }

    [Fact]
    public void OllamaLifecycleOptions_HasOllamaBinaryPathProperty()
    {
        // Arrange
        var options = new OllamaLifecycleOptions();

        // Act
        var binaryPath = options.OllamaBinaryPath;

        // Assert
        Assert.NotNull(binaryPath);
        Assert.NotEmpty(binaryPath);
    }

    [Fact]
    public void OllamaLifecycleOptions_HasPortProperty()
    {
        // Arrange
        var options = new OllamaLifecycleOptions();

        // Act
        var port = options.Port;

        // Assert
        Assert.True(port > 0, "Port should be positive");
        Assert.True(port <= 65535, "Port should be valid range");
        Assert.Equal(11434, port); // Default Ollama port
    }

    [Theory]
    [InlineData(OllamaLifecycleMode.Managed)]
    [InlineData(OllamaLifecycleMode.Monitored)]
    [InlineData(OllamaLifecycleMode.External)]
    public void OllamaLifecycleOptions_CanSetMode(OllamaLifecycleMode mode)
    {
        // Arrange
        var options = new OllamaLifecycleOptions { Mode = mode };

        // Act
        var resultMode = options.Mode;

        // Assert
        Assert.Equal(mode, resultMode);
    }

    [Theory]
    [InlineData(10)]
    [InlineData(30)]
    [InlineData(60)]
    public void OllamaLifecycleOptions_CanSetStartTimeout(int seconds)
    {
        // Arrange & Act
        var options = new OllamaLifecycleOptions { StartTimeoutSeconds = seconds };

        // Assert
        Assert.Equal(seconds, options.StartTimeoutSeconds);
    }

    [Theory]
    [InlineData(30)]
    [InlineData(60)]
    [InlineData(120)]
    public void OllamaLifecycleOptions_CanSetHealthCheckInterval(int seconds)
    {
        // Arrange & Act
        var options = new OllamaLifecycleOptions { HealthCheckIntervalSeconds = seconds };

        // Assert
        Assert.Equal(seconds, options.HealthCheckIntervalSeconds);
    }
}
