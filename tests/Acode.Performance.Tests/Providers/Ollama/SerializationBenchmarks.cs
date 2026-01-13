using System.Text.Json;
using Acode.Infrastructure.Ollama.Models;
using Acode.Infrastructure.Ollama.Serialization;
using BenchmarkDotNet.Attributes;

namespace Acode.Performance.Tests.Providers.Ollama;

/// <summary>
/// Performance benchmarks for Ollama serialization operations.
/// </summary>
/// <remarks>
/// Gap #20: Performance benchmarks per Testing Requirements lines 598-605.
/// NFR-001: Request serialization must complete in &lt; 1ms.
/// NFR-002: Response parsing must complete in &lt; 5ms.
/// NFR-003: Chunk parsing must complete in &lt; 100μs.
/// NFR-004: Memory allocation should be minimal.
/// </remarks>
[MemoryDiagnoser]
[SimpleJob(warmupCount: 3, iterationCount: 10)]
public class SerializationBenchmarks
{
    private OllamaRequest _testRequest = null!;
    private string _testResponseJson = null!;
    private string _testChunkJson = null!;

    [GlobalSetup]
    public void Setup()
    {
        // Prepare test data
        _testRequest = new OllamaRequest(
            model: "llama3.2:8b",
            messages: new[]
            {
                new OllamaMessage(role: "system", content: "You are a helpful assistant."),
                new OllamaMessage(role: "user", content: "Hello, how are you?"),
            },
            stream: false,
            tools: null,
            format: null,
            options: new OllamaOptions(
                temperature: 0.7,
                topP: 0.9,
                seed: 42,
                numCtx: 4096,
                stop: new[] { "STOP" }),
            keepAlive: "5m");

        _testResponseJson = """
        {
            "model": "llama3.2:8b",
            "created_at": "2024-01-01T12:00:00Z",
            "message": {
                "role": "assistant",
                "content": "Hello! I'm doing well, thank you for asking. How can I assist you today?"
            },
            "done": true,
            "done_reason": "stop",
            "total_duration": 1500000000,
            "prompt_eval_count": 25,
            "eval_count": 18
        }
        """;

        _testChunkJson = """
        {
            "model": "llama3.2:8b",
            "message": {
                "role": "assistant",
                "content": "Hello"
            },
            "done": false
        }
        """;
    }

    /// <summary>
    /// Benchmark request serialization performance.
    /// </summary>
    /// <remarks>
    /// NFR-001: Must complete in &lt; 1ms.
    /// Measures time to serialize OllamaRequest to JSON.
    /// </remarks>
    [Benchmark]
    public string Benchmark_Request_Serialization()
    {
        return JsonSerializer.Serialize(_testRequest, OllamaJsonContext.Default.OllamaRequest);
    }

    /// <summary>
    /// Benchmark response parsing performance.
    /// </summary>
    /// <remarks>
    /// NFR-002: Must complete in &lt; 5ms.
    /// Measures time to deserialize JSON to OllamaResponse.
    /// </remarks>
    [Benchmark]
    public OllamaResponse? Benchmark_Response_Parsing()
    {
        return JsonSerializer.Deserialize(_testResponseJson, OllamaJsonContext.Default.OllamaResponse);
    }

    /// <summary>
    /// Benchmark stream chunk parsing performance.
    /// </summary>
    /// <remarks>
    /// NFR-003: Must complete in &lt; 100μs.
    /// Measures time to deserialize JSON to OllamaStreamChunk.
    /// </remarks>
    [Benchmark]
    public OllamaStreamChunk? Benchmark_Chunk_Parsing()
    {
        return JsonSerializer.Deserialize(_testChunkJson, OllamaJsonContext.Default.OllamaStreamChunk);
    }

    /// <summary>
    /// Benchmark request serialization with source generator options.
    /// </summary>
    /// <remarks>
    /// Measures performance using source generator context (recommended approach).
    /// Should be faster than reflection-based serialization.
    /// </remarks>
    [Benchmark]
    public string Benchmark_Request_Serialization_SourceGen()
    {
        var options = new JsonSerializerOptions
        {
            TypeInfoResolver = OllamaJsonContext.Default,
        };

        return JsonSerializer.Serialize(_testRequest, options);
    }

    /// <summary>
    /// Benchmark response parsing with source generator options.
    /// </summary>
    /// <remarks>
    /// Measures performance using source generator context (recommended approach).
    /// Should be faster than reflection-based deserialization.
    /// </remarks>
    [Benchmark]
    public OllamaResponse? Benchmark_Response_Parsing_SourceGen()
    {
        var options = new JsonSerializerOptions
        {
            TypeInfoResolver = OllamaJsonContext.Default,
        };

        return JsonSerializer.Deserialize<OllamaResponse>(_testResponseJson, options);
    }

    /// <summary>
    /// Benchmark end-to-end serialization round-trip.
    /// </summary>
    /// <remarks>
    /// Measures combined performance of serialization + deserialization.
    /// Useful for understanding total overhead in request/response cycle.
    /// </remarks>
    [Benchmark]
    public OllamaResponse? Benchmark_RoundTrip()
    {
        // Serialize request
        _ = JsonSerializer.Serialize(_testRequest, OllamaJsonContext.Default.OllamaRequest);

        // Deserialize response (simulating server response)
        return JsonSerializer.Deserialize(_testResponseJson, OllamaJsonContext.Default.OllamaResponse);
    }
}
