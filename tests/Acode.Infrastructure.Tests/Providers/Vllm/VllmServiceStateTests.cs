using Acode.Domain.Providers.Vllm;
using FluentAssertions;

namespace Acode.Infrastructure.Tests.Providers.Vllm;

/// <summary>
/// Tests for VllmServiceState enum.
/// </summary>
public class VllmServiceStateTests
{
    [Fact]
    public void Test_VllmServiceState_HasSevenValues()
    {
        // Arrange & Act
        var values = Enum.GetValues(typeof(VllmServiceState));

        // Assert
        values.Length.Should().Be(7, "VllmServiceState should have exactly 7 values");
    }

    [Fact]
    public void Test_VllmServiceState_Values_Accessible()
    {
        // Arrange & Act & Assert
        // These should not throw - all values accessible by name
        _ = VllmServiceState.Running;
        _ = VllmServiceState.Starting;
        _ = VllmServiceState.Stopping;
        _ = VllmServiceState.Stopped;
        _ = VllmServiceState.Failed;
        _ = VllmServiceState.Crashed;
        _ = VllmServiceState.Unknown;
    }

    [Fact]
    public void Test_VllmServiceState_Values_Numeric()
    {
        // Arrange & Act & Assert
        ((int)VllmServiceState.Running).Should().Be(0);
        ((int)VllmServiceState.Starting).Should().Be(1);
        ((int)VllmServiceState.Stopping).Should().Be(2);
        ((int)VllmServiceState.Stopped).Should().Be(3);
        ((int)VllmServiceState.Failed).Should().Be(4);
        ((int)VllmServiceState.Crashed).Should().Be(5);
        ((int)VllmServiceState.Unknown).Should().Be(6);
    }

    [Fact]
    public void Test_VllmServiceState_ToString()
    {
        // Arrange & Act & Assert
        VllmServiceState.Running.ToString().Should().Be("Running");
        VllmServiceState.Starting.ToString().Should().Be("Starting");
        VllmServiceState.Stopping.ToString().Should().Be("Stopping");
        VllmServiceState.Stopped.ToString().Should().Be("Stopped");
        VllmServiceState.Failed.ToString().Should().Be("Failed");
        VllmServiceState.Crashed.ToString().Should().Be("Crashed");
        VllmServiceState.Unknown.ToString().Should().Be("Unknown");
    }

    [Fact]
    public void Test_VllmServiceState_Parse()
    {
        // Arrange & Act & Assert
        Enum.Parse<VllmServiceState>("Running").Should().Be(VllmServiceState.Running);
        Enum.Parse<VllmServiceState>("Starting").Should().Be(VllmServiceState.Starting);
        Enum.Parse<VllmServiceState>("Stopped").Should().Be(VllmServiceState.Stopped);
        Enum.Parse<VllmServiceState>("Failed").Should().Be(VllmServiceState.Failed);
        Enum.Parse<VllmServiceState>("Crashed").Should().Be(VllmServiceState.Crashed);
        Enum.Parse<VllmServiceState>("Unknown").Should().Be(VllmServiceState.Unknown);
    }
}
