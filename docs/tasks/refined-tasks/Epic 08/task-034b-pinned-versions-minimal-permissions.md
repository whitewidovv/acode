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