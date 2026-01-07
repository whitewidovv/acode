# Acode Constraints Reference

**Version:** 1.0.0
**Last Updated:** 2026-01-06
**Owner:** Acode Security Team
**Status:** Approved

---

## Table of Contents

1. [Quick Reference](#quick-reference)
2. [Severity Levels](#severity-levels)
3. [Hard Constraints](#hard-constraints)
4. [Soft Constraints](#soft-constraints)
5. [Enforcement Mechanisms](#enforcement-mechanisms)
6. [Compliance Mapping](#compliance-mapping)
7. [FAQ](#faq)
8. [Change History](#change-history)

---

## Quick Reference

| ID | Constraint | Severity | Modes Apply |
|----|------------|----------|-------------|
| HC-01 | No external LLM API in LocalOnly | Critical | LocalOnly, Airgapped |
| HC-02 | No network access in Airgapped | Critical | Airgapped |
| HC-03 | Consent required for Burst mode | Critical | Burst |
| HC-04 | Secrets redacted before transmission | Critical | Burst |
| HC-05 | All mode changes logged | High | All |
| HC-06 | Violations logged and aborted | High | All |
| HC-07 | Fail-safe to LocalOnly on error | High | All |

---

## Severity Levels

| Level | Description | Response |
|-------|-------------|----------|
| **Critical** | Core privacy/security guarantee | Immediate abort, logged as error |
| **High** | Important operational requirement | Operation blocked, logged as warning |
| **Medium** | Best practice enforcement | Warning issued, operation continues |
| **Low** | Guidance/recommendation | Logged for awareness |

---

## Hard Constraints

### HC-01: No External LLM API in LocalOnly Mode

**ID:** HC-01
**Severity:** Critical
**Applies To:** LocalOnly mode, Airgapped mode

**Description:**
When operating in LocalOnly or Airgapped mode, the system MUST NOT make any API calls to external LLM services including but not limited to OpenAI, Anthropic, Azure OpenAI, Google AI, AWS Bedrock, Cohere, Hugging Face Inference API, Together.ai, Replicate, and AI21 Labs.

**Rationale:**
This constraint is the foundation of Acode's privacy guarantee. Users choose LocalOnly mode specifically to ensure their source code, prompts, and development context never leave their local machine. Violating this constraint would fundamentally breach user trust and potentially violate enterprise security policies.

**Enforcement Mechanisms:**
1. **Denylist** - Comprehensive list of known LLM API endpoints (hostname patterns)
   - Implemented in `LlmApiDenylist.cs`
   - Covers all major providers (OpenAI, Anthropic, Google, Cohere, AI21, etc.)
   - Immutable - cannot be removed or bypassed
2. **Mode Matrix** - Permission level check before any network request
   - Implemented in `ModeMatrix.cs`
   - LocalOnly/Airgapped modes deny all external LLM capabilities
3. **Defense-in-Depth** - Multiple validation checkpoints (planned for Epic 2)

**Test Requirements:**
- Unit test for each denylist pattern ✅
- Integration test for HTTP client blocking (Future: Epic 2)
- E2E test verifying user workflow in LocalOnly mode (Future: Epic 3)
- Negative tests attempting bypass (Future: Epic 9)

**Violation Response:**
- Request immediately blocked (no data sent)
- Error logged with constraint ID (HC-01)
- User-facing error with remediation guidance
- Operation aborted

**Related:** Task 001.b, ADR-001 (Future)

---

### HC-02: No Network Access in Airgapped Mode

**ID:** HC-02
**Severity:** Critical
**Applies To:** Airgapped mode

**Description:**
When operating in Airgapped mode, the system MUST NOT make ANY network connections, including localhost connections. This includes connections to local Ollama instances, local databases, or any network-accessible service.

**Rationale:**
Airgapped mode is designed for environments with complete network isolation (e.g., classified government systems, air-gapped corporate networks). ANY network access violates the security model of such environments.

**Enforcement Mechanisms:**
1. **Mode Matrix** - All network capabilities denied in Airgapped mode
   - Implemented in `ModeMatrix.cs`
   - Denies LocalhostNetwork, LAN, External, DNS
2. **Network Stack Disable** (Future: Epic 4)
3. **Runtime Validation** (Future: Epic 9)

**Test Requirements:**
- Mode matrix tests verify all network denied ✅
- Integration test: Airgapped mode blocks all sockets (Future)
- E2E test: Full workflow without any network (Future)

**Violation Response:**
- Request immediately blocked
- Critical error logged with HC-02
- Application may terminate depending on severity

**Related:** Task 001.a

---

### HC-03: Consent Required for Burst Mode

**ID:** HC-03
**Severity:** Critical
**Applies To:** Burst mode

**Description:**
When operating in Burst mode, ANY external LLM API call or data transmission MUST require explicit user consent. Consent must be session-scoped and cannot be persisted across sessions.

**Rationale:**
Even when the user has enabled Burst mode (allowing external compute), they must still have granular control over what data leaves their machine. This prevents accidental data exfiltration and ensures compliance with data handling policies.

**Enforcement Mechanisms:**
1. **Mode Matrix** - External APIs marked as ConditionalOnConsent
   - Implemented in `ModeMatrix.cs`
2. **Consent Dialog** (Future: Epic 2 CLI)
3. **Session Tracking** (Future: Epic 2)

**Test Requirements:**
- Mode matrix tests verify ConditionalOnConsent ✅
- Integration test: Consent required before API call (Future)
- E2E test: User consent flow (Future)

**Violation Response:**
- Operation blocked until consent obtained
- Denial logged
- User prompted for consent

**Related:** Task 001.a

---

### HC-04: Secrets Redacted Before Transmission

**ID:** HC-04
**Severity:** Critical
**Applies To:** Burst mode

**Description:**
Before ANY data is transmitted externally in Burst mode, secrets (API keys, passwords, tokens, private keys, environment variables matching common patterns) MUST be redacted.

**Enforcement Mechanisms:**
1. **Secret Scanner** (Future: Epic 9)
2. **Redaction Engine** (Future: Epic 9)
3. **Audit Logging** (Future: Epic 9)

**Test Requirements:**
- Unit tests for secret patterns (Future)
- Integration test: Secrets redacted in prompts (Future)
- E2E test: No secrets in audit logs (Future)

**Violation Response:**
- Transmission blocked
- Critical error logged with HC-04
- User warned of detected secret

**Related:** Task 003 (Threat Model), Epic 9 (Safety)

---

### HC-05: All Mode Changes Logged

**ID:** HC-05
**Severity:** High
**Applies To:** All modes

**Description:**
Every operating mode change (LocalOnly → Burst, etc.) MUST be logged with timestamp, previous mode, new mode, and initiating user/process.

**Enforcement Mechanisms:**
1. **Audit Logger** (Future: Epic 9)
2. **Mode Change Event** (Future: Epic 2)

**Test Requirements:**
- Integration test: Mode change creates audit log (Future)

**Violation Response:**
- Warning logged if audit fails
- Mode change may proceed

**Related:** Epic 9 (Safety)

---

### HC-06: Violations Logged and Aborted

**ID:** HC-06
**Severity:** High
**Applies To:** All modes

**Description:**
Any constraint violation attempt MUST be logged with full context (constraint ID, mode, operation, timestamp) and the operation MUST be aborted.

**Enforcement Mechanisms:**
1. **Centralized Validation** (Future: Epic 2)
2. **Audit Logging** (Future: Epic 9)

**Test Requirements:**
- Integration test: Violation logged correctly (Future)

**Violation Response:**
- Operation aborted
- Error logged with constraint ID
- User notified

**Related:** Epic 9

---

### HC-07: Fail-Safe to LocalOnly on Error

**ID:** HC-07
**Severity:** High
**Applies To:** All modes

**Description:**
If mode detection or validation fails for any reason, the system MUST default to LocalOnly mode (most restrictive).

**Enforcement Mechanisms:**
1. **Mode Provider** (Future: Epic 2)
2. **Default Mode Enum Value** ✅ (LocalOnly = 0)

**Test Requirements:**
- Unit test: Default mode is LocalOnly ✅
- Integration test: Config error defaults to LocalOnly (Future)

**Violation Response:**
- LocalOnly mode enforced
- Warning logged

**Related:** Task 001.a

---

## Soft Constraints

**Status**: No soft constraints currently defined.

Soft constraints represent best practices and recommendations that SHOULD be followed but MAY be overridden with explicit justification. Unlike hard constraints (which are absolute requirements), soft constraints allow flexibility for specific use cases while maintaining general guidance.

**Future Soft Constraints** may include:
- Performance recommendations (e.g., "API calls SHOULD complete within 5 seconds")
- Resource limits (e.g., "Context window SHOULD NOT exceed 100K tokens")
- Usability guidelines (e.g., "Error messages SHOULD include remediation steps")
- Code quality standards (e.g., "Complexity SHOULD remain below threshold")

**Process for Adding Soft Constraints**:
1. Propose soft constraint with rationale
2. Discuss trade-offs with team
3. Document in this section with SC-XX identifier
4. Update PR checklist to include verification
5. Track violations as warnings (not errors)

---

## Enforcement Mechanisms

### Code-Level Enforcement
- **ModeMatrix** (`src/Acode.Domain/Modes/ModeMatrix.cs`) - 81 mode-capability entries ✅
- **LlmApiDenylist** (`src/Acode.Domain/Validation/LlmApiDenylist.cs`) - Immutable denylist ✅
- **Default Mode** - LocalOnly is enum value 0 ✅

### Runtime Enforcement (Future)
- HTTP client interception (Epic 2)
- Network socket blocking (Epic 4)
- Process sandboxing (Epic 4)

### Review-Time Enforcement
- PR checklist (See PULL_REQUEST_TEMPLATE.md) (Future: This file)
- Security audit checklist (See docs/security-audit-checklist.md) (Future)

---

## Compliance Mapping

| Constraint | GDPR | SOC2 | ISO 27001 | NIST |
|------------|------|------|-----------|------|
| HC-01 | Art. 32 | CC6.1 | A.13.1.1 | SC-7 |
| HC-02 | Art. 32 | CC6.1 | A.13.1.1 | SC-7 |
| HC-03 | Art. 7 | CC6.2 | A.18.1.4 | AC-3 |
| HC-04 | Art. 32 | CC6.1 | A.10.1.1 | IA-5 |
| HC-05 | Art. 30 | CC7.2 | A.12.4.1 | AU-2 |
| HC-06 | Art. 32 | CC7.2 | A.16.1.4 | IR-4 |
| HC-07 | Art. 32 | CC9.1 | A.17.2.1 | CP-2 |

---

## FAQ

### Q: Can I remove entries from the LLM API denylist?
**A:** No. The denylist is immutable by design to enforce HC-01. If you need to use a custom API, enable Burst mode (which requires consent per HC-03).

### Q: What if I need to access GitHub API in LocalOnly mode?
**A:** GitHub is not an LLM provider and is not on the denylist. However, external network access in LocalOnly is still restricted. Consider Burst mode for external API access.

### Q: Can Airgapped mode access local Ollama?
**A:** No. HC-02 prohibits ALL network access in Airgapped mode, including localhost. Airgapped mode is for environments with complete network isolation.

### Q: How do I switch from LocalOnly to Burst?
**A:** Use `acode --mode=burst` when starting the CLI (Future). Mode changes require consent and are logged per HC-05.

### Q: What happens if a constraint is violated?
**A:** The operation is immediately aborted (HC-06), logged with the constraint ID, and the user is notified. No data transmission occurs.

---

## Change History

| Version | Date | Changes |
|---------|------|---------|
| 1.0.0 | 2026-01-03 | Initial version: HC-01 through HC-07 defined |

---

**END OF CONSTRAINTS REFERENCE**

For implementation details, see:
- Task 001.a: Mode Matrix
- Task 001.b: Validation Rules
- Task 003: Threat Model
