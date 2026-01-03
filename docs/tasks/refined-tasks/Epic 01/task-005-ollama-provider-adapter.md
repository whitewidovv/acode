# Task 005: Ollama Provider Adapter

**Priority:** P0 – Critical Path  
**Tier:** Core Infrastructure  
**Complexity:** 21 (Fibonacci points)  
**Phase:** Foundation  
**Dependencies:** Task 004, Task 004.a, Task 004.b, Task 004.c, Task 001, Task 002  

---

## Description

Task 005 implements the Ollama Provider Adapter, the primary model inference backend for the Agentic Coding Bot (Acode) system. Ollama is a local-first inference server that runs large language models on consumer hardware without requiring cloud connectivity, making it the ideal default provider for Acode's "no external LLM API" constraint defined in Task 001. This adapter translates between Acode's canonical model interface (Task 004) and Ollama's HTTP API.

The Ollama Provider Adapter is a critical component in the Acode architecture. It sits in the Infrastructure layer, implementing the IModelProvider interface defined in the Application layer. This Clean Architecture boundary ensures that application logic remains decoupled from Ollama-specific concerns—the rest of the system interacts with providers through the abstract interface, enabling seamless substitution of providers for testing or alternative deployments.

The adapter MUST handle the full spectrum of Ollama's chat completion API capabilities including streaming responses, tool calling, JSON mode, and multi-turn conversations. Ollama's API closely follows OpenAI's chat completion format, which simplifies mapping but still requires careful translation of tool call structures, message roles, and response formats. The adapter normalizes all Ollama-specific quirks into the canonical Acode types.

Streaming support is essential for responsive user experience. When users interact with Acode, they expect to see tokens appear as they're generated rather than waiting for complete responses. The adapter implements IAsyncEnumerable-based streaming that delivers ResponseDelta instances (Task 004.b) as Ollama produces them. This requires proper handling of Ollama's NDJSON streaming format and management of HTTP response streams.

Tool calling integration is a defining feature of the Acode agent system. When the model decides to invoke a tool, the adapter MUST correctly parse the tool call from Ollama's response, validate the JSON arguments, and surface structured ToolCall instances (Task 004.a). Ollama's tool calling support varies by model—some models support native function calling while others require prompt-based tool patterns. The adapter MUST detect and adapt to model capabilities.

Error handling within the adapter covers multiple failure domains: network errors (Ollama unreachable), HTTP errors (4xx/5xx responses), parsing errors (malformed JSON), and semantic errors (invalid tool calls). Each error type MUST be translated into appropriate Acode exceptions with error codes enabling callers to distinguish failure modes. The adapter MUST NOT swallow errors—all failures bubble up with full context.

Configuration for the Ollama adapter flows from `.agent/config.yml` (Task 002) through the Provider Registry (Task 004.c). Configuration includes the Ollama endpoint URL (default http://localhost:11434), request timeouts, model mappings, and retry policies. The adapter MUST support environment variable overrides for containerized deployments where config files may be inconvenient.

Health checking enables the Provider Registry to route around unhealthy Ollama instances. The adapter implements health check logic that validates Ollama connectivity by calling the /api/tags endpoint. Health checks MUST timeout appropriately and report accurate status (Healthy, Degraded, Unhealthy) based on response time and error patterns.

Model enumeration allows the registry to discover available models. The adapter queries Ollama's model list and reports capabilities for each model. This enables capability-based provider selection—if a request requires a model only available on Ollama, the registry can route accordingly.

The adapter implements retry logic for transient failures with configurable backoff. Network glitches, temporary Ollama overload, and connection resets should trigger retries rather than immediate failure. However, retries MUST be bounded and MUST NOT retry non-transient errors like authentication failures or model not found errors.

Performance requirements mandate efficient resource usage. The adapter MUST use HTTP connection pooling, avoid unnecessary allocations on the hot path, and release resources promptly. Memory usage MUST remain bounded even for large responses—the adapter MUST NOT buffer entire streaming responses in memory.

---

## Glossary / Terms

| Term | Definition |
|------|------------|
| Ollama | Local inference server for running LLMs on consumer hardware |
| OllamaProvider | Implementation of IModelProvider for Ollama backend |
| Chat Completion | Ollama's /api/chat endpoint for conversational inference |
| Generate | Ollama's /api/generate endpoint for raw completion |
| NDJSON | Newline-delimited JSON format used for streaming responses |
| Model Tag | Ollama's model identifier format: name:version (e.g., llama3.2:8b) |
| Modelfile | Ollama configuration file defining model parameters |
| Context Window | Maximum tokens a model can process in one request |
| KV Cache | Key-value cache for efficient inference on continued conversations |
| Tool Calling | Model capability to invoke external functions |
| JSON Mode | Request mode ensuring model outputs valid JSON |
| Temperature | Sampling parameter controlling response randomness |
| Top-P | Nucleus sampling parameter limiting token probability mass |
| Seed | Random seed for deterministic generation |
| Keep Alive | Duration to keep model loaded in memory |
| Embedding | Vector representation of text (not used in chat) |
| System Prompt | Initial instruction message setting model behavior |
| Context Length | Number of tokens in the conversation context |
| Quantization | Model compression technique (q4_0, q8_0, etc.) |
| Pull | Ollama command to download a model |

---

## Out of Scope

The following items are explicitly excluded from Task 005:

- **Ollama installation or management** - Users must have Ollama running
- **Model downloading** - Users must pull models separately
- **Embeddings API** - Only chat completion is supported
- **Vision/multimodal** - Image processing not in scope
- **Ollama process management** - Starting/stopping Ollama service
- **GPU allocation** - Hardware management is user responsibility
- **Model fine-tuning** - Training is not supported
- **Custom Modelfiles** - Creating Modelfiles is not automated
- **Ollama version upgrades** - Version management not handled
- **Multi-Ollama routing** - Single Ollama instance assumed
- **Authentication** - Ollama doesn't require auth by default
- **TLS certificate management** - HTTPS setup is infrastructure concern

---

## Functional Requirements

### OllamaProvider Class

- FR-001: OllamaProvider MUST implement IModelProvider interface (Task 004)
- FR-002: OllamaProvider MUST be defined in the Infrastructure layer
- FR-003: OllamaProvider MUST be registered with the Provider Registry
- FR-004: OllamaProvider MUST accept configuration via dependency injection
- FR-005: OllamaProvider MUST use HttpClient for API communication
- FR-006: OllamaProvider MUST implement IAsyncDisposable for cleanup
- FR-007: OllamaProvider MUST log all API interactions with correlation IDs

### Configuration

- FR-008: Adapter MUST read endpoint from config (default http://localhost:11434)
- FR-009: Adapter MUST support connect timeout configuration (default 5s)
- FR-010: Adapter MUST support request timeout configuration (default 120s)
- FR-011: Adapter MUST support streaming timeout configuration (default 300s)
- FR-012: Adapter MUST support retry configuration (default 3 retries)
- FR-013: Adapter MUST support keep_alive configuration (default 5m)
- FR-014: Adapter MUST support environment variable overrides
- FR-015: Adapter MUST validate configuration on startup

### Chat Completion - Non-Streaming

- FR-016: CompleteAsync MUST call /api/chat endpoint
- FR-017: CompleteAsync MUST map ChatRequest to Ollama request format
- FR-018: CompleteAsync MUST set model from request or default
- FR-019: CompleteAsync MUST set stream: false for non-streaming
- FR-020: CompleteAsync MUST map messages to Ollama format
- FR-021: CompleteAsync MUST map tool definitions to Ollama format
- FR-022: CompleteAsync MUST include options (temperature, top_p, etc.)
- FR-023: CompleteAsync MUST parse Ollama response to ChatResponse
- FR-024: CompleteAsync MUST map finish reason from done_reason
- FR-025: CompleteAsync MUST extract usage from eval_count/prompt_eval_count
- FR-026: CompleteAsync MUST handle tool calls in response
- FR-027: CompleteAsync MUST timeout after configured duration
- FR-028: CompleteAsync MUST support cancellation via CancellationToken

### Chat Completion - Streaming

- FR-029: StreamAsync MUST return IAsyncEnumerable<ResponseDelta>
- FR-030: StreamAsync MUST call /api/chat with stream: true
- FR-031: StreamAsync MUST parse NDJSON stream incrementally
- FR-032: StreamAsync MUST yield ResponseDelta for each chunk
- FR-033: StreamAsync MUST accumulate content deltas correctly
- FR-034: StreamAsync MUST accumulate tool call deltas correctly
- FR-035: StreamAsync MUST detect final chunk (done: true)
- FR-036: StreamAsync MUST include usage in final delta
- FR-037: StreamAsync MUST support cancellation mid-stream
- FR-038: StreamAsync MUST cleanup HTTP stream on cancellation
- FR-039: StreamAsync MUST timeout on stalled streams

### Message Mapping

- FR-040: Adapter MUST map MessageRole.System to "system"
- FR-041: Adapter MUST map MessageRole.User to "user"
- FR-042: Adapter MUST map MessageRole.Assistant to "assistant"
- FR-043: Adapter MUST map MessageRole.Tool to "tool"
- FR-044: Adapter MUST include tool_calls in assistant messages
- FR-045: Adapter MUST include tool_call_id in tool messages
- FR-046: Adapter MUST preserve message ordering
- FR-047: Adapter MUST handle null content in messages

### Tool Calling

- FR-048: Adapter MUST map ToolDefinition to Ollama function format
- FR-049: Adapter MUST serialize JSON Schema parameters correctly
- FR-050: Adapter MUST parse tool_calls from response
- FR-051: Adapter MUST extract function name and arguments
- FR-052: Adapter MUST validate tool call JSON arguments
- FR-053: Adapter MUST retry on malformed tool call JSON (configurable)
- FR-054: Adapter MUST set FinishReason.ToolCalls when tools requested
- FR-055: Adapter MUST support multiple simultaneous tool calls

### JSON Mode

- FR-056: Adapter MUST support format: "json" option
- FR-057: Adapter MUST set response format when JSON mode requested
- FR-058: Adapter MUST validate response is valid JSON in JSON mode
- FR-059: Adapter MUST retry on invalid JSON in JSON mode

### Model Management

- FR-060: ListModelsAsync MUST call /api/tags endpoint
- FR-061: ListModelsAsync MUST parse model list response
- FR-062: ListModelsAsync MUST return model names with tags
- FR-063: GetModelInfoAsync MUST call /api/show endpoint
- FR-064: GetModelInfoAsync MUST extract context length
- FR-065: GetModelInfoAsync MUST extract supported features
- FR-066: Adapter MUST cache model info to reduce API calls

### Health Checking

- FR-067: CheckHealthAsync MUST call /api/tags endpoint
- FR-068: CheckHealthAsync MUST measure response time
- FR-069: CheckHealthAsync MUST return Healthy on success
- FR-070: CheckHealthAsync MUST return Unhealthy on failure
- FR-071: CheckHealthAsync MUST return Degraded on slow response (>2s)
- FR-072: CheckHealthAsync MUST timeout after 5 seconds
- FR-073: CheckHealthAsync MUST NOT throw exceptions

### Error Handling

- FR-074: Adapter MUST wrap network errors in OllamaConnectionException
- FR-075: Adapter MUST wrap timeout errors in OllamaTimeoutException
- FR-076: Adapter MUST wrap 4xx errors in OllamaRequestException
- FR-077: Adapter MUST wrap 5xx errors in OllamaServerException
- FR-078: Adapter MUST wrap parse errors in OllamaParseException
- FR-079: Adapter MUST include original exception as inner exception
- FR-080: Adapter MUST include request ID in exception data
- FR-081: Adapter MUST log all exceptions with full context

### Retry Logic

- FR-082: Adapter MUST retry on transient network errors
- FR-083: Adapter MUST retry on 503 Service Unavailable
- FR-084: Adapter MUST NOT retry on 4xx client errors (except 429)
- FR-085: Adapter MUST retry on 429 Too Many Requests with backoff
- FR-086: Adapter MUST implement exponential backoff
- FR-087: Adapter MUST respect retry count limit
- FR-088: Adapter MUST log each retry attempt
- FR-089: Adapter MUST throw after max retries exceeded

### Request/Response Types

- FR-090: OllamaRequest MUST be defined for API requests
- FR-091: OllamaResponse MUST be defined for API responses
- FR-092: OllamaMessage MUST map to/from ChatMessage
- FR-093: OllamaToolCall MUST map to/from ToolCall
- FR-094: OllamaUsage MUST map to UsageInfo
- FR-095: Types MUST use System.Text.Json source generators

---

## Non-Functional Requirements

### Performance

- NFR-001: Connection establishment MUST complete in < 100ms (local)
- NFR-002: Request serialization MUST complete in < 1ms
- NFR-003: Response parsing MUST complete in < 5ms
- NFR-004: Streaming delta parsing MUST complete in < 100μs
- NFR-005: Memory allocation per request MUST be < 10KB (excluding content)
- NFR-006: HttpClient MUST use connection pooling
- NFR-007: Adapter MUST NOT buffer entire streaming responses
- NFR-008: Adapter MUST release HTTP streams promptly

### Reliability

- NFR-009: Adapter MUST handle Ollama restarts gracefully
- NFR-010: Adapter MUST not crash on malformed responses
- NFR-011: Adapter MUST timeout stalled requests appropriately
- NFR-012: Adapter MUST cleanup resources on disposal
- NFR-013: Adapter MUST be thread-safe for concurrent requests
- NFR-014: Adapter MUST not leak file handles or connections

### Security

- NFR-015: Adapter MUST only connect to configured endpoints
- NFR-016: Adapter MUST NOT log request content at INFO level
- NFR-017: Adapter MUST sanitize error messages for logging
- NFR-018: Adapter MUST validate URLs are local in airgapped mode
- NFR-019: Adapter MUST NOT execute arbitrary code from responses

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

The Ollama Provider Adapter enables Acode to use Ollama as its inference backend. Ollama runs large language models locally on your machine, providing fast, private inference without cloud dependencies.

### Prerequisites

1. **Install Ollama**: Download from https://ollama.ai
2. **Pull a model**: `ollama pull llama3.2:8b`
3. **Verify Ollama is running**: `curl http://localhost:11434/api/tags`

### Quick Start

#### Basic Configuration

Configure Ollama in `.agent/config.yml`:

```yaml
model:
  default_provider: ollama
  
  providers:
    ollama:
      enabled: true
      endpoint: http://localhost:11434
      default_model: llama3.2:8b
```

#### Verify Connection

```
$ acode providers health
┌──────────────────────────────────────────────────────────┐
│ Provider Health                                           │
├─────────┬─────────┬──────────┬──────────────────────────┤
│ ollama  │ Healthy │ 45ms     │ llama3.2:8b loaded       │
└─────────┴─────────┴──────────┴──────────────────────────┘
```

#### Run Inference

```
$ acode ask "What is recursion?"
Recursion is a programming technique where a function calls itself...

Tokens: 25 prompt, 142 completion (167 total)
Speed: 38.4 tok/s | Model: llama3.2:8b
```

### Configuration Reference

#### Full Configuration Example

```yaml
model:
  default_provider: ollama
  
  providers:
    ollama:
      # Enable/disable this provider
      enabled: true
      
      # Ollama API endpoint
      endpoint: http://localhost:11434
      
      # Default model for requests without explicit model
      default_model: llama3.2:8b
      
      # Timeouts
      connect_timeout_seconds: 5
      request_timeout_seconds: 120
      streaming_timeout_seconds: 300
      
      # Retry configuration
      retry:
        max_retries: 3
        initial_delay_ms: 100
        max_delay_ms: 10000
        backoff_multiplier: 2.0
      
      # Model-specific settings
      models:
        llama3.2:8b:
          context_length: 8192
          supports_tools: true
        
        codellama:13b:
          context_length: 16384
          supports_tools: false
      
      # Default generation parameters
      options:
        temperature: 0.7
        top_p: 0.9
        seed: null  # null for random
        keep_alive: "5m"
```

#### Environment Variable Overrides

```bash
# Override endpoint
ACODE_MODEL_PROVIDERS_OLLAMA_ENDPOINT=http://192.168.1.100:11434

# Override default model
ACODE_MODEL_PROVIDERS_OLLAMA_DEFAULT_MODEL=codellama:34b

# Override timeout
ACODE_MODEL_PROVIDERS_OLLAMA_REQUEST_TIMEOUT_SECONDS=300
```

### Model Management

#### List Available Models

```
$ acode models list --provider ollama
┌────────────────────────────────────────────────────────────┐
│ Ollama Models                                               │
├─────────────────────┬───────────┬─────────┬────────────────┤
│ Name                │ Size      │ Context │ Tools          │
├─────────────────────┼───────────┼─────────┼────────────────┤
│ llama3.2:8b         │ 4.7 GB    │ 8192    │ Yes            │
│ codellama:13b       │ 7.4 GB    │ 16384   │ No             │
│ mistral:7b          │ 4.1 GB    │ 8192    │ Yes            │
└─────────────────────┴───────────┴─────────┴────────────────┘
```

#### Pull a Model

```bash
# Pull models via Ollama CLI
ollama pull llama3.2:8b
ollama pull codellama:13b
```

### Tool Calling

#### Example with Tools

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
                "path": { "type": "string", "description": "File path" }
            },
            "required": ["path"]
        }
        """).RootElement
    }
};

var request = new ChatRequest
{
    Model = "llama3.2:8b",
    Messages = messages,
    Tools = tools
};

var response = await provider.CompleteAsync(request);

if (response.HasToolCalls)
{
    foreach (var call in response.Message.ToolCalls!)
    {
        Console.WriteLine($"Tool: {call.Name}");
        Console.WriteLine($"Args: {call.Arguments}");
    }
}
```

### Streaming Responses

#### Basic Streaming

```csharp
await foreach (var delta in provider.StreamAsync(request))
{
    if (delta.ContentDelta is not null)
    {
        Console.Write(delta.ContentDelta);
    }
    
    if (delta.IsComplete)
    {
        Console.WriteLine($"\n[Done: {delta.FinishReason}]");
    }
}
```

### Performance Tuning

#### Optimize for Speed

```yaml
model:
  providers:
    ollama:
      options:
        # Lower temperature = faster (less sampling)
        temperature: 0.3
        # Smaller context = faster
        num_ctx: 4096
        # Keep model loaded
        keep_alive: "30m"
```

#### Optimize for Quality

```yaml
model:
  providers:
    ollama:
      options:
        temperature: 0.7
        top_p: 0.95
        repeat_penalty: 1.1
        num_ctx: 8192
```

### Troubleshooting

#### Ollama Not Responding

**Error:** `OllamaConnectionException: Unable to connect to Ollama`

**Diagnosis:**
```bash
# Check if Ollama is running
curl http://localhost:11434/api/tags

# Check Ollama status
ollama ps

# Check logs
journalctl -u ollama -f  # Linux
```

**Resolution:**
1. Start Ollama: `ollama serve`
2. Verify endpoint configuration
3. Check firewall rules

#### Model Not Found

**Error:** `OllamaRequestException: model 'xyz' not found`

**Resolution:**
```bash
# Pull the model
ollama pull xyz

# Verify model exists
ollama list
```

#### Slow Response Times

**Symptoms:** Tokens per second very low

**Diagnosis:**
1. Check GPU utilization
2. Check model quantization
3. Check context length

**Resolution:**
- Use smaller/quantized model
- Reduce context window
- Ensure GPU is being used

#### Out of Memory

**Error:** `OllamaServerException: out of memory`

**Resolution:**
1. Use smaller model
2. Use quantized model (q4_0)
3. Reduce num_ctx
4. Close other GPU applications

---

## Acceptance Criteria

### OllamaProvider Class

- [ ] AC-001: OllamaProvider implements IModelProvider
- [ ] AC-002: Located in Infrastructure layer
- [ ] AC-003: Registered with Provider Registry
- [ ] AC-004: Accepts configuration via DI
- [ ] AC-005: Uses HttpClient with pooling
- [ ] AC-006: Implements IAsyncDisposable
- [ ] AC-007: Logs with correlation IDs

### Configuration

- [ ] AC-008: Reads endpoint from config
- [ ] AC-009: Default endpoint is localhost:11434
- [ ] AC-010: Connect timeout configurable
- [ ] AC-011: Request timeout configurable
- [ ] AC-012: Streaming timeout configurable
- [ ] AC-013: Retry config supported
- [ ] AC-014: Keep alive configurable
- [ ] AC-015: Environment overrides work
- [ ] AC-016: Config validated on startup

### Non-Streaming Completion

- [ ] AC-017: Calls /api/chat endpoint
- [ ] AC-018: Maps ChatRequest correctly
- [ ] AC-019: Sets model from request
- [ ] AC-020: Sets stream: false
- [ ] AC-021: Maps messages correctly
- [ ] AC-022: Maps tool definitions
- [ ] AC-023: Includes generation options
- [ ] AC-024: Parses response to ChatResponse
- [ ] AC-025: Maps finish reason correctly
- [ ] AC-026: Extracts usage correctly
- [ ] AC-027: Handles tool calls in response
- [ ] AC-028: Respects timeout
- [ ] AC-029: Supports cancellation

### Streaming Completion

- [ ] AC-030: Returns IAsyncEnumerable
- [ ] AC-031: Sets stream: true
- [ ] AC-032: Parses NDJSON incrementally
- [ ] AC-033: Yields ResponseDelta per chunk
- [ ] AC-034: Accumulates content correctly
- [ ] AC-035: Accumulates tool calls correctly
- [ ] AC-036: Detects final chunk
- [ ] AC-037: Includes usage in final delta
- [ ] AC-038: Supports mid-stream cancellation
- [ ] AC-039: Cleans up HTTP stream
- [ ] AC-040: Timeouts on stalled streams

### Message Mapping

- [ ] AC-041: System role maps to "system"
- [ ] AC-042: User role maps to "user"
- [ ] AC-043: Assistant role maps to "assistant"
- [ ] AC-044: Tool role maps to "tool"
- [ ] AC-045: Tool calls included in assistant
- [ ] AC-046: Tool call ID included in tool
- [ ] AC-047: Message order preserved
- [ ] AC-048: Null content handled

### Tool Calling

- [ ] AC-049: Maps ToolDefinition correctly
- [ ] AC-050: Serializes JSON Schema
- [ ] AC-051: Parses tool_calls from response
- [ ] AC-052: Extracts function name
- [ ] AC-053: Validates tool call JSON
- [ ] AC-054: Retries on malformed JSON
- [ ] AC-055: Sets FinishReason.ToolCalls
- [ ] AC-056: Supports multiple tool calls

### JSON Mode

- [ ] AC-057: Supports format: json
- [ ] AC-058: Sets response format
- [ ] AC-059: Validates JSON response
- [ ] AC-060: Retries on invalid JSON

### Model Management

- [ ] AC-061: ListModelsAsync calls /api/tags
- [ ] AC-062: Parses model list
- [ ] AC-063: Returns names with tags
- [ ] AC-064: GetModelInfoAsync calls /api/show
- [ ] AC-065: Extracts context length
- [ ] AC-066: Caches model info

### Health Checking

- [ ] AC-067: CheckHealthAsync calls /api/tags
- [ ] AC-068: Measures response time
- [ ] AC-069: Returns Healthy on success
- [ ] AC-070: Returns Unhealthy on failure
- [ ] AC-071: Returns Degraded on slow (>2s)
- [ ] AC-072: Timeouts after 5s
- [ ] AC-073: Never throws exceptions

### Error Handling

- [ ] AC-074: Network errors wrapped
- [ ] AC-075: Timeout errors wrapped
- [ ] AC-076: 4xx errors wrapped
- [ ] AC-077: 5xx errors wrapped
- [ ] AC-078: Parse errors wrapped
- [ ] AC-079: Inner exception preserved
- [ ] AC-080: Request ID in exception
- [ ] AC-081: Errors logged fully

### Retry Logic

- [ ] AC-082: Retries transient network errors
- [ ] AC-083: Retries 503 errors
- [ ] AC-084: No retry on 4xx (except 429)
- [ ] AC-085: Retries 429 with backoff
- [ ] AC-086: Exponential backoff works
- [ ] AC-087: Retry limit respected
- [ ] AC-088: Retry attempts logged
- [ ] AC-089: Throws after max retries

### Performance

- [ ] AC-090: Connection < 100ms local
- [ ] AC-091: Serialization < 1ms
- [ ] AC-092: Response parsing < 5ms
- [ ] AC-093: Delta parsing < 100μs
- [ ] AC-094: Memory < 10KB per request
- [ ] AC-095: Connection pooling used
- [ ] AC-096: No streaming buffering
- [ ] AC-097: Prompt resource release

### Security

- [ ] AC-098: Only configured endpoints
- [ ] AC-099: No content at INFO level
- [ ] AC-100: Error messages sanitized
- [ ] AC-101: Local-only in airgapped
- [ ] AC-102: No code execution

### Documentation

- [ ] AC-103: XML docs complete
- [ ] AC-104: Config examples provided
- [ ] AC-105: Troubleshooting guide
- [ ] AC-106: Performance tuning guide

---

## Testing Requirements

### Unit Tests

```
Tests/Unit/Infrastructure/Ollama/
├── OllamaProviderTests.cs
│   ├── CompleteAsync_Should_Call_Chat_Endpoint()
│   ├── CompleteAsync_Should_Map_Request_Correctly()
│   ├── CompleteAsync_Should_Parse_Response()
│   ├── CompleteAsync_Should_Handle_ToolCalls()
│   ├── CompleteAsync_Should_Timeout()
│   ├── CompleteAsync_Should_Support_Cancellation()
│   ├── StreamAsync_Should_Return_Deltas()
│   ├── StreamAsync_Should_Parse_NDJSON()
│   ├── StreamAsync_Should_Accumulate_Content()
│   ├── StreamAsync_Should_Handle_Cancellation()
│   ├── CheckHealthAsync_Should_Return_Healthy()
│   ├── CheckHealthAsync_Should_Return_Unhealthy()
│   └── CheckHealthAsync_Should_Not_Throw()
│
├── OllamaMessageMapperTests.cs
│   ├── Should_Map_System_Role()
│   ├── Should_Map_User_Role()
│   ├── Should_Map_Assistant_Role()
│   ├── Should_Map_Tool_Role()
│   ├── Should_Include_ToolCalls()
│   └── Should_Preserve_Order()
│
├── OllamaToolMapperTests.cs
│   ├── Should_Map_ToolDefinition()
│   ├── Should_Serialize_JsonSchema()
│   ├── Should_Parse_ToolCalls()
│   └── Should_Validate_Arguments()
│
├── OllamaResponseParserTests.cs
│   ├── Should_Parse_Complete_Response()
│   ├── Should_Extract_Usage()
│   ├── Should_Map_FinishReason()
│   └── Should_Handle_Malformed_Response()
│
└── OllamaRetryPolicyTests.cs
    ├── Should_Retry_Transient_Errors()
    ├── Should_Not_Retry_4xx()
    ├── Should_Retry_429()
    ├── Should_Apply_Backoff()
    └── Should_Respect_MaxRetries()
```

### Integration Tests

```
Tests/Integration/Infrastructure/Ollama/
├── OllamaProviderIntegrationTests.cs
│   ├── Should_Complete_Request()
│   ├── Should_Stream_Response()
│   ├── Should_Handle_ToolCalls()
│   ├── Should_List_Models()
│   └── Should_Check_Health()
│
└── OllamaConfigurationTests.cs
    ├── Should_Load_From_Config()
    ├── Should_Apply_Env_Overrides()
    └── Should_Validate_Config()
```

### End-to-End Tests

```
Tests/E2E/Ollama/
├── OllamaE2ETests.cs
│   ├── Should_Complete_Multi_Turn_Conversation()
│   ├── Should_Execute_Tool_And_Continue()
│   ├── Should_Stream_Long_Response()
│   └── Should_Handle_Provider_Restart()
```

### Performance Tests

```
Tests/Performance/Ollama/
├── OllamaBenchmarks.cs
│   ├── Benchmark_Request_Serialization()
│   ├── Benchmark_Response_Parsing()
│   ├── Benchmark_Stream_Delta_Parsing()
│   ├── Benchmark_Connection_Reuse()
│   └── Benchmark_Concurrent_Requests()
```

---

## User Verification Steps

### Scenario 1: Basic Completion

1. Configure Ollama provider in config
2. Start Acode
3. Run `acode ask "Hello"`
4. Verify response received
5. Verify token counts displayed

### Scenario 2: Streaming Response

1. Run `acode ask "Write a poem" --stream`
2. Verify tokens appear incrementally
3. Verify final stats displayed

### Scenario 3: Tool Calling

1. Create request with tool definitions
2. Send to provider
3. Verify tool call in response
4. Verify arguments are valid JSON

### Scenario 4: Multi-Turn Conversation

1. Start conversation
2. Send multiple messages
3. Verify context maintained
4. Verify response coherent

### Scenario 5: Health Check

1. Run `acode providers health`
2. Verify Ollama shows Healthy
3. Stop Ollama
4. Run health check again
5. Verify shows Unhealthy

### Scenario 6: Model Listing

1. Pull multiple models in Ollama
2. Run `acode models list`
3. Verify all models shown

### Scenario 7: Timeout Handling

1. Configure short timeout (1s)
2. Send request requiring long generation
3. Verify timeout error raised
4. Verify error code is correct

### Scenario 8: Retry on Failure

1. Configure retry policy
2. Simulate transient failure
3. Verify retry occurs
4. Verify eventual success

### Scenario 9: Environment Override

1. Set ACODE_MODEL_PROVIDERS_OLLAMA_ENDPOINT
2. Start Acode
3. Verify new endpoint used

### Scenario 10: JSON Mode

1. Request with JSON format
2. Verify response is valid JSON
3. Verify can parse response

---

## Implementation Prompt

### File Structure

```
src/AgenticCoder.Infrastructure/Ollama/
├── OllamaProvider.cs
├── OllamaConfiguration.cs
├── OllamaHttpClient.cs
├── Mapping/
│   ├── OllamaMessageMapper.cs
│   ├── OllamaToolMapper.cs
│   └── OllamaResponseParser.cs
├── Models/
│   ├── OllamaRequest.cs
│   ├── OllamaResponse.cs
│   ├── OllamaMessage.cs
│   ├── OllamaToolCall.cs
│   └── OllamaStreamChunk.cs
├── Streaming/
│   └── OllamaStreamReader.cs
├── Health/
│   └── OllamaHealthChecker.cs
└── Exceptions/
    ├── OllamaException.cs
    ├── OllamaConnectionException.cs
    ├── OllamaTimeoutException.cs
    ├── OllamaRequestException.cs
    ├── OllamaServerException.cs
    └── OllamaParseException.cs
```

### OllamaProvider Implementation

```csharp
namespace AgenticCoder.Infrastructure.Ollama;

public sealed class OllamaProvider : IModelProvider, IAsyncDisposable
{
    private readonly OllamaHttpClient _client;
    private readonly OllamaConfiguration _config;
    private readonly ILogger<OllamaProvider> _logger;
    
    public string ProviderId => "ollama";
    public ProviderType Type => ProviderType.Ollama;
    
    public async Task<ChatResponse> CompleteAsync(
        ChatRequest request,
        CancellationToken cancellationToken = default)
    {
        var correlationId = Guid.NewGuid().ToString();
        using var scope = _logger.BeginScope(new { CorrelationId = correlationId });
        
        var ollamaRequest = OllamaMessageMapper.MapRequest(request, _config);
        ollamaRequest.Stream = false;
        
        _logger.LogDebug("Sending chat request to Ollama: {Model}", request.Model);
        
        var response = await _client.PostAsync<OllamaResponse>(
            "/api/chat",
            ollamaRequest,
            cancellationToken);
        
        return OllamaResponseParser.Parse(response, correlationId);
    }
    
    public async IAsyncEnumerable<ResponseDelta> StreamAsync(
        ChatRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var ollamaRequest = OllamaMessageMapper.MapRequest(request, _config);
        ollamaRequest.Stream = true;
        
        await using var stream = await _client.PostStreamAsync(
            "/api/chat",
            ollamaRequest,
            cancellationToken);
        
        await foreach (var chunk in OllamaStreamReader.ReadAsync(stream, cancellationToken))
        {
            yield return OllamaResponseParser.ParseDelta(chunk);
        }
    }
    
    // Additional implementation...
}
```

### Error Codes

| Code | Message |
|------|---------|
| ACODE-OLM-001 | Unable to connect to Ollama |
| ACODE-OLM-002 | Ollama request timeout |
| ACODE-OLM-003 | Model not found |
| ACODE-OLM-004 | Invalid request parameters |
| ACODE-OLM-005 | Ollama server error |
| ACODE-OLM-006 | Failed to parse response |
| ACODE-OLM-007 | Invalid tool call JSON |
| ACODE-OLM-008 | Streaming connection lost |
| ACODE-OLM-009 | Max retries exceeded |
| ACODE-OLM-010 | Health check failed |

### Implementation Checklist

1. [ ] Create OllamaConfiguration class
2. [ ] Create OllamaHttpClient with pooling
3. [ ] Implement OllamaMessageMapper
4. [ ] Implement OllamaToolMapper
5. [ ] Implement OllamaResponseParser
6. [ ] Implement OllamaStreamReader
7. [ ] Create OllamaProvider class
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
dotnet test --filter "FullyQualifiedName~Ollama"
```

---

**End of Task 005 Specification**
