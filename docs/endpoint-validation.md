# Endpoint Validation

## Understanding External LLM API Validation

Acode validates every outgoing network request to ensure it doesn't contact an external LLM API when in LocalOnly or Airgapped mode. This is your protection against accidental data exfiltration.

## What Counts as an External LLM API?

**Blocked by Default:**
- OpenAI API (api.openai.com)
- Anthropic API (api.anthropic.com)
- Azure OpenAI (*.openai.azure.com)
- Google AI (generativelanguage.googleapis.com)
- AWS Bedrock (bedrock*.amazonaws.com)
- Cohere (api.cohere.ai)
- Hugging Face Inference (api-inference.huggingface.co)
- Together.ai (api.together.xyz)
- Replicate (api.replicate.com)
- Any other non-localhost LLM endpoint

**Always Allowed (in LocalOnly mode):**
- 127.0.0.1 on port 11434 (Ollama)
- localhost on port 11434
- ::1 (IPv6 loopback) on port 11434

## The Denylist

Acode maintains a denylist of known LLM API patterns in `data/denylist.json`:

```json
{
  "version": "1.0.0",
  "updated": "2026-01-11",
  "patterns": [
    {
      "pattern": "api.openai.com",
      "type": "exact",
      "description": "OpenAI API"
    },
    {
      "pattern": "*.openai.com",
      "type": "wildcard",
      "description": "OpenAI subdomains"
    },
    {
      "pattern": "api.anthropic.com",
      "type": "exact",
      "description": "Anthropic API"
    },
    {
      "pattern": "*.anthropic.com",
      "type": "wildcard",
      "description": "Anthropic subdomains"
    },
    {
      "pattern": ".*\\.openai\\.azure\\.com",
      "type": "regex",
      "description": "Azure OpenAI endpoints"
    }
  ]
}
```

The denylist supports three pattern types:
- **exact**: Matches hostname exactly (e.g., "api.openai.com")
- **wildcard**: Matches subdomains (e.g., "*.openai.com" matches "chat.openai.com" but NOT "openai.com")
- **regex**: Full regex matching (e.g., "bedrock.*\\.amazonaws\\.com")

## The Allowlist

The allowlist explicitly permits certain endpoints. The default allowlist includes localhost on port 11434 for Ollama:

```csharp
// Default allowlist (built-in)
{
    Host = "localhost",
    Ports = new[] { 11434 },
    Reason = "Ollama local inference server"
}
```

**Localhost Equivalence:** The following are treated as equivalent:
- `localhost`
- `127.0.0.1`
- `::1`
- `[::1]`

## Validation Checkpoints

Requests are validated at multiple points based on the current operating mode:

### Operating Modes

1. **LocalOnly Mode** (default)
   - ✅ Allowlist endpoints (localhost:11434 by default)
   - ❌ All external LLM APIs (denylist)
   - ✅ Other local/internal endpoints not on denylist

2. **Burst Mode**
   - ✅ ALL endpoints allowed
   - External LLM APIs explicitly permitted

3. **Airgapped Mode**
   - ❌ ALL network access blocked
   - Even localhost is denied

### Validation Order

1. **Check Airgapped Mode** — If Airgapped, deny immediately (HC-02)
2. **Check Allowlist** — If on allowlist and not Airgapped, allow immediately
3. **Check Burst Mode** — If Burst mode, allow all
4. **Check Denylist** — If LocalOnly mode, check if endpoint matches denylist (HC-01)
5. **Default Allow** — If LocalOnly and not on denylist, allow

**Key Rule:** Allowlist is checked BEFORE denylist (except in Airgapped mode where everything is denied).

## What Happens When Blocked

When a request is blocked, you'll receive a detailed error message:

```
ERROR: External LLM API access denied
  URL: https://api.openai.com/v1/chat/completions
  Mode: LocalOnly
  Constraint: HC-01

  External LLM API 'api.openai.com' is denied in LocalOnly mode.
  Switch to Burst mode to use external APIs, or use local inference (localhost:11434).
```

## Viewing Blocked Attempts

> **Note:** Audit logging for blocked attempts is planned for a future task. This section describes the intended functionality.

```bash
# Show recent blocked attempts (Future feature)
acode audit blocked --recent

# Output:
# 2026-01-15 10:30:00 BLOCKED api.openai.com (LocalOnly mode)
# 2026-01-15 10:30:01 BLOCKED api.anthropic.com (LocalOnly mode)

# Show all blocked attempts in session (Future feature)
acode audit blocked --session
```

## Configuring Custom Allowlist

> **Note:** Configuration file support is planned for a future task. Currently, custom allowlists can be provided programmatically via the `IAllowlistProvider` interface.

For self-hosted LLM instances, you'll be able to add to allowlist in your config file:

```yaml
# .agent/config.yml (Future feature)
network:
  allowlist:
    - host: "llm.internal.company.com"
      ports: [443]
      reason: "Internal LLM server"
```

**Current Workaround:** Implement a custom `IAllowlistProvider` and pass it to the `EndpointValidator` constructor:

```csharp
public class CustomAllowlistProvider : IAllowlistProvider
{
    public IReadOnlyList<AllowlistEntry> GetDefaultAllowlist()
    {
        return new[]
        {
            new AllowlistEntry
            {
                Host = "llm.internal.company.com",
                Ports = new[] { 443 },
                Reason = "Internal LLM server"
            }
        }.ToList().AsReadOnly();
    }

    public bool IsAllowed(Uri uri)
    {
        return GetDefaultAllowlist().Any(entry => entry.Matches(uri));
    }
}

// Usage
var customAllowlist = new CustomAllowlistProvider();
var validator = new EndpointValidator(customAllowlist);
```

## Troubleshooting

**Q: Why is my local LLM blocked?**

A: Ensure it's running on 127.0.0.1 or localhost on port 11434. Other ports or IPs require custom allowlist configuration.

**Q: Can I disable the denylist?**

A: No. The denylist is a core security feature that cannot be disabled. Use Burst mode if you need to access external LLM APIs.

**Q: Why is my internal endpoint blocked?**

A: Add it to the allowlist using a custom `IAllowlistProvider` (programmatic) or via config file (future feature).

**Q: How do I use OpenAI/Anthropic?**

A: Switch to Burst mode when starting Acode. Burst mode allows all external endpoints including LLM APIs.

**Q: What's the difference between hard constraints HC-01 and HC-02?**

A:
- **HC-01**: "No external LLM APIs in LocalOnly/Airgapped modes" - Prevents accidental use of paid APIs
- **HC-02**: "No network access in Airgapped mode" - Complete network isolation

**Q: Can wildcards match the root domain?**

A: No. The wildcard pattern `*.openai.com` matches `chat.openai.com` but does NOT match `openai.com` itself. This prevents overly broad blocking.

## Implementation Details

### Architecture

```
┌─────────────────────────────────────┐
│ EndpointValidator                   │
│ (Acode.Infrastructure.Network)      │
├─────────────────────────────────────┤
│ • Validate(Uri, OperatingMode)      │
│ • ValidateIp(IPAddress, OpMode)     │
│ • Uses allowlist and denylist       │
└──────────────┬──────────────────────┘
               │
               ├─────────────────────────────────┐
               │                                 │
      ┌────────▼────────┐              ┌────────▼────────┐
      │ DefaultAllowlist│              │ LlmApiDenylist  │
      │   (Domain)      │              │    (Domain)     │
      ├─────────────────┤              ├─────────────────┤
      │ • Localhost     │              │ • Patterns from │
      │   on 11434      │              │   denylist.json │
      │ • IPv4/IPv6     │              │ • 3 types:      │
      │   loopback      │              │   Exact/Wild/Re │
      └─────────────────┘              └─────────────────┘
```

### Key Classes

- **`EndpointValidator`** (`Acode.Infrastructure.Network`) - Main validation orchestrator
- **`IEndpointValidator`** (`Acode.Domain.Validation`) - Validation contract
- **`EndpointPattern`** (`Acode.Domain.Validation`) - Pattern matching logic
- **`LlmApiDenylist`** (`Acode.Domain.Validation`) - Built-in denylist patterns
- **`AllowlistEntry`** (`Acode.Domain.Validation`) - Allowlist entry with localhost equivalence
- **`DefaultAllowlist`** (`Acode.Domain.Validation`) - Default Ollama allowlist
- **`IAllowlistProvider`** (`Acode.Domain.Validation`) - Custom allowlist interface
- **`DenylistProvider`** (`Acode.Infrastructure.Network`) - Loads denylist from JSON file

### Testing

Comprehensive test coverage includes:
- **Unit tests** for all pattern types, allowlist/denylist logic, validation precedence
- **Integration tests** for end-to-end validation across all operating modes
- **Edge cases** like DNS rebinding, IPv6, port restrictions, localhost equivalence

See `tests/Acode.Domain.Tests/Validation/` and `tests/Acode.Infrastructure.Tests/Network/` for test implementations.

---

**Last Updated:** 2026-01-11
**Task:** 001b - Define No External LLM API Validation Rules
**Status:** Implemented and tested
