#pragma warning disable IDE0005
using Acode.Domain.Providers.Vllm;
using Acode.Infrastructure.Providers.Vllm.Lifecycle;
using FluentAssertions;
#pragma warning restore IDE0005

namespace Acode.Infrastructure.Tests.Providers.Vllm.Lifecycle;

/// <summary>
/// Tests for VllmGpuMonitor GPU detection and monitoring.
/// </summary>
public class VllmGpuMonitorTests
{
    [Fact]
    public async Task Test_VllmGpuMonitor_NoGpus_ReturnsEmptyList()
    {
        // Arrange - Mock with no GPUs
        var monitor = new VllmGpuMonitor();

        // Act
        var gpus = await monitor.GetAvailableGpusAsync();

        // Assert
        gpus.Should().NotBeNull("Should return a list even if no GPUs detected");
        gpus.Should().BeEmpty("Should return empty list when no GPUs available");
    }

    [Fact]
    public async Task Test_VllmGpuMonitor_IsGpuAvailableAsync_False_WhenNoGpus()
    {
        // Arrange
        var monitor = new VllmGpuMonitor();

        // Act
        var isAvailable = await monitor.IsGpuAvailableAsync();

        // Assert
        isAvailable.Should().BeFalse("Should return false when no GPUs detected");
    }

    [Fact]
    public async Task Test_VllmGpuMonitor_DetectGpuError_ReturnsNull_WhenAvailable()
    {
        // Arrange
        var monitor = new VllmGpuMonitor();

        // Act
        var error = await monitor.DetectGpuErrorAsync();

        // Assert
        error.Should().BeNull("Should return null if no GPU error detected");
    }

    [Fact]
    public async Task Test_VllmGpuMonitor_GetGpuUtilization_ReturnsNull_WhenDeviceNotFound()
    {
        // Arrange
        var monitor = new VllmGpuMonitor();

        // Act
        var gpuInfo = await monitor.GetGpuUtilizationAsync(999);

        // Assert
        gpuInfo.Should().BeNull("Should return null for non-existent device");
    }

    [Fact]
    public async Task Test_VllmGpuMonitor_AvailableGpus_ReturnsValidStructure()
    {
        // Arrange
        var monitor = new VllmGpuMonitor();

        // Act
        var gpus = await monitor.GetAvailableGpusAsync();

        // Assert
        if (gpus.Count > 0)
        {
            // If GPUs are found, verify structure
            foreach (var gpu in gpus)
            {
                gpu.DeviceId.Should().BeGreaterThanOrEqualTo(0);
                gpu.Name.Should().NotBeNullOrEmpty("GPU name should not be empty");
                gpu.TotalMemoryMb.Should().BeGreaterThan(0);
                gpu.AvailableMemoryMb.Should().BeGreaterThanOrEqualTo(0);
                gpu.UtilizationPercent.Should().BeGreaterThanOrEqualTo(0).And.BeLessThanOrEqualTo(100);
            }
        }
    }

    [Fact]
    public async Task Test_VllmGpuMonitor_MemoryUtilization_IsValid()
    {
        // Arrange
        var monitor = new VllmGpuMonitor();

        // Act
        var gpus = await monitor.GetAvailableGpusAsync();

        // Assert
        if (gpus.Count > 0)
        {
            foreach (var gpu in gpus)
            {
                // Available should not exceed total
                gpu.AvailableMemoryMb.Should().BeLessThanOrEqualTo(gpu.TotalMemoryMb);

                // Usage should be reasonable
                var usage = gpu.TotalMemoryMb - gpu.AvailableMemoryMb;
                usage.Should().BeGreaterThanOrEqualTo(0);
                usage.Should().BeLessThanOrEqualTo(gpu.TotalMemoryMb);
            }
        }
    }

    [Fact]
    public async Task Test_VllmGpuMonitor_MultipleGpus_ReturnsDistinctDeviceIds()
    {
        // Arrange
        var monitor = new VllmGpuMonitor();

        // Act
        var gpus = await monitor.GetAvailableGpusAsync();

        // Assert
        if (gpus.Count > 1)
        {
            var deviceIds = gpus.Select(g => g.DeviceId).ToList();
            deviceIds.Should().AllSatisfy(x => x.Should().BeGreaterThanOrEqualTo(0));

            // If multiple GPUs, IDs should be unique
            deviceIds.Distinct().Count().Should().Be(deviceIds.Count, "Device IDs should be unique");
        }
    }

    [Fact]
    public async Task Test_VllmGpuMonitor_Temperature_Optional()
    {
        // Arrange
        var monitor = new VllmGpuMonitor();

        // Act
        var gpus = await monitor.GetAvailableGpusAsync();

        // Assert
        if (gpus.Count > 0)
        {
            // Temperature can be null or valid value
            foreach (var gpu in gpus)
            {
                if (gpu.TemperatureCelsius.HasValue)
                {
                    gpu.TemperatureCelsius.Should().BeGreaterThan(-50).And.BeLessThan(150);
                }
            }
        }
    }

    [Fact]
    public async Task Test_VllmGpuMonitor_IsGpuAvailable_ConsistenWithGpuList()
    {
        // Arrange
        var monitor = new VllmGpuMonitor();

        // Act
        var isAvailable = await monitor.IsGpuAvailableAsync();
        var gpus = await monitor.GetAvailableGpusAsync();

        // Assert
        if (gpus.Count > 0)
        {
            isAvailable.Should().BeTrue("IsGpuAvailableAsync should match GetAvailableGpusAsync");
        }
        else
        {
            isAvailable.Should().BeFalse("IsGpuAvailableAsync should return false if no GPUs");
        }
    }

    [Fact]
    public async Task Test_VllmGpuMonitor_ErrorDetection_Descriptive()
    {
        // Arrange
        var monitor = new VllmGpuMonitor();

        // Act
        var error = await monitor.DetectGpuErrorAsync();

        // Assert
        // Error should be null if no error, or contain helpful message if error
        if (error != null)
        {
            error.Should().NotBeEmpty("Error message should be descriptive");
        }
    }

    [Fact]
    public async Task Test_VllmGpuMonitor_ConcurrentCalls_Safe()
    {
        // Arrange
        var monitor = new VllmGpuMonitor();

        // Act - Call multiple times concurrently
        var tasks = Enumerable.Range(0, 5)
            .Select(_ => monitor.GetAvailableGpusAsync())
            .ToList();

        var results = await Task.WhenAll(tasks);

        // Assert - All should succeed
        results.Should().HaveCount(5);
        foreach (var result in results)
        {
            result.Should().NotBeNull();
        }
    }
}
