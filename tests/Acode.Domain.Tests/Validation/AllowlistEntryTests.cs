using Acode.Domain.Validation;
using FluentAssertions;

namespace Acode.Domain.Tests.Validation;

/// <summary>
/// Tests for AllowlistEntry record.
/// Verifies allowlist matching logic per Task 001.b.
/// </summary>
public class AllowlistEntryTests
{
    [Fact]
    public void AllowlistEntry_ShouldMatchLocalhostWithPort()
    {
        // Arrange
        var entry = new AllowlistEntry
        {
            Host = "localhost",
            Ports = new[] { 11434 },
            Reason = "Ollama local server"
        };
        var uri = new Uri("http://localhost:11434/api/generate");

        // Act
        var matches = entry.Matches(uri);

        // Assert
        matches.Should().BeTrue();
    }

    [Fact]
    public void AllowlistEntry_ShouldMatchLoopbackIpv4()
    {
        // Arrange
        var entry = new AllowlistEntry
        {
            Host = "127.0.0.1",
            Ports = new[] { 11434 },
            Reason = "Ollama local server"
        };
        var uri = new Uri("http://127.0.0.1:11434/api/generate");

        // Act
        var matches = entry.Matches(uri);

        // Assert
        matches.Should().BeTrue();
    }

    [Fact]
    public void AllowlistEntry_ShouldMatchLoopbackIpv6()
    {
        // Arrange
        var entry = new AllowlistEntry
        {
            Host = "::1",
            Ports = new[] { 11434 },
            Reason = "Ollama local server"
        };
        var uri = new Uri("http://[::1]:11434/api/generate");

        // Act
        var matches = entry.Matches(uri);

        // Assert
        matches.Should().BeTrue();
    }

    [Fact]
    public void AllowlistEntry_WithPortRestriction_ShouldMatchSpecifiedPort()
    {
        // Arrange
        var entry = new AllowlistEntry
        {
            Host = "localhost",
            Ports = new[] { 11434 },
            Reason = "Ollama"
        };
        var uri = new Uri("http://localhost:11434/");

        // Act
        var matches = entry.Matches(uri);

        // Assert
        matches.Should().BeTrue();
    }

    [Fact]
    public void AllowlistEntry_WithPortRestriction_ShouldNotMatchDifferentPort()
    {
        // Arrange
        var entry = new AllowlistEntry
        {
            Host = "localhost",
            Ports = new[] { 11434 },
            Reason = "Ollama"
        };
        var uri = new Uri("http://localhost:8080/");

        // Act
        var matches = entry.Matches(uri);

        // Assert
        matches.Should().BeFalse();
    }

    [Fact]
    public void AllowlistEntry_WithoutPortRestriction_ShouldMatchAnyPort()
    {
        // Arrange
        var entry = new AllowlistEntry
        {
            Host = "localhost",
            Ports = null,
            Reason = "Local development"
        };

        // Act & Assert
        entry.Matches(new Uri("http://localhost:11434/")).Should().BeTrue();
        entry.Matches(new Uri("http://localhost:8080/")).Should().BeTrue();
        entry.Matches(new Uri("http://localhost:3000/")).Should().BeTrue();
    }

    [Fact]
    public void AllowlistEntry_ShouldBeCaseInsensitiveForHost()
    {
        // Arrange
        var entry = new AllowlistEntry
        {
            Host = "LOCALHOST",
            Ports = new[] { 11434 },
            Reason = "Test"
        };
        var uri = new Uri("http://localhost:11434/");

        // Act
        var matches = entry.Matches(uri);

        // Assert
        matches.Should().BeTrue();
    }

    [Fact]
    public void AllowlistEntry_ShouldTreatLocalhostAnd127001AsEquivalent()
    {
        // Arrange
        var entry = new AllowlistEntry
        {
            Host = "localhost",
            Ports = new[] { 11434 },
            Reason = "Ollama"
        };

        // Act & Assert - both should match
        entry.Matches(new Uri("http://localhost:11434/")).Should().BeTrue();
        entry.Matches(new Uri("http://127.0.0.1:11434/")).Should().BeTrue();
    }

    [Fact]
    public void AllowlistEntry_ShouldSupportRecordEquality()
    {
        // Arrange
        var entry1 = new AllowlistEntry
        {
            Host = "localhost",
            Ports = new[] { 11434 },
            Reason = "Test"
        };
        var entry2 = new AllowlistEntry
        {
            Host = "localhost",
            Ports = new[] { 11434 },
            Reason = "Test"
        };

        // Assert
        entry1.Should().Be(entry2);
    }

    [Fact]
    public void AllowlistEntry_WithMultiplePorts_ShouldMatchAnyListedPort()
    {
        // Arrange
        var entry = new AllowlistEntry
        {
            Host = "localhost",
            Ports = new[] { 11434, 8080, 3000 },
            Reason = "Multiple services"
        };

        // Act & Assert
        entry.Matches(new Uri("http://localhost:11434/")).Should().BeTrue();
        entry.Matches(new Uri("http://localhost:8080/")).Should().BeTrue();
        entry.Matches(new Uri("http://localhost:3000/")).Should().BeTrue();
        entry.Matches(new Uri("http://localhost:9999/")).Should().BeFalse();
    }
}
