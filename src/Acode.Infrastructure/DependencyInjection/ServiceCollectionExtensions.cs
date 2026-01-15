using Acode.Application.Configuration;
using Acode.Application.Inference;
using Acode.Application.Tools;
using Acode.Application.Tools.Retry;
using Acode.Infrastructure.Configuration;
using Acode.Infrastructure.Ollama;
using Acode.Infrastructure.PromptPacks;
using Acode.Infrastructure.Tools;
using Acode.Infrastructure.Vllm;
using Acode.Infrastructure.Vllm.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

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

        // Register logging if not already configured (required by PromptPacks services).
        // Note: AddLogging() is idempotent and won't override existing configuration,
        // but we call it to ensure logging is available for PromptPacks services.
        services.AddLogging();

        // Register prompt pack services
        services.AddPromptPacks();

        return services;
    }

    /// <summary>
    /// Registers Ollama provider with the DI container.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">Optional Ollama configuration. Uses defaults if null.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddOllamaProvider(
        this IServiceCollection services,
        OllamaConfiguration? configuration = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        var config = configuration ?? new OllamaConfiguration();

        // Register configuration as singleton
        services.AddSingleton(config);

        // Register HttpClient for Ollama (named client with pooling)
        services.AddHttpClient("Ollama", client =>
        {
            client.BaseAddress = new Uri(config.BaseUrl);
            client.Timeout = config.RequestTimeout;
        });

        // Register OllamaProvider as IModelProvider
        services.AddSingleton<IModelProvider>(sp =>
        {
            var httpClientFactory = sp.GetRequiredService<System.Net.Http.IHttpClientFactory>();
            var httpClient = httpClientFactory.CreateClient("Ollama");
            return new OllamaProvider(httpClient, config);
        });

        return services;
    }

    /// <summary>
    /// Registers vLLM provider with the DI container.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">Optional vLLM client configuration. Uses defaults if null.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddVllmProvider(
        this IServiceCollection services,
        VllmClientConfiguration? configuration = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        var config = configuration ?? new VllmClientConfiguration();
        config.Validate();

        // Ensure logging is registered (required by VllmProvider)
        services.AddLogging();

        // Register configuration as singleton
        services.AddSingleton(config);

        // Register VllmProvider as IModelProvider
        services.AddSingleton<IModelProvider>(sp =>
        {
            var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
            return new VllmProvider(config, loggerFactory);
        });

        return services;
    }

    /// <summary>
    /// Registers the Tool Schema Registry with the DI container.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// FR-007: Tool Schema Registry requirements.
    /// Registers ToolSchemaRegistry as singleton implementing IToolSchemaRegistry.
    /// </remarks>
    public static IServiceCollection AddToolSchemaRegistry(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<IToolSchemaRegistry, ToolSchemaRegistry>();

        return services;
    }

    /// <summary>
    /// Registers tool validation retry components with the DI container.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">Optional retry configuration. Uses defaults if null.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// FR-007b: Validation error retry contract.
    /// Registers ValidationErrorFormatter and RetryTracker as singletons.
    /// </remarks>
    public static IServiceCollection AddToolValidationRetry(
        this IServiceCollection services,
        RetryConfiguration? configuration = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        var config = configuration ?? RetryConfiguration.Default;

        // Register configuration
        services.AddSingleton(config);

        // Register formatter and tracker
        services.AddSingleton<IValidationErrorFormatter, ValidationErrorFormatter>();
        services.AddSingleton<IRetryTracker, RetryTracker>();

        return services;
    }
}
