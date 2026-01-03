using Acode.Application.Configuration;
using Acode.Infrastructure.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Acode.Infrastructure.DependencyInjection;

/// <summary>
/// Extension methods for configuring Acode Infrastructure services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers all Acode Infrastructure layer services with the DI container.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="schemaPath">Optional path to the JSON schema file. If not specified, uses embedded resource.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddAcodeInfrastructure(
        this IServiceCollection services,
        string? schemaPath = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        // Register configuration readers/validators
        services.AddSingleton<IConfigReader, YamlConfigReader>();

        // Register JsonSchemaValidator as a factory to handle async initialization
        services.AddSingleton<ISchemaValidator>(sp =>
        {
            return schemaPath is null
                ? JsonSchemaValidator.CreateFromEmbeddedResourceAsync().GetAwaiter().GetResult()
                : JsonSchemaValidator.CreateAsync(schemaPath).GetAwaiter().GetResult();
        });

        return services;
    }
}
