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
    /// <param name="schemaPath">Path to the JSON schema file. If not specified, uses embedded resource.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddAcodeInfrastructure(
        this IServiceCollection services,
        string? schemaPath = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        // Register configuration readers/validators
        services.AddSingleton<IConfigReader, YamlConfigReader>();

        // Register JsonSchemaValidator as a factory to handle async initialization
        services.AddSingleton<JsonSchemaValidator>(sp =>
        {
            var path = schemaPath ?? GetDefaultSchemaPath();
            return JsonSchemaValidator.CreateAsync(path).GetAwaiter().GetResult();
        });

        return services;
    }

    private static string GetDefaultSchemaPath()
    {
        // For now, use file path. Will switch to embedded resource in final packaging.
        var baseDir = AppDomain.CurrentDomain.BaseDirectory;
        return Path.Combine(baseDir, "..", "..", "..", "..", "..", "data", "config-schema.json");
    }
}
