using Acode.Infrastructure.Vllm.Health.Metrics;
using FluentAssertions;

namespace Acode.Infrastructure.Tests.Vllm.Health.Metrics;

/// <summary>
/// Tests for VllmMetricsParser.
/// </summary>
public class VllmMetricsParserTests
{
    [Fact]
    public void Should_Parse_Prometheus_Format()
    {
        // Arrange
        var prometheus = @"
vllm_num_requests_running 5
vllm_num_requests_waiting 12
vllm_gpu_cache_usage_perc 67.5
";

        var parser = new VllmMetricsParser();

        // Act
        var metrics = parser.Parse(prometheus);

        // Assert
        metrics.RunningRequests.Should().Be(5);
        metrics.WaitingRequests.Should().Be(12);
        metrics.GpuUtilizationPercent.Should().BeApproximately(67.5, 0.1);
    }

    [Fact]
    public void Should_Parse_Running_Requests()
    {
        // Arrange
        var prometheus = "vllm_num_requests_running 10";
        var parser = new VllmMetricsParser();

        // Act
        var metrics = parser.Parse(prometheus);

        // Assert
        metrics.RunningRequests.Should().Be(10);
    }

    [Fact]
    public void Should_Parse_Waiting_Requests()
    {
        // Arrange
        var prometheus = "vllm_num_requests_waiting 3";
        var parser = new VllmMetricsParser();

        // Act
        var metrics = parser.Parse(prometheus);

        // Assert
        metrics.WaitingRequests.Should().Be(3);
    }

    [Fact]
    public void Should_Parse_GPU_Usage()
    {
        // Arrange
        var prometheus = "vllm_gpu_cache_usage_perc 85.5";
        var parser = new VllmMetricsParser();

        // Act
        var metrics = parser.Parse(prometheus);

        // Assert
        metrics.GpuUtilizationPercent.Should().BeApproximately(85.5, 0.1);
    }

    [Fact]
    public void Should_Handle_Missing_Metrics()
    {
        // Arrange
        var prometheus = "vllm_num_requests_running 5";  // Only running, no waiting or GPU
        var parser = new VllmMetricsParser();

        // Act
        var metrics = parser.Parse(prometheus);

        // Assert
        metrics.RunningRequests.Should().Be(5);
        metrics.WaitingRequests.Should().Be(0);
        metrics.GpuUtilizationPercent.Should().Be(0.0);
    }

    [Fact]
    public void Should_Handle_Malformed_Prometheus()
    {
        // Arrange
        var prometheus = "invalid format here";
        var parser = new VllmMetricsParser();

        // Act
        var metrics = parser.Parse(prometheus);

        // Assert
        metrics.RunningRequests.Should().Be(0);
        metrics.WaitingRequests.Should().Be(0);
        metrics.GpuUtilizationPercent.Should().Be(0.0);
    }

    [Fact]
    public void Should_Handle_Empty_String()
    {
        // Arrange
        var prometheus = string.Empty;
        var parser = new VllmMetricsParser();

        // Act
        var metrics = parser.Parse(prometheus);

        // Assert
        metrics.RunningRequests.Should().Be(0);
        metrics.WaitingRequests.Should().Be(0);
        metrics.GpuUtilizationPercent.Should().Be(0.0);
    }

    [Fact]
    public void Should_Skip_Comments()
    {
        // Arrange
        var prometheus = @"
# HELP vllm_num_requests_running Number of requests running
# TYPE vllm_num_requests_running gauge
vllm_num_requests_running 7
# HELP vllm_num_requests_waiting Number waiting
vllm_num_requests_waiting 2
";
        var parser = new VllmMetricsParser();

        // Act
        var metrics = parser.Parse(prometheus);

        // Assert
        metrics.RunningRequests.Should().Be(7);
        metrics.WaitingRequests.Should().Be(2);
    }

    [Fact]
    public void Should_Handle_Null_String()
    {
        // Arrange
        var parser = new VllmMetricsParser();

        // Act
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
        var metrics = parser.Parse(null);
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.

        // Assert
        metrics.RunningRequests.Should().Be(0);
        metrics.WaitingRequests.Should().Be(0);
        metrics.GpuUtilizationPercent.Should().Be(0.0);
    }

    [Fact]
    public void Should_Handle_Whitespace_Only()
    {
        // Arrange
        var prometheus = "   \n\n   ";
        var parser = new VllmMetricsParser();

        // Act
        var metrics = parser.Parse(prometheus);

        // Assert
        metrics.RunningRequests.Should().Be(0);
        metrics.WaitingRequests.Should().Be(0);
        metrics.GpuUtilizationPercent.Should().Be(0.0);
    }
}
