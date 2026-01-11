# Acode Security Audit Checklist

**Version**: 1.0.0
**Last Updated**: 2026-01-06
**Audience**: Security auditors, compliance teams, security researchers

---

## Purpose

This checklist provides a systematic approach to auditing Acode's implementation of security constraints defined in [CONSTRAINTS.md](../CONSTRAINTS.md). Use this checklist during:

- Pre-release security reviews
- Penetration testing
- Compliance audits (SOC2, ISO 27001, GDPR)
- Quarterly security assessments
- Incident response investigations

---

## How to Use This Checklist

1. **Read CONSTRAINTS.md first** - Understand the security model
2. **Verify each item** - Check implementation, not just documentation
3. **Record evidence** - Document findings with file paths, line numbers, test results
4. **Mark status**: ✅ Pass, ❌ Fail, ⚠️ Partial, 🔍 Needs Investigation, N/A
5. **Document exceptions** - Note any constraints that don't apply to current build
6. **Report findings** - Create issues for failures, track to resolution

---

## Constraint Verification

### HC-01: No External LLM API in LocalOnly Mode

**Constraint**: When operating in LocalOnly or Airgapped mode, the system MUST NOT make any API calls to external LLM services.

**Audit Steps**:

- [ ] **Code Review**: Verify mode matrix denies external LLM capability in LocalOnly/Airgapped
  - File: `src/Acode.Domain/Modes/ModeMatrix.cs`
  - Check: `GetPermission(OperatingMode.LocalOnly, Capability.ExternalLlmApi)` returns `Permission.Denied`
  - Check: `GetPermission(OperatingMode.Airgapped, Capability.ExternalLlmApi)` returns `Permission.Denied`

- [ ] **Denylist Verification**: Confirm denylist contains all major LLM providers
  - File: `src/Acode.Domain/Validation/LlmApiDenylist.cs` (Task 001b)
  - Check patterns for: OpenAI, Anthropic, Google, Cohere, AI21, Together.ai, Replicate, Hugging Face
  - Verify denylist is immutable (no `Add()` or `Remove()` methods)

- [ ] **Runtime Test**: Start Acode in LocalOnly mode, attempt external LLM call
  - Command: `acode --mode=localonly`
  - Attempt API call to OpenAI/Anthropic
  - Expected: Request blocked, error logged with "HC-01"
  - Verify: No network packet leaves to external IP

- [ ] **Network Monitoring**: Monitor network traffic during LocalOnly operation
  - Tool: Wireshark, tcpdump, or similar
  - Filter: Outbound HTTP/HTTPS to non-localhost/LAN IPs
  - Expected: Zero connections to external LLM domains

- [ ] **Test Coverage**: Verify unit tests exist for HC-01
  - Search test files for "HC-01" references
  - Verify tests for each denylist pattern
  - Check integration test blocks actual API calls

**Evidence Required**:
- Screenshot of mode matrix code
- Denylist patterns documentation
- Test execution results (all passing)
- Network capture showing no external traffic

**Severity if Failed**: **CRITICAL** - Core privacy guarantee violated

---

### HC-02: No Network Access in Airgapped Mode

**Constraint**: When operating in Airgapped mode, the system MUST NOT make ANY network connections, including localhost.

**Audit Steps**:

- [ ] **Code Review**: Verify mode matrix denies ALL network capabilities in Airgapped
  - File: `src/Acode.Domain/Modes/ModeMatrix.cs`
  - Check: All network capabilities (LocalhostNetwork, LAN, External, DNS) denied for Airgapped
  - Verify: No exceptions or conditional logic bypasses

- [ ] **Runtime Test**: Start Acode in Airgapped mode, attempt any network operation
  - Command: `acode --mode=airgapped`
  - Attempt localhost connection (e.g., Ollama on 127.0.0.1)
  - Expected: Request blocked, error logged with "HC-02"

- [ ] **Network Stack Test**: Verify network stack disabled in Airgapped mode
  - Test: Attempt socket creation, DNS lookup, HTTP request
  - Expected: All fail immediately
  - Check: Error messages reference HC-02

- [ ] **Mode Transition Test**: Verify Airgapped mode is permanent
  - Start in Airgapped mode
  - Attempt to switch to LocalOnly or Burst
  - Expected: Transition blocked (see ADR-003)
  - Check: Error logged, audit event recorded

**Evidence Required**:
- Mode matrix code for Airgapped
- Test results showing all network attempts blocked
- Mode transition rejection log

**Severity if Failed**: **CRITICAL** - Air-gap security model violated

---

### HC-03: Consent Required for Burst Mode

**Constraint**: When operating in Burst mode, ANY external LLM API call MUST require explicit user consent.

**Audit Steps**:

- [ ] **Code Review**: Verify consent check before external API calls
  - File: (Epic 2 - not yet implemented)
  - Check: `ConsentManager.HasConsent()` called before each API call
  - Verify: Consent cannot be bypassed programmatically

- [ ] **Session Scope Test**: Verify consent is session-scoped (not persistent)
  - Grant consent in Burst mode
  - Terminate Acode process
  - Restart in Burst mode
  - Expected: Consent prompt shown again (not remembered)

- [ ] **Consent UI Test**: Verify consent prompt is clear and informed
  - Check: Prompt explains what data will be transmitted
  - Check: Prompt lists specific providers (OpenAI, Anthropic, etc.)
  - Check: User must explicitly accept (not default/pre-checked)

- [ ] **Revocation Test**: Verify user can revoke consent mid-session
  - Grant consent
  - Switch from Burst to LocalOnly
  - Expected: Subsequent external API attempts blocked

**Evidence Required**:
- Consent flow screenshots
- Session scope verification (restart test)
- Consent revocation test results

**Severity if Failed**: **CRITICAL** - Legal compliance (GDPR Article 7) violated

**Status**: ⚠️ **NOT YET IMPLEMENTED** (Epic 2)

---

### HC-04: Secrets Redacted Before Transmission

**Constraint**: Before ANY data is transmitted externally in Burst mode, secrets MUST be redacted.

**Audit Steps**:

- [ ] **Code Review**: Verify redaction logic exists and is called
  - File: (Epic 9 - not yet implemented)
  - Check: `SecretScanner.Redact()` called before all transmissions
  - Verify: Transmission blocked if redaction fails

- [ ] **Pattern Coverage Test**: Verify all secret types detected
  - Test patterns: AWS keys, API keys, GitHub tokens, private keys, passwords
  - Feed test secrets to redaction engine
  - Expected: All detected and replaced with `[REDACTED:TYPE]`

- [ ] **False Negative Test**: Attempt to bypass secret detection
  - Obfuscate secrets (Base64, hex encoding)
  - Use novel secret formats
  - Expected: High-entropy detection catches obfuscated secrets

- [ ] **False Positive Test**: Verify non-secrets are not redacted
  - Test with high-entropy code (hashes, UUIDs for non-secret purposes)
  - Expected: Code preserved if not matching secret patterns

- [ ] **Audit Logging Test**: Verify redaction events logged
  - Trigger redaction
  - Check audit log contains redaction event (type, position, NOT value)
  - Verify: Sensitive value itself is not logged

**Evidence Required**:
- Secret pattern test results
- False positive/negative analysis
- Audit log samples (redacted appropriately)

**Severity if Failed**: **CRITICAL** - Credential leaks possible

**Status**: ⚠️ **NOT YET IMPLEMENTED** (Epic 9)

---

### HC-05: All Mode Changes Logged

**Constraint**: Every operating mode change MUST be logged with timestamp, previous mode, new mode, and initiating user/process.

**Audit Steps**:

- [ ] **Code Review**: Verify mode change logging
  - File: (Epic 2 - not yet implemented)
  - Check: Log event generated on every mode transition
  - Verify: Log includes required fields (timestamp, old mode, new mode, user)

- [ ] **Runtime Test**: Trigger mode changes and verify logs
  - Switch: LocalOnly → Burst
  - Switch: Burst → LocalOnly
  - Check: Each transition logged
  - Verify: Timestamps accurate, user identified

- [ ] **Audit Trail Test**: Verify logs are tamper-evident
  - Check: Logs append-only (cannot be modified after write)
  - Check: Log integrity mechanism (checksums, signatures)

**Evidence Required**:
- Log samples for mode transitions
- Tamper-evidence verification

**Severity if Failed**: **HIGH** - Auditability compromised

**Status**: ⚠️ **NOT YET IMPLEMENTED** (Epic 2)

---

### HC-06: Violations Logged and Aborted

**Constraint**: Any constraint violation attempt MUST be logged with full context and the operation MUST be aborted.

**Audit Steps**:

- [ ] **Code Review**: Verify violation handling logic
  - Check: All constraint checks throw/return errors on violation
  - Verify: Violations logged before aborting
  - Verify: No fallback logic allows continuation after violation

- [ ] **Violation Test**: Intentionally violate each constraint
  - Violate HC-01 (attempt external API in LocalOnly)
  - Violate HC-02 (attempt network in Airgapped)
  - For each: Verify operation aborted, logged correctly

- [ ] **Log Completeness Test**: Verify violation logs contain required context
  - Check fields: Constraint ID (HC-XX), mode, operation, timestamp, stack trace
  - Verify: Sufficient detail for debugging/forensics

**Evidence Required**:
- Violation test results (operation aborted)
- Log samples with full context

**Severity if Failed**: **HIGH** - Security incidents not properly handled

**Status**: ⚠️ **PARTIAL** - Violations abort, logging not fully implemented

---

### HC-07: Fail-Safe to LocalOnly on Error

**Constraint**: If mode detection or validation fails, the system MUST default to LocalOnly mode.

**Audit Steps**:

- [ ] **Code Review**: Verify enum default value
  - File: `src/Acode.Domain/Modes/OperatingMode.cs`
  - Check: `LocalOnly = 0` (enum default)
  - Verify: No logic overrides this default

- [ ] **Fault Injection Test**: Simulate mode detection failure
  - Corrupt mode configuration file
  - Expected: Acode defaults to LocalOnly
  - Verify: Warning logged about configuration error

- [ ] **Uninitialized Mode Test**: Check behavior with uninitialized mode variable
  - Create scenario where mode is not explicitly set
  - Expected: Defaults to LocalOnly (enum default value 0)

**Evidence Required**:
- Enum definition screenshot
- Fault injection test results
- Default mode verification

**Severity if Failed**: **HIGH** - Unsafe fallback could allow violations

**Status**: ✅ **IMPLEMENTED** (OperatingMode enum default = LocalOnly)

---

## Integration Testing

### Mode Matrix Integrity

- [ ] **Completeness Test**: Verify all mode-capability combinations defined
  - Expected: 3 modes × 26 capabilities = 78 entries
  - File: `src/Acode.Domain/Modes/ModeMatrix.cs`
  - Check: No missing entries

- [ ] **Consistency Test**: Verify permissions align with constraints
  - LocalOnly: External LLM denied (HC-01)
  - Airgapped: All network denied (HC-02)
  - Burst: External LLM conditional on consent (HC-03)

- [ ] **Immutability Test**: Verify matrix cannot be modified at runtime
  - Type: `FrozenDictionary` (immutable)
  - Verify: No methods to add/remove entries

### Defense-in-Depth Verification

- [ ] **Multiple Enforcement Layers**: Verify constraints enforced at multiple points
  - Layer 1: Mode matrix permission check
  - Layer 2: Network handler validation
  - Layer 3: Secret redaction (future)
  - Check: Bypassing one layer doesn't bypass all

### Cross-Cutting Concerns

- [ ] **Documentation Accuracy**: Verify CONSTRAINTS.md matches implementation
  - Cross-reference: Each constraint ID in docs exists in code
  - Verify: Code comments reference correct constraint IDs

- [ ] **Test Coverage**: Verify comprehensive test coverage for constraints
  - Minimum: 80% code coverage for constraint enforcement code
  - Check: Tests for positive cases (allowed), negative cases (denied), edge cases

---

## Compliance Mapping Verification

### GDPR Compliance

- [ ] **Article 7 (Consent)**: HC-03 provides explicit, informed consent
- [ ] **Article 13 (Information)**: Users informed before data processing (HC-03)
- [ ] **Article 32 (Security)**: HC-01, HC-02, HC-04 implement security measures

### SOC2 Compliance

- [ ] **CC6.1 (Logical Access)**: HC-01, HC-02, HC-04 control data access
- [ ] **CC6.2 (Access Authorization)**: HC-03 implements authorization via consent
- [ ] **CC7.2 (Monitoring)**: HC-05, HC-06 provide audit logging

### ISO 27001 Compliance

- [ ] **A.13.1.1 (Network Controls)**: HC-01, HC-02 implement network segmentation
- [ ] **A.10.1.1 (Cryptographic Controls)**: Future - encryption at rest
- [ ] **A.12.4.1 (Event Logging)**: HC-05, HC-06 implement comprehensive logging

---

## Penetration Testing Scenarios

### Scenario 1: Bypass External LLM Block

**Objective**: Attempt to bypass HC-01 and make external API call in LocalOnly mode

**Steps**:
1. Start Acode in LocalOnly mode
2. Attempt direct API call to OpenAI via HTTP
3. Attempt DNS rebinding attack (resolve LLM domain to localhost, then switch)
4. Attempt environment variable manipulation to override mode
5. Attempt reflection/metaprogramming to modify mode at runtime

**Expected**: All attempts blocked, each logged as HC-01 violation

---

### Scenario 2: Exfiltrate Data from Airgapped Mode

**Objective**: Attempt to establish network connection from Airgapped mode

**Steps**:
1. Start Acode in Airgapped mode
2. Attempt HTTP request to localhost Ollama
3. Attempt DNS lookup
4. Attempt raw socket creation
5. Attempt process spawning with network access

**Expected**: All attempts blocked, each logged as HC-02 violation

---

### Scenario 3: Secret Leakage in Burst Mode

**Objective**: Attempt to transmit secrets without redaction

**Steps**:
1. Start Acode in Burst mode, grant consent
2. Include secrets in various formats (Base64, hex, obfuscated)
3. Attempt to send via API call
4. Inspect transmitted data (intercept network traffic)

**Expected**: All secrets redacted with `[REDACTED:TYPE]` markers, original values never transmitted

---

### Scenario 4: Mode Transition Attack

**Objective**: Switch from Airgapped to Burst to exfiltrate data

**Steps**:
1. Start Acode in Airgapped mode
2. Attempt to switch mode to Burst programmatically
3. Attempt to modify configuration to override mode
4. Attempt to send SIGHUP or other signal to reload config

**Expected**: All attempts fail, mode remains Airgapped, attempts logged

---

## Reporting Template

### Security Audit Report

**Audit Date**: [Date]
**Auditor**: [Name]
**Acode Version**: [Version]
**Build**: [Commit Hash]

#### Summary

- Total Checks: [N]
- Passed: [N] ✅
- Failed: [N] ❌
- Partial: [N] ⚠️
- Needs Investigation: [N] 🔍
- Not Applicable: [N]

#### Critical Findings

| ID | Constraint | Finding | Severity | Status |
|----|------------|---------|----------|--------|
| F-001 | HC-01 | [Description] | Critical | ❌ |
| ... | ... | ... | ... | ... |

#### Recommendations

1. [Recommendation 1]
2. [Recommendation 2]
3. [Recommendation 3]

#### Evidence

Attach:
- Code review notes
- Test execution logs
- Network capture files
- Screenshots

---

## Maintenance

This checklist should be updated when:
- New constraints added to CONSTRAINTS.md
- Constraint enforcement implementation changes
- New attack vectors discovered
- Compliance requirements change
- Audit findings reveal checklist gaps

**Checklist Version History**:

| Version | Date | Changes |
|---------|------|---------|
| 1.0.0 | 2026-01-06 | Initial version covering HC-01 through HC-07 |

---

## Resources

- [CONSTRAINTS.md](../CONSTRAINTS.md) - Constraint definitions
- [ADR Index](adr/README.md) - Architecture decisions
- [SECURITY.md](../SECURITY.md) - Security policy and vulnerability reporting
- [CONTRIBUTING.md](../CONTRIBUTING.md) - Development guidelines

**Questions?** Contact: security@acode-project.org
