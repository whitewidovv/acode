namespace Acode.Domain.Tests.Security;

using Acode.Domain.Security;
using FluentAssertions;
using Xunit;

public class TrustBoundaryTests
{
    [Fact]
    public void TrustBoundary_ShouldHaveUserInputValue()
    {
        // Arrange & Act
        var boundary = TrustBoundary.UserInput;

        // Assert
        boundary.Should().Be(TrustBoundary.UserInput);
    }

    [Fact]
    public void TrustBoundary_ShouldHaveLlmOutputValue()
    {
        // Arrange & Act
        var boundary = TrustBoundary.LlmOutput;

        // Assert
        boundary.Should().Be(TrustBoundary.LlmOutput);
    }

    [Fact]
    public void TrustBoundary_ShouldHaveFileSystemValue()
    {
        // Arrange & Act
        var boundary = TrustBoundary.FileSystem;

        // Assert
        boundary.Should().Be(TrustBoundary.FileSystem);
    }

    [Fact]
    public void TrustBoundary_ShouldHaveProcessExecutionValue()
    {
        // Arrange & Act
        var boundary = TrustBoundary.ProcessExecution;

        // Assert
        boundary.Should().Be(TrustBoundary.ProcessExecution);
    }

    [Fact]
    public void TrustBoundary_ShouldHaveNetworkValue()
    {
        // Arrange & Act
        var boundary = TrustBoundary.Network;

        // Assert
        boundary.Should().Be(TrustBoundary.Network);
    }

    [Fact]
    public void TrustBoundary_ShouldHaveConfigurationValue()
    {
        // Arrange & Act
        var boundary = TrustBoundary.Configuration;

        // Assert
        boundary.Should().Be(TrustBoundary.Configuration);
    }

    [Fact]
    public void TrustBoundary_ShouldHaveRepositoryValue()
    {
        // Arrange & Act
        var boundary = TrustBoundary.Repository;

        // Assert
        boundary.Should().Be(TrustBoundary.Repository);
    }

    [Fact]
    public void TrustBoundary_ShouldHaveExternalDependencyValue()
    {
        // Arrange & Act
        var boundary = TrustBoundary.ExternalDependency;

        // Assert
        boundary.Should().Be(TrustBoundary.ExternalDependency);
    }

    [Fact]
    public void TrustBoundary_ShouldHaveExactlyEightValues()
    {
        // Arrange & Act
        var values = Enum.GetValues<TrustBoundary>();

        // Assert
        values.Should().HaveCount(8);
    }

    [Fact]
    public void TrustBoundary_AllValuesShouldBeDistinct()
    {
        // Arrange & Act
        var values = Enum.GetValues<TrustBoundary>();

        // Assert
        values.Should().OnlyHaveUniqueItems();
    }
}
