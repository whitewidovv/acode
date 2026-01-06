using Acode.Domain.PromptPacks;
using FluentAssertions;
using Xunit;

namespace Acode.Domain.Tests.PromptPacks;

/// <summary>
/// Tests for <see cref="PackVersion"/> value object.
/// </summary>
public class PackVersionTests
{
    [Fact]
    public void Parse_ValidSemVer_ShouldSucceed()
    {
        // Arrange
        var versionString = "1.2.3";

        // Act
        var version = PackVersion.Parse(versionString);

        // Assert
        version.Major.Should().Be(1);
        version.Minor.Should().Be(2);
        version.Patch.Should().Be(3);
        version.PreRelease.Should().BeNull();
        version.BuildMetadata.Should().BeNull();
    }

    [Fact]
    public void Parse_WithPreRelease_ShouldSucceed()
    {
        // Arrange
        var versionString = "1.0.0-alpha.1";

        // Act
        var version = PackVersion.Parse(versionString);

        // Assert
        version.Major.Should().Be(1);
        version.Minor.Should().Be(0);
        version.Patch.Should().Be(0);
        version.PreRelease.Should().Be("alpha.1");
        version.BuildMetadata.Should().BeNull();
    }

    [Fact]
    public void Parse_WithBuildMetadata_ShouldSucceed()
    {
        // Arrange
        var versionString = "1.0.0+20240101.abc123";

        // Act
        var version = PackVersion.Parse(versionString);

        // Assert
        version.Major.Should().Be(1);
        version.Minor.Should().Be(0);
        version.Patch.Should().Be(0);
        version.PreRelease.Should().BeNull();
        version.BuildMetadata.Should().Be("20240101.abc123");
    }

    [Fact]
    public void Parse_WithPreReleaseAndBuildMetadata_ShouldSucceed()
    {
        // Arrange
        var versionString = "1.0.0-beta.2+exp.sha.5114f85";

        // Act
        var version = PackVersion.Parse(versionString);

        // Assert
        version.Major.Should().Be(1);
        version.Minor.Should().Be(0);
        version.Patch.Should().Be(0);
        version.PreRelease.Should().Be("beta.2");
        version.BuildMetadata.Should().Be("exp.sha.5114f85");
    }

    [Fact]
    public void Parse_InvalidFormat_ShouldThrowArgumentException()
    {
        // Arrange
        var invalidVersion = "1.2"; // Missing patch

        // Act
        var act = () => PackVersion.Parse(invalidVersion);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Semantic Versioning*");
    }

    [Fact]
    public void Parse_NegativeNumber_ShouldThrowArgumentException()
    {
        // Arrange
        var invalidVersion = "1.-2.3";

        // Act
        var act = () => PackVersion.Parse(invalidVersion);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void CompareTo_SameVersion_ShouldReturnZero()
    {
        // Arrange
        var version1 = PackVersion.Parse("1.2.3");
        var version2 = PackVersion.Parse("1.2.3");

        // Act
        var result = version1.CompareTo(version2);

        // Assert
        result.Should().Be(0);
    }

    [Fact]
    public void CompareTo_GreaterMajor_ShouldReturnPositive()
    {
        // Arrange
        var version1 = PackVersion.Parse("2.0.0");
        var version2 = PackVersion.Parse("1.9.9");

        // Act
        var result = version1.CompareTo(version2);

        // Assert
        result.Should().BeGreaterThan(0);
    }

    [Fact]
    public void CompareTo_GreaterMinor_ShouldReturnPositive()
    {
        // Arrange
        var version1 = PackVersion.Parse("1.5.0");
        var version2 = PackVersion.Parse("1.4.99");

        // Act
        var result = version1.CompareTo(version2);

        // Assert
        result.Should().BeGreaterThan(0);
    }

    [Fact]
    public void CompareTo_GreaterPatch_ShouldReturnPositive()
    {
        // Arrange
        var version1 = PackVersion.Parse("1.2.4");
        var version2 = PackVersion.Parse("1.2.3");

        // Act
        var result = version1.CompareTo(version2);

        // Assert
        result.Should().BeGreaterThan(0);
    }

    [Fact]
    public void CompareTo_PreReleaseVsRelease_PreReleaseShouldBeLower()
    {
        // Arrange
        var preRelease = PackVersion.Parse("1.0.0-alpha");
        var release = PackVersion.Parse("1.0.0");

        // Act
        var result = preRelease.CompareTo(release);

        // Assert
        result.Should().BeLessThan(0);
    }

    [Fact]
    public void CompareTo_PreReleaseVersions_ShouldCompareAlphabetically()
    {
        // Arrange
        var alpha = PackVersion.Parse("1.0.0-alpha");
        var beta = PackVersion.Parse("1.0.0-beta");

        // Act
        var result = alpha.CompareTo(beta);

        // Assert
        result.Should().BeLessThan(0);
    }

    [Fact]
    public void CompareTo_BuildMetadata_ShouldBeIgnored()
    {
        // Arrange
        var version1 = PackVersion.Parse("1.0.0+build1");
        var version2 = PackVersion.Parse("1.0.0+build2");

        // Act
        var result = version1.CompareTo(version2);

        // Assert
        result.Should().Be(0); // Build metadata should be ignored in comparison
    }

    [Fact]
    public void OperatorGreaterThan_ShouldWork()
    {
        // Arrange
        var version1 = PackVersion.Parse("2.0.0");
        var version2 = PackVersion.Parse("1.0.0");

        // Act & Assert
        (version1 > version2).Should().BeTrue();
        (version2 > version1).Should().BeFalse();
    }

    [Fact]
    public void OperatorLessThan_ShouldWork()
    {
        // Arrange
        var version1 = PackVersion.Parse("1.0.0");
        var version2 = PackVersion.Parse("2.0.0");

        // Act & Assert
        (version1 < version2).Should().BeTrue();
        (version2 < version1).Should().BeFalse();
    }

    [Fact]
    public void OperatorGreaterThanOrEqual_ShouldWork()
    {
        // Arrange
        var version1 = PackVersion.Parse("1.0.0");
        var version2 = PackVersion.Parse("1.0.0");
        var version3 = PackVersion.Parse("2.0.0");

        // Act & Assert
        (version1 >= version2).Should().BeTrue();
        (version3 >= version1).Should().BeTrue();
        (version1 >= version3).Should().BeFalse();
    }

    [Fact]
    public void OperatorLessThanOrEqual_ShouldWork()
    {
        // Arrange
        var version1 = PackVersion.Parse("1.0.0");
        var version2 = PackVersion.Parse("1.0.0");
        var version3 = PackVersion.Parse("0.9.0");

        // Act & Assert
        (version1 <= version2).Should().BeTrue();
        (version3 <= version1).Should().BeTrue();
        (version1 <= version3).Should().BeFalse();
    }

    [Fact]
    public void ToString_SimpleVersion_ShouldReturnCorrectFormat()
    {
        // Arrange
        var version = PackVersion.Parse("1.2.3");

        // Act
        var result = version.ToString();

        // Assert
        result.Should().Be("1.2.3");
    }

    [Fact]
    public void ToString_WithPreRelease_ShouldIncludePreRelease()
    {
        // Arrange
        var version = PackVersion.Parse("1.0.0-alpha.1");

        // Act
        var result = version.ToString();

        // Assert
        result.Should().Be("1.0.0-alpha.1");
    }

    [Fact]
    public void ToString_WithBuildMetadata_ShouldIncludeBuildMetadata()
    {
        // Arrange
        var version = PackVersion.Parse("1.0.0+build.123");

        // Act
        var result = version.ToString();

        // Assert
        result.Should().Be("1.0.0+build.123");
    }

    [Fact]
    public void ToString_WithBoth_ShouldIncludeBoth()
    {
        // Arrange
        var version = PackVersion.Parse("1.0.0-rc.1+build.456");

        // Act
        var result = version.ToString();

        // Assert
        result.Should().Be("1.0.0-rc.1+build.456");
    }
}
