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
