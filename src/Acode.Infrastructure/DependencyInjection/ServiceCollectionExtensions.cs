using Acode.Application.Configuration;
using Acode.Application.Inference;
using Acode.Application.Providers.Vllm;
using Acode.Application.Tools;
using Acode.Application.Tools.Retry;
using Acode.Application.ToolSchemas.Retry;
using Acode.Infrastructure.Configuration;
using Acode.Infrastructure.Ollama;
using Acode.Infrastructure.PromptPacks;
using Acode.Infrastructure.Providers.Vllm.Lifecycle;
using Acode.Infrastructure.Tools;
using Acode.Infrastructure.ToolSchemas.Providers;
using Acode.Infrastructure.ToolSchemas.Retry;
using Acode.Infrastructure.Vllm;
using Acode.Infrastructure.Vllm.Client;
using Acode.Infrastructure.Vllm.Health;
using Acode.Infrastructure.Vllm.Health.Errors;
using Acode.Infrastructure.Vllm.Health.Metrics;
using Acode.Infrastructure.Vllm.StructuredOutput;
using Acode.Infrastructure.Vllm.StructuredOutput.Capability;
using Acode.Infrastructure.Vllm.StructuredOutput.Configuration;
using Acode.Infrastructure.Vllm.StructuredOutput.Fallback;
using Acode.Infrastructure.Vllm.StructuredOutput.ResponseFormat;
using Acode.Infrastructure.Vllm.StructuredOutput.Schema;
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

        // Register logging if not already configured
        // This ensures ILogger<T> is available for StructuredOutputHandler
        services.AddLogging();

        // Register configuration as singleton
        services.AddSingleton(config);

        // Register Structured Output components for vLLM
        services.AddStructuredOutputComponents();

        // Register VllmProvider as IModelProvider with optional StructuredOutputHandler
        services.AddSingleton<IModelProvider>(sp =>
        {
            var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
            var structuredOutputHandler = sp.GetService<StructuredOutputHandler>();
            return new VllmProvider(config, loggerFactory, structuredOutputHandler);
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
    /// Registers the Core Tools schema provider with the DI container.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// FR-007a: Core Tools Schema Provider registration.
    /// Registers CoreToolsProvider as singleton implementing IToolSchemaProvider.
    /// Core tools have Order=0 and are loaded first.
    /// </remarks>
    public static IServiceCollection AddCoreToolsProvider(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<IToolSchemaProvider, CoreToolsProvider>();

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
        Application.Tools.Retry.RetryConfiguration? configuration = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        var config = configuration ?? Application.Tools.Retry.RetryConfiguration.Default;

        // Register configuration
        services.AddSingleton(config);

        // Register formatter and tracker
        services.AddSingleton<IValidationErrorFormatter, ValidationErrorFormatter>();
        services.AddSingleton<Application.Tools.Retry.IRetryTracker, Infrastructure.Tools.RetryTracker>();

        return services;
    }

    /// <summary>
    /// Registers vLLM health checking components with the DI container.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">Optional health check configuration. Uses defaults if null.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// Task 006c: Load/Health-Check Endpoints + Error Handling
    /// Registers all health checking and error handling components.
    /// </remarks>
    public static IServiceCollection AddVllmHealthChecking(
        this IServiceCollection services,
        VllmHealthConfiguration? configuration = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        // Register health check configuration
        var healthConfig = configuration ?? new VllmHealthConfiguration();
        healthConfig.Validate();
        services.AddSingleton(healthConfig);

        // Register metrics subsystem
        services.AddSingleton<VllmMetricsParser>();
        services.AddSingleton<VllmMetricsClient>(sp =>
        {
            var cfg = sp.GetRequiredService<VllmHealthConfiguration>();
            return new VllmMetricsClient(cfg.BaseUrl, cfg.LoadMonitoring.MetricsEndpoint);
        });

        // Register error handling subsystem
        services.AddSingleton<VllmErrorParser>();
        services.AddSingleton<VllmErrorClassifier>();
        services.AddSingleton<VllmExceptionMapper>();

        // Register health checker
        services.AddSingleton<VllmHealthChecker>();

        return services;
    }

    /// <summary>
    /// Registers vLLM lifecycle management components with the DI container.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="options">Optional lifecycle options. Uses defaults if null.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// Task 006d: vLLM Lifecycle Management
    /// Registers all lifecycle orchestration components for managing vLLM service.
    /// </remarks>
    public static IServiceCollection AddVllmLifecycleManagement(
        this IServiceCollection services,
        VllmLifecycleOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        // Register lifecycle options
        var lifecycleOptions = options ?? new VllmLifecycleOptions();
        lifecycleOptions.Validate();
        services.AddSingleton(lifecycleOptions);

        // Register lifecycle helper components
        services.AddSingleton<VllmServiceStateTracker>();
        services.AddSingleton<VllmRestartPolicyEnforcer>();
        services.AddSingleton<VllmGpuMonitor>();
        services.AddSingleton<VllmModelLoader>();
        services.AddSingleton<VllmHealthCheckWorker>();

        // Register main orchestrator implementing IVllmServiceOrchestrator
        services.AddSingleton<IVllmServiceOrchestrator>(sp =>
        {
            var opts = sp.GetRequiredService<VllmLifecycleOptions>();
            var stateTracker = sp.GetRequiredService<VllmServiceStateTracker>();
            var restartPolicy = sp.GetRequiredService<VllmRestartPolicyEnforcer>();
            var gpuMonitor = sp.GetRequiredService<VllmGpuMonitor>();
            var modelLoader = sp.GetRequiredService<VllmModelLoader>();
            var healthCheckWorker = sp.GetRequiredService<VllmHealthCheckWorker>();

            return new VllmServiceOrchestrator(
                opts,
                stateTracker,
                restartPolicy,
                gpuMonitor,
                modelLoader,
                healthCheckWorker);
        });

        return services;
    }

    /// <summary>
    /// Registers the retry contract components with the DI container.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">Optional retry configuration. Uses defaults if null.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// Task 007b: Validator Errors and Model Retry Contract.
    /// Registers ErrorFormatter, RetryTracker, and EscalationFormatter as singletons.
    /// Implements IErrorFormatter, IRetryTracker, and IEscalationFormatter interfaces.
    /// </remarks>
    public static IServiceCollection AddRetryContract(
        this IServiceCollection services,
        Application.ToolSchemas.Retry.RetryConfiguration? configuration = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        var config = configuration ?? new Application.ToolSchemas.Retry.RetryConfiguration();

        // Register configuration as singleton with explicit type to avoid DI conflicts
        services.AddSingleton<Application.ToolSchemas.Retry.RetryConfiguration>(config);

        // Register retry contract services
        services.AddSingleton<IErrorFormatter, ErrorFormatter>();
        services.AddSingleton<Application.ToolSchemas.Retry.IRetryTracker>(sp =>
        {
            var cfg = sp.GetRequiredService<Application.ToolSchemas.Retry.RetryConfiguration>();
            return new Infrastructure.ToolSchemas.Retry.RetryTracker(cfg.MaxAttempts);
        });
        services.AddSingleton<IEscalationFormatter, EscalationFormatter>();

        return services;
    }

    /// <summary>
    /// Registers Structured Output enforcement components for vLLM with the DI container.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// FR-054 through FR-058: Structured Output Enforcement.
    /// Registers all components needed for JSON schema and guided decoding enforcement.
    /// </remarks>
    private static IServiceCollection AddStructuredOutputComponents(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        // Register configuration
        services.AddSingleton<StructuredOutputConfiguration>();

        // Register schema components
        services.AddSingleton<SchemaValidator>();

        // Register capability components
        services.AddSingleton<CapabilityDetector>();
        services.AddSingleton<CapabilityCache>();

        // Register response format components
        services.AddSingleton<ResponseFormatBuilder>();
        services.AddSingleton<GuidedDecodingBuilder>();

        // Register fallback components
        services.AddSingleton<OutputValidator>();
        services.AddSingleton<FallbackHandler>();

        // Register Tool Schema Registry (required for StructuredOutputHandler)
        services.AddSingleton<IToolSchemaRegistry, ToolSchemaRegistry>();

        // Register main orchestrator with required dependencies
        services.AddSingleton<StructuredOutputHandler>(sp =>
        {
            var config = sp.GetRequiredService<StructuredOutputConfiguration>();
            var schemaValidator = sp.GetRequiredService<SchemaValidator>();
            var capabilityDetector = sp.GetRequiredService<CapabilityDetector>();
            var capabilityCache = sp.GetRequiredService<CapabilityCache>();
            var responseFormatBuilder = sp.GetRequiredService<ResponseFormatBuilder>();
            var guidedDecodingBuilder = sp.GetRequiredService<GuidedDecodingBuilder>();
            var fallbackHandler = sp.GetRequiredService<FallbackHandler>();
            var logger = sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<StructuredOutputHandler>>();
            var schemaRegistry = sp.GetRequiredService<IToolSchemaRegistry>();

            return new StructuredOutputHandler(
                config,
                schemaValidator,
                capabilityDetector,
                capabilityCache,
                responseFormatBuilder,
                guidedDecodingBuilder,
                fallbackHandler,
                logger,
                schemaRegistry);
        });

        return services;
    }
}
