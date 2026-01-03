# Task 001.b: Define "No External LLM API" Validation Rules

**Priority:** 6 / 49  
**Tier:** Foundation  
**Complexity:** 5 (Fibonacci points)  
**Phase:** Phase 0 — Foundation  
**Dependencies:** Task 001 (parent), Task 001.a (mode matrix)  

---

## Description

### Overview

Task 001.b defines the validation rules that enforce the "no external LLM API" constraint. This is Acode's most critical privacy protection—ensuring that user code and prompts never reach external AI services unless the user explicitly consents by entering Burst mode.

These validation rules are the implementation specification for the constraints defined in Task 001 and the mode matrix defined in Task 001.a. They define exactly what constitutes an "external LLM API," how to detect attempted violations, and how to block them.

### Business Value

The "no external LLM API" constraint is the foundation of Acode's value proposition:

1. **Trust** — Users can confidently use Acode on proprietary codebases
2. **Compliance** — Enterprises can approve Acode for sensitive projects
3. **Differentiation** — Distinguishes Acode from cloud-dependent tools
4. **Auditability** — Violations are detectable and loggable
5. **Enforcement** — Constraints are programmatically enforced, not just policy

### Scope Boundaries

**In Scope:**
- Definition of "external LLM API" (what's prohibited)
- Detection rules for LLM API endpoints
- Validation checkpoints (where checks occur)
- Violation response (what happens on detection)
- Allowlist for legitimate endpoints (Ollama localhost)
- Denylist patterns for known LLM APIs
- Runtime validation implementation requirements
- Compile-time / static analysis rules
- Configuration for custom endpoints

**Out of Scope:**
- Network blocking implementation (Task 007)
- Secrets detection and redaction (separate concern)
- Telemetry blocking (separate from LLM APIs)
- General network security
- Provider implementation (Tasks 004-006)

### Integration Points

| Task | Relationship | Description |
|------|--------------|-------------|
| Task 001 | Parent | Defines constraint architecture |
| Task 001.a | Sibling | Matrix defines when rule applies |
| Task 001.c | Sibling | Documents validation rules |
| Task 002 | Consumer | Config may specify custom endpoints |
| Tasks 004-006 | Consumer | Providers must pass validation |
| Task 007 | Consumer | Network guard implements blocking |

### Failure Modes

| Failure | Impact | Mitigation |
|---------|--------|------------|
| False positive | Blocks legitimate Ollama | Precise localhost detection |
| False negative | Leaks to external API | Defense in depth |
| Bypass via DNS | Circumvents check | Validate resolved IP |
| Bypass via redirect | Follows to external | Block redirects |
| New LLM API unknown | Not in denylist | Pattern-based detection |

### Assumptions

1. External LLM APIs use HTTPS
2. Localhost (127.0.0.1) is reliably local
3. DNS resolution can be observed
4. HTTP client can be intercepted/wrapped
5. New LLM APIs can be added to denylist via updates

---

## Glossary / Terms

| Term | Definition |
|------|------------|
| **External LLM API** | Any AI/LLM service endpoint not running locally |
| **Validation Rule** | Logic that checks if an action is permitted |
| **Checkpoint** | Location in code where validation occurs |
| **Allowlist** | Explicitly permitted endpoints |
| **Denylist** | Explicitly prohibited endpoints |
| **Pattern Match** | Regex or wildcard matching for URLs |
| **DNS Rebinding** | Attack redirecting to different IP |
| **IP Validation** | Checking resolved IP address |
| **Localhost** | 127.0.0.1 or ::1 |
| **Loopback** | Network interface for localhost |
| **HTTP Interception** | Middleware inspecting HTTP requests |
| **Defense in Depth** | Multiple validation layers |
| **Fail-Safe** | Default to blocking on uncertainty |
| **Violation** | Attempted access to prohibited endpoint |
| **Audit Log** | Record of validation checks and results |

---

## Out of Scope

- Implementation of network-level blocking
- Implementation of TLS/SSL interception
- Secrets detection in prompts
- Telemetry and analytics blocking
- General firewall functionality
- VPN or proxy configuration
- Corporate network policies
- Browser-based access control
- Mobile app considerations
- Plugin security sandboxing

---

## Functional Requirements

### External LLM API Definition (FR-001b-01 to FR-001b-20)

| ID | Requirement |
|----|-------------|
| FR-001b-01 | "External LLM API" MUST be precisely defined |
| FR-001b-02 | Definition MUST include OpenAI endpoints |
| FR-001b-03 | Definition MUST include Anthropic endpoints |
| FR-001b-04 | Definition MUST include Azure OpenAI endpoints |
| FR-001b-05 | Definition MUST include Google AI endpoints |
| FR-001b-06 | Definition MUST include Cohere endpoints |
| FR-001b-07 | Definition MUST include Hugging Face Inference API |
| FR-001b-08 | Definition MUST include Together.ai endpoints |
| FR-001b-09 | Definition MUST include Replicate endpoints |
| FR-001b-10 | Definition MUST include AWS Bedrock endpoints |
| FR-001b-11 | Definition MUST include any non-localhost AI endpoint |
| FR-001b-12 | Definition MUST be extensible (new providers) |
| FR-001b-13 | Definition MUST include pattern matching |
| FR-001b-14 | Definition MUST specify port considerations |
| FR-001b-15 | Definition MUST handle subdomains |
| FR-001b-16 | Definition MUST handle path-based endpoints |
| FR-001b-17 | Definition MUST handle custom domains |
| FR-001b-18 | Definition MUST be documented |
| FR-001b-19 | Definition MUST be versioned |
| FR-001b-20 | Definition MUST be updatable without code change |

### Denylist Patterns (FR-001b-21 to FR-001b-40)

| ID | Requirement |
|----|-------------|
| FR-001b-21 | Denylist MUST include api.openai.com |
| FR-001b-22 | Denylist MUST include *.openai.com |
| FR-001b-23 | Denylist MUST include api.anthropic.com |
| FR-001b-24 | Denylist MUST include *.anthropic.com |
| FR-001b-25 | Denylist MUST include *.azure.com/*openai* |
| FR-001b-26 | Denylist MUST include generativelanguage.googleapis.com |
| FR-001b-27 | Denylist MUST include *.googleapis.com/*ai* |
| FR-001b-28 | Denylist MUST include api.cohere.ai |
| FR-001b-29 | Denylist MUST include api-inference.huggingface.co |
| FR-001b-30 | Denylist MUST include api.together.xyz |
| FR-001b-31 | Denylist MUST include api.replicate.com |
| FR-001b-32 | Denylist MUST include bedrock.*.amazonaws.com |
| FR-001b-33 | Denylist MUST include bedrock-runtime.*.amazonaws.com |
| FR-001b-34 | Denylist patterns MUST be regex-capable |
| FR-001b-35 | Denylist MUST support wildcards |
| FR-001b-36 | Denylist MUST be loadable from file |
| FR-001b-37 | Denylist MUST be updatable |
| FR-001b-38 | Denylist MUST log when matched |
| FR-001b-39 | Denylist MUST be testable |
| FR-001b-40 | Denylist MUST include common LLM path patterns |

### Allowlist Rules (FR-001b-41 to FR-001b-55)

| ID | Requirement |
|----|-------------|
| FR-001b-41 | Allowlist MUST include 127.0.0.1 |
| FR-001b-42 | Allowlist MUST include localhost |
| FR-001b-43 | Allowlist MUST include ::1 (IPv6 loopback) |
| FR-001b-44 | Allowlist MUST include Ollama default port 11434 |
| FR-001b-45 | Allowlist MUST be configurable per-repo |
| FR-001b-46 | Allowlist additions MUST require explicit config |
| FR-001b-47 | Allowlist MUST NOT include any external IPs by default |
| FR-001b-48 | Allowlist MUST log when matched |
| FR-001b-49 | Allowlist MUST be checked before denylist |
| FR-001b-50 | Allowlist MUST support port specification |
| FR-001b-51 | Allowlist MUST support path specification |
| FR-001b-52 | Allowlist MUST be validated at startup |
| FR-001b-53 | Invalid allowlist entries MUST fail startup |
| FR-001b-54 | Allowlist MUST NOT allow bypassing denylist patterns |
| FR-001b-55 | Allowlist MUST be auditable |

### Validation Checkpoints (FR-001b-56 to FR-001b-75)

| ID | Requirement |
|----|-------------|
| FR-001b-56 | Validation MUST occur at HTTP client level |
| FR-001b-57 | Validation MUST occur before DNS resolution |
| FR-001b-58 | Validation MUST occur after DNS resolution |
| FR-001b-59 | Validation MUST check final IP (not just hostname) |
| FR-001b-60 | Validation MUST block HTTP redirects to external |
| FR-001b-61 | Validation MUST occur in provider adapter |
| FR-001b-62 | Validation MUST occur in network guard |
| FR-001b-63 | Validation MUST be synchronous |
| FR-001b-64 | Validation MUST NOT be bypassable |
| FR-001b-65 | Validation MUST check on every request |
| FR-001b-66 | Validation MUST handle connection reuse |
| FR-001b-67 | Validation MUST handle HTTP/2 multiplexing |
| FR-001b-68 | Validation MUST check WebSocket connections |
| FR-001b-69 | Validation MUST check streaming responses |
| FR-001b-70 | Validation MUST handle timeout scenarios |
| FR-001b-71 | Validation MUST handle retry scenarios |
| FR-001b-72 | Validation result MUST be cached per request |
| FR-001b-73 | Validation MUST log all checks |
| FR-001b-74 | Validation MUST not impact performance significantly |
| FR-001b-75 | Validation MUST be testable in isolation |

### Violation Response (FR-001b-76 to FR-001b-90)

| ID | Requirement |
|----|-------------|
| FR-001b-76 | Violations MUST block the request |
| FR-001b-77 | Violations MUST NOT send any data |
| FR-001b-78 | Violations MUST log the attempt |
| FR-001b-79 | Violation log MUST include timestamp |
| FR-001b-80 | Violation log MUST include target URL |
| FR-001b-81 | Violation log MUST include resolved IP |
| FR-001b-82 | Violation log MUST include rule matched |
| FR-001b-83 | Violation log MUST include current mode |
| FR-001b-84 | Violations MUST return clear error to caller |
| FR-001b-85 | Violation error MUST include reason |
| FR-001b-86 | Violation error MUST include remediation |
| FR-001b-87 | Violations MUST increment counter |
| FR-001b-88 | Repeated violations SHOULD trigger warning |
| FR-001b-89 | Violations MUST NOT crash the application |
| FR-001b-90 | Violation state MUST be queryable |

---

## Non-Functional Requirements

### Security (NFR-001b-01 to NFR-001b-15)

| ID | Requirement |
|----|-------------|
| NFR-001b-01 | Validation code MUST NOT be modifiable at runtime |
| NFR-001b-02 | Denylist MUST be protected from tampering |
| NFR-001b-03 | Allowlist MUST be protected from tampering |
| NFR-001b-04 | DNS rebinding attacks MUST be mitigated |
| NFR-001b-05 | IP spoofing MUST be considered |
| NFR-001b-06 | Validation MUST NOT trust HTTP headers |
| NFR-001b-07 | Validation MUST check actual connection IP |
| NFR-001b-08 | Defense in depth required |
| NFR-001b-09 | Fail-safe to blocking |
| NFR-001b-10 | No logging of sensitive prompt data |
| NFR-001b-11 | Validation MUST NOT introduce vulnerabilities |
| NFR-001b-12 | Validation MUST handle malformed URLs |
| NFR-001b-13 | Validation MUST handle unicode in URLs |
| NFR-001b-14 | Validation MUST handle URL encoding |
| NFR-001b-15 | Audit logs MUST be append-only |

### Performance (NFR-001b-16 to NFR-001b-25)

| ID | Requirement |
|----|-------------|
| NFR-001b-16 | URL validation MUST complete in under 1ms |
| NFR-001b-17 | Pattern matching MUST be optimized |
| NFR-001b-18 | Allowlist check MUST be O(1) |
| NFR-001b-19 | Denylist check MUST be O(n) worst case |
| NFR-001b-20 | Validation overhead MUST be under 5% |
| NFR-001b-21 | Memory usage MUST be under 1MB |
| NFR-001b-22 | Startup loading MUST be under 100ms |
| NFR-001b-23 | Regex patterns MUST be pre-compiled |
| NFR-001b-24 | No allocation per check preferred |
| NFR-001b-25 | Caching for repeated URLs |

### Reliability (NFR-001b-26 to NFR-001b-35)

| ID | Requirement |
|----|-------------|
| NFR-001b-26 | Validation MUST NOT crash |
| NFR-001b-27 | Invalid input MUST be handled |
| NFR-001b-28 | Null URLs MUST be blocked |
| NFR-001b-29 | Empty URLs MUST be blocked |
| NFR-001b-30 | Malformed URLs MUST be blocked |
| NFR-001b-31 | Validation MUST be deterministic |
| NFR-001b-32 | Validation MUST be thread-safe |
| NFR-001b-33 | Concurrent checks MUST work |
| NFR-001b-34 | Pattern updates MUST be atomic |
| NFR-001b-35 | 100% test coverage for validation |

---

## User Manual Documentation

### Understanding External LLM API Validation

Acode validates every outgoing network request to ensure it doesn't contact an external LLM API when in LocalOnly or Airgapped mode. This is your protection against accidental data exfiltration.

### What Counts as an External LLM API?

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

**Always Allowed:**
- 127.0.0.1 (Ollama)
- localhost
- ::1 (IPv6 loopback)

### The Denylist

Acode maintains a denylist of known LLM API patterns:

```yaml
# Built-in denylist (cannot be disabled)
denylist:
  - pattern: "api.openai.com"
    type: exact
  - pattern: "*.openai.com"
    type: wildcard
  - pattern: "api.anthropic.com"
    type: exact
  - pattern: "*.anthropic.com"
    type: wildcard
  - pattern: "*.openai.azure.com"
    type: wildcard
  - pattern: "bedrock.*\\.amazonaws\\.com"
    type: regex
  # ... many more patterns
```

### The Allowlist

The allowlist explicitly permits certain endpoints:

```yaml
# .agent/config.yml
network:
  allowlist:
    - host: "127.0.0.1"
      ports: [11434]  # Ollama default
      reason: "Local Ollama instance"
    - host: "localhost"
      ports: [11434]
      reason: "Local Ollama instance"
```

### Validation Checkpoints

Requests are validated at multiple points:

1. **Before DNS Resolution** — Hostname checked against denylist
2. **After DNS Resolution** — Resolved IP checked (prevents DNS rebinding)
3. **Before Connection** — Final validation before socket connect
4. **On Redirect** — Any redirect target is re-validated

### What Happens When Blocked

When a request is blocked:

```
ERROR: External LLM API access denied
  URL: https://api.openai.com/v1/chat/completions
  Mode: LocalOnly
  Rule: Denylist match: api.openai.com
  
  Remediation:
    - Use local Ollama instead
    - Or enter Burst mode: acode --mode burst
```

### Viewing Blocked Attempts

```bash
# Show recent blocked attempts
acode audit blocked --recent

# Output:
# 2024-01-15 10:30:00 BLOCKED api.openai.com (LocalOnly mode)
# 2024-01-15 10:30:01 BLOCKED api.anthropic.com (LocalOnly mode)

# Show all blocked attempts in session
acode audit blocked --session
```

### Configuring Custom Allowlist

For self-hosted LLM instances, add to allowlist:

```yaml
# .agent/config.yml
network:
  allowlist:
    - host: "llm.internal.company.com"
      ports: [443]
      reason: "Internal LLM server"
      require_mode: local-only  # Optional: restrict to mode
```

**Important:** Allowlist entries cannot bypass the built-in denylist. If an endpoint matches the denylist (e.g., a proxied OpenAI endpoint), it will still be blocked.

### Troubleshooting

**Q: Why is my local LLM blocked?**
A: Ensure it's running on 127.0.0.1 or localhost. Other local IPs require allowlist configuration.

**Q: Can I disable the denylist?**
A: No. The denylist is a core security feature that cannot be disabled.

**Q: Why is my internal endpoint blocked?**
A: Add it to the allowlist in your config file.

**Q: How do I use OpenAI/Anthropic?**
A: Enter Burst mode with `acode --mode burst` and provide consent.

---

## Acceptance Criteria / Definition of Done

### External LLM API Definition (25 items)

- [ ] Definition document exists
- [ ] OpenAI endpoints included
- [ ] Anthropic endpoints included
- [ ] Azure OpenAI endpoints included
- [ ] Google AI endpoints included
- [ ] AWS Bedrock endpoints included
- [ ] Cohere endpoints included
- [ ] Hugging Face endpoints included
- [ ] Together.ai endpoints included
- [ ] Replicate endpoints included
- [ ] Definition is extensible
- [ ] Pattern matching supported
- [ ] Port considerations documented
- [ ] Subdomain handling documented
- [ ] Path-based endpoints covered
- [ ] Custom domains addressed
- [ ] Definition versioned
- [ ] Definition updatable
- [ ] Definition tested
- [ ] Definition reviewed
- [ ] Definition matches implementation
- [ ] Definition in code
- [ ] Definition in documentation
- [ ] Definition in config schema
- [ ] Definition validated at startup

### Denylist Implementation (25 items)

- [ ] Denylist data structure exists
- [ ] OpenAI patterns included
- [ ] Anthropic patterns included
- [ ] Azure patterns included
- [ ] AWS patterns included
- [ ] Google patterns included
- [ ] All major providers covered
- [ ] Regex patterns work
- [ ] Wildcard patterns work
- [ ] Exact match works
- [ ] Case insensitive matching
- [ ] Subdomain matching works
- [ ] Path matching works
- [ ] Patterns pre-compiled
- [ ] Patterns loadable from file
- [ ] Patterns updatable
- [ ] Patterns tested
- [ ] No false positives
- [ ] No false negatives (known providers)
- [ ] Matching logged
- [ ] Matching performant
- [ ] Pattern update atomic
- [ ] Denylist protected
- [ ] Denylist auditable
- [ ] Denylist documented

### Allowlist Implementation (20 items)

- [ ] Allowlist data structure exists
- [ ] 127.0.0.1 allowed
- [ ] localhost allowed
- [ ] ::1 allowed
- [ ] Ollama port (11434) allowed
- [ ] Per-repo config supported
- [ ] Explicit config required
- [ ] No external IPs by default
- [ ] Matching logged
- [ ] Checked before denylist
- [ ] Port specification works
- [ ] Path specification works
- [ ] Validated at startup
- [ ] Invalid entries fail startup
- [ ] Cannot bypass denylist
- [ ] Allowlist auditable
- [ ] Allowlist protected
- [ ] Allowlist documented
- [ ] Allowlist tested
- [ ] Allowlist performant

### Validation Checkpoints (25 items)

- [ ] HTTP client level validation
- [ ] Pre-DNS validation
- [ ] Post-DNS validation
- [ ] IP validation
- [ ] Redirect blocking
- [ ] Provider adapter validation
- [ ] Network guard validation
- [ ] Synchronous validation
- [ ] Non-bypassable
- [ ] Every request checked
- [ ] Connection reuse handled
- [ ] HTTP/2 handled
- [ ] WebSocket handled
- [ ] Streaming handled
- [ ] Timeout handled
- [ ] Retry handled
- [ ] Result cached per request
- [ ] All checks logged
- [ ] Performance acceptable
- [ ] Testable in isolation
- [ ] Thread-safe
- [ ] No race conditions
- [ ] Defense in depth achieved
- [ ] Multiple checkpoints work
- [ ] Checkpoints documented

### Violation Response (20 items)

- [ ] Requests blocked
- [ ] No data sent
- [ ] Attempts logged
- [ ] Timestamp in log
- [ ] URL in log (redacted if needed)
- [ ] IP in log
- [ ] Rule in log
- [ ] Mode in log
- [ ] Clear error returned
- [ ] Reason in error
- [ ] Remediation in error
- [ ] Counter incremented
- [ ] Warning on repeated
- [ ] No crash on violation
- [ ] State queryable
- [ ] Audit trail complete
- [ ] Error messages tested
- [ ] Remediation actionable
- [ ] User experience acceptable
- [ ] Violation tests pass

### Testing (20 items)

- [ ] Unit tests for denylist
- [ ] Unit tests for allowlist
- [ ] Unit tests for URL parsing
- [ ] Unit tests for pattern matching
- [ ] Unit tests for IP validation
- [ ] Integration tests for HTTP client
- [ ] Integration tests for providers
- [ ] E2E tests for LocalOnly
- [ ] E2E tests for Burst
- [ ] E2E tests for Airgapped
- [ ] Security tests for bypass
- [ ] Performance tests
- [ ] Edge case tests
- [ ] Malformed URL tests
- [ ] Unicode URL tests
- [ ] Redirect tests
- [ ] DNS rebinding tests
- [ ] Concurrent access tests
- [ ] 100% validation coverage
- [ ] All tests pass

---

## Testing Requirements

### Unit Tests

| ID | Test | Expected |
|----|------|----------|
| UT-001b-01 | Denylist contains OpenAI patterns | Found |
| UT-001b-02 | Denylist matches api.openai.com | Match |
| UT-001b-03 | Denylist matches chat.openai.com | Match |
| UT-001b-04 | Denylist matches api.anthropic.com | Match |
| UT-001b-05 | Allowlist contains 127.0.0.1 | Found |
| UT-001b-06 | Allowlist matches localhost:11434 | Match |
| UT-001b-07 | Validate URL: https://api.openai.com/v1/chat | Denied |
| UT-001b-08 | Validate URL: http://127.0.0.1:11434/api | Allowed |
| UT-001b-09 | Validate URL: null | Denied |
| UT-001b-10 | Validate URL: empty | Denied |
| UT-001b-11 | Validate URL: malformed | Denied |
| UT-001b-12 | Validate IP: 127.0.0.1 | Allowed |
| UT-001b-13 | Validate IP: 8.8.8.8 | Denied |
| UT-001b-14 | Validate IP: ::1 | Allowed |
| UT-001b-15 | Pattern: wildcard matching | Works |
| UT-001b-16 | Pattern: regex matching | Works |
| UT-001b-17 | Pattern: case insensitive | Works |
| UT-001b-18 | Pattern: subdomain | Works |
| UT-001b-19 | Pattern: path | Works |
| UT-001b-20 | Violation: error message | Has reason |

### Integration Tests

| ID | Test | Expected |
|----|------|----------|
| IT-001b-01 | HTTP client calls allowed endpoint | Success |
| IT-001b-02 | HTTP client calls denied endpoint | Blocked |
| IT-001b-03 | Provider uses validation | Checked |
| IT-001b-04 | Redirect to denied endpoint | Blocked |
| IT-001b-05 | DNS rebinding attempt | Blocked |
| IT-001b-06 | Multiple checkpoints triggered | All fire |
| IT-001b-07 | Violation logged | In audit |
| IT-001b-08 | Burst mode allows external | Allowed |
| IT-001b-09 | Airgapped blocks localhost | Blocked |
| IT-001b-10 | Config allowlist works | Extended |

### End-to-End Tests

| ID | Test | Expected |
|----|------|----------|
| E2E-001b-01 | LocalOnly: attempt OpenAI | Blocked with message |
| E2E-001b-02 | LocalOnly: use Ollama | Success |
| E2E-001b-03 | Burst: use OpenAI with consent | Success |
| E2E-001b-04 | Airgapped: attempt any network | Blocked |
| E2E-001b-05 | View blocked attempts via CLI | Shows list |
| E2E-001b-06 | Add custom allowlist entry | Works |
| E2E-001b-07 | Invalid allowlist entry | Fails startup |
| E2E-001b-08 | Repeated violations | Warning shown |

### Performance Benchmarks

| ID | Metric | Target |
|----|--------|--------|
| PB-001b-01 | URL validation time | < 1ms |
| PB-001b-02 | Denylist check time | < 500μs |
| PB-001b-03 | Allowlist check time | < 100μs |
| PB-001b-04 | Pattern compile time | < 100ms |
| PB-001b-05 | Memory for patterns | < 1MB |
| PB-001b-06 | Validation overhead | < 5% |

---

## User Verification Steps

### Verification 1: OpenAI Blocked in LocalOnly
1. Start Acode in LocalOnly mode
2. Configure OpenAI as provider (temporarily)
3. Attempt to use
4. **Verify:** Request blocked with clear error

### Verification 2: Ollama Allowed in LocalOnly
1. Start Acode in LocalOnly mode
2. Use Ollama provider
3. **Verify:** Request succeeds

### Verification 3: Denylist Matches Patterns
1. Run `acode config denylist`
2. **Verify:** All major LLM providers listed

### Verification 4: Allowlist Shows Localhost
1. Run `acode config allowlist`
2. **Verify:** 127.0.0.1 and localhost listed

### Verification 5: Custom Allowlist Works
1. Add internal endpoint to config allowlist
2. Restart Acode
3. **Verify:** Endpoint accessible

### Verification 6: Invalid Allowlist Fails
1. Add invalid entry to allowlist
2. Attempt to start Acode
3. **Verify:** Startup fails with clear error

### Verification 7: Violations Logged
1. Attempt blocked request
2. Run `acode audit blocked`
3. **Verify:** Attempt appears in log

### Verification 8: Burst Mode Allows External
1. Enter Burst mode with consent
2. Use external API
3. **Verify:** Request succeeds

### Verification 9: Error Message Helpful
1. Trigger a violation
2. Read error message
3. **Verify:** Includes reason and remediation

### Verification 10: Redirect Blocked
1. Set up redirect from allowed to denied
2. Attempt request
3. **Verify:** Redirect blocked

---

## Implementation Prompt for Claude

### Files to Create

```
src/Acode.Domain/
├── Validation/
│   ├── IEndpointValidator.cs       # Validation interface
│   ├── ValidationResult.cs         # Result type
│   └── EndpointPatterns.cs         # Pattern definitions
│
src/Acode.Infrastructure/
├── Network/
│   ├── EndpointValidator.cs        # Implementation
│   ├── DenylistProvider.cs         # Denylist management
│   ├── AllowlistProvider.cs        # Allowlist management
│   └── ValidatingHttpHandler.cs    # HTTP interception
│
data/
├── denylist.json                   # Default denylist patterns
│
docs/
└── endpoint-validation.md          # User documentation
```

### Core Types

```csharp
namespace Acode.Domain.Validation;

/// <summary>
/// Result of endpoint validation.
/// </summary>
public sealed record EndpointValidationResult
{
    public bool IsAllowed { get; init; }
    public string? DenialReason { get; init; }
    public string? MatchedPattern { get; init; }
    public string? Remediation { get; init; }
    
    public static EndpointValidationResult Allowed() => new() { IsAllowed = true };
    
    public static EndpointValidationResult Denied(
        string reason,
        string pattern,
        string remediation) => new()
    {
        IsAllowed = false,
        DenialReason = reason,
        MatchedPattern = pattern,
        Remediation = remediation
    };
}

/// <summary>
/// Validates endpoints against allowlist/denylist.
/// </summary>
public interface IEndpointValidator
{
    EndpointValidationResult Validate(
        Uri endpoint,
        OperatingMode mode);
    
    EndpointValidationResult ValidateIp(
        IPAddress ip,
        OperatingMode mode);
}

/// <summary>
/// Pattern for denylist/allowlist matching.
/// </summary>
public sealed record EndpointPattern
{
    public required string Pattern { get; init; }
    public required PatternType Type { get; init; }
    public string? Description { get; init; }
    
    private readonly Regex? _compiledRegex;
    
    public bool Matches(Uri uri)
    {
        return Type switch
        {
            PatternType.Exact => MatchExact(uri),
            PatternType.Wildcard => MatchWildcard(uri),
            PatternType.Regex => MatchRegex(uri),
            _ => false
        };
    }
}

public enum PatternType
{
    Exact,
    Wildcard,
    Regex
}
```

### Default Denylist (data/denylist.json)

```json
{
  "version": "1.0.0",
  "updated": "2024-01-15",
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
    },
    {
      "pattern": "generativelanguage.googleapis.com",
      "type": "exact",
      "description": "Google AI API"
    },
    {
      "pattern": "bedrock.*\\.amazonaws\\.com",
      "type": "regex",
      "description": "AWS Bedrock"
    },
    {
      "pattern": "api.cohere.ai",
      "type": "exact",
      "description": "Cohere API"
    },
    {
      "pattern": "api-inference.huggingface.co",
      "type": "exact",
      "description": "Hugging Face Inference"
    },
    {
      "pattern": "api.together.xyz",
      "type": "exact",
      "description": "Together.ai"
    },
    {
      "pattern": "api.replicate.com",
      "type": "exact",
      "description": "Replicate"
    }
  ]
}
```

### Validation Checklist Before Merge

- [ ] All major LLM APIs in denylist
- [ ] Localhost in allowlist
- [ ] Validation integrated with HTTP client
- [ ] Validation at all checkpoints
- [ ] Violations logged
- [ ] Errors include remediation
- [ ] Performance targets met
- [ ] Security review passed
- [ ] All tests passing
- [ ] Documentation complete

---

**END OF TASK 001.b**
