// <copyright file="StarterPackTests.cs" company="Acode">
// Copyright (c) Acode. All rights reserved.
// </copyright>

using Acode.Domain.PromptPacks;
using Acode.Infrastructure.PromptPacks;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Acode.Infrastructure.Tests.PromptPacks;

/// <summary>
/// Unit tests for built-in starter packs.
/// </summary>
public sealed class StarterPackTests
{
    private readonly EmbeddedPackProvider _provider;

    /// <summary>
    /// Initializes a new instance of the <see cref="StarterPackTests"/> class.
    /// </summary>
    public StarterPackTests()
    {
        var manifestParser = new ManifestParser();
        var logger = NullLogger<EmbeddedPackProvider>.Instance;
        _provider = new EmbeddedPackProvider(manifestParser, logger);
    }

    /// <summary>
    /// Verifies that the acode-standard pack is embedded.
    /// </summary>
    [Fact]
    public void Should_Have_Standard_Pack()
    {
        // Arrange
        var assembly = typeof(EmbeddedPackProvider).Assembly;
        var expectedResourcePrefix = "Acode.Infrastructure.Resources.PromptPacks.acode_standard";

        // Act
        var resources = assembly.GetManifestResourceNames()
            .Where(r => r.StartsWith(expectedResourcePrefix, StringComparison.Ordinal))
            .ToList();

        // Assert
        resources.Should().NotBeEmpty("acode-standard pack must be embedded");
        resources.Should().Contain(
            r => r.EndsWith("manifest.yml", StringComparison.Ordinal),
            "pack must have manifest");
        resources.Should().Contain(
            r => r.EndsWith("system.md", StringComparison.Ordinal),
            "pack must have system prompt");
        resources.Should().Contain(
            r => r.Contains("roles", StringComparison.Ordinal) && r.EndsWith("planner.md", StringComparison.Ordinal),
            "pack must have planner role");
        resources.Should().Contain(
            r => r.Contains("roles", StringComparison.Ordinal) && r.EndsWith("coder.md", StringComparison.Ordinal),
            "pack must have coder role");
        resources.Should().Contain(
            r => r.Contains("roles", StringComparison.Ordinal) && r.EndsWith("reviewer.md", StringComparison.Ordinal),
            "pack must have reviewer role");
    }

    /// <summary>
    /// Verifies that the acode-dotnet pack is embedded.
    /// </summary>
    [Fact]
    public void Should_Have_DotNet_Pack()
    {
        // Arrange
        var assembly = typeof(EmbeddedPackProvider).Assembly;
        var expectedResourcePrefix = "Acode.Infrastructure.Resources.PromptPacks.acode_dotnet";

        // Act
        var resources = assembly.GetManifestResourceNames()
            .Where(r => r.StartsWith(expectedResourcePrefix, StringComparison.Ordinal))
            .ToList();

        // Assert
        resources.Should().NotBeEmpty("acode-dotnet pack must be embedded");
        resources.Should().Contain(r => r.EndsWith("manifest.yml", StringComparison.Ordinal));
        resources.Should().Contain(r => r.EndsWith("system.md", StringComparison.Ordinal));

        // Standard roles
        resources.Should().Contain(
            r => r.Contains("roles", StringComparison.Ordinal) && r.EndsWith("planner.md", StringComparison.Ordinal));
        resources.Should().Contain(
            r => r.Contains("roles", StringComparison.Ordinal) && r.EndsWith("coder.md", StringComparison.Ordinal));
        resources.Should().Contain(
            r => r.Contains("roles", StringComparison.Ordinal) && r.EndsWith("reviewer.md", StringComparison.Ordinal));

        // Language-specific
        resources.Should().Contain(
            r => r.Contains("languages", StringComparison.Ordinal) && r.EndsWith("csharp.md", StringComparison.Ordinal),
            "dotnet pack must include C# language guidance");

        // Framework-specific
        resources.Should().Contain(
            r => r.Contains("frameworks", StringComparison.Ordinal) && r.EndsWith("aspnetcore.md", StringComparison.Ordinal),
            "dotnet pack must include ASP.NET Core framework guidance");
    }

    /// <summary>
    /// Verifies that the acode-react pack is embedded.
    /// </summary>
    [Fact]
    public void Should_Have_React_Pack()
    {
        // Arrange
        var assembly = typeof(EmbeddedPackProvider).Assembly;
        var expectedResourcePrefix = "Acode.Infrastructure.Resources.PromptPacks.acode_react";

        // Act
        var resources = assembly.GetManifestResourceNames()
            .Where(r => r.StartsWith(expectedResourcePrefix, StringComparison.Ordinal))
            .ToList();

        // Assert
        resources.Should().NotBeEmpty("acode-react pack must be embedded");
        resources.Should().Contain(r => r.EndsWith("manifest.yml", StringComparison.Ordinal));
        resources.Should().Contain(r => r.EndsWith("system.md", StringComparison.Ordinal));

        // Standard roles
        resources.Should().Contain(
            r => r.Contains("roles", StringComparison.Ordinal) && r.EndsWith("planner.md", StringComparison.Ordinal));
        resources.Should().Contain(
            r => r.Contains("roles", StringComparison.Ordinal) && r.EndsWith("coder.md", StringComparison.Ordinal));
        resources.Should().Contain(
            r => r.Contains("roles", StringComparison.Ordinal) && r.EndsWith("reviewer.md", StringComparison.Ordinal));

        // Language-specific
        resources.Should().Contain(
            r => r.Contains("languages", StringComparison.Ordinal) && r.EndsWith("typescript.md", StringComparison.Ordinal),
            "react pack must include TypeScript language guidance");

        // Framework-specific
        resources.Should().Contain(
            r => r.Contains("frameworks", StringComparison.Ordinal) && r.EndsWith("react.md", StringComparison.Ordinal),
            "react pack must include React framework guidance");
    }

    /// <summary>
    /// Verifies that all packs have valid manifests.
    /// </summary>
    [Fact]
    public void Should_Have_Valid_Manifests()
    {
        // Arrange
        var packIds = new[] { "acode-standard", "acode-dotnet", "acode-react" };

        foreach (var packId in packIds)
        {
            // Act
            var manifest = _provider.LoadManifest(packId);

            // Assert
            manifest.Should().NotBeNull($"{packId} manifest should load");
            manifest.Id.Should().Be(packId, $"{packId} manifest id should match");
            manifest.Version.Should().NotBeNull($"{packId} must have version");
            manifest.FormatVersion.Should().Be("1.0", $"{packId} must use format version 1.0");
            manifest.Name.Should().NotBeNullOrEmpty($"{packId} must have display name");
            manifest.Description.Should().NotBeNullOrEmpty($"{packId} must have description");
            manifest.Components.Should().NotBeEmpty($"{packId} must have components");
        }
    }

    /// <summary>
    /// Verifies that all packs have their required components.
    /// </summary>
    [Fact]
    public void Should_Have_All_Required_Components()
    {
        // Arrange
        var testCases = new (string PackId, string[] RequiredComponents)[]
        {
            (
                "acode-standard",
                new[] { "system.md", "roles/planner.md", "roles/coder.md", "roles/reviewer.md" }
            ),
            (
                "acode-dotnet",
                new[]
                {
                    "system.md", "roles/planner.md", "roles/coder.md", "roles/reviewer.md",
                    "languages/csharp.md", "frameworks/aspnetcore.md",
                }
            ),
            (
                "acode-react",
                new[]
                {
                    "system.md", "roles/planner.md", "roles/coder.md", "roles/reviewer.md",
                    "languages/typescript.md", "frameworks/react.md",
                }
            ),
        };

        foreach (var (packId, requiredComponents) in testCases)
        {
            // Act
            var manifest = _provider.LoadManifest(packId);

            // Assert
            foreach (var requiredComponent in requiredComponents)
            {
                manifest.Components.Should().Contain(
                    c => c.Path == requiredComponent,
                    $"{packId} must include {requiredComponent}");
            }
        }
    }

    /// <summary>
    /// Verifies that components have correct types.
    /// </summary>
    [Fact]
    public void Should_Have_Correct_Component_Types()
    {
        // Arrange & Act
        var manifest = _provider.LoadManifest("acode-dotnet");

        // Assert
        var systemComponent = manifest.Components.Single(c => c.Path == "system.md");
        systemComponent.Type.Should().Be(ComponentType.System);

        var coderComponent = manifest.Components.Single(c => c.Path == "roles/coder.md");
        coderComponent.Type.Should().Be(ComponentType.Role);

        var csharpComponent = manifest.Components.Single(c => c.Path == "languages/csharp.md");
        csharpComponent.Type.Should().Be(ComponentType.Language);

        var aspnetComponent = manifest.Components.Single(c => c.Path == "frameworks/aspnetcore.md");
        aspnetComponent.Type.Should().Be(ComponentType.Framework);
    }

    /// <summary>
    /// Verifies that available pack IDs include all starter packs.
    /// </summary>
    [Fact]
    public void Should_List_All_Available_Packs()
    {
        // Act
        var availablePackIds = _provider.GetAvailablePackIds();

        // Assert
        availablePackIds.Should().HaveCount(3, "should have 3 built-in starter packs");
        availablePackIds.Should().Contain("acode-standard");
        availablePackIds.Should().Contain("acode-dotnet");
        availablePackIds.Should().Contain("acode-react");
    }

    /// <summary>
    /// Verifies that HasPack returns true for existing packs.
    /// </summary>
    /// <param name="packId">The pack ID to test.</param>
    [Theory]
    [InlineData("acode-standard")]
    [InlineData("acode-dotnet")]
    [InlineData("acode-react")]
    public void HasPack_Should_Return_True_For_Existing_Packs(string packId)
    {
        // Act
        var hasPack = _provider.HasPack(packId);

        // Assert
        hasPack.Should().BeTrue($"{packId} should exist as built-in pack");
    }

    /// <summary>
    /// Verifies that HasPack returns false for non-existing packs.
    /// </summary>
    [Fact]
    public void HasPack_Should_Return_False_For_NonExisting_Packs()
    {
        // Act
        var hasPack = _provider.HasPack("non-existent-pack");

        // Assert
        hasPack.Should().BeFalse();
    }
}
