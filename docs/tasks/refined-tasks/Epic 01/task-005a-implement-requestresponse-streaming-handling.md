# Task 005.a: Implement Request/Response and Streaming Handling

**Priority:** P0 – Critical Path  
**Tier:** Core Infrastructure  
**Complexity:** 13 (Fibonacci points)  
**Phase:** Foundation  
**Dependencies:** Task 005, Task 004, Task 004.a, Task 004.b  

---

## Description

Task 005.a specifies the detailed implementation of request serialization, response deserialization, and streaming handling for the Ollama provider adapter. While Task 005 defines the overall adapter architecture, this subtask focuses on the low-level HTTP communication patterns that ensure reliable, performant data exchange between Acode and the Ollama inference server.

Request handling encompasses the complete lifecycle from ChatRequest construction through HTTP transmission. The implementation MUST serialize Acode's canonical request types into Ollama's expected JSON format, manage HTTP connection lifecycle, handle request timeouts, and provide cancellation support. Request serialization MUST be efficient—using source-generated JSON serializers rather than reflection—to minimize latency overhead.

Response handling addresses both synchronous (non-streaming) and asynchronous (streaming) response patterns. For non-streaming requests, the adapter receives a complete JSON response body, parses it into Ollama-specific types, then maps to Acode's ChatResponse. For streaming requests, the adapter processes NDJSON (newline-delimited JSON) incrementally, yielding ResponseDelta instances as each chunk arrives. Both patterns MUST handle malformed responses gracefully without crashing.

Streaming is the more complex and critical path. When streaming is enabled, Ollama sends a series of JSON objects, each on its own line, with partial content that accumulates into the complete response. The streaming implementation MUST parse each line independently, extract the content delta or tool call delta, and yield immediately to the caller. This enables real-time display of generation progress—essential for user experience with large responses.

The streaming reader MUST handle edge cases that occur in real-world usage: partial lines split across TCP packets, connection drops mid-stream, timeouts on stalled streams, and graceful handling of the final chunk that includes usage statistics. The implementation MUST NOT buffer entire responses in memory—each chunk MUST be processed and yielded before reading the next to bound memory usage regardless of response size.

HTTP client configuration significantly impacts reliability and performance. The implementation MUST use HttpClient with connection pooling to avoid the overhead of establishing new TCP connections for each request. Timeouts MUST be configured at multiple levels: connection timeout (how long to wait for TCP handshake), request timeout (total time for non-streaming requests), and streaming timeout (idle timeout between chunks for streaming requests).

Error handling within request/response processing spans multiple failure modes. Network errors (DNS resolution, connection refused, connection reset) MUST be distinguished from HTTP errors (4xx, 5xx status codes) and parsing errors (invalid JSON, unexpected schema). Each error type requires different handling: network errors may be transient and retriable, 4xx errors indicate client issues that won't resolve with retry, and 5xx errors may indicate temporary server issues.

Cancellation support via CancellationToken is essential for responsive applications. Users may cancel long-running requests, and the implementation MUST propagate cancellation through the HTTP client to abort in-flight requests promptly. Cancelled requests MUST NOT leave orphaned resources or dangling connections. The implementation MUST cleanup HTTP response streams even when cancellation interrupts processing.

This subtask delivers the foundational communication layer that all other Ollama adapter functionality depends on. Robust request/response handling enables reliable tool calling (Task 005.b), supports the smoke test script (Task 005.c), and provides the performance characteristics required for production use. Any bugs in this layer cascade throughout the system, making comprehensive testing critical.

---

## Glossary / Terms

| Term | Definition |
|------|------------|
| OllamaHttpClient | HTTP client wrapper for Ollama API communication |
| OllamaRequest | Serializable request type matching Ollama's API schema |
| OllamaResponse | Deserializable response type matching Ollama's API schema |
| OllamaStreamChunk | Single NDJSON line from a streaming response |
| StreamReader | Component parsing NDJSON stream into chunks |
| RequestSerializer | Component converting ChatRequest to OllamaRequest JSON |
| ResponseParser | Component converting OllamaResponse to ChatResponse |
| DeltaParser | Component converting OllamaStreamChunk to ResponseDelta |
| NDJSON | Newline-delimited JSON format used for streaming |
| HttpClient | .NET HTTP client for making API requests |
| HttpClientFactory | Factory for creating configured HttpClient instances |
| Connection Pool | Reusable pool of HTTP connections to Ollama |
| Idle Timeout | Time before idle connections are closed |
| Read Timeout | Maximum time to wait for response data |
| Chunk Timeout | Maximum idle time between stream chunks |
| Backpressure | Flow control when consumer is slower than producer |
| PipeReader | High-performance streaming reader for HTTP responses |
| JsonDocument | DOM representation for JSON parsing |
| Utf8JsonReader | Forward-only JSON reader for streaming |
| Source Generator | Compile-time JSON serializer generation |

---

## Out of Scope

The following items are explicitly excluded from Task 005.a:

- **Tool call parsing logic** - Detailed tool call handling is Task 005.b
- **Retry policies** - Retry logic is defined at the provider level in Task 005
- **Health checking** - Health check implementation is in Task 005
- **Model enumeration** - Model listing is in Task 005
- **Configuration loading** - Config parsing is in Task 005
- **Provider registration** - Registry integration is in Task 005
- **Request validation** - Request validation is caller responsibility
- **Response caching** - Caching is not implemented
- **Request queuing** - Queuing is not implemented
- **Compression** - Request/response compression not required
- **Authentication** - Ollama doesn't require authentication

---

## Functional Requirements

### OllamaHttpClient Class

- FR-001: OllamaHttpClient MUST be defined in Infrastructure layer
- FR-002: OllamaHttpClient MUST accept OllamaConfiguration via constructor
- FR-003: OllamaHttpClient MUST use IHttpClientFactory for HttpClient creation
- FR-004: OllamaHttpClient MUST configure base address from configuration
- FR-005: OllamaHttpClient MUST configure timeout from configuration
- FR-006: OllamaHttpClient MUST implement IDisposable for cleanup
- FR-007: OllamaHttpClient MUST expose correlation ID for request tracing

### Request Serialization

- FR-008: RequestSerializer MUST convert ChatRequest to OllamaRequest
- FR-009: RequestSerializer MUST use System.Text.Json source generators
- FR-010: RequestSerializer MUST set "model" from request or default
- FR-011: RequestSerializer MUST set "stream" based on request type
- FR-012: RequestSerializer MUST map messages array correctly
- FR-013: RequestSerializer MUST map tool definitions when present
- FR-014: RequestSerializer MUST include options (temperature, top_p, etc.)
- FR-015: RequestSerializer MUST set format: "json" for JSON mode
- FR-016: RequestSerializer MUST set keep_alive from configuration
- FR-017: RequestSerializer MUST omit null/default values
- FR-018: Serialized JSON MUST use snake_case property names

### OllamaRequest Type

- FR-019: OllamaRequest MUST include model (string, required)
- FR-020: OllamaRequest MUST include messages (array, required)
- FR-021: OllamaRequest MUST include stream (bool, required)
- FR-022: OllamaRequest MUST include tools (array, optional)
- FR-023: OllamaRequest MUST include format (string, optional)
- FR-024: OllamaRequest MUST include options (object, optional)
- FR-025: OllamaRequest MUST include keep_alive (string, optional)
- FR-026: OllamaRequest.options MUST support temperature
- FR-027: OllamaRequest.options MUST support top_p
- FR-028: OllamaRequest.options MUST support seed
- FR-029: OllamaRequest.options MUST support num_ctx
- FR-030: OllamaRequest.options MUST support stop sequences

### Non-Streaming Request Execution

- FR-031: PostAsync MUST send POST request to specified endpoint
- FR-032: PostAsync MUST set Content-Type to application/json
- FR-033: PostAsync MUST serialize request body with RequestSerializer
- FR-034: PostAsync MUST read complete response body
- FR-035: PostAsync MUST deserialize response with ResponseParser
- FR-036: PostAsync MUST throw on non-success status codes
- FR-037: PostAsync MUST timeout after configured duration
- FR-038: PostAsync MUST support cancellation via CancellationToken
- FR-039: PostAsync MUST dispose response properly on all paths
- FR-040: PostAsync MUST log request and response timing

### OllamaResponse Type

- FR-041: OllamaResponse MUST include model (string)
- FR-042: OllamaResponse MUST include created_at (string)
- FR-043: OllamaResponse MUST include message (OllamaMessage)
- FR-044: OllamaResponse MUST include done (bool)
- FR-045: OllamaResponse MUST include done_reason (string, optional)
- FR-046: OllamaResponse MUST include total_duration (long, nanoseconds)
- FR-047: OllamaResponse MUST include prompt_eval_count (int)
- FR-048: OllamaResponse MUST include eval_count (int)
- FR-049: OllamaMessage MUST include role (string)
- FR-050: OllamaMessage MUST include content (string, optional)
- FR-051: OllamaMessage MUST include tool_calls (array, optional)

### Response Parsing

- FR-052: ResponseParser MUST convert OllamaResponse to ChatResponse
- FR-053: ResponseParser MUST map message content to ChatMessage
- FR-054: ResponseParser MUST map done_reason to FinishReason
- FR-055: ResponseParser MUST map "stop" to FinishReason.Stop
- FR-056: ResponseParser MUST map "length" to FinishReason.Length
- FR-057: ResponseParser MUST map "tool_calls" to FinishReason.ToolCalls
- FR-058: ResponseParser MUST calculate UsageInfo from token counts
- FR-059: ResponseParser MUST calculate ResponseMetadata from timing
- FR-060: ResponseParser MUST preserve tool_calls in message
- FR-061: ResponseParser MUST handle missing optional fields gracefully

### Streaming Request Execution

- FR-062: PostStreamAsync MUST send POST request with stream: true
- FR-063: PostStreamAsync MUST return Stream for response body
- FR-064: PostStreamAsync MUST NOT dispose stream (caller responsibility)
- FR-065: PostStreamAsync MUST throw on non-success status codes
- FR-066: PostStreamAsync MUST support cancellation
- FR-067: PostStreamAsync MUST configure response buffering appropriately

### Stream Reading

- FR-068: StreamReader MUST read NDJSON format (one JSON object per line)
- FR-069: StreamReader MUST handle lines split across reads
- FR-070: StreamReader MUST parse each line as OllamaStreamChunk
- FR-071: StreamReader MUST yield chunks via IAsyncEnumerable
- FR-072: StreamReader MUST detect final chunk (done: true)
- FR-073: StreamReader MUST handle empty lines gracefully
- FR-074: StreamReader MUST timeout on stalled streams
- FR-075: StreamReader MUST propagate cancellation
- FR-076: StreamReader MUST dispose stream on completion
- FR-077: StreamReader MUST dispose stream on exception
- FR-078: StreamReader MUST dispose stream on cancellation

### OllamaStreamChunk Type

- FR-079: OllamaStreamChunk MUST include model (string)
- FR-080: OllamaStreamChunk MUST include message (OllamaMessage)
- FR-081: OllamaStreamChunk MUST include done (bool)
- FR-082: OllamaStreamChunk MUST include done_reason (string, optional, final only)
- FR-083: OllamaStreamChunk MUST include total_duration (long, final only)
- FR-084: OllamaStreamChunk MUST include eval_count (int, final only)
- FR-085: OllamaStreamChunk MUST include prompt_eval_count (int, final only)

### Delta Parsing

- FR-086: DeltaParser MUST convert OllamaStreamChunk to ResponseDelta
- FR-087: DeltaParser MUST extract content delta from message.content
- FR-088: DeltaParser MUST extract tool call delta from message.tool_calls
- FR-089: DeltaParser MUST set FinishReason on final chunk
- FR-090: DeltaParser MUST set Usage on final chunk
- FR-091: DeltaParser MUST track chunk index
- FR-092: DeltaParser MUST handle empty content chunks

### Error Handling

- FR-093: Network errors MUST be wrapped in OllamaConnectionException
- FR-094: Timeout errors MUST be wrapped in OllamaTimeoutException
- FR-095: HTTP 4xx errors MUST be wrapped in OllamaRequestException
- FR-096: HTTP 5xx errors MUST be wrapped in OllamaServerException
- FR-097: Parse errors MUST be wrapped in OllamaParseException
- FR-098: All exceptions MUST include original exception as InnerException
- FR-099: All exceptions MUST include request correlation ID
- FR-100: Stream read errors MUST be properly handled and wrapped

---

## Non-Functional Requirements

### Performance

- NFR-001: Request serialization MUST complete in < 1 millisecond
- NFR-002: Response parsing MUST complete in < 5 milliseconds
- NFR-003: Stream chunk parsing MUST complete in < 100 microseconds
- NFR-004: Memory allocation per chunk MUST be < 1 KB
- NFR-005: HttpClient MUST reuse connections (connection pooling)
- NFR-006: Streaming MUST NOT buffer entire response
- NFR-007: Streaming MUST yield chunks immediately (no batching)
- NFR-008: JSON serialization MUST use source generators (no reflection)

### Reliability

- NFR-009: Partial stream reads MUST be handled correctly
- NFR-010: Connection drops MUST be detected and reported
- NFR-011: Stalled streams MUST timeout appropriately
- NFR-012: Resource cleanup MUST occur on all code paths
- NFR-013: Concurrent requests MUST be supported
- NFR-014: Thread safety MUST be maintained

### Security

- NFR-015: Request bodies MUST NOT be logged at INFO level
- NFR-016: Response bodies MUST NOT be logged at INFO level
- NFR-017: Exception messages MUST NOT expose sensitive data
- NFR-018: Only configured endpoints MUST be contacted

### Observability

- NFR-019: All requests MUST be logged with correlation IDs
- NFR-020: Request/response timing MUST be logged
- NFR-021: Stream chunk counts MUST be tracked
- NFR-022: Errors MUST be logged with full context

---

## User Manual Documentation

### Overview

This subtask implements the core HTTP communication layer for the Ollama provider. It handles all request serialization, response parsing, and streaming functionality.

### Request Flow

```
ChatRequest → RequestSerializer → OllamaRequest → HTTP POST → OllamaResponse → ResponseParser → ChatResponse
```

### Streaming Flow

```
ChatRequest → RequestSerializer → OllamaRequest → HTTP POST → NDJSON Stream → StreamReader → OllamaStreamChunk → DeltaParser → ResponseDelta
```

### Configuration

The HTTP client uses configuration from the Ollama provider section:

```yaml
model:
  providers:
    ollama:
      endpoint: http://localhost:11434
      connect_timeout_seconds: 5
      request_timeout_seconds: 120
      streaming_timeout_seconds: 300
```

### Timeout Behavior

| Timeout Type | Default | Description |
|--------------|---------|-------------|
| Connect | 5s | TCP connection establishment |
| Request | 120s | Total time for non-streaming requests |
| Streaming | 300s | Idle time between stream chunks |

### Streaming Best Practices

1. **Always dispose streams**: Use `await using` for streaming enumerables
2. **Handle cancellation**: Pass CancellationToken through the call chain
3. **Process chunks immediately**: Don't buffer chunks before processing
4. **Monitor for stalls**: Watch for streams that stop producing chunks

### Error Handling Patterns

```csharp
try
{
    var response = await client.PostAsync<OllamaResponse>("/api/chat", request);
}
catch (OllamaConnectionException ex)
{
    // Network issue - may be transient
    logger.LogWarning(ex, "Connection to Ollama failed");
}
catch (OllamaTimeoutException ex)
{
    // Request took too long
    logger.LogWarning(ex, "Ollama request timed out");
}
catch (OllamaRequestException ex)
{
    // Client error (4xx) - check request
    logger.LogError(ex, "Invalid request to Ollama");
}
catch (OllamaServerException ex)
{
    // Server error (5xx) - may be transient
    logger.LogWarning(ex, "Ollama server error");
}
catch (OllamaParseException ex)
{
    // Response parsing failed
    logger.LogError(ex, "Failed to parse Ollama response");
}
```

### Streaming Consumption

```csharp
await using var stream = await client.PostStreamAsync("/api/chat", request);

await foreach (var chunk in StreamReader.ReadAsync(stream, cancellationToken))
{
    var delta = DeltaParser.Parse(chunk);
    
    if (delta.ContentDelta is not null)
    {
        Console.Write(delta.ContentDelta);
    }
    
    if (delta.IsComplete)
    {
        Console.WriteLine($"\nTokens: {delta.Usage?.TotalTokens}");
    }
}
```

### Troubleshooting

#### Connection Refused

**Cause**: Ollama not running or wrong endpoint

**Fix**: 
```bash
# Verify Ollama is running
curl http://localhost:11434/api/tags

# Check configured endpoint
cat .agent/config.yml | grep endpoint
```

#### Request Timeout

**Cause**: Large generation taking too long

**Fix**: Increase timeout in config:
```yaml
model:
  providers:
    ollama:
      request_timeout_seconds: 300
```

#### Stream Stall

**Cause**: Model computation pause or network issue

**Symptoms**: No chunks received for extended period

**Fix**: Ensure streaming_timeout is reasonable and model isn't overloaded

---

## Acceptance Criteria

### OllamaHttpClient

- [ ] AC-001: Defined in Infrastructure layer
- [ ] AC-002: Accepts configuration via constructor
- [ ] AC-003: Uses IHttpClientFactory
- [ ] AC-004: Configures base address
- [ ] AC-005: Configures timeout
- [ ] AC-006: Implements IDisposable
- [ ] AC-007: Exposes correlation ID

### Request Serialization

- [ ] AC-008: Converts ChatRequest to OllamaRequest
- [ ] AC-009: Uses source generators
- [ ] AC-010: Sets model correctly
- [ ] AC-011: Sets stream flag
- [ ] AC-012: Maps messages array
- [ ] AC-013: Maps tool definitions
- [ ] AC-014: Includes options
- [ ] AC-015: Sets format for JSON mode
- [ ] AC-016: Sets keep_alive
- [ ] AC-017: Omits null values
- [ ] AC-018: Uses snake_case

### OllamaRequest Type

- [ ] AC-019: model property exists
- [ ] AC-020: messages property exists
- [ ] AC-021: stream property exists
- [ ] AC-022: tools property exists
- [ ] AC-023: format property exists
- [ ] AC-024: options property exists
- [ ] AC-025: keep_alive property exists
- [ ] AC-026: temperature in options
- [ ] AC-027: top_p in options
- [ ] AC-028: seed in options
- [ ] AC-029: num_ctx in options
- [ ] AC-030: stop in options

### Non-Streaming Execution

- [ ] AC-031: Sends POST request
- [ ] AC-032: Sets Content-Type
- [ ] AC-033: Serializes request body
- [ ] AC-034: Reads complete response
- [ ] AC-035: Deserializes response
- [ ] AC-036: Throws on non-success
- [ ] AC-037: Respects timeout
- [ ] AC-038: Supports cancellation
- [ ] AC-039: Disposes response
- [ ] AC-040: Logs timing

### OllamaResponse Type

- [ ] AC-041: model property exists
- [ ] AC-042: created_at property exists
- [ ] AC-043: message property exists
- [ ] AC-044: done property exists
- [ ] AC-045: done_reason property exists
- [ ] AC-046: total_duration property exists
- [ ] AC-047: prompt_eval_count property exists
- [ ] AC-048: eval_count property exists
- [ ] AC-049: OllamaMessage.role exists
- [ ] AC-050: OllamaMessage.content exists
- [ ] AC-051: OllamaMessage.tool_calls exists

### Response Parsing

- [ ] AC-052: Converts to ChatResponse
- [ ] AC-053: Maps message content
- [ ] AC-054: Maps done_reason
- [ ] AC-055: Maps "stop" correctly
- [ ] AC-056: Maps "length" correctly
- [ ] AC-057: Maps "tool_calls" correctly
- [ ] AC-058: Calculates UsageInfo
- [ ] AC-059: Calculates Metadata
- [ ] AC-060: Preserves tool_calls
- [ ] AC-061: Handles missing fields

### Streaming Execution

- [ ] AC-062: Sends POST with stream
- [ ] AC-063: Returns Stream
- [ ] AC-064: Caller disposes stream
- [ ] AC-065: Throws on non-success
- [ ] AC-066: Supports cancellation
- [ ] AC-067: Configures buffering

### Stream Reading

- [ ] AC-068: Reads NDJSON format
- [ ] AC-069: Handles split lines
- [ ] AC-070: Parses each line
- [ ] AC-071: Yields via IAsyncEnumerable
- [ ] AC-072: Detects final chunk
- [ ] AC-073: Handles empty lines
- [ ] AC-074: Times out on stall
- [ ] AC-075: Propagates cancellation
- [ ] AC-076: Disposes on completion
- [ ] AC-077: Disposes on exception
- [ ] AC-078: Disposes on cancellation

### OllamaStreamChunk Type

- [ ] AC-079: model property exists
- [ ] AC-080: message property exists
- [ ] AC-081: done property exists
- [ ] AC-082: done_reason optional
- [ ] AC-083: total_duration optional
- [ ] AC-084: eval_count optional
- [ ] AC-085: prompt_eval_count optional

### Delta Parsing

- [ ] AC-086: Converts to ResponseDelta
- [ ] AC-087: Extracts content delta
- [ ] AC-088: Extracts tool call delta
- [ ] AC-089: Sets FinishReason on final
- [ ] AC-090: Sets Usage on final
- [ ] AC-091: Tracks chunk index
- [ ] AC-092: Handles empty content

### Error Handling

- [ ] AC-093: Wraps network errors
- [ ] AC-094: Wraps timeout errors
- [ ] AC-095: Wraps 4xx errors
- [ ] AC-096: Wraps 5xx errors
- [ ] AC-097: Wraps parse errors
- [ ] AC-098: Includes inner exception
- [ ] AC-099: Includes correlation ID
- [ ] AC-100: Handles stream errors

### Performance

- [ ] AC-101: Serialization < 1ms
- [ ] AC-102: Parsing < 5ms
- [ ] AC-103: Chunk parsing < 100μs
- [ ] AC-104: Memory < 1KB per chunk
- [ ] AC-105: Connection pooling
- [ ] AC-106: No response buffering
- [ ] AC-107: Immediate yielding
- [ ] AC-108: Source generators used

---

## Testing Requirements

### Unit Tests

```
Tests/Unit/Infrastructure/Ollama/Http/
├── OllamaHttpClientTests.cs
│   ├── Should_Configure_BaseAddress()
│   ├── Should_Configure_Timeout()
│   ├── Should_Generate_CorrelationId()
│   └── Should_Dispose_Properly()
│
├── RequestSerializerTests.cs
│   ├── Should_Map_Model()
│   ├── Should_Map_Messages()
│   ├── Should_Map_Tools()
│   ├── Should_Map_Options()
│   ├── Should_Use_SnakeCase()
│   └── Should_Omit_Nulls()
│
├── ResponseParserTests.cs
│   ├── Should_Map_Message()
│   ├── Should_Map_FinishReason_Stop()
│   ├── Should_Map_FinishReason_Length()
│   ├── Should_Map_FinishReason_ToolCalls()
│   ├── Should_Calculate_Usage()
│   └── Should_Handle_Missing_Fields()
│
├── StreamReaderTests.cs
│   ├── Should_Parse_NDJSON()
│   ├── Should_Handle_Split_Lines()
│   ├── Should_Yield_Chunks()
│   ├── Should_Detect_Final()
│   ├── Should_Handle_Empty_Lines()
│   ├── Should_Timeout_On_Stall()
│   ├── Should_Propagate_Cancellation()
│   └── Should_Dispose_Stream()
│
└── DeltaParserTests.cs
    ├── Should_Extract_ContentDelta()
    ├── Should_Extract_ToolCallDelta()
    ├── Should_Set_FinishReason()
    ├── Should_Set_Usage()
    └── Should_Track_Index()
```

### Integration Tests

```
Tests/Integration/Ollama/
├── OllamaHttpIntegrationTests.cs
│   ├── Should_Send_Request()
│   ├── Should_Receive_Response()
│   ├── Should_Stream_Response()
│   └── Should_Handle_Errors()
```

### Performance Tests

```
Tests/Performance/Ollama/
├── SerializationBenchmarks.cs
│   ├── Benchmark_Request_Serialization()
│   ├── Benchmark_Response_Parsing()
│   └── Benchmark_Chunk_Parsing()
```

---

## User Verification Steps

### Scenario 1: Send Non-Streaming Request

1. Create OllamaHttpClient with valid config
2. Create OllamaRequest with stream: false
3. Call PostAsync
4. Verify response parsed correctly

### Scenario 2: Send Streaming Request

1. Create OllamaHttpClient
2. Create OllamaRequest with stream: true
3. Call PostStreamAsync
4. Iterate over StreamReader.ReadAsync
5. Verify all chunks received
6. Verify final chunk has done: true

### Scenario 3: Handle Connection Error

1. Configure wrong endpoint
2. Attempt request
3. Verify OllamaConnectionException thrown

### Scenario 4: Handle Timeout

1. Configure very short timeout
2. Send long-running request
3. Verify OllamaTimeoutException thrown

### Scenario 5: Cancel Request

1. Start streaming request
2. Cancel via CancellationToken
3. Verify stream disposed
4. Verify no resource leaks

### Scenario 6: Parse Malformed Response

1. Mock malformed JSON response
2. Attempt to parse
3. Verify OllamaParseException thrown

### Scenario 7: Split Line Handling

1. Mock stream with line split across reads
2. Process via StreamReader
3. Verify line reconstructed correctly

### Scenario 8: Empty Content Delta

1. Process chunk with empty content
2. Verify handled gracefully

---

## Implementation Prompt

### File Structure

```
src/AgenticCoder.Infrastructure/Ollama/
├── Http/
│   ├── OllamaHttpClient.cs
│   └── OllamaHttpClientFactory.cs
├── Serialization/
│   ├── RequestSerializer.cs
│   ├── ResponseParser.cs
│   ├── DeltaParser.cs
│   └── OllamaJsonContext.cs
├── Streaming/
│   └── OllamaStreamReader.cs
└── Models/
    ├── OllamaRequest.cs
    ├── OllamaResponse.cs
    ├── OllamaMessage.cs
    ├── OllamaStreamChunk.cs
    └── OllamaOptions.cs
```

### OllamaHttpClient Implementation

```csharp
namespace AgenticCoder.Infrastructure.Ollama.Http;

public sealed class OllamaHttpClient : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<OllamaHttpClient> _logger;
    
    public string CorrelationId { get; } = Guid.NewGuid().ToString();
    
    public async Task<TResponse> PostAsync<TResponse>(
        string endpoint,
        object request,
        CancellationToken cancellationToken = default)
    {
        using var scope = _logger.BeginScope(new { CorrelationId });
        
        var json = RequestSerializer.Serialize(request);
        using var content = new StringContent(json, Encoding.UTF8, "application/json");
        
        var stopwatch = Stopwatch.StartNew();
        using var response = await _httpClient.PostAsync(endpoint, content, cancellationToken);
        
        _logger.LogDebug("POST {Endpoint} completed in {ElapsedMs}ms with status {StatusCode}",
            endpoint, stopwatch.ElapsedMilliseconds, (int)response.StatusCode);
        
        await EnsureSuccessAsync(response, cancellationToken);
        
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        return ResponseParser.Parse<TResponse>(body);
    }
    
    public async Task<Stream> PostStreamAsync(
        string endpoint,
        object request,
        CancellationToken cancellationToken = default)
    {
        var json = RequestSerializer.Serialize(request);
        using var content = new StringContent(json, Encoding.UTF8, "application/json");
        
        var response = await _httpClient.PostAsync(
            endpoint,
            content,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);
        
        await EnsureSuccessAsync(response, cancellationToken);
        
        return await response.Content.ReadAsStreamAsync(cancellationToken);
    }
    
    // Additional implementation...
}
```

### Error Codes

| Code | Message |
|------|---------|
| ACODE-OLM-HTTP-001 | Failed to establish connection |
| ACODE-OLM-HTTP-002 | Request timed out |
| ACODE-OLM-HTTP-003 | HTTP error: {StatusCode} |
| ACODE-OLM-HTTP-004 | Failed to parse response |
| ACODE-OLM-HTTP-005 | Stream read error |
| ACODE-OLM-HTTP-006 | Stream timeout |

### Implementation Checklist

1. [ ] Create OllamaRequest model
2. [ ] Create OllamaResponse model
3. [ ] Create OllamaStreamChunk model
4. [ ] Create OllamaJsonContext source generator
5. [ ] Implement RequestSerializer
6. [ ] Implement ResponseParser
7. [ ] Implement DeltaParser
8. [ ] Implement OllamaHttpClient
9. [ ] Implement OllamaStreamReader
10. [ ] Write unit tests
11. [ ] Write integration tests
12. [ ] Add XML documentation

### Dependencies

- Task 005 (Provider adapter)
- Task 004.a (Message types)
- Task 004.b (Response types)
- System.Text.Json

### Verification Command

```bash
dotnet test --filter "FullyQualifiedName~Ollama.Http"
```

---

**End of Task 005.a Specification**