# Mode Capability Matrix

**Last Updated**: 2026-01-06

This document defines the authoritative mode capability matrix for Acode. The matrix specifies what capabilities are allowed, denied, or conditional in each operating mode.

## Table of Contents

- [Overview](#overview)
- [Permission Levels](#permission-levels)
- [Matrix by Mode](#matrix-by-mode)
  - [LocalOnly Mode](#localonly-mode)
  - [Burst Mode](#burst-mode)
  - [Airgapped Mode](#airgapped-mode)
- [Matrix by Capability](#matrix-by-capability)
- [How to Use This Matrix](#how-to-use-this-matrix)
- [FAQ](#faq)

## Overview

The mode capability matrix is the **authoritative source** for determining what operations Acode can perform in each operating mode. It implements the hard constraints (HC-01, HC-02, HC-03) defined in the project specification.

**Key Principles**:
- **Fail-safe defaults**: Unknown combinations default to Denied
- **Immutable at runtime**: Matrix cannot be modified during execution
- **Auditable**: All lookups are logged for security review
- **Performance**: O(1) lookup time using frozen dictionaries

## Permission Levels

| Permission | Meaning | Example |
|------------|---------|---------|
| **Allowed** | Capability is permitted without restrictions | Reading project files |
| **Denied** | Capability is blocked in this mode | External network in LocalOnly |
| **ConditionalOnConsent** | User must explicitly consent each session | External LLM APIs in Burst |
| **ConditionalOnConfig** | Must be enabled in `.agent/config.yml` | Custom tools |
| **LimitedScope** | Allowed with restrictions (sandboxing, path limits) | Shell commands (limited to project directory) |

## Matrix by Mode

### LocalOnly Mode

**Default mode**. Maximum privacy and data sovereignty. No data leaves your machine.

#### Network Capabilities

| Capability | Permission | Rationale |
|------------|------------|-----------|
| LocalhostNetwork | **Allowed** | Required for Ollama communication |
| LocalAreaNetwork | **Denied** | LAN access denied in LocalOnly mode per privacy-first design |
| ExternalNetwork | **Denied** | External network denied in LocalOnly mode per HC-01 |
| DnsLookup | **Allowed** | DNS lookup allowed for localhost resolution |

#### LLM Provider Capabilities

| Capability | Permission | Rationale |
|------------|------------|-----------|
| OllamaLocal | **Allowed** | Local Ollama is the primary inference provider in LocalOnly mode |
| OpenAiApi | **Denied** | HC-01: External LLM APIs denied in LocalOnly mode |
| AnthropicApi | **Denied** | HC-01: External LLM APIs denied in LocalOnly mode |
| AzureOpenAiApi | **Denied** | HC-01: External LLM APIs denied in LocalOnly mode |
| CustomLlmApi | **Denied** | HC-01: External LLM APIs denied in LocalOnly mode |

#### File System Capabilities

| Capability | Permission | Rationale |
|------------|------------|-----------|
| ReadProjectFiles | **Allowed** | Reading project files is core functionality |
| WriteProjectFiles | **Allowed** | Writing project files is core functionality |
| ReadSystemFiles | **LimitedScope** | System file reads allowed only for specific allowlisted paths |
| WriteSystemFiles | **Denied** | System file writes denied for safety |
| ReadHomeDirectory | **LimitedScope** | Home directory reads limited to ~/.acode and config files |
| WriteAcodeDirectory | **Allowed** | Writing to ~/.acode required for configuration and cache |

#### Tool Execution Capabilities

| Capability | Permission | Rationale |
|------------|------------|-----------|
| DotnetCli | **Allowed** | Dotnet CLI execution is core functionality |
| GitOperations | **Allowed** | Git operations are core functionality |
| NpmYarn | **ConditionalOnConfig** | NPM/Yarn may attempt network access, requires explicit consent |
| CustomTools | **ConditionalOnConfig** | Custom tools require repo contract allowlist |
| ShellCommands | **LimitedScope** | Shell commands sandboxed to project directory |

#### Data Transmission Capabilities

| Capability | Permission | Rationale |
|------------|------------|-----------|
| SendPrompts | **Denied** | HC-01: No external data transmission in LocalOnly mode |
| SendCodeSnippets | **Denied** | HC-01: No external data transmission in LocalOnly mode |
| SendFullFiles | **Denied** | HC-01: No external data transmission in LocalOnly mode |
| SendRepositoryData | **Denied** | HC-01: No external data transmission in LocalOnly mode |
| SendTelemetry | **Denied** | Privacy-first: no telemetry in LocalOnly mode |
| SendCrashReports | **Denied** | Privacy-first: no crash reports in LocalOnly mode |

---

### Burst Mode

**Explicit consent required**. Allows cloud compute for heavy workloads, but still blocks external LLM APIs by default.

#### Network Capabilities

| Capability | Permission | Rationale |
|------------|------------|-----------|
| LocalhostNetwork | **Allowed** | Localhost access for local Ollama if available |
| LocalAreaNetwork | **Allowed** | LAN access allowed in Burst mode for local infrastructure |
| ExternalNetwork | **Allowed** | External network required for cloud compute in Burst mode |
| DnsLookup | **Allowed** | DNS required for cloud service discovery |

#### LLM Provider Capabilities

| Capability | Permission | Rationale |
|------------|------------|-----------|
| OllamaLocal | **Allowed** | Local Ollama available as fallback in Burst mode |
| OpenAiApi | **ConditionalOnConsent** | HC-03: External LLM APIs require explicit user consent in Burst |
| AnthropicApi | **ConditionalOnConsent** | HC-03: External LLM APIs require explicit user consent in Burst |
| AzureOpenAiApi | **ConditionalOnConsent** | HC-03: Azure OpenAI requires consent in Burst |
| CustomLlmApi | **ConditionalOnConfig** | Custom APIs require config allowlist and consent |

#### File System Capabilities

*(Same as LocalOnly mode - file system permissions don't change)*

| Capability | Permission | Rationale |
|------------|------------|-----------|
| ReadProjectFiles | **Allowed** | Reading project files is core functionality |
| WriteProjectFiles | **Allowed** | Writing project files is core functionality |
| ReadSystemFiles | **LimitedScope** | System file reads allowed only for specific allowlisted paths |
| WriteSystemFiles | **Denied** | System file writes denied for safety |
| ReadHomeDirectory | **LimitedScope** | Home directory reads limited to ~/.acode and config files |
| WriteAcodeDirectory | **Allowed** | Writing to ~/.acode required for configuration and cache |

#### Tool Execution Capabilities

| Capability | Permission | Rationale |
|------------|------------|-----------|
| DotnetCli | **Allowed** | Dotnet CLI execution is core functionality |
| GitOperations | **Allowed** | Git operations are core functionality |
| NpmYarn | **Allowed** | NPM/Yarn allowed with network access in Burst |
| CustomTools | **ConditionalOnConfig** | Custom tools require repo contract allowlist |
| ShellCommands | **LimitedScope** | Shell commands sandboxed to project directory |

#### Data Transmission Capabilities

| Capability | Permission | Rationale |
|------------|------------|-----------|
| SendPrompts | **ConditionalOnConsent** | HC-03: Sending prompts requires consent even in Burst |
| SendCodeSnippets | **ConditionalOnConsent** | HC-03: Code snippets require consent and redaction |
| SendFullFiles | **Denied** | Privacy: full file transmission denied even in Burst |
| SendRepositoryData | **Denied** | Privacy: bulk repository data transmission always denied |
| SendTelemetry | **ConditionalOnConfig** | Telemetry requires explicit opt-in in config |
| SendCrashReports | **ConditionalOnConfig** | Crash reports require explicit opt-in in config |

---

### Airgapped Mode

**Complete network isolation**. Strictest security mode for environments with no network access.

#### Network Capabilities

| Capability | Permission | Rationale |
|------------|------------|-----------|
| LocalhostNetwork | **Denied** | HC-02: Complete network isolation in Airgapped mode |
| LocalAreaNetwork | **Denied** | HC-02: Complete network isolation in Airgapped mode |
| ExternalNetwork | **Denied** | HC-02: Complete network isolation in Airgapped mode |
| DnsLookup | **Denied** | HC-02: No DNS in Airgapped mode |

#### LLM Provider Capabilities

*(All denied - no network means no LLM access)*

| Capability | Permission | Rationale |
|------------|------------|-----------|
| OllamaLocal | **Denied** | HC-02: No localhost network means no Ollama access |
| OpenAiApi | **Denied** | HC-02: No network means no external APIs |
| AnthropicApi | **Denied** | HC-02: No network means no external APIs |
| AzureOpenAiApi | **Denied** | HC-02: No network means no external APIs |
| CustomLlmApi | **Denied** | HC-02: No network means no external APIs |

**Note**: Airgapped mode requires pre-loaded models stored locally. See [Airgapped Setup Guide](airgapped-setup.md) for details.

#### File System Capabilities

*(Same as LocalOnly mode - file system permissions don't change)*

#### Tool Execution Capabilities

| Capability | Permission | Rationale |
|------------|------------|-----------|
| DotnetCli | **Allowed** | Dotnet CLI execution is core functionality |
| GitOperations | **Allowed** | Git operations are core functionality (local only) |
| NpmYarn | **Denied** | NPM/Yarn denied in Airgapped due to network requirements |
| CustomTools | **ConditionalOnConfig** | Custom tools allowed if they don't require network |
| ShellCommands | **LimitedScope** | Shell commands sandboxed to project directory |

#### Data Transmission Capabilities

*(All denied - no network means no data transmission)*

| Capability | Permission | Rationale |
|------------|------------|-----------|
| SendPrompts | **Denied** | HC-02: No data transmission in Airgapped mode |
| SendCodeSnippets | **Denied** | HC-02: No data transmission in Airgapped mode |
| SendFullFiles | **Denied** | HC-02: No data transmission in Airgapped mode |
| SendRepositoryData | **Denied** | HC-02: No data transmission in Airgapped mode |
| SendTelemetry | **Denied** | HC-02: No data transmission in Airgapped mode |
| SendCrashReports | **Denied** | HC-02: No data transmission in Airgapped mode |

---

## Matrix by Capability

### Network Access

| Capability | LocalOnly | Burst | Airgapped |
|------------|-----------|-------|-----------|
| LocalhostNetwork | ✅ Allowed | ✅ Allowed | ❌ Denied |
| LocalAreaNetwork | ❌ Denied | ✅ Allowed | ❌ Denied |
| ExternalNetwork | ❌ Denied | ✅ Allowed | ❌ Denied |
| DnsLookup | ✅ Allowed | ✅ Allowed | ❌ Denied |

### LLM Providers

| Capability | LocalOnly | Burst | Airgapped |
|------------|-----------|-------|-----------|
| OllamaLocal | ✅ Allowed | ✅ Allowed | ❌ Denied |
| OpenAiApi | ❌ Denied | ⚠️ Conditional | ❌ Denied |
| AnthropicApi | ❌ Denied | ⚠️ Conditional | ❌ Denied |
| AzureOpenAiApi | ❌ Denied | ⚠️ Conditional | ❌ Denied |
| CustomLlmApi | ❌ Denied | ⚠️ Conditional | ❌ Denied |

### Data Transmission

| Capability | LocalOnly | Burst | Airgapped |
|------------|-----------|-------|-----------|
| SendPrompts | ❌ Denied | ⚠️ Conditional | ❌ Denied |
| SendCodeSnippets | ❌ Denied | ⚠️ Conditional | ❌ Denied |
| SendFullFiles | ❌ Denied | ❌ Denied | ❌ Denied |
| SendRepositoryData | ❌ Denied | ❌ Denied | ❌ Denied |
| SendTelemetry | ❌ Denied | ⚠️ Conditional | ❌ Denied |
| SendCrashReports | ❌ Denied | ⚠️ Conditional | ❌ Denied |

**Legend**:
- ✅ **Allowed**: Permitted
- ❌ **Denied**: Blocked
- ⚠️ **Conditional**: Requires consent or config
- 🔒 **LimitedScope**: Allowed with restrictions

---

## How to Use This Matrix

### Via CLI

View the matrix from command line:

```bash
# Display full matrix
acode matrix

# Filter by mode
acode matrix --mode LocalOnly

# Compare capability across modes
acode matrix --capability OpenAiApi

# Export as JSON
acode matrix --format json
```

### Via API

Query the matrix programmatically:

```csharp
using Acode.Domain.Modes;

// Check if a capability is allowed
var permission = ModeMatrix.GetPermission(OperatingMode.LocalOnly, Capability.OpenAiApi);
// Returns: Permission.Denied

// Get full entry with rationale
var entry = ModeMatrix.GetEntry(OperatingMode.Burst, Capability.OpenAiApi);
Console.WriteLine(entry.Rationale);
// Output: "HC-03: External LLM APIs require explicit user consent in Burst"

// Get all capabilities for a mode
var localOnlyCapabilities = ModeMatrix.GetEntriesForMode(OperatingMode.LocalOnly);

// Get how a capability varies across modes
var openAiAcrossModes = ModeMatrix.GetEntriesForCapability(Capability.OpenAiApi);
```

### In Configuration

Enable conditional capabilities in `.agent/config.yml`:

```yaml
mode: "Burst"

# Enable conditional capabilities
capabilities:
  npm_yarn: true
  custom_tools:
    - "dotnet format"
    - "eslint"
```

---

## FAQ

### Q: Why can't I use OpenAI in LocalOnly mode?

**A**: LocalOnly mode enforces **HC-01**: No external LLM API calls. This is a core privacy constraint. Your code never leaves your machine in LocalOnly mode.

To use external APIs, switch to Burst mode and provide explicit consent.

### Q: What's the difference between Denied and ConditionalOnConsent?

**A**:
- **Denied**: Blocked completely in this mode. Cannot be overridden.
- **ConditionalOnConsent**: Allowed IF user explicitly consents each session. Consent prompt shown at runtime.

### Q: Can I customize the matrix for my project?

**A**: No. The matrix is immutable and enforces Acode's hard constraints (HC-01, HC-02, HC-03). However, you can:
- Use ConditionalOnConfig capabilities by enabling them in `.agent/config.yml`
- Switch modes as needed (LocalOnly → Burst with consent)
- Use LimitedScope capabilities which allow sandboxed access

### Q: How do I know which mode I'm in?

**A**: Run `acode config show` to see current operating mode. Mode is also displayed in the CLI status line.

### Q: What happens if I try a denied capability?

**A**: The operation is blocked immediately and logged to the audit log. You'll see an error like:

```
❌ Error: Network access blocked in LocalOnly mode

Attempted operation: HTTP GET https://api.openai.com
Current mode: LocalOnly
Required mode: Burst or higher

To allow this operation:
  acode --mode Burst <your command>
```

### Q: Why is OllamaLocal denied in Airgapped mode?

**A**: Airgapped mode blocks **all** network access, including localhost. To use Ollama in an airgapped environment, you must use pre-loaded models accessible without network. See [Airgapped Setup Guide](airgapped-setup.md).

### Q: Are file system permissions the same in all modes?

**A**: Yes. File system permissions (reading/writing files) are identical across all three modes. Only network and data transmission permissions vary by mode.

---

## Related Documentation

- [OPERATING_MODES.md](OPERATING_MODES.md) - Operating mode definitions and constraints
- [CONFIG.md](CONFIG.md) - Configuration reference
- [SECURITY.md](../SECURITY.md) - Security policy and reporting vulnerabilities

---

**For questions or issues**, see [CONTRIBUTING.md](../CONTRIBUTING.md).
