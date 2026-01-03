# Task 006: vLLM Provider Adapter

**Priority:** P1 – High Priority  
**Tier:** Core Infrastructure  
**Complexity:** 21 (Fibonacci points)  
**Phase:** Foundation  
**Dependencies:** Task 004, Task 004.a, Task 004.b, Task 004.c, Task 001, Task 002  

---

## Description

Task 006 implements the vLLM Provider Adapter, an alternative high-performance inference backend for the Agentic Coding Bot (Acode) system. vLLM is a production-grade inference engine optimized for throughput and GPU memory efficiency, making it an excellent choice for users with demanding workloads or advanced GPU infrastructure. This adapter translates between Acode's canonical model interface (Task 004) and vLLM's OpenAI-compatible API.

vLLM occupies a different niche than Ollama in the Acode ecosystem. While Ollama prioritizes ease of setup and consumer hardware compatibility, vLLM targets high-throughput scenarios with features like continuous batching, PagedAttention for memory efficiency, and optimized CUDA kernels. Users with GPU clusters, dedicated inference servers, or production workloads benefit from vLLM's performance characteristics.

The vLLM adapter implements the same IModelProvider interface as the Ollama adapter, ensuring interchangeability at the application layer. Consumer code does not need to know which provider serves a request—the Provider Registry (Task 004.c) routes requests to the appropriate provider based on configuration and capabilities. This abstraction enables users to switch providers or use multiple providers simultaneously.

vLLM exposes an OpenAI-compatible API, which simplifies the adapter implementation. The `/v1/chat/completions` endpoint accepts requests in OpenAI format and returns compatible responses. However, vLLM-specific extensions (structured output enforcement, guided decoding, speculation) require additional mapping. The adapter MUST support both the standard OpenAI subset and vLLM-specific features where relevant.

Streaming support in vLLM follows the OpenAI Server-Sent Events (SSE) format rather than Ollama's NDJSON. The adapter MUST parse SSE streams correctly, handling the `data: ` prefix, `[DONE]` sentinel, and potential keep-alive comments. Streaming enables real-time response delivery and MUST be implemented with proper cancellation and cleanup handling.

Tool calling in vLLM uses the OpenAI function calling format. The adapter MUST map Acode's ToolDefinition to OpenAI's function format, handle tool_choice parameters, and parse tool_calls from responses. vLLM's tool calling support depends on the underlying model—some models support native function calling while others require guided JSON generation.

Structured output enforcement is a key vLLM feature addressed in subtask 006.b. vLLM can constrain generation to match a provided JSON Schema, eliminating the need for retry-on-invalid-JSON logic used with Ollama. The adapter MUST expose this capability through configuration and integrate with the Tool Schema Registry (Task 007).

Health checking and load monitoring are critical for production vLLM deployments. The adapter MUST query vLLM's metrics endpoint to assess server health, GPU utilization, and request queue depth. This information feeds into the Provider Registry's health status, enabling intelligent routing and fallback when vLLM is overloaded or unhealthy.

Configuration for vLLM follows the same patterns as Ollama, with provider-specific settings in `.agent/config.yml`. The adapter MUST support endpoint configuration, timeout tuning, model mappings, and vLLM-specific options like tensor parallel degree hints. Environment variable overrides enable deployment flexibility without config file changes.

Error handling addresses vLLM-specific failure modes: CUDA out-of-memory errors, model loading failures, request queue overflow, and network errors. Each error type MUST be translated into appropriate Acode exceptions with error codes enabling callers to distinguish failures. The adapter MUST NOT mask vLLM errors but MUST present them in Acode's exception hierarchy.

The vLLM adapter expands Acode's deployment options, enabling users to choose the inference backend that best matches their requirements. Some users may use Ollama for development and vLLM for production, or use both simultaneously with capability-based routing. This flexibility requires both adapters to behave consistently from the application layer's perspective.

---

## Glossary / Terms

| Term | Definition |
|------|------------|
| vLLM | High-performance LLM inference engine with GPU optimization |
| VllmProvider | Implementation of IModelProvider for vLLM backend |
| OpenAI-Compatible API | vLLM's API following OpenAI's specification |
| Chat Completions | /v1/chat/completions endpoint for conversation |
| SSE | Server-Sent Events format for streaming responses |
| PagedAttention | vLLM's memory-efficient attention mechanism |
| Continuous Batching | Dynamic request batching for throughput |
| Tensor Parallelism | Distributing model across multiple GPUs |
| Pipeline Parallelism | Distributing model layers across GPUs |
| Guided Decoding | Constraining output to match schema |
| Structured Output | JSON Schema-enforced response format |
| Token Streaming | Real-time delivery of generated tokens |
| Usage Info | Token counts in response |
| Finish Reason | Why generation stopped (stop, length, tool_calls) |
| System Prompt | Initial instruction message |
| Temperature | Sampling randomness parameter |
| Top-P | Nucleus sampling parameter |
| Model Loading | Loading model weights into GPU memory |
| KV Cache | Key-value cache for efficient inference |
| Request Queue | Pending requests waiting for processing |

---

## Out of Scope

The following items are explicitly excluded from Task 006:

- **vLLM installation** - Users must have vLLM running
- **Model downloading** - Model preparation is user responsibility  
- **GPU cluster management** - Infrastructure is user responsibility
- **Tensor parallelism configuration** - vLLM server config
- **Model quantization** - Quantization is preprocessing step
- **vLLM version upgrades** - Version management not handled
- **Multi-instance load balancing** - Beyond single endpoint
- **Authentication** - Token auth addressed if needed
- **Custom sampling strategies** - Standard sampling only
- **Speculative decoding config** - Advanced optimization excluded
- **CUDA driver management** - System administration excluded

---

## Functional Requirements

### VllmProvider Class

- FR-001: VllmProvider MUST implement IModelProvider interface (Task 004)
- FR-002: VllmProvider MUST be defined in the Infrastructure layer
- FR-003: VllmProvider MUST be registered with the Provider Registry
- FR-004: VllmProvider MUST accept configuration via dependency injection
- FR-005: VllmProvider MUST use HttpClient for API communication
- FR-006: VllmProvider MUST implement IAsyncDisposable for cleanup
- FR-007: VllmProvider MUST log all API interactions with correlation IDs

### Configuration

- FR-008: Adapter MUST read endpoint from config (default http://localhost:8000)
- FR-009: Adapter MUST support connect timeout configuration (default 5s)
- FR-010: Adapter MUST support request timeout configuration (default 300s)
- FR-011: Adapter MUST support streaming timeout configuration (default 600s)
- FR-012: Adapter MUST support retry configuration (default 3 retries)
- FR-013: Adapter MUST support API key configuration (optional)
- FR-014: Adapter MUST support environment variable overrides
- FR-015: Adapter MUST validate configuration on startup

### Chat Completion - Non-Streaming

- FR-016: CompleteAsync MUST call /v1/chat/completions endpoint
- FR-017: CompleteAsync MUST map ChatRequest to OpenAI request format
- FR-018: CompleteAsync MUST set model from request or default
- FR-019: CompleteAsync MUST set stream: false for non-streaming
- FR-020: CompleteAsync MUST map messages to OpenAI format
- FR-021: CompleteAsync MUST map tool definitions to functions format
- FR-022: CompleteAsync MUST include temperature, top_p, max_tokens
- FR-023: CompleteAsync MUST parse OpenAI response to ChatResponse
- FR-024: CompleteAsync MUST map finish_reason correctly
- FR-025: CompleteAsync MUST extract usage from response
- FR-026: CompleteAsync MUST handle tool_calls in response
- FR-027: CompleteAsync MUST timeout after configured duration
- FR-028: CompleteAsync MUST support cancellation via CancellationToken

### Chat Completion - Streaming

- FR-029: StreamAsync MUST return IAsyncEnumerable<ResponseDelta>
- FR-030: StreamAsync MUST call /v1/chat/completions with stream: true
- FR-031: StreamAsync MUST parse SSE format (data: prefix)
- FR-032: StreamAsync MUST handle [DONE] sentinel
- FR-033: StreamAsync MUST yield ResponseDelta for each chunk
- FR-034: StreamAsync MUST accumulate content deltas correctly
- FR-035: StreamAsync MUST accumulate tool call deltas correctly
- FR-036: StreamAsync MUST detect final chunk
- FR-037: StreamAsync MUST include usage in final delta (if returned)
- FR-038: StreamAsync MUST support cancellation mid-stream
- FR-039: StreamAsync MUST cleanup HTTP stream on cancellation
- FR-040: StreamAsync MUST timeout on stalled streams

### Message Mapping

- FR-041: Adapter MUST map MessageRole.System to "system"
- FR-042: Adapter MUST map MessageRole.User to "user"
- FR-043: Adapter MUST map MessageRole.Assistant to "assistant"
- FR-044: Adapter MUST map MessageRole.Tool to "tool"
- FR-045: Adapter MUST include tool_calls in assistant messages
- FR-046: Adapter MUST include tool_call_id in tool messages
- FR-047: Adapter MUST preserve message ordering
- FR-048: Adapter MUST handle null content in messages

### Tool Calling

- FR-049: Adapter MUST map ToolDefinition to OpenAI function format
- FR-050: Adapter MUST set tools array in request
- FR-051: Adapter MUST support tool_choice parameter
- FR-052: Adapter MUST parse tool_calls from response
- FR-053: Adapter MUST extract function name and arguments
- FR-054: Adapter MUST validate tool call JSON arguments
- FR-055: Adapter MUST set FinishReason.ToolCalls appropriately
- FR-056: Adapter MUST support multiple simultaneous tool calls

### Structured Output (Basic)

- FR-057: Adapter MUST support response_format parameter
- FR-058: Adapter MUST support type: "json_object" format
- FR-059: Adapter MUST pass JSON Schema when configured
- FR-060: Advanced structured output is in Task 006.b

### Model Management

- FR-061: ListModelsAsync MUST call /v1/models endpoint
- FR-062: ListModelsAsync MUST parse model list response
- FR-063: ListModelsAsync MUST return model identifiers
- FR-064: Adapter MUST handle models with path prefixes

### Health Checking

- FR-065: CheckHealthAsync MUST call /health endpoint
- FR-066: CheckHealthAsync MUST check /v1/models as fallback
- FR-067: CheckHealthAsync MUST measure response time
- FR-068: CheckHealthAsync MUST return Healthy on success
- FR-069: CheckHealthAsync MUST return Unhealthy on failure
- FR-070: CheckHealthAsync MUST return Degraded on slow response (>5s)
- FR-071: CheckHealthAsync MUST timeout after 10 seconds
- FR-072: CheckHealthAsync MUST NOT throw exceptions

### Error Handling

- FR-073: Adapter MUST wrap network errors in VllmConnectionException
- FR-074: Adapter MUST wrap timeout errors in VllmTimeoutException
- FR-075: Adapter MUST wrap 4xx errors in VllmRequestException
- FR-076: Adapter MUST wrap 5xx errors in VllmServerException
- FR-077: Adapter MUST wrap parse errors in VllmParseException
- FR-078: Adapter MUST include original exception as inner exception
- FR-079: Adapter MUST include request ID in exception data
- FR-080: Adapter MUST log all exceptions with full context

### Retry Logic

- FR-081: Adapter MUST retry on transient network errors
- FR-082: Adapter MUST retry on 503 Service Unavailable
- FR-083: Adapter MUST NOT retry on 4xx client errors (except 429)
- FR-084: Adapter MUST retry on 429 Too Many Requests with backoff
- FR-085: Adapter MUST implement exponential backoff
- FR-086: Adapter MUST respect retry count limit
- FR-087: Adapter MUST log each retry attempt
- FR-088: Adapter MUST throw after max retries exceeded

### Request/Response Types

- FR-089: VllmRequest MUST be defined for API requests
- FR-090: VllmResponse MUST be defined for API responses
- FR-091: VllmMessage MUST map to/from ChatMessage
- FR-092: VllmToolCall MUST map to/from ToolCall
- FR-093: VllmUsage MUST map to UsageInfo
- FR-094: Types MUST use System.Text.Json source generators

---

## Non-Functional Requirements

### Performance

- NFR-001: Connection establishment MUST complete in < 500ms
- NFR-002: Request serialization MUST complete in < 1ms
- NFR-003: Response parsing MUST complete in < 5ms
- NFR-004: Streaming chunk parsing MUST complete in < 100μs
- NFR-005: Memory allocation per request MUST be < 10KB (excluding content)
- NFR-006: HttpClient MUST use connection pooling
- NFR-007: Adapter MUST NOT buffer entire streaming responses
- NFR-008: Adapter MUST release HTTP streams promptly

### Reliability

- NFR-009: Adapter MUST handle vLLM restarts gracefully
- NFR-010: Adapter MUST not crash on malformed responses
- NFR-011: Adapter MUST timeout stalled requests appropriately
- NFR-012: Adapter MUST cleanup resources on disposal
- NFR-013: Adapter MUST be thread-safe for concurrent requests
- NFR-014: Adapter MUST not leak file handles or connections

### Security

- NFR-015: Adapter MUST only connect to configured endpoints
- NFR-016: API key MUST NOT be logged (redact in all logs)
- NFR-017: Adapter MUST NOT log request content at INFO level
- NFR-018: Adapter MUST sanitize error messages for logging
- NFR-019: Adapter MUST validate URLs are local in airgapped mode

### Observability

- NFR-020: All requests MUST have correlation IDs
- NFR-021: Request/response timing MUST be logged
- NFR-022: Token counts MUST be logged for each request
- NFR-023: Errors MUST be logged with structured fields
- NFR-024: Health check results MUST be logged

### Maintainability

- NFR-025: All public APIs MUST have XML documentation
- NFR-026: Adapter MUST follow Clean Architecture patterns
- NFR-027: Adapter MUST be testable with mock HTTP responses
- NFR-028: Configuration MUST be documented with examples

---

## User Manual Documentation

### Overview

The vLLM Provider Adapter enables Acode to use vLLM as its inference backend. vLLM provides high-performance inference optimized for throughput and GPU efficiency, ideal for production deployments.

### Prerequisites

1. **vLLM Server**: vLLM must be installed and running
2. **Model Loaded**: Model must be loaded in vLLM
3. **Network Access**: Acode must reach vLLM endpoint

### Quick Start

#### Start vLLM Server

```bash
# Start vLLM with a model
python -m vllm.entrypoints.openai.api_server \
    --model meta-llama/Llama-3.2-8B-Instruct \
    --port 8000

# Verify server is running
curl http://localhost:8000/v1/models
```

#### Configure Acode

```yaml
# .agent/config.yml
model:
  default_provider: vllm
  
  providers:
    vllm:
      enabled: true
      endpoint: http://localhost:8000
      default_model: meta-llama/Llama-3.2-8B-Instruct
```

#### Verify Connection

```
$ acode providers health
┌────────────────────────────────────────────────────────────┐
│ Provider Health                                             │
├─────────┬─────────┬──────────┬────────────────────────────┤
│ vllm    │ Healthy │ 120ms    │ 1 model loaded              │
└─────────┴─────────┴──────────┴────────────────────────────┘
```

### Configuration Reference

```yaml
model:
  providers:
    vllm:
      # Enable/disable provider
      enabled: true
      
      # vLLM API endpoint
      endpoint: http://localhost:8000
      
      # API key (optional, if vLLM configured with auth)
      api_key: ${VLLM_API_KEY}
      
      # Default model for requests
      default_model: meta-llama/Llama-3.2-8B-Instruct
      
      # Timeouts
      connect_timeout_seconds: 5
      request_timeout_seconds: 300
      streaming_timeout_seconds: 600
      
      # Retry configuration
      retry:
        max_retries: 3
        initial_delay_ms: 100
        max_delay_ms: 30000
        backoff_multiplier: 2.0
      
      # Default generation parameters
      options:
        temperature: 0.7
        top_p: 0.9
        max_tokens: 4096
```

### Environment Variables

```bash
# Override endpoint
ACODE_MODEL_PROVIDERS_VLLM_ENDPOINT=http://gpu-server:8000

# Set API key
ACODE_MODEL_PROVIDERS_VLLM_API_KEY=your-api-key

# Override timeout
ACODE_MODEL_PROVIDERS_VLLM_REQUEST_TIMEOUT_SECONDS=600
```

### vLLM vs Ollama

| Feature | Ollama | vLLM |
|---------|--------|------|
| Setup | Easy | Complex |
| Throughput | Moderate | High |
| GPU Memory | Standard | Optimized (PagedAttention) |
| Batching | Limited | Continuous |
| Tool Calling | Basic | OpenAI-compatible |
| Structured Output | Retry-based | Guided decoding |
| Target Use | Development | Production |

### Tool Calling

```csharp
var tools = new[]
{
    new ToolDefinition
    {
        Name = "read_file",
        Description = "Read contents of a file",
        Parameters = JsonDocument.Parse("""
        {
            "type": "object",
            "properties": {
                "path": { "type": "string" }
            },
            "required": ["path"]
        }
        """).RootElement
    }
};

var request = new ChatRequest
{
    Model = "meta-llama/Llama-3.2-8B-Instruct",
    Messages = messages,
    Tools = tools,
    ToolChoice = "auto"
};

var response = await provider.CompleteAsync(request);
```

### Streaming

```csharp
await foreach (var delta in provider.StreamAsync(request))
{
    if (delta.ContentDelta is not null)
    {
        Console.Write(delta.ContentDelta);
    }
}
```

### Troubleshooting

#### Connection Refused

**Error:** `VllmConnectionException: Connection refused`

**Resolution:**
```bash
# Check vLLM is running
curl http://localhost:8000/health

# Check endpoint config
cat .agent/config.yml | grep endpoint
```

#### Model Not Found

**Error:** `VllmRequestException: Model not found`

**Resolution:**
1. Verify model name matches exactly
2. Check with: `curl http://localhost:8000/v1/models`

#### GPU Out of Memory

**Error:** `VllmServerException: CUDA out of memory`

**Resolution:**
1. Use smaller model
2. Reduce max_tokens
3. Use quantized model
4. Increase GPU memory

---

## Acceptance Criteria

### VllmProvider Class

- [ ] AC-001: Implements IModelProvider
- [ ] AC-002: Located in Infrastructure layer
- [ ] AC-003: Registered with Provider Registry
- [ ] AC-004: Accepts configuration via DI
- [ ] AC-005: Uses HttpClient with pooling
- [ ] AC-006: Implements IAsyncDisposable
- [ ] AC-007: Logs with correlation IDs

### Configuration

- [ ] AC-008: Reads endpoint from config
- [ ] AC-009: Default endpoint is localhost:8000
- [ ] AC-010: Connect timeout configurable
- [ ] AC-011: Request timeout configurable
- [ ] AC-012: Streaming timeout configurable
- [ ] AC-013: Retry config supported
- [ ] AC-014: API key configurable
- [ ] AC-015: Environment overrides work
- [ ] AC-016: Config validated on startup

### Non-Streaming Completion

- [ ] AC-017: Calls /v1/chat/completions
- [ ] AC-018: Maps ChatRequest correctly
- [ ] AC-019: Sets model from request
- [ ] AC-020: Sets stream: false
- [ ] AC-021: Maps messages correctly
- [ ] AC-022: Maps tool definitions
- [ ] AC-023: Includes generation options
- [ ] AC-024: Parses response correctly
- [ ] AC-025: Maps finish_reason
- [ ] AC-026: Extracts usage
- [ ] AC-027: Handles tool_calls
- [ ] AC-028: Respects timeout
- [ ] AC-029: Supports cancellation

### Streaming Completion

- [ ] AC-030: Returns IAsyncEnumerable
- [ ] AC-031: Sets stream: true
- [ ] AC-032: Parses SSE format
- [ ] AC-033: Handles [DONE]
- [ ] AC-034: Yields ResponseDelta
- [ ] AC-035: Accumulates content
- [ ] AC-036: Accumulates tool calls
- [ ] AC-037: Detects final chunk
- [ ] AC-038: Includes usage when present
- [ ] AC-039: Supports cancellation
- [ ] AC-040: Cleans up stream
- [ ] AC-041: Times out on stall

### Message Mapping

- [ ] AC-042: System role maps correctly
- [ ] AC-043: User role maps correctly
- [ ] AC-044: Assistant role maps correctly
- [ ] AC-045: Tool role maps correctly
- [ ] AC-046: Tool calls in messages
- [ ] AC-047: Tool call ID in messages
- [ ] AC-048: Order preserved
- [ ] AC-049: Null content handled

### Tool Calling

- [ ] AC-050: Maps ToolDefinition
- [ ] AC-051: Sets tools array
- [ ] AC-052: Supports tool_choice
- [ ] AC-053: Parses tool_calls
- [ ] AC-054: Extracts function info
- [ ] AC-055: Validates JSON
- [ ] AC-056: Sets FinishReason
- [ ] AC-057: Supports multiple calls

### Structured Output

- [ ] AC-058: response_format supported
- [ ] AC-059: json_object type works
- [ ] AC-060: JSON Schema passthrough

### Model Management

- [ ] AC-061: ListModelsAsync works
- [ ] AC-062: Parses model list
- [ ] AC-063: Returns identifiers
- [ ] AC-064: Handles path prefixes

### Health Checking

- [ ] AC-065: Calls /health
- [ ] AC-066: Falls back to /v1/models
- [ ] AC-067: Measures response time
- [ ] AC-068: Returns Healthy
- [ ] AC-069: Returns Unhealthy
- [ ] AC-070: Returns Degraded
- [ ] AC-071: Respects timeout
- [ ] AC-072: Never throws

### Error Handling

- [ ] AC-073: Wraps network errors
- [ ] AC-074: Wraps timeout errors
- [ ] AC-075: Wraps 4xx errors
- [ ] AC-076: Wraps 5xx errors
- [ ] AC-077: Wraps parse errors
- [ ] AC-078: Includes inner exception
- [ ] AC-079: Includes request ID
- [ ] AC-080: Logs errors fully

### Security

- [ ] AC-081: Only configured endpoints
- [ ] AC-082: API key redacted in logs
- [ ] AC-083: No content at INFO level
- [ ] AC-084: Error messages sanitized
- [ ] AC-085: Local-only in airgapped

---

## Testing Requirements

### Unit Tests

```
Tests/Unit/Infrastructure/Vllm/
├── VllmProviderTests.cs
│   ├── CompleteAsync_Should_Call_Endpoint()
│   ├── CompleteAsync_Should_Map_Request()
│   ├── CompleteAsync_Should_Parse_Response()
│   ├── CompleteAsync_Should_Handle_ToolCalls()
│   ├── StreamAsync_Should_Parse_SSE()
│   ├── StreamAsync_Should_Handle_Done()
│   ├── CheckHealthAsync_Should_Return_Healthy()
│   └── CheckHealthAsync_Should_Return_Unhealthy()
│
├── VllmMessageMapperTests.cs
│   ├── Should_Map_Roles()
│   ├── Should_Map_ToolCalls()
│   └── Should_Preserve_Order()
│
├── VllmSseParserTests.cs
│   ├── Should_Parse_Data_Lines()
│   ├── Should_Handle_Done()
│   └── Should_Handle_Comments()
│
└── VllmRetryPolicyTests.cs
    ├── Should_Retry_Transient()
    ├── Should_Not_Retry_4xx()
    └── Should_Apply_Backoff()
```

### Integration Tests

```
Tests/Integration/Vllm/
├── VllmProviderIntegrationTests.cs
│   ├── Should_Complete_Request()
│   ├── Should_Stream_Response()
│   └── Should_Handle_ToolCalls()
```

### Performance Tests

```
Tests/Performance/Vllm/
├── VllmBenchmarks.cs
│   ├── Benchmark_Serialization()
│   ├── Benchmark_Parsing()
│   └── Benchmark_SSE_Parsing()
```

---

## User Verification Steps

### Scenario 1: Basic Completion

1. Start vLLM server
2. Configure vLLM provider
3. Run `acode ask "Hello"`
4. Verify response received

### Scenario 2: Streaming

1. Run `acode ask "Write a story" --stream`
2. Verify tokens appear incrementally
3. Verify final stats displayed

### Scenario 3: Tool Calling

1. Create request with tools
2. Send to provider
3. Verify tool call in response

### Scenario 4: Health Check

1. Run `acode providers health`
2. Verify vLLM shows Healthy
3. Stop vLLM
4. Verify shows Unhealthy

### Scenario 5: Model Listing

1. Run `acode models list --provider vllm`
2. Verify model shown

### Scenario 6: API Key

1. Configure vLLM with auth
2. Set API key in config
3. Verify requests work
4. Verify key not logged

### Scenario 7: Timeout

1. Configure short timeout
2. Send long request
3. Verify timeout error

### Scenario 8: Retry

1. Configure retry
2. Simulate transient failure
3. Verify retry occurs

---

## Implementation Prompt

### File Structure

```
src/AgenticCoder.Infrastructure/Vllm/
├── VllmProvider.cs
├── VllmConfiguration.cs
├── VllmHttpClient.cs
├── Mapping/
│   ├── VllmMessageMapper.cs
│   ├── VllmToolMapper.cs
│   └── VllmResponseParser.cs
├── Streaming/
│   └── VllmSseReader.cs
├── Models/
│   ├── VllmRequest.cs
│   ├── VllmResponse.cs
│   ├── VllmMessage.cs
│   ├── VllmToolCall.cs
│   └── VllmStreamChunk.cs
├── Health/
│   └── VllmHealthChecker.cs
└── Exceptions/
    ├── VllmException.cs
    ├── VllmConnectionException.cs
    ├── VllmTimeoutException.cs
    ├── VllmRequestException.cs
    ├── VllmServerException.cs
    └── VllmParseException.cs
```

### VllmProvider Implementation

```csharp
namespace AgenticCoder.Infrastructure.Vllm;

public sealed class VllmProvider : IModelProvider, IAsyncDisposable
{
    private readonly VllmHttpClient _client;
    private readonly VllmConfiguration _config;
    private readonly ILogger<VllmProvider> _logger;
    
    public string ProviderId => "vllm";
    public ProviderType Type => ProviderType.Vllm;
    
    public async Task<ChatResponse> CompleteAsync(
        ChatRequest request,
        CancellationToken cancellationToken = default)
    {
        var correlationId = Guid.NewGuid().ToString();
        using var scope = _logger.BeginScope(new { CorrelationId = correlationId });
        
        var vllmRequest = VllmMessageMapper.MapRequest(request, _config);
        vllmRequest.Stream = false;
        
        _logger.LogDebug("Sending chat request to vLLM: {Model}", request.Model);
        
        var response = await _client.PostAsync<VllmResponse>(
            "/v1/chat/completions",
            vllmRequest,
            cancellationToken);
        
        return VllmResponseParser.Parse(response, correlationId);
    }
    
    // Additional implementation...
}
```

### Error Codes

| Code | Message |
|------|---------|
| ACODE-VLM-001 | Unable to connect to vLLM |
| ACODE-VLM-002 | vLLM request timeout |
| ACODE-VLM-003 | Model not found |
| ACODE-VLM-004 | Invalid request parameters |
| ACODE-VLM-005 | vLLM server error |
| ACODE-VLM-006 | Failed to parse response |
| ACODE-VLM-007 | Invalid tool call |
| ACODE-VLM-008 | Streaming connection lost |
| ACODE-VLM-009 | Max retries exceeded |
| ACODE-VLM-010 | Health check failed |

### Implementation Checklist

1. [ ] Create VllmConfiguration class
2. [ ] Create VllmHttpClient
3. [ ] Implement VllmMessageMapper
4. [ ] Implement VllmToolMapper
5. [ ] Implement VllmResponseParser
6. [ ] Implement VllmSseReader
7. [ ] Create VllmProvider class
8. [ ] Implement CompleteAsync
9. [ ] Implement StreamAsync
10. [ ] Implement ListModelsAsync
11. [ ] Implement CheckHealthAsync
12. [ ] Create exception types
13. [ ] Implement retry logic
14. [ ] Register with Provider Registry
15. [ ] Write unit tests
16. [ ] Write integration tests
17. [ ] Add XML documentation

### Dependencies

- Task 004 (IModelProvider interface)
- Task 004.a (Message/ToolCall types)
- Task 004.b (Response types)
- Task 004.c (Provider Registry)
- System.Net.Http for HTTP client

### Verification Command

```bash
dotnet test --filter "FullyQualifiedName~Vllm"
```

---

**End of Task 006 Specification**