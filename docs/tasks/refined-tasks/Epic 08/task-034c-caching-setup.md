# Task 034.c: CI Workflow Caching Setup

**Priority:** P1 – High  
**Tier:** L – Feature Layer  
**Complexity:** 3 (Fibonacci points)  
**Phase:** Phase 8 – CI/CD Integration  
**Dependencies:** Task 034 (CI Template Generator), Task 034.a (Stack Templates), Task 050 (Workspace DB)  

---

## Description

Task 034.c implements intelligent caching setup for generated CI workflows. Dependency caching MUST be configured automatically based on detected stack and package manager. Cache keys MUST be deterministic and include lockfile hashes.

Proper caching dramatically reduces CI build times. Without caching, every build downloads all dependencies from scratch. With caching, subsequent builds restore dependencies in seconds instead of minutes.

This task generates the caching configuration sections for GitHub Actions workflows. It supports NuGet caching for .NET, npm/yarn/pnpm caching for Node.js, and provides an extensible framework for additional package managers.

Cache configuration MUST include proper key generation with fallback keys, cache path specification, and restoration/saving logic. Cache metrics (hits/misses) SHOULD be recordable in the Workspace DB for diagnostics and optimization.

### Business Value

Caching configuration provides:
- Faster CI builds (60-80% time reduction typical)
- Reduced bandwidth and egress costs
- Improved developer experience
- Consistent, optimized cache key strategies

### Scope Boundaries

This task covers cache configuration generation. Template content is in 034.a. Security settings are in 034.b. Cache metrics storage is optional integration with Task 050.

### Integration Points

| Component | Interface | Data Flow | Notes |
|-----------|-----------|-----------|-------|
| Template Generator | `ICiTemplateGenerator` | Requests cache config | From 034 |
| Stack Provider | `ICiStackProvider` | Provides cache paths | Per-stack |
| Package Manager | `IPackageManagerDetector` | Detects lockfiles | For key hash |
| Workspace DB | `IWorkspaceDb` | Store cache metrics | Optional |
| GitHub Actions | `actions/cache@v4` | Cache action used | SHA-pinned |
| Security Module | Task 034.b | Pins cache action | Required |
| Audit Events | `IEventPublisher` | Publish cache events | Async |

### Failure Modes

| Failure | Detection | Recovery | User Impact |
|---------|-----------|----------|-------------|
| Lockfile not found | File scan fails | Skip cache config, warn | Build works, no caching |
| Unsupported package manager | Registry lookup fails | Generic cache suggestion | Manual configuration needed |
| Hash calculation fails | IO exception | Use fallback key only | Cache may not restore |
| Cache path invalid | Validation error | Use default path | May cache wrong location |
| Cache action unavailable | Reference validation | Use tag instead of SHA | Degraded security |
| Workspace DB unavailable | Connection error | Skip metrics, log warning | No cache analytics |
| Large cache size | Size check | Warn user, suggest .gitignore | Slow cache operations |
| Concurrent cache writes | Race condition | GitHub handles | Possible cache miss |

### Mode Compliance

| Operating Mode | Cache Config Generation | Metrics Storage |
|----------------|-------------------------|-----------------|
| Local-Only | ALLOWED | Local DB only |
| Burst | ALLOWED | Full metrics |
| Air-Gapped | ALLOWED | Local DB only |

### Assumptions

1. **Lockfiles exist**: Projects have package-lock.json, yarn.lock, or packages.lock.json
2. **Standard cache paths**: Package managers use conventional cache locations
3. **GitHub Actions runner**: Cache action is available on runner
4. **Cache size limits**: GitHub has 10GB cache limit per repository
5. **Hash algorithm stable**: SHA-256 for lockfile hashing
6. **Restore before save**: Cache restored at start, saved at end
7. **Fallback keys ordered**: More specific keys tried first
8. **Cross-OS cache compatible**: Cache keys include OS for isolation

### Security Considerations

1. **No secrets in cache keys**: Cache keys never include secret values
2. **Pinned cache action**: actions/cache uses SHA pinning (034.b)
3. **Cache isolation**: Caches are repository-scoped by GitHub
4. **No credential caching**: Never cache authentication tokens
5. **Lockfile integrity**: Lockfile hash ensures dependency integrity
6. **Fork cache isolation**: Forks have separate cache namespace
7. **Audit trail**: Cache configuration changes are logged
8. **Minimal cache scope**: Only cache what's necessary

---

## Glossary / Terms

| Term | Definition |
|------|------------|
| Cache Key | Unique identifier for cached artifacts |
| Restore Key | Fallback key for partial cache match |
| Cache Path | Directory location to cache |
| Lockfile | Dependency version lock file |
| Cache Hit | Exact key match found |
| Cache Miss | No matching cache found |
| Partial Hit | Restore key match (not exact) |
| TTL | Time-to-live for cache validity |

---

## Out of Scope

- Remote cache servers (not GitHub Actions cache)
- Docker layer caching
- Build artifact caching (separate from dependency cache)
- Cache cleanup automation
- Multi-repository shared caches
- Custom cache backends

---

## Functional Requirements

### FR-001 to FR-015: Cache Configuration Interface

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-034C-01 | `ICacheConfigProvider` interface MUST exist in Application layer | P0 |
| FR-034C-02 | `GetCacheConfigAsync` MUST return cache configuration for stack | P0 |
| FR-034C-03 | Input MUST include stack type and package manager | P0 |
| FR-034C-04 | Output MUST include cache key, restore keys, and paths | P0 |
| FR-034C-05 | Provider MUST be extensible for new package managers | P1 |
| FR-034C-06 | Cache config MUST be optional (skip if not applicable) | P1 |
| FR-034C-07 | Registry MUST resolve provider by stack and package manager | P1 |
| FR-034C-08 | Missing provider MUST return empty config, not error | P1 |
| FR-034C-09 | Config MUST include cache action reference (SHA-pinned) | P0 |
| FR-034C-10 | Config MUST include proper YAML structure | P0 |
| FR-034C-11 | Provider MUST log cache configuration decisions | P1 |
| FR-034C-12 | Provider MUST emit metrics on cache config generation | P2 |
| FR-034C-13 | Config MUST be insertable into workflow at correct position | P0 |
| FR-034C-14 | Cache step MUST come after checkout, before build | P0 |
| FR-034C-15 | Config MUST support conditional caching (if: statements) | P2 |

### FR-016 to FR-030: Cache Key Generation

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-034C-16 | Cache key MUST include runner OS | P0 |
| FR-034C-17 | Key format MUST be `{prefix}-{os}-{hash}` | P1 |
| FR-034C-18 | Hash MUST be calculated from lockfile | P0 |
| FR-034C-19 | Hash MUST use `hashFiles()` GitHub function | P0 |
| FR-034C-20 | Lockfile pattern MUST be correct for package manager | P0 |
| FR-034C-21 | Restore keys MUST be ordered by specificity | P1 |
| FR-034C-22 | First restore key SHOULD be `{prefix}-{os}-` | P1 |
| FR-034C-23 | Second restore key SHOULD be `{prefix}-` | P2 |
| FR-034C-24 | Key prefix MUST identify package manager | P1 |
| FR-034C-25 | NuGet prefix MUST be `nuget` | P1 |
| FR-034C-26 | npm prefix MUST be `npm` | P1 |
| FR-034C-27 | yarn prefix MUST be `yarn` | P1 |
| FR-034C-28 | pnpm prefix MUST be `pnpm` | P1 |
| FR-034C-29 | Key MUST NOT contain secrets or sensitive data | P0 |
| FR-034C-30 | Key MUST be deterministic for same inputs | P0 |

### FR-031 to FR-045: .NET NuGet Caching

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-034C-31 | NuGet cache provider MUST exist | P0 |
| FR-034C-32 | Cache path MUST be `~/.nuget/packages` | P0 |
| FR-034C-33 | Lockfile MUST be `**/packages.lock.json` | P1 |
| FR-034C-34 | Fallback MUST use `**/*.csproj` hash if no lockfile | P1 |
| FR-034C-35 | Multiple csproj files MUST be included in hash | P1 |
| FR-034C-36 | NuGet.Config SHOULD be included in hash | P2 |
| FR-034C-37 | Cache MUST work with `dotnet restore` | P0 |
| FR-034C-38 | Cache MUST work across .NET versions | P1 |
| FR-034C-39 | Windows path MUST be handled correctly | P1 |
| FR-034C-40 | Linux/macOS path MUST be handled correctly | P1 |
| FR-034C-41 | Global packages folder MUST be cached | P0 |
| FR-034C-42 | HTTP cache MAY be optionally cached | P2 |
| FR-034C-43 | Cache size warning if >1GB packages | P2 |
| FR-034C-44 | Restore step MUST log cache hit/miss | P1 |
| FR-034C-45 | Save step MUST run on success only | P1 |

### FR-046 to FR-060: Node.js Caching

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-034C-46 | Node.js cache provider MUST exist | P0 |
| FR-034C-47 | npm cache path MUST be `~/.npm` | P0 |
| FR-034C-48 | yarn cache path MUST use `yarn cache dir` | P0 |
| FR-034C-49 | pnpm cache path MUST be `~/.pnpm-store` | P1 |
| FR-034C-50 | npm lockfile MUST be `**/package-lock.json` | P0 |
| FR-034C-51 | yarn lockfile MUST be `**/yarn.lock` | P0 |
| FR-034C-52 | pnpm lockfile MUST be `**/pnpm-lock.yaml` | P1 |
| FR-034C-53 | node_modules MUST NOT be cached (restore is faster) | P0 |
| FR-034C-54 | setup-node cache option MAY be used instead | P2 |
| FR-034C-55 | Monorepo lockfiles MUST be detected | P1 |
| FR-034C-56 | Workspace root lockfile MUST be used | P1 |
| FR-034C-57 | .npmrc SHOULD NOT affect cache key | P2 |
| FR-034C-58 | Cache MUST work with npm ci | P0 |
| FR-034C-59 | Cache MUST work with yarn install --frozen-lockfile | P0 |
| FR-034C-60 | Cache MUST work with pnpm install --frozen-lockfile | P1 |

### FR-061 to FR-075: Cache Metrics and Diagnostics

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-034C-61 | Cache hit/miss events MAY be recorded | P2 |
| FR-034C-62 | Events MUST include cache key used | P2 |
| FR-034C-63 | Events MUST include timestamp | P2 |
| FR-034C-64 | Events MUST include workflow run context | P2 |
| FR-034C-65 | Workspace DB integration MUST be optional | P1 |
| FR-034C-66 | Metrics MUST include cache size estimate | P2 |
| FR-034C-67 | Metrics MUST include restore duration | P2 |
| FR-034C-68 | Dashboard query SHOULD show hit rate | P2 |
| FR-034C-69 | Cache key history SHOULD be stored | P2 |
| FR-034C-70 | Stale cache detection SHOULD be available | P2 |
| FR-034C-71 | Cache generation events MUST be published | P1 |
| FR-034C-72 | Event MUST include stack and package manager | P1 |
| FR-034C-73 | Event MUST include generated key pattern | P1 |
| FR-034C-74 | Event MUST include cache paths | P1 |
| FR-034C-75 | Event MUST include workflow file path | P1 |

---

## Non-Functional Requirements

### Performance Requirements

| ID | Requirement | Target | Priority |
|----|-------------|--------|----------|
| NFR-034C-01 | Cache config generation latency | <100ms | P1 |
| NFR-034C-02 | Lockfile hash calculation | <50ms | P1 |
| NFR-034C-03 | Provider lookup latency | <10ms | P2 |
| NFR-034C-04 | Memory for config generation | <10MB | P2 |
| NFR-034C-05 | YAML rendering time | <20ms | P2 |
| NFR-034C-06 | File scan for lockfiles | <500ms | P1 |
| NFR-034C-07 | Metrics write latency | <50ms | P2 |
| NFR-034C-08 | Cache path validation | <10ms | P2 |
| NFR-034C-09 | Concurrent config generation | 5 parallel | P2 |
| NFR-034C-10 | Template insertion time | <20ms | P2 |

### Reliability Requirements

| ID | Requirement | Target | Priority |
|----|-------------|--------|----------|
| NFR-034C-11 | Valid cache config output | 100% | P0 |
| NFR-034C-12 | Deterministic key generation | Always | P0 |
| NFR-034C-13 | Graceful fallback on error | Use defaults | P0 |
| NFR-034C-14 | Missing lockfile handling | Warn, continue | P1 |
| NFR-034C-15 | Cross-platform path handling | Correct | P0 |
| NFR-034C-16 | Cache action compatibility | actions/cache@v4 | P0 |
| NFR-034C-17 | Idempotent generation | Same input = same output | P0 |
| NFR-034C-18 | No cache corruption | Safe keys | P0 |
| NFR-034C-19 | Extensible provider system | Plugin-based | P1 |
| NFR-034C-20 | Backward compatible output | Current GitHub | P1 |

### Observability Requirements

| ID | Requirement | Target | Priority |
|----|-------------|--------|----------|
| NFR-034C-21 | Structured logging for generation | JSON format | P1 |
| NFR-034C-22 | Metrics on config generation | Per-stack | P2 |
| NFR-034C-23 | Events on cache config created | Async publish | P1 |
| NFR-034C-24 | Clear warnings for issues | Human-readable | P0 |
| NFR-034C-25 | Cache key logged | Info level | P1 |
| NFR-034C-26 | Cache paths logged | Debug level | P2 |
| NFR-034C-27 | Provider selection logged | Debug level | P2 |
| NFR-034C-28 | Lockfile detection logged | Info level | P1 |
| NFR-034C-29 | Fallback usage logged | Warning level | P1 |
| NFR-034C-30 | Metrics export support | Prometheus | P2 |

---

## User Manual Documentation

### Configuration

```yaml
ciTemplates:
  caching:
    enabled: true
    nuget:
      enabled: true
      includeLockfile: true
      includeNuGetConfig: false
    node:
      enabled: true
      preferSetupNodeCache: false
      cacheNodeModules: false
```

### CLI Usage

```bash
# Generate workflow with caching
acode ci generate --stack dotnet

# Generate without caching
acode ci generate --stack node --no-cache

# Show cache configuration that would be generated
acode ci cache-config --stack dotnet --dry-run
```

### Generated Cache Configuration Examples

#### .NET NuGet Caching
```yaml
- name: Cache NuGet packages
  uses: actions/cache@0c45773b623bea8c8e75f6c82b208c3cf94ea4f9 # v4.0.2
  with:
    path: ~/.nuget/packages
    key: nuget-${{ runner.os }}-${{ hashFiles('**/packages.lock.json') }}
    restore-keys: |
      nuget-${{ runner.os }}-
      nuget-
```

#### Node.js npm Caching
```yaml
- name: Cache npm dependencies
  uses: actions/cache@0c45773b623bea8c8e75f6c82b208c3cf94ea4f9 # v4.0.2
  with:
    path: ~/.npm
    key: npm-${{ runner.os }}-${{ hashFiles('**/package-lock.json') }}
    restore-keys: |
      npm-${{ runner.os }}-
      npm-
```

---

## Acceptance Criteria / Definition of Done

### Cache Configuration Interface
- [ ] AC-001: `ICacheConfigProvider` interface exists
- [ ] AC-002: `GetCacheConfigAsync` returns valid config
- [ ] AC-003: Input includes stack and package manager
- [ ] AC-004: Output includes key, restore keys, paths
- [ ] AC-005: Registry resolves providers correctly
- [ ] AC-006: Missing provider returns empty, not error
- [ ] AC-007: Config includes SHA-pinned cache action

### Cache Key Generation
- [ ] AC-008: Key includes runner OS
- [ ] AC-009: Key format is `{prefix}-{os}-{hash}`
- [ ] AC-010: Hash uses `hashFiles()` function
- [ ] AC-011: Lockfile pattern correct per package manager
- [ ] AC-012: Restore keys ordered by specificity
- [ ] AC-013: Keys are deterministic
- [ ] AC-014: No secrets in keys

### .NET NuGet Caching
- [ ] AC-015: NuGet provider exists
- [ ] AC-016: Cache path is `~/.nuget/packages`
- [ ] AC-017: Lockfile pattern is `**/packages.lock.json`
- [ ] AC-018: Fallback uses `**/*.csproj` hash
- [ ] AC-019: Works with `dotnet restore`
- [ ] AC-020: Cross-platform paths correct
- [ ] AC-021: Key prefix is `nuget`

### Node.js Caching
- [ ] AC-022: Node.js provider exists
- [ ] AC-023: npm path is `~/.npm`
- [ ] AC-024: yarn path uses `yarn cache dir`
- [ ] AC-025: pnpm path is `~/.pnpm-store`
- [ ] AC-026: npm lockfile is `**/package-lock.json`
- [ ] AC-027: yarn lockfile is `**/yarn.lock`
- [ ] AC-028: pnpm lockfile is `**/pnpm-lock.yaml`
- [ ] AC-029: node_modules NOT cached
- [ ] AC-030: Monorepo lockfiles detected

### Workflow Integration
- [ ] AC-031: Cache step after checkout
- [ ] AC-032: Cache step before build
- [ ] AC-033: Valid YAML generated
- [ ] AC-034: Indentation correct
- [ ] AC-035: Comment explains caching

### Error Handling
- [ ] AC-036: Missing lockfile warns
- [ ] AC-037: Unknown package manager skips
- [ ] AC-038: Fallback to project files works
- [ ] AC-039: Error messages actionable
- [ ] AC-040: Graceful degradation

### Metrics and Diagnostics
- [ ] AC-041: Cache config event published
- [ ] AC-042: Event includes stack type
- [ ] AC-043: Event includes package manager
- [ ] AC-044: Event includes key pattern
- [ ] AC-045: Workspace DB integration optional

### Observability
- [ ] AC-046: Generation logged structured
- [ ] AC-047: Provider selection logged
- [ ] AC-048: Lockfile detection logged
- [ ] AC-049: Fallback usage warned
- [ ] AC-050: Clear error messages

---

## User Verification Scenarios

### Scenario 1: .NET Project with Lockfile
**Persona:** Developer with .NET project  
**Preconditions:** Repository has packages.lock.json  
**Steps:**
1. Run `acode ci generate --stack dotnet`
2. Check generated workflow
3. Find cache step
4. Verify key uses lockfile hash

**Verification Checklist:**
- [ ] Cache step present
- [ ] Key includes `packages.lock.json`
- [ ] Path is `~/.nuget/packages`
- [ ] Restore keys present

### Scenario 2: .NET Project without Lockfile
**Persona:** Developer with older .NET project  
**Preconditions:** No packages.lock.json  
**Steps:**
1. Run `acode ci generate --stack dotnet`
2. See warning about missing lockfile
3. Check fallback to csproj hash
4. Verify cache still works

**Verification Checklist:**
- [ ] Warning displayed
- [ ] Fallback to `*.csproj` used
- [ ] Cache step still present
- [ ] Recommendation to add lockfile

### Scenario 3: Node.js with npm
**Persona:** Developer with npm project  
**Preconditions:** Has package-lock.json  
**Steps:**
1. Run `acode ci generate --stack node`
2. Check cache configuration
3. Verify npm path used
4. Confirm lockfile in key

**Verification Checklist:**
- [ ] Path is `~/.npm`
- [ ] Key prefix is `npm`
- [ ] Lockfile is package-lock.json
- [ ] node_modules NOT cached

### Scenario 4: Node.js with yarn
**Persona:** Developer using yarn  
**Preconditions:** Has yarn.lock  
**Steps:**
1. Run `acode ci generate --stack node`
2. yarn detected from lockfile
3. Cache path correct
4. Key uses yarn.lock

**Verification Checklist:**
- [ ] yarn detected
- [ ] Path is yarn cache dir
- [ ] Key prefix is `yarn`
- [ ] yarn.lock in hash

### Scenario 5: Disable Caching
**Persona:** Developer wanting minimal workflow  
**Preconditions:** Any project  
**Steps:**
1. Run `acode ci generate --stack dotnet --no-cache`
2. Check workflow
3. No cache step present
4. Build still works

**Verification Checklist:**
- [ ] No cache step
- [ ] Workflow valid
- [ ] No cache-related config
- [ ] Smaller workflow file

### Scenario 6: pnpm Monorepo
**Persona:** Developer with pnpm workspace  
**Preconditions:** pnpm-workspace.yaml and pnpm-lock.yaml  
**Steps:**
1. Run `acode ci generate --stack node`
2. pnpm detected
3. Root lockfile used
4. Correct store path

**Verification Checklist:**
- [ ] pnpm detected
- [ ] Path is `~/.pnpm-store`
- [ ] Root pnpm-lock.yaml used
- [ ] Key prefix is `pnpm`

---

## Testing Requirements

### Unit Tests

| ID | Test Case | Validates |
|----|-----------|-----------|
| UT-034C-01 | NuGet cache key generation | FR-034C-31 |
| UT-034C-02 | npm cache key generation | FR-034C-46 |
| UT-034C-03 | yarn cache key generation | FR-034C-48 |
| UT-034C-04 | pnpm cache key generation | FR-034C-49 |
| UT-034C-05 | Lockfile pattern matching | FR-034C-20 |
| UT-034C-06 | Restore keys ordering | FR-034C-21 |
| UT-034C-07 | Key determinism | FR-034C-30 |
| UT-034C-08 | Fallback to project files | FR-034C-34 |
| UT-034C-09 | Cross-platform path handling | FR-034C-39 |
| UT-034C-10 | Provider registry lookup | FR-034C-07 |
| UT-034C-11 | Empty config for unknown PM | FR-034C-08 |
| UT-034C-12 | YAML rendering correctness | FR-034C-10 |
| UT-034C-13 | Cache step positioning | FR-034C-14 |
| UT-034C-14 | SHA-pinned action reference | FR-034C-09 |
| UT-034C-15 | Config generation < 100ms | NFR-034C-01 |

### Integration Tests

| ID | Test Case | Validates |
|----|-----------|-----------|
| IT-034C-01 | Full .NET workflow with cache | E2E |
| IT-034C-02 | Full Node.js workflow with cache | E2E |
| IT-034C-03 | Real lockfile detection | FR-034C-20 |
| IT-034C-04 | Monorepo lockfile handling | FR-034C-55 |
| IT-034C-05 | --no-cache flag | Scenario 5 |
| IT-034C-06 | Missing lockfile fallback | Scenario 2 |
| IT-034C-07 | Package manager auto-detection | FR-034C-28 |
| IT-034C-08 | Event publishing | NFR-034C-23 |
| IT-034C-09 | Workspace DB metrics (optional) | FR-034C-65 |
| IT-034C-10 | Cross-platform generation | NFR-034C-15 |
| IT-034C-11 | Cache + security integration | 034.b |
| IT-034C-12 | Workflow YAML validity | NFR-034C-11 |
| IT-034C-13 | Logging correctness | NFR-034C-21 |
| IT-034C-14 | Warning on issues | NFR-034C-24 |
| IT-034C-15 | Multiple package managers | Complex |

---

## Implementation Prompt

### Part 1: File Structure

```
src/
├── Acode.Domain/
│   └── CiCd/
│       └── Caching/
│           └── Events/
│               ├── CacheConfigGeneratedEvent.cs
│               └── CacheLockfileNotFoundEvent.cs
├── Acode.Application/
│   └── CiCd/
│       └── Caching/
│           ├── ICacheConfigProvider.cs
│           ├── ICacheConfigRegistry.cs
│           ├── CacheConfig.cs
│           └── CacheConfigRequest.cs
└── Acode.Infrastructure/
    └── CiCd/
        └── Caching/
            ├── CacheConfigRegistry.cs
            └── Providers/
                ├── NuGetCacheProvider.cs
                ├── NpmCacheProvider.cs
                ├── YarnCacheProvider.cs
                └── PnpmCacheProvider.cs
```

### Part 2: Core Interfaces

```csharp
// src/Acode.Application/CiCd/Caching/CacheConfig.cs
namespace Acode.Application.CiCd.Caching;

public sealed record CacheConfig
{
    public bool Enabled { get; init; } = true;
    public required string ActionRef { get; init; }
    public required string Path { get; init; }
    public required string Key { get; init; }
    public required IReadOnlyList<string> RestoreKeys { get; init; }
    public string? Condition { get; init; }
}

// src/Acode.Application/CiCd/Caching/ICacheConfigProvider.cs
namespace Acode.Application.CiCd.Caching;

public interface ICacheConfigProvider
{
    string PackageManager { get; }
    Task<CacheConfig?> GetCacheConfigAsync(CacheConfigRequest request, CancellationToken ct = default);
}
```

### Implementation Checklist

| Step | Action | Verification |
|------|--------|--------------|
| 1 | Create CacheConfig record | Compiles |
| 2 | Define ICacheConfigProvider | Interface contract clear |
| 3 | Implement NuGetCacheProvider | NuGet caching works |
| 4 | Implement NpmCacheProvider | npm caching works |
| 5 | Implement YarnCacheProvider | yarn caching works |
| 6 | Implement PnpmCacheProvider | pnpm caching works |
| 7 | Create CacheConfigRegistry | Provider lookup works |
| 8 | Integrate with template generator | Cache step in workflow |
| 9 | Add lockfile detection | Correct patterns used |
| 10 | Add fallback logic | Graceful degradation |
| 11 | Add event publishing | Events emitted |
| 12 | Add logging | Structured logs |
| 13 | Unit tests | All pass |
| 14 | Integration tests | E2E works |

**End of Task 034.c Specification**
