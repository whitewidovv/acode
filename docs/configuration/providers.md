# Provider Configuration

This guide explains how to configure model providers in Acode using the provider registry system.

## Overview

The provider registry manages multiple LLM providers (Ollama, vLLM, etc.) and intelligently routes requests based on:
- **Default provider** preference
- **Capability matching** (streaming, tools, models)
- **Health status** monitoring
- **Fallback chains** for resilience

## Configuration File

Provider configuration is defined in `.agent/config.yml` in your repository:

```yaml
schema_version: "1.0.0"

# Operating mode (affects provider validation)
mode: LocalOnly  # LocalOnly, Burst, or Airgapped

# Provider registry configuration
providers:
  default_provider: "ollama"  # Provider to use by default

  ollama:
    endpoint: "http://localhost:11434"
    timeout: 300  # Request timeout in seconds
    connect_timeout: 5  # Connection timeout in seconds
    max_retries: 3
    health_check_interval: 60  # Health check every 60 seconds
    fallback_provider: "vllm"  # Fallback if ollama fails
    enabled: true

  vllm:
    endpoint: "http://localhost:8000"
    timeout: 300
    connect_timeout: 5
    max_retries: 3
    health_check_interval: 60
    enabled: true
```

## Provider Properties

### Required Properties

- **`endpoint`** (string, URI): Provider API endpoint URL
  - Must be `http://` or `https://` scheme
  - In `LocalOnly` mode: Must be localhost/127.0.0.1
  - In `Airgapped` mode: All external endpoints blocked

### Optional Properties

- **`timeout`** (integer, default: 300): Request timeout in seconds
  - Minimum: 1 second
  - Recommended: 300 seconds for large responses

- **`connect_timeout`** (integer, default: 5): Connection timeout in seconds
  - Minimum: 1 second
  - Recommended: 5-10 seconds

- **`max_retries`** (integer, default: 3): Maximum retry attempts on failure
  - Minimum: 0 (no retries)
  - Recommended: 3 attempts

- **`health_check_interval`** (integer, default: 60): Health check frequency in seconds
  - Minimum: 10 seconds
  - Recommended: 60 seconds for local providers

- **`fallback_provider`** (string, optional): Provider ID to use if this provider fails
  - Must reference another registered provider
  - Prevents circular fallback chains

- **`enabled`** (boolean, default: true): Whether provider is active
  - Set to `false` to temporarily disable a provider

## Tool Call Retry Configuration

### Overview

Acode supports automatic retry with JSON repair when model-generated tool calls have malformed JSON arguments. This feature is particularly useful when working with local models that may occasionally generate invalid JSON.

### How It Works

1. **Initial Parsing**: Tool calls are parsed and validated
2. **Automatic Repair**: If JSON is malformed, automatic repair attempts common fixes
3. **Retry on Failure**: If repair fails, re-request from model with error details
4. **Exponential Backoff**: Delays between retries increase exponentially
5. **Success or Exhaustion**: Returns corrected tool calls or throws exception after max retries

### Configuration Properties

#### `tool_call_retry.max_retries`

- **Type**: integer
- **Default**: 3
- **Range**: 0 (no retries) to 10 (max)
- **Description**: Maximum number of retry attempts when tool call parsing fails

```yaml
providers:
  ollama:
    tool_call_retry:
      max_retries: 3  # Try up to 3 times
```

**Recommendations**:
- **Local models**: 3 retries (models may need multiple attempts)
- **Production models**: 2 retries (faster failure)
- **Development/testing**: 0 retries (fail fast for debugging)

#### `tool_call_retry.enable_auto_repair`

- **Type**: boolean
- **Default**: true
- **Description**: Enable automatic JSON repair before retrying

```yaml
providers:
  ollama:
    tool_call_retry:
      enable_auto_repair: true  # Attempt automatic fixes
```

**Repair Heuristics** (when enabled):
- Remove trailing commas in objects and arrays
- Add missing closing braces `}` and brackets `]`
- Replace single quotes with double quotes
- Close unclosed strings
- Balance nested structures

**When to Disable**:
- Strict validation required (no automatic fixes)
- Debugging JSON generation issues
- Testing error handling paths

#### `tool_call_retry.retry_delay_ms`

- **Type**: integer
- **Default**: 100
- **Range**: 10ms to 5000ms
- **Description**: Base delay in milliseconds between retry attempts (with exponential backoff)

```yaml
providers:
  ollama:
    tool_call_retry:
      retry_delay_ms: 100  # Start with 100ms delay
```

**Exponential Backoff**:
- Attempt 1: `delay_ms` (100ms)
- Attempt 2: `delay_ms * 2` (200ms)
- Attempt 3: `delay_ms * 4` (400ms)
- Attempt 4: `delay_ms * 8` (800ms)

**Recommendations**:
- **Local models**: 100ms (fast local inference)
- **Remote models**: 500ms+ (network latency)
- **Overloaded providers**: 1000ms+ (give provider time to recover)

#### `tool_call_retry.repair_timeout_ms`

- **Type**: integer
- **Default**: 100
- **Range**: 10ms to 1000ms
- **Description**: Maximum time to spend attempting JSON repair

```yaml
providers:
  ollama:
    tool_call_retry:
      repair_timeout_ms: 100  # Timeout after 100ms
```

**Recommendations**:
- **Simple JSON**: 50ms sufficient
- **Complex nested structures**: 100-200ms
- **Very large arguments**: 500ms+

#### `tool_call_retry.strict_validation`

- **Type**: boolean
- **Default**: true
- **Description**: Enforce strict JSON validation rules

```yaml
providers:
  ollama:
    tool_call_retry:
      strict_validation: true  # Strict mode
```

**Strict Mode** (true):
- Rejects duplicate keys in objects
- Enforces JSON spec compliance
- No lenient parsing

**Lenient Mode** (false):
- Allows duplicate keys (last value wins)
- More forgiving of spec violations
- Use only for legacy compatibility

#### `tool_call_retry.max_nesting_depth`

- **Type**: integer
- **Default**: 64
- **Range**: 1 to 128
- **Description**: Maximum allowed JSON nesting depth

```yaml
providers:
  ollama:
    tool_call_retry:
      max_nesting_depth: 64  # Prevent deeply nested structures
```

**Security Consideration**: Limits deeply nested JSON that could cause stack overflow or excessive memory usage.

#### `tool_call_retry.max_argument_size`

- **Type**: integer
- **Default**: 1048576 (1MB)
- **Range**: 1024 (1KB) to 10485760 (10MB)
- **Description**: Maximum size in bytes for tool call arguments

```yaml
providers:
  ollama:
    tool_call_retry:
      max_argument_size: 1048576  # 1MB limit
```

**Recommendations**:
- **Simple tools**: 10KB-100KB
- **File operations**: 1MB (default)
- **Large data transfers**: 5MB+ (with caution)

**Security Consideration**: Prevents memory exhaustion from oversized arguments.

#### `tool_call_retry.retry_prompt_template`

- **Type**: string
- **Default**: See below
- **Description**: Template for retry prompts sent to the model

```yaml
providers:
  ollama:
    tool_call_retry:
      retry_prompt_template: |
        The previous tool call had an error in the JSON arguments.

        Error: {error_message}
        Tool name: {tool_name}
        Malformed JSON: {malformed_json}

        Please provide the corrected tool call with valid JSON arguments.
```

**Template Variables**:
- `{error_message}`: Parsing error description
- `{error_position}`: Character position of error (if available)
- `{malformed_json}`: The original malformed JSON
- `{tool_name}`: Name of the tool that failed
- `{schema_example}`: Expected JSON schema (if available)

**Customization Tips**:
- Keep prompts concise (models may ignore long corrections)
- Include specific error details for better correction
- Provide schema examples if tools have complex arguments
- Use clear, direct language

### Complete Example Configuration

```yaml
providers:
  default_provider: "ollama"

  ollama:
    endpoint: "http://localhost:11434"
    timeout: 300
    max_retries: 3

    # Tool call retry configuration
    tool_call_retry:
      # Retry behavior
      max_retries: 3                  # Up to 3 retry attempts
      retry_delay_ms: 100             # Start with 100ms, exponential backoff

      # JSON repair
      enable_auto_repair: true        # Attempt automatic fixes
      repair_timeout_ms: 100          # 100ms timeout for repair

      # Validation
      strict_validation: true         # Strict JSON compliance
      max_nesting_depth: 64           # Limit nesting depth
      max_argument_size: 1048576      # 1MB max argument size

      # Custom retry prompt (optional)
      retry_prompt_template: |
        Error in tool call arguments: {error_message}
        Tool: {tool_name}
        Invalid JSON: {malformed_json}

        Please provide corrected JSON arguments.
```

### Error Codes

When tool call parsing fails, error codes indicate the specific issue:

| Code | Description | Auto-Repair | Retry Helpful |
|------|-------------|-------------|---------------|
| ACODE-TLP-001 | Missing function definition | No | Yes |
| ACODE-TLP-002 | Empty function name | No | Yes |
| ACODE-TLP-003 | Invalid function name format | No | Yes |
| ACODE-TLP-004 | Malformed JSON arguments | **Yes** | **Yes** |
| ACODE-TLP-005 | Function name too long | No | Yes |
| ACODE-TLP-006 | Internal validation error | No | Maybe |

See [Error Code Documentation](../error-codes/ollama-tool-call-errors.md) for detailed descriptions.

### Retry Scenarios

#### Scenario 1: Trailing Comma (Repairable)

```json
// Model generates:
{
  "function": {
    "name": "read_file",
    "arguments": "{\"path\": \"test.txt\",}"
  }
}

// Auto-repair fixes:
{"path": "test.txt"}

// Result: Success without retry
```

#### Scenario 2: Unbalanced Braces (Repairable)

```json
// Model generates:
{
  "arguments": "{\"path\": \"test.txt\", \"mode\": \"r\""
}

// Auto-repair fixes:
{"path": "test.txt", "mode": "r"}

// Result: Success without retry
```

#### Scenario 3: Invalid JSON (Retry Needed)

```json
// Model generates:
{
  "arguments": "not json at all"
}

// Auto-repair fails
// Retry prompt sent to model:
"Error: Failed to parse JSON. Please provide valid JSON arguments."

// Model corrects:
{
  "arguments": "{\"path\": \"test.txt\"}"
}

// Result: Success after 1 retry
```

#### Scenario 4: Persistent Malformed JSON (Exhausted)

```json
// Model generates malformed JSON repeatedly
// After 3 retries, all attempts fail

// Exception thrown:
ToolCallRetryExhaustedException:
  Failed to parse tool calls after 3 retry attempts.
```

### Monitoring and Telemetry

#### Audit Logging

All tool call parsing errors are logged to the audit trail:

```jsonl
{"timestamp":"2026-01-13T10:30:45.123Z","level":"error","component":"ToolCallParser","error_code":"ACODE-TLP-004","message":"Unable to parse arguments: Trailing comma","tool_name":"read_file","repair_attempted":true,"repair_success":true,"retry_count":0}

{"timestamp":"2026-01-13T10:31:12.456Z","level":"warning","component":"ToolCallRetryHandler","error_code":"ACODE-TLP-004","message":"Retry attempt 1 of 3","tool_name":"write_file","malformed_json":"{\"path\": \"out.txt\"","retry_delay_ms":100}

{"timestamp":"2026-01-13T10:31:23.789Z","level":"info","component":"ToolCallRetryHandler","message":"Retry successful after 2 attempts","tool_name":"write_file"}
```

#### Metrics to Monitor

- **Repair Success Rate**: `repairs_successful / repairs_attempted`
- **Retry Success Rate**: `retries_successful / retries_attempted`
- **Average Retries per Request**: Track how often retries are needed
- **Error Code Distribution**: Which errors are most common
- **Retry Exhaustion Rate**: How often max retries are exceeded

#### Alerts

Consider alerting when:
- Retry exhaustion rate > 5% (model quality issue)
- Average retries per request > 1 (consistent malformed JSON)
- ACODE-TLP-004 errors > 10% of requests (repair not effective)

### Performance Considerations

#### Latency Impact

Retries add latency to requests:

```
Base request time: 2000ms
+ Retry 1: 100ms delay + 2000ms = 2100ms
+ Retry 2: 200ms delay + 2000ms = 2200ms
+ Retry 3: 400ms delay + 2000ms = 2400ms
-------------------------------------------
Total with 3 retries: 8700ms
```

**Optimization**:
- Set `max_retries` appropriately (don't over-retry)
- Use `retry_delay_ms: 50` for latency-sensitive applications
- Monitor retry rates and improve model quality if high

#### Memory Usage

- **JSON Repair**: Temporary allocations during repair (typically < 1KB)
- **Retry History**: Stores original and retry requests in memory
- **Argument Size Limit**: Controls maximum memory per tool call

### Troubleshooting

#### High Retry Rates

**Symptoms**: Many requests require 2+ retries

**Causes**:
- Model not fine-tuned for tool calling
- Tool schemas too complex
- Insufficient prompt engineering

**Solutions**:
1. Fine-tune model on tool calling examples
2. Simplify tool argument schemas
3. Provide clearer tool descriptions
4. Use retry prompt template to guide model

#### Retry Exhaustion

**Symptoms**: `ToolCallRetryExhaustedException` thrown frequently

**Causes**:
- Model fundamentally unable to generate valid JSON
- Tool schema too complex for model
- max_retries set too low

**Solutions**:
1. Increase `max_retries` (temporary fix)
2. Switch to more capable model
3. Simplify tool arguments
4. Enable `enable_auto_repair` if disabled

#### Slow Retries

**Symptoms**: Retries take too long, impacting UX

**Causes**:
- `retry_delay_ms` too high
- Exponential backoff accumulating
- Remote model with high latency

**Solutions**:
1. Reduce `retry_delay_ms` to 50ms
2. Reduce `max_retries` to fail faster
3. Use local model instead of remote
4. Cache common tool call patterns

### Best Practices

#### 1. Use Auto-Repair for Local Models

```yaml
ollama:
  tool_call_retry:
    enable_auto_repair: true  # Local models benefit from repair
    max_retries: 3
```

#### 2. Fail Fast in Development

```yaml
ollama:
  tool_call_retry:
    max_retries: 0             # Fail immediately
    enable_auto_repair: false  # See raw errors
```

#### 3. Customize Retry Prompts

```yaml
ollama:
  tool_call_retry:
    retry_prompt_template: |
      JSON Error: {error_message}
      Tool: {tool_name}

      Fix the JSON and try again. Example: {schema_example}
```

#### 4. Monitor Retry Metrics

Track retry rates to identify:
- Tools with complex schemas needing simplification
- Models requiring fine-tuning
- Patterns in malformed JSON (inform repair heuristics)

#### 5. Set Appropriate Limits

```yaml
ollama:
  tool_call_retry:
    max_nesting_depth: 32      # Lower if tools use simple JSON
    max_argument_size: 102400  # 100KB if tools don't need large data
```

## Provider Selection

Acode uses intelligent provider selection:

### 1. Default Provider Selection

When no specific provider is requested, the `default_provider` is used:

```yaml
providers:
  default_provider: "ollama"  # Always try ollama first
```

### 2. Capability-Based Selection

Requests are matched to providers based on capabilities:

- **Streaming**: If `stream: true`, requires `SupportsStreaming`
- **Tools**: If tools provided, requires `SupportsTools`
- **Models**: Provider must support requested model

Example: Streaming request automatically selects streaming-capable provider.

### 3. Health-Aware Selection

Providers are monitored for health:
- **Unknown**: Not checked yet (acceptable for selection)
- **Healthy**: Passing health checks (preferred)
- **Degraded**: Partial failures (used if no healthy)
- **Unhealthy**: Failing health checks (skipped)

Selection prefers healthy providers, falls back to degraded/unknown if needed.

### 4. Fallback Chain

If a provider fails, the system follows the fallback chain:

```yaml
providers:
  ollama:
    endpoint: "http://localhost:11434"
    fallback_provider: "vllm"  # If ollama fails, try vllm

  vllm:
    endpoint: "http://localhost:8000"
    # No fallback - final provider in chain
```

## Operating Mode Validation

Provider endpoints are validated against the operating mode:

### LocalOnly Mode

- ✅ Allowed: `http://localhost:*`, `http://127.0.0.1:*`
- ⚠️  Warning: External endpoints logged but allowed
- 🔒 No external LLM APIs (OpenAI, Anthropic, etc.)

```yaml
mode: LocalOnly
providers:
  ollama:
    endpoint: "http://localhost:11434"  # OK
```

### Burst Mode

- ✅ Allowed: All HTTP/HTTPS endpoints
- ⚠️  Warning: External compute allowed, no external LLM APIs
- 🔒 No external LLM APIs

```yaml
mode: Burst
providers:
  vllm-cloud:
    endpoint: "https://my-vllm-instance.example.com"  # OK
```

### Airgapped Mode

- ✅ Allowed: `http://localhost:*`, `http://127.0.0.1:*` only
- ❌ Blocked: All external endpoints
- 🔒 Strict network isolation

```yaml
mode: Airgapped
providers:
  ollama:
    endpoint: "http://localhost:11434"  # OK
  vllm:
    endpoint: "http://192.168.1.100:8000"  # BLOCKED
```

## Health Checks

The provider registry performs periodic health checks:

### Health Check Behavior

1. **Initial State**: Providers start as `Unknown`
2. **First Check**: Scheduled after `health_check_interval`
3. **Status Updates**: Health status updated on each check
4. **Consecutive Failures**: Tracked for degraded status determination

### Health Status Transitions

```
Unknown → Healthy     (First successful check)
Healthy → Degraded    (1-2 consecutive failures)
Degraded → Unhealthy  (3+ consecutive failures)
Unhealthy → Healthy   (Successful check resets failures)
```

### Manual Health Checks

Check provider health manually:

```bash
acode providers health
```

Output:
```
Provider: ollama
  Status: Healthy
  Last Checked: 2026-01-12T10:30:45Z
  Consecutive Failures: 0

Provider: vllm
  Status: Degraded
  Last Checked: 2026-01-12T10:30:42Z
  Last Error: Connection timeout
  Consecutive Failures: 2
```

## Example Configurations

### Single Local Provider

```yaml
providers:
  default_provider: "ollama"

  ollama:
    endpoint: "http://localhost:11434"
    timeout: 300
    max_retries: 3
```

### Multiple Providers with Fallback

```yaml
providers:
  default_provider: "ollama"

  ollama:
    endpoint: "http://localhost:11434"
    fallback_provider: "vllm"
    max_retries: 2  # Try twice, then fallback

  vllm:
    endpoint: "http://localhost:8000"
    max_retries: 3
```

### High-Availability Setup

```yaml
providers:
  default_provider: "vllm-primary"

  vllm-primary:
    endpoint: "http://vllm-1.local:8000"
    health_check_interval: 30  # Check frequently
    fallback_provider: "vllm-secondary"

  vllm-secondary:
    endpoint: "http://vllm-2.local:8000"
    health_check_interval: 30
    fallback_provider: "ollama"  # Final fallback

  ollama:
    endpoint: "http://localhost:11434"
    # No fallback - last resort
```

### Development vs Production

**Development** (`.agent/config.yml`):
```yaml
mode: LocalOnly
providers:
  default_provider: "ollama"
  ollama:
    endpoint: "http://localhost:11434"
    timeout: 600  # Longer timeout for debugging
    max_retries: 1  # Fail fast for quick feedback
```

**Production** (`.agent/config.yml`):
```yaml
mode: Burst
providers:
  default_provider: "vllm-prod"
  vllm-prod:
    endpoint: "https://vllm.prod.example.com"
    timeout: 300
    max_retries: 3
    fallback_provider: "ollama"
    health_check_interval: 30

  ollama:
    endpoint: "http://localhost:11434"
    # Fallback for when cloud provider unavailable
```

## Troubleshooting

### Provider Not Found

**Symptoms**: `ProviderNotFoundException: Provider 'xyz' not found`

**Causes**:
- Provider not configured in `.agent/config.yml`
- Typo in provider ID
- Provider disabled (`enabled: false`)

**Solutions**:
1. Check provider is defined in config
2. Verify provider ID spelling matches
3. Ensure `enabled: true` (or omit for default)

### No Capable Provider

**Symptoms**: `NoCapableProviderException: No provider capable of handling the request`

**Causes**:
- Request requires streaming, no provider supports it
- Request requires tools, no provider supports it
- All providers are unhealthy

**Solutions**:
1. Check provider capabilities match request needs
2. Run `acode providers health` to check status
3. Reduce request requirements (disable streaming/tools)
4. Add provider with required capabilities

### Connection Timeout

**Symptoms**: `Connection timeout after 5 seconds`

**Causes**:
- Provider endpoint not reachable
- Firewall blocking connection
- Provider not running

**Solutions**:
1. Verify provider is running: `curl http://localhost:11434/api/version`
2. Check firewall rules
3. Increase `connect_timeout` if network is slow
4. Verify endpoint URL is correct

### Request Timeout

**Symptoms**: `Request timeout after 300 seconds`

**Causes**:
- Large response taking too long
- Model processing complex request
- Provider overloaded

**Solutions**:
1. Increase `timeout` value
2. Use smaller/faster model
3. Reduce request complexity
4. Check provider resource usage

### Health Check Failures

**Symptoms**: Provider status shows `Unhealthy` or `Degraded`

**Causes**:
- Provider intermittently unavailable
- Network issues
- Provider overloaded

**Solutions**:
1. Check provider logs for errors
2. Increase `health_check_interval` to reduce check frequency
3. Verify provider has sufficient resources
4. Configure fallback provider for resilience

## Best Practices

### 1. Always Configure Fallbacks

```yaml
providers:
  primary:
    endpoint: "..."
    fallback_provider: "secondary"  # Always have a backup

  secondary:
    endpoint: "..."
```

### 2. Use Appropriate Timeouts

- **Local providers**: 300s timeout sufficient
- **Remote providers**: 600s+ for large responses
- **Connect timeout**: Keep at 5-10s for fast failure detection

### 3. Monitor Health Status

- Check health regularly: `acode providers health`
- Set up alerts for unhealthy providers
- Use health checks to detect issues early

### 4. Test Fallback Chains

- Intentionally disable primary provider
- Verify fallback works as expected
- Ensure fallback chain doesn't loop

### 5. Respect Operating Modes

- **LocalOnly**: Use only localhost providers for privacy
- **Airgapped**: Test that external endpoints are blocked
- **Burst**: Validate cloud providers before deploying

## CLI Commands

### List Providers

```bash
acode providers list
```

### Check Health

```bash
acode providers health
```

### Test Connection

```bash
acode providers test <provider-id>
```

## See Also

- [Operating Modes](../operating-modes.md)
- [Configuration Schema](../../data/config-schema.json)
- [Provider Registry API](../api/provider-registry.md)
