namespace Acode.Domain.Tests.Security;

using Acode.Domain.Security;
using FluentAssertions;
using Xunit;

public class ThreatActorTests
{
    [Fact]
    public void ThreatActor_ShouldHaveUserValue()
    {
        // Arrange & Act
        var actor = ThreatActor.User;

        // Assert
        actor.Should().Be(ThreatActor.User);
    }

    [Fact]
    public void ThreatActor_ShouldHaveAgentValue()
    {
        // Arrange & Act
        var actor = ThreatActor.Agent;

        // Assert
        actor.Should().Be(ThreatActor.Agent);
    }

    [Fact]
    public void ThreatActor_ShouldHaveExternalLlmValue()
    {
        // Arrange & Act
        var actor = ThreatActor.ExternalLlm;

        // Assert
        actor.Should().Be(ThreatActor.ExternalLlm);
    }

    [Fact]
    public void ThreatActor_ShouldHaveLocalModelValue()
    {
        // Arrange & Act
        var actor = ThreatActor.LocalModel;

        // Assert
        actor.Should().Be(ThreatActor.LocalModel);
    }

    [Fact]
    public void ThreatActor_ShouldHaveFileSystemValue()
    {
        // Arrange & Act
        var actor = ThreatActor.FileSystem;

        // Assert
        actor.Should().Be(ThreatActor.FileSystem);
    }

    [Fact]
    public void ThreatActor_ShouldHaveProcessValue()
    {
        // Arrange & Act
        var actor = ThreatActor.Process;

        // Assert
        actor.Should().Be(ThreatActor.Process);
    }

    [Fact]
    public void ThreatActor_ShouldHaveNetworkValue()
    {
        // Arrange & Act
        var actor = ThreatActor.Network;

        // Assert
        actor.Should().Be(ThreatActor.Network);
    }

    [Fact]
    public void ThreatActor_ShouldHaveMaliciousInputValue()
    {
        // Arrange & Act
        var actor = ThreatActor.MaliciousInput;

        // Assert
        actor.Should().Be(ThreatActor.MaliciousInput);
    }

    [Fact]
    public void ThreatActor_ShouldHaveCompromisedDependencyValue()
    {
        // Arrange & Act
        var actor = ThreatActor.CompromisedDependency;

        // Assert
        actor.Should().Be(ThreatActor.CompromisedDependency);
    }

    [Fact]
    public void ThreatActor_ShouldHaveInsiderValue()
    {
        // Arrange & Act
        var actor = ThreatActor.Insider;

        // Assert
        actor.Should().Be(ThreatActor.Insider);
    }

    [Fact]
    public void ThreatActor_ShouldHaveExactlyTenValues()
    {
        // Arrange & Act
        var values = Enum.GetValues<ThreatActor>();

        // Assert
        values.Should().HaveCount(10);
    }

    [Fact]
    public void ThreatActor_AllValuesShouldBeDistinct()
    {
        // Arrange & Act
        var values = Enum.GetValues<ThreatActor>();

        // Assert
        values.Should().OnlyHaveUniqueItems();
    }
}
