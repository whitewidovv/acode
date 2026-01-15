using Acode.Application.Providers.Vllm;
using Acode.Domain.Providers.Vllm;
using FluentAssertions;

namespace Acode.Application.Tests.Providers.Vllm;

/// <summary>
/// Tests for VllmLifecycleOptions configuration class.
/// </summary>
public class VllmLifecycleOptionsTests
{
    [Fact]
    public void Test_VllmLifecycleOptions_DefaultValues_AreValid()
    {
        // Arrange & Act
        var options = new VllmLifecycleOptions();

        // Assert
        options.Mode.Should().Be(VllmLifecycleMode.Managed);
        options.StartTimeoutSeconds.Should().Be(30);
        options.HealthCheckIntervalSeconds.Should().Be(60);
        options.MaxRestartsPerMinute.Should().Be(3);
        options.ModelLoadTimeoutSeconds.Should().Be(300);
        options.Port.Should().Be(8000);
        options.StopOnExit.Should().BeFalse();
        options.GpuMemoryUtilization.Should().Be(0.9);
        options.TensorParallelSize.Should().Be(1);

        // Should not throw - defaults are valid
        options.Validate();
    }

    [Fact]
    public void Test_VllmLifecycleOptions_Validate_ThrowsOn_InvalidMode()
    {
        // Arrange
        var options = new VllmLifecycleOptions();
        options.Mode = (VllmLifecycleMode)999; // Invalid mode value

        // Act & Assert
        var exception = Record.Exception(() => options.Validate());
        exception.Should().BeOfType<ArgumentException>();
        exception?.Message.Should().Contain("Invalid lifecycle mode");
    }

    [Fact]
    public void Test_VllmLifecycleOptions_Validate_ThrowsOn_NonPositiveStartTimeout()
    {
        // Arrange
        var options = new VllmLifecycleOptions
        {
            StartTimeoutSeconds = 0,
        };

        // Act & Assert
        var exception = Record.Exception(() => options.Validate());
        exception.Should().BeOfType<ArgumentException>();
        exception?.Message.Should().Contain("StartTimeoutSeconds must be positive");
    }

    [Fact]
    public void Test_VllmLifecycleOptions_Validate_ThrowsOn_NonPositiveHealthCheckInterval()
    {
        // Arrange
        var options = new VllmLifecycleOptions
        {
            HealthCheckIntervalSeconds = -1,
        };

        // Act & Assert
        var exception = Record.Exception(() => options.Validate());
        exception.Should().BeOfType<ArgumentException>();
        exception?.Message.Should().Contain("HealthCheckIntervalSeconds must be positive");
    }

    [Fact]
    public void Test_VllmLifecycleOptions_Validate_ThrowsOn_InvalidMaxRestarts()
    {
        // Arrange
        var options = new VllmLifecycleOptions
        {
            MaxRestartsPerMinute = 0,
        };

        // Act & Assert
        var exception = Record.Exception(() => options.Validate());
        exception.Should().BeOfType<ArgumentException>();
        exception?.Message.Should().Contain("MaxRestartsPerMinute must be positive");
    }

    [Fact]
    public void Test_VllmLifecycleOptions_Validate_ThrowsOn_NonPositiveModelLoadTimeout()
    {
        // Arrange
        var options = new VllmLifecycleOptions
        {
            ModelLoadTimeoutSeconds = -100,
        };

        // Act & Assert
        var exception = Record.Exception(() => options.Validate());
        exception.Should().BeOfType<ArgumentException>();
        exception?.Message.Should().Contain("ModelLoadTimeoutSeconds must be positive");
    }

    [Fact]
    public void Test_VllmLifecycleOptions_Validate_ThrowsOn_PortBelow1024()
    {
        // Arrange
        var options = new VllmLifecycleOptions
        {
            Port = 1023,
        };

        // Act & Assert
        var exception = Record.Exception(() => options.Validate());
        exception.Should().BeOfType<ArgumentException>();
        exception?.Message.Should().Contain("Port must be 1024-65535");
    }

    [Fact]
    public void Test_VllmLifecycleOptions_Validate_ThrowsOn_PortAbove65535()
    {
        // Arrange
        var options = new VllmLifecycleOptions
        {
            Port = 65536,
        };

        // Act & Assert
        var exception = Record.Exception(() => options.Validate());
        exception.Should().BeOfType<ArgumentException>();
        exception?.Message.Should().Contain("Port must be 1024-65535");
    }

    [Fact]
    public void Test_VllmLifecycleOptions_Validate_ThrowsOn_GpuMemory_Below0()
    {
        // Arrange
        var options = new VllmLifecycleOptions
        {
            GpuMemoryUtilization = -0.1,
        };

        // Act & Assert
        var exception = Record.Exception(() => options.Validate());
        exception.Should().BeOfType<ArgumentException>();
        exception?.Message.Should().Contain("GpuMemoryUtilization must be 0.0-1.0");
    }

    [Fact]
    public void Test_VllmLifecycleOptions_Validate_ThrowsOn_GpuMemory_Above1()
    {
        // Arrange
        var options = new VllmLifecycleOptions
        {
            GpuMemoryUtilization = 1.1,
        };

        // Act & Assert
        var exception = Record.Exception(() => options.Validate());
        exception.Should().BeOfType<ArgumentException>();
        exception?.Message.Should().Contain("GpuMemoryUtilization must be 0.0-1.0");
    }

    [Fact]
    public void Test_VllmLifecycleOptions_Validate_ThrowsOn_InvalidTensorParallelSize()
    {
        // Arrange
        var options = new VllmLifecycleOptions
        {
            TensorParallelSize = 0,
        };

        // Act & Assert
        var exception = Record.Exception(() => options.Validate());
        exception.Should().BeOfType<ArgumentException>();
        exception?.Message.Should().Contain("TensorParallelSize must be >= 1");
    }

    [Fact]
    public void Test_VllmLifecycleOptions_Validate_SucceedsOn_ValidConfig()
    {
        // Arrange
        var options = new VllmLifecycleOptions
        {
            Mode = VllmLifecycleMode.Managed,
            StartTimeoutSeconds = 45,
            HealthCheckIntervalSeconds = 90,
            MaxRestartsPerMinute = 5,
            ModelLoadTimeoutSeconds = 600,
            Port = 8080,
            StopOnExit = true,
            GpuMemoryUtilization = 0.95,
            TensorParallelSize = 2,
        };

        // Act & Assert
        var exception = Record.Exception(() => options.Validate());
        exception.Should().BeNull("Valid configuration should not throw");
    }

    [Fact]
    public void Test_VllmLifecycleOptions_Validate_CustomPort_Accepted()
    {
        // Arrange
        var options = new VllmLifecycleOptions
        {
            Port = 9000,
        };

        // Act & Assert
        var exception = Record.Exception(() => options.Validate());
        exception.Should().BeNull("Custom port 9000 should be accepted");
    }

    [Fact]
    public void Test_VllmLifecycleOptions_Validate_CustomGpuMemory_Accepted()
    {
        // Arrange
        var options = new VllmLifecycleOptions
        {
            GpuMemoryUtilization = 0.5,
        };

        // Act & Assert
        var exception = Record.Exception(() => options.Validate());
        exception.Should().BeNull("Custom GPU memory 0.5 should be accepted");
    }

    [Fact]
    public void Test_VllmLifecycleOptions_CustomTensorParallelSize_Accepted()
    {
        // Arrange
        var options = new VllmLifecycleOptions
        {
            TensorParallelSize = 4,
        };

        // Act & Assert
        var exception = Record.Exception(() => options.Validate());
        exception.Should().BeNull("Custom tensor parallel size 4 should be accepted");
    }

    [Fact]
    public void Test_VllmLifecycleOptions_StopOnExit_HasDefault()
    {
        // Arrange & Act
        var options = new VllmLifecycleOptions();

        // Assert
        options.StopOnExit.Should().BeFalse("Default StopOnExit should be false");

        // Act - set to true
        options.StopOnExit = true;
        options.StopOnExit.Should().BeTrue();

        // Act - can be set back to false
        options.StopOnExit = false;
        options.StopOnExit.Should().BeFalse();
    }

    [Fact]
    public void Test_VllmLifecycleOptions_PortBoundaryValues_Accepted()
    {
        // Arrange & Act
        var options1 = new VllmLifecycleOptions { Port = 1024 };
        var options2 = new VllmLifecycleOptions { Port = 65535 };

        // Assert
        Record.Exception(() => options1.Validate()).Should().BeNull("Port 1024 should be valid");
        Record.Exception(() => options2.Validate()).Should().BeNull("Port 65535 should be valid");
    }

    [Fact]
    public void Test_VllmLifecycleOptions_GpuMemoryBoundaryValues_Accepted()
    {
        // Arrange & Act
        var options1 = new VllmLifecycleOptions { GpuMemoryUtilization = 0.0 };
        var options2 = new VllmLifecycleOptions { GpuMemoryUtilization = 1.0 };

        // Assert
        Record.Exception(() => options1.Validate()).Should().BeNull("GPU memory 0.0 should be valid");
        Record.Exception(() => options2.Validate()).Should().BeNull("GPU memory 1.0 should be valid");
    }
}
