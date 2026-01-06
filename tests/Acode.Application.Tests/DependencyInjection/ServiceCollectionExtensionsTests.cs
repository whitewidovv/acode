using Acode.Application.Configuration;
using Acode.Application.DependencyInjection;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace Acode.Application.Tests.DependencyInjection;

/// <summary>
/// Tests for Application layer DI registration.
/// </summary>
public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddAcodeApplication_ShouldRegisterConfigLoader()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton(Substitute.For<IConfigReader>()); // Mock dependency

        // Act
        services.AddAcodeApplication();
        var provider = services.BuildServiceProvider();

        // Assert
        var configLoader = provider.GetService<IConfigLoader>();
        configLoader.Should().NotBeNull();
        configLoader.Should().BeOfType<ConfigLoader>();
    }

    [Fact]
    public void AddAcodeApplication_ShouldRegisterConfigValidator()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddAcodeApplication();
        var provider = services.BuildServiceProvider();

        // Assert
        var configValidator = provider.GetService<IConfigValidator>();
        configValidator.Should().NotBeNull();
        configValidator.Should().BeOfType<ConfigValidator>();
    }

    [Fact]
    public void AddAcodeApplication_ShouldRegisterConfigCache()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddAcodeApplication();
        var provider = services.BuildServiceProvider();

        // Assert
        var configCache = provider.GetService<IConfigCache>();
        configCache.Should().NotBeNull();
        configCache.Should().BeOfType<ConfigCache>();
    }

    [Fact]
    public void AddAcodeApplication_ShouldRegisterAsSingleton()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddAcodeApplication();
        var provider = services.BuildServiceProvider();

        // Assert - same instance returned
        var cache1 = provider.GetService<IConfigCache>();
        var cache2 = provider.GetService<IConfigCache>();
        cache1.Should().BeSameAs(cache2);
    }

    [Fact]
    public void AddAcodeApplication_WithNullServices_ShouldThrowArgumentNullException()
    {
        // Arrange
        ServiceCollection? services = null;

        // Act & Assert
        var act = () => services!.AddAcodeApplication();
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("services");
    }
}
