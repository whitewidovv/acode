# Task 004.c: Provider Registry and Config Selection

**Priority:** P0 – Critical Path  
**Tier:** Core Infrastructure  
**Complexity:** 13 (Fibonacci points)  
**Phase:** Foundation  
**Dependencies:** Task 004, Task 004.a, Task 004.b, Task 001, Task 002  

---

## Description

Task 004.c defines the Provider Registry system that manages registration, discovery, and selection of model providers within the Agentic Coding Bot (Acode) system. The registry serves as the central coordination point for all provider operations, implementing the service locator pattern with compile-time registration to ensure providers are available when needed without sacrificing testability or introducing runtime reflection overhead.

The provider registry addresses a fundamental architectural requirement: the Acode system MUST support multiple inference providers (Ollama, vLLM, and potentially others) while presenting a unified interface to application-layer code. Consumer code should not need to know which specific provider is being used—it simply requests inference through the registry, which routes to the appropriate provider based on configuration and capabilities.

Configuration selection encompasses both static configuration from `.agent/config.yml` (Task 002) and dynamic selection based on request characteristics. Static configuration specifies the default provider, provider-specific settings (endpoints, timeouts, model mappings), and fallback behavior. Dynamic selection evaluates factors like model availability, provider health status, and request requirements (streaming support, tool calling capability) to select the optimal provider for each request.

The registry maintains provider lifecycle management including initialization, health checking, and graceful shutdown. When the application starts, the registry initializes configured providers and performs connectivity validation. During operation, the registry monitors provider health and can route around unhealthy providers if fallback is configured. At shutdown, the registry coordinates orderly provider termination to prevent request abandonment.

Integration with Task 001 operating modes is critical. In airgapped mode, the registry MUST verify that all configured providers are local-only and MUST NOT attempt network connectivity to external endpoints. In burst mode, the registry MAY allow additional provider configurations. The registry MUST emit warnings if provider configuration appears inconsistent with the current operating mode.

The configuration model follows a hierarchical structure with sensible defaults. Provider-specific configuration overrides global defaults, and per-request overrides can further customize behavior. This allows simple configurations for basic usage while enabling sophisticated multi-provider setups for advanced users.

Provider capability discovery enables intelligent routing. Each registered provider reports its capabilities: supported models, streaming support, tool calling support, maximum context window, and generation parameters. The registry indexes these capabilities and can match incoming requests to capable providers. If a request requires a capability no provider offers, the registry fails fast with a clear error.

Fallback and retry policies are configurable per-provider and globally. When a provider fails, the registry can automatically attempt the request on a fallback provider if configured. Retry policies specify maximum attempts, backoff timing, and which errors are retryable. These policies balance reliability against latency and resource consumption.

The registry exposes both synchronous and asynchronous APIs appropriate to different usage contexts. Provider registration occurs synchronously at startup. Provider health checks may be synchronous or asynchronous. Request routing is always asynchronous as it may involve provider communication.

Error handling within the registry covers multiple failure modes: provider not found, provider unhealthy, capability not available, configuration invalid, and timeout. Each failure mode has a distinct error code and structured error information enabling appropriate handling by callers. The registry never swallows errors—all failures are surfaced with full context.

Observability requirements mandate comprehensive logging and metrics. The registry logs provider registration, selection decisions, health check results, and failures. Metrics track request routing, provider utilization, and failure rates. This observability enables operators to understand system behavior and diagnose issues.

---

## Glossary / Terms

| Term | Definition |
|------|------------|
| Provider Registry | Central service managing provider registration and selection |
| IProviderRegistry | Interface defining registry operations |
| ProviderRegistration | Metadata describing a registered provider |
| ProviderDescriptor | Immutable description of a provider's capabilities |
| ProviderConfig | Configuration settings for a specific provider |
| ProviderSelector | Strategy for selecting among available providers |
| DefaultSelector | Selector using configured default provider |
| CapabilitySelector | Selector matching request requirements to capabilities |
| ProviderHealth | Current health status of a provider |
| HealthCheck | Operation to verify provider availability |
| ProviderFactory | Factory for creating provider instances |
| ProviderLifecycle | Enumeration of provider states (created, initialized, healthy, unhealthy, disposed) |
| FallbackPolicy | Rules for selecting alternative providers on failure |
| RetryPolicy | Rules for retrying failed requests |
| ModelMapping | Association between model alias and provider-specific identifier |
| ProviderEndpoint | Connection details for a provider service |
| ProviderTimeout | Timeout settings for provider operations |
| CapabilityMatrix | Indexed map of provider capabilities |
| RouteDecision | Result of provider selection including rationale |
| ProviderMetrics | Operational metrics for a provider |

---

## Out of Scope

The following items are explicitly excluded from Task 004.c:

- **Provider implementation** - Ollama (Task 005) and vLLM (Task 006) are separate tasks
- **Load balancing across instances** - Multi-instance provider scaling is not addressed
- **Provider authentication** - Local providers don't require authentication
- **Provider auto-discovery** - Providers must be explicitly configured
- **Hot reload of provider config** - Requires application restart
- **Provider priority scheduling** - Round-robin or weighted selection not implemented
- **Geographic routing** - Not applicable for local providers
- **Cost-based routing** - No billing in Acode system
- **Provider quotas or rate limiting** - Infrastructure concern for later
- **Provider versioning** - API version negotiation not required
- **Provider plugin system** - Dynamic loading not supported
- **Multi-tenancy** - Single-user application

---

## Functional Requirements

### IProviderRegistry Interface

- FR-001: IProviderRegistry MUST be defined as an interface in the Application layer
- FR-002: IProviderRegistry MUST provide Register(ProviderDescriptor) method
- FR-003: IProviderRegistry MUST provide Unregister(string providerId) method
- FR-004: IProviderRegistry MUST provide GetProvider(string providerId) returning IModelProvider
- FR-005: IProviderRegistry MUST provide GetDefaultProvider() returning IModelProvider
- FR-006: IProviderRegistry MUST provide GetProviderFor(ChatRequest) returning IModelProvider
- FR-007: IProviderRegistry MUST provide ListProviders() returning IReadOnlyList<ProviderDescriptor>
- FR-008: IProviderRegistry MUST provide IsRegistered(string providerId) returning bool
- FR-009: IProviderRegistry MUST provide GetProviderHealth(string providerId) returning ProviderHealth
- FR-010: IProviderRegistry MUST provide CheckAllHealthAsync() returning Task<IReadOnlyDictionary<string, ProviderHealth>>
- FR-011: IProviderRegistry MUST extend IAsyncDisposable for cleanup
- FR-012: IProviderRegistry MUST support CancellationToken on async operations

### ProviderDescriptor Record

- FR-013: ProviderDescriptor MUST be defined as an immutable record type
- FR-014: ProviderDescriptor MUST include Id property (string, unique identifier)
- FR-015: ProviderDescriptor MUST include Name property (string, display name)
- FR-016: ProviderDescriptor MUST include Type property (ProviderType enum)
- FR-017: ProviderDescriptor MUST include Endpoint property (ProviderEndpoint)
- FR-018: ProviderDescriptor MUST include Capabilities property (ProviderCapabilities)
- FR-019: ProviderDescriptor MUST include Config property (ProviderConfig)
- FR-020: ProviderDescriptor MUST include Priority property (int, for fallback ordering)
- FR-021: ProviderDescriptor MUST include Enabled property (bool)
- FR-022: ProviderDescriptor MUST validate Id is non-empty
- FR-023: ProviderDescriptor MUST validate unique Id across registry

### ProviderType Enum

- FR-024: ProviderType MUST be defined in Domain layer
- FR-025: ProviderType MUST include Ollama value
- FR-026: ProviderType MUST include Vllm value
- FR-027: ProviderType MUST include Mock value (for testing)
- FR-028: ProviderType MUST serialize to lowercase strings

### ProviderCapabilities Record

- FR-029: ProviderCapabilities MUST be defined as an immutable record
- FR-030: ProviderCapabilities MUST include SupportedModels (IReadOnlyList<string>)
- FR-031: ProviderCapabilities MUST include SupportsStreaming (bool)
- FR-032: ProviderCapabilities MUST include SupportsToolCalls (bool)
- FR-033: ProviderCapabilities MUST include MaxContextTokens (int)
- FR-034: ProviderCapabilities MUST include MaxOutputTokens (int)
- FR-035: ProviderCapabilities MUST include SupportsJsonMode (bool)
- FR-036: ProviderCapabilities MUST provide Supports(CapabilityRequirement) method
- FR-037: ProviderCapabilities MUST provide Merge(ProviderCapabilities) method

### ProviderEndpoint Record

- FR-038: ProviderEndpoint MUST be defined as an immutable record
- FR-039: ProviderEndpoint MUST include BaseUrl property (Uri)
- FR-040: ProviderEndpoint MUST include ConnectTimeout property (TimeSpan)
- FR-041: ProviderEndpoint MUST include RequestTimeout property (TimeSpan)
- FR-042: ProviderEndpoint MUST validate BaseUrl is valid URI
- FR-043: ProviderEndpoint MUST validate timeouts are positive
- FR-044: ProviderEndpoint MUST provide default timeout values

### ProviderConfig Record

- FR-045: ProviderConfig MUST be defined as an immutable record
- FR-046: ProviderConfig MUST include ModelMappings (IReadOnlyDictionary<string, string>)
- FR-047: ProviderConfig MUST include DefaultModel (string?)
- FR-048: ProviderConfig MUST include RetryPolicy (RetryPolicy)
- FR-049: ProviderConfig MUST include FallbackProviderId (string?)
- FR-050: ProviderConfig MUST include CustomSettings (IReadOnlyDictionary<string, JsonElement>)

### RetryPolicy Record

- FR-051: RetryPolicy MUST be defined as an immutable record
- FR-052: RetryPolicy MUST include MaxRetries (int, default 3)
- FR-053: RetryPolicy MUST include InitialDelay (TimeSpan, default 100ms)
- FR-054: RetryPolicy MUST include MaxDelay (TimeSpan, default 10s)
- FR-055: RetryPolicy MUST include BackoffMultiplier (double, default 2.0)
- FR-056: RetryPolicy MUST include RetryableErrors (IReadOnlyList<string>)
- FR-057: RetryPolicy MUST provide static None property for no retries
- FR-058: RetryPolicy MUST provide static Default property

### ProviderHealth Record

- FR-059: ProviderHealth MUST be defined as an immutable record
- FR-060: ProviderHealth MUST include Status (HealthStatus enum)
- FR-061: ProviderHealth MUST include LastCheck (DateTimeOffset)
- FR-062: ProviderHealth MUST include LastError (string?)
- FR-063: ProviderHealth MUST include ResponseTimeMs (long?)
- FR-064: ProviderHealth MUST include ConsecutiveFailures (int)

### HealthStatus Enum

- FR-065: HealthStatus MUST include Unknown value (not yet checked)
- FR-066: HealthStatus MUST include Healthy value
- FR-067: HealthStatus MUST include Degraded value (slow but working)
- FR-068: HealthStatus MUST include Unhealthy value (failing)
- FR-069: HealthStatus MUST include Disabled value (manually disabled)

### Provider Registration

- FR-070: Registry MUST accept provider registration at startup
- FR-071: Registry MUST reject duplicate provider IDs
- FR-072: Registry MUST validate provider descriptor on registration
- FR-073: Registry MUST initialize provider on registration
- FR-074: Registry MUST perform initial health check on registration
- FR-075: Registry MUST log provider registration events
- FR-076: Registry MUST update capability index on registration

### Provider Selection

- FR-077: GetDefaultProvider MUST return configured default provider
- FR-078: GetDefaultProvider MUST throw if no default configured
- FR-079: GetProvider(id) MUST return specific provider by ID
- FR-080: GetProvider(id) MUST throw ProviderNotFoundException if not found
- FR-081: GetProviderFor(request) MUST match request to capable provider
- FR-082: GetProviderFor(request) MUST consider model compatibility
- FR-083: GetProviderFor(request) MUST consider capability requirements
- FR-084: GetProviderFor(request) MUST consider provider health
- FR-085: GetProviderFor(request) MUST log selection decision
- FR-086: GetProviderFor(request) MUST throw if no provider matches

### Health Checking

- FR-087: CheckAllHealthAsync MUST check all registered providers
- FR-088: Health check MUST timeout after configured interval
- FR-089: Health check MUST update LastCheck timestamp
- FR-090: Health check MUST record ResponseTimeMs on success
- FR-091: Health check MUST record LastError on failure
- FR-092: Health check MUST increment ConsecutiveFailures on failure
- FR-093: Health check MUST reset ConsecutiveFailures on success
- FR-094: Registry MUST support periodic background health checks
- FR-095: Registry MUST emit health status change events

### Configuration Integration

- FR-096: Registry MUST load provider config from .agent/config.yml
- FR-097: Registry MUST support providers section in config
- FR-098: Registry MUST apply default values for missing config
- FR-099: Registry MUST validate config on load
- FR-100: Registry MUST log config loading results
- FR-101: Registry MUST support environment variable overrides

### Operating Mode Integration

- FR-102: Registry MUST validate providers against Task 001 operating mode
- FR-103: Registry MUST reject external providers in airgapped mode
- FR-104: Registry MUST warn if config inconsistent with mode
- FR-105: Registry MUST log operating mode validation results

### Fallback Behavior

- FR-106: Registry MUST support fallback provider configuration
- FR-107: Registry MUST attempt fallback when primary fails
- FR-108: Registry MUST respect fallback ordering
- FR-109: Registry MUST log fallback attempts
- FR-110: Registry MUST limit fallback chain length (max 3)

---

## Non-Functional Requirements

### Performance

- NFR-001: Provider registration MUST complete in < 100 milliseconds
- NFR-002: GetDefaultProvider MUST complete in < 1 microsecond
- NFR-003: GetProvider(id) MUST complete in O(1) time
- NFR-004: GetProviderFor(request) MUST complete in < 100 microseconds
- NFR-005: ListProviders MUST complete in O(n) time
- NFR-006: Health check MUST timeout at configured interval
- NFR-007: Registry MUST support 100+ registered providers
- NFR-008: Registry operations MUST not block each other
- NFR-009: Capability matching MUST use indexed lookup

### Reliability

- NFR-010: Registry MUST be thread-safe for concurrent access
- NFR-011: Registry MUST handle provider failures gracefully
- NFR-012: Registry MUST not crash on invalid provider responses
- NFR-013: Registry MUST maintain consistent state under failures
- NFR-014: Registry MUST support graceful shutdown
- NFR-015: Registry MUST complete disposal in < 5 seconds

### Security

- NFR-016: Registry MUST NOT log provider credentials
- NFR-017: Registry MUST validate provider endpoints
- NFR-018: Registry MUST reject non-local endpoints in airgapped mode
- NFR-019: Registry MUST sanitize provider responses for logging

### Maintainability

- NFR-020: Registry MUST have complete XML documentation
- NFR-021: Registry MUST follow dependency injection patterns
- NFR-022: Registry MUST support mock providers for testing
- NFR-023: Registry MUST emit structured log events
- NFR-024: Registry configuration MUST be documented

### Observability

- NFR-025: Registry MUST log all provider state changes
- NFR-026: Registry MUST log selection decisions with rationale
- NFR-027: Registry MUST expose provider metrics
- NFR-028: Registry MUST support health check endpoints

---

## User Manual Documentation

### Overview

The Provider Registry is the central component managing model provider registration, discovery, and selection in Acode. It provides a unified interface for accessing inference capabilities regardless of the underlying provider.

### Quick Start

#### Configuring Providers

Configure providers in `.agent/config.yml`:

```yaml
model:
  default_provider: ollama
  
  providers:
    ollama:
      enabled: true
      endpoint: http://localhost:11434
      timeout_seconds: 120
      models:
        - llama3.2:8b
        - codellama:13b
        
    vllm:
      enabled: false
      endpoint: http://localhost:8000
      timeout_seconds: 300
```

#### Using the Registry

```csharp
using AgenticCoder.Application.Providers;

// Inject registry
public class InferenceService
{
    private readonly IProviderRegistry _registry;
    
    public InferenceService(IProviderRegistry registry)
    {
        _registry = registry;
    }
    
    public async Task<ChatResponse> GenerateAsync(string prompt)
    {
        // Get default provider
        var provider = _registry.GetDefaultProvider();
        
        // Or get specific provider
        var ollamaProvider = _registry.GetProvider("ollama");
        
        // Or get provider matching request requirements
        var request = new ChatRequest { /* ... */ };
        var matchedProvider = _registry.GetProviderFor(request);
        
        return await matchedProvider.CompleteAsync(request);
    }
}
```

### Configuration Reference

#### Provider Section

```yaml
model:
  # Default provider for requests without explicit provider
  default_provider: ollama
  
  # Provider definitions
  providers:
    <provider_id>:
      # Enable/disable provider
      enabled: true
      
      # Provider endpoint
      endpoint: http://localhost:11434
      
      # Timeouts
      connect_timeout_seconds: 5
      request_timeout_seconds: 120
      
      # Available models
      models:
        - model-name:tag
      
      # Default model for this provider
      default_model: model-name:tag
      
      # Retry configuration
      retry:
        max_retries: 3
        initial_delay_ms: 100
        max_delay_ms: 10000
        backoff_multiplier: 2.0
      
      # Fallback provider on failure
      fallback: vllm
      
      # Priority (lower = preferred)
      priority: 1
```

#### Environment Variable Overrides

```bash
# Override provider endpoint
ACODE_MODEL_PROVIDERS_OLLAMA_ENDPOINT=http://custom:11434

# Override default provider
ACODE_MODEL_DEFAULT_PROVIDER=vllm

# Override timeout
ACODE_MODEL_PROVIDERS_OLLAMA_TIMEOUT_SECONDS=300
```

### Provider Selection

#### Default Selection

When no specific provider is requested:

```csharp
var provider = registry.GetDefaultProvider();
```

Returns the provider specified by `default_provider` in config.

#### ID-Based Selection

Request a specific provider:

```csharp
var provider = registry.GetProvider("ollama");
```

Throws `ProviderNotFoundException` if provider not registered.

#### Capability-Based Selection

Match provider to request requirements:

```csharp
var request = new ChatRequest
{
    Model = "codellama:13b",
    Stream = true,
    Tools = new[] { /* tool definitions */ }
};

var provider = registry.GetProviderFor(request);
```

The registry selects a provider that:
1. Supports the requested model
2. Supports streaming (if requested)
3. Supports tool calling (if tools provided)
4. Is currently healthy

### Health Monitoring

#### Check Provider Health

```csharp
// Check specific provider
var health = registry.GetProviderHealth("ollama");
Console.WriteLine($"Status: {health.Status}");
Console.WriteLine($"Last check: {health.LastCheck}");
Console.WriteLine($"Response time: {health.ResponseTimeMs}ms");

// Check all providers
var allHealth = await registry.CheckAllHealthAsync();
foreach (var (id, status) in allHealth)
{
    Console.WriteLine($"{id}: {status.Status}");
}
```

#### Health Status Values

| Status | Description | Behavior |
|--------|-------------|----------|
| Unknown | Not yet checked | Will be checked on first use |
| Healthy | Provider responding normally | Full functionality |
| Degraded | Slow but operational | May use fallback |
| Unhealthy | Failing requests | Uses fallback if configured |
| Disabled | Manually disabled | Skipped in selection |

### Fallback Configuration

Configure fallback chains:

```yaml
model:
  providers:
    ollama:
      enabled: true
      fallback: vllm  # Use vLLM if Ollama fails
      
    vllm:
      enabled: true
      fallback: null  # No further fallback
```

Fallback behavior:
- Triggered on provider error or unhealthy status
- Maximum 3 providers in fallback chain
- Each fallback attempt logged
- Final failure surfaces original error

### CLI Integration

#### List Providers

```
$ acode providers list
┌────────────────────────────────────────────────────────────┐
│ Registered Providers                                        │
├──────────┬─────────┬────────────────────────────┬──────────┤
│ ID       │ Status  │ Endpoint                   │ Models   │
├──────────┼─────────┼────────────────────────────┼──────────┤
│ ollama   │ Healthy │ http://localhost:11434     │ 3        │
│ vllm     │ Disabled│ http://localhost:8000      │ 1        │
└──────────┴─────────┴────────────────────────────┴──────────┘
```

#### Check Health

```
$ acode providers health
┌────────────────────────────────────────────────────────────┐
│ Provider Health Check                                       │
├──────────┬─────────┬────────────┬──────────────────────────┤
│ ID       │ Status  │ Latency    │ Last Check               │
├──────────┼─────────┼────────────┼──────────────────────────┤
│ ollama   │ Healthy │ 45ms       │ 2024-01-15 10:30:00      │
│ vllm     │ Disabled│ -          │ -                        │
└──────────┴─────────┴────────────┴──────────────────────────┘
```

#### Show Provider Details

```
$ acode providers show ollama
Provider: ollama
  Type: Ollama
  Endpoint: http://localhost:11434
  Status: Healthy
  Last Check: 2024-01-15 10:30:00 (45ms)
  
Capabilities:
  Streaming: Yes
  Tool Calls: Yes
  Max Context: 8192 tokens
  
Models:
  - llama3.2:8b (default)
  - codellama:13b
  - mistral:7b
  
Configuration:
  Priority: 1
  Fallback: vllm
  Retry: 3 attempts, 100ms-10s backoff
```

### Troubleshooting

#### Provider Not Found

**Error:** `ProviderNotFoundException: Provider 'xyz' is not registered`

**Causes:**
1. Provider not defined in config
2. Provider disabled (`enabled: false`)
3. Typo in provider ID

**Resolution:**
```bash
# Check registered providers
acode providers list

# Verify config
cat .agent/config.yml | grep -A 10 "providers:"
```

#### No Provider Matches Request

**Error:** `NoCapableProviderException: No provider supports model 'unknown-model'`

**Causes:**
1. Requested model not available
2. All capable providers unhealthy
3. Capability requirement not met

**Resolution:**
```bash
# Check available models
acode providers show --models

# Check provider health
acode providers health
```

#### Provider Unhealthy

**Error:** Provider showing Unhealthy status

**Diagnosis:**
```bash
# Check provider health details
acode providers health --verbose

# Check provider process
curl http://localhost:11434/api/tags  # Ollama
curl http://localhost:8000/v1/models   # vLLM
```

**Resolution:**
1. Verify provider service is running
2. Check provider logs
3. Verify endpoint configuration
4. Check network connectivity

---

## Acceptance Criteria

### IProviderRegistry Interface

- [ ] AC-001: IProviderRegistry interface defined
- [ ] AC-002: Register method exists
- [ ] AC-003: Unregister method exists
- [ ] AC-004: GetProvider method exists
- [ ] AC-005: GetDefaultProvider method exists
- [ ] AC-006: GetProviderFor method exists
- [ ] AC-007: ListProviders method exists
- [ ] AC-008: IsRegistered method exists
- [ ] AC-009: GetProviderHealth method exists
- [ ] AC-010: CheckAllHealthAsync method exists
- [ ] AC-011: Implements IAsyncDisposable
- [ ] AC-012: CancellationToken supported

### ProviderDescriptor Record

- [ ] AC-013: ProviderDescriptor defined
- [ ] AC-014: Id property exists and required
- [ ] AC-015: Id validation works
- [ ] AC-016: Name property exists
- [ ] AC-017: Type property exists
- [ ] AC-018: Endpoint property exists
- [ ] AC-019: Capabilities property exists
- [ ] AC-020: Config property exists
- [ ] AC-021: Priority property exists
- [ ] AC-022: Enabled property exists
- [ ] AC-023: Immutability verified

### ProviderType Enum

- [ ] AC-024: ProviderType enum defined
- [ ] AC-025: Ollama value exists
- [ ] AC-026: Vllm value exists
- [ ] AC-027: Mock value exists
- [ ] AC-028: Serialization to lowercase

### ProviderCapabilities Record

- [ ] AC-029: ProviderCapabilities defined
- [ ] AC-030: SupportedModels property exists
- [ ] AC-031: SupportsStreaming property exists
- [ ] AC-032: SupportsToolCalls property exists
- [ ] AC-033: MaxContextTokens property exists
- [ ] AC-034: MaxOutputTokens property exists
- [ ] AC-035: SupportsJsonMode property exists
- [ ] AC-036: Supports method works
- [ ] AC-037: Merge method works

### ProviderEndpoint Record

- [ ] AC-038: ProviderEndpoint defined
- [ ] AC-039: BaseUrl property exists
- [ ] AC-040: ConnectTimeout property exists
- [ ] AC-041: RequestTimeout property exists
- [ ] AC-042: URL validation works
- [ ] AC-043: Timeout validation works
- [ ] AC-044: Default values provided

### ProviderConfig Record

- [ ] AC-045: ProviderConfig defined
- [ ] AC-046: ModelMappings property exists
- [ ] AC-047: DefaultModel property exists
- [ ] AC-048: RetryPolicy property exists
- [ ] AC-049: FallbackProviderId property exists
- [ ] AC-050: CustomSettings property exists

### RetryPolicy Record

- [ ] AC-051: RetryPolicy defined
- [ ] AC-052: MaxRetries property exists
- [ ] AC-053: InitialDelay property exists
- [ ] AC-054: MaxDelay property exists
- [ ] AC-055: BackoffMultiplier property exists
- [ ] AC-056: RetryableErrors property exists
- [ ] AC-057: None static property works
- [ ] AC-058: Default static property works

### ProviderHealth Record

- [ ] AC-059: ProviderHealth defined
- [ ] AC-060: Status property exists
- [ ] AC-061: LastCheck property exists
- [ ] AC-062: LastError property exists
- [ ] AC-063: ResponseTimeMs property exists
- [ ] AC-064: ConsecutiveFailures property exists

### HealthStatus Enum

- [ ] AC-065: HealthStatus enum defined
- [ ] AC-066: Unknown value exists
- [ ] AC-067: Healthy value exists
- [ ] AC-068: Degraded value exists
- [ ] AC-069: Unhealthy value exists
- [ ] AC-070: Disabled value exists

### Provider Registration

- [ ] AC-071: Registration accepts valid descriptor
- [ ] AC-072: Registration rejects duplicate IDs
- [ ] AC-073: Registration validates descriptor
- [ ] AC-074: Registration initializes provider
- [ ] AC-075: Registration performs health check
- [ ] AC-076: Registration logs events
- [ ] AC-077: Capability index updated

### Provider Selection

- [ ] AC-078: GetDefaultProvider returns configured default
- [ ] AC-079: GetDefaultProvider throws when no default
- [ ] AC-080: GetProvider returns by ID
- [ ] AC-081: GetProvider throws for missing ID
- [ ] AC-082: GetProviderFor matches capabilities
- [ ] AC-083: GetProviderFor considers model
- [ ] AC-084: GetProviderFor considers health
- [ ] AC-085: GetProviderFor logs decision
- [ ] AC-086: GetProviderFor throws when no match

### Health Checking

- [ ] AC-087: CheckAllHealthAsync checks all
- [ ] AC-088: Health check respects timeout
- [ ] AC-089: LastCheck updated
- [ ] AC-090: ResponseTimeMs recorded
- [ ] AC-091: LastError recorded on failure
- [ ] AC-092: ConsecutiveFailures incremented
- [ ] AC-093: ConsecutiveFailures reset on success
- [ ] AC-094: Background checks supported
- [ ] AC-095: Health change events emitted

### Configuration Integration

- [ ] AC-096: Config loaded from yml
- [ ] AC-097: Providers section parsed
- [ ] AC-098: Defaults applied
- [ ] AC-099: Config validated
- [ ] AC-100: Loading logged
- [ ] AC-101: Env vars override

### Operating Mode Integration

- [ ] AC-102: Mode validation performed
- [ ] AC-103: External rejected in airgapped
- [ ] AC-104: Inconsistency warnings emitted
- [ ] AC-105: Validation logged

### Fallback Behavior

- [ ] AC-106: Fallback configured
- [ ] AC-107: Fallback attempted on failure
- [ ] AC-108: Ordering respected
- [ ] AC-109: Attempts logged
- [ ] AC-110: Chain length limited

### Performance

- [ ] AC-111: Registration < 100ms
- [ ] AC-112: GetDefaultProvider < 1μs
- [ ] AC-113: GetProvider O(1)
- [ ] AC-114: GetProviderFor < 100μs
- [ ] AC-115: Thread-safety verified

### Security

- [ ] AC-116: No credentials logged
- [ ] AC-117: Endpoints validated
- [ ] AC-118: Local-only in airgapped
- [ ] AC-119: Responses sanitized

### Documentation

- [ ] AC-120: XML documentation complete
- [ ] AC-121: Config examples provided
- [ ] AC-122: CLI commands documented
- [ ] AC-123: Error codes documented

---

## Testing Requirements

### Unit Tests

```
Tests/Unit/Application/Providers/
├── ProviderRegistryTests.cs
│   ├── Should_Register_Valid_Provider()
│   ├── Should_Reject_Duplicate_Id()
│   ├── Should_Validate_Descriptor()
│   ├── Should_Return_Default_Provider()
│   ├── Should_Throw_When_No_Default()
│   ├── Should_Get_Provider_By_Id()
│   ├── Should_Throw_For_Missing_Provider()
│   ├── Should_Match_Request_To_Provider()
│   ├── Should_Consider_Model_In_Selection()
│   ├── Should_Consider_Health_In_Selection()
│   ├── Should_List_All_Providers()
│   ├── Should_Check_If_Registered()
│   ├── Should_Handle_Fallback()
│   ├── Should_Limit_Fallback_Chain()
│   └── Should_Be_Thread_Safe()
│
├── ProviderDescriptorTests.cs
│   ├── Should_Require_Id()
│   ├── Should_Validate_Id_Not_Empty()
│   ├── Should_Be_Immutable()
│   └── Should_Support_All_Properties()
│
├── ProviderCapabilitiesTests.cs
│   ├── Should_Check_Supports()
│   ├── Should_Merge_Capabilities()
│   └── Should_Be_Immutable()
│
├── ProviderEndpointTests.cs
│   ├── Should_Validate_Url()
│   ├── Should_Validate_Timeouts()
│   └── Should_Provide_Defaults()
│
├── RetryPolicyTests.cs
│   ├── Should_Have_Defaults()
│   ├── None_Should_Disable_Retries()
│   └── Should_Be_Immutable()
│
└── ProviderHealthTests.cs
    ├── Should_Track_Status()
    ├── Should_Record_Failures()
    └── Should_Reset_On_Success()
```

### Integration Tests

```
Tests/Integration/Providers/
├── ProviderConfigLoadingTests.cs
│   ├── Should_Load_From_Config_Yml()
│   ├── Should_Apply_Defaults()
│   ├── Should_Override_With_Env_Vars()
│   └── Should_Validate_Config()
│
├── ProviderHealthCheckTests.cs
│   ├── Should_Check_Provider_Health()
│   ├── Should_Timeout_Appropriately()
│   └── Should_Update_Health_Status()
│
└── OperatingModeValidationTests.cs
    ├── Should_Validate_Airgapped_Mode()
    └── Should_Warn_On_Inconsistency()
```

### End-to-End Tests

```
Tests/E2E/Providers/
├── ProviderSelectionE2ETests.cs
│   ├── Should_Select_Default_Provider()
│   ├── Should_Select_By_Capability()
│   ├── Should_Fallback_On_Failure()
│   └── Should_Fail_When_No_Match()
```

### Performance Tests

```
Tests/Performance/Providers/
├── ProviderRegistryBenchmarks.cs
│   ├── Benchmark_Registration()
│   ├── Benchmark_GetDefaultProvider()
│   ├── Benchmark_GetProviderById()
│   ├── Benchmark_GetProviderFor()
│   └── Benchmark_ConcurrentAccess()
```

---

## User Verification Steps

### Scenario 1: Register Provider

1. Create ProviderDescriptor with valid data
2. Call registry.Register(descriptor)
3. Verify IsRegistered returns true
4. Verify ListProviders includes provider

### Scenario 2: Get Default Provider

1. Configure default_provider in config
2. Start application
3. Call GetDefaultProvider()
4. Verify correct provider returned

### Scenario 3: Get Provider by ID

1. Register multiple providers
2. Call GetProvider("ollama")
3. Verify correct provider returned
4. Call GetProvider("nonexistent")
5. Verify ProviderNotFoundException thrown

### Scenario 4: Capability-Based Selection

1. Register provider with streaming support
2. Create request requiring streaming
3. Call GetProviderFor(request)
4. Verify streaming-capable provider returned

### Scenario 5: Health Check

1. Register provider
2. Call GetProviderHealth(id)
3. Verify status is Healthy
4. Stop provider service
5. Call CheckAllHealthAsync()
6. Verify status is Unhealthy

### Scenario 6: Fallback on Failure

1. Configure provider with fallback
2. Make primary provider unhealthy
3. Request provider for request
4. Verify fallback provider returned

### Scenario 7: Config Loading

1. Create .agent/config.yml with providers
2. Start application
3. Verify all configured providers registered
4. Verify settings applied correctly

### Scenario 8: Environment Override

1. Set ACODE_MODEL_PROVIDERS_OLLAMA_ENDPOINT
2. Start application
3. Verify endpoint overridden

### Scenario 9: Airgapped Mode Validation

1. Set operating mode to airgapped
2. Configure provider with external endpoint
3. Verify registration rejected or warned

### Scenario 10: CLI Provider List

1. Run `acode providers list`
2. Verify all providers shown
3. Verify status displayed correctly

---

## Implementation Prompt

### File Structure

```
src/AgenticCoder.Application/Providers/
├── IProviderRegistry.cs
├── ProviderRegistry.cs
├── ProviderDescriptor.cs
├── ProviderType.cs
├── ProviderCapabilities.cs
├── ProviderEndpoint.cs
├── ProviderConfig.cs
├── RetryPolicy.cs
├── ProviderHealth.cs
├── HealthStatus.cs
├── Selection/
│   ├── IProviderSelector.cs
│   ├── DefaultProviderSelector.cs
│   └── CapabilityProviderSelector.cs
└── Exceptions/
    ├── ProviderNotFoundException.cs
    ├── NoCapableProviderException.cs
    └── ProviderRegistrationException.cs
```

### IProviderRegistry Implementation

```csharp
namespace AgenticCoder.Application.Providers;

public interface IProviderRegistry : IAsyncDisposable
{
    void Register(ProviderDescriptor descriptor);
    void Unregister(string providerId);
    
    IModelProvider GetProvider(string providerId);
    IModelProvider GetDefaultProvider();
    IModelProvider GetProviderFor(ChatRequest request);
    
    IReadOnlyList<ProviderDescriptor> ListProviders();
    bool IsRegistered(string providerId);
    
    ProviderHealth GetProviderHealth(string providerId);
    Task<IReadOnlyDictionary<string, ProviderHealth>> CheckAllHealthAsync(
        CancellationToken cancellationToken = default);
}
```

### ProviderRegistry Implementation

```csharp
namespace AgenticCoder.Application.Providers;

public sealed class ProviderRegistry : IProviderRegistry
{
    private readonly ConcurrentDictionary<string, ProviderRegistration> _providers = new();
    private readonly ILogger<ProviderRegistry> _logger;
    private readonly ProviderConfig _config;
    private readonly IProviderSelector _selector;
    
    public ProviderRegistry(
        IOptions<ProviderConfig> config,
        IProviderSelector selector,
        ILogger<ProviderRegistry> logger)
    {
        _config = config.Value;
        _selector = selector;
        _logger = logger;
    }
    
    public void Register(ProviderDescriptor descriptor)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        ValidateDescriptor(descriptor);
        
        if (!_providers.TryAdd(descriptor.Id, new ProviderRegistration(descriptor)))
        {
            throw new ProviderRegistrationException(
                $"Provider '{descriptor.Id}' is already registered",
                "ACODE-PRV-001");
        }
        
        _logger.LogInformation(
            "Provider registered: {ProviderId} ({ProviderType})",
            descriptor.Id, descriptor.Type);
    }
    
    public IModelProvider GetDefaultProvider()
    {
        if (string.IsNullOrEmpty(_config.DefaultProviderId))
        {
            throw new NoCapableProviderException(
                "No default provider configured",
                "ACODE-PRV-002");
        }
        
        return GetProvider(_config.DefaultProviderId);
    }
    
    // Additional implementation...
}
```

### Error Codes

| Code | Message |
|------|---------|
| ACODE-PRV-001 | Provider already registered |
| ACODE-PRV-002 | No default provider configured |
| ACODE-PRV-003 | Provider not found |
| ACODE-PRV-004 | No provider supports request |
| ACODE-PRV-005 | Invalid provider descriptor |
| ACODE-PRV-006 | Provider initialization failed |
| ACODE-PRV-007 | Health check timeout |
| ACODE-PRV-008 | Fallback chain exhausted |
| ACODE-PRV-009 | Invalid provider endpoint |
| ACODE-PRV-010 | Operating mode violation |

### Implementation Checklist

1. [ ] Define IProviderRegistry interface
2. [ ] Implement ProviderRegistry class
3. [ ] Define ProviderDescriptor record
4. [ ] Define ProviderType enum
5. [ ] Define ProviderCapabilities record
6. [ ] Define ProviderEndpoint record
7. [ ] Define ProviderConfig record
8. [ ] Define RetryPolicy record
9. [ ] Define ProviderHealth record
10. [ ] Define HealthStatus enum
11. [ ] Implement IProviderSelector interface
12. [ ] Implement DefaultProviderSelector
13. [ ] Implement CapabilityProviderSelector
14. [ ] Implement health checking
15. [ ] Implement fallback logic
16. [ ] Implement config loading
17. [ ] Implement operating mode validation
18. [ ] Add exception types
19. [ ] Write unit tests
20. [ ] Write integration tests
21. [ ] Add XML documentation

### Dependencies

- Task 004 (IModelProvider interface)
- Task 004.a (ChatRequest types)
- Task 004.b (ChatResponse types)
- Task 001 (Operating modes)
- Task 002 (Config schema)

### Verification Command

```bash
dotnet test --filter "FullyQualifiedName~Providers"
```

---

**End of Task 004.c Specification**
