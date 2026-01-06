using Acode.Domain.Modes;
using FluentAssertions;

namespace Acode.Domain.Tests.Modes;

/// <summary>
/// Tests for the OperatingMode enum.
/// Verifies mode definitions per Task 001.a requirements.
/// </summary>
public class OperatingModeTests
{
    [Fact]
    public void OperatingMode_ShouldHaveExactlyThreeValues()
    {
        // Arrange & Act
        var values = Enum.GetValues<OperatingMode>();

        // Assert
        values.Should().HaveCount(3, "Task 001.a requires exactly three operating modes");
        values.Should().Contain(OperatingMode.LocalOnly);
        values.Should().Contain(OperatingMode.Burst);
        values.Should().Contain(OperatingMode.Airgapped);
    }

    [Fact]
    public void OperatingMode_DefaultValue_ShouldBeLocalOnly()
    {
        // Arrange & Act
        var defaultMode = default(OperatingMode);

        // Assert
        defaultMode.Should().Be(
            OperatingMode.LocalOnly,
            "LocalOnly must be the default mode per FR-001-03");
    }

    [Fact]
    public void OperatingMode_ToString_ShouldReturnCorrectNames()
    {
        // Arrange & Act & Assert
        OperatingMode.LocalOnly.ToString().Should().Be("LocalOnly");
        OperatingMode.Burst.ToString().Should().Be("Burst");
        OperatingMode.Airgapped.ToString().Should().Be("Airgapped");
    }
}
