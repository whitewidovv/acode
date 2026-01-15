using Acode.Application.PromptPacks;
using Acode.Domain.PromptPacks;
using Acode.Infrastructure.PromptPacks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
#pragma warning disable IDE0005 // Using directive is unnecessary - Required for [Fact] and IAsyncLifetime
using Xunit;
#pragma warning restore IDE0005

namespace Acode.Integration.Tests.PromptPacks;

public class StarterPackLoadingTests : IAsyncLifetime
{
    private ServiceProvider _serviceProvider = null!;
    private IPromptPackRegistry _registry = null!;
    private IPromptPackLoader _loader = null!;

    public async Task InitializeAsync()
    {
        // Setup dependency injection with all PromptPacks services
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddPromptPacks();

        _serviceProvider = services.BuildServiceProvider();
        _registry = _serviceProvider.GetRequiredService<IPromptPackRegistry>();
        _loader = _serviceProvider.GetRequiredService<IPromptPackLoader>();

        await Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        _serviceProvider?.Dispose();
        await Task.CompletedTask;
    }

    [Fact]
    public async Task Should_Load_Standard_Pack()
    {
        // Act
        var pack = await _loader.LoadBuiltInPackAsync("acode-standard");

        // Assert
        pack.Should().NotBeNull();
        pack!.Id.Should().Be("acode-standard");
        pack.Source.Should().Be(PackSource.BuiltIn);
        pack.Name.Should().NotBeNullOrEmpty();
        pack.Components.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Should_Load_DotNet_Pack()
    {
        // Act
        var pack = await _loader.LoadBuiltInPackAsync("acode-dotnet");

        // Assert
        pack.Should().NotBeNull();
        pack!.Id.Should().Be("acode-dotnet");
        pack.Source.Should().Be(PackSource.BuiltIn);

        // DotNet pack should include language and framework prompts
        var componentPaths = pack.Components.Select(c => c.Path).ToList();
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
        pack!.Id.Should().Be("acode-react");
        pack.Source.Should().Be(PackSource.BuiltIn);

        // React pack should include language and framework prompts
        var componentPaths = pack.Components.Select(c => c.Path).ToList();
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
        secondLoad!.Id.Should().Be(firstLoad!.Id);
        secondLoad.Version.Should().Be(firstLoad.Version);
    }

    [Fact]
    public void Should_List_All_Starter_Packs()
    {
        // Act
        var packs = _registry.ListPacks();

        // Assert - Should find built-in starter packs
        packs.Should().NotBeNull();
        packs.Should().NotBeEmpty();

        var packIds = packs.Select(p => p.Id).ToList();
        packIds.Should().Contain("acode-standard");
        packIds.Should().Contain("acode-dotnet");
        packIds.Should().Contain("acode-react");
    }
}
