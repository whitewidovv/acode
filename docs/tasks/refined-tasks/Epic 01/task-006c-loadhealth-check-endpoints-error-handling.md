# Task 006.c: Load/Health-Check Endpoints + Error Handling

**Priority:** P1 – High Priority  
**Tier:** Core Infrastructure  
**Complexity:** 8 (Fibonacci points)  
**Phase:** Foundation  
**Dependencies:** Task 006, Task 006.a, Task 004.c (Provider Registry), Task 001, Task 002  

---

## Description

Task 006.c implements health checking, load monitoring, and comprehensive error handling for the vLLM provider adapter. These capabilities enable Acode to intelligently route requests, fail gracefully, and provide operators with visibility into vLLM backend status. Production deployments depend on robust health checking to ensure reliable inference and appropriate fallback when problems occur.

Health checking serves multiple purposes in the Acode architecture. The Provider Registry (Task 004.c) uses health status to determine which providers are available for routing. Users invoke health checks via CLI to diagnose issues. Background monitoring tracks provider health over time, enabling proactive alerts and historical analysis. Each use case has different latency and accuracy requirements addressed by this task.

vLLM exposes health information through multiple endpoints. The `/health` endpoint provides a simple liveness check—if it responds, vLLM is running. The `/v1/models` endpoint lists loaded models, confirming the inference engine is operational. vLLM's Prometheus metrics endpoint (`/metrics`) provides detailed statistics including GPU utilization, request queue depth, and generation throughput. The health checker MUST query appropriate endpoints based on the desired check depth.

Health status is categorized into four states: Healthy, Degraded, Unhealthy, and Unknown. Healthy means vLLM responds quickly and has reasonable resource utilization. Degraded means vLLM is responding but slowly, or resource utilization is high. Unhealthy means vLLM is not responding or returning errors. Unknown means health could not be determined (e.g., timeout during check). Each state triggers different behavior in the Provider Registry.

Load monitoring tracks vLLM's current resource utilization to inform intelligent routing. When vLLM's request queue is full or GPU memory is exhausted, routing requests to it would result in failures or queuing delays. By monitoring these metrics, Acode can route to alternative providers or throttle requests before failures occur. Load information augments health status in routing decisions.

Error handling translates vLLM-specific errors into Acode's exception hierarchy. vLLM returns errors in OpenAI format with error codes, types, and messages. HTTP status codes indicate error categories. The adapter MUST parse these errors, extract relevant information, and construct appropriate exceptions. Error codes enable callers to distinguish error types and respond appropriately.

vLLM-specific error scenarios require special handling. CUDA out-of-memory errors indicate the model is too large or too many requests are concurrent. Model loading failures indicate configuration problems. Request queue overflow indicates vLLM is overloaded. Each error type has different implications for retry, fallback, and user messaging.

Timeout handling addresses inference-specific timing concerns. vLLM inference can take seconds to minutes depending on request length. Connection timeouts must be short enough to detect unreachable servers quickly. Request timeouts must be long enough to allow inference to complete. Streaming timeouts detect stalled connections without prematurely canceling slow generation.

Retry logic distinguishes transient from permanent failures. Network glitches and brief overload conditions are transient—retry may succeed. Model not found, invalid parameters, and authentication failures are permanent—retry will not help. The adapter MUST classify errors correctly and only retry transient failures with appropriate backoff.

The health checker integrates with Acode's observability infrastructure. Health check results are logged with structured fields for querying. Metrics track health check success rates, response times, and state transitions. Alerts can be configured for persistent unhealthy states. This visibility is essential for operating Acode in production environments.

CLI integration exposes health checking to users. The `acode providers health` command shows current status of all configured providers. The `acode providers status vllm` command shows detailed vLLM status including loaded models and resource utilization. These commands help users diagnose problems and verify configuration.

Error handling extends to graceful degradation. When vLLM becomes unavailable, Acode should attempt fallback to alternative providers (if configured) rather than failing immediately. The Provider Registry uses health status to make these routing decisions. Users can configure fallback behavior through `.agent/config.yml`.

---

## Glossary / Terms

| Term | Definition |
|------|------------|
| Health Check | Query to determine provider availability |
| Liveness Check | Simple check if service is running |
| Readiness Check | Check if service can handle requests |
| Health Status | Healthy, Degraded, Unhealthy, or Unknown |
| Load Monitoring | Tracking resource utilization |
| Request Queue | Pending requests waiting for processing |
| GPU Utilization | Percentage of GPU being used |
| KV Cache Utilization | Memory used for attention cache |
| Prometheus Metrics | Monitoring data in Prometheus format |
| Error Code | Structured identifier for error type |
| Error Type | Category of error (e.g., invalid_request_error) |
| Transient Error | Temporary failure that may resolve |
| Permanent Error | Failure that will not resolve without change |
| Fallback | Using alternative provider on failure |
| Circuit Breaker | Pattern to prevent repeated failures |
| Backoff | Increasing delay between retries |
| Health Endpoint | /health API endpoint |
| Models Endpoint | /v1/models API endpoint |
| Metrics Endpoint | /metrics Prometheus endpoint |

---

## Out of Scope

The following items are explicitly excluded from Task 006.c:

- **Provider Registry implementation** - Task 004.c
- **Fallback routing logic** - Provider Registry handles
- **vLLM server monitoring** - Infrastructure responsibility
- **GPU management** - System administration
- **Prometheus scraping** - External monitoring systems
- **Alerting configuration** - Operations responsibility
- **Historical health data storage** - Not persisted
- **Health check scheduling** - Called on demand
- **Custom health check endpoints** - Standard endpoints only
- **vLLM cluster coordination** - Single instance focus

---

## Functional Requirements

### Health Check Endpoint Queries

- FR-001: Health checker MUST query /health endpoint
- FR-002: Health checker MUST parse /health response
- FR-003: Health checker MUST query /v1/models as backup
- FR-004: Health checker MUST timeout after 10 seconds
- FR-005: Health checker MUST handle connection failures
- FR-006: Health checker MUST handle HTTP errors
- FR-007: Health checker MUST return HealthStatus result

### Health Status Determination

- FR-008: Return Healthy when /health returns 200 in <1s
- FR-009: Return Degraded when /health returns 200 in >5s
- FR-010: Return Unhealthy when /health returns non-200
- FR-011: Return Unhealthy when connection fails
- FR-012: Return Unknown when timeout occurs
- FR-013: Include response time in health result
- FR-014: Include error message when unhealthy

### Metrics Endpoint Integration

- FR-015: Health checker MUST query /metrics endpoint (optional)
- FR-016: Health checker MUST parse Prometheus format
- FR-017: Health checker MUST extract vllm_num_requests_running
- FR-018: Health checker MUST extract vllm_num_requests_waiting
- FR-019: Health checker MUST extract vllm_gpu_cache_usage_perc
- FR-020: Health checker MUST calculate load score

### Load Status Determination

- FR-021: Include current request count in status
- FR-022: Include queue depth in status
- FR-023: Include GPU utilization in status
- FR-024: Flag as overloaded when queue > threshold
- FR-025: Flag as overloaded when GPU > 95%
- FR-026: Load thresholds MUST be configurable

### Model Availability Check

- FR-027: Health checker MUST list loaded models
- FR-028: Health checker MUST check specific model available
- FR-029: Health checker MUST report model status
- FR-030: Return warning if expected model not loaded
- FR-031: Include model list in detailed status

### Error Response Parsing

- FR-032: Parser MUST extract error object from response
- FR-033: Parser MUST extract error.message field
- FR-034: Parser MUST extract error.type field
- FR-035: Parser MUST extract error.code field
- FR-036: Parser MUST extract error.param field
- FR-037: Parser MUST handle missing optional fields
- FR-038: Parser MUST handle malformed error JSON

### Error Classification

- FR-039: Classify 400 as client error (permanent)
- FR-040: Classify 401 as auth error (permanent)
- FR-041: Classify 403 as forbidden error (permanent)
- FR-042: Classify 404 as not found error (permanent)
- FR-043: Classify 429 as rate limit (transient)
- FR-044: Classify 500 as server error (transient)
- FR-045: Classify 502/503/504 as gateway errors (transient)
- FR-046: Classify connection errors as transient
- FR-047: Classify timeouts as transient

### Exception Mapping

- FR-048: Map to VllmConnectionException on connection failure
- FR-049: Map to VllmTimeoutException on timeout
- FR-050: Map to VllmAuthException on 401
- FR-051: Map to VllmModelNotFoundException on model 404
- FR-052: Map to VllmRequestException on 400
- FR-053: Map to VllmRateLimitException on 429
- FR-054: Map to VllmServerException on 5xx
- FR-055: Map to VllmOutOfMemoryException on CUDA OOM
- FR-056: Include original response in exception

### Exception Content

- FR-057: All exceptions MUST include error code
- FR-058: All exceptions MUST include message
- FR-059: All exceptions MUST include request ID
- FR-060: All exceptions MUST include timestamp
- FR-061: All exceptions MUST include IsTransient flag
- FR-062: Exceptions MUST implement IVllmException interface

### Retry Classification

- FR-063: IsTransient MUST be true for connection errors
- FR-064: IsTransient MUST be true for timeouts
- FR-065: IsTransient MUST be true for 429
- FR-066: IsTransient MUST be true for 5xx
- FR-067: IsTransient MUST be false for 400
- FR-068: IsTransient MUST be false for 401/403
- FR-069: IsTransient MUST be false for 404
- FR-070: IsTransient MUST be false for invalid input

### CLI Integration

- FR-071: `acode providers health` MUST show vLLM status
- FR-072: `acode providers status vllm` MUST show details
- FR-073: CLI MUST show health status (Healthy/Degraded/etc)
- FR-074: CLI MUST show response time
- FR-075: CLI MUST show loaded models
- FR-076: CLI MUST show error message when unhealthy
- FR-077: CLI MUST exit 0 on healthy, 1 on unhealthy

### Logging

- FR-078: Log health check start
- FR-079: Log health check result with status
- FR-080: Log response time
- FR-081: Log error details when unhealthy
- FR-082: Log status transitions (healthy→unhealthy)
- FR-083: Use structured logging fields

---

## Non-Functional Requirements

### Performance

- NFR-001: Health check MUST complete in < 2 seconds (happy path)
- NFR-002: Metrics parsing MUST complete in < 10ms
- NFR-003: Error parsing MUST complete in < 1ms
- NFR-004: Health check MUST NOT block other requests
- NFR-005: Connection MUST reuse existing pool

### Reliability

- NFR-006: Health check MUST NOT throw exceptions
- NFR-007: Health check MUST return result even on error
- NFR-008: Health check MUST timeout reliably
- NFR-009: Health check MUST handle malformed responses
- NFR-010: Status MUST be accurate within 10 seconds

### Security

- NFR-011: Health check MUST NOT log request content
- NFR-012: Error messages MUST NOT expose internal details
- NFR-013: Metrics MUST NOT expose sensitive information
- NFR-014: Health endpoint MUST NOT require auth (or use configured auth)

### Observability

- NFR-015: Health checks MUST be logged
- NFR-016: Status transitions MUST be logged
- NFR-017: Errors MUST include correlation IDs
- NFR-018: Metrics MUST be available for monitoring
- NFR-019: CLI output MUST be machine-parseable

### Maintainability

- NFR-020: All public APIs MUST have XML documentation
- NFR-021: Exception hierarchy MUST be clear
- NFR-022: Error codes MUST be documented
- NFR-023: Configuration MUST have defaults

---

## User Manual Documentation

### Overview

Health checking and error handling ensure Acode can detect vLLM problems and respond appropriately. Regular health checks inform routing decisions, while proper error handling enables graceful degradation.

### Quick Start

Check vLLM health:

```bash
$ acode providers health
┌─────────────────────────────────────────────────────────────┐
│ Provider Health                                              │
├─────────┬─────────┬──────────┬─────────────────────────────┤
│ Provider│ Status  │ Latency  │ Details                      │
├─────────┼─────────┼──────────┼─────────────────────────────┤
│ vllm    │ Healthy │ 45ms     │ 1 model loaded               │
└─────────┴─────────┴──────────┴─────────────────────────────┘
```

### Health Status Values

| Status | Description | Routing |
|--------|-------------|---------|
| Healthy | vLLM responding normally | Route requests |
| Degraded | vLLM slow or overloaded | Route with caution |
| Unhealthy | vLLM not responding | Route to fallback |
| Unknown | Cannot determine status | Route to fallback |

### Configuration

```yaml
model:
  providers:
    vllm:
      health:
        # Health check endpoint
        endpoint: /health
        
        # Timeout for health check
        timeout_seconds: 10
        
        # Response time thresholds
        healthy_threshold_ms: 1000
        degraded_threshold_ms: 5000
        
        # Load monitoring
        load_monitoring:
          enabled: true
          metrics_endpoint: /metrics
          queue_threshold: 10
          gpu_threshold_percent: 95
```

### Detailed Status

```bash
$ acode providers status vllm
┌─────────────────────────────────────────────────────────────┐
│ vLLM Provider Status                                         │
├─────────────────────────────────────────────────────────────┤
│ Status: Healthy                                              │
│ Endpoint: http://localhost:8000                              │
│ Response Time: 45ms                                          │
│                                                              │
│ Loaded Models:                                               │
│   - meta-llama/Llama-3.2-8B-Instruct                        │
│                                                              │
│ Load Metrics:                                                │
│   - Running Requests: 2                                      │
│   - Waiting Requests: 0                                      │
│   - GPU Cache Usage: 45%                                     │
│   - Load Status: Normal                                      │
└─────────────────────────────────────────────────────────────┘
```

### Error Codes

| Code | Type | Transient | Description |
|------|------|-----------|-------------|
| ACODE-VLM-001 | Connection | Yes | Cannot connect to vLLM |
| ACODE-VLM-002 | Timeout | Yes | Request timed out |
| ACODE-VLM-003 | NotFound | No | Model not found |
| ACODE-VLM-004 | Request | No | Invalid request |
| ACODE-VLM-005 | Server | Yes | vLLM server error |
| ACODE-VLM-011 | Auth | No | Authentication failed |
| ACODE-VLM-012 | RateLimit | Yes | Rate limited |
| ACODE-VLM-013 | OOM | Maybe | CUDA out of memory |

### Handling Errors

#### Transient Errors

Transient errors are automatically retried:

```
[WARN] vLLM request failed (transient), retrying (attempt 1/3)
[WARN] vLLM request failed (transient), retrying (attempt 2/3)
[INFO] vLLM request succeeded on retry
```

#### Permanent Errors

Permanent errors fail immediately:

```
[ERROR] vLLM request failed: Model 'nonexistent' not found
Error: ACODE-VLM-003 - Model not found
```

#### CUDA Out of Memory

```
[ERROR] vLLM CUDA out of memory
Suggestions:
  - Reduce max_tokens
  - Use a smaller model
  - Wait for other requests to complete
  - Increase GPU memory
```

### Fallback Configuration

Configure fallback to Ollama when vLLM is unhealthy:

```yaml
model:
  fallback:
    enabled: true
    providers:
      - vllm
      - ollama
    
    on_unhealthy: fallback
    on_degraded: continue  # or fallback
    on_error: fallback
```

### Troubleshooting

#### "Connection refused"

```bash
# Check vLLM is running
curl http://localhost:8000/health

# Check port binding
netstat -an | grep 8000
```

#### "Health check timeout"

```bash
# Increase timeout
model:
  providers:
    vllm:
      health:
        timeout_seconds: 30
```

#### "Degraded status"

```bash
# Check load metrics
$ acode providers status vllm

# If queue is high, requests are backing up
# Consider reducing concurrency or adding capacity
```

### CLI Exit Codes

| Code | Meaning |
|------|---------|
| 0 | All providers healthy |
| 1 | One or more providers unhealthy |
| 2 | Configuration error |

---

## Acceptance Criteria

### Health Check Queries

- [ ] AC-001: Queries /health endpoint
- [ ] AC-002: Parses /health response
- [ ] AC-003: Queries /v1/models as backup
- [ ] AC-004: Timeout after 10 seconds
- [ ] AC-005: Handles connection failures
- [ ] AC-006: Handles HTTP errors
- [ ] AC-007: Returns HealthStatus result

### Health Status

- [ ] AC-008: Healthy when <1s response
- [ ] AC-009: Degraded when >5s response
- [ ] AC-010: Unhealthy on non-200
- [ ] AC-011: Unhealthy on connection fail
- [ ] AC-012: Unknown on timeout
- [ ] AC-013: Includes response time
- [ ] AC-014: Includes error message

### Metrics Integration

- [ ] AC-015: Queries /metrics
- [ ] AC-016: Parses Prometheus format
- [ ] AC-017: Extracts running requests
- [ ] AC-018: Extracts waiting requests
- [ ] AC-019: Extracts GPU usage
- [ ] AC-020: Calculates load score

### Load Status

- [ ] AC-021: Includes request count
- [ ] AC-022: Includes queue depth
- [ ] AC-023: Includes GPU utilization
- [ ] AC-024: Flags overloaded queue
- [ ] AC-025: Flags overloaded GPU
- [ ] AC-026: Thresholds configurable

### Model Availability

- [ ] AC-027: Lists loaded models
- [ ] AC-028: Checks specific model
- [ ] AC-029: Reports model status
- [ ] AC-030: Warns if model missing
- [ ] AC-031: Includes model list

### Error Parsing

- [ ] AC-032: Extracts error object
- [ ] AC-033: Extracts message
- [ ] AC-034: Extracts type
- [ ] AC-035: Extracts code
- [ ] AC-036: Extracts param
- [ ] AC-037: Handles missing fields
- [ ] AC-038: Handles malformed JSON

### Error Classification

- [ ] AC-039: 400 is permanent
- [ ] AC-040: 401 is permanent
- [ ] AC-041: 403 is permanent
- [ ] AC-042: 404 is permanent
- [ ] AC-043: 429 is transient
- [ ] AC-044: 500 is transient
- [ ] AC-045: 502/503/504 transient
- [ ] AC-046: Connection transient
- [ ] AC-047: Timeout transient

### Exception Mapping

- [ ] AC-048: VllmConnectionException
- [ ] AC-049: VllmTimeoutException
- [ ] AC-050: VllmAuthException
- [ ] AC-051: VllmModelNotFoundException
- [ ] AC-052: VllmRequestException
- [ ] AC-053: VllmRateLimitException
- [ ] AC-054: VllmServerException
- [ ] AC-055: VllmOutOfMemoryException
- [ ] AC-056: Includes original response

### Exception Content

- [ ] AC-057: Includes error code
- [ ] AC-058: Includes message
- [ ] AC-059: Includes request ID
- [ ] AC-060: Includes timestamp
- [ ] AC-061: Includes IsTransient
- [ ] AC-062: Implements interface

### CLI Integration

- [ ] AC-063: health command works
- [ ] AC-064: status command works
- [ ] AC-065: Shows health status
- [ ] AC-066: Shows response time
- [ ] AC-067: Shows loaded models
- [ ] AC-068: Shows error message
- [ ] AC-069: Exit codes correct

### Logging

- [ ] AC-070: Logs check start
- [ ] AC-071: Logs result
- [ ] AC-072: Logs response time
- [ ] AC-073: Logs error details
- [ ] AC-074: Logs transitions
- [ ] AC-075: Uses structured fields

### Performance

- [ ] AC-076: Completes in <2s
- [ ] AC-077: Metrics parsing <10ms
- [ ] AC-078: Error parsing <1ms
- [ ] AC-079: Non-blocking
- [ ] AC-080: Reuses connections

---

## Testing Requirements

### Unit Tests

```
Tests/Unit/Infrastructure/Vllm/Health/
├── VllmHealthCheckerTests.cs
│   ├── Should_Return_Healthy()
│   ├── Should_Return_Degraded_On_Slow()
│   ├── Should_Return_Unhealthy_On_Error()
│   ├── Should_Return_Unknown_On_Timeout()
│   ├── Should_Include_ResponseTime()
│   └── Should_Query_Fallback_Endpoint()
│
├── VllmMetricsParserTests.cs
│   ├── Should_Parse_Prometheus_Format()
│   ├── Should_Extract_Running_Requests()
│   ├── Should_Extract_GPU_Usage()
│   └── Should_Calculate_Load_Score()
│
├── VllmErrorParserTests.cs
│   ├── Should_Extract_Error_Fields()
│   ├── Should_Handle_Missing_Fields()
│   └── Should_Handle_Malformed_JSON()
│
├── VllmErrorClassifierTests.cs
│   ├── Should_Classify_400_As_Permanent()
│   ├── Should_Classify_429_As_Transient()
│   ├── Should_Classify_500_As_Transient()
│   └── Should_Classify_Connection_As_Transient()
│
└── VllmExceptionMapperTests.cs
    ├── Should_Map_To_Correct_Exception()
    ├── Should_Include_Error_Code()
    └── Should_Include_IsTransient()
```

### Integration Tests

```
Tests/Integration/Vllm/Health/
├── VllmHealthIntegrationTests.cs
│   ├── Should_Check_Real_VllmHealth()
│   ├── Should_List_Models()
│   └── Should_Parse_Metrics()
```

### E2E Tests

```
Tests/E2E/Vllm/Health/
├── HealthCommandTests.cs
│   ├── Should_Show_Healthy_Status()
│   ├── Should_Show_Unhealthy_Status()
│   └── Should_Exit_With_Correct_Code()
```

---

## User Verification Steps

### Scenario 1: Healthy Check

1. Start vLLM server
2. Run `acode providers health`
3. Verify: Status shows Healthy
4. Verify: Response time shown
5. Verify: Exit code 0

### Scenario 2: Unhealthy Check

1. Stop vLLM server
2. Run `acode providers health`
3. Verify: Status shows Unhealthy
4. Verify: Error message shown
5. Verify: Exit code 1

### Scenario 3: Degraded Status

1. Overload vLLM server
2. Run `acode providers health`
3. Verify: Status shows Degraded
4. Verify: Response time >5s shown

### Scenario 4: Model Availability

1. Run `acode providers status vllm`
2. Verify: Loaded models listed
3. Verify: Model name correct

### Scenario 5: Load Metrics

1. Configure metrics endpoint
2. Run `acode providers status vllm`
3. Verify: GPU utilization shown
4. Verify: Queue depth shown

### Scenario 6: Error Classification

1. Send request with invalid params
2. Verify: VllmRequestException thrown
3. Verify: IsTransient is false
4. Verify: Error code ACODE-VLM-004

### Scenario 7: Transient Error

1. Cause connection failure
2. Verify: VllmConnectionException thrown
3. Verify: IsTransient is true
4. Verify: Retry attempted

### Scenario 8: CUDA OOM

1. Cause GPU OOM
2. Verify: VllmOutOfMemoryException thrown
3. Verify: Helpful message shown

---

## Implementation Prompt

### File Structure

```
src/AgenticCoder.Infrastructure/Vllm/Health/
├── VllmHealthChecker.cs
├── VllmHealthConfiguration.cs
├── VllmHealthResult.cs
├── VllmLoadStatus.cs
├── Metrics/
│   ├── VllmMetricsClient.cs
│   └── VllmMetricsParser.cs
├── Errors/
│   ├── VllmErrorParser.cs
│   ├── VllmErrorClassifier.cs
│   └── VllmExceptionMapper.cs
└── Exceptions/
    ├── IVllmException.cs
    ├── VllmOutOfMemoryException.cs
    └── ...
```

### VllmHealthChecker Implementation

```csharp
namespace AgenticCoder.Infrastructure.Vllm.Health;

public sealed class VllmHealthChecker : IHealthChecker
{
    private readonly VllmHttpClient _client;
    private readonly VllmHealthConfiguration _config;
    private readonly VllmMetricsClient _metrics;
    private readonly ILogger<VllmHealthChecker> _logger;
    
    public async Task<VllmHealthResult> CheckHealthAsync(
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            var response = await _client.GetAsync(
                _config.HealthEndpoint,
                cancellationToken);
            
            stopwatch.Stop();
            var responseTime = stopwatch.Elapsed;
            
            var status = DetermineStatus(response.StatusCode, responseTime);
            
            var models = await GetLoadedModelsAsync(cancellationToken);
            var load = await GetLoadStatusAsync(cancellationToken);
            
            return new VllmHealthResult
            {
                Status = status,
                ResponseTime = responseTime,
                Models = models,
                Load = load
            };
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (TimeoutException)
        {
            return VllmHealthResult.Unknown("Health check timed out");
        }
        catch (Exception ex)
        {
            return VllmHealthResult.Unhealthy(ex.Message);
        }
    }
    
    private HealthStatus DetermineStatus(HttpStatusCode code, TimeSpan responseTime)
    {
        if (code != HttpStatusCode.OK)
            return HealthStatus.Unhealthy;
        
        if (responseTime > _config.DegradedThreshold)
            return HealthStatus.Degraded;
        
        return HealthStatus.Healthy;
    }
}
```

### Error Codes

| Code | Message |
|------|---------|
| ACODE-VLM-001 | Unable to connect to vLLM |
| ACODE-VLM-002 | vLLM request timeout |
| ACODE-VLM-003 | Model not found |
| ACODE-VLM-004 | Invalid request |
| ACODE-VLM-005 | vLLM server error |
| ACODE-VLM-011 | Authentication failed |
| ACODE-VLM-012 | Rate limit exceeded |
| ACODE-VLM-013 | CUDA out of memory |
| ACODE-VLM-014 | Health check failed |
| ACODE-VLM-015 | Metrics unavailable |

### Implementation Checklist

1. [ ] Create VllmHealthConfiguration
2. [ ] Create VllmHealthResult
3. [ ] Create VllmLoadStatus
4. [ ] Implement VllmHealthChecker
5. [ ] Implement VllmMetricsClient
6. [ ] Implement VllmMetricsParser
7. [ ] Implement VllmErrorParser
8. [ ] Implement VllmErrorClassifier
9. [ ] Implement VllmExceptionMapper
10. [ ] Create IVllmException interface
11. [ ] Create VllmOutOfMemoryException
12. [ ] Integrate with CLI commands
13. [ ] Wire up DI registration
14. [ ] Write unit tests
15. [ ] Write integration tests
16. [ ] Add XML documentation

### Dependencies

- Task 006 (VllmProvider)
- Task 006.a (VllmHttpClient)
- Task 004.c (Provider Registry)
- System.Net.Http

### Verification Command

```bash
dotnet test --filter "FullyQualifiedName~Vllm.Health"
```

---

**End of Task 006.c Specification**