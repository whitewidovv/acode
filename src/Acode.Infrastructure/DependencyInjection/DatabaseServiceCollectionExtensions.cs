// src/Acode.Infrastructure/DependencyInjection/DatabaseServiceCollectionExtensions.cs
namespace Acode.Infrastructure.DependencyInjection;

using Acode.Application.Interfaces.Persistence;
using Acode.Infrastructure.Configuration;
using Acode.Infrastructure.Persistence.Connections;
using Acode.Infrastructure.Persistence.Retry;
using Acode.Infrastructure.Persistence.Transactions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods for registering database services in dependency injection container.
/// </summary>
public static class DatabaseServiceCollectionExtensions
{
    /// <summary>
    /// Registers database services including connection factories, unit of work, and retry policy.
    /// </summary>
    /// <param name="services">Service collection.</param>
    /// <param name="configuration">Application configuration.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddDatabaseServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        // Bind database configuration
        services.Configure<DatabaseOptions>(
            configuration.GetSection(DatabaseOptions.SectionName));

        // Register both connection factories as singletons
        services.AddSingleton<SqliteConnectionFactory>();
        services.AddSingleton<PostgresConnectionFactory>();

        // Register connection factory selector (chooses based on provider config)
        services.AddSingleton<IConnectionFactory, ConnectionFactorySelector>();

        // Register transactional components
        services.AddScoped<IUnitOfWorkFactory, UnitOfWorkFactory>();

        // Register retry policy
        services.AddSingleton<IDatabaseRetryPolicy, DatabaseRetryPolicy>();

        return services;
    }
}
