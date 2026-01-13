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
