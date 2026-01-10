using Acode.Domain.PromptPacks;
using FluentAssertions;
using Xunit;

namespace Acode.Domain.Tests.PromptPacks;

/// <summary>
/// Unit tests for semantic versioning parsing and comparison.
/// </summary>
public class SemVerTests
{
    [Theory]
    [InlineData("1.0.0", 1, 0, 0, null, null)]
    [InlineData("2.3.4", 2, 3, 4, null, null)]
    [InlineData("1.0.0-alpha", 1, 0, 0, "alpha", null)]
    [InlineData("1.0.0-alpha.1", 1, 0, 0, "alpha.1", null)]
    [InlineData("1.0.0+build", 1, 0, 0, null, "build")]
    [InlineData("1.0.0-beta+build.123", 1, 0, 0, "beta", "build.123")]
    public void Should_Parse_Major_Minor_Patch(
        string version,
        int major,
        int minor,
        int patch,
        string? preRelease,
        string? buildMetadata)
    {
        // Act
        var v = PackVersion.Parse(version);

        // Assert
        v.Major.Should().Be(major);
        v.Minor.Should().Be(minor);
        v.Patch.Should().Be(patch);
        v.Prerelease.Should().Be(preRelease);
        v.BuildMetadata.Should().Be(buildMetadata);
    }

    [Theory]
    [InlineData("1.0")]
    [InlineData("1")]
    [InlineData("a.b.c")]
    [InlineData("1.0.0.0")]
    [InlineData("")]
    public void Should_Reject_Invalid_Versions(string version)
    {
        // Act
        var act = () => PackVersion.Parse(version);

        // Assert
        act.Should().Throw<Exception>();
    }

    [Theory]
    [InlineData("1.0.0", "1.0.1", -1)]
    [InlineData("1.0.0", "1.1.0", -1)]
    [InlineData("1.0.0", "2.0.0", -1)]
    [InlineData("2.0.0", "1.0.0", 1)]
    [InlineData("1.0.0", "1.0.0", 0)]
    [InlineData("1.0.0-alpha", "1.0.0", -1)]
    [InlineData("1.0.0-alpha", "1.0.0-beta", -1)]
    public void Should_Compare_Versions(string v1, string v2, int expected)
    {
        // Arrange
        var version1 = PackVersion.Parse(v1);
        var version2 = PackVersion.Parse(v2);

        // Act
        var result = version1.CompareTo(version2);

        // Assert
        Math.Sign(result).Should().Be(expected);
    }

    [Fact]
    public void Should_Sort_Versions()
    {
        // Arrange
        var versions = new[]
        {
            "2.0.0",
            "1.0.0-alpha",
            "1.0.0",
            "1.0.0-beta",
            "1.0.1",
        }.Select(PackVersion.Parse).ToList();

        // Act
        var sorted = versions.Order().ToList();

        // Assert
        sorted.Select(v => v.ToString()).Should().Equal(
            "1.0.0-alpha",
            "1.0.0-beta",
            "1.0.0",
            "1.0.1",
            "2.0.0");
    }

    [Fact]
    public void Should_Return_True_For_TryParse_With_Valid_Version()
    {
        // Arrange
        var versionString = "1.2.3-alpha+build";

        // Act
        var result = PackVersion.TryParse(versionString, out var version);

        // Assert
        result.Should().BeTrue();
        version.Should().NotBeNull();
        version!.Major.Should().Be(1);
        version.Minor.Should().Be(2);
        version.Patch.Should().Be(3);
    }

    [Fact]
    public void Should_Return_False_For_TryParse_With_Invalid_Version()
    {
        // Arrange
        var versionString = "invalid";

        // Act
        var result = PackVersion.TryParse(versionString, out var version);

        // Assert
        result.Should().BeFalse();
        version.Should().BeNull();
    }
}
