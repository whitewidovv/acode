// <copyright file="EmbeddedPackProviderTests.cs" company="Acode">
// Copyright (c) Acode. All rights reserved.
// </copyright>

using Acode.Domain.PromptPacks;
using Acode.Domain.PromptPacks.Exceptions;
using Acode.Infrastructure.PromptPacks;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Acode.Infrastructure.Tests.PromptPacks;

/// <summary>
/// Unit tests for the <see cref="EmbeddedPackProvider"/> class.
/// </summary>
public sealed class EmbeddedPackProviderTests
{
    private readonly EmbeddedPackProvider _provider;

    /// <summary>
    /// Initializes a new instance of the <see cref="EmbeddedPackProviderTests"/> class.
    /// </summary>
    public EmbeddedPackProviderTests()
    {
        var manifestParser = new ManifestParser();
        var logger = NullLogger<EmbeddedPackProvider>.Instance;
        _provider = new EmbeddedPackProvider(manifestParser, logger);
    }

    /// <summary>
    /// Verifies that loading a valid pack returns the pack.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Fact]
    public async Task LoadPackAsync_Should_Return_Pack_For_Valid_PackId()
    {
        // Act
        var pack = await _provider.LoadPackAsync("acode-standard");

        // Assert
        pack.Should().NotBeNull();
        pack.Id.Should().Be("acode-standard");
        pack.Source.Should().Be(PackSource.BuiltIn);
        pack.Components.Should().NotBeEmpty();
    }

    /// <summary>
    /// Verifies that loading an invalid pack throws.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Fact]
    public async Task LoadPackAsync_Should_Throw_For_Invalid_PackId()
    {
        // Act
        var act = () => _provider.LoadPackAsync("non-existent-pack");

        // Assert
        await act.Should().ThrowAsync<PackNotFoundException>();
    }

    /// <summary>
    /// Verifies that LoadManifest throws for null pack ID.
    /// </summary>
    [Fact]
    public void LoadManifest_Should_Throw_For_Null_PackId()
    {
        // Act
        var act = () => _provider.LoadManifest(null!);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    /// <summary>
    /// Verifies that LoadManifest throws for empty pack ID.
    /// </summary>
    [Fact]
    public void LoadManifest_Should_Throw_For_Empty_PackId()
    {
        // Act
        var act = () => _provider.LoadManifest(string.Empty);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    /// <summary>
    /// Verifies that pack extraction creates directory.
    /// </summary>
    [Fact]
    public void ExtractPack_Should_Create_Directory()
    {
        // Act
        var extractPath = _provider.ExtractPack("acode-standard");

        // Assert
        Directory.Exists(extractPath).Should().BeTrue();
        File.Exists(Path.Combine(extractPath, "manifest.yml")).Should().BeTrue();
        File.Exists(Path.Combine(extractPath, "system.md")).Should().BeTrue();
    }

    /// <summary>
    /// Verifies that subsequent extracts use the cache.
    /// </summary>
    [Fact]
    public void ExtractPack_Should_Use_Cache_On_Subsequent_Calls()
    {
        // Act
        var path1 = _provider.ExtractPack("acode-dotnet");
        var path2 = _provider.ExtractPack("acode-dotnet");

        // Assert
        path1.Should().Be(path2, "subsequent extracts should return cached path");
    }

    /// <summary>
    /// Verifies that loaded pack has correct component content.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Fact]
    public async Task LoadPackAsync_Should_Include_Component_Content()
    {
        // Act
        var pack = await _provider.LoadPackAsync("acode-standard");
        var systemComponent = pack.GetComponent("system.md");

        // Assert
        systemComponent.Should().NotBeNull();
        systemComponent!.Content.Should().NotBeNullOrEmpty();
        systemComponent.Content.Should().Contain("Acode");
        systemComponent.Type.Should().Be(ComponentType.System);
    }

    /// <summary>
    /// Verifies that loaded pack has correct number of components.
    /// </summary>
    /// <param name="packId">The pack ID to test.</param>
    /// <param name="expectedCount">The expected component count.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Theory]
    [InlineData("acode-standard", 4)]
    [InlineData("acode-dotnet", 6)]
    [InlineData("acode-react", 6)]
    public async Task LoadPackAsync_Should_Have_Expected_Component_Count(string packId, int expectedCount)
    {
        // Act
        var pack = await _provider.LoadPackAsync(packId);

        // Assert
        pack.Components.Should().HaveCount(expectedCount);
    }

    /// <summary>
    /// Verifies that loaded pack directory exists and contains files.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Fact]
    public async Task LoadPackAsync_Should_Extract_Files_To_Temp_Directory()
    {
        // Act
        var pack = await _provider.LoadPackAsync("acode-react");

        // Assert
        Directory.Exists(pack.PackPath).Should().BeTrue("pack directory should be extracted");
        File.Exists(Path.Combine(pack.PackPath, "system.md")).Should().BeTrue();
        File.Exists(Path.Combine(pack.PackPath, "roles", "coder.md")).Should().BeTrue();
        File.Exists(Path.Combine(pack.PackPath, "languages", "typescript.md")).Should().BeTrue();
        File.Exists(Path.Combine(pack.PackPath, "frameworks", "react.md")).Should().BeTrue();
    }

    /// <summary>
    /// Verifies that GetComponentsByType returns correct components.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Fact]
    public async Task GetComponentsByType_Should_Return_Correct_Components()
    {
        // Act
        var pack = await _provider.LoadPackAsync("acode-dotnet");
        var roleComponents = pack.GetComponentsByType(ComponentType.Role).ToList();

        // Assert
        roleComponents.Should().HaveCount(3);
        roleComponents.Should().Contain(c => c.Path == "roles/planner.md");
        roleComponents.Should().Contain(c => c.Path == "roles/coder.md");
        roleComponents.Should().Contain(c => c.Path == "roles/reviewer.md");
    }

    /// <summary>
    /// Verifies that GetSystemPrompt returns system content.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Fact]
    public async Task GetSystemPrompt_Should_Return_System_Content()
    {
        // Act
        var pack = await _provider.LoadPackAsync("acode-standard");
        var systemPrompt = pack.GetSystemPrompt();

        // Assert
        systemPrompt.Should().NotBeNullOrEmpty();
        systemPrompt.Should().ContainEquivalentOf("strict minimal diff");
    }
}
