using Acode.Infrastructure.Vllm.Health;
using FluentAssertions;

namespace Acode.Infrastructure.Tests.Vllm.Health;

public class VllmHealthConfigurationTests
{
    [Fact]
    public void Should_Have_Default_Values()
    {
        // Arrange & Act
        var config = new VllmHealthConfiguration();

        // Assert
        config.HealthEndpoint.Should().Be("/health");
        config.TimeoutSeconds.Should().Be(10);
        config.HealthyThresholdMs.Should().Be(1000);
        config.DegradedThresholdMs.Should().Be(5000);
        config.LoadMonitoring.Should().NotBeNull();
        config.LoadMonitoring.Enabled.Should().BeTrue();
        config.LoadMonitoring.MetricsEndpoint.Should().Be("/metrics");
        config.LoadMonitoring.QueueThreshold.Should().Be(10);
        config.LoadMonitoring.GpuThresholdPercent.Should().Be(95.0);
    }

    [Fact]
    public void Should_Allow_Setting_Properties()
    {
        // Arrange
        var config = new VllmHealthConfiguration
        {
            HealthEndpoint = "/health-check",
            TimeoutSeconds = 15,
            HealthyThresholdMs = 500,
            DegradedThresholdMs = 3000
        };

        // Act & Assert
        config.HealthEndpoint.Should().Be("/health-check");
        config.TimeoutSeconds.Should().Be(15);
        config.HealthyThresholdMs.Should().Be(500);
        config.DegradedThresholdMs.Should().Be(3000);
    }

    [Fact]
    public void Should_Validate_TimeoutSeconds()
    {
        // Arrange
        var config = new VllmHealthConfiguration { TimeoutSeconds = 0 };

        // Act & Assert
        var act = () => config.Validate();
        act.Should().Throw<ArgumentException>()
            .WithMessage("*TimeoutSeconds*")
            .WithParameterName("TimeoutSeconds");
    }

    [Fact]
    public void Should_Validate_HealthyThresholdMs()
    {
        // Arrange
        var config = new VllmHealthConfiguration { HealthyThresholdMs = -1 };

        // Act & Assert
        var act = () => config.Validate();
        act.Should().Throw<ArgumentException>()
            .WithMessage("*HealthyThresholdMs*")
            .WithParameterName("HealthyThresholdMs");
    }

    [Fact]
    public void Should_Validate_DegradedThresholdMs_Greater_Than_Healthy()
    {
        // Arrange
        var config = new VllmHealthConfiguration
        {
            HealthyThresholdMs = 2000,
            DegradedThresholdMs = 1000
        };

        // Act & Assert
        var act = () => config.Validate();
        act.Should().Throw<ArgumentException>()
            .WithMessage("*DegradedThresholdMs*HealthyThresholdMs*")
            .WithParameterName("DegradedThresholdMs");
    }

    [Fact]
    public void Should_Pass_Validation_With_Valid_Config()
    {
        // Arrange
        var config = new VllmHealthConfiguration
        {
            TimeoutSeconds = 10,
            HealthyThresholdMs = 1000,
            DegradedThresholdMs = 5000
        };

        // Act & Assert - Should not throw
        config.Validate();
    }

    [Fact]
    public void Should_Allow_Customizing_LoadMonitoring()
    {
        // Arrange
        var config = new VllmHealthConfiguration();

        // Act
        config.LoadMonitoring.Enabled = false;
        config.LoadMonitoring.MetricsEndpoint = "/custom-metrics";
        config.LoadMonitoring.QueueThreshold = 20;
        config.LoadMonitoring.GpuThresholdPercent = 80.0;

        // Assert
        config.LoadMonitoring.Enabled.Should().BeFalse();
        config.LoadMonitoring.MetricsEndpoint.Should().Be("/custom-metrics");
        config.LoadMonitoring.QueueThreshold.Should().Be(20);
        config.LoadMonitoring.GpuThresholdPercent.Should().Be(80.0);
    }
}
