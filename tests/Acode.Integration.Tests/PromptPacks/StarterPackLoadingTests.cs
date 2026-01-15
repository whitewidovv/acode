using Acode.Application.PromptPacks;
using Acode.Domain.PromptPacks;
using Acode.Infrastructure.PromptPacks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Acode.Integration.Tests.PromptPacks;

public class StarterPackLoadingTests : IAsyncLifetime
{
    private ServiceProvider _serviceProvider = null!;
    private IPromptPackRegistry _registry = null!;
    private IPromptPackLoader _loader = null!;
    private string _tempDirectory = null!;

    public async Task InitializeAsync()
    {
        // Setup temp directory for pack extraction
        _tempDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDirectory);

        // Setup dependency injection
        var services = new ServiceCollection();
        services.AddSingleton<IPromptPackLoader, PromptPackLoader>();
        services.AddSingleton<IPackValidator, PackValidator>();
        services.AddSingleton<IContentHasher, ContentHasher>();
        services.AddSingleton<IPromptPackRegistry>(sp =>
            new PromptPackRegistry(
                packDirectory: _tempDirectory,
                loader: sp.GetRequiredService<IPromptPackLoader>()
            )
        );

        _serviceProvider = services.BuildServiceProvider();
        _registry = _serviceProvider.GetRequiredService<IPromptPackRegistry>();
        _loader = _serviceProvider.GetRequiredService<IPromptPackLoader>();

        await Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        _serviceProvider?.Dispose();
        if (Directory.Exists(_tempDirectory))
        {
            try
            {
                Directory.Delete(_tempDirectory, recursive: true);
            }
            catch
            {
                // Ignore cleanup errors
            }
        }

        await Task.CompletedTask;
    }

    [Fact]
    public async Task Should_Load_Standard_Pack()
    {
        // Act
        var pack = await _loader.LoadBuiltInPackAsync("acode-standard");

        // Assert
        pack.Should().NotBeNull();
        pack!.Manifest.Id.Should().Be("acode-standard");
        pack.Manifest.Source.Should().Be(PackSource.BuiltIn);
        pack.Manifest.Name.Should().NotBeNullOrEmpty();
        pack.Components.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Should_Load_DotNet_Pack()
    {
        // Act
        var pack = await _loader.LoadBuiltInPackAsync("acode-dotnet");

        // Assert
        pack.Should().NotBeNull();
        pack!.Manifest.Id.Should().Be("acode-dotnet");
        pack.Manifest.Source.Should().Be(PackSource.BuiltIn);

        // DotNet pack should include language and framework prompts
        var componentPaths = pack.Components.Keys.ToList();
        componentPaths.Should().Contain(c => c.Contains("csharp"));
        componentPaths.Should().Contain(c => c.Contains("aspnetcore"));
    }

    [Fact]
    public async Task Should_Load_React_Pack()
    {
        // Act
        var pack = await _loader.LoadBuiltInPackAsync("acode-react");

        // Assert
        pack.Should().NotBeNull();
        pack!.Manifest.Id.Should().Be("acode-react");
        pack.Manifest.Source.Should().Be(PackSource.BuiltIn);

        // React pack should include language and framework prompts
        var componentPaths = pack.Components.Keys.ToList();
        componentPaths.Should().Contain(c => c.Contains("typescript"));
        componentPaths.Should().Contain(c => c.Contains("react"));
    }

    [Fact]
    public async Task Should_Cache_Extracted_Packs()
    {
        // Arrange - Load a pack first time
        var firstLoad = await _loader.LoadBuiltInPackAsync("acode-standard");
        firstLoad.Should().NotBeNull();

        // Act - Load the same pack again
        var secondLoad = await _loader.LoadBuiltInPackAsync("acode-standard");

        // Assert - Both loads should succeed and have same data
        secondLoad.Should().NotBeNull();
        secondLoad!.Manifest.Id.Should().Be(firstLoad!.Manifest.Id);
        secondLoad.Manifest.Version.Should().Be(firstLoad.Manifest.Version);
    }

    [Fact]
    public async Task Should_List_All_Starter_Packs()
    {
        // Act
        var packs = await _registry.ListPacksAsync();

        // Assert - Should find built-in starter packs
        packs.Should().NotBeNull();
        packs.Should().NotBeEmpty();

        var packIds = packs.Select(p => p.Id).ToList();
        packIds.Should().Contain("acode-standard");
        packIds.Should().Contain("acode-dotnet");
        packIds.Should().Contain("acode-react");
    }
}
