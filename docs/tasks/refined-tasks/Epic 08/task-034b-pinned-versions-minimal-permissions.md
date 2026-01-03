# Task 034.b: Pinned Versions + Minimal Permissions

**Priority:** P1 – High  
**Tier:** L – Feature Layer  
**Complexity:** 3 (Fibonacci points)  
**Phase:** Phase 8 – CI/CD Integration  
**Dependencies:** Task 034 (CI Template Generator)  

---

## Description

Task 034.b implements security best practices for generated workflows. Actions MUST use pinned versions. Permissions MUST be minimal. Security vulnerabilities MUST be prevented.

Pinned versions prevent supply chain attacks. Minimal permissions limit blast radius. These are non-negotiable security requirements.

This task covers security configuration. Templates are in 034.a. Caching is in 034.c.

### Business Value

Security hardening provides:
- Supply chain protection
- Reduced attack surface
- Compliance adherence
- Trust by default

### Scope Boundaries

This task covers workflow security. Template content is in 034.a. Caching is in 034.c.

### Integration Points

- Task 034.a: Applied to templates
- Epic 11: Part of security policy
- Task 035: Maintenance checks versions

### Failure Modes

- Version lookup fails → Use known good
- Unknown action → Warn user
- Permission too broad → Error

---

## Glossary / Terms

| Term | Definition |
|------|------------|
| Pinned Version | Specific commit SHA |
| Tag Version | Semantic version tag |
| Permissions | GitHub token scopes |
| GITHUB_TOKEN | Auto-generated token |
| Least Privilege | Minimum required access |
| Supply Chain | Dependency trust chain |

---

## Out of Scope

- Dependency scanning
- SAST integration
- Container scanning
- License compliance
- Vulnerability remediation

---

## Functional Requirements

### FR-001 to FR-020: Pinned Versions

- FR-001: Actions MUST use pinned versions
- FR-002: Pinning MUST use commit SHA
- FR-003: Format: `uses: owner/repo@sha`
- FR-004: SHA MUST be full 40 characters
- FR-005: Comment MUST show version tag
- FR-006: Format: `# v4.0.0`
- FR-007: Version lookup MUST work
- FR-008: GitHub API for SHA lookup
- FR-009: Cache MUST store lookups
- FR-010: Cache TTL: 24 hours
- FR-011: Fallback to tag MUST work
- FR-012: Tag format: `@v4`
- FR-013: Major version only
- FR-014: First-party actions list
- FR-015: actions/checkout, setup-*
- FR-016: Third-party actions MUST pin
- FR-017: Unknown action MUST warn
- FR-018: Custom action MUST allow
- FR-019: Local action MUST work
- FR-020: Version update check MUST exist

### FR-021 to FR-040: Minimal Permissions

- FR-021: `permissions` block MUST exist
- FR-022: Top-level permissions MUST be set
- FR-023: Default: `contents: read`
- FR-024: Write permissions MUST be explicit
- FR-025: Job-level override MUST work
- FR-026: Permission per job MUST work
- FR-027: Common permission sets:
- FR-028: Build: contents read
- FR-029: Release: contents write
- FR-030: Pages: pages write
- FR-031: PR comment: pull-requests write
- FR-032: Issues: issues write
- FR-033: Packages: packages write
- FR-034: Security: security-events write
- FR-035: Broad permissions MUST warn
- FR-036: `write-all` MUST error
- FR-037: Permission inference MUST work
- FR-038: From step actions
- FR-039: Suggest minimal set
- FR-040: Document why needed

### FR-041 to FR-055: Security Checks

- FR-041: Workflow validation MUST run
- FR-042: Check for dangerous patterns
- FR-043: No `${{ github.event.* }}` in run
- FR-044: Script injection prevention
- FR-045: Environment variable safety
- FR-046: Secret masking verification
- FR-047: No hardcoded secrets
- FR-048: No exposed tokens
- FR-049: OIDC preferred over secrets
- FR-050: Concurrency limits MUST exist
- FR-051: Timeout limits MUST exist
- FR-052: Default timeout: 60 minutes
- FR-053: Fork handling MUST be safe
- FR-054: `pull_request_target` warning
- FR-055: Dependabot-safe patterns

---

## Non-Functional Requirements

- NFR-001: Version lookup <500ms
- NFR-002: All actions pinned
- NFR-003: Minimal permissions always
- NFR-004: Clear permission comments
- NFR-005: Security warnings visible
- NFR-006: No security shortcuts
- NFR-007: Audit-friendly output
- NFR-008: Updateable versions
- NFR-009: Clear documentation
- NFR-010: Fail secure

---

## User Manual Documentation

### Pinned Version Example

```yaml
steps:
  - uses: actions/checkout@b4ffde65f46336ab88eb53be808477a3936bae11 # v4.1.1
  - uses: actions/setup-dotnet@4d6c8fcf3c8f7a60068d26b594648e99df24cee3 # v4.0.0
```

### Minimal Permissions Example

```yaml
# Restrict default permissions
permissions:
  contents: read

jobs:
  build:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@...
      
  release:
    runs-on: ubuntu-latest
    # Only this job needs write
    permissions:
      contents: write
    steps:
      - uses: actions/checkout@...
```

### Configuration

```yaml
ciSecurity:
  pinVersions: true           # Always pin to SHA
  minimalPermissions: true    # Least privilege
  allowTagVersions: false     # Force SHA pinning
  warnOnThirdParty: true      # Warn for non-GitHub actions
  blockDangerousPatterns: true
```

### Security Warnings

| Pattern | Risk | Mitigation |
|---------|------|------------|
| `${{ github.event.issue.title }}` | Script injection | Use environment variable |
| `permissions: write-all` | Over-privileged | Specify exact needs |
| `pull_request_target` | Fork attack | Careful with checkout |

---

## Acceptance Criteria / Definition of Done

- [ ] AC-001: Actions use SHA pins
- [ ] AC-002: Version comments present
- [ ] AC-003: SHA lookup works
- [ ] AC-004: Cache works
- [ ] AC-005: Permissions minimal
- [ ] AC-006: Job permissions work
- [ ] AC-007: Dangerous patterns blocked
- [ ] AC-008: Warnings generated
- [ ] AC-009: Third-party flagged
- [ ] AC-010: Documentation clear

---

## Testing Requirements

### Unit Tests

- [ ] UT-001: SHA format validation
- [ ] UT-002: Permission parsing
- [ ] UT-003: Pattern detection
- [ ] UT-004: Version cache

### Integration Tests

- [ ] IT-001: GitHub API lookup
- [ ] IT-002: Full workflow validation
- [ ] IT-003: Security scan
- [ ] IT-004: Permission inference

---

## Implementation Prompt

### Interface

```csharp
public interface IActionVersionResolver
{
    Task<PinnedAction> ResolveAsync(
        string actionRef,
        CancellationToken ct);
}

public record PinnedAction(
    string Owner,
    string Repo,
    string Sha,
    string Tag,
    string FullRef);  // owner/repo@sha

public interface IPermissionAnalyzer
{
    PermissionSet InferMinimal(CiWorkflow workflow);
    IReadOnlyList<PermissionWarning> Analyze(CiWorkflow workflow);
}

public record PermissionSet(
    IReadOnlyDictionary<string, string> Permissions,
    IReadOnlyList<string> Reasons);

public record PermissionWarning(
    string Permission,
    string Reason,
    WarningSeverity Severity);
```

### Security Scanner

```csharp
public interface IWorkflowSecurityScanner
{
    IReadOnlyList<SecurityIssue> Scan(string workflowContent);
}

public record SecurityIssue(
    string Pattern,
    string Description,
    SecuritySeverity Severity,
    int Line,
    string Suggestion);

public enum SecuritySeverity { Info, Warning, Error, Critical }
```

---

**End of Task 034.b Specification**