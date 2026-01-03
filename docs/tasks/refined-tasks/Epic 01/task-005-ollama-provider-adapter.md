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

#### Unit Test Code Examples

```csharp
// OllamaProviderTests.cs
[TestClass]
public class OllamaProviderTests
{
    private Mock<IOllamaHttpClient> _mockClient;
    private Mock<ILogger<OllamaProvider>> _mockLogger;
    private OllamaProvider _provider;

    [TestInitialize]
    public void Setup()
    {
        _mockClient = new Mock<IOllamaHttpClient>();
        _mockLogger = new Mock<ILogger<OllamaProvider>>();
        var config = new OllamaConfiguration
        {
            Endpoint = "http://localhost:11434",
            DefaultModel = "llama3.2:8b",
            ConnectTimeoutSeconds = 5,
            RequestTimeoutSeconds = 120
        };
        _provider = new OllamaProvider(_mockClient.Object, config, _mockLogger.Object);
    }

    [TestMethod]
    public async Task CompleteAsync_Should_Call_Chat_Endpoint()
    {
        // Arrange
        var request = new ChatRequest
        {
            Model = "llama3.2:8b",
            Messages = new[] { new ChatMessage(MessageRole.User, "Hello") }
        };
        
        var ollamaResponse = new OllamaResponse
        {
            Message = new OllamaMessage { Role = "assistant", Content = "Hi there!" },
            Done = true,
            DoneReason = "stop",
            EvalCount = 10,
            PromptEvalCount = 5
        };
        
        _mockClient
            .Setup(c => c.PostAsync<OllamaResponse>(
                "/api/chat",
                It.IsAny<OllamaRequest>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(ollamaResponse);

        // Act
        var response = await _provider.CompleteAsync(request);

        // Assert
        Assert.IsNotNull(response);
        Assert.AreEqual("Hi there!", response.Message.Content);
        Assert.AreEqual(FinishReason.Stop, response.FinishReason);
        _mockClient.Verify(c => c.PostAsync<OllamaResponse>(
            "/api/chat",
            It.IsAny<OllamaRequest>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestMethod]
    public async Task CompleteAsync_Should_Handle_ToolCalls()
    {
        // Arrange
        var request = new ChatRequest
        {
            Model = "llama3.2:8b",
            Messages = new[] { new ChatMessage(MessageRole.User, "Read file.txt") },
            Tools = new[] { CreateReadFileTool() }
        };
        
        var ollamaResponse = new OllamaResponse
        {
            Message = new OllamaMessage 
            { 
                Role = "assistant", 
                Content = null,
                ToolCalls = new[]
                {
                    new OllamaToolCall
                    {
                        Function = new OllamaFunction
                        {
                            Name = "read_file",
                            Arguments = new { path = "file.txt" }
                        }
                    }
                }
            },
            Done = true,
            DoneReason = "tool_calls"
        };
        
        _mockClient
            .Setup(c => c.PostAsync<OllamaResponse>(
                "/api/chat",
                It.IsAny<OllamaRequest>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(ollamaResponse);

        // Act
        var response = await _provider.CompleteAsync(request);

        // Assert
        Assert.IsTrue(response.HasToolCalls);
        Assert.AreEqual(1, response.Message.ToolCalls.Count);
        Assert.AreEqual("read_file", response.Message.ToolCalls[0].Name);
        Assert.AreEqual(FinishReason.ToolCalls, response.FinishReason);
    }

    [TestMethod]
    public async Task CompleteAsync_Should_Timeout()
    {
        // Arrange
        var request = new ChatRequest
        {
            Model = "llama3.2:8b",
            Messages = new[] { new ChatMessage(MessageRole.User, "Hello") }
        };
        
        _mockClient
            .Setup(c => c.PostAsync<OllamaResponse>(
                "/api/chat",
                It.IsAny<OllamaRequest>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new TaskCanceledException("Request timed out"));

        // Act & Assert
        var ex = await Assert.ThrowsExceptionAsync<OllamaTimeoutException>(
            () => _provider.CompleteAsync(request));
        Assert.AreEqual("ACODE-OLM-002", ex.ErrorCode);
    }

    [TestMethod]
    public async Task StreamAsync_Should_Return_Deltas()
    {
        // Arrange
        var request = new ChatRequest
        {
            Model = "llama3.2:8b",
            Messages = new[] { new ChatMessage(MessageRole.User, "Count to 3") }
        };
        
        var chunks = new[]
        {
            new OllamaStreamChunk { Message = new OllamaMessage { Content = "1" }, Done = false },
            new OllamaStreamChunk { Message = new OllamaMessage { Content = " 2" }, Done = false },
            new OllamaStreamChunk { Message = new OllamaMessage { Content = " 3" }, Done = true, DoneReason = "stop" }
        };
        
        _mockClient
            .Setup(c => c.PostStreamAsync(
                "/api/chat",
                It.IsAny<OllamaRequest>(),
                It.IsAny<CancellationToken>()))
            .Returns(ToAsyncEnumerable(chunks));

        // Act
        var deltas = new List<ResponseDelta>();
        await foreach (var delta in _provider.StreamAsync(request))
        {
            deltas.Add(delta);
        }

        // Assert
        Assert.AreEqual(3, deltas.Count);
        Assert.AreEqual("1", deltas[0].ContentDelta);
        Assert.AreEqual(" 2", deltas[1].ContentDelta);
        Assert.AreEqual(" 3", deltas[2].ContentDelta);
        Assert.IsTrue(deltas[2].IsComplete);
    }

    [TestMethod]
    public async Task CheckHealthAsync_Should_Return_Healthy()
    {
        // Arrange
        _mockClient
            .Setup(c => c.GetAsync<OllamaTagsResponse>(
                "/api/tags",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OllamaTagsResponse { Models = new[] { new OllamaModel { Name = "llama3.2:8b" } } });

        // Act
        var health = await _provider.CheckHealthAsync();

        // Assert
        Assert.AreEqual(HealthStatus.Healthy, health.Status);
        Assert.IsTrue(health.ResponseTimeMs < 2000);
    }

    [TestMethod]
    public async Task CheckHealthAsync_Should_Not_Throw()
    {
        // Arrange
        _mockClient
            .Setup(c => c.GetAsync<OllamaTagsResponse>(
                "/api/tags",
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Connection refused"));

        // Act
        var health = await _provider.CheckHealthAsync();

        // Assert
        Assert.AreEqual(HealthStatus.Unhealthy, health.Status);
        Assert.AreEqual("Connection refused", health.Message);
    }
}

// OllamaMessageMapperTests.cs
[TestClass]
public class OllamaMessageMapperTests
{
    [TestMethod]
    public void Should_Map_System_Role()
    {
        // Arrange
        var message = new ChatMessage(MessageRole.System, "You are helpful.");

        // Act
        var ollamaMessage = OllamaMessageMapper.MapMessage(message);

        // Assert
        Assert.AreEqual("system", ollamaMessage.Role);
        Assert.AreEqual("You are helpful.", ollamaMessage.Content);
    }

    [TestMethod]
    public void Should_Map_All_Roles_Correctly()
    {
        // Arrange
        var testCases = new[]
        {
            (MessageRole.System, "system"),
            (MessageRole.User, "user"),
            (MessageRole.Assistant, "assistant"),
            (MessageRole.Tool, "tool")
        };

        // Act & Assert
        foreach (var (role, expectedOllamaRole) in testCases)
        {
            var message = new ChatMessage(role, "test");
            var ollamaMessage = OllamaMessageMapper.MapMessage(message);
            Assert.AreEqual(expectedOllamaRole, ollamaMessage.Role, $"Failed for role {role}");
        }
    }

    [TestMethod]
    public void Should_Include_ToolCalls_In_Assistant_Message()
    {
        // Arrange
        var toolCalls = new[]
        {
            new ToolCall("call_1", "read_file", JsonDocument.Parse("{\"path\":\"a.txt\"}").RootElement)
        };
        var message = new ChatMessage(MessageRole.Assistant, null, toolCalls);

        // Act
        var ollamaMessage = OllamaMessageMapper.MapMessage(message);

        // Assert
        Assert.IsNotNull(ollamaMessage.ToolCalls);
        Assert.AreEqual(1, ollamaMessage.ToolCalls.Length);
        Assert.AreEqual("read_file", ollamaMessage.ToolCalls[0].Function.Name);
    }

    [TestMethod]
    public void Should_Preserve_Message_Order()
    {
        // Arrange
        var messages = new[]
        {
            new ChatMessage(MessageRole.System, "System"),
            new ChatMessage(MessageRole.User, "User 1"),
            new ChatMessage(MessageRole.Assistant, "Assistant 1"),
            new ChatMessage(MessageRole.User, "User 2")
        };

        // Act
        var ollamaMessages = OllamaMessageMapper.MapMessages(messages);

        // Assert
        Assert.AreEqual(4, ollamaMessages.Length);
        Assert.AreEqual("system", ollamaMessages[0].Role);
        Assert.AreEqual("User 1", ollamaMessages[1].Content);
        Assert.AreEqual("Assistant 1", ollamaMessages[2].Content);
        Assert.AreEqual("User 2", ollamaMessages[3].Content);
    }
}

// OllamaRetryPolicyTests.cs
[TestClass]
public class OllamaRetryPolicyTests
{
    [TestMethod]
    public void Should_Retry_Transient_Network_Errors()
    {
        // Arrange
        var policy = new OllamaRetryPolicy(maxRetries: 3, initialDelayMs: 100);
        var exception = new HttpRequestException("Connection reset");

        // Act
        var shouldRetry = policy.ShouldRetry(exception, attemptNumber: 1);

        // Assert
        Assert.IsTrue(shouldRetry);
    }

    [TestMethod]
    public void Should_Not_Retry_4xx_Errors()
    {
        // Arrange
        var policy = new OllamaRetryPolicy(maxRetries: 3, initialDelayMs: 100);
        var exception = new OllamaRequestException("Bad request", HttpStatusCode.BadRequest);

        // Act
        var shouldRetry = policy.ShouldRetry(exception, attemptNumber: 1);

        // Assert
        Assert.IsFalse(shouldRetry);
    }

    [TestMethod]
    public void Should_Retry_429_With_Backoff()
    {
        // Arrange
        var policy = new OllamaRetryPolicy(maxRetries: 3, initialDelayMs: 100, backoffMultiplier: 2.0);
        var exception = new OllamaRequestException("Too many requests", HttpStatusCode.TooManyRequests);

        // Act
        var shouldRetry = policy.ShouldRetry(exception, attemptNumber: 1);
        var delay1 = policy.GetDelay(attemptNumber: 1);
        var delay2 = policy.GetDelay(attemptNumber: 2);
        var delay3 = policy.GetDelay(attemptNumber: 3);

        // Assert
        Assert.IsTrue(shouldRetry);
        Assert.AreEqual(100, delay1.TotalMilliseconds);
        Assert.AreEqual(200, delay2.TotalMilliseconds);
        Assert.AreEqual(400, delay3.TotalMilliseconds);
    }

    [TestMethod]
    public void Should_Respect_Max_Retries()
    {
        // Arrange
        var policy = new OllamaRetryPolicy(maxRetries: 3, initialDelayMs: 100);
        var exception = new HttpRequestException("Connection reset");

        // Act & Assert
        Assert.IsTrue(policy.ShouldRetry(exception, attemptNumber: 1));
        Assert.IsTrue(policy.ShouldRetry(exception, attemptNumber: 2));
        Assert.IsTrue(policy.ShouldRetry(exception, attemptNumber: 3));
        Assert.IsFalse(policy.ShouldRetry(exception, attemptNumber: 4));
    }
}
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

#### Integration Test Code Examples

```csharp
// OllamaProviderIntegrationTests.cs
[TestClass]
[TestCategory("Integration")]
[TestCategory("RequiresOllama")]
public class OllamaProviderIntegrationTests : IDisposable
{
    private OllamaProvider _provider;
    private IServiceProvider _services;

    [TestInitialize]
    public void Setup()
    {
        // Skip if Ollama is not available
        if (!IsOllamaAvailable())
        {
            Assert.Inconclusive("Ollama is not running - skipping integration tests");
        }

        var services = new ServiceCollection();
        services.AddLogging(b => b.AddConsole());
        services.AddSingleton(new OllamaConfiguration
        {
            Endpoint = "http://localhost:11434",
            DefaultModel = "llama3.2:8b",
            ConnectTimeoutSeconds = 5,
            RequestTimeoutSeconds = 120
        });
        services.AddHttpClient<IOllamaHttpClient, OllamaHttpClient>();
        services.AddScoped<OllamaProvider>();
        
        _services = services.BuildServiceProvider();
        _provider = _services.GetRequiredService<OllamaProvider>();
    }

    [TestMethod]
    public async Task Should_Complete_Request_Against_Live_Ollama()
    {
        // Arrange
        var request = new ChatRequest
        {
            Model = "llama3.2:8b",
            Messages = new[]
            {
                new ChatMessage(MessageRole.System, "You are a helpful assistant. Be concise."),
                new ChatMessage(MessageRole.User, "What is 2+2? Reply with just the number.")
            },
            Options = new GenerationOptions { Temperature = 0.1f, MaxTokens = 10 }
        };

        // Act
        var response = await _provider.CompleteAsync(request);

        // Assert
        Assert.IsNotNull(response);
        Assert.IsNotNull(response.Message);
        Assert.AreEqual(MessageRole.Assistant, response.Message.Role);
        Assert.IsNotNull(response.Message.Content);
        Assert.IsTrue(response.Message.Content.Contains("4"));
        Assert.AreEqual(FinishReason.Stop, response.FinishReason);
        Assert.IsNotNull(response.Usage);
        Assert.IsTrue(response.Usage.TotalTokens > 0);
    }

    [TestMethod]
    public async Task Should_Stream_Response_With_Deltas()
    {
        // Arrange
        var request = new ChatRequest
        {
            Model = "llama3.2:8b",
            Messages = new[]
            {
                new ChatMessage(MessageRole.User, "Count from 1 to 5, one number per line.")
            },
            Options = new GenerationOptions { Temperature = 0.1f, MaxTokens = 50 }
        };

        // Act
        var deltas = new List<ResponseDelta>();
        var contentBuilder = new StringBuilder();
        
        await foreach (var delta in _provider.StreamAsync(request))
        {
            deltas.Add(delta);
            if (delta.ContentDelta is not null)
            {
                contentBuilder.Append(delta.ContentDelta);
            }
        }

        // Assert
        Assert.IsTrue(deltas.Count > 1, "Should receive multiple streaming deltas");
        Assert.IsTrue(deltas.Last().IsComplete, "Last delta should be complete");
        
        var fullContent = contentBuilder.ToString();
        Assert.IsTrue(fullContent.Contains("1"));
        Assert.IsTrue(fullContent.Contains("5"));
    }

    [TestMethod]
    public async Task Should_Handle_ToolCalls_When_Model_Supports_Tools()
    {
        // Arrange
        var tools = new[]
        {
            new ToolDefinition
            {
                Name = "get_weather",
                Description = "Get current weather for a location",
                Parameters = JsonDocument.Parse("""
                {
                    "type": "object",
                    "properties": {
                        "location": { "type": "string", "description": "City name" }
                    },
                    "required": ["location"]
                }
                """).RootElement
            }
        };

        var request = new ChatRequest
        {
            Model = "llama3.2:8b",
            Messages = new[]
            {
                new ChatMessage(MessageRole.User, "What's the weather in Seattle?")
            },
            Tools = tools,
            Options = new GenerationOptions { Temperature = 0.1f }
        };

        // Act
        var response = await _provider.CompleteAsync(request);

        // Assert
        // Note: Tool calling behavior depends on model capability
        Assert.IsNotNull(response);
        if (response.HasToolCalls)
        {
            Assert.AreEqual(FinishReason.ToolCalls, response.FinishReason);
            var toolCall = response.Message.ToolCalls!.First();
            Assert.AreEqual("get_weather", toolCall.Name);
            Assert.IsNotNull(toolCall.Arguments);
        }
    }

    [TestMethod]
    public async Task Should_List_Available_Models()
    {
        // Act
        var models = await _provider.ListModelsAsync();

        // Assert
        Assert.IsNotNull(models);
        Assert.IsTrue(models.Any(), "Should have at least one model");
        
        var model = models.First();
        Assert.IsNotNull(model.Name);
        Assert.IsTrue(model.ContextLength > 0);
    }

    [TestMethod]
    public async Task Should_Return_Healthy_Status()
    {
        // Act
        var health = await _provider.CheckHealthAsync();

        // Assert
        Assert.AreEqual(HealthStatus.Healthy, health.Status);
        Assert.IsTrue(health.ResponseTimeMs > 0);
        Assert.IsTrue(health.ResponseTimeMs < 5000);
    }

    private static bool IsOllamaAvailable()
    {
        try
        {
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(2) };
            var response = client.GetAsync("http://localhost:11434/api/tags").Result;
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public void Dispose()
    {
        (_services as IDisposable)?.Dispose();
    }
}

// OllamaConfigurationTests.cs
[TestClass]
[TestCategory("Integration")]
public class OllamaConfigurationTests
{
    [TestMethod]
    public void Should_Load_Configuration_From_AgentConfig()
    {
        // Arrange
        var configYaml = """
            model:
              default_provider: ollama
              providers:
                ollama:
                  enabled: true
                  endpoint: http://localhost:11434
                  default_model: codellama:13b
                  connect_timeout_seconds: 10
                  request_timeout_seconds: 180
            """;
        
        var tempPath = Path.Combine(Path.GetTempPath(), ".agent", "config.yml");
        Directory.CreateDirectory(Path.GetDirectoryName(tempPath)!);
        File.WriteAllText(tempPath, configYaml);

        try
        {
            // Act
            var loader = new AgentConfigLoader();
            var config = loader.LoadFromPath(Path.GetDirectoryName(tempPath)!);
            var ollamaConfig = config.GetOllamaConfiguration();

            // Assert
            Assert.AreEqual("http://localhost:11434", ollamaConfig.Endpoint);
            Assert.AreEqual("codellama:13b", ollamaConfig.DefaultModel);
            Assert.AreEqual(10, ollamaConfig.ConnectTimeoutSeconds);
            Assert.AreEqual(180, ollamaConfig.RequestTimeoutSeconds);
        }
        finally
        {
            File.Delete(tempPath);
        }
    }

    [TestMethod]
    public void Should_Apply_Environment_Variable_Overrides()
    {
        // Arrange
        Environment.SetEnvironmentVariable("ACODE_MODEL_PROVIDERS_OLLAMA_ENDPOINT", "http://192.168.1.100:11434");
        Environment.SetEnvironmentVariable("ACODE_MODEL_PROVIDERS_OLLAMA_DEFAULT_MODEL", "mistral:7b");

        try
        {
            // Act
            var configBuilder = new OllamaConfigurationBuilder();
            var config = configBuilder
                .WithDefaults()
                .ApplyEnvironmentOverrides()
                .Build();

            // Assert
            Assert.AreEqual("http://192.168.1.100:11434", config.Endpoint);
            Assert.AreEqual("mistral:7b", config.DefaultModel);
        }
        finally
        {
            Environment.SetEnvironmentVariable("ACODE_MODEL_PROVIDERS_OLLAMA_ENDPOINT", null);
            Environment.SetEnvironmentVariable("ACODE_MODEL_PROVIDERS_OLLAMA_DEFAULT_MODEL", null);
        }
    }

    [TestMethod]
    public void Should_Validate_Invalid_Configuration()
    {
        // Arrange
        var config = new OllamaConfiguration
        {
            Endpoint = "not-a-valid-url",
            DefaultModel = "",
            ConnectTimeoutSeconds = -1
        };

        // Act & Assert
        var validator = new OllamaConfigurationValidator();
        var result = validator.Validate(config);
        
        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(e => e.Contains("Endpoint")));
        Assert.IsTrue(result.Errors.Any(e => e.Contains("DefaultModel")));
        Assert.IsTrue(result.Errors.Any(e => e.Contains("ConnectTimeoutSeconds")));
    }
}
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

#### E2E Test Scenarios

```gherkin
Feature: Ollama Provider End-to-End

  Background:
    Given Ollama is running on localhost:11434
    And model "llama3.2:8b" is available
    And Acode is configured with Ollama as default provider

  @Critical
  Scenario: Multi-Turn Conversation with Context Retention
    When I start a new conversation
    And I send message "My name is Alice"
    And I receive a response acknowledging my name
    And I send message "What is my name?"
    Then the response MUST contain "Alice"
    And both messages MUST appear in conversation history
    And token count MUST be logged for each turn

  @Critical
  Scenario: Tool Execution and Continuation
    Given I have defined a "read_file" tool
    When I send message "Read the contents of README.md"
    Then the response MUST contain a tool call for "read_file"
    And tool call arguments MUST include path "README.md"
    When I provide tool result "# Project\nThis is the readme."
    And I request continuation
    Then the response MUST reference the readme content
    And the response MUST be coherent with tool result

  @Performance
  Scenario: Streaming Long Response Without Timeout
    When I send message "Write a 500 word essay about programming"
    And I enable streaming mode
    Then I MUST receive first token within 2 seconds
    And tokens MUST continue arriving without gaps > 5 seconds
    And final response MUST be at least 400 words
    And streaming MUST complete with proper finish reason

  @Resilience
  Scenario: Handle Provider Restart During Conversation
    Given I have an active conversation with 3 messages
    When Ollama service is restarted
    And I send a new message
    Then Acode MUST detect connection failure
    And Acode MUST retry the request
    And request MUST eventually succeed
    And conversation context MUST be preserved
```

```csharp
// OllamaE2ETests.cs
[TestClass]
[TestCategory("E2E")]
[TestCategory("RequiresOllama")]
public class OllamaE2ETests : E2ETestBase
{
    [TestMethod]
    public async Task Should_Complete_Multi_Turn_Conversation_With_Context()
    {
        // Arrange
        var conversation = StartNewConversation();

        // Act - Turn 1
        var response1 = await conversation.SendMessageAsync("My name is Alice.");
        Assert.IsNotNull(response1.Content);
        
        // Act - Turn 2
        var response2 = await conversation.SendMessageAsync("What is my name?");
        
        // Assert
        Assert.IsTrue(
            response2.Content!.Contains("Alice", StringComparison.OrdinalIgnoreCase),
            $"Expected response to contain 'Alice', got: {response2.Content}");
        Assert.AreEqual(4, conversation.MessageCount); // System + 2 user + 2 assistant
    }

    [TestMethod]
    public async Task Should_Execute_Tool_And_Continue_Conversation()
    {
        // Arrange
        var readFileTool = CreateReadFileTool();
        var conversation = StartNewConversation(tools: new[] { readFileTool });

        // Act - Request that triggers tool
        var response1 = await conversation.SendMessageAsync("What's in the README.md file?");
        
        // Assert - Tool call received
        Assert.IsTrue(response1.HasToolCalls);
        var toolCall = response1.ToolCalls!.Single();
        Assert.AreEqual("read_file", toolCall.Name);

        // Act - Provide tool result and continue
        conversation.AddToolResult(toolCall.Id, "# My Project\nA sample project for testing.");
        var response2 = await conversation.ContinueAsync();

        // Assert - Response incorporates tool result
        Assert.IsNotNull(response2.Content);
        Assert.IsTrue(
            response2.Content.Contains("project", StringComparison.OrdinalIgnoreCase) ||
            response2.Content.Contains("sample", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    [Timeout(60000)] // 60 second timeout
    public async Task Should_Stream_Long_Response_Continuously()
    {
        // Arrange
        var conversation = StartNewConversation();
        var stopwatch = Stopwatch.StartNew();
        var tokenCount = 0;
        var lastTokenTime = stopwatch.Elapsed;
        var maxGap = TimeSpan.Zero;

        // Act
        await foreach (var delta in conversation.StreamMessageAsync(
            "Write a detailed 500 word essay about the history of computing."))
        {
            if (delta.ContentDelta is not null)
            {
                tokenCount++;
                var gap = stopwatch.Elapsed - lastTokenTime;
                if (gap > maxGap) maxGap = gap;
                lastTokenTime = stopwatch.Elapsed;
                
                // First token should arrive within 2 seconds
                if (tokenCount == 1)
                {
                    Assert.IsTrue(
                        stopwatch.Elapsed < TimeSpan.FromSeconds(2),
                        $"First token took {stopwatch.Elapsed.TotalSeconds}s");
                }
            }
        }

        // Assert
        Assert.IsTrue(tokenCount > 300, $"Expected >300 tokens, got {tokenCount}");
        Assert.IsTrue(
            maxGap < TimeSpan.FromSeconds(5),
            $"Max gap between tokens was {maxGap.TotalSeconds}s");
    }
}
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

#### Performance Benchmark Code

```csharp
// OllamaBenchmarks.cs
[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net80)]
public class OllamaBenchmarks
{
    private OllamaMessageMapper _messageMapper;
    private OllamaResponseParser _responseParser;
    private OllamaStreamReader _streamReader;
    private string _sampleResponse;
    private string _sampleStreamChunk;
    private ChatRequest _sampleRequest;

    [GlobalSetup]
    public void Setup()
    {
        _messageMapper = new OllamaMessageMapper();
        _responseParser = new OllamaResponseParser();
        _streamReader = new OllamaStreamReader();
        
        _sampleRequest = new ChatRequest
        {
            Model = "llama3.2:8b",
            Messages = Enumerable.Range(0, 10).Select(i => 
                new ChatMessage(
                    i % 2 == 0 ? MessageRole.User : MessageRole.Assistant,
                    $"Message content {i} with some text to make it realistic."))
                .ToArray(),
            Tools = new[]
            {
                new ToolDefinition
                {
                    Name = "read_file",
                    Description = "Read file contents",
                    Parameters = JsonDocument.Parse("{\"type\":\"object\"}").RootElement
                }
            }
        };
        
        _sampleResponse = """
            {
                "model": "llama3.2:8b",
                "message": {
                    "role": "assistant",
                    "content": "This is a sample response with enough content to be realistic for benchmarking purposes."
                },
                "done": true,
                "done_reason": "stop",
                "eval_count": 25,
                "prompt_eval_count": 150,
                "total_duration": 1234567890,
                "load_duration": 123456789,
                "eval_duration": 987654321
            }
            """;
        
        _sampleStreamChunk = """
            {"model":"llama3.2:8b","message":{"role":"assistant","content":"token"},"done":false}
            """;
    }

    [Benchmark]
    public OllamaRequest Benchmark_Request_Serialization()
    {
        return _messageMapper.MapRequest(_sampleRequest, new OllamaConfiguration());
    }

    [Benchmark]
    public ChatResponse Benchmark_Response_Parsing()
    {
        return _responseParser.Parse(_sampleResponse, "correlation-id");
    }

    [Benchmark]
    public ResponseDelta Benchmark_Stream_Delta_Parsing()
    {
        return _streamReader.ParseChunk(_sampleStreamChunk);
    }

    [Benchmark]
    public async Task<HttpResponseMessage> Benchmark_Connection_Reuse()
    {
        // This benchmark tests HttpClient connection pooling
        using var client = new HttpClient();
        var responses = new List<HttpResponseMessage>();
        
        for (int i = 0; i < 10; i++)
        {
            var response = await client.GetAsync("http://localhost:11434/api/tags");
            responses.Add(response);
        }
        
        return responses.Last();
    }

    [Benchmark]
    [Arguments(1)]
    [Arguments(5)]
    [Arguments(10)]
    public async Task Benchmark_Concurrent_Requests(int concurrency)
    {
        var tasks = Enumerable.Range(0, concurrency)
            .Select(_ => SendSimpleRequestAsync())
            .ToArray();
        
        await Task.WhenAll(tasks);
    }

    private async Task SendSimpleRequestAsync()
    {
        using var client = new HttpClient();
        await client.GetAsync("http://localhost:11434/api/tags");
    }
}
```

#### Performance Targets

| Metric | Target | Critical Threshold |
|--------|--------|-------------------|
| Request Serialization | < 1ms | < 5ms |
| Response Parsing | < 5ms | < 20ms |
| Stream Delta Parsing | < 100μs | < 500μs |
| Connection Establishment (local) | < 100ms | < 500ms |
| Memory per Request (excluding content) | < 10KB | < 50KB |
| 10 Concurrent Requests | < 200ms total | < 1s total |

### Regression Testing

The following areas MUST have regression tests to prevent regressions when modifying Ollama adapter:

| Area | Test Coverage | Impacted By |
|------|---------------|-------------|
| Message Mapping | All role types, tool calls, null content | Changes to ChatMessage types |
| Response Parsing | All finish reasons, usage extraction | Changes to ChatResponse types |
| Streaming | NDJSON parsing, accumulation, cancellation | Changes to streaming infrastructure |
| Error Handling | All exception types, error codes | Changes to exception hierarchy |
| Retry Logic | All retry conditions, backoff calculation | Changes to retry policy |
| Health Check | All health statuses, timeout handling | Changes to health check interface |
| Configuration | Loading, validation, environment overrides | Changes to config schema |

---

## User Verification Steps

### Scenario 1: Basic Completion Request
1. Configure Ollama provider in `.agent/config.yml` with `default_provider: ollama`
2. Ensure Ollama is running with `llama3.2:8b` model available
3. Run command: `acode ask "What is 2 plus 2?"`
4. **Verify:** Response contains the number 4
5. **Verify:** Token counts are displayed (prompt + completion)
6. **Verify:** Response time is displayed in milliseconds
7. **Verify:** No error messages appear

### Scenario 2: Streaming Response Display
1. Run command: `acode ask "Write a haiku about coding" --stream`
2. **Verify:** Tokens appear incrementally on screen as they're generated
3. **Verify:** There is no long pause before text appears (< 2s to first token)
4. **Verify:** Final statistics appear after streaming completes
5. **Verify:** Stream can be interrupted with Ctrl+C cleanly

### Scenario 3: Tool Calling Integration
1. Create a request with tool definitions via API:
   ```csharp
   var tools = new[] { new ToolDefinition { Name = "read_file", ... } };
   var response = await provider.CompleteAsync(new ChatRequest { Tools = tools, ... });
   ```
2. **Verify:** Response includes `FinishReason.ToolCalls` when model chooses to call tool
3. **Verify:** `response.Message.ToolCalls` contains valid tool call(s)
4. **Verify:** Each tool call has valid JSON arguments matching schema
5. **Verify:** Tool call IDs are unique and non-empty

### Scenario 4: Multi-Turn Conversation Continuity
1. Start interactive conversation: `acode chat`
2. Enter: "Remember that my favorite color is blue"
3. **Verify:** Response acknowledges the information
4. Enter: "What is my favorite color?"
5. **Verify:** Response correctly recalls "blue"
6. **Verify:** All messages appear in conversation history

### Scenario 5: Provider Health Check
1. Run command: `acode providers health`
2. **Verify:** Ollama provider shows status "Healthy" with green indicator
3. **Verify:** Response time in milliseconds is displayed
4. Stop Ollama service: `systemctl stop ollama` or equivalent
5. Run health check again
6. **Verify:** Ollama provider shows status "Unhealthy" with red indicator
7. **Verify:** Error message indicates connection failure
8. Restart Ollama service
9. **Verify:** Health returns to "Healthy" on next check

### Scenario 6: Model Listing and Information
1. Ensure multiple models are pulled in Ollama (`llama3.2:8b`, `codellama:13b`)
2. Run command: `acode models list --provider ollama`
3. **Verify:** All pulled models are listed
4. **Verify:** Model sizes are displayed
5. **Verify:** Context lengths are displayed
6. **Verify:** Tool support indication is shown

### Scenario 7: Request Timeout Handling
1. Configure short timeout: `request_timeout_seconds: 1` in config
2. Send a request requiring long generation: `acode ask "Write a 1000 word essay"`
3. **Verify:** Request fails with timeout error after ~1 second
4. **Verify:** Error message includes error code `ACODE-OLM-002`
5. **Verify:** Error is logged with full context
6. **Verify:** No hanging connections or resource leaks

### Scenario 8: Retry Behavior on Transient Failure
1. Configure retry policy with 3 retries
2. Simulate network glitch (brief network interruption)
3. Send a request during the glitch
4. **Verify:** Request is automatically retried
5. **Verify:** Retry attempts are logged with attempt number
6. **Verify:** Request eventually succeeds after network recovers
7. **Verify:** Final response is correct

### Scenario 9: Environment Variable Override
1. Set environment variable: `ACODE_MODEL_PROVIDERS_OLLAMA_DEFAULT_MODEL=codellama:13b`
2. Start Acode without specifying model in config
3. Run: `acode ask "Hello"`
4. **Verify:** Request is sent to `codellama:13b` (not config default)
5. **Verify:** Response shows correct model name

### Scenario 10: JSON Mode Output
1. Request with JSON format specified:
   ```csharp
   var request = new ChatRequest { Format = OutputFormat.Json, ... };
   ```
2. **Verify:** Response content is valid JSON (parseable)
3. **Verify:** Invalid JSON triggers retry (if configured)
4. **Verify:** Final response is always valid JSON or error

### Scenario 11: Large Context Window Handling
1. Build conversation with ~4000 tokens of context
2. Send additional request
3. **Verify:** Request succeeds without context overflow error
4. **Verify:** Model responds coherently with context
5. **Verify:** Token counts reflect full context size

### Scenario 12: Concurrent Request Handling
1. Send 5 concurrent requests programmatically
2. **Verify:** All requests complete successfully
3. **Verify:** Responses are correct (not mixed up)
4. **Verify:** Connection pooling is used (check logs)
5. **Verify:** No thread safety issues or race conditions

### Scenario 13: Airgapped Mode Compliance
1. Configure airgapped operating mode in `.agent/config.yml`
2. Configure Ollama endpoint to localhost
3. **Verify:** Requests succeed normally
4. Attempt to configure non-local endpoint
5. **Verify:** Configuration validation fails with clear error
6. **Verify:** No requests are sent to non-local addresses

### Scenario 14: Graceful Degradation
1. Start with healthy Ollama provider
2. Stop Ollama mid-conversation
3. Attempt to send message
4. **Verify:** Error is returned (not crash)
5. **Verify:** Error message is user-friendly
6. **Verify:** Acode remains responsive
7. Restart Ollama
8. **Verify:** Next request succeeds without restart

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
├── Retry/
│   ├── OllamaRetryPolicy.cs
│   └── OllamaRetryHandler.cs
└── Exceptions/
    ├── OllamaException.cs
    ├── OllamaConnectionException.cs
    ├── OllamaTimeoutException.cs
    ├── OllamaRequestException.cs
    ├── OllamaServerException.cs
    └── OllamaParseException.cs
```

### Interface Contracts

```csharp
// IOllamaHttpClient.cs - HTTP communication contract
namespace AgenticCoder.Infrastructure.Ollama;

public interface IOllamaHttpClient : IAsyncDisposable
{
    /// <summary>
    /// Sends a POST request and deserializes the response.
    /// </summary>
    Task<TResponse> PostAsync<TResponse>(
        string path,
        object request,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Sends a POST request and returns a stream for NDJSON parsing.
    /// </summary>
    IAsyncEnumerable<string> PostStreamAsync(
        string path,
        object request,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Sends a GET request and deserializes the response.
    /// </summary>
    Task<TResponse> GetAsync<TResponse>(
        string path,
        CancellationToken cancellationToken = default);
}

// IOllamaRetryPolicy.cs - Retry policy contract
public interface IOllamaRetryPolicy
{
    /// <summary>
    /// Determines if the exception warrants a retry.
    /// </summary>
    bool ShouldRetry(Exception exception, int attemptNumber);
    
    /// <summary>
    /// Gets the delay before the next retry attempt.
    /// </summary>
    TimeSpan GetDelay(int attemptNumber);
    
    /// <summary>
    /// Maximum number of retry attempts.
    /// </summary>
    int MaxRetries { get; }
}

// IOllamaHealthChecker.cs - Health check contract
public interface IOllamaHealthChecker
{
    /// <summary>
    /// Performs a health check against Ollama.
    /// MUST NOT throw exceptions - always returns HealthCheckResult.
    /// </summary>
    Task<HealthCheckResult> CheckAsync(CancellationToken cancellationToken = default);
}
```

### OllamaProvider Full Implementation

```csharp
namespace AgenticCoder.Infrastructure.Ollama;

public sealed class OllamaProvider : IModelProvider, IAsyncDisposable
{
    private readonly IOllamaHttpClient _client;
    private readonly OllamaConfiguration _config;
    private readonly IOllamaRetryPolicy _retryPolicy;
    private readonly IOllamaHealthChecker _healthChecker;
    private readonly ILogger<OllamaProvider> _logger;
    private readonly ConcurrentDictionary<string, ModelInfo> _modelInfoCache = new();
    
    public string ProviderId => "ollama";
    public ProviderType Type => ProviderType.Ollama;
    
    public OllamaProvider(
        IOllamaHttpClient client,
        OllamaConfiguration config,
        IOllamaRetryPolicy retryPolicy,
        IOllamaHealthChecker healthChecker,
        ILogger<OllamaProvider> logger)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _retryPolicy = retryPolicy ?? throw new ArgumentNullException(nameof(retryPolicy));
        _healthChecker = healthChecker ?? throw new ArgumentNullException(nameof(healthChecker));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }
    
    public async Task<ChatResponse> CompleteAsync(
        ChatRequest request,
        CancellationToken cancellationToken = default)
    {
        var correlationId = Guid.NewGuid().ToString();
        using var scope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["CorrelationId"] = correlationId,
            ["Provider"] = ProviderId,
            ["Model"] = request.Model ?? _config.DefaultModel
        });
        
        var ollamaRequest = OllamaMessageMapper.MapRequest(request, _config);
        ollamaRequest.Stream = false;
        
        _logger.LogDebug(
            "Sending chat completion request. MessageCount={MessageCount}, HasTools={HasTools}",
            request.Messages.Length,
            request.Tools?.Length > 0);
        
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            var response = await ExecuteWithRetryAsync(
                () => _client.PostAsync<OllamaResponse>("/api/chat", ollamaRequest, cancellationToken),
                cancellationToken);
            
            stopwatch.Stop();
            
            var chatResponse = OllamaResponseParser.Parse(response, correlationId);
            
            _logger.LogInformation(
                "Chat completion succeeded. Duration={Duration}ms, PromptTokens={PromptTokens}, CompletionTokens={CompletionTokens}, FinishReason={FinishReason}",
                stopwatch.ElapsedMilliseconds,
                chatResponse.Usage?.PromptTokens,
                chatResponse.Usage?.CompletionTokens,
                chatResponse.FinishReason);
            
            return chatResponse;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            stopwatch.Stop();
            _logger.LogError(ex,
                "Chat completion failed. Duration={Duration}ms, ErrorCode={ErrorCode}",
                stopwatch.ElapsedMilliseconds,
                (ex as OllamaException)?.ErrorCode ?? "UNKNOWN");
            throw;
        }
    }
    
    public async IAsyncEnumerable<ResponseDelta> StreamAsync(
        ChatRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var correlationId = Guid.NewGuid().ToString();
        using var scope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["CorrelationId"] = correlationId,
            ["Provider"] = ProviderId,
            ["Model"] = request.Model ?? _config.DefaultModel,
            ["Streaming"] = true
        });
        
        var ollamaRequest = OllamaMessageMapper.MapRequest(request, _config);
        ollamaRequest.Stream = true;
        
        _logger.LogDebug("Starting streaming chat completion");
        
        var stopwatch = Stopwatch.StartNew();
        var deltaCount = 0;
        var totalTokens = 0;
        
        IAsyncEnumerable<string> stream;
        try
        {
            stream = _client.PostStreamAsync("/api/chat", ollamaRequest, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initiate streaming connection");
            throw WrapException(ex);
        }
        
        await foreach (var line in stream.WithCancellation(cancellationToken))
        {
            ResponseDelta delta;
            try
            {
                var chunk = JsonSerializer.Deserialize<OllamaStreamChunk>(line);
                delta = OllamaResponseParser.ParseDelta(chunk!, correlationId);
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Failed to parse stream chunk: {Line}", line);
                continue;
            }
            
            deltaCount++;
            if (delta.IsComplete && delta.Usage is not null)
            {
                totalTokens = delta.Usage.TotalTokens;
            }
            
            yield return delta;
        }
        
        stopwatch.Stop();
        _logger.LogInformation(
            "Streaming completed. Duration={Duration}ms, Deltas={DeltaCount}, TotalTokens={TotalTokens}",
            stopwatch.ElapsedMilliseconds,
            deltaCount,
            totalTokens);
    }
    
    public async Task<IReadOnlyList<ModelInfo>> ListModelsAsync(
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Listing available models");
        
        var response = await _client.GetAsync<OllamaTagsResponse>("/api/tags", cancellationToken);
        
        var models = new List<ModelInfo>();
        foreach (var model in response.Models)
        {
            var info = await GetModelInfoAsync(model.Name, cancellationToken);
            models.Add(info);
        }
        
        _logger.LogInformation("Found {ModelCount} models", models.Count);
        return models;
    }
    
    public async Task<ModelInfo> GetModelInfoAsync(
        string modelName,
        CancellationToken cancellationToken = default)
    {
        if (_modelInfoCache.TryGetValue(modelName, out var cached))
        {
            return cached;
        }
        
        var request = new { name = modelName };
        var response = await _client.PostAsync<OllamaShowResponse>("/api/show", request, cancellationToken);
        
        var info = new ModelInfo
        {
            Name = modelName,
            ContextLength = ParseContextLength(response.Parameters),
            SupportsTools = DetectToolSupport(response.Template),
            Quantization = ParseQuantization(modelName)
        };
        
        _modelInfoCache.TryAdd(modelName, info);
        return info;
    }
    
    public Task<HealthCheckResult> CheckHealthAsync(
        CancellationToken cancellationToken = default)
    {
        return _healthChecker.CheckAsync(cancellationToken);
    }
    
    private async Task<T> ExecuteWithRetryAsync<T>(
        Func<Task<T>> operation,
        CancellationToken cancellationToken)
    {
        var attempt = 0;
        while (true)
        {
            attempt++;
            try
            {
                return await operation();
            }
            catch (Exception ex) when (attempt <= _retryPolicy.MaxRetries && 
                                        _retryPolicy.ShouldRetry(ex, attempt))
            {
                var delay = _retryPolicy.GetDelay(attempt);
                _logger.LogWarning(
                    "Request failed, retrying. Attempt={Attempt}, Delay={Delay}ms, Error={Error}",
                    attempt,
                    delay.TotalMilliseconds,
                    ex.Message);
                
                await Task.Delay(delay, cancellationToken);
            }
            catch (Exception ex)
            {
                throw WrapException(ex);
            }
        }
    }
    
    private static OllamaException WrapException(Exception ex)
    {
        return ex switch
        {
            OllamaException => ex as OllamaException,
            HttpRequestException httpEx when httpEx.StatusCode == HttpStatusCode.NotFound =>
                new OllamaRequestException("Model not found", HttpStatusCode.NotFound, "ACODE-OLM-003", httpEx),
            HttpRequestException httpEx when httpEx.StatusCode >= HttpStatusCode.BadRequest &&
                                             httpEx.StatusCode < HttpStatusCode.InternalServerError =>
                new OllamaRequestException(httpEx.Message, httpEx.StatusCode!.Value, "ACODE-OLM-004", httpEx),
            HttpRequestException httpEx when httpEx.StatusCode >= HttpStatusCode.InternalServerError =>
                new OllamaServerException(httpEx.Message, httpEx.StatusCode!.Value, "ACODE-OLM-005", httpEx),
            HttpRequestException =>
                new OllamaConnectionException("Unable to connect to Ollama", "ACODE-OLM-001", ex),
            TaskCanceledException when !cancellationToken.IsCancellationRequested =>
                new OllamaTimeoutException("Request timed out", "ACODE-OLM-002", ex),
            JsonException =>
                new OllamaParseException("Failed to parse response", "ACODE-OLM-006", ex),
            _ => new OllamaException("Unexpected error", "ACODE-OLM-099", ex)
        };
    }
    
    public async ValueTask DisposeAsync()
    {
        await _client.DisposeAsync();
        _modelInfoCache.Clear();
    }
}
```

### Error Codes

| Code | Constant | Message | HTTP Status | Retryable |
|------|----------|---------|-------------|-----------|
| ACODE-OLM-001 | `OllamaConnectionError` | Unable to connect to Ollama | N/A | Yes |
| ACODE-OLM-002 | `OllamaTimeoutError` | Ollama request timeout | N/A | Yes |
| ACODE-OLM-003 | `OllamaModelNotFound` | Model not found | 404 | No |
| ACODE-OLM-004 | `OllamaInvalidRequest` | Invalid request parameters | 4xx | No |
| ACODE-OLM-005 | `OllamaServerError` | Ollama server error | 5xx | Yes |
| ACODE-OLM-006 | `OllamaParseError` | Failed to parse response | N/A | Yes (once) |
| ACODE-OLM-007 | `OllamaInvalidToolCall` | Invalid tool call JSON | N/A | Yes |
| ACODE-OLM-008 | `OllamaStreamLost` | Streaming connection lost | N/A | Yes |
| ACODE-OLM-009 | `OllamaMaxRetries` | Max retries exceeded | N/A | No |
| ACODE-OLM-010 | `OllamaHealthCheckFailed` | Health check failed | N/A | N/A |

### Logging Schema

All log entries from OllamaProvider MUST include these structured fields:

```json
{
  "timestamp": "2024-01-15T10:30:00.000Z",
  "level": "Information|Warning|Error",
  "category": "AgenticCoder.Infrastructure.Ollama.OllamaProvider",
  "correlationId": "abc-123-def-456",
  "provider": "ollama",
  "model": "llama3.2:8b",
  "eventName": "ChatCompletionSucceeded|ChatCompletionFailed|StreamStarted|StreamCompleted|HealthCheck|RetryAttempt",
  "durationMs": 1234,
  "promptTokens": 150,
  "completionTokens": 25,
  "totalTokens": 175,
  "finishReason": "stop|tool_calls|length",
  "streaming": true,
  "deltaCount": 45,
  "retryAttempt": 1,
  "errorCode": "ACODE-OLM-001",
  "errorMessage": "Unable to connect to Ollama",
  "endpoint": "http://localhost:11434"
}
```

Log levels:
- **Debug**: Request/response details (redacted content), stream chunks
- **Information**: Successful completions, health checks, model listing
- **Warning**: Retries, non-critical parse failures, slow responses
- **Error**: All failures with full exception context

### CLI Exit Codes

| Code | Name | Description |
|------|------|-------------|
| 0 | Success | Request completed successfully |
| 10 | OllamaConnectionFailed | Cannot connect to Ollama endpoint |
| 11 | OllamaTimeout | Request timed out |
| 12 | OllamaModelNotFound | Requested model not available |
| 13 | OllamaRequestInvalid | Invalid request parameters |
| 14 | OllamaServerError | Ollama returned 5xx error |
| 15 | OllamaParseError | Failed to parse Ollama response |

### Configuration Defaults

```yaml
model:
  providers:
    ollama:
      # Connection settings
      enabled: true
      endpoint: "http://localhost:11434"
      default_model: "llama3.2:8b"
      
      # Timeouts (seconds)
      connect_timeout_seconds: 5
      request_timeout_seconds: 120
      streaming_timeout_seconds: 300
      health_check_timeout_seconds: 5
      
      # Retry policy
      retry:
        max_retries: 3
        initial_delay_ms: 100
        max_delay_ms: 10000
        backoff_multiplier: 2.0
        retryable_status_codes: [429, 503]
      
      # Model defaults
      options:
        temperature: 0.7
        top_p: 0.9
        top_k: 40
        repeat_penalty: 1.1
        seed: null
        num_ctx: null  # Uses model default
        keep_alive: "5m"
      
      # Health check
      health_check:
        enabled: true
        interval_seconds: 30
        degraded_threshold_ms: 2000
```

Configuration precedence (highest to lowest):
1. Environment variables (ACODE_MODEL_PROVIDERS_OLLAMA_*)
2. .agent/config.yml in current directory
3. ~/.acode/config.yml (user default)
4. Built-in defaults

### Implementation Checklist

#### Core Implementation
- [ ] Create OllamaConfiguration class with validation
- [ ] Create IOllamaHttpClient interface
- [ ] Implement OllamaHttpClient with connection pooling
- [ ] Create all Ollama model types (OllamaRequest, OllamaResponse, etc.)
- [ ] Implement OllamaMessageMapper for request mapping
- [ ] Implement OllamaToolMapper for tool definition mapping
- [ ] Implement OllamaResponseParser for response parsing
- [ ] Implement OllamaStreamReader for NDJSON streaming
- [ ] Create OllamaProvider class implementing IModelProvider
- [ ] Implement CompleteAsync with full error handling
- [ ] Implement StreamAsync with cancellation support
- [ ] Implement ListModelsAsync with caching
- [ ] Implement GetModelInfoAsync with caching
- [ ] Implement IOllamaHealthChecker interface
- [ ] Implement OllamaHealthChecker class

#### Error Handling
- [ ] Create OllamaException base class
- [ ] Create OllamaConnectionException
- [ ] Create OllamaTimeoutException
- [ ] Create OllamaRequestException
- [ ] Create OllamaServerException
- [ ] Create OllamaParseException
- [ ] Implement exception wrapping in provider

#### Retry Logic
- [ ] Create IOllamaRetryPolicy interface
- [ ] Implement OllamaRetryPolicy with exponential backoff
- [ ] Implement OllamaRetryHandler delegating handler
- [ ] Integrate retry with CompleteAsync
- [ ] Add retry logging

#### Registration & Configuration
- [ ] Register OllamaProvider with DI container
- [ ] Register with Provider Registry (Task 004.c)
- [ ] Implement environment variable override loading
- [ ] Add configuration validation on startup

#### Testing
- [ ] Write unit tests for OllamaProvider
- [ ] Write unit tests for OllamaMessageMapper
- [ ] Write unit tests for OllamaToolMapper
- [ ] Write unit tests for OllamaResponseParser
- [ ] Write unit tests for OllamaRetryPolicy
- [ ] Write integration tests with live Ollama
- [ ] Write E2E tests for full conversations
- [ ] Write performance benchmarks

#### Documentation
- [ ] Add XML documentation to all public APIs
- [ ] Update config examples in User Manual
- [ ] Document all error codes
- [ ] Document troubleshooting steps

### Rollout Plan

#### Phase 1: Core Implementation (Days 1-3)
1. Implement configuration and HTTP client
2. Implement message and tool mappers
3. Implement response parser and stream reader
4. Create exception types
5. Write unit tests for mappers and parsers

#### Phase 2: Provider Implementation (Days 4-5)
1. Implement OllamaProvider class
2. Implement CompleteAsync
3. Implement StreamAsync
4. Implement health checking
5. Write unit tests for provider

#### Phase 3: Retry & Integration (Days 6-7)
1. Implement retry policy and handler
2. Register with DI and Provider Registry
3. Write integration tests
4. Write E2E tests

#### Phase 4: Polish & Documentation (Days 8-9)
1. Performance benchmarks and optimization
2. Complete XML documentation
3. Update User Manual
4. Final code review

#### Phase 5: Validation (Day 10)
1. Run full test suite
2. Manual verification of all scenarios
3. Performance validation
4. Security review

### Dependencies

| Dependency | Version | Purpose |
|------------|---------|---------|
| Task 004 | Complete | IModelProvider interface |
| Task 004.a | Complete | ChatMessage, ToolCall types |
| Task 004.b | Complete | ChatResponse, ResponseDelta types |
| Task 004.c | Complete | Provider Registry for registration |
| System.Net.Http | .NET 8.0 | HTTP communication |
| System.Text.Json | .NET 8.0 | JSON serialization |
| Microsoft.Extensions.Http | 8.0.0 | HttpClientFactory |
| Microsoft.Extensions.Logging | 8.0.0 | Structured logging |

### Verification Command

```bash
# Run all Ollama-related tests
dotnet test --filter "FullyQualifiedName~Ollama"

# Run only unit tests (no Ollama required)
dotnet test --filter "FullyQualifiedName~Ollama&TestCategory=Unit"

# Run integration tests (Ollama must be running)
dotnet test --filter "FullyQualifiedName~Ollama&TestCategory=Integration"

# Run performance benchmarks
dotnet run -c Release --project Tests/Performance/Performance.csproj -- --filter "OllamaBenchmarks"
```

---

**End of Task 005 Specification**
