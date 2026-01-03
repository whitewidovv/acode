# Operating Modes

**Last Updated**: 2025-01-03

This document defines the three operating modes for Acode and their security constraints.

## Table of Contents

- [Overview](#overview)
- [Mode Comparison](#mode-comparison)
- [LocalOnly Mode](#localonly-mode)
- [Burst Mode](#burst-mode)
- [Airgapped Mode](#airgapped-mode)
- [Mode Selection](#mode-selection)
- [Enforcement Mechanisms](#enforcement-mechanisms)
- [FAQ](#faq)

## Overview

Acode supports three operating modes that define what network access and external services the agent can use. These modes exist to give you precise control over data sovereignty and security.

**Key Principle**: **All modes block external LLM API calls** (OpenAI, Anthropic, Cohere, etc.). Acode is designed for local-first AI inference.

### The Three Modes

1. **LocalOnly** - Maximum privacy, no network access
2. **Burst** - Allows cloud compute for heavy workloads, but no external LLMs
3. **Airgapped** - Complete network isolation, strictest security

## Mode Comparison

| Feature | LocalOnly | Burst | Airgapped |
|---------|-----------|-------|-----------|
| **Network Access** | ❌ Blocked | ✅ Allowed | ❌ Blocked |
| **External LLM APIs** | ❌ Blocked | ❌ Blocked | ❌ Blocked |
| **Cloud Compute** | ❌ Blocked | ✅ Allowed | ❌ Blocked |
| **Local Models** | ✅ Required | ✅ Optional | ✅ Required |
| **Internet Browsing** | ❌ Blocked | ✅ Allowed | ❌ Blocked |
| **Package Downloads** | ❌ Blocked | ✅ Allowed | ❌ Blocked |
| **Git Push/Pull** | ❌ Blocked | ✅ Allowed | ❌ Blocked |
| **Localhost Access** | ✅ Allowed | ✅ Allowed | ✅ Allowed |

## LocalOnly Mode

**Default Mode**. Maximum privacy and data sovereignty.

### Description

LocalOnly mode ensures that **all** data stays on your local machine. No network requests are allowed except to localhost services (like your local model endpoint). This mode is ideal for working with sensitive codebases, proprietary algorithms, or when you simply want complete control over data flow.

### When to Use

- Working with confidential or proprietary code
- Compliance requirements (GDPR, HIPAA, SOC2)
- No internet access available
- Maximum privacy preference
- Offline development environments

### Allowed Operations

- ✅ Read and write local files (within safety constraints)
- ✅ Connect to localhost model endpoints (e.g., `http://localhost:11434` for Ollama)
- ✅ Execute local commands (build, test, lint)
- ✅ Read local git repository information
- ✅ Generate code using local models
- ✅ Access local documentation

### Blocked Operations

- ❌ Any network requests to non-localhost addresses
- ❌ External LLM API calls (OpenAI, Anthropic, Cohere, Google, etc.)
- ❌ Package manager downloads (npm, pip, NuGet)
- ❌ Git push/pull/fetch operations
- ❌ Web browsing or HTTP fetches
- ❌ Cloud compute services
- ❌ Telemetry or analytics reporting

### Requirements

- Local model provider (Ollama, vLLM, llama.cpp, or similar)
- Model downloaded and available locally
- All package dependencies pre-installed

### Example Configuration

```yaml
version: "1.0"
mode: "LocalOnly"
model:
  provider: "ollama"
  name: "codellama:13b"
  url: "http://localhost:11434"
```

## Burst Mode

Cloud compute for intensive tasks, but **no external LLMs**.

### Description

Burst mode allows network access for cloud compute resources and package downloads, but **still blocks all external LLM APIs**. This mode is useful when you need to spin up temporary compute for model inference, CI/CD pipelines, or resource-intensive tasks, while maintaining control over your AI model.

Use cases: Running large models on cloud GPUs, downloading dependencies, accessing documentation online, collaborating via git.

### When to Use

- Need to download packages or dependencies
- Want to use cloud GPU for inference
- Collaborating with a team (git push/pull)
- Need to access online documentation
- Running CI/CD pipelines
- Large models that don't fit on local hardware

### Allowed Operations

- ✅ Everything allowed in LocalOnly mode
- ✅ Network requests to cloud compute providers
- ✅ Package manager operations (npm install, pip install, dotnet restore)
- ✅ Git operations (push, pull, fetch, clone)
- ✅ HTTP requests for documentation or APIs (except LLM APIs)
- ✅ Cloud storage access
- ✅ Temporary cloud compute for model inference

### Blocked Operations

- ❌ External LLM API calls (OpenAI, Anthropic, Cohere, Google, etc.)
- ❌ Sending code to third-party LLM services
- ❌ Any operation that uploads code to external LLM providers

### Requirements

- Network connectivity
- Cloud compute credentials (if using cloud GPUs)
- Local or remote model (but **not** via external LLM API)

### Cloud Compute Providers Allowed

Burst mode permits connecting to:
- Cloud GPU providers (AWS, Azure, GCP) for **your own model inference**
- Cloud storage (S3, Azure Blob, GCS)
- Private cloud infrastructure
- CI/CD services (GitHub Actions, GitLab CI, etc.)

### Blocked Endpoints

Even in Burst mode, these endpoints are **always blocked**:

- `api.openai.com`
- `api.anthropic.com`
- `api.cohere.ai`
- `api.ai21.com`
- `generativelanguage.googleapis.com` (Google AI)
- Any other commercial LLM API

### Example Configuration

```yaml
version: "1.0"
mode: "Burst"
model:
  provider: "vllm"
  name: "deepseek-coder-33b"
  url: "https://my-gpu-instance.cloud.example.com:8000"
commands:
  setup:
    - "npm install"  # Allowed: package download
    - "pip install -r requirements.txt"
```

## Airgapped Mode

Complete network isolation for maximum security.

### Description

Airgapped mode provides the **strictest security posture**. Even localhost network access is restricted. This mode is designed for environments with absolute network isolation requirements, such as secure government facilities, financial institutions with strict controls, or highly sensitive research environments.

### When to Use

- Secure government or military environments
- Financial systems with strict isolation requirements
- Medical records systems (HIPAA)
- Classified or top-secret projects
- Zero-trust network policies
- Maximum paranoia mode

### Allowed Operations

- ✅ Read and write local files (within safety constraints)
- ✅ Execute local commands (build, test, lint)
- ✅ Use local model **embedded in the Acode binary** (future feature)
- ✅ Read local git repository information (no network operations)

### Blocked Operations

- ❌ **All network access** (including localhost)
- ❌ External LLM API calls
- ❌ Cloud compute
- ❌ Package downloads
- ❌ Git network operations
- ❌ Any HTTP/HTTPS requests

### Requirements

- Model embedded in Acode binary OR accessible via file system
- All dependencies pre-installed
- No network connectivity required (or allowed)

### Example Configuration

```yaml
version: "1.0"
mode: "Airgapped"
model:
  provider: "embedded"  # Future: model embedded in binary
  name: "codellama-7b-quantized"
commands:
  build:
    - "dotnet build --no-restore"  # No network restore allowed
```

## Mode Selection

### Setting the Mode

Mode can be set via three methods (in order of precedence):

1. **CLI Flag** (highest precedence)
   ```bash
   acode --mode Burst
   ```

2. **Environment Variable**
   ```bash
   export ACODE_MODE=LocalOnly
   ```

3. **Configuration File** (`.agent/config.yml`)
   ```yaml
   mode: "LocalOnly"
   ```

4. **Default** (if not specified): `LocalOnly`

### Mode Switching

**Important**: Changing modes requires explicit user confirmation. Acode will display the security implications and ask for confirmation before switching.

```bash
$ acode --mode Burst
⚠️  Mode Change Requested: LocalOnly → Burst

This will enable:
  ✅ Network access for package downloads
  ✅ Git push/pull operations
  ✅ Cloud compute connections

This will still block:
  ❌ External LLM API calls

Continue? [y/N]: _
```

### Mode Decision Flowchart

```
                    ┌─────────────────────┐
                    │ Need cloud compute  │
                    │ or network access?  │
                    └──────────┬──────────┘
                               │
                ┌──────────────┴──────────────┐
                │                             │
               NO                            YES
                │                             │
                ▼                             ▼
     ┌──────────────────────┐    ┌───────────────────────┐
     │ Need ANY network     │    │ Need external         │
     │ (even localhost)?    │    │ LLM APIs?             │
     └──────────┬───────────┘    └───────────┬───────────┘
                │                            │
         ┌──────┴──────┐              ┌─────┴─────┐
        NO            YES             NO         YES
         │              │              │           │
         ▼              ▼              ▼           ▼
    ┌─────────┐   ┌──────────┐  ┌─────────┐  ┌─────────┐
    │Airgapped│   │LocalOnly │  │  Burst  │  │  NOT    │
    │         │   │          │  │         │  │SUPPORTED│
    └─────────┘   └──────────┘  └─────────┘  └─────────┘
```

## Enforcement Mechanisms

### How Modes Are Enforced

1. **Network Layer Interception**
   - All network calls go through Acode's controlled interfaces
   - Attempted violations are caught before execution
   - Violations are logged to audit log

2. **Endpoint Blocklist**
   - External LLM API endpoints are hard-coded and blocked
   - Blocklist cannot be disabled or overridden
   - Applies to all modes equally

3. **Pre-execution Validation**
   - Every operation is validated against current mode before execution
   - Invalid operations raise `ModeViolationAttempted` event
   - Operation is aborted with clear error message

4. **Audit Logging**
   - All operations logged with mode context
   - Attempted violations logged separately
   - Audit log cannot be disabled in production

### Violation Handling

When a mode violation is attempted:

1. **Operation is blocked** immediately
2. **Event is logged** to audit log with full context
3. **Error is returned** to user with explanation
4. **Suggestion is provided** (e.g., "Use --mode Burst to allow network access")

Example:
```bash
$ acode generate --fetch-docs https://docs.example.com
❌ Error: Network access blocked in LocalOnly mode

Attempted operation: HTTP GET https://docs.example.com
Current mode: LocalOnly
Required mode: Burst or higher

To allow this operation:
  acode --mode Burst generate --fetch-docs https://docs.example.com

Or update your .agent/config.yml:
  mode: "Burst"
```

## FAQ

### Q: Can I use ChatGPT/Claude/GPT-4 with Acode?

**A**: No. Acode is designed for local-first AI inference and **never** sends your code to external LLM APIs. This is a core design principle and cannot be overridden.

If you need cloud-based AI, consider:
- Running your own model on cloud infrastructure (allowed in Burst mode)
- Using Acode for local development and a different tool for cloud AI

### Q: What's the difference between LocalOnly and Airgapped?

**A**:
- **LocalOnly**: Allows localhost network access (for local model providers like Ollama at `localhost:11434`)
- **Airgapped**: Blocks **all** network access, even to localhost

### Q: Can I add exceptions to the LLM API blocklist?

**A**: No. The blocklist is hard-coded and cannot be modified. This ensures that your code never accidentally gets sent to external LLM providers, even if a dependency or plugin tries to do so.

### Q: Does Burst mode send my code to the cloud?

**A**: Burst mode allows network access, but it does **not** send your code to external LLM APIs. You can use Burst mode for:
- Downloading packages
- Pushing to git
- Running models on **your own** cloud GPU

Your code is only sent where **you** explicitly direct it.

### Q: How do I switch modes?

**A**: Use the `--mode` flag, environment variable, or config file. See [Mode Selection](#mode-selection).

### Q: Can Acode switch modes automatically?

**A**: No. Mode switching always requires explicit user confirmation to ensure you're aware of the security implications.

### Q: What happens if I'm offline in Burst mode?

**A**: Network operations will fail, but Acode will continue to work for local operations. You may want to switch to LocalOnly mode if you know you'll be offline.

### Q: How are modes validated?

**A**: Before every operation, Acode checks if the operation is permitted in the current mode. Violations are blocked and logged. See [Enforcement Mechanisms](#enforcement-mechanisms).

### Q: Can I create a custom mode?

**A**: Not currently. The three modes cover the security spectrum from least to most restrictive. If you have a use case not covered, please open a GitHub issue to discuss.

---

For configuration details, see [CONFIG.md](CONFIG.md).
For threat model and security architecture, see `docs/tasks/refined-tasks/Epic 00/task-003-threat-model.md` (coming in Task 003).
