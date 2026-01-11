using Acode.Domain.Validation;
using FluentAssertions;

namespace Acode.Domain.Tests.Validation;

/// <summary>
/// Tests for DefaultAllowlist static class.
/// Verifies default allowlist contains localhost entries per Task 001.b.
/// </summary>
public class DefaultAllowlistTests
{
    [Fact]
    public void DefaultAllowlist_ShouldContainLocalhostEntry()
    {
        // Act
        var allowlist = DefaultAllowlist.GetDefaultEntries();

        // Assert
        allowlist.Should().Contain(entry => entry.Host == "localhost");
    }

    [Fact]
    public void DefaultAllowlist_ShouldContainLoopbackIpv4Entry()
    {
        // Act
        var allowlist = DefaultAllowlist.GetDefaultEntries();

        // Assert
        allowlist.Should().Contain(entry => entry.Host == "127.0.0.1");
    }

    [Fact]
    public void DefaultAllowlist_ShouldContainLoopbackIpv6Entry()
    {
        // Act
        var allowlist = DefaultAllowlist.GetDefaultEntries();

        // Assert
        allowlist.Should().Contain(entry => entry.Host == "::1");
    }

    [Fact]
    public void DefaultAllowlist_LocalhostEntry_ShouldIncludeOllamaPort()
    {
        // Act
        var allowlist = DefaultAllowlist.GetDefaultEntries();
        var localhostEntry = allowlist.First(e => e.Host == "localhost");

        // Assert
        localhostEntry.Ports.Should().Contain(11434, "Ollama default port should be allowed");
    }

    [Fact]
    public void DefaultAllowlist_ShouldAllowLocalhostPort11434()
    {
        // Arrange
        var uri = new Uri("http://localhost:11434/api/generate");

        // Act
        var isAllowed = DefaultAllowlist.IsAllowed(uri);

        // Assert
        isAllowed.Should().BeTrue("localhost:11434 should be allowed for Ollama");
    }

    [Fact]
    public void DefaultAllowlist_ShouldAllow127001Port11434()
    {
        // Arrange
        var uri = new Uri("http://127.0.0.1:11434/api/generate");

        // Act
        var isAllowed = DefaultAllowlist.IsAllowed(uri);

        // Assert
        isAllowed.Should().BeTrue("127.0.0.1:11434 should be allowed for Ollama");
    }

    [Fact]
    public void DefaultAllowlist_ShouldAllowIpv6LoopbackPort11434()
    {
        // Arrange
        var uri = new Uri("http://[::1]:11434/api/generate");

        // Act
        var isAllowed = DefaultAllowlist.IsAllowed(uri);

        // Assert
        isAllowed.Should().BeTrue("::1:11434 should be allowed for Ollama");
    }

    [Fact]
    public void DefaultAllowlist_ShouldNotAllowExternalHost()
    {
        // Arrange
        var uri = new Uri("https://api.openai.com/v1/models");

        // Act
        var isAllowed = DefaultAllowlist.IsAllowed(uri);

        // Assert
        isAllowed.Should().BeFalse("External hosts should NOT be allowed");
    }

    [Fact]
    public void DefaultAllowlist_ShouldNotAllowLocalhostWrongPort()
    {
        // Arrange
        var uri = new Uri("http://localhost:8080/");

        // Act
        var isAllowed = DefaultAllowlist.IsAllowed(uri);

        // Assert
        isAllowed.Should().BeFalse("localhost with non-Ollama port should NOT be allowed by default");
    }

    [Fact]
    public void DefaultAllowlist_ShouldBeImmutable()
    {
        // Act
        var allowlist = DefaultAllowlist.GetDefaultEntries();

        // Assert
        allowlist.Should().BeAssignableTo<IReadOnlyList<AllowlistEntry>>();
    }
}
