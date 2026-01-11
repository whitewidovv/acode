using System.Diagnostics;
using Acode.Domain.PromptPacks;
using Acode.Infrastructure.PromptPacks;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;

namespace Acode.Integration.Tests.PromptPacks;

/// <summary>
/// Performance tests for prompt pack operations.
/// Tests 31-34 from Task 008 parent spec (lines 2079-2175).
/// </summary>
public class PromptPackPerformanceTests
{
    private readonly ManifestParser _manifestParser;
    private readonly ContentHasher _contentHasher;
    private readonly EmbeddedPackProvider _embeddedPackProvider;
    private readonly PromptPackLoader _loader;
    private readonly TemplateEngine _templateEngine;

    public PromptPackPerformanceTests()
    {
        _manifestParser = new ManifestParser();
        _contentHasher = new ContentHasher();
        _embeddedPackProvider = new EmbeddedPackProvider(_manifestParser, NullLogger<EmbeddedPackProvider>.Instance);
        _loader = new PromptPackLoader(
            _manifestParser,
            _contentHasher,
            _embeddedPackProvider,
            NullLogger<PromptPackLoader>.Instance);
        _templateEngine = new TemplateEngine();
    }

    /// <summary>
    /// Test 31: Composition Should Complete Under 10ms For Typical Pack.
    /// </summary>
    [Fact]
    public async Task Composition_Should_Complete_Under_10ms_For_Typical_Pack()
    {
        // Arrange
        var components = new List<LoadedComponent>
        {
            new LoadedComponent("system.md", ComponentType.System, new string('x', 5000), null),
            new LoadedComponent(
                "roles/coder.md",
                ComponentType.Role,
                new string('y', 3000),
                new Dictionary<string, string> { ["role"] = "coder" }),
            new LoadedComponent(
                "languages/csharp.md",
                ComponentType.Language,
                new string('z', 2000),
                new Dictionary<string, string> { ["language"] = "csharp" }),
        };

        var pack = new PromptPack(
            "benchmark-pack",
            new PackVersion(1, 0, 0),
            "Benchmark Pack",
            "Performance test pack",
            PackSource.BuiltIn,
            "/builtin/benchmark-pack",
            null,
            components);

        var composer = new PromptComposer(_templateEngine, logger: NullLogger<PromptComposer>.Instance);
        var context = new CompositionContext { Role = "coder", Language = "csharp" };

        // Warmup
        await composer.ComposeAsync(pack, context);

        // Act
        var stopwatch = Stopwatch.StartNew();
        var result = await composer.ComposeAsync(pack, context);
        stopwatch.Stop();

        // Assert
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(10, "composition should be fast");
        result.Should().NotBeEmpty();
    }

    /// <summary>
    /// Test 32: Pack Loading Should Complete Under 100ms.
    /// </summary>
    [Fact]
    public async Task Pack_Loading_Should_Complete_Under_100ms()
    {
        // Arrange & Warmup
        await _loader.LoadBuiltInPackAsync("acode-standard");

        // Act
        var stopwatch = Stopwatch.StartNew();
        var pack = await _loader.LoadBuiltInPackAsync("acode-standard");
        stopwatch.Stop();

        // Assert
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(100, "pack loading should be fast");
        pack.Should().NotBeNull();
    }

    /// <summary>
    /// Test 33: Registry Indexing Should Complete Under 200ms.
    /// Note: This tests the embedded pack discovery which is analogous to registry indexing.
    /// </summary>
    [Fact]
    public async Task Registry_Indexing_Should_Complete_Under_200ms()
    {
        // Arrange
        var packIds = new[] { "acode-standard", "acode-dotnet", "acode-react" };

        // Warmup
        foreach (var packId in packIds)
        {
            await _loader.LoadBuiltInPackAsync(packId);
        }

        // Act - simulate registry indexing by loading all packs
        var stopwatch = Stopwatch.StartNew();
        foreach (var packId in packIds)
        {
            await _loader.LoadBuiltInPackAsync(packId);
        }

        stopwatch.Stop();

        // Assert
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(200, "registry indexing should be fast");
    }

    /// <summary>
    /// Test 34: Template Variable Substitution Should Complete Under 1ms.
    /// </summary>
    [Fact]
    public void Template_Variable_Substitution_Should_Complete_Under_1ms()
    {
        // Arrange
        var template = "Project: {{workspace_name}}, Lang: {{language}}, Framework: {{framework}}";
        var context = new CompositionContext
        {
            Variables = new Dictionary<string, string>
            {
                ["workspace_name"] = "TestProject",
                ["language"] = "csharp",
                ["framework"] = "aspnetcore"
            }
        };

        // Warmup
        _templateEngine.Substitute(template, context);

        // Act
        var stopwatch = Stopwatch.StartNew();
        var result = _templateEngine.Substitute(template, context);
        stopwatch.Stop();

        // Assert
        stopwatch.Elapsed.TotalMilliseconds.Should().BeLessThan(1, "template substitution should be very fast");
        result.Should().NotBeEmpty();
        result.Should().Contain("TestProject");
    }
}
