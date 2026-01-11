using Acode.Domain.Commands;
using FluentAssertions;

namespace Acode.Domain.Tests.Commands;

/// <summary>
/// Tests for ExitCodes constants and descriptions.
/// Verifies common exit codes are defined and described correctly.
/// </summary>
public class ExitCodesTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(124)]
    [InlineData(126)]
    [InlineData(127)]
    [InlineData(130)]
    public void ExitCodes_CommonCodes_ShouldHaveDescriptions(int exitCode)
    {
        // Act
        var description = ExitCodes.GetDescription(exitCode);

        // Assert
        description.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void ExitCodes_Success_ShouldBeZero()
    {
        // Assert
        ExitCodes.Success.Should().Be(0, "per FR-002c-95");
    }

    [Fact]
    public void ExitCodes_GeneralError_ShouldBeOne()
    {
        // Assert
        ExitCodes.GeneralError.Should().Be(1, "per FR-002c-104");
    }

    [Fact]
    public void ExitCodes_Misuse_ShouldBeTwo()
    {
        // Assert
        ExitCodes.Misuse.Should().Be(2, "per FR-002c-105");
    }

    [Fact]
    public void ExitCodes_Timeout_ShouldBe124()
    {
        // Assert
        ExitCodes.Timeout.Should().Be(124, "per FR-002c-106");
    }

    [Fact]
    public void ExitCodes_NotExecutable_ShouldBe126()
    {
        // Assert
        ExitCodes.NotExecutable.Should().Be(126, "per FR-002c-107");
    }

    [Fact]
    public void ExitCodes_NotFound_ShouldBe127()
    {
        // Assert
        ExitCodes.NotFound.Should().Be(127, "per FR-002c-108");
    }

    [Fact]
    public void ExitCodes_Interrupted_ShouldBe130()
    {
        // Assert
        ExitCodes.Interrupted.Should().Be(130, "per FR-002c-109");
    }

    [Fact]
    public void ExitCodes_GetDescription_Success_ShouldReturnSuccessMessage()
    {
        // Act
        var description = ExitCodes.GetDescription(ExitCodes.Success);

        // Assert
        description.Should().Be("Success");
    }

    [Fact]
    public void ExitCodes_GetDescription_GeneralError_ShouldReturnErrorMessage()
    {
        // Act
        var description = ExitCodes.GetDescription(ExitCodes.GeneralError);

        // Assert
        description.Should().Be("General error");
    }

    [Fact]
    public void ExitCodes_GetDescription_Timeout_ShouldReturnTimeoutMessage()
    {
        // Act
        var description = ExitCodes.GetDescription(ExitCodes.Timeout);

        // Assert
        description.Should().Be("Command timed out");
    }

    [Fact]
    public void ExitCodes_GetDescription_SignalCodes_ShouldReturnSignalMessage()
    {
        // Arrange - signal termination returns 128 + signal number
        var sigterm = 128 + 15; // SIGTERM

        // Act
        var description = ExitCodes.GetDescription(sigterm);

        // Assert
        description.Should().Contain("signal");
        description.Should().Contain("15");
    }

    [Fact]
    public void ExitCodes_GetDescription_UnknownCode_ShouldReturnGenericMessage()
    {
        // Arrange
        var unknownCode = 42;

        // Act
        var description = ExitCodes.GetDescription(unknownCode);

        // Assert
        description.Should().Contain("Failed");
        description.Should().Contain("42");
    }

    [Fact]
    public void ExitCodes_GetDescription_NegativeCode_ShouldBeHandled()
    {
        // Arrange - Windows can return negative exit codes
        var negativeCode = -1;

        // Act
        var description = ExitCodes.GetDescription(negativeCode);

        // Assert
        description.Should().NotBeNullOrWhiteSpace();
        description.Should().Contain("-1");
    }
}
