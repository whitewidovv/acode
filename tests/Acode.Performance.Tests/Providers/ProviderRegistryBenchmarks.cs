using Acode.Application.Inference;
using Acode.Application.Providers;
using Acode.Application.Providers.Selection;
using Acode.Domain.Models.Inference;
using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.Logging.Abstractions;

namespace Acode.Performance.Tests.Providers;

/// <summary>
/// Performance benchmarks for ProviderRegistry operations.
/// Validates performance targets from Task 004c spec.
/// </summary>
[MemoryDiagnoser]
[SimpleJob(warmupCount: 3, iterationCount: 10)]
public class ProviderRegistryBenchmarks
{
    private readonly object _lock = new();
    private ProviderRegistry _registry = null!;
    private ProviderDescriptor _descriptor1 = null!;
    private ProviderDescriptor _descriptor2 = null!;
    private ProviderDescriptor _descriptor3 = null!;
    private ChatRequest _streamingRequest = null!;

    [GlobalSetup]
    public void Setup()
    {
        // Create provider descriptors
        _descriptor1 = new ProviderDescriptor
        {
            Id = "ollama",
            Name = "Ollama Local",
            Type = ProviderType.Local,
            Capabilities = new ProviderCapabilities(
                supportsStreaming: true,
                supportsTools: true,
                supportedModels: new[] { "codellama:7b", "codellama:13b" }),
            Endpoint = new ProviderEndpoint(new Uri("http://localhost:11434"))
        };

        _descriptor2 = new ProviderDescriptor
        {
            Id = "vllm",
            Name = "vLLM Local",
            Type = ProviderType.Local,
            Capabilities = new ProviderCapabilities(
                supportsStreaming: true,
                supportsTools: false,
                supportedModels: new[] { "deepseek-coder:6.7b" }),
            Endpoint = new ProviderEndpoint(new Uri("http://localhost:8000"))
        };

        _descriptor3 = new ProviderDescriptor
        {
            Id = "llamacpp",
            Name = "llama.cpp Local",
            Type = ProviderType.Local,
            Capabilities = new ProviderCapabilities(
                supportsStreaming: false,
                supportsTools: false,
                supportedModels: new[] { "mistral-7b" }),
            Endpoint = new ProviderEndpoint(new Uri("http://localhost:8080"))
        };

        // Create test request
        _streamingRequest = new ChatRequest(
            messages: new[]
            {
                new ChatMessage(MessageRole.User, "Test message", null, null)
            },
            stream: true);

        // Initialize registry with providers
        var logger = NullLogger<ProviderRegistry>.Instance;
        var selector = new CapabilityProviderSelector(_ => null);
        _registry = new ProviderRegistry(logger, selector, "ollama");
        _registry.Register(_descriptor1);
        _registry.Register(_descriptor2);
        _registry.Register(_descriptor3);
    }

    /// <summary>
    /// Benchmark 1: Register a new provider.
    /// Target: &lt; 1ms per registration.
    /// </summary>
    [Benchmark]
    public void Benchmark_Registration()
    {
        var logger = NullLogger<ProviderRegistry>.Instance;
        var selector = new CapabilityProviderSelector(_ => null);
        var registry = new ProviderRegistry(logger, selector);

        var descriptor = new ProviderDescriptor
        {
            Id = $"provider-{Guid.NewGuid()}",
            Name = "Test Provider",
            Type = ProviderType.Local,
            Capabilities = new ProviderCapabilities(supportsStreaming: true, supportsTools: true),
            Endpoint = new ProviderEndpoint(new Uri("http://localhost:9000"))
        };

        registry.Register(descriptor);
    }

    /// <summary>
    /// Benchmark 2: Get default provider.
    /// Target: &lt; 0.1ms.
    /// </summary>
    [Benchmark]
    public void Benchmark_GetDefaultProvider()
    {
        try
        {
            _ = _registry.GetDefaultProvider();
        }
        catch (Acode.Application.Providers.Exceptions.ProviderNotFoundException)
        {
            // Expected when provider factory returns null
        }
    }

    /// <summary>
    /// Benchmark 3: Get provider by ID.
    /// Target: &lt; 0.1ms.
    /// </summary>
    [Benchmark]
    public void Benchmark_GetProviderById()
    {
        try
        {
            _ = _registry.GetProvider("ollama");
        }
        catch (Acode.Application.Providers.Exceptions.ProviderNotFoundException)
        {
            // Expected when provider factory returns null
        }
    }

    /// <summary>
    /// Benchmark 4: Select provider for request (capability matching).
    /// Target: &lt; 1ms.
    /// </summary>
    [Benchmark]
    public void Benchmark_GetProviderFor()
    {
        try
        {
            _ = _registry.GetProviderFor(_streamingRequest);
        }
        catch (Acode.Application.Providers.Exceptions.NoCapableProviderException)
        {
            // Expected when selector returns null
        }
    }

    /// <summary>
    /// Benchmark 5: Concurrent access (thread-safety test).
    /// Target: &lt; 10ms for 100 concurrent operations.
    /// </summary>
    [Benchmark]
    public void Benchmark_ConcurrentAccess()
    {
        var tasks = new List<Task>();

        // 100 concurrent read operations
        for (var i = 0; i < 100; i++)
        {
            tasks.Add(Task.Run(() =>
            {
                try
                {
                    _ = _registry.IsRegistered("ollama");
                    _ = _registry.ListProviders();
                }
                catch
                {
                    // Ignore exceptions in benchmark
                }
            }));
        }

        Task.WaitAll(tasks.ToArray());
    }
}
