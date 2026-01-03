using Acode.Application.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Acode.Application.DependencyInjection;

/// <summary>
/// Extension methods for configuring Acode Application services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers all Acode Application layer services with the DI container.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddAcodeApplication(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        // Register configuration services
        services.AddSingleton<IConfigLoader, ConfigLoader>();
        services.AddSingleton<IConfigValidator, ConfigValidator>();
        services.AddSingleton<IConfigCache, ConfigCache>();

        return services;
    }
}
