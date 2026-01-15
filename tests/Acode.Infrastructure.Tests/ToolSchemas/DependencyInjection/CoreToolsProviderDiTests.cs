namespace Acode.Infrastructure.Tests.ToolSchemas.DependencyInjection;

using Acode.Application.Tools;
using Acode.Infrastructure.DependencyInjection;
using Acode.Infrastructure.ToolSchemas.Providers;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Tests for CoreToolsProvider DI registration.
/// </summary>
public sealed class CoreToolsProviderDiTests
{
    [Fact]
    public void AddCoreToolsProvider_Should_Register_IToolSchemaProvider()
    {
        var services = new ServiceCollection();

        services.AddCoreToolsProvider();

        var provider = services.BuildServiceProvider();
        var toolSchemaProvider = provider.GetService<IToolSchemaProvider>();

        toolSchemaProvider.Should().NotBeNull();
        toolSchemaProvider.Should().BeOfType<CoreToolsProvider>();
    }

    [Fact]
    public void AddCoreToolsProvider_Should_Return_ServiceCollection()
    {
        var services = new ServiceCollection();

        var result = services.AddCoreToolsProvider();

        result.Should().BeSameAs(services);
    }

    [Fact]
    public void AddCoreToolsProvider_Should_Register_As_Singleton()
    {
        var services = new ServiceCollection();
        services.AddCoreToolsProvider();
        var provider = services.BuildServiceProvider();

        var instance1 = provider.GetService<IToolSchemaProvider>();
        var instance2 = provider.GetService<IToolSchemaProvider>();

        instance1.Should().BeSameAs(instance2);
    }

    [Fact]
    public void Registered_CoreToolsProvider_Should_Return_17_Tools()
    {
        var services = new ServiceCollection();
        services.AddCoreToolsProvider();
        var provider = services.BuildServiceProvider();

        var toolSchemaProvider = provider.GetRequiredService<IToolSchemaProvider>();
        var tools = toolSchemaProvider.GetToolDefinitions().ToList();

        tools.Should().HaveCount(17);
    }

    [Fact]
    public void Registered_CoreToolsProvider_Should_Have_Order_Zero()
    {
        var services = new ServiceCollection();
        services.AddCoreToolsProvider();
        var provider = services.BuildServiceProvider();

        var toolSchemaProvider = provider.GetRequiredService<IToolSchemaProvider>();

        toolSchemaProvider.Order.Should().Be(0, "Core tools should load first");
    }
}
