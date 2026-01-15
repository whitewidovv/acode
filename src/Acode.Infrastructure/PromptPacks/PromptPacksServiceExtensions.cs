using Acode.Application.PromptPacks;
using Microsoft.Extensions.DependencyInjection;

namespace Acode.Infrastructure.PromptPacks;

/// <summary>
/// Extension methods for registering prompt pack services.
/// </summary>
public static class PromptPacksServiceExtensions
{
    /// <summary>
    /// Registers prompt pack infrastructure services with the DI container.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configureOptions">Optional action to configure options.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddPromptPacks(
        this IServiceCollection services,
        Action<PackDiscoveryOptions>? configureOptions = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        // Configure options
        if (configureOptions is not null)
        {
            services.Configure(configureOptions);
        }
        else
        {
            services.Configure<PackDiscoveryOptions>(_ => { });
        }

        // Register core services
        services.AddSingleton<ManifestParser>();
        services.AddSingleton<ContentHasher>();
        services.AddSingleton<HashVerifier>();
        services.AddSingleton<EmbeddedPackProvider>();
        services.AddSingleton<PackDiscovery>();

        // Register loader, validator, and registry
        services.AddSingleton<PromptPackLoader>();
        services.AddSingleton<IPromptPackLoader>(sp => sp.GetRequiredService<PromptPackLoader>());
        services.AddSingleton<PackValidator>();
        services.AddSingleton<IPackValidator>(sp => sp.GetRequiredService<PackValidator>());
        services.AddSingleton<PackCache>();
        services.AddSingleton<PackConfiguration>();
        services.AddSingleton<PromptPackRegistry>();
        services.AddSingleton<IPromptPackRegistry>(sp => sp.GetRequiredService<PromptPackRegistry>());

        return services;
    }
}
