// <copyright file="PromptContentTests.cs" company="Acode">
// Copyright (c) Acode. All rights reserved.
// </copyright>

using System.Text.RegularExpressions;
using Acode.Infrastructure.PromptPacks;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Acode.Infrastructure.Tests.PromptPacks;

/// <summary>
/// Unit tests for prompt pack content.
/// </summary>
public sealed class PromptContentTests
{
    private readonly EmbeddedPackProvider _provider;

    /// <summary>
    /// Initializes a new instance of the <see cref="PromptContentTests"/> class.
    /// </summary>
    public PromptContentTests()
    {
        var manifestParser = new ManifestParser();
        var logger = NullLogger<EmbeddedPackProvider>.Instance;
        _provider = new EmbeddedPackProvider(manifestParser, logger);
    }

    /// <summary>
    /// Verifies that all packs include strict minimal diff instructions.
    /// </summary>
    /// <param name="packId">The pack ID to test.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Theory]
    [InlineData("acode-standard")]
    [InlineData("acode-dotnet")]
    [InlineData("acode-react")]
    public async Task Should_Include_Minimal_Diff_Instructions(string packId)
    {
        // Arrange
        ArgumentNullException.ThrowIfNull(packId);

        var pack = await _provider.LoadPackAsync(packId);
        var systemPromptPath = Path.Combine(pack.PackPath, "system.md");
        var coderPromptPath = Path.Combine(pack.PackPath, "roles", "coder.md");

        // Act
        var systemContent = await File.ReadAllTextAsync(systemPromptPath);
        var coderContent = await File.ReadAllTextAsync(coderPromptPath);

        // Assert - use case-insensitive matching
        systemContent.Should().ContainEquivalentOf(
            "strict minimal diff",
            "system prompt must define strict minimal diff principle");
        systemContent.Should().MatchRegex(
            "(?i)(smallest possible changes|smallest possible change|only modify)",
            "system prompt must emphasize minimal changes");

        coderContent.Should().ContainEquivalentOf(
            "minimal",
            "coder prompt must reinforce minimal changes");
        coderContent.Should().MatchRegex(
            "(?i)(only modify|preserve existing|do not fix)",
            "coder prompt must have explicit minimal diff constraints");
    }

    /// <summary>
    /// Verifies that prompts use valid template variables.
    /// </summary>
    /// <param name="packId">The pack ID to test.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Theory]
    [InlineData("acode-standard")]
    [InlineData("acode-dotnet")]
    [InlineData("acode-react")]
    public async Task Should_Have_Valid_Template_Variables(string packId)
    {
        // Arrange
        ArgumentNullException.ThrowIfNull(packId);

        var pack = await _provider.LoadPackAsync(packId);
        var systemPromptPath = Path.Combine(pack.PackPath, "system.md");
        var systemContent = await File.ReadAllTextAsync(systemPromptPath);

        // Act
        var templateVarPattern = new Regex(@"\{\{([a-z_]+)\}\}");
        var matches = templateVarPattern.Matches(systemContent);
        var variables = matches.Select(m => m.Groups[1].Value).Distinct().ToList();

        // Assert
        variables.Should().NotBeEmpty("system prompt should use template variables");

        var validVariables = new[] { "workspace_name", "date", "language", "framework" };
        foreach (var variable in variables)
        {
            validVariables.Should().Contain(
                variable,
                $"template variable '{variable}' must be in allowed list");
        }
    }

    /// <summary>
    /// Verifies that prompts are under token limits.
    /// </summary>
    /// <param name="packId">The pack ID to test.</param>
    /// <param name="componentPath">The component path to test.</param>
    /// <param name="maxTokens">The maximum token count.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Theory]
    [InlineData("acode-standard", "system.md", 4000)]
    [InlineData("acode-dotnet", "system.md", 4000)]
    [InlineData("acode-react", "system.md", 4000)]
    [InlineData("acode-dotnet", "roles/coder.md", 2000)]
    [InlineData("acode-react", "roles/coder.md", 2000)]
    [InlineData("acode-dotnet", "languages/csharp.md", 2000)]
    [InlineData("acode-react", "languages/typescript.md", 2000)]
    [InlineData("acode-dotnet", "frameworks/aspnetcore.md", 2000)]
    [InlineData("acode-react", "frameworks/react.md", 2000)]
    public async Task Should_Be_Under_Token_Limits(string packId, string componentPath, int maxTokens)
    {
        // Arrange
        ArgumentNullException.ThrowIfNull(packId);
        ArgumentNullException.ThrowIfNull(componentPath);

        var pack = await _provider.LoadPackAsync(packId);
        var fullPath = Path.Combine(pack.PackPath, componentPath.Replace('/', Path.DirectorySeparatorChar));
        var content = await File.ReadAllTextAsync(fullPath);

        // Act - Rough token estimation: ~4 characters per token
        var estimatedTokens = content.Length / 4;

        // Assert
        estimatedTokens.Should().BeLessThan(
            maxTokens,
            $"{packId}/{componentPath} should be under {maxTokens} tokens (estimated {estimatedTokens})");
    }

    /// <summary>
    /// Verifies that language prompts include conventions.
    /// </summary>
    /// <param name="packId">The pack ID to test.</param>
    /// <param name="componentPath">The component path to test.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Theory]
    [InlineData("acode-dotnet", "languages/csharp.md")]
    [InlineData("acode-react", "languages/typescript.md")]
    public async Task Should_Include_Language_Conventions(string packId, string componentPath)
    {
        // Arrange
        ArgumentNullException.ThrowIfNull(packId);
        ArgumentNullException.ThrowIfNull(componentPath);

        var pack = await _provider.LoadPackAsync(packId);
        var fullPath = Path.Combine(pack.PackPath, componentPath.Replace('/', Path.DirectorySeparatorChar));
        var content = await File.ReadAllTextAsync(fullPath);

        // Assert - use case-insensitive matching
        content.Should().ContainEquivalentOf("naming", "language prompts must cover naming conventions");
        content.Should().MatchRegex(
            "(?i)(pattern|idiom|convention)",
            "language prompts must reference common patterns");
        content.Length.Should().BeGreaterThan(
            500,
            "language prompts should have substantive content");
    }

    /// <summary>
    /// Verifies that framework prompts include patterns.
    /// </summary>
    /// <param name="packId">The pack ID to test.</param>
    /// <param name="componentPath">The component path to test.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Theory]
    [InlineData("acode-dotnet", "frameworks/aspnetcore.md")]
    [InlineData("acode-react", "frameworks/react.md")]
    public async Task Should_Include_Framework_Patterns(string packId, string componentPath)
    {
        // Arrange
        ArgumentNullException.ThrowIfNull(packId);
        ArgumentNullException.ThrowIfNull(componentPath);

        var pack = await _provider.LoadPackAsync(packId);
        var fullPath = Path.Combine(pack.PackPath, componentPath.Replace('/', Path.DirectorySeparatorChar));
        var content = await File.ReadAllTextAsync(fullPath);

        // Assert
        content.Length.Should().BeGreaterThan(
            500,
            "framework prompts should have substantive content");
        content.Should().MatchRegex(
            "(?i)(pattern|architecture|best practice)",
            "framework prompts must include patterns");
    }

    /// <summary>
    /// Verifies that dotnet pack covers async patterns.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Fact]
    public async Task DotNet_Pack_Should_Cover_Async_Patterns()
    {
        // Arrange
        var pack = await _provider.LoadPackAsync("acode-dotnet");
        var csharpPath = Path.Combine(pack.PackPath, "languages", "csharp.md");
        var content = await File.ReadAllTextAsync(csharpPath);

        // Assert
        content.Should().Contain("async", "C# prompt must cover async/await");
        content.Should().Contain("await", "C# prompt must cover async/await");
        content.Should().MatchRegex(
            "(?i)cancellationtoken",
            "C# prompt should mention cancellation tokens");
    }

    /// <summary>
    /// Verifies that react pack covers hooks.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Fact]
    public async Task React_Pack_Should_Cover_Hooks()
    {
        // Arrange
        var pack = await _provider.LoadPackAsync("acode-react");
        var reactPath = Path.Combine(pack.PackPath, "frameworks", "react.md");
        var content = await File.ReadAllTextAsync(reactPath);

        // Assert
        content.Should().Contain("hook", "React prompt must cover hooks");
        content.Should().MatchRegex(
            "(?i)usestate|useeffect",
            "React prompt should mention specific hooks");
        content.Should().MatchRegex(
            "(?i)functional component",
            "React prompt should emphasize functional components");
    }
}
