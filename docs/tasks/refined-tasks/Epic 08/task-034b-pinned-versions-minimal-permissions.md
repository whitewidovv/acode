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

| Component | Interface | Data Flow | Notes |
|-----------|-----------|-----------|-------|
| Template Generator | `ICiTemplateGenerator` | Requests security config | From 034 |
| Action Resolver | `IActionVersionResolver` | Looks up SHA for actions | GitHub API |
| Permission Analyzer | `IPermissionAnalyzer` | Determines minimal permissions | Per-step |
| Security Scanner | `IWorkflowSecurityScanner` | Checks for vulnerabilities | Patterns |
| Version Cache | `IVersionCache` | Caches SHA lookups | 24h TTL |
| Task 035 | Maintenance | Version update checks | Future |
| Epic 11 | Security Policy | Compliance rules | Shared |

### Failure Modes

| Failure | Detection | Recovery | User Impact |
|---------|-----------|----------|-------------|
| Version lookup fails | GitHub API error | Use known-good cached version | Warning logged |
| Unknown action | Not in registry | Warn user, allow with tag | Security warning |
| Permission too broad | Validation check | Error and block generation | Must fix config |
| Network unavailable | Timeout exception | Use cached versions | May be outdated |
| Rate limit hit | 403 response | Exponential backoff | Delayed generation |
| Invalid SHA format | Regex validation | Reject and error | Clear message |
| Dangerous pattern | Pattern match | Block generation | Security error |
| Fork attack pattern | Pattern match | Warn user | Security warning |

### Mode Compliance

| Operating Mode | Version Lookup | Permission Analysis |
|----------------|----------------|---------------------|
| Local-Only | Cached only | ALLOWED |
| Burst | Network allowed | ALLOWED |
| Air-Gapped | Cached only | ALLOWED |

### Assumptions

1. **GitHub API accessible**: Version lookup requires GitHub API access
2. **Actions are public**: Only public GitHub Actions supported
3. **First-party actions trusted**: actions/* are considered safe
4. **Cache valid for 24 hours**: Version cache TTL is sufficient
5. **Known dangerous patterns**: Injection patterns are documented
6. **Permissions are job-level**: GitHub supports job-level permissions
7. **SHA format is stable**: 40-character hex SHA format
8. **Version tags follow semver**: Actions use semantic versioning tags

### Security Considerations

1. **Supply chain protection**: SHA pinning prevents action tampering
2. **Least privilege enforcement**: Minimal permissions by default
3. **Script injection prevention**: Dangerous patterns detected and blocked
4. **Third-party action warnings**: Non-verified actions flagged
5. **Version tampering detection**: SHA mismatch would be detected
6. **Cache poisoning prevention**: Cache is local, not shared
7. **OIDC preferred**: Token-based auth over long-lived secrets
8. **Audit trail**: All security decisions logged

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

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-034B-01 | All GitHub Actions MUST use pinned versions | P0 |
| FR-034B-02 | Pinning MUST use full commit SHA (40 characters) | P0 |
| FR-034B-03 | Format MUST be `uses: owner/repo@sha` | P0 |
| FR-034B-04 | SHA MUST be validated as 40 hexadecimal characters | P0 |
| FR-034B-05 | Comment MUST show human-readable version tag | P0 |
| FR-034B-06 | Comment format MUST be `# v4.0.0` after SHA | P1 |
| FR-034B-07 | Version lookup MUST resolve tag to SHA | P0 |
| FR-034B-08 | GitHub API MUST be used for SHA resolution | P1 |
| FR-034B-09 | Cache MUST store SHA lookups locally | P1 |
| FR-034B-10 | Cache TTL MUST be 24 hours default | P2 |
| FR-034B-11 | Fallback to tag MUST work when SHA unavailable | P1 |
| FR-034B-12 | Tag fallback format MUST be `@v4` (major only) | P2 |
| FR-034B-13 | Major version only MUST be used for tag fallback | P2 |
| FR-034B-14 | First-party actions list MUST be maintained | P1 |
| FR-034B-15 | First-party includes actions/checkout, setup-* | P1 |
| FR-034B-16 | Third-party actions MUST always pin to SHA | P0 |
| FR-034B-17 | Unknown action MUST trigger warning | P1 |
| FR-034B-18 | Custom action paths MUST be allowed | P2 |
| FR-034B-19 | Local action (`./.github/actions/`) MUST work | P2 |
| FR-034B-20 | Version update check MUST be available | P2 |

### FR-021 to FR-040: Minimal Permissions

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-034B-21 | `permissions` block MUST exist in workflow | P0 |
| FR-034B-22 | Top-level permissions MUST restrict default | P0 |
| FR-034B-23 | Default permission MUST be `contents: read` | P0 |
| FR-034B-24 | Write permissions MUST be explicitly granted | P0 |
| FR-034B-25 | Job-level permission override MUST work | P1 |
| FR-034B-26 | Permissions MUST be specifiable per job | P1 |
| FR-034B-27 | Common permission sets MUST be predefined | P1 |
| FR-034B-28 | Build permission: `contents: read` only | P0 |
| FR-034B-29 | Release permission: `contents: write` | P1 |
| FR-034B-30 | Pages permission: `pages: write, id-token: write` | P2 |
| FR-034B-31 | PR comment permission: `pull-requests: write` | P2 |
| FR-034B-32 | Issues permission: `issues: write` | P2 |
| FR-034B-33 | Packages permission: `packages: write` | P2 |
| FR-034B-34 | Security scanning: `security-events: write` | P2 |
| FR-034B-35 | Broad permissions MUST trigger warning | P0 |
| FR-034B-36 | `write-all` MUST trigger error and block | P0 |
| FR-034B-37 | Permission inference MUST analyze steps | P1 |
| FR-034B-38 | Inference MUST examine action requirements | P1 |
| FR-034B-39 | Minimal set MUST be suggested | P1 |
| FR-034B-40 | Comments MUST document why permissions needed | P2 |

### FR-041 to FR-055: Security Checks

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-034B-41 | Workflow validation MUST run before output | P0 |
| FR-034B-42 | Dangerous patterns MUST be detected | P0 |
| FR-034B-43 | `${{ github.event.* }}` in `run:` MUST be flagged | P0 |
| FR-034B-44 | Script injection prevention MUST be enforced | P0 |
| FR-034B-45 | Environment variable safety MUST be checked | P1 |
| FR-034B-46 | Secret masking MUST be verified | P1 |
| FR-034B-47 | No hardcoded secrets MUST be enforced | P0 |
| FR-034B-48 | No exposed tokens in workflow MUST be enforced | P0 |
| FR-034B-49 | OIDC MUST be preferred over static secrets | P2 |
| FR-034B-50 | Concurrency limits MUST be set | P1 |
| FR-034B-51 | Timeout limits MUST be set | P1 |
| FR-034B-52 | Default timeout MUST be 60 minutes | P2 |
| FR-034B-53 | Fork handling MUST be safe | P1 |
| FR-034B-54 | `pull_request_target` MUST trigger warning | P0 |
| FR-034B-55 | Dependabot-safe patterns MUST be used | P2 |

---

## Non-Functional Requirements

### Performance Requirements

| ID | Requirement | Target | Priority |
|----|-------------|--------|----------|
| NFR-034B-01 | Version lookup latency (cached) | <10ms | P1 |
| NFR-034B-02 | Version lookup latency (network) | <500ms | P1 |
| NFR-034B-03 | Cache hit rate | >95% | P2 |
| NFR-034B-04 | Permission analysis latency | <100ms | P1 |
| NFR-034B-05 | Security scan latency | <200ms | P1 |
| NFR-034B-06 | Pattern matching time | <50ms | P2 |
| NFR-034B-07 | Memory for cache | <10MB | P2 |
| NFR-034B-08 | Parallel lookups | 5 concurrent | P2 |
| NFR-034B-09 | API rate limit buffer | 10% headroom | P2 |
| NFR-034B-10 | Batch lookup support | Up to 10 | P2 |

### Reliability Requirements

| ID | Requirement | Target | Priority |
|----|-------------|--------|----------|
| NFR-034B-11 | All actions pinned in output | 100% | P0 |
| NFR-034B-12 | Minimal permissions applied | 100% | P0 |
| NFR-034B-13 | Clear permission comments | Always | P1 |
| NFR-034B-14 | Security warnings visible | Clear | P0 |
| NFR-034B-15 | No security shortcuts | Never bypass | P0 |
| NFR-034B-16 | Audit-friendly output | Traceable | P1 |
| NFR-034B-17 | Version updateable | Configurable | P2 |
| NFR-034B-18 | Clear documentation | Comments | P1 |
| NFR-034B-19 | Fail secure on error | Block generation | P0 |
| NFR-034B-20 | Graceful API failure | Use cache | P1 |

### Observability Requirements

| ID | Requirement | Target | Priority |
|----|-------------|--------|----------|
| NFR-034B-21 | Structured logging for lookups | JSON format | P1 |
| NFR-034B-22 | Metrics on cache hits/misses | Counter | P2 |
| NFR-034B-23 | Events on security issues | Async publish | P1 |
| NFR-034B-24 | Permission decisions logged | Info level | P1 |
| NFR-034B-25 | Dangerous pattern alerts | Warning level | P0 |
| NFR-034B-26 | Version resolution logged | Debug level | P2 |
| NFR-034B-27 | Third-party warnings logged | Warning level | P1 |
| NFR-034B-28 | API call metrics | Counter | P2 |
| NFR-034B-29 | Security scan results logged | Info level | P1 |
| NFR-034B-30 | Audit trail for decisions | Full history | P1 |

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

### Pinned Versions
- [ ] AC-001: All actions use SHA pinning
- [ ] AC-002: SHA is 40 hexadecimal characters
- [ ] AC-003: Format is `owner/repo@sha`
- [ ] AC-004: Comment shows version tag
- [ ] AC-005: Comment format is `# v4.0.0`
- [ ] AC-006: GitHub API lookup works
- [ ] AC-007: Cache stores lookups
- [ ] AC-008: Cache TTL is 24 hours
- [ ] AC-009: Tag fallback works when API fails
- [ ] AC-010: First-party actions identified
- [ ] AC-011: Third-party always pinned

### Minimal Permissions
- [ ] AC-012: `permissions` block exists
- [ ] AC-013: Top-level is `contents: read`
- [ ] AC-014: Write permissions explicit
- [ ] AC-015: Job-level override works
- [ ] AC-016: Build job read-only
- [ ] AC-017: Release job has write
- [ ] AC-018: Broad permissions warn
- [ ] AC-019: `write-all` errors
- [ ] AC-020: Permission inference works
- [ ] AC-021: Comments document needs

### Security Checks
- [ ] AC-022: Dangerous patterns detected
- [ ] AC-023: Script injection blocked
- [ ] AC-024: `github.event.*` in run flagged
- [ ] AC-025: No hardcoded secrets
- [ ] AC-026: Concurrency limits set
- [ ] AC-027: Timeout limits set
- [ ] AC-028: `pull_request_target` warned
- [ ] AC-029: Fork safety enforced
- [ ] AC-030: OIDC suggested

### Cache
- [ ] AC-031: Cache stores SHA lookups
- [ ] AC-032: Cache hit avoids network
- [ ] AC-033: Cache miss fetches API
- [ ] AC-034: Cache expires after TTL
- [ ] AC-035: Cache persists restarts

### Warnings and Errors
- [ ] AC-036: Unknown action warns
- [ ] AC-037: Third-party action warns
- [ ] AC-038: Dangerous pattern errors
- [ ] AC-039: Over-permissive warns
- [ ] AC-040: Clear error messages

### Version Updates
- [ ] AC-041: Update check available
- [ ] AC-042: Outdated versions flagged
- [ ] AC-043: Update suggestions provided
- [ ] AC-044: Breaking changes noted

### Output Quality
- [ ] AC-045: Human-readable format
- [ ] AC-046: Comments explain security
- [ ] AC-047: Permissions documented
- [ ] AC-048: Audit-friendly
- [ ] AC-049: SHA visible
- [ ] AC-050: Version tag visible

---

## User Verification Scenarios

### Scenario 1: SHA Pinning Applied
**Persona:** Security-conscious developer  
**Preconditions:** Generate .NET workflow  
**Steps:**
1. Run `acode ci generate --stack dotnet`
2. Check actions/checkout reference
3. Verify SHA format
4. Confirm version comment

**Verification Checklist:**
- [ ] Uses `@sha` not `@v4`
- [ ] SHA is 40 characters
- [ ] Comment shows `# v4.x.x`
- [ ] All actions pinned

### Scenario 2: Minimal Permissions Default
**Persona:** Developer checking permissions  
**Preconditions:** Generate any workflow  
**Steps:**
1. Run `acode ci generate --stack node`
2. Check top-level permissions
3. Verify `contents: read`
4. No write permissions

**Verification Checklist:**
- [ ] `permissions:` block exists
- [ ] `contents: read` only
- [ ] No write permissions
- [ ] Secure by default

### Scenario 3: Job-Level Permission Override
**Persona:** Developer with release job  
**Preconditions:** Workflow with release step  
**Steps:**
1. Generate workflow with release
2. Check release job permissions
3. Verify contents write
4. Build job still read-only

**Verification Checklist:**
- [ ] Top-level read
- [ ] Release job has write
- [ ] Build job unchanged
- [ ] Isolation maintained

### Scenario 4: Dangerous Pattern Blocked
**Persona:** Developer with unsafe template  
**Preconditions:** Template with github.event in run  
**Steps:**
1. Attempt generation with injection
2. Error blocks generation
3. Clear message shown
4. Fix suggestion provided

**Verification Checklist:**
- [ ] Generation blocked
- [ ] Security error shown
- [ ] Pattern identified
- [ ] Fix suggested

### Scenario 5: Third-Party Action Warning
**Persona:** Developer using external action  
**Preconditions:** Template references non-GitHub action  
**Steps:**
1. Generate workflow with third-party
2. Warning displayed
3. SHA still pinned
4. User acknowledges

**Verification Checklist:**
- [ ] Warning shown
- [ ] Action still included
- [ ] SHA pinned
- [ ] Risk documented

### Scenario 6: Cache Behavior
**Persona:** Developer generating multiple times  
**Preconditions:** Previous SHA lookups cached  
**Steps:**
1. First generation (cache miss)
2. Second generation (cache hit)
3. Check latency difference
4. Verify same SHA used

**Verification Checklist:**
- [ ] First slower (API call)
- [ ] Second faster (cached)
- [ ] Same SHA both times
- [ ] No API call on hit

---

## Testing Requirements

### Unit Tests

| ID | Test Case | Validates |
|----|-----------|-----------|
| UT-034B-01 | SHA format validation 40 hex | FR-034B-04 |
| UT-034B-02 | Permission block parsing | FR-034B-21 |
| UT-034B-03 | Dangerous pattern detection | FR-034B-42 |
| UT-034B-04 | Script injection pattern | FR-034B-43 |
| UT-034B-05 | Version cache read | FR-034B-09 |
| UT-034B-06 | Version cache write | FR-034B-09 |
| UT-034B-07 | Cache TTL expiry | FR-034B-10 |
| UT-034B-08 | First-party action list | FR-034B-14 |
| UT-034B-09 | Permission inference | FR-034B-37 |
| UT-034B-10 | write-all rejection | FR-034B-36 |
| UT-034B-11 | Broad permission warning | FR-034B-35 |
| UT-034B-12 | Local action path | FR-034B-19 |
| UT-034B-13 | Comment generation | FR-034B-06 |
| UT-034B-14 | Fallback to tag | FR-034B-11 |
| UT-034B-15 | pull_request_target warning | FR-034B-54 |

### Integration Tests

| ID | Test Case | Validates |
|----|-----------|-----------|
| IT-034B-01 | GitHub API SHA lookup | FR-034B-08 |
| IT-034B-02 | Full workflow with pinning | E2E |
| IT-034B-03 | Security scan full workflow | FR-034B-41 |
| IT-034B-04 | Permission inference full | FR-034B-38 |
| IT-034B-05 | Cache persistence | NFR-034B-03 |
| IT-034B-06 | API rate limit handling | NFR-034B-09 |
| IT-034B-07 | Third-party warning flow | AC-037 |
| IT-034B-08 | Dangerous pattern block | AC-023 |
| IT-034B-09 | Job-level permissions | FR-034B-25 |
| IT-034B-10 | Version update check | FR-034B-20 |
| IT-034B-11 | Offline mode (cache only) | NFR-034B-20 |
| IT-034B-12 | Concurrency and timeout | FR-034B-50 |
| IT-034B-13 | Comment documentation | FR-034B-40 |
| IT-034B-14 | Audit trail logging | NFR-034B-30 |
| IT-034B-15 | Fail secure behavior | NFR-034B-19 |

---

## Implementation Prompt

### Part 1: File Structure + Domain Models

```
src/
├── Acode.Domain/
│   └── CiCd/
│       └── Security/
│           ├── SecuritySeverity.cs
│           └── Events/
│               ├── ActionPinnedEvent.cs
│               ├── SecurityIssueDetectedEvent.cs
│               └── PermissionWarningEvent.cs
├── Acode.Application/
│   └── CiCd/
│       └── Security/
│           ├── IActionVersionResolver.cs
│           ├── IPermissionAnalyzer.cs
│           ├── IWorkflowSecurityScanner.cs
│           ├── PinnedAction.cs
│           ├── PermissionSet.cs
│           ├── PermissionWarning.cs
│           └── SecurityIssue.cs
└── Acode.Infrastructure/
    └── CiCd/
        └── Security/
            ├── ActionVersionResolver.cs
            ├── ActionVersionCache.cs
            ├── PermissionAnalyzer.cs
            ├── WorkflowSecurityScanner.cs
            └── Patterns/
                ├── ScriptInjectionPattern.cs
                └── DangerousPatternRegistry.cs
```

```csharp
// src/Acode.Domain/CiCd/Security/SecuritySeverity.cs
namespace Acode.Domain.CiCd.Security;

public enum SecuritySeverity
{
    Info,
    Warning,
    Error,
    Critical
}

// src/Acode.Domain/CiCd/Security/Events/ActionPinnedEvent.cs
namespace Acode.Domain.CiCd.Security.Events;

public sealed record ActionPinnedEvent(
    string ActionRef,
    string Sha,
    string Tag,
    bool FromCache,
    DateTimeOffset Timestamp) : IDomainEvent;

// src/Acode.Domain/CiCd/Security/Events/SecurityIssueDetectedEvent.cs
namespace Acode.Domain.CiCd.Security.Events;

public sealed record SecurityIssueDetectedEvent(
    string Pattern,
    SecuritySeverity Severity,
    int Line,
    string WorkflowName,
    DateTimeOffset Timestamp) : IDomainEvent;

// src/Acode.Domain/CiCd/Security/Events/PermissionWarningEvent.cs
namespace Acode.Domain.CiCd.Security.Events;

public sealed record PermissionWarningEvent(
    string Permission,
    string Reason,
    SecuritySeverity Severity,
    DateTimeOffset Timestamp) : IDomainEvent;
```

**End of Task 034.b Specification - Part 1/3**

### Part 2: Application Interfaces

```csharp
// src/Acode.Application/CiCd/Security/PinnedAction.cs
namespace Acode.Application.CiCd.Security;

public sealed record PinnedAction
{
    public required string Owner { get; init; }
    public required string Repo { get; init; }
    public required string Sha { get; init; }
    public required string Tag { get; init; }
    public string FullRef => $"{Owner}/{Repo}@{Sha}";
    public string Comment => $"# {Tag}";
}

// src/Acode.Application/CiCd/Security/PermissionSet.cs
namespace Acode.Application.CiCd.Security;

public sealed record PermissionSet
{
    public IReadOnlyDictionary<string, string> Permissions { get; init; } = new Dictionary<string, string>();
    public IReadOnlyList<string> Reasons { get; init; } = [];
}

// src/Acode.Application/CiCd/Security/PermissionWarning.cs
namespace Acode.Application.CiCd.Security;

public sealed record PermissionWarning
{
    public required string Permission { get; init; }
    public required string Reason { get; init; }
    public SecuritySeverity Severity { get; init; }
}

// src/Acode.Application/CiCd/Security/SecurityIssue.cs
namespace Acode.Application.CiCd.Security;

public sealed record SecurityIssue
{
    public required string Pattern { get; init; }
    public required string Description { get; init; }
    public SecuritySeverity Severity { get; init; }
    public int Line { get; init; }
    public string? Suggestion { get; init; }
}

// src/Acode.Application/CiCd/Security/IActionVersionResolver.cs
namespace Acode.Application.CiCd.Security;

public interface IActionVersionResolver
{
    Task<PinnedAction?> ResolveAsync(string actionRef, CancellationToken ct = default);
    Task<PinnedAction?> GetCachedAsync(string actionRef);
    Task InvalidateCacheAsync(string actionRef, CancellationToken ct = default);
}

// src/Acode.Application/CiCd/Security/IPermissionAnalyzer.cs
namespace Acode.Application.CiCd.Security;

public interface IPermissionAnalyzer
{
    PermissionSet InferMinimal(CiWorkflow workflow);
    IReadOnlyList<PermissionWarning> Analyze(CiWorkflow workflow);
    bool HasDangerousPermissions(CiWorkflow workflow);
}

// src/Acode.Application/CiCd/Security/IWorkflowSecurityScanner.cs
namespace Acode.Application.CiCd.Security;

public interface IWorkflowSecurityScanner
{
    IReadOnlyList<SecurityIssue> Scan(string workflowContent);
    bool HasCriticalIssues(string workflowContent);
}
```

**End of Task 034.b Specification - Part 2/3**

### Part 3: Infrastructure Implementation + Checklist

```csharp
// src/Acode.Infrastructure/CiCd/Security/ActionVersionCache.cs
namespace Acode.Infrastructure.CiCd.Security;

public sealed class ActionVersionCache
{
    private readonly ConcurrentDictionary<string, CacheEntry> _cache = new();
    private readonly TimeSpan _ttl = TimeSpan.FromHours(24);
    
    public PinnedAction? Get(string actionRef)
    {
        if (_cache.TryGetValue(actionRef, out var entry) && !entry.IsExpired(_ttl))
            return entry.Action;
        return null;
    }
    
    public void Set(string actionRef, PinnedAction action)
    {
        _cache[actionRef] = new CacheEntry(action, DateTimeOffset.UtcNow);
    }
    
    private sealed record CacheEntry(PinnedAction Action, DateTimeOffset CachedAt)
    {
        public bool IsExpired(TimeSpan ttl) => DateTimeOffset.UtcNow - CachedAt > ttl;
    }
}

// src/Acode.Infrastructure/CiCd/Security/ActionVersionResolver.cs
namespace Acode.Infrastructure.CiCd.Security;

public sealed class ActionVersionResolver : IActionVersionResolver
{
    private readonly ActionVersionCache _cache;
    private readonly HttpClient _http;
    private readonly IEventPublisher _events;
    
    public async Task<PinnedAction?> ResolveAsync(string actionRef, CancellationToken ct)
    {
        var cached = _cache.Get(actionRef);
        if (cached != null)
        {
            await _events.PublishAsync(new ActionPinnedEvent(actionRef, cached.Sha, cached.Tag, true, DateTimeOffset.UtcNow), ct);
            return cached;
        }
        
        var (owner, repo, tag) = ParseActionRef(actionRef);
        var sha = await LookupShaAsync(owner, repo, tag, ct);
        if (sha == null) return null;
        
        var pinned = new PinnedAction { Owner = owner, Repo = repo, Sha = sha, Tag = tag };
        _cache.Set(actionRef, pinned);
        
        await _events.PublishAsync(new ActionPinnedEvent(actionRef, sha, tag, false, DateTimeOffset.UtcNow), ct);
        return pinned;
    }
    
    private async Task<string?> LookupShaAsync(string owner, string repo, string tag, CancellationToken ct)
    {
        var url = $"https://api.github.com/repos/{owner}/{repo}/git/ref/tags/{tag}";
        var response = await _http.GetAsync(url, ct);
        if (!response.IsSuccessStatusCode) return null;
        
        var json = await response.Content.ReadAsStringAsync(ct);
        var match = Regex.Match(json, @"""sha"":\s*""([a-f0-9]{40})""");
        return match.Success ? match.Groups[1].Value : null;
    }
}

// src/Acode.Infrastructure/CiCd/Security/WorkflowSecurityScanner.cs
namespace Acode.Infrastructure.CiCd.Security;

public sealed class WorkflowSecurityScanner : IWorkflowSecurityScanner
{
    private static readonly (string Pattern, string Description, SecuritySeverity Severity, string Suggestion)[] DangerousPatterns =
    [
        (@"\$\{\{\s*github\.event\.(issue|pull_request)\.(title|body)", "Script injection risk", SecuritySeverity.Critical, "Use environment variable with proper escaping"),
        (@"permissions:\s*write-all", "Over-privileged workflow", SecuritySeverity.Error, "Specify exact permissions needed"),
        (@"pull_request_target:", "Fork attack risk", SecuritySeverity.Warning, "Be careful with checkout ref"),
        (@"env:\s*\$\{\{.*secrets\.", "Potential secret exposure", SecuritySeverity.Warning, "Use secrets context directly in steps")
    ];
    
    public IReadOnlyList<SecurityIssue> Scan(string workflowContent)
    {
        var issues = new List<SecurityIssue>();
        var lines = workflowContent.Split('\n');
        
        for (var i = 0; i < lines.Length; i++)
        {
            foreach (var (pattern, desc, severity, suggestion) in DangerousPatterns)
            {
                if (Regex.IsMatch(lines[i], pattern, RegexOptions.IgnoreCase))
                {
                    issues.Add(new SecurityIssue
                    {
                        Pattern = pattern,
                        Description = desc,
                        Severity = severity,
                        Line = i + 1,
                        Suggestion = suggestion
                    });
                }
            }
        }
        
        return issues;
    }
    
    public bool HasCriticalIssues(string content) =>
        Scan(content).Any(i => i.Severity == SecuritySeverity.Critical);
}
```

### Implementation Checklist

| Step | Action | Verification |
|------|--------|--------------|
| 1 | Create SecuritySeverity enum | Enum compiles |
| 2 | Add security events | Event serialization verified |
| 3 | Define PinnedAction, PermissionSet, SecurityIssue records | Records compile |
| 4 | Create IActionVersionResolver, IPermissionAnalyzer, IWorkflowSecurityScanner | Interfaces clear |
| 5 | Implement ActionVersionCache with 24h TTL | Cache expiry works |
| 6 | Implement ActionVersionResolver | GitHub API lookup works |
| 7 | Parse action references | owner/repo@tag parsing |
| 8 | SHA lookup via GitHub API | Full 40-char SHA returned |
| 9 | Implement PermissionAnalyzer | Minimal permissions inferred |
| 10 | Add dangerous permission detection | write-all blocked |
| 11 | Implement WorkflowSecurityScanner | Pattern matching works |
| 12 | Add script injection pattern | ${{ github.event.* }} detected |
| 13 | Add permission warning generation | Warnings have suggestions |
| 14 | Register in DI | All services resolved |

### Rollout Plan

1. **Phase 1**: Implement ActionVersionCache and resolver
2. **Phase 2**: Add GitHub API integration for SHA lookup
3. **Phase 3**: Build PermissionAnalyzer with minimal inference
4. **Phase 4**: Implement WorkflowSecurityScanner with patterns
5. **Phase 5**: Integration with template generator

**End of Task 034.b Specification**