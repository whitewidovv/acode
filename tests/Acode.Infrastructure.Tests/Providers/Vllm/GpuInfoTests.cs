using Acode.Domain.Providers.Vllm;
using FluentAssertions;

namespace Acode.Infrastructure.Tests.Providers.Vllm;

/// <summary>
/// Tests for GpuInfo class.
/// </summary>
public class GpuInfoTests
{
    [Fact]
    public void Test_GpuInfo_Construction_WithAllProperties()
    {
        // Arrange & Act
        var gpuInfo = new GpuInfo
        {
            DeviceId = 0,
            Name = "NVIDIA RTX 4090",
            TotalMemoryMb = 24576,
            AvailableMemoryMb = 12288,
            UtilizationPercent = 50.0,
            TemperatureCelsius = 65.5,
        };

        // Assert
        gpuInfo.DeviceId.Should().Be(0);
        gpuInfo.Name.Should().Be("NVIDIA RTX 4090");
        gpuInfo.TotalMemoryMb.Should().Be(24576);
        gpuInfo.AvailableMemoryMb.Should().Be(12288);
        gpuInfo.UtilizationPercent.Should().Be(50.0);
        gpuInfo.TemperatureCelsius.Should().Be(65.5);
    }

    [Fact]
    public void Test_GpuInfo_IsAvailable_True_WhenMemoryAvailable()
    {
        // Arrange & Act
        var gpuInfo = new GpuInfo
        {
            DeviceId = 0,
            Name = "NVIDIA RTX 4090",
            TotalMemoryMb = 24576,
            AvailableMemoryMb = 12288,
        };

        // Assert
        gpuInfo.IsAvailable.Should().BeTrue("GPU should be available when memory is available");
    }

    [Fact]
    public void Test_GpuInfo_IsAvailable_False_WhenNoMemory()
    {
        // Arrange & Act
        var gpuInfo = new GpuInfo
        {
            DeviceId = 0,
            Name = "NVIDIA RTX 4090",
            TotalMemoryMb = 24576,
            AvailableMemoryMb = 0,
        };

        // Assert
        gpuInfo.IsAvailable.Should().BeFalse("GPU should not be available when no memory is available");
    }

    [Fact]
    public void Test_GpuInfo_MemoryUsagePercent_Calculated()
    {
        // Arrange & Act
        var gpuInfo = new GpuInfo
        {
            DeviceId = 0,
            Name = "NVIDIA RTX 4090",
            TotalMemoryMb = 10000,
            AvailableMemoryMb = 2500,
        };

        // Assert
        // Used = 10000 - 2500 = 7500
        // Percentage = (7500 * 100) / 10000 = 75%
        gpuInfo.MemoryUsagePercent.Should().BeApproximately(75.0, 0.1);
    }

    [Fact]
    public void Test_GpuInfo_MemoryUsagePercent_SafeWhenZeroTotal()
    {
        // Arrange & Act
        var gpuInfo = new GpuInfo
        {
            DeviceId = 0,
            Name = "NVIDIA RTX 4090",
            TotalMemoryMb = 0,
            AvailableMemoryMb = 0,
        };

        // Assert
        gpuInfo.MemoryUsagePercent.Should().Be(0.0, "percentage should be 0 when total is 0 to avoid divide by zero");
    }

    [Fact]
    public void Test_GpuInfo_MemoryUsagePercent_FullyUsed()
    {
        // Arrange & Act
        var gpuInfo = new GpuInfo
        {
            DeviceId = 0,
            Name = "NVIDIA RTX 4090",
            TotalMemoryMb = 24576,
            AvailableMemoryMb = 0,
        };

        // Assert
        // Used = 24576 - 0 = 24576
        // Percentage = (24576 * 100) / 24576 = 100%
        gpuInfo.MemoryUsagePercent.Should().BeApproximately(100.0, 0.1);
    }

    [Fact]
    public void Test_GpuInfo_Properties_Immutable()
    {
        // Arrange & Act
        var gpuInfo = new GpuInfo
        {
            DeviceId = 0,
            Name = "NVIDIA RTX 4090",
            TotalMemoryMb = 24576,
            AvailableMemoryMb = 12288,
        };

        // Assert - All property values should be accessible and correct
        gpuInfo.DeviceId.Should().Be(0);
        gpuInfo.Name.Should().Be("NVIDIA RTX 4090");
        gpuInfo.TotalMemoryMb.Should().Be(24576);
        gpuInfo.AvailableMemoryMb.Should().Be(12288);

        // Assert - Computed properties should be readable
        gpuInfo.IsAvailable.Should().BeTrue();
        gpuInfo.MemoryUsagePercent.Should().BeApproximately(50.0, 0.1);
    }

    [Fact]
    public void Test_GpuInfo_TemperatureCelsius_Optional()
    {
        // Arrange & Act
        var gpuInfo = new GpuInfo
        {
            DeviceId = 0,
            Name = "NVIDIA RTX 4090",
            TotalMemoryMb = 24576,
            AvailableMemoryMb = 12288,
            TemperatureCelsius = null,
        };

        // Assert
        gpuInfo.TemperatureCelsius.Should().BeNull("temperature should be optional");
    }
}
