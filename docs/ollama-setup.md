# Ollama Provider Setup Guide

This guide provides complete setup instructions for using Ollama as your local AI inference provider with Acode.

## Prerequisites

Before using the Ollama provider, ensure you have:

### 1. Ollama Installation

**Minimum Version:** 0.1.23 or later

Download and install Ollama from [https://ollama.ai](https://ollama.ai)

**Installation Verification:**

```bash
# Check Ollama is installed and get version
ollama --version

# Expected output: ollama version 0.1.30 (or later)
```

### 2. Start Ollama Server

Ollama must be running before Acode can use it:

```bash
# Start Ollama server (runs in foreground)
ollama serve

# Or run as background service (varies by OS)
# Linux/macOS: ollama serve &
# Windows: Install as Windows Service via installer
```

**Verify server is running:**

```bash
# Check server is listening
curl http://localhost:11434/api/tags

# Expected: JSON response with model list
```

### 3. Download a Model

At least one model must be available locally:

```bash
# Download recommended model (8B parameters, ~4.7GB)
ollama pull llama3.2:latest

# Or download smaller model for testing (3B parameters, ~2GB)
ollama pull llama3.2:3b

# Verify model downloaded
ollama list

# Expected output shows model name, size, and modified date
```

**Recommended Models:**

| Model | Size | Use Case | Command |
|-------|------|----------|---------|
| llama3.2:latest | 4.7GB | General purpose, best quality | `ollama pull llama3.2:latest` |
| llama3.2:3b | 2GB | Faster inference, lower quality | `ollama pull llama3.2:3b` |
| codellama:latest | 3.8GB | Code-specific tasks | `ollama pull codellama:latest` |
| mistral:latest | 4.1GB | Good balance of speed/quality | `ollama pull mistral:latest` |

## Quick Start

Minimal steps to get Ollama working with Acode:

```yaml
# 1. Create .agent/config.yml in your project root
model:
  default_provider: ollama

  providers:
    ollama:
      enabled: true
      endpoint: http://localhost:11434
      default_model: llama3.2:latest
```

```bash
# 2. Verify Ollama is running
curl http://localhost:11434/api/tags

# 3. Run smoke test to verify integration
./scripts/smoke-test-ollama.sh

# 4. Start using Acode with Ollama
acode chat "Hello, can you help me with this code?"
```

**Success Criteria:**
- Ollama responds to /api/tags
- Smoke test shows all tests PASS
- Acode commands complete without errors

## Configuration

### Complete Configuration Example

```yaml
model:
  default_provider: ollama

  providers:
    ollama:
      # Required: Base URL for Ollama API
      endpoint: http://localhost:11434

      # Default model to use (must be pulled via ollama pull)
      default_model: llama3.2:latest

      # Timeouts (seconds)
      request_timeout: 120        # Default: 120s for completions
      health_check_timeout: 5     # Default: 5s for health checks

      # Retry configuration
      max_retries: 3              # Default: 3 retry attempts
      enable_retry: true          # Default: true

      # Model parameters (can override per request)
      parameters:
        temperature: 0.7          # Default: 0.7 (0.0 = deterministic, 1.0 = creative)
        top_p: 0.9                # Default: 0.9 (nucleus sampling)
        max_tokens: 2048          # Default: 2048 (max output length)
```

### Configuration Options

| Setting | Type | Default | Description |
|---------|------|---------|-------------|
| `endpoint` | string | `http://localhost:11434` | Ollama server URL |
| `default_model` | string | `llama3.2:latest` | Model to use by default |
| `request_timeout` | int | `120` | Max seconds to wait for completion |
| `health_check_timeout` | int | `5` | Max seconds for health check |
| `max_retries` | int | `3` | Retry attempts on transient errors |
| `enable_retry` | bool | `true` | Enable automatic retries |
| `parameters.temperature` | float | `0.7` | Randomness (0.0-1.0) |
| `parameters.top_p` | float | `0.9` | Nucleus sampling threshold |
| `parameters.max_tokens` | int | `2048` | Maximum output tokens |

### Environment Variable Overrides

*Note: Environment variable overrides are planned for a future release.*

Currently, all configuration must be specified in `.agent/config.yml`.

### Timeout Tuning

Adjust timeouts based on your hardware and model size:

**Fast Hardware (GPU, high-end CPU):**
```yaml
request_timeout: 60
health_check_timeout: 2
```

**Slow Hardware (CPU-only, older systems):**
```yaml
request_timeout: 300
health_check_timeout: 10
```

**Large Models (70B+ parameters):**
```yaml
request_timeout: 600  # 10 minutes
```

### Retry Configuration

Control how Acode handles transient failures:

**Aggressive Retries (flaky network):**
```yaml
max_retries: 5
enable_retry: true
```

**No Retries (fail fast):**
```yaml
max_retries: 0
enable_retry: false
```

## Troubleshooting

### Connection Refused

**Symptoms:**
- Error: `OllamaConnectionException: Connection to http://localhost:11434 refused`
- Health check fails immediately

**Resolution:**
```bash
# 1. Verify Ollama is running
ps aux | grep ollama

# 2. If not running, start it
ollama serve

# 3. Check it's listening on correct port
curl http://localhost:11434/api/tags

# 4. If using non-default port, update config.yml endpoint
```

### Model Not Found

**Symptoms:**
- Error: `model 'llama3.2:latest' not found`
- List models returns empty or doesn't include requested model

**Resolution:**
```bash
# 1. List available models
ollama list

# 2. If model missing, pull it
ollama pull llama3.2:latest

# 3. Verify model appears in list
ollama list | grep llama3.2

# 4. Update config.yml to use available model
```

### Timeout Errors

**Symptoms:**
- Error: `OllamaTimeoutException: Request exceeded timeout of 120s`
- Requests never complete

**Resolution:**
```bash
# 1. Check system resources
htop  # or Task Manager on Windows

# 2. If CPU/memory maxed, try smaller model
ollama pull llama3.2:3b

# 3. Increase timeout in config.yml
model:
  providers:
    ollama:
      request_timeout: 300

# 4. Reduce max_tokens to speed up generation
      parameters:
        max_tokens: 512
```

### Memory Errors

**Symptoms:**
- Ollama crashes or refuses to load model
- Error: `failed to load model: out of memory`
- System freezes or swaps heavily

**Resolution:**
```bash
# 1. Check available memory
free -h  # Linux/macOS
# Windows: Task Manager → Performance → Memory

# 2. Use smaller quantized model
ollama pull llama3.2:3b-q4_0  # 4-bit quantization

# 3. Close other applications
# 4. Increase swap space (Linux)
sudo fallocate -l 8G /swapfile

# 5. Or use model with smaller context window
```

### Slow Generation

**Symptoms:**
- Responses take >60 seconds for simple prompts
- Tokens/second is <5

**Resolution:**
```bash
# 1. Check if using GPU acceleration
ollama ps  # Shows model + "on GPU" if accelerated

# 2. If CPU-only, consider:
#    - Smaller model (3b instead of 8b)
#    - Reduce max_tokens
#    - Reduce temperature (faster sampling)

# 3. Update config for speed
model:
  providers:
    ollama:
      parameters:
        max_tokens: 256
        temperature: 0.3

# 4. Check background processes aren't CPU-bound
```

### Tool Call Failures

**Symptoms:**
- Tool calling doesn't work
- Functions not invoked

**Resolution:**

*Note: Tool calling support is implemented in Task 007d and requires:*
- A model that supports function calling (e.g., llama3.2:latest with function calling enabled)
- Tool Schema Registry (Task 007) to be implemented

**Current Status:** Tool calling is not yet available in this version. Basic chat completion is fully supported.

## Version Compatibility

### Tested Versions

| Ollama Version | Status | Notes |
|----------------|--------|-------|
| 0.1.23 | Minimum | Oldest supported version |
| 0.1.25 | Tested | Stable |
| 0.1.27 | Tested | Recommended |
| 0.1.30+ | Tested | Latest features, tool calling improved |

### Version Warnings

**Below 0.1.23:**
- Some API endpoints may not exist
- Response format may differ
- **Not supported** - please upgrade

**Above 0.1.35 (untested):**
- Should work, but not explicitly tested
- API changes may cause issues
- Report bugs if encountered

### Check Your Version

```bash
ollama --version

# If below minimum:
# macOS: brew upgrade ollama
# Linux: Download latest from ollama.ai
# Windows: Download installer from ollama.ai
```

## Diagnostic Commands

Use these commands to diagnose issues:

```bash
# Health Check
curl http://localhost:11434/api/tags
# Expected: JSON with models array

# List Models
ollama list
# Expected: Table of installed models

# Test Completion (simple)
curl http://localhost:11434/api/generate -d '{
  "model": "llama3.2:latest",
  "prompt": "Say hello",
  "stream": false
}'
# Expected: JSON with response text

# Check Ollama Logs
journalctl -u ollama -f  # Linux systemd
# macOS/Windows: check terminal where ollama serve is running

# Check System Resources
htop  # Linux/macOS
# Windows: Task Manager

# Verify Model Loaded
ollama ps
# Shows currently loaded models in memory
```

## Additional Resources

- [Ollama Documentation](https://github.com/ollama/ollama/blob/main/README.md)
- [Ollama Model Library](https://ollama.ai/library)
- [Acode Configuration Reference](./CONFIG.md)
- [Acode Troubleshooting Guide](./README.md#troubleshooting)

## Next Steps

After setup is complete:

1. **Run Smoke Tests:** `./scripts/smoke-test-ollama.sh`
2. **Try a Simple Command:** `acode chat "Hello"`
3. **Read Usage Guide:** See [README.md](../README.md) for Acode commands
4. **Configure for Your Workflow:** Adjust parameters in `.agent/config.yml`

---

**Need Help?**

- Check [Troubleshooting](#troubleshooting) section above
- Review [Ollama Issues](https://github.com/ollama/ollama/issues)
- File [Acode Issue](https://github.com/whitewidovv/acode/issues)
