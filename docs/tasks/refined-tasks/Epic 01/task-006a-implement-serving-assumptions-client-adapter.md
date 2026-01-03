# Task 006.a: Implement Serving Assumptions + Client Adapter

**Priority:** P1 – High Priority  
**Tier:** Core Infrastructure  
**Complexity:** 13 (Fibonacci points)  
**Phase:** Foundation  
**Dependencies:** Task 006, Task 004, Task 004.a, Task 004.b, Task 001, Task 002  

---

## Description

Task 006.a implements the vLLM serving assumptions and low-level client adapter, establishing the HTTP communication layer between Acode and vLLM's OpenAI-compatible API. This subtask focuses on the wire protocol, serialization/deserialization, streaming mechanics, and connection management that underpin the higher-level VllmProvider (Task 006).

vLLM exposes an OpenAI-compatible HTTP API, but with important differences in behavior, extensions, and assumptions. This task documents and codifies those assumptions into a robust client adapter that handles the nuances of vLLM's implementation. Understanding these assumptions is critical for reliable operation—misunderstanding them leads to subtle bugs, performance issues, or silent failures.

The core serving assumption is that vLLM runs as a persistent HTTP server exposing endpoints at a configured base URL. Unlike Ollama's automatic model loading, vLLM typically serves a single model (or a few models with careful configuration) that is loaded at server startup. The client adapter MUST NOT assume dynamic model loading—requests for unavailable models fail immediately. Users must ensure the model is loaded before Acode connects.

vLLM's OpenAI-compatible API lives under the `/v1/` path prefix. The `/v1/chat/completions` endpoint accepts POST requests with JSON bodies following the OpenAI chat completion format. The adapter MUST construct requests matching this format exactly, including proper handling of optional fields, default values, and vLLM-specific extensions.

Streaming in vLLM uses Server-Sent Events (SSE), not NDJSON like Ollama. SSE has specific format requirements: each event line starts with `data: ` followed by JSON, blank lines separate events, and the stream terminates with `data: [DONE]`. The client adapter MUST parse this format correctly, handling edge cases like incomplete lines, keep-alive comments, and network interruptions.

The client adapter is responsible for connection management, implementing connection pooling to avoid the overhead of establishing new connections for each request. HttpClient's default pooling is used, but the adapter MUST configure it appropriately for the vLLM use case—longer timeouts for inference, proper idle connection cleanup, and connection limits matching expected concurrency.

Authentication is optional but MUST be supported when vLLM is configured with an API key. The adapter MUST include the `Authorization: Bearer <key>` header when configured, while ensuring the key never appears in logs or error messages. The key can come from configuration or environment variables, following Acode's standard secret management patterns.

Request serialization uses System.Text.Json with source generators for performance. The adapter MUST serialize requests correctly, handling nested structures (messages, tools, tool calls), optional fields (null omission vs. explicit null), and proper numeric formatting (no scientific notation for token counts). Deserialization MUST be lenient for unknown fields but strict for required fields.

Response parsing handles both streaming and non-streaming responses. Non-streaming responses are complete JSON documents that the adapter deserializes into response types. Streaming responses are sequences of SSE events that the adapter parses incrementally, yielding delta objects as they arrive. The adapter MUST handle partial responses, merging content deltas into coherent output.

Error responses from vLLM follow OpenAI's error format with `error` objects containing `message`, `type`, `param`, and `code` fields. The adapter MUST parse these errors and translate them into Acode exceptions with appropriate error codes. Different HTTP status codes indicate different error categories—4xx for client errors, 5xx for server errors—and MUST be handled accordingly.

Timeout handling addresses vLLM's specific behavior. Connect timeouts prevent hanging on unreachable servers. Request timeouts bound total operation time for non-streaming requests. Streaming timeouts detect stalled streams where no data arrives for too long. Each timeout is configurable with sensible defaults appropriate for inference workloads.

Retry logic handles transient failures without retrying permanent failures. Network errors, 503 Service Unavailable, and 429 Too Many Requests are retryable with exponential backoff. 4xx client errors (except 429) are permanent failures that should not be retried. The adapter MUST respect retry configuration and log retry attempts for debugging.

The client adapter integrates with vLLM-specific features when available. vLLM supports additional parameters like `best_of`, `use_beam_search`, `presence_penalty`, and `frequency_penalty`. The adapter MUST support passing these parameters when configured, even if not all are exposed at higher layers. This extensibility enables power users to access vLLM's full capabilities.

Observability is built into the adapter from the ground up. Every request gets a correlation ID for tracing through logs. Request and response timing is logged for performance analysis. Token counts from usage data are logged for capacity planning. Errors are logged with full context for debugging. All logging uses structured fields for easy querying.

The client adapter is designed for testability. All HTTP communication goes through an injectable HttpClient, enabling unit tests with mock responses. Serialization and parsing are testable in isolation. Timeout and retry behavior is testable with controlled delays. Integration tests verify behavior against real vLLM instances.

---

## Glossary / Terms

| Term | Definition |
|------|------------|
| Client Adapter | HTTP communication layer for vLLM API |
| VllmHttpClient | Class implementing HTTP communication |
| Serving Assumption | Behavior vLLM is expected to exhibit |
| SSE | Server-Sent Events streaming format |
| OpenAI-Compatible | API following OpenAI's specification |
| Connection Pooling | Reusing HTTP connections across requests |
| Source Generators | Compile-time JSON serialization |
| Correlation ID | Unique identifier for request tracing |
| Exponential Backoff | Retry delay that increases each attempt |
| Wire Protocol | Low-level communication format |
| Content Negotiation | HTTP Accept header handling |
| Keep-Alive | Connection persistence mechanism |
| Chunked Transfer | HTTP streaming encoding |
| Request Serializer | Converts request objects to JSON |
| Response Parser | Converts JSON to response objects |
| Bearer Token | Authorization header format |
| Idle Timeout | When to close unused connections |
| Socket Reuse | Reusing TCP connections |

---

## Out of Scope

The following items are explicitly excluded from Task 006.a:

- **VllmProvider implementation** - Parent Task 006
- **Structured output enforcement** - Task 006.b
- **Health check endpoints** - Task 006.c
- **Tool call parsing logic** - Separate concern in parent task
- **Provider Registry integration** - Task 004.c
- **vLLM server configuration** - User responsibility
- **Load balancing multiple instances** - Beyond single endpoint
- **gRPC API** - OpenAI-compatible HTTP only
- **Custom authentication schemes** - Bearer token only
- **Response caching** - Not implemented
- **Request deduplication** - Not implemented

---

## Functional Requirements

### VllmHttpClient Class

- FR-001: VllmHttpClient MUST be defined in Infrastructure layer
- FR-002: VllmHttpClient MUST use injected HttpClient
- FR-003: VllmHttpClient MUST implement IAsyncDisposable
- FR-004: VllmHttpClient MUST accept VllmClientConfiguration
- FR-005: VllmHttpClient MUST log all requests with correlation IDs
- FR-006: VllmHttpClient MUST be thread-safe for concurrent use
- FR-007: VllmHttpClient MUST expose PostAsync for non-streaming
- FR-008: VllmHttpClient MUST expose PostStreamingAsync for streaming

### Connection Management

- FR-009: Adapter MUST use SocketsHttpHandler for connection pooling
- FR-010: Adapter MUST configure PooledConnectionLifetime (5 minutes)
- FR-011: Adapter MUST configure PooledConnectionIdleTimeout (2 minutes)
- FR-012: Adapter MUST configure MaxConnectionsPerServer (10)
- FR-013: Adapter MUST configure ConnectTimeout (5 seconds default)
- FR-014: Adapter MUST enable TCP keep-alive
- FR-015: Adapter MUST disable Expect: 100-continue header

### Request Serialization

- FR-016: Serializer MUST use System.Text.Json source generators
- FR-017: Serializer MUST use camelCase property naming
- FR-018: Serializer MUST omit null optional fields
- FR-019: Serializer MUST NOT use scientific notation for numbers
- FR-020: Serializer MUST serialize nested message arrays
- FR-021: Serializer MUST serialize tool definitions correctly
- FR-022: Serializer MUST preserve message ordering
- FR-023: Serializer MUST handle Unicode content correctly

### Request Construction

- FR-024: Adapter MUST set Content-Type: application/json
- FR-025: Adapter MUST set Accept: application/json (non-streaming)
- FR-026: Adapter MUST set Accept: text/event-stream (streaming)
- FR-027: Adapter MUST set Authorization header when configured
- FR-028: Adapter MUST include correlation ID in X-Request-ID header
- FR-029: Adapter MUST use POST method for completions
- FR-030: Adapter MUST use GET method for model listing
- FR-031: Adapter MUST construct correct endpoint URLs

### Non-Streaming Response Handling

- FR-032: Parser MUST deserialize complete JSON response
- FR-033: Parser MUST extract choices array
- FR-034: Parser MUST extract message from first choice
- FR-035: Parser MUST extract content from message
- FR-036: Parser MUST extract tool_calls from message
- FR-037: Parser MUST extract finish_reason from choice
- FR-038: Parser MUST extract usage from response
- FR-039: Parser MUST handle missing optional fields
- FR-040: Parser MUST validate required fields present

### SSE Streaming

- FR-041: StreamReader MUST parse lines with "data: " prefix
- FR-042: StreamReader MUST strip "data: " prefix before parsing
- FR-043: StreamReader MUST handle "[DONE]" as stream termination
- FR-044: StreamReader MUST handle ": " comment lines (keep-alive)
- FR-045: StreamReader MUST handle blank lines between events
- FR-046: StreamReader MUST handle incomplete lines (line buffering)
- FR-047: StreamReader MUST yield chunks as they arrive
- FR-048: StreamReader MUST detect stream end properly
- FR-049: StreamReader MUST support cancellation token

### Streaming Response Parsing

- FR-050: Parser MUST extract delta from streaming chunks
- FR-051: Parser MUST extract content delta
- FR-052: Parser MUST extract tool_calls delta
- FR-053: Parser MUST detect IsFinal on last chunk
- FR-054: Parser MUST extract usage from final chunk (if present)
- FR-055: Parser MUST handle chunks with empty choices
- FR-056: Parser MUST merge partial tool call deltas

### Error Response Handling

- FR-057: Adapter MUST detect error responses (non-2xx status)
- FR-058: Adapter MUST parse error JSON body
- FR-059: Adapter MUST extract error.message
- FR-060: Adapter MUST extract error.type
- FR-061: Adapter MUST extract error.code
- FR-062: Adapter MUST map 400 to VllmRequestException
- FR-063: Adapter MUST map 401 to VllmAuthException
- FR-064: Adapter MUST map 404 to VllmModelNotFoundException
- FR-065: Adapter MUST map 429 to VllmRateLimitException
- FR-066: Adapter MUST map 500 to VllmServerException
- FR-067: Adapter MUST map 503 to VllmServerException
- FR-068: Adapter MUST include request ID in exception

### Timeout Handling

- FR-069: Adapter MUST configure connect timeout (default 5s)
- FR-070: Adapter MUST configure request timeout (default 300s)
- FR-071: Adapter MUST configure streaming read timeout (default 60s per chunk)
- FR-072: Adapter MUST throw VllmTimeoutException on timeout
- FR-073: Adapter MUST cleanup resources on timeout
- FR-074: Adapter MUST log timeout with request context

### Retry Logic

- FR-075: Adapter MUST implement IRetryPolicy
- FR-076: Adapter MUST retry on SocketException
- FR-077: Adapter MUST retry on HttpRequestException (transient)
- FR-078: Adapter MUST retry on 503 Service Unavailable
- FR-079: Adapter MUST retry on 429 with Retry-After header
- FR-080: Adapter MUST NOT retry on 4xx (except 429)
- FR-081: Adapter MUST implement exponential backoff
- FR-082: Adapter MUST respect max retry count (default 3)
- FR-083: Adapter MUST log each retry attempt
- FR-084: Adapter MUST throw after max retries exceeded

### Authentication

- FR-085: Adapter MUST read API key from configuration
- FR-086: Adapter MUST read API key from environment (override)
- FR-087: Adapter MUST format as "Bearer {key}" header
- FR-088: Adapter MUST NOT log API key value
- FR-089: Adapter MUST redact API key in error messages
- FR-090: Adapter MUST work without API key when not configured

### Configuration

- FR-091: Configuration MUST support endpoint URL
- FR-092: Configuration MUST support API key (optional)
- FR-093: Configuration MUST support all timeout values
- FR-094: Configuration MUST support retry settings
- FR-095: Configuration MUST support connection pool settings
- FR-096: Configuration MUST validate values on load
- FR-097: Configuration MUST provide sensible defaults

---

## Non-Functional Requirements

### Performance

- NFR-001: Connection pooling MUST reduce connection overhead by 90%+
- NFR-002: Request serialization MUST complete in < 1ms
- NFR-003: Response parsing MUST complete in < 5ms
- NFR-004: SSE line parsing MUST complete in < 100μs
- NFR-005: Memory allocation per request MUST be < 5KB (excluding content)
- NFR-006: Adapter MUST NOT buffer entire streaming responses
- NFR-007: Adapter MUST release connections promptly on completion

### Reliability

- NFR-008: Adapter MUST handle server restarts gracefully
- NFR-009: Adapter MUST handle network interruptions
- NFR-010: Adapter MUST recover from transient failures via retry
- NFR-011: Adapter MUST cleanup resources on disposal
- NFR-012: Adapter MUST not crash on malformed responses
- NFR-013: Adapter MUST timeout reliably on stalled connections

### Security

- NFR-014: API keys MUST NOT appear in any logs
- NFR-015: API keys MUST be redacted in error messages
- NFR-016: Adapter MUST validate endpoint URLs
- NFR-017: Adapter MUST only connect to configured endpoints
- NFR-018: Adapter MUST support HTTPS endpoints
- NFR-019: Request content MUST NOT be logged at INFO level

### Observability

- NFR-020: Every request MUST have a correlation ID
- NFR-021: Request timing MUST be logged
- NFR-022: Response timing MUST be logged
- NFR-023: Token usage MUST be logged (if returned)
- NFR-024: Errors MUST be logged with structured fields
- NFR-025: Retries MUST be logged with attempt count

### Maintainability

- NFR-026: All public APIs MUST have XML documentation
- NFR-027: Code MUST follow Clean Architecture patterns
- NFR-028: Adapter MUST be testable with mock HttpClient
- NFR-029: Configuration MUST be documented with examples

---

## User Manual Documentation

### Overview

The vLLM Client Adapter handles low-level HTTP communication with vLLM's OpenAI-compatible API. Most users interact with the higher-level VllmProvider, but understanding the adapter helps with debugging and advanced configuration.

### Serving Assumptions

Before using vLLM with Acode, understand these key assumptions:

1. **vLLM runs as a persistent server** - Model is loaded at startup
2. **Single model per endpoint** - Multi-model requires separate instances
3. **OpenAI-compatible API** - Uses /v1/ path prefix
4. **SSE for streaming** - Not NDJSON like Ollama
5. **No automatic model loading** - Model must be pre-loaded

### Connection Configuration

```yaml
model:
  providers:
    vllm:
      endpoint: http://localhost:8000
      
      # Connection pool settings
      connection:
        max_connections: 10
        idle_timeout_seconds: 120
        connection_lifetime_seconds: 300
        connect_timeout_seconds: 5
```

### Timeout Configuration

```yaml
model:
  providers:
    vllm:
      # Timeouts
      timeouts:
        connect_seconds: 5
        request_seconds: 300
        streaming_read_seconds: 60
```

### Retry Configuration

```yaml
model:
  providers:
    vllm:
      retry:
        max_retries: 3
        initial_delay_ms: 100
        max_delay_ms: 30000
        backoff_multiplier: 2.0
```

### Authentication

```yaml
model:
  providers:
    vllm:
      # API key from environment or config
      api_key: ${VLLM_API_KEY}
```

### SSE Streaming Format

vLLM streaming uses Server-Sent Events:

```
data: {"id":"chatcmpl-1","object":"chat.completion.chunk","choices":[{"delta":{"content":"Hello"}}]}

data: {"id":"chatcmpl-1","object":"chat.completion.chunk","choices":[{"delta":{"content":" world"}}]}

data: [DONE]
```

The adapter:
1. Reads lines from the stream
2. Strips "data: " prefix
3. Parses JSON (or detects [DONE])
4. Yields parsed deltas
5. Terminates on [DONE]

### Error Handling

The adapter translates vLLM errors to Acode exceptions:

| HTTP Status | Exception Type | Error Code |
|-------------|---------------|------------|
| 400 | VllmRequestException | ACODE-VLM-004 |
| 401 | VllmAuthException | ACODE-VLM-011 |
| 404 | VllmModelNotFoundException | ACODE-VLM-003 |
| 429 | VllmRateLimitException | ACODE-VLM-012 |
| 500 | VllmServerException | ACODE-VLM-005 |
| 503 | VllmServerException | ACODE-VLM-005 |
| Timeout | VllmTimeoutException | ACODE-VLM-002 |
| Network | VllmConnectionException | ACODE-VLM-001 |

### Debugging Connection Issues

1. **Check vLLM is running**:
   ```bash
   curl http://localhost:8000/health
   ```

2. **Check model is loaded**:
   ```bash
   curl http://localhost:8000/v1/models
   ```

3. **Test request manually**:
   ```bash
   curl -X POST http://localhost:8000/v1/chat/completions \
     -H "Content-Type: application/json" \
     -d '{"model":"model-name","messages":[{"role":"user","content":"hi"}]}'
   ```

4. **Enable debug logging**:
   ```bash
   ACODE_LOG_LEVEL=Debug acode ask "test"
   ```

### Performance Tuning

For high-throughput scenarios:

```yaml
model:
  providers:
    vllm:
      connection:
        max_connections: 50  # Match expected concurrency
        idle_timeout_seconds: 300  # Keep connections warm
```

For low-memory scenarios:

```yaml
model:
  providers:
    vllm:
      connection:
        max_connections: 5  # Limit connections
        idle_timeout_seconds: 30  # Release connections faster
```

---

## Acceptance Criteria

### VllmHttpClient Class

- [ ] AC-001: Located in Infrastructure layer
- [ ] AC-002: Uses injected HttpClient
- [ ] AC-003: Implements IAsyncDisposable
- [ ] AC-004: Accepts configuration
- [ ] AC-005: Thread-safe for concurrency
- [ ] AC-006: Exposes PostAsync method
- [ ] AC-007: Exposes PostStreamingAsync method
- [ ] AC-008: Logs with correlation IDs

### Connection Management

- [ ] AC-009: Uses SocketsHttpHandler
- [ ] AC-010: Configures connection lifetime
- [ ] AC-011: Configures idle timeout
- [ ] AC-012: Configures max connections
- [ ] AC-013: Configures connect timeout
- [ ] AC-014: Enables TCP keep-alive
- [ ] AC-015: Disables Expect: 100-continue

### Request Serialization

- [ ] AC-016: Uses source generators
- [ ] AC-017: Uses camelCase naming
- [ ] AC-018: Omits null fields
- [ ] AC-019: No scientific notation
- [ ] AC-020: Serializes messages array
- [ ] AC-021: Serializes tool definitions
- [ ] AC-022: Preserves message order
- [ ] AC-023: Handles Unicode

### Request Construction

- [ ] AC-024: Sets Content-Type header
- [ ] AC-025: Sets Accept header (non-streaming)
- [ ] AC-026: Sets Accept header (streaming)
- [ ] AC-027: Sets Authorization when configured
- [ ] AC-028: Includes X-Request-ID
- [ ] AC-029: Uses POST for completions
- [ ] AC-030: Uses GET for models
- [ ] AC-031: Correct endpoint URLs

### Non-Streaming Response

- [ ] AC-032: Deserializes JSON response
- [ ] AC-033: Extracts choices array
- [ ] AC-034: Extracts message
- [ ] AC-035: Extracts content
- [ ] AC-036: Extracts tool_calls
- [ ] AC-037: Extracts finish_reason
- [ ] AC-038: Extracts usage
- [ ] AC-039: Handles missing optional fields
- [ ] AC-040: Validates required fields

### SSE Streaming

- [ ] AC-041: Parses data: prefix
- [ ] AC-042: Strips prefix before parsing
- [ ] AC-043: Handles [DONE]
- [ ] AC-044: Handles comment lines
- [ ] AC-045: Handles blank lines
- [ ] AC-046: Handles incomplete lines
- [ ] AC-047: Yields chunks incrementally
- [ ] AC-048: Detects stream end
- [ ] AC-049: Supports cancellation

### Streaming Response

- [ ] AC-050: Extracts delta
- [ ] AC-051: Extracts content delta
- [ ] AC-052: Extracts tool_calls delta
- [ ] AC-053: Detects IsFinal
- [ ] AC-054: Extracts usage from final
- [ ] AC-055: Handles empty choices
- [ ] AC-056: Merges partial deltas

### Error Handling

- [ ] AC-057: Detects error responses
- [ ] AC-058: Parses error JSON
- [ ] AC-059: Extracts error.message
- [ ] AC-060: Extracts error.type
- [ ] AC-061: Extracts error.code
- [ ] AC-062: Maps 400 correctly
- [ ] AC-063: Maps 401 correctly
- [ ] AC-064: Maps 404 correctly
- [ ] AC-065: Maps 429 correctly
- [ ] AC-066: Maps 500 correctly
- [ ] AC-067: Maps 503 correctly
- [ ] AC-068: Includes request ID

### Timeout Handling

- [ ] AC-069: Connect timeout works
- [ ] AC-070: Request timeout works
- [ ] AC-071: Streaming timeout works
- [ ] AC-072: Throws VllmTimeoutException
- [ ] AC-073: Cleans up on timeout
- [ ] AC-074: Logs timeout context

### Retry Logic

- [ ] AC-075: Implements IRetryPolicy
- [ ] AC-076: Retries SocketException
- [ ] AC-077: Retries transient HTTP errors
- [ ] AC-078: Retries 503
- [ ] AC-079: Retries 429 with backoff
- [ ] AC-080: Does NOT retry 4xx
- [ ] AC-081: Exponential backoff works
- [ ] AC-082: Respects max retries
- [ ] AC-083: Logs retry attempts
- [ ] AC-084: Throws after max retries

### Authentication

- [ ] AC-085: Reads key from config
- [ ] AC-086: Reads key from environment
- [ ] AC-087: Formats Bearer header
- [ ] AC-088: Never logs key
- [ ] AC-089: Redacts key in errors
- [ ] AC-090: Works without key

### Security

- [ ] AC-091: Key not in logs
- [ ] AC-092: Key redacted in errors
- [ ] AC-093: Validates URLs
- [ ] AC-094: Only configured endpoints
- [ ] AC-095: Supports HTTPS
- [ ] AC-096: No content at INFO level

---

## Testing Requirements

### Unit Tests

```
Tests/Unit/Infrastructure/Vllm/Client/
├── VllmHttpClientTests.cs
│   ├── PostAsync_Should_Serialize_Request()
│   ├── PostAsync_Should_Parse_Response()
│   ├── PostAsync_Should_Throw_On_Error()
│   ├── PostStreamingAsync_Should_Return_Enumerable()
│   ├── PostStreamingAsync_Should_Support_Cancellation()
│   └── Should_Include_CorrelationId()
│
├── VllmRequestSerializerTests.cs
│   ├── Should_Use_CamelCase()
│   ├── Should_Omit_Nulls()
│   ├── Should_Serialize_Messages()
│   ├── Should_Serialize_Tools()
│   └── Should_Handle_Unicode()
│
├── VllmSseReaderTests.cs
│   ├── Should_Parse_Data_Lines()
│   ├── Should_Strip_Prefix()
│   ├── Should_Handle_Done()
│   ├── Should_Handle_Comments()
│   ├── Should_Handle_Blank_Lines()
│   └── Should_Buffer_Incomplete_Lines()
│
├── VllmRetryPolicyTests.cs
│   ├── Should_Retry_Socket_Errors()
│   ├── Should_Retry_503()
│   ├── Should_Retry_429_With_Backoff()
│   ├── Should_Not_Retry_400()
│   └── Should_Apply_Exponential_Backoff()
│
└── VllmAuthenticationTests.cs
    ├── Should_Include_Bearer_Header()
    ├── Should_Read_From_Environment()
    ├── Should_Not_Log_Key()
    └── Should_Work_Without_Key()
```

### Integration Tests

```
Tests/Integration/Vllm/Client/
├── VllmHttpClientIntegrationTests.cs
│   ├── Should_Connect_To_Vllm()
│   ├── Should_Complete_Request()
│   ├── Should_Stream_Response()
│   └── Should_Handle_Auth()
```

### Performance Tests

```
Tests/Performance/Vllm/Client/
├── VllmClientBenchmarks.cs
│   ├── Benchmark_Serialization()
│   ├── Benchmark_Parsing()
│   ├── Benchmark_SSE_Parsing()
│   └── Benchmark_Connection_Reuse()
```

---

## User Verification Steps

### Scenario 1: Basic Connection

1. Start vLLM server
2. Configure endpoint in config
3. Run `acode providers health --provider vllm`
4. Verify: Connection succeeds

### Scenario 2: Request/Response

1. Send completion request
2. Verify: Request serialized correctly
3. Verify: Response parsed correctly
4. Verify: Content extracted

### Scenario 3: SSE Streaming

1. Send streaming request
2. Verify: SSE lines parsed
3. Verify: Deltas yielded incrementally
4. Verify: [DONE] terminates stream

### Scenario 4: Authentication

1. Configure API key
2. Send request
3. Verify: Authorization header included
4. Verify: Key not in logs

### Scenario 5: Timeout

1. Configure short timeout
2. Send to slow endpoint
3. Verify: Timeout exception thrown
4. Verify: Resources cleaned up

### Scenario 6: Retry

1. Configure retry
2. Cause transient failure
3. Verify: Retry occurs
4. Verify: Backoff applied

### Scenario 7: Error Handling

1. Send request to cause 400
2. Verify: VllmRequestException thrown
3. Verify: Error details extracted
4. Verify: Error logged

### Scenario 8: Connection Pooling

1. Send multiple requests
2. Verify: Connections reused
3. Check debug logs for connection info

---

## Implementation Prompt

### File Structure

```
src/AgenticCoder.Infrastructure/Vllm/Client/
├── VllmHttpClient.cs
├── VllmClientConfiguration.cs
├── Serialization/
│   ├── VllmJsonSerializerContext.cs
│   ├── VllmRequestSerializer.cs
│   └── VllmResponseParser.cs
├── Streaming/
│   ├── VllmSseReader.cs
│   └── VllmSseParser.cs
├── Retry/
│   ├── IVllmRetryPolicy.cs
│   ├── VllmRetryPolicy.cs
│   └── VllmRetryContext.cs
├── Authentication/
│   └── VllmAuthHandler.cs
└── Exceptions/
    ├── VllmAuthException.cs
    ├── VllmModelNotFoundException.cs
    └── VllmRateLimitException.cs
```

### VllmHttpClient Implementation

```csharp
namespace AgenticCoder.Infrastructure.Vllm.Client;

public sealed class VllmHttpClient : IAsyncDisposable
{
    private readonly HttpClient _httpClient;
    private readonly VllmClientConfiguration _config;
    private readonly IVllmRetryPolicy _retryPolicy;
    private readonly ILogger<VllmHttpClient> _logger;
    
    public async Task<TResponse> PostAsync<TResponse>(
        string path,
        object request,
        CancellationToken cancellationToken = default)
    {
        var correlationId = Guid.NewGuid().ToString();
        using var scope = _logger.BeginScope(new { CorrelationId = correlationId });
        
        var json = VllmRequestSerializer.Serialize(request);
        using var content = new StringContent(json, Encoding.UTF8, "application/json");
        
        var httpRequest = new HttpRequestMessage(HttpMethod.Post, path)
        {
            Content = content
        };
        httpRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        httpRequest.Headers.Add("X-Request-ID", correlationId);
        
        return await _retryPolicy.ExecuteAsync(
            async ct => await ExecuteAsync<TResponse>(httpRequest, ct),
            cancellationToken);
    }
    
    public async IAsyncEnumerable<VllmStreamChunk> PostStreamingAsync(
        string path,
        object request,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // SSE streaming implementation
    }
}
```

### VllmSseReader Implementation

```csharp
namespace AgenticCoder.Infrastructure.Vllm.Client.Streaming;

public sealed class VllmSseReader
{
    public async IAsyncEnumerable<string> ReadEventsAsync(
        Stream stream,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        using var reader = new StreamReader(stream, Encoding.UTF8);
        var buffer = new StringBuilder();
        
        while (!cancellationToken.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(cancellationToken);
            if (line == null) break;
            
            if (line.StartsWith("data: "))
            {
                var data = line.Substring(6);
                if (data == "[DONE]") break;
                yield return data;
            }
            // Ignore blank lines and comments
        }
    }
}
```

### Error Codes

| Code | Message |
|------|---------|
| ACODE-VLM-001 | Unable to connect to vLLM server |
| ACODE-VLM-002 | vLLM request timeout |
| ACODE-VLM-003 | Model not found on vLLM server |
| ACODE-VLM-004 | Invalid request parameters |
| ACODE-VLM-005 | vLLM server error |
| ACODE-VLM-006 | Failed to parse response |
| ACODE-VLM-011 | Authentication failed |
| ACODE-VLM-012 | Rate limit exceeded |

### Implementation Checklist

1. [ ] Create VllmClientConfiguration
2. [ ] Create VllmJsonSerializerContext
3. [ ] Implement VllmRequestSerializer
4. [ ] Implement VllmResponseParser
5. [ ] Implement VllmSseReader
6. [ ] Create IVllmRetryPolicy
7. [ ] Implement VllmRetryPolicy
8. [ ] Create VllmAuthHandler
9. [ ] Create VllmHttpClient
10. [ ] Implement PostAsync
11. [ ] Implement PostStreamingAsync
12. [ ] Create exception types
13. [ ] Wire up DI registration
14. [ ] Write unit tests
15. [ ] Write integration tests
16. [ ] Add XML documentation

### Dependencies

- Task 006 (VllmProvider uses this)
- System.Net.Http for HTTP client
- System.Text.Json for serialization
- Microsoft.Extensions.Logging

### Verification Command

```bash
dotnet test --filter "FullyQualifiedName~Vllm.Client"
```

---

**End of Task 006.a Specification**