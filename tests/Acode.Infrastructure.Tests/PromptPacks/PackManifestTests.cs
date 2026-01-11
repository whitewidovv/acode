using Acode.Domain.PromptPacks;
using Acode.Domain.PromptPacks.Exceptions;
using Acode.Infrastructure.PromptPacks;
using FluentAssertions;

namespace Acode.Infrastructure.Tests.PromptPacks;

/// <summary>
/// Unit tests for pack manifest parsing and validation.
/// </summary>
public class PackManifestTests
{
    [Fact]
    public void Should_Parse_Valid_Manifest()
    {
        // Arrange
        var yaml = """
            format_version: "1.0"
            id: test-pack
            version: "1.2.3"
            name: Test Pack
            description: A test prompt pack
            content_hash: a1b2c3d4e5f6789012345678901234567890abcdef1234567890abcdef123456
            created_at: 2024-01-15T10:00:00Z
            components:
              - path: system.md
                type: system
            """;
        var parser = new ManifestParser();

        // Act
        var manifest = parser.Parse(yaml);

        // Assert
        manifest.FormatVersion.Should().Be("1.0");
        manifest.Id.Should().Be("test-pack");
        manifest.Version.ToString().Should().Be("1.2.3");
        manifest.Name.Should().Be("Test Pack");
        manifest.Description.Should().Be("A test prompt pack");
        manifest.ContentHash!.ToString().Should().StartWith("a1b2c3d4");
        manifest.Components.Should().HaveCount(1);
    }

    [Fact]
    public void Should_Reject_Invalid_Format_Version()
    {
        // Arrange
        var yaml = """
            format_version: "2.0"
            id: test-pack
            version: "1.0.0"
            name: Test Pack
            description: A test prompt pack
            content_hash: a1b2c3d4e5f6789012345678901234567890abcdef1234567890abcdef123456
            created_at: 2024-01-15T10:00:00Z
            components: []
            """;
        var parser = new ManifestParser();

        // Act
        var act = () => parser.Parse(yaml);

        // Assert
        act.Should().Throw<ManifestParseException>()
            .WithMessage("*format_version*")
            .Where(e => e.ErrorCode == "ACODE-PKL-003");
    }

    [Theory]
    [InlineData("My Pack")]
    [InlineData("MyPack")]
    [InlineData("my_pack")]
    [InlineData("ab")]
    [InlineData("a")]
    public void Should_Validate_Pack_Id_Format(string invalidId)
    {
        // Arrange
        var yaml = $"""
            format_version: "1.0"
            id: {invalidId}
            version: "1.0.0"
            name: Test Pack
            description: A test prompt pack
            content_hash: a1b2c3d4e5f6789012345678901234567890abcdef1234567890abcdef123456
            created_at: 2024-01-15T10:00:00Z
            components: []
            """;
        var parser = new ManifestParser();

        // Act
        var act = () => parser.Parse(yaml);

        // Assert
        act.Should().Throw<ManifestParseException>()
            .Where(e => e.ErrorCode == "ACODE-PKL-004");
    }

    [Theory]
    [InlineData("my-pack")]
    [InlineData("my-custom-pack")]
    [InlineData("team-dotnet-v2")]
    [InlineData("abc")]
    public void Should_Accept_Valid_Pack_Id_Format(string validId)
    {
        // Arrange
        var yaml = $"""
            format_version: "1.0"
            id: {validId}
            version: "1.0.0"
            name: Test Pack
            description: A test prompt pack
            content_hash: a1b2c3d4e5f6789012345678901234567890abcdef1234567890abcdef123456
            created_at: 2024-01-15T10:00:00Z
            components: []
            """;
        var parser = new ManifestParser();

        // Act
        var manifest = parser.Parse(yaml);

        // Assert
        manifest.Id.Should().Be(validId);
    }

    [Fact]
    public void Should_Parse_SemVer_Version()
    {
        // Arrange
        var yaml = """
            format_version: "1.0"
            id: test-pack
            version: "2.3.4-beta.1+build.456"
            name: Test Pack
            description: A test prompt pack
            content_hash: a1b2c3d4e5f6789012345678901234567890abcdef1234567890abcdef123456
            created_at: 2024-01-15T10:00:00Z
            components: []
            """;
        var parser = new ManifestParser();

        // Act
        var manifest = parser.Parse(yaml);

        // Assert
        manifest.Version.Major.Should().Be(2);
        manifest.Version.Minor.Should().Be(3);
        manifest.Version.Patch.Should().Be(4);
        manifest.Version.PreRelease.Should().Be("beta.1");
        manifest.Version.BuildMetadata.Should().Be("build.456");
    }

    [Fact]
    public void Should_Parse_Components_With_Metadata()
    {
        // Arrange
        var yaml = """
            format_version: "1.0"
            id: test-pack
            version: "1.0.0"
            name: Test Pack
            description: A test prompt pack
            content_hash: a1b2c3d4e5f6789012345678901234567890abcdef1234567890abcdef123456
            created_at: 2024-01-15T10:00:00Z
            components:
              - path: roles/coder.md
                type: role
                metadata:
                  role: coder
              - path: languages/csharp.md
                type: language
                metadata:
                  language: csharp
                  version: "12"
            """;
        var parser = new ManifestParser();

        // Act
        var manifest = parser.Parse(yaml);

        // Assert
        manifest.Components.Should().HaveCount(2);
        manifest.Components[0].Type.Should().Be(ComponentType.Role);
        manifest.Components[0].Metadata!["role"].Should().Be("coder");
        manifest.Components[1].Type.Should().Be(ComponentType.Language);
        manifest.Components[1].Metadata!["language"].Should().Be("csharp");
    }

    [Fact]
    public void Should_Require_Created_At_Field()
    {
        // Arrange - missing created_at
        var yaml = """
            format_version: "1.0"
            id: test-pack
            version: "1.0.0"
            name: Test Pack
            description: A test prompt pack
            content_hash: a1b2c3d4e5f6789012345678901234567890abcdef1234567890abcdef123456
            components: []
            """;
        var parser = new ManifestParser();

        // Act
        var act = () => parser.Parse(yaml);

        // Assert
        act.Should().Throw<ManifestParseException>()
            .Where(e => e.ErrorCode == "ACODE-PKL-002")
            .WithMessage("*created_at*");
    }

    [Fact]
    public void Should_Require_Id_Field()
    {
        // Arrange - missing id
        var yaml = """
            format_version: "1.0"
            version: "1.0.0"
            name: Test Pack
            description: A test prompt pack
            content_hash: a1b2c3d4e5f6789012345678901234567890abcdef1234567890abcdef123456
            created_at: 2024-01-15T10:00:00Z
            components: []
            """;
        var parser = new ManifestParser();

        // Act
        var act = () => parser.Parse(yaml);

        // Assert
        act.Should().Throw<ManifestParseException>()
            .Where(e => e.ErrorCode == "ACODE-PKL-002")
            .WithMessage("*id*");
    }

    [Fact]
    public void Should_Require_Version_Field()
    {
        // Arrange - missing version
        var yaml = """
            format_version: "1.0"
            id: test-pack
            name: Test Pack
            description: A test prompt pack
            content_hash: a1b2c3d4e5f6789012345678901234567890abcdef1234567890abcdef123456
            created_at: 2024-01-15T10:00:00Z
            components: []
            """;
        var parser = new ManifestParser();

        // Act
        var act = () => parser.Parse(yaml);

        // Assert
        act.Should().Throw<ManifestParseException>()
            .Where(e => e.ErrorCode == "ACODE-PKL-002")
            .WithMessage("*version*");
    }
}
