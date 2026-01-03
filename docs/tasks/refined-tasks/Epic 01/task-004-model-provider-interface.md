# Task 004: Model Provider Interface

**Priority:** P0 (Critical)  
**Tier:** Foundation  
**Complexity:** 13 (Fibonacci)  
**Phase:** 1 — Model Runtime, Inference, Tool-Calling Contract  
**Dependencies:** Task 001 (Operating Modes), Task 002 (Config Contract), Task 003.c (Audit)  

---

## Description

### Business Value

The Model Provider Interface is the central abstraction that enables Agentic Coding Bot to communicate with local language model runtimes. This interface decouples the application logic from specific model serving technologies (Ollama, vLLM), enabling flexibility in deployment scenarios while maintaining a consistent programming model for all inference operations.

Without this abstraction, the application would be tightly coupled to a specific runtime, making it impossible to support different deployment scenarios (developer laptops with Ollama, high-performance servers with vLLM). The interface also enables testing by allowing mock providers, and future extensibility for additional local runtimes without modifying core application code.

### Scope

This task defines the core abstractions for model interaction:

1. **IModelProvider Interface:** The primary contract for model providers. Defines synchronous completion, streaming, health checking, and capability reporting. All concrete providers (Ollama, vLLM) MUST implement this interface.

2. **Message Types:** Defines `ChatMessage`, `MessageRole`, `ToolCall`, `ToolResult`, and related types that represent the conversation structure passed to and from models.

3. **Request/Response Types:** Defines `ChatRequest` (what goes to the model) and `ChatResponse` (what comes back), including all parameters, tool definitions, and model configuration.

4. **Usage Reporting:** Defines `UsageInfo` for token counting and resource tracking. Essential for debugging, optimization, and understanding model behavior.

5. **Provider Registry:** A central registry that manages provider instances, enabling lookup by ID and coordinated lifecycle management.

### Integration Points

- **Task 001 (Operating Modes):** Provider behavior MUST respect operating mode. Local-Only mode requires local-only providers. Air-gapped mode requires all resources pre-loaded.
- **Task 002 (Config Contract):** Provider configuration is specified in `.agent/config.yml` under the `model_providers` section.
- **Task 002.b (Parser/Validator):** Config parser loads provider settings.
- **Task 003.c (Audit):** All inference operations MUST be audited with appropriate event types.
- **Task 005 (Ollama Provider):** Implements IModelProvider for Ollama runtime.
- **Task 006 (vLLM Provider):** Implements IModelProvider for vLLM runtime.
- **Task 007 (Tool Schema):** Tool definitions use types defined here.
- **Task 009 (Routing):** Router selects providers based on this interface.

### Failure Modes

| Failure | Impact | Mitigation |
|---------|--------|------------|
| Provider unavailable | Cannot perform inference | Health check, fallback providers |
| Request timeout | Operation delayed/failed | Configurable timeout, retry |
| Invalid response format | Cannot parse model output | Retry with feedback, error handling |
| Out of memory | Provider crashes | Token limits, input truncation |
| Provider crashes mid-stream | Partial response, state corruption | Stream error handling, cleanup |
| Configuration invalid | Cannot start | Validation at startup |
| Provider version mismatch | Unexpected behavior | Version detection, compatibility check |

### Assumptions

1. Model providers expose HTTP-based APIs (REST or similar)
2. Providers support chat completion semantics
3. Providers can optionally support streaming
4. Providers report token usage
5. Tool calls are represented as structured data (JSON)
6. Multiple providers may be configured simultaneously
7. Providers may have different capabilities
8. Network latency to localhost is negligible
9. Provider responses are UTF-8 encoded

### Security Considerations

The Model Provider Interface is a critical security boundary. All data flowing through this interface may be processed by an LLM, which means:

1. Sensitive data in prompts MUST be carefully controlled
2. Model outputs MUST be treated as untrusted
3. Tool calls from models MUST be validated before execution
4. Provider credentials (if any) MUST be secured
5. Audit logs MUST NOT contain full prompt/response content

---

## Glossary / Terms

| Term | Definition |
|------|------------|
| Model Provider | Component that sends requests to and receives responses from a model runtime |
| Chat Completion | API pattern where a list of messages is sent and a response message is returned |
| Streaming | Response delivery pattern where tokens are sent incrementally |
| Tool Call | Model's request to invoke a defined tool/function |
| Tool Result | Response to a tool call that is fed back to the model |
| Message Role | Classification of message source: System, User, Assistant, or Tool |
| Token | Smallest unit of text processed by the model |
| Prompt Tokens | Tokens in the input (messages sent to model) |
| Completion Tokens | Tokens in the output (model's response) |
| Finish Reason | Why the model stopped generating: Stop, Length, ToolCalls, Error |
| Provider Registry | Central store of available model provider instances |
| Model Parameters | Configuration values for model behavior (temperature, max tokens, etc.) |
| Capability | Feature a provider supports (streaming, tools, structured output) |
| Health Check | Verification that a provider is available and responsive |
| Backpressure | Flow control mechanism for streaming to prevent buffer overflow |
| Timeout | Maximum duration to wait for a response |

---

## Out of Scope

The following items are explicitly NOT part of this task:

- Concrete provider implementations (Ollama = Task 005, vLLM = Task 006)
- Tool schema definitions (Task 007)
- Prompt templates (Task 008)
- Routing logic (Task 009)
- External LLM API support (OpenAI, Anthropic, etc.)
- Model downloading or management
- GPU resource allocation
- Token counting implementation (provider-specific)
- Response caching
- Request batching
- Rate limiting
- Cost tracking beyond usage reporting
- Multi-modal (image, audio) support
- Embeddings generation
- Fine-tuning APIs

---

## Functional Requirements

### IModelProvider Interface (FR-004-01 to FR-004-25)

| ID | Requirement |
|----|-------------|
| FR-004-01 | System MUST define IModelProvider interface |
| FR-004-02 | IModelProvider MUST have ProviderId property |
| FR-004-03 | ProviderId MUST be unique string identifier |
| FR-004-04 | IModelProvider MUST have IsAvailableAsync method |
| FR-004-05 | IsAvailableAsync MUST return health check result |
| FR-004-06 | IsAvailableAsync MUST accept CancellationToken |
| FR-004-07 | IsAvailableAsync MUST timeout within 5 seconds |
| FR-004-08 | IModelProvider MUST have CompleteAsync method |
| FR-004-09 | CompleteAsync MUST accept ChatRequest parameter |
| FR-004-10 | CompleteAsync MUST accept CancellationToken |
| FR-004-11 | CompleteAsync MUST return ChatResponse |
| FR-004-12 | IModelProvider MUST have StreamAsync method |
| FR-004-13 | StreamAsync MUST return IAsyncEnumerable |
| FR-004-14 | StreamAsync MUST yield StreamingChunk |
| FR-004-15 | StreamAsync MUST accept CancellationToken |
| FR-004-16 | IModelProvider MUST have GetCapabilities method |
| FR-004-17 | GetCapabilities MUST return ProviderCapabilities |
| FR-004-18 | IModelProvider MUST have GetModelInfo method |
| FR-004-19 | GetModelInfo MUST return available model metadata |
| FR-004-20 | IModelProvider MUST be disposable |
| FR-004-21 | Disposal MUST clean up resources |
| FR-004-22 | IModelProvider MUST support timeout configuration |
| FR-004-23 | Default timeout MUST be 120 seconds |
| FR-004-24 | Timeout MUST be configurable per request |
| FR-004-25 | IModelProvider MUST NOT make external network calls |

### Message Types (FR-004-26 to FR-004-45)

| ID | Requirement |
|----|-------------|
| FR-004-26 | System MUST define ChatMessage record |
| FR-004-27 | ChatMessage MUST have Role property |
| FR-004-28 | ChatMessage MUST have Content property |
| FR-004-29 | ChatMessage Content MAY be null (tool-call-only) |
| FR-004-30 | ChatMessage MUST have ToolCalls property |
| FR-004-31 | ToolCalls MUST be IReadOnlyList<ToolCall> |
| FR-004-32 | ToolCalls MAY be null or empty |
| FR-004-33 | System MUST define MessageRole enum |
| FR-004-34 | MessageRole MUST include System value |
| FR-004-35 | MessageRole MUST include User value |
| FR-004-36 | MessageRole MUST include Assistant value |
| FR-004-37 | MessageRole MUST include Tool value |
| FR-004-38 | System MUST define ToolCall record |
| FR-004-39 | ToolCall MUST have Id property |
| FR-004-40 | ToolCall MUST have Name property |
| FR-004-41 | ToolCall MUST have Arguments property |
| FR-004-42 | Arguments MUST be JsonElement type |
| FR-004-43 | System MUST define ToolResult record |
| FR-004-44 | ToolResult MUST have ToolCallId property |
| FR-004-45 | ToolResult MUST have Result and IsError properties |

### Request Types (FR-004-46 to FR-004-65)

| ID | Requirement |
|----|-------------|
| FR-004-46 | System MUST define ChatRequest record |
| FR-004-47 | ChatRequest MUST have Messages property |
| FR-004-48 | Messages MUST be IReadOnlyList<ChatMessage> |
| FR-004-49 | Messages MUST NOT be null or empty |
| FR-004-50 | ChatRequest MUST have Tools property |
| FR-004-51 | Tools MUST be IReadOnlyList<ToolDefinition> |
| FR-004-52 | Tools MAY be null or empty |
| FR-004-53 | ChatRequest MUST have Parameters property |
| FR-004-54 | Parameters MUST be ModelParameters type |
| FR-004-55 | System MUST define ModelParameters record |
| FR-004-56 | ModelParameters MUST have Model property |
| FR-004-57 | Model MUST be model identifier string |
| FR-004-58 | ModelParameters MUST have Temperature property |
| FR-004-59 | Temperature MUST default to 0.7 |
| FR-004-60 | Temperature MUST be in range [0.0, 2.0] |
| FR-004-61 | ModelParameters MUST have MaxTokens property |
| FR-004-62 | MaxTokens MUST be nullable (use model default) |
| FR-004-63 | ModelParameters MUST have TopP property |
| FR-004-64 | TopP MUST default to 1.0 |
| FR-004-65 | ModelParameters MUST have StopSequences property |

### Response Types (FR-004-66 to FR-004-85)

| ID | Requirement |
|----|-------------|
| FR-004-66 | System MUST define ChatResponse record |
| FR-004-67 | ChatResponse MUST have Message property |
| FR-004-68 | Message MUST be ChatMessage type |
| FR-004-69 | ChatResponse MUST have Usage property |
| FR-004-70 | Usage MUST be UsageInfo type |
| FR-004-71 | ChatResponse MUST have FinishReason property |
| FR-004-72 | FinishReason MUST be enum |
| FR-004-73 | FinishReason MUST include Stop value |
| FR-004-74 | FinishReason MUST include Length value |
| FR-004-75 | FinishReason MUST include ToolCalls value |
| FR-004-76 | FinishReason MUST include Error value |
| FR-004-77 | FinishReason MUST include Cancelled value |
| FR-004-78 | System MUST define StreamingChunk record |
| FR-004-79 | StreamingChunk MUST have Delta property |
| FR-004-80 | Delta MUST contain incremental content |
| FR-004-81 | StreamingChunk MUST have IsComplete property |
| FR-004-82 | StreamingChunk MUST have ToolCallDeltas property |
| FR-004-83 | Final chunk MUST have FinishReason |
| FR-004-84 | Final chunk MUST have Usage if available |
| FR-004-85 | Streaming MUST support cancellation |

### Usage Reporting (FR-004-86 to FR-004-100)

| ID | Requirement |
|----|-------------|
| FR-004-86 | System MUST define UsageInfo record |
| FR-004-87 | UsageInfo MUST have PromptTokens property |
| FR-004-88 | UsageInfo MUST have CompletionTokens property |
| FR-004-89 | UsageInfo MUST have TotalTokens property |
| FR-004-90 | TotalTokens MUST equal PromptTokens + CompletionTokens |
| FR-004-91 | UsageInfo MAY have TimeToFirstToken property |
| FR-004-92 | UsageInfo MAY have TotalDuration property |
| FR-004-93 | Duration MUST be TimeSpan type |
| FR-004-94 | Usage MUST be reported for every completion |
| FR-004-95 | Streaming MUST report usage in final chunk |
| FR-004-96 | Usage MUST be auditable per Task 003.c |
| FR-004-97 | Usage MAY include model-specific metadata |
| FR-004-98 | Zero tokens MUST be valid (empty response) |
| FR-004-99 | Provider MAY estimate tokens if not reported |
| FR-004-100 | Estimated tokens MUST be marked as estimated |

### Provider Registry (FR-004-101 to FR-004-115)

| ID | Requirement |
|----|-------------|
| FR-004-101 | System MUST define IProviderRegistry interface |
| FR-004-102 | IProviderRegistry MUST have Register method |
| FR-004-103 | Register MUST accept IModelProvider |
| FR-004-104 | Register MUST fail if ProviderId exists |
| FR-004-105 | IProviderRegistry MUST have Get method |
| FR-004-106 | Get MUST accept provider ID string |
| FR-004-107 | Get MUST return IModelProvider or null |
| FR-004-108 | IProviderRegistry MUST have GetAll method |
| FR-004-109 | GetAll MUST return all registered providers |
| FR-004-110 | IProviderRegistry MUST have GetDefault method |
| FR-004-111 | GetDefault MUST return configured default |
| FR-004-112 | Default MUST be configurable in config file |
| FR-004-113 | IProviderRegistry MUST be singleton |
| FR-004-114 | Registry MUST support provider disposal |
| FR-004-115 | Registry disposal MUST dispose all providers |

---

## Non-Functional Requirements

### Performance (NFR-004-01 to NFR-004-15)

| ID | Category | Requirement |
|----|----------|-------------|
| NFR-004-01 | Performance | Interface overhead MUST be < 1ms |
| NFR-004-02 | Performance | Request serialization MUST be < 10ms |
| NFR-004-03 | Performance | Response deserialization MUST be < 10ms |
| NFR-004-04 | Performance | Streaming latency MUST be < 50ms per chunk |
| NFR-004-05 | Performance | Registry lookup MUST be O(1) |
| NFR-004-06 | Performance | Health check MUST complete in < 5s |
| NFR-004-07 | Performance | Memory per request MUST be < 10MB |
| NFR-004-08 | Performance | Streaming MUST NOT buffer full response |
| NFR-004-09 | Performance | Cancellation MUST be immediate |
| NFR-004-10 | Performance | Large responses MUST stream efficiently |
| NFR-004-11 | Performance | JSON parsing MUST use System.Text.Json |
| NFR-004-12 | Performance | No reflection in hot paths |
| NFR-004-13 | Performance | Connection pooling MUST be used |
| NFR-004-14 | Performance | HTTP/2 SHOULD be preferred |
| NFR-004-15 | Performance | Keep-alive MUST be used |

### Security (NFR-004-16 to NFR-004-30)

| ID | Category | Requirement |
|----|----------|-------------|
| NFR-004-16 | Security | Providers MUST be local only |
| NFR-004-17 | Security | No external network calls allowed |
| NFR-004-18 | Security | Provider URLs validated as localhost |
| NFR-004-19 | Security | Tool call arguments MUST be validated |
| NFR-004-20 | Security | Prompts MUST NOT be logged in full |
| NFR-004-21 | Security | Responses MUST NOT be logged in full |
| NFR-004-22 | Security | Usage metrics MAY be logged |
| NFR-004-23 | Security | Error messages MUST NOT leak prompts |
| NFR-004-24 | Security | Provider credentials MUST be secured |
| NFR-004-25 | Security | TLS SHOULD be used even for localhost |
| NFR-004-26 | Security | Certificate validation configurable |
| NFR-004-27 | Security | No secrets in error messages |
| NFR-004-28 | Security | Model output treated as untrusted |
| NFR-004-29 | Security | Input validation required |
| NFR-004-30 | Security | Audit events per Task 003.c |

### Reliability (NFR-004-31 to NFR-004-45)

| ID | Category | Requirement |
|----|----------|-------------|
| NFR-004-31 | Reliability | Timeouts MUST be enforced |
| NFR-004-32 | Reliability | Cancellation MUST be honored |
| NFR-004-33 | Reliability | Connection failures MUST be reported |
| NFR-004-34 | Reliability | Retry logic in consumer, not interface |
| NFR-004-35 | Reliability | Stream errors MUST be propagated |
| NFR-004-36 | Reliability | Partial responses MUST be usable |
| NFR-004-37 | Reliability | Provider crash MUST NOT crash app |
| NFR-004-38 | Reliability | Disposal MUST be idempotent |
| NFR-004-39 | Reliability | Thread safety MUST be documented |
| NFR-004-40 | Reliability | Concurrent requests MUST work |
| NFR-004-41 | Reliability | Health check MUST be lightweight |
| NFR-004-42 | Reliability | Registry MUST be thread-safe |
| NFR-004-43 | Reliability | Invalid responses MUST throw typed exceptions |
| NFR-004-44 | Reliability | Exception types MUST be documented |
| NFR-004-45 | Reliability | All resources MUST be cleaned up |

### Maintainability (NFR-004-46 to NFR-004-55)

| ID | Category | Requirement |
|----|----------|-------------|
| NFR-004-46 | Maintainability | Interfaces MUST be in Domain layer |
| NFR-004-47 | Maintainability | Types MUST be immutable records |
| NFR-004-48 | Maintainability | Nullable reference types MUST be enabled |
| NFR-004-49 | Maintainability | XML documentation required |
| NFR-004-50 | Maintainability | Interface versioning strategy defined |
| NFR-004-51 | Maintainability | Breaking changes MUST be documented |
| NFR-004-52 | Maintainability | Test mocks MUST be easy to create |
| NFR-004-53 | Maintainability | Extension points MUST be documented |
| NFR-004-54 | Maintainability | Code MUST follow style guide |
| NFR-004-55 | Maintainability | Complexity MUST be low (< 10 per method) |

---

## User Manual Documentation

### Overview

The Model Provider Interface defines how Agentic Coding Bot communicates with local language model runtimes. This abstraction enables support for multiple model providers (Ollama, vLLM) while maintaining consistent behavior across the application.

### Architecture

```
Application Layer
       │
       ▼
┌─────────────────────┐
│  IModelProvider     │ ◄─── Interface (Domain Layer)
└─────────────────────┘
       │
       ▼
┌─────────────────────┐
│  Provider Registry  │ ◄─── Manages instances (Application Layer)
└─────────────────────┘
       │
       ├──────────────┐
       ▼              ▼
┌───────────┐  ┌───────────┐
│  Ollama   │  │   vLLM    │ ◄─── Implementations (Infrastructure)
│  Provider │  │  Provider │
└───────────┘  └───────────┘
```

### Configuration

Configure model providers in `.agent/config.yml`:

```yaml
model_providers:
  # Default provider when none specified
  default: ollama
  
  providers:
    ollama:
      type: ollama
      endpoint: http://localhost:11434
      timeout: 120
      models:
        - name: qwen2.5-coder:32b
          context_length: 32768
        - name: llama3.1:70b
          context_length: 131072
    
    vllm:
      type: vllm
      endpoint: http://localhost:8000
      timeout: 300
      health_check_interval: 30
      models:
        - name: Qwen/Qwen2.5-Coder-32B-Instruct
          context_length: 32768
```

### Message Types

#### MessageRole

Indicates the source of a message in the conversation:

| Role | Description |
|------|-------------|
| `System` | System prompt defining behavior |
| `User` | User input or request |
| `Assistant` | Model's response |
| `Tool` | Result of a tool call |

#### ChatMessage

```csharp
var message = new ChatMessage(
    Role: MessageRole.User,
    Content: "Write a hello world program in C#",
    ToolCalls: null
);
```

#### ToolCall

Model's request to invoke a tool:

```csharp
var toolCall = new ToolCall(
    Id: "call_abc123",
    Name: "write_file",
    Arguments: JsonSerializer.SerializeToElement(new {
        path = "hello.cs",
        content = "Console.WriteLine(\"Hello, World!\");"
    })
);
```

#### ToolResult

Response to a tool call:

```csharp
var toolResult = new ToolResult(
    ToolCallId: "call_abc123",
    Result: "File written successfully",
    IsError: false
);
```

### Making Requests

#### Basic Completion

```csharp
// Get provider from registry
var provider = registry.GetDefault();

// Create request
var request = new ChatRequest(
    Messages: new[] {
        new ChatMessage(MessageRole.System, "You are a helpful assistant.", null),
        new ChatMessage(MessageRole.User, "Hello!", null)
    },
    Tools: null,
    Parameters: new ModelParameters(Model: "qwen2.5-coder:32b")
);

// Execute
var response = await provider.CompleteAsync(request, cancellationToken);

// Use response
Console.WriteLine(response.Message.Content);
Console.WriteLine($"Tokens: {response.Usage.TotalTokens}");
```

#### Streaming Response

```csharp
var contentBuilder = new StringBuilder();

await foreach (var chunk in provider.StreamAsync(request, cancellationToken))
{
    if (!string.IsNullOrEmpty(chunk.Delta))
    {
        contentBuilder.Append(chunk.Delta);
        Console.Write(chunk.Delta); // Real-time output
    }
    
    if (chunk.IsComplete)
    {
        Console.WriteLine($"\nFinish: {chunk.FinishReason}");
        Console.WriteLine($"Tokens: {chunk.Usage?.TotalTokens}");
    }
}
```

#### With Tool Calls

```csharp
var request = new ChatRequest(
    Messages: messages,
    Tools: new[] {
        new ToolDefinition(
            Name: "write_file",
            Description: "Write content to a file",
            Parameters: JsonSchema.FromType<WriteFileArgs>()
        )
    },
    Parameters: parameters
);

var response = await provider.CompleteAsync(request, cancellationToken);

if (response.FinishReason == FinishReason.ToolCalls)
{
    foreach (var toolCall in response.Message.ToolCalls!)
    {
        // Execute tool
        var result = await ExecuteToolAsync(toolCall);
        
        // Add tool result to messages
        messages.Add(new ChatMessage(
            MessageRole.Tool,
            result.Result,
            null
        ) { ToolCallId = result.ToolCallId });
    }
    
    // Continue conversation with tool results
    response = await provider.CompleteAsync(
        request with { Messages = messages },
        cancellationToken
    );
}
```

### Provider Registry

#### Getting Providers

```csharp
// Get default provider
var defaultProvider = registry.GetDefault();

// Get specific provider
var ollamaProvider = registry.Get("ollama");
var vllmProvider = registry.Get("vllm");

// Get all providers
foreach (var provider in registry.GetAll())
{
    Console.WriteLine($"{provider.ProviderId}: {await provider.IsAvailableAsync()}");
}
```

#### Health Checking

```csharp
// Check single provider
if (await provider.IsAvailableAsync(cancellationToken))
{
    Console.WriteLine("Provider is available");
}
else
{
    Console.WriteLine("Provider is not available");
}

// Check all providers
foreach (var p in registry.GetAll())
{
    var available = await p.IsAvailableAsync(cancellationToken);
    Console.WriteLine($"{p.ProviderId}: {(available ? "✓" : "✗")}");
}
```

### Model Parameters

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `Model` | string | required | Model identifier |
| `Temperature` | float | 0.7 | Sampling temperature (0.0-2.0) |
| `MaxTokens` | int? | null | Maximum response tokens |
| `TopP` | float | 1.0 | Nucleus sampling parameter |
| `StopSequences` | string[] | null | Sequences that stop generation |
| `Seed` | int? | null | Random seed for reproducibility |

### Usage Reporting

Every response includes usage information:

```csharp
var usage = response.Usage;

Console.WriteLine($"Prompt tokens: {usage.PromptTokens}");
Console.WriteLine($"Completion tokens: {usage.CompletionTokens}");
Console.WriteLine($"Total tokens: {usage.TotalTokens}");
Console.WriteLine($"Time to first token: {usage.TimeToFirstToken}");
Console.WriteLine($"Total duration: {usage.TotalDuration}");
```

### Error Handling

```csharp
try
{
    var response = await provider.CompleteAsync(request, cancellationToken);
}
catch (ProviderUnavailableException ex)
{
    // Provider not running or unreachable
    Console.WriteLine($"Provider unavailable: {ex.Message}");
}
catch (ProviderTimeoutException ex)
{
    // Request timed out
    Console.WriteLine($"Timeout after {ex.Timeout}");
}
catch (ProviderResponseException ex)
{
    // Invalid response from provider
    Console.WriteLine($"Invalid response: {ex.Message}");
}
catch (OperationCanceledException)
{
    // Request was cancelled
    Console.WriteLine("Request cancelled");
}
```

### CLI Commands

```bash
# List configured providers
agentic-coder provider list

# Check provider health
agentic-coder provider health

# Check specific provider
agentic-coder provider health --provider ollama

# Show provider info
agentic-coder provider info ollama

# Test provider with simple prompt
agentic-coder provider test ollama "Hello, world!"
```

### Troubleshooting

#### Provider Not Available

1. Verify provider is running:
   - Ollama: `ollama list`
   - vLLM: `curl http://localhost:8000/health`

2. Check endpoint configuration in `.agent/config.yml`

3. Verify no firewall blocking localhost connections

#### Timeout Errors

1. Increase timeout in configuration:
   ```yaml
   model_providers:
     providers:
       ollama:
         timeout: 300  # 5 minutes
   ```

2. Use streaming for long operations

3. Check model is loaded (first request may be slow)

#### Invalid Response

1. Check model supports tool calling
2. Verify model is compatible with chat completion format
3. Check for model-specific quirks

---

## Acceptance Criteria

### IModelProvider Interface

- [ ] AC-001: IModelProvider interface defined
- [ ] AC-002: ProviderId property exists
- [ ] AC-003: ProviderId is string type
- [ ] AC-004: IsAvailableAsync method exists
- [ ] AC-005: IsAvailableAsync accepts CancellationToken
- [ ] AC-006: IsAvailableAsync returns Task<bool>
- [ ] AC-007: IsAvailableAsync times out in 5 seconds
- [ ] AC-008: CompleteAsync method exists
- [ ] AC-009: CompleteAsync accepts ChatRequest
- [ ] AC-010: CompleteAsync accepts CancellationToken
- [ ] AC-011: CompleteAsync returns Task<ChatResponse>
- [ ] AC-012: StreamAsync method exists
- [ ] AC-013: StreamAsync returns IAsyncEnumerable
- [ ] AC-014: StreamAsync yields StreamingChunk
- [ ] AC-015: StreamAsync accepts CancellationToken
- [ ] AC-016: GetCapabilities method exists
- [ ] AC-017: GetCapabilities returns ProviderCapabilities
- [ ] AC-018: GetModelInfo method exists
- [ ] AC-019: IModelProvider implements IAsyncDisposable
- [ ] AC-020: Disposal cleans up resources

### Message Types

- [ ] AC-021: ChatMessage record defined
- [ ] AC-022: ChatMessage has Role property
- [ ] AC-023: ChatMessage has Content property
- [ ] AC-024: ChatMessage Content is nullable
- [ ] AC-025: ChatMessage has ToolCalls property
- [ ] AC-026: ToolCalls is IReadOnlyList<ToolCall>
- [ ] AC-027: MessageRole enum defined
- [ ] AC-028: MessageRole.System exists
- [ ] AC-029: MessageRole.User exists
- [ ] AC-030: MessageRole.Assistant exists
- [ ] AC-031: MessageRole.Tool exists
- [ ] AC-032: ToolCall record defined
- [ ] AC-033: ToolCall has Id property
- [ ] AC-034: ToolCall has Name property
- [ ] AC-035: ToolCall has Arguments property
- [ ] AC-036: Arguments is JsonElement
- [ ] AC-037: ToolResult record defined
- [ ] AC-038: ToolResult has ToolCallId
- [ ] AC-039: ToolResult has Result
- [ ] AC-040: ToolResult has IsError

### Request Types

- [ ] AC-041: ChatRequest record defined
- [ ] AC-042: ChatRequest has Messages property
- [ ] AC-043: Messages is IReadOnlyList<ChatMessage>
- [ ] AC-044: ChatRequest has Tools property
- [ ] AC-045: Tools is IReadOnlyList<ToolDefinition>
- [ ] AC-046: ChatRequest has Parameters property
- [ ] AC-047: Parameters is ModelParameters
- [ ] AC-048: ModelParameters record defined
- [ ] AC-049: ModelParameters has Model property
- [ ] AC-050: Model is required string
- [ ] AC-051: ModelParameters has Temperature
- [ ] AC-052: Temperature defaults to 0.7
- [ ] AC-053: Temperature range is [0.0, 2.0]
- [ ] AC-054: ModelParameters has MaxTokens
- [ ] AC-055: MaxTokens is nullable int
- [ ] AC-056: ModelParameters has TopP
- [ ] AC-057: TopP defaults to 1.0
- [ ] AC-058: ModelParameters has StopSequences
- [ ] AC-059: StopSequences is nullable array
- [ ] AC-060: ModelParameters has Seed

### Response Types

- [ ] AC-061: ChatResponse record defined
- [ ] AC-062: ChatResponse has Message property
- [ ] AC-063: Message is ChatMessage
- [ ] AC-064: ChatResponse has Usage property
- [ ] AC-065: Usage is UsageInfo
- [ ] AC-066: ChatResponse has FinishReason
- [ ] AC-067: FinishReason enum defined
- [ ] AC-068: FinishReason.Stop exists
- [ ] AC-069: FinishReason.Length exists
- [ ] AC-070: FinishReason.ToolCalls exists
- [ ] AC-071: FinishReason.Error exists
- [ ] AC-072: FinishReason.Cancelled exists
- [ ] AC-073: StreamingChunk record defined
- [ ] AC-074: StreamingChunk has Delta
- [ ] AC-075: StreamingChunk has IsComplete
- [ ] AC-076: StreamingChunk has ToolCallDeltas
- [ ] AC-077: Final chunk has FinishReason
- [ ] AC-078: Final chunk has Usage
- [ ] AC-079: Streaming supports cancellation
- [ ] AC-080: Streaming propagates errors

### Usage Reporting

- [ ] AC-081: UsageInfo record defined
- [ ] AC-082: UsageInfo has PromptTokens
- [ ] AC-083: UsageInfo has CompletionTokens
- [ ] AC-084: UsageInfo has TotalTokens
- [ ] AC-085: TotalTokens = Prompt + Completion
- [ ] AC-086: UsageInfo has TimeToFirstToken
- [ ] AC-087: UsageInfo has TotalDuration
- [ ] AC-088: Duration is TimeSpan
- [ ] AC-089: Usage reported for completions
- [ ] AC-090: Usage in final streaming chunk

### Provider Registry

- [ ] AC-091: IProviderRegistry interface defined
- [ ] AC-092: Register method exists
- [ ] AC-093: Register accepts IModelProvider
- [ ] AC-094: Register fails on duplicate ID
- [ ] AC-095: Get method exists
- [ ] AC-096: Get accepts string ID
- [ ] AC-097: Get returns IModelProvider or null
- [ ] AC-098: GetAll method exists
- [ ] AC-099: GetAll returns all providers
- [ ] AC-100: GetDefault method exists
- [ ] AC-101: GetDefault returns configured default
- [ ] AC-102: Default configurable in config
- [ ] AC-103: Registry is singleton
- [ ] AC-104: Registry supports disposal
- [ ] AC-105: Disposal disposes all providers

### Exceptions

- [ ] AC-106: ProviderUnavailableException defined
- [ ] AC-107: ProviderTimeoutException defined
- [ ] AC-108: ProviderResponseException defined
- [ ] AC-109: ProviderConfigurationException defined
- [ ] AC-110: All exceptions have message
- [ ] AC-111: All exceptions have InnerException
- [ ] AC-112: Exceptions are serializable
- [ ] AC-113: Exceptions include provider ID
- [ ] AC-114: Timeout exception includes timeout value
- [ ] AC-115: Response exception includes status code

### Configuration

- [ ] AC-116: Config section model_providers exists
- [ ] AC-117: Default provider configurable
- [ ] AC-118: Multiple providers configurable
- [ ] AC-119: Endpoint configurable per provider
- [ ] AC-120: Timeout configurable per provider
- [ ] AC-121: Model list configurable
- [ ] AC-122: Config validation at startup
- [ ] AC-123: Invalid config fails with clear error
- [ ] AC-124: Missing config uses defaults
- [ ] AC-125: Config changes require restart

### CLI Commands

- [ ] AC-126: provider list command exists
- [ ] AC-127: provider health command exists
- [ ] AC-128: provider info command exists
- [ ] AC-129: provider test command exists
- [ ] AC-130: Commands show helpful output
- [ ] AC-131: Commands handle errors gracefully
- [ ] AC-132: Exit codes are correct

### Performance

- [ ] AC-133: Interface overhead < 1ms
- [ ] AC-134: Registry lookup is O(1)
- [ ] AC-135: Health check < 5 seconds
- [ ] AC-136: Streaming is efficient
- [ ] AC-137: Memory usage reasonable
- [ ] AC-138: No hot path allocations

### Security

- [ ] AC-139: Local only providers
- [ ] AC-140: No external network calls
- [ ] AC-141: Endpoint validation
- [ ] AC-142: Prompts not logged in full
- [ ] AC-143: Responses not logged in full
- [ ] AC-144: Audit events generated

### Documentation

- [ ] AC-145: Interface documented
- [ ] AC-146: All types documented
- [ ] AC-147: Configuration documented
- [ ] AC-148: CLI commands documented
- [ ] AC-149: Examples provided
- [ ] AC-150: Troubleshooting guide exists

---

## Testing Requirements

### Unit Tests

```
Tests/Unit/Domain/Models/
├── MessageTypeTests.cs
│   ├── ChatMessage_Should_BeImmutable()
│   ├── ChatMessage_Should_AllowNullContent()
│   ├── ChatMessage_Should_AllowNullToolCalls()
│   ├── MessageRole_Should_HaveAllValues()
│   ├── ToolCall_Should_BeImmutable()
│   ├── ToolCall_Should_HaveRequiredFields()
│   ├── ToolResult_Should_BeImmutable()
│   └── ToolResult_Should_SupportErrors()
│
├── RequestTypeTests.cs
│   ├── ChatRequest_Should_RequireMessages()
│   ├── ChatRequest_Should_AllowNullTools()
│   ├── ChatRequest_Should_HaveParameters()
│   ├── ModelParameters_Should_HaveDefaults()
│   ├── ModelParameters_Should_ValidateTemperature()
│   ├── ModelParameters_Should_ValidateTopP()
│   └── ModelParameters_Should_AllowNullMaxTokens()
│
├── ResponseTypeTests.cs
│   ├── ChatResponse_Should_HaveMessage()
│   ├── ChatResponse_Should_HaveUsage()
│   ├── ChatResponse_Should_HaveFinishReason()
│   ├── FinishReason_Should_HaveAllValues()
│   ├── StreamingChunk_Should_HaveDelta()
│   ├── StreamingChunk_Should_IndicateComplete()
│   └── StreamingChunk_Should_SupportToolCalls()
│
├── UsageInfoTests.cs
│   ├── UsageInfo_Should_BeImmutable()
│   ├── UsageInfo_Should_CalculateTotal()
│   ├── UsageInfo_Should_SupportDuration()
│   └── UsageInfo_Should_AllowZeroTokens()
│
└── ProviderRegistryTests.cs
    ├── Registry_Should_RegisterProvider()
    ├── Registry_Should_RejectDuplicate()
    ├── Registry_Should_GetById()
    ├── Registry_Should_ReturnNullForUnknown()
    ├── Registry_Should_GetAll()
    ├── Registry_Should_GetDefault()
    ├── Registry_Should_BeSingleton()
    ├── Registry_Should_DisposeAll()
    └── Registry_Should_BeThreadSafe()
```

### Integration Tests

```
Tests/Integration/Models/
├── ProviderRegistryIntegrationTests.cs
│   ├── Should_LoadFromConfiguration()
│   ├── Should_UseDefaultFromConfig()
│   ├── Should_RegisterMultipleProviders()
│   ├── Should_HandleMissingProvider()
│   └── Should_DisposeOnShutdown()
│
├── MockProviderTests.cs
│   ├── MockProvider_Should_ImplementInterface()
│   ├── MockProvider_Should_ReturnConfiguredResponse()
│   ├── MockProvider_Should_SimulateStreaming()
│   ├── MockProvider_Should_SimulateToolCalls()
│   ├── MockProvider_Should_SimulateErrors()
│   └── MockProvider_Should_TrackCalls()
│
└── ConfigurationTests.cs
    ├── Should_ParseProviderConfig()
    ├── Should_ValidateEndpoints()
    ├── Should_ValidateTimeouts()
    ├── Should_UseDefaults()
    └── Should_RejectInvalidConfig()
```

### End-to-End Tests

```
Tests/E2E/Models/
├── ProviderScenarios.cs
│   ├── Scenario_ListProviders()
│   ├── Scenario_CheckProviderHealth()
│   ├── Scenario_GetProviderInfo()
│   ├── Scenario_TestProviderWithPrompt()
│   ├── Scenario_SwitchDefaultProvider()
│   └── Scenario_HandleUnavailableProvider()
```

### Performance Tests

```
Tests/Performance/Models/
├── ProviderBenchmarks.cs
│   ├── Benchmark_RegistryLookup()
│   ├── Benchmark_RequestSerialization()
│   ├── Benchmark_ResponseDeserialization()
│   ├── Benchmark_HealthCheck()
│   └── Benchmark_ConcurrentRequests()
```

### Regression Tests

```
Tests/Regression/Models/
├── InterfaceCompatibilityTests.cs
│   ├── Should_MaintainTypeCompatibility()
│   ├── Should_SerializeCorrectly()
│   ├── Should_DeserializeOldFormats()
│   └── Should_HandleNullsGracefully()
```

---

## User Verification Steps

### Scenario 1: Verify Provider List

**Objective:** Confirm provider list command shows configured providers

1. Configure providers in `.agent/config.yml`
2. Run: `agentic-coder provider list`
3. Verify each configured provider is listed
4. Verify provider IDs match configuration
5. Verify default provider is indicated

**Expected Result:**
- All configured providers listed
- Default clearly marked
- Provider types shown

### Scenario 2: Verify Provider Health Check

**Objective:** Confirm health check works for available provider

1. Start Ollama: `ollama serve`
2. Run: `agentic-coder provider health`
3. Verify Ollama shows as available (✓)
4. Stop Ollama
5. Run: `agentic-coder provider health`
6. Verify Ollama shows as unavailable (✗)

**Expected Result:**
- Available providers show ✓
- Unavailable providers show ✗
- Health check completes quickly

### Scenario 3: Verify Provider Info

**Objective:** Confirm provider info command shows details

1. Run: `agentic-coder provider info ollama`
2. Verify endpoint is shown
3. Verify configured models are listed
4. Verify timeout is shown
5. Verify capabilities are listed

**Expected Result:**
- Complete provider information displayed
- Configuration matches file
- Models listed correctly

### Scenario 4: Verify Simple Completion

**Objective:** Confirm basic completion works through interface

1. Ensure provider is running
2. Run: `agentic-coder provider test ollama "What is 2+2?"`
3. Verify response is received
4. Verify response contains answer
5. Verify usage statistics shown

**Expected Result:**
- Response received
- Coherent answer
- Token usage reported

### Scenario 5: Verify Streaming

**Objective:** Confirm streaming output works

1. Run: `agentic-coder provider test ollama "Count from 1 to 10" --stream`
2. Verify tokens appear incrementally
3. Verify complete response is received
4. Verify final usage statistics shown

**Expected Result:**
- Tokens appear one at a time
- Full response received
- Usage reported at end

### Scenario 6: Verify Timeout Handling

**Objective:** Confirm timeout is enforced

1. Configure short timeout: 1 second
2. Run complex prompt that takes longer
3. Verify timeout error is returned
4. Verify error message includes timeout value
5. Restore normal timeout

**Expected Result:**
- Operation times out
- Clear error message
- Timeout value shown

### Scenario 7: Verify Cancellation

**Objective:** Confirm cancellation works

1. Start long-running completion
2. Press Ctrl+C to cancel
3. Verify operation stops promptly
4. Verify partial result is handled
5. Verify no resource leaks

**Expected Result:**
- Cancellation is immediate
- Clean shutdown
- No hanging connections

### Scenario 8: Verify Default Provider

**Objective:** Confirm default provider is used

1. Configure default in `.agent/config.yml`
2. Run: `agentic-coder provider test "Hello"`
3. Verify default provider is used (no --provider flag)
4. Change default configuration
5. Restart and verify new default is used

**Expected Result:**
- Default provider used automatically
- Configuration change takes effect

### Scenario 9: Verify Registry Lookup

**Objective:** Confirm provider lookup by ID works

1. Run: `agentic-coder provider test ollama "Test"`
2. Verify ollama provider is used
3. Run: `agentic-coder provider test vllm "Test"`
4. Verify vllm provider is used (if configured)
5. Run: `agentic-coder provider test nonexistent "Test"`
6. Verify clear error message

**Expected Result:**
- Correct provider selected by ID
- Unknown provider gives error
- Error message is helpful

### Scenario 10: Verify Configuration Validation

**Objective:** Confirm invalid configuration is rejected

1. Set invalid endpoint in config
2. Run: `agentic-coder provider health`
3. Verify validation error
4. Fix endpoint
5. Set invalid timeout (negative)
6. Verify validation error
7. Restore valid configuration

**Expected Result:**
- Invalid config rejected
- Clear error messages
- Specific field identified

### Scenario 11: Verify Usage Reporting

**Objective:** Confirm token usage is reported

1. Run completion request
2. Verify prompt tokens reported
3. Verify completion tokens reported
4. Verify total tokens = prompt + completion
5. Verify timing information if available

**Expected Result:**
- All token counts present
- Math is correct
- Timing reported if supported

### Scenario 12: Verify Error Handling

**Objective:** Confirm errors are handled gracefully

1. Stop provider service
2. Run: `agentic-coder provider test ollama "Test"`
3. Verify ProviderUnavailableException or similar
4. Verify error message is clear
5. Verify exit code is non-zero

**Expected Result:**
- Error handled gracefully
- No crash or stack trace (unless debug)
- Correct exit code

---

## Implementation Prompt

### File Structure

```
src/
├── AgenticCoder.Domain/
│   ├── Models/
│   │   ├── IModelProvider.cs
│   │   ├── ChatMessage.cs
│   │   ├── MessageRole.cs
│   │   ├── ToolCall.cs
│   │   ├── ToolResult.cs
│   │   ├── ToolDefinition.cs
│   │   ├── ChatRequest.cs
│   │   ├── ChatResponse.cs
│   │   ├── ModelParameters.cs
│   │   ├── UsageInfo.cs
│   │   ├── FinishReason.cs
│   │   ├── StreamingChunk.cs
│   │   ├── ProviderCapabilities.cs
│   │   ├── ModelInfo.cs
│   │   └── Exceptions/
│   │       ├── ProviderException.cs
│   │       ├── ProviderUnavailableException.cs
│   │       ├── ProviderTimeoutException.cs
│   │       ├── ProviderResponseException.cs
│   │       └── ProviderConfigurationException.cs
│
├── AgenticCoder.Application/
│   ├── Models/
│   │   ├── IProviderRegistry.cs
│   │   ├── ProviderRegistry.cs
│   │   └── ProviderConfiguration.cs
│
├── AgenticCoder.Infrastructure/
│   ├── Models/
│   │   ├── HttpModelProviderBase.cs
│   │   └── MockModelProvider.cs
│
└── AgenticCoder.CLI/
    └── Commands/
        └── Provider/
            ├── ProviderListCommand.cs
            ├── ProviderHealthCommand.cs
            ├── ProviderInfoCommand.cs
            └── ProviderTestCommand.cs
```

### Core Interface Definition

```csharp
namespace AgenticCoder.Domain.Models;

/// <summary>
/// Abstraction for model inference providers.
/// All implementations MUST be local-only (no external API calls).
/// </summary>
public interface IModelProvider : IAsyncDisposable
{
    /// <summary>
    /// Unique identifier for this provider instance.
    /// </summary>
    string ProviderId { get; }
    
    /// <summary>
    /// Checks if the provider is available and responsive.
    /// MUST complete within 5 seconds.
    /// </summary>
    Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Sends a chat completion request and returns the response.
    /// </summary>
    /// <exception cref="ProviderUnavailableException">Provider not reachable</exception>
    /// <exception cref="ProviderTimeoutException">Request timed out</exception>
    /// <exception cref="ProviderResponseException">Invalid response from provider</exception>
    Task<ChatResponse> CompleteAsync(
        ChatRequest request,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Sends a chat completion request and streams the response.
    /// </summary>
    IAsyncEnumerable<StreamingChunk> StreamAsync(
        ChatRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets the capabilities of this provider.
    /// </summary>
    ProviderCapabilities GetCapabilities();
    
    /// <summary>
    /// Gets information about available models.
    /// </summary>
    Task<IReadOnlyList<ModelInfo>> GetModelsAsync(
        CancellationToken cancellationToken = default);
}
```

### Message Type Definitions

```csharp
namespace AgenticCoder.Domain.Models;

/// <summary>
/// Role of a message in the conversation.
/// </summary>
public enum MessageRole
{
    /// <summary>System prompt defining agent behavior.</summary>
    System,
    /// <summary>User input or request.</summary>
    User,
    /// <summary>Model's response.</summary>
    Assistant,
    /// <summary>Result of a tool call.</summary>
    Tool
}

/// <summary>
/// A message in the conversation.
/// </summary>
public sealed record ChatMessage
{
    public required MessageRole Role { get; init; }
    public string? Content { get; init; }
    public IReadOnlyList<ToolCall>? ToolCalls { get; init; }
    
    // For tool messages, the ID of the tool call this responds to
    public string? ToolCallId { get; init; }
}

/// <summary>
/// A tool call requested by the model.
/// </summary>
public sealed record ToolCall
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required JsonElement Arguments { get; init; }
}

/// <summary>
/// Result of executing a tool call.
/// </summary>
public sealed record ToolResult
{
    public required string ToolCallId { get; init; }
    public required string Result { get; init; }
    public bool IsError { get; init; }
}

/// <summary>
/// Definition of a tool the model can call.
/// </summary>
public sealed record ToolDefinition
{
    public required string Name { get; init; }
    public required string Description { get; init; }
    public required JsonElement Parameters { get; init; } // JSON Schema
}
```

### Request/Response Type Definitions

```csharp
namespace AgenticCoder.Domain.Models;

/// <summary>
/// Request to the model for chat completion.
/// </summary>
public sealed record ChatRequest
{
    public required IReadOnlyList<ChatMessage> Messages { get; init; }
    public IReadOnlyList<ToolDefinition>? Tools { get; init; }
    public required ModelParameters Parameters { get; init; }
}

/// <summary>
/// Parameters controlling model behavior.
/// </summary>
public sealed record ModelParameters
{
    public required string Model { get; init; }
    public float Temperature { get; init; } = 0.7f;
    public int? MaxTokens { get; init; }
    public float TopP { get; init; } = 1.0f;
    public IReadOnlyList<string>? StopSequences { get; init; }
    public int? Seed { get; init; }
    public TimeSpan? Timeout { get; init; }
}

/// <summary>
/// Response from the model.
/// </summary>
public sealed record ChatResponse
{
    public required ChatMessage Message { get; init; }
    public required UsageInfo Usage { get; init; }
    public required FinishReason FinishReason { get; init; }
}

/// <summary>
/// Reason the model stopped generating.
/// </summary>
public enum FinishReason
{
    /// <summary>Natural stop (end of response).</summary>
    Stop,
    /// <summary>Maximum tokens reached.</summary>
    Length,
    /// <summary>Model wants to call tools.</summary>
    ToolCalls,
    /// <summary>Error occurred.</summary>
    Error,
    /// <summary>Request was cancelled.</summary>
    Cancelled
}

/// <summary>
/// Chunk of streaming response.
/// </summary>
public sealed record StreamingChunk
{
    public string? Delta { get; init; }
    public IReadOnlyList<ToolCallDelta>? ToolCallDeltas { get; init; }
    public bool IsComplete { get; init; }
    public FinishReason? FinishReason { get; init; }
    public UsageInfo? Usage { get; init; }
}

/// <summary>
/// Token usage information.
/// </summary>
public sealed record UsageInfo
{
    public required int PromptTokens { get; init; }
    public required int CompletionTokens { get; init; }
    public int TotalTokens => PromptTokens + CompletionTokens;
    public TimeSpan? TimeToFirstToken { get; init; }
    public TimeSpan? TotalDuration { get; init; }
    public bool IsEstimated { get; init; }
}
```

### Provider Registry

```csharp
namespace AgenticCoder.Application.Models;

public interface IProviderRegistry : IAsyncDisposable
{
    void Register(IModelProvider provider);
    IModelProvider? Get(string providerId);
    IModelProvider GetDefault();
    IReadOnlyList<IModelProvider> GetAll();
}

public sealed class ProviderRegistry : IProviderRegistry
{
    private readonly ConcurrentDictionary<string, IModelProvider> _providers = new();
    private readonly string _defaultProviderId;
    
    public ProviderRegistry(ProviderConfiguration config)
    {
        _defaultProviderId = config.DefaultProvider;
    }
    
    public void Register(IModelProvider provider)
    {
        if (!_providers.TryAdd(provider.ProviderId, provider))
        {
            throw new InvalidOperationException(
                $"Provider '{provider.ProviderId}' is already registered");
        }
    }
    
    public IModelProvider? Get(string providerId)
    {
        return _providers.TryGetValue(providerId, out var provider) ? provider : null;
    }
    
    public IModelProvider GetDefault()
    {
        return Get(_defaultProviderId) 
            ?? throw new InvalidOperationException(
                $"Default provider '{_defaultProviderId}' not found");
    }
    
    public IReadOnlyList<IModelProvider> GetAll()
    {
        return _providers.Values.ToList();
    }
    
    public async ValueTask DisposeAsync()
    {
        foreach (var provider in _providers.Values)
        {
            await provider.DisposeAsync();
        }
        _providers.Clear();
    }
}
```

### Error Codes

| Code | Description |
|------|-------------|
| ACODE-MDL-001 | Provider unavailable |
| ACODE-MDL-002 | Request timeout |
| ACODE-MDL-003 | Invalid response format |
| ACODE-MDL-004 | Provider configuration invalid |
| ACODE-MDL-005 | Provider not found |
| ACODE-MDL-006 | Model not found |
| ACODE-MDL-007 | Request cancelled |
| ACODE-MDL-008 | Streaming error |
| ACODE-MDL-009 | Tool call parse error |
| ACODE-MDL-010 | Token limit exceeded |

### CLI Exit Codes

| Exit Code | Meaning |
|-----------|---------|
| 0 | Success |
| 1 | Provider error |
| 2 | Invalid arguments |
| 3 | Configuration error |
| 4 | Timeout |
| 5 | Cancelled |

### Audit Event Types

| Event | Fields |
|-------|--------|
| InferenceStarted | provider_id, model, token_estimate |
| InferenceCompleted | provider_id, model, prompt_tokens, completion_tokens, duration |
| InferenceFailed | provider_id, model, error_code, error_message |
| ProviderHealthCheck | provider_id, available, latency |

### Implementation Checklist

1. [ ] Define IModelProvider interface
2. [ ] Define MessageRole enum
3. [ ] Define ChatMessage record
4. [ ] Define ToolCall record
5. [ ] Define ToolResult record
6. [ ] Define ToolDefinition record
7. [ ] Define ChatRequest record
8. [ ] Define ModelParameters record
9. [ ] Define ChatResponse record
10. [ ] Define FinishReason enum
11. [ ] Define StreamingChunk record
12. [ ] Define UsageInfo record
13. [ ] Define ProviderCapabilities record
14. [ ] Define ModelInfo record
15. [ ] Define exception types
16. [ ] Implement IProviderRegistry interface
17. [ ] Implement ProviderRegistry
18. [ ] Implement MockModelProvider for testing
19. [ ] Implement CLI provider list command
20. [ ] Implement CLI provider health command
21. [ ] Implement CLI provider info command
22. [ ] Implement CLI provider test command
23. [ ] Add configuration loading
24. [ ] Add audit event integration
25. [ ] Write unit tests for all types
26. [ ] Write unit tests for registry
27. [ ] Write integration tests
28. [ ] Write CLI tests
29. [ ] Document all interfaces
30. [ ] Document configuration

### Dependencies

- Task 002 (Config Contract) - for configuration loading
- Task 002.b (Parser/Validator) - for config parsing
- Task 003.c (Audit) - for audit event logging

### Verification Command

```bash
# Run all model interface tests
dotnet test --filter "FullyQualifiedName~Models"

# Verify provider list
agentic-coder provider list

# Verify health check
agentic-coder provider health
```

---

**End of Task 004 Specification**