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

    /// <summary>
    /// AC-022: Pack name must be 3-100 characters.
    /// </summary>
    /// <param name="invalidName">The invalid name to test.</param>
    [Theory]
    [InlineData("ab")] // 2 chars - too short
    [InlineData("x")] // 1 char - too short
    public void Should_Reject_Name_Too_Short(string invalidName)
    {
        // Arrange - create manifest YAML with name too short
        var yaml = $"""
            format_version: "1.0"
            id: test-pack
            version: "1.0.0"
            name: {invalidName}
            description: A test prompt pack for validation testing
            content_hash: a1b2c3d4e5f6789012345678901234567890abcdef1234567890abcdef123456
            created_at: 2024-01-15T10:00:00Z
            components: []
            """;
        var parser = new ManifestParser();

        // Act - attempt to parse
        var act = () => parser.Parse(yaml);

        // Assert - should throw ManifestParseException with appropriate error code
        act.Should().Throw<ManifestParseException>()
            .Where(e => e.ErrorCode == "ACODE-PKL-008")
            .WithMessage("*3*100*");
    }

    /// <summary>
    /// AC-022: Pack name must be 3-100 characters.
    /// </summary>
    [Fact]
    public void Should_Reject_Name_Too_Long()
    {
        // Arrange - create manifest YAML with name too long (101 chars)
        var longName = new string('x', 101);
        var yaml = $"""
            format_version: "1.0"
            id: test-pack
            version: "1.0.0"
            name: {longName}
            description: A test prompt pack for validation testing
            content_hash: a1b2c3d4e5f6789012345678901234567890abcdef1234567890abcdef123456
            created_at: 2024-01-15T10:00:00Z
            components: []
            """;
        var parser = new ManifestParser();

        // Act - attempt to parse
        var act = () => parser.Parse(yaml);

        // Assert - should throw ManifestParseException with appropriate error code
        act.Should().Throw<ManifestParseException>()
            .Where(e => e.ErrorCode == "ACODE-PKL-008")
            .WithMessage("*3*100*");
    }

    /// <summary>
    /// AC-022: Pack name must be 3-100 characters - valid lengths.
    /// </summary>
    /// <param name="validName">The valid name to test.</param>
    [Theory]
    [InlineData("Abc")] // 3 chars - minimum valid
    [InlineData("My Test Pack")] // normal length
    public void Should_Accept_Valid_Name_Length(string validName)
    {
        // Arrange - create valid manifest with acceptable name
        var yaml = $"""
            format_version: "1.0"
            id: test-pack
            version: "1.0.0"
            name: {validName}
            description: A test prompt pack for validation testing
            content_hash: a1b2c3d4e5f6789012345678901234567890abcdef1234567890abcdef123456
            created_at: 2024-01-15T10:00:00Z
            components: []
            """;
        var parser = new ManifestParser();

        // Act - parse valid manifest
        var manifest = parser.Parse(yaml);

        // Assert - should parse successfully with correct name
        manifest.Name.Should().Be(validName);
    }

    /// <summary>
    /// AC-022: Pack name must be 3-100 characters - maximum valid.
    /// </summary>
    [Fact]
    public void Should_Accept_Name_At_Max_Length()
    {
        // Arrange - create manifest with 100-char name (max valid)
        var maxName = new string('x', 100);
        var yaml = $"""
            format_version: "1.0"
            id: test-pack
            version: "1.0.0"
            name: {maxName}
            description: A test prompt pack for validation testing
            content_hash: a1b2c3d4e5f6789012345678901234567890abcdef1234567890abcdef123456
            created_at: 2024-01-15T10:00:00Z
            components: []
            """;
        var parser = new ManifestParser();

        // Act - parse valid manifest
        var manifest = parser.Parse(yaml);

        // Assert - should parse successfully with correct name
        manifest.Name.Should().Be(maxName);
    }

    /// <summary>
    /// AC-024: Pack description must be 10-500 characters.
    /// </summary>
    /// <param name="invalidDesc">The invalid description to test.</param>
    [Theory]
    [InlineData("short")] // 5 chars - too short
    [InlineData("tiny")] // 4 chars - too short
    public void Should_Reject_Description_Too_Short(string invalidDesc)
    {
        // Arrange - create manifest YAML with description too short
        var yaml = $"""
            format_version: "1.0"
            id: test-pack
            version: "1.0.0"
            name: Test Pack
            description: {invalidDesc}
            content_hash: a1b2c3d4e5f6789012345678901234567890abcdef1234567890abcdef123456
            created_at: 2024-01-15T10:00:00Z
            components: []
            """;
        var parser = new ManifestParser();

        // Act - attempt to parse
        var act = () => parser.Parse(yaml);

        // Assert - should throw ManifestParseException with appropriate error code
        act.Should().Throw<ManifestParseException>()
            .Where(e => e.ErrorCode == "ACODE-PKL-009")
            .WithMessage("*10*500*");
    }

    /// <summary>
    /// AC-024: Pack description must be 10-500 characters.
    /// </summary>
    [Fact]
    public void Should_Reject_Description_Too_Long()
    {
        // Arrange - create manifest YAML with description too long (501 chars)
        var longDesc = new string('x', 501);
        var yaml = $"""
            format_version: "1.0"
            id: test-pack
            version: "1.0.0"
            name: Test Pack
            description: {longDesc}
            content_hash: a1b2c3d4e5f6789012345678901234567890abcdef1234567890abcdef123456
            created_at: 2024-01-15T10:00:00Z
            components: []
            """;
        var parser = new ManifestParser();

        // Act - attempt to parse
        var act = () => parser.Parse(yaml);

        // Assert - should throw ManifestParseException with appropriate error code
        act.Should().Throw<ManifestParseException>()
            .Where(e => e.ErrorCode == "ACODE-PKL-009")
            .WithMessage("*10*500*");
    }

    /// <summary>
    /// AC-024: Pack description must be 10-500 characters - valid lengths.
    /// </summary>
    /// <param name="validDesc">The valid description to test.</param>
    [Theory]
    [InlineData("Ten chars!")] // 10 chars - minimum valid
    [InlineData("This is a test prompt pack for coding assistant tasks")] // normal length
    public void Should_Accept_Valid_Description_Length(string validDesc)
    {
        // Arrange - create valid manifest with acceptable description
        var yaml = $"""
            format_version: "1.0"
            id: test-pack
            version: "1.0.0"
            name: Test Pack
            description: {validDesc}
            content_hash: a1b2c3d4e5f6789012345678901234567890abcdef1234567890abcdef123456
            created_at: 2024-01-15T10:00:00Z
            components: []
            """;
        var parser = new ManifestParser();

        // Act - parse valid manifest
        var manifest = parser.Parse(yaml);

        // Assert - should parse successfully with correct description
        manifest.Description.Should().Be(validDesc);
    }

    /// <summary>
    /// AC-024: Pack description must be 10-500 characters - maximum valid.
    /// </summary>
    [Fact]
    public void Should_Accept_Description_At_Max_Length()
    {
        // Arrange - create manifest with 500-char description (max valid)
        var maxDesc = new string('x', 500);
        var yaml = $"""
            format_version: "1.0"
            id: test-pack
            version: "1.0.0"
            name: Test Pack
            description: {maxDesc}
            content_hash: a1b2c3d4e5f6789012345678901234567890abcdef1234567890abcdef123456
            created_at: 2024-01-15T10:00:00Z
            components: []
            """;
        var parser = new ManifestParser();

        // Act - parse valid manifest
        var manifest = parser.Parse(yaml);

        // Assert - should parse successfully with correct description
        manifest.Description.Should().Be(maxDesc);
    }

    /// <summary>
    /// AC-024: Pack description is required.
    /// </summary>
    [Fact]
    public void Should_Require_Description_Field()
    {
        // Arrange - missing description
        var yaml = """
            format_version: "1.0"
            id: test-pack
            version: "1.0.0"
            name: Test Pack
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
            .WithMessage("*description*");
    }
}
