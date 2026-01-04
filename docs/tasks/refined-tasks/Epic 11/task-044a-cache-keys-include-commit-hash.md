# Task 044.a: Cache Keys Include Commit Hash

**Priority:** P0 – Critical  
**Tier:** F – Foundation Layer  
**Complexity:** 5 (Fibonacci points)  
**Phase:** Phase 11 – Optimization  
**Dependencies:** Task 044 (Caching), Task 008 (Git Integration)  

---

## Description

Task 044.a implements commit-hash-based cache keys—the mechanism that ensures cache entries are automatically invalidated when the underlying code changes. By including the Git commit hash in cache keys, the cache self-invalidates: any change to the repository produces a new commit hash, which generates new keys, causing cache misses and fresh computation.

This approach is more reliable than file timestamp monitoring. Timestamps can be unreliable (file copy, sync issues), but commit hashes are cryptographically tied to content. If the hash is the same, the content is the same.

### Business Value

Commit-hash keys provide:
- Automatic invalidation
- No stale data
- Content-addressed reliability
- Simple invalidation logic
- Cross-machine consistency

### Scope Boundaries

This task covers key generation with commit hashes. Core caching is Task 044. Stats and clear are Task 044.b. Telemetry is Task 044.c.

### Integration Points

| Component | Interface | Data Flow | Notes |
|-----------|-----------|-----------|-------|
| Cache | Task 044 | Key generation | Consumer |
| Git | Task 008 | Commit hash | Source |
| File System | Path resolution | File identity | Component |
| Indexer | Task 025 | Cache user | Consumer |

### Failure Modes

| Failure | Detection | Recovery | User Impact |
|---------|-----------|----------|-------------|
| Not git repo | Check | Use file hash | Still works |
| Git unavailable | Timeout | File hash | Degraded |
| Dirty worktree | Detect | Include dirty | Fresh compute |
| Detached HEAD | Detect | Use hash | Works |
| Shallow clone | Detect | Use available | Works |

### Assumptions

1. **Git available**: Most of the time
2. **Commit hash stable**: Immutable
3. **Hash changes with content**: By definition
4. **Fallback exists**: File hash
5. **Performance OK**: Hash lookup fast

---

## Glossary / Terms

| Term | Definition |
|------|------------|
| Commit Hash | Git SHA-1/SHA-256 |
| Content Hash | Hash of file content |
| Cache Key | Unique identifier |
| Worktree | Working directory |
| Dirty | Uncommitted changes |
| HEAD | Current commit |
| Tree Hash | Hash of directory |
| Blob Hash | Hash of file |
| Composite Key | Multi-part key |
| Fallback | Alternative method |

---

## Out of Scope

- Partial commit tracking
- Branch prediction
- Merge conflict handling
- Remote tracking
- Submodule hashing
- LFS content hashing

---

## Functional Requirements

### FR-001 to FR-015: Key Generation

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-044a-01 | Key MUST include commit hash | P0 |
| FR-044a-02 | Commit hash MUST be HEAD | P0 |
| FR-044a-03 | Key MUST include file path | P0 |
| FR-044a-04 | Path MUST be normalized | P0 |
| FR-044a-05 | Path MUST be relative to repo | P0 |
| FR-044a-06 | Key MUST include operation type | P0 |
| FR-044a-07 | Key MUST be deterministic | P0 |
| FR-044a-08 | Same inputs = same key | P0 |
| FR-044a-09 | Key MUST be string | P0 |
| FR-044a-10 | Key format MUST be documented | P0 |
| FR-044a-11 | Key MUST be URL-safe | P1 |
| FR-044a-12 | Key length MUST be bounded | P0 |
| FR-044a-13 | Max key MUST be 256 chars | P0 |
| FR-044a-14 | Long paths MUST be hashed | P0 |
| FR-044a-15 | Key MUST be case-sensitive | P0 |

### FR-016 to FR-035: Commit Hash Handling

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-044a-16 | HEAD hash MUST be retrieved | P0 |
| FR-044a-17 | Hash MUST be full SHA | P0 |
| FR-044a-18 | Short SHA MUST NOT be used | P0 |
| FR-044a-19 | Hash MUST be cached briefly | P0 |
| FR-044a-20 | Cache duration MUST be 1s | P0 |
| FR-044a-21 | Cache MUST be per-repo | P0 |
| FR-044a-22 | Dirty worktree MUST be detected | P0 |
| FR-044a-23 | Dirty MUST append marker | P0 |
| FR-044a-24 | Dirty marker MUST be "-dirty" | P0 |
| FR-044a-25 | Staged changes MUST trigger dirty | P0 |
| FR-044a-26 | Unstaged changes MUST trigger dirty | P0 |
| FR-044a-27 | Untracked MUST be ignored | P0 |
| FR-044a-28 | Detached HEAD MUST work | P0 |
| FR-044a-29 | Tag checkout MUST work | P0 |
| FR-044a-30 | Submodule MUST use own hash | P1 |
| FR-044a-31 | Nested repo MUST be handled | P1 |
| FR-044a-32 | Multiple repos MUST work | P0 |
| FR-044a-33 | Repo root MUST be detected | P0 |
| FR-044a-34 | Cross-platform paths MUST normalize | P0 |
| FR-044a-35 | Symlinks MUST resolve | P1 |

### FR-036 to FR-050: Fallback Handling

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-044a-36 | Non-git MUST fallback | P0 |
| FR-044a-37 | Fallback MUST use file hash | P0 |
| FR-044a-38 | File hash MUST be content-based | P0 |
| FR-044a-39 | File hash algorithm MUST be SHA256 | P0 |
| FR-044a-40 | Fallback MUST be logged | P0 |
| FR-044a-41 | Git error MUST fallback | P0 |
| FR-044a-42 | Timeout MUST fallback | P0 |
| FR-044a-43 | Default timeout MUST be 100ms | P0 |
| FR-044a-44 | Timeout MUST be configurable | P1 |
| FR-044a-45 | Fallback MUST not block | P0 |
| FR-044a-46 | Directory hash MUST work | P1 |
| FR-044a-47 | Directory MUST hash recursively | P1 |
| FR-044a-48 | Gitignore MUST be respected | P1 |
| FR-044a-49 | Binary files MUST hash | P0 |
| FR-044a-50 | Large files MUST stream hash | P0 |

### FR-051 to FR-060: Composite Keys

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-044a-51 | Composite keys MUST work | P0 |
| FR-044a-52 | Multiple paths MUST combine | P0 |
| FR-044a-53 | Order MUST be deterministic | P0 |
| FR-044a-54 | Paths MUST be sorted | P0 |
| FR-044a-55 | Separator MUST be consistent | P0 |
| FR-044a-56 | Separator MUST be "::" | P0 |
| FR-044a-57 | Extra data MUST be includable | P0 |
| FR-044a-58 | Version MUST be in key | P0 |
| FR-044a-59 | Schema version MUST invalidate | P0 |
| FR-044a-60 | Key builder MUST be fluent | P1 |

---

## Non-Functional Requirements

### Performance Requirements

| ID | Requirement | Target | Priority |
|----|-------------|--------|----------|
| NFR-044a-01 | Key generation | <5ms | P0 |
| NFR-044a-02 | Hash lookup (cached) | <1ms | P0 |
| NFR-044a-03 | Hash lookup (git) | <50ms | P0 |
| NFR-044a-04 | File hash | <10ms/100KB | P0 |
| NFR-044a-05 | Directory hash | <100ms/1000 files | P0 |
| NFR-044a-06 | Dirty check | <20ms | P0 |
| NFR-044a-07 | Memory usage | <1MB | P0 |
| NFR-044a-08 | Concurrent | Thread-safe | P0 |
| NFR-044a-09 | Cache hit ratio | >99% short-term | P0 |
| NFR-044a-10 | Throughput | 10000 keys/s | P0 |

### Reliability Requirements

| ID | Requirement | Target | Priority |
|----|-------------|--------|----------|
| NFR-044a-11 | Determinism | 100% | P0 |
| NFR-044a-12 | Collision resistance | SHA level | P0 |
| NFR-044a-13 | Cross-platform | Same key | P0 |
| NFR-044a-14 | Fallback success | 100% | P0 |
| NFR-044a-15 | No throw | Fallback | P0 |
| NFR-044a-16 | Git failure recovery | Always | P0 |
| NFR-044a-17 | Path normalization | All OS | P0 |
| NFR-044a-18 | Unicode paths | Supported | P0 |
| NFR-044a-19 | Long paths | Supported | P0 |
| NFR-044a-20 | Concurrent git | Safe | P0 |

### Observability Requirements

| ID | Requirement | Target | Priority |
|----|-------------|--------|----------|
| NFR-044a-21 | Key generated logged | Debug | P0 |
| NFR-044a-22 | Fallback logged | Info | P0 |
| NFR-044a-23 | Dirty logged | Debug | P0 |
| NFR-044a-24 | Error logged | Warning | P0 |
| NFR-044a-25 | Metrics: keys/s | Counter | P1 |
| NFR-044a-26 | Metrics: fallbacks | Counter | P1 |
| NFR-044a-27 | Metrics: dirty rate | Gauge | P2 |
| NFR-044a-28 | Structured logging | JSON | P0 |
| NFR-044a-29 | Performance trace | Optional | P1 |
| NFR-044a-30 | Key format logged | Debug | P0 |

---

## Acceptance Criteria / Definition of Done

### Key Generation
- [ ] AC-001: Commit hash in key
- [ ] AC-002: File path in key
- [ ] AC-003: Operation type in key
- [ ] AC-004: Deterministic
- [ ] AC-005: URL-safe
- [ ] AC-006: Bounded length
- [ ] AC-007: Case-sensitive
- [ ] AC-008: Documented format

### Commit Handling
- [ ] AC-009: HEAD hash retrieved
- [ ] AC-010: Full SHA used
- [ ] AC-011: Hash cached briefly
- [ ] AC-012: Dirty detected
- [ ] AC-013: Dirty marker added
- [ ] AC-014: Detached works
- [ ] AC-015: Multiple repos work
- [ ] AC-016: Paths normalized

### Fallback
- [ ] AC-017: Non-git fallback
- [ ] AC-018: File hash fallback
- [ ] AC-019: Git error fallback
- [ ] AC-020: Timeout fallback
- [ ] AC-021: Fallback logged
- [ ] AC-022: Non-blocking
- [ ] AC-023: Directory hash
- [ ] AC-024: Large files stream

### Composite
- [ ] AC-025: Multiple paths work
- [ ] AC-026: Order deterministic
- [ ] AC-027: Extra data works
- [ ] AC-028: Version included
- [ ] AC-029: Cross-platform
- [ ] AC-030: Tests pass
- [ ] AC-031: Documented
- [ ] AC-032: Reviewed

---

## User Verification Scenarios

### Scenario 1: Fresh Commit
**Persona:** Developer after commit  
**Preconditions:** New commit made  
**Steps:**
1. Make commit
2. Search codebase
3. Cache miss
4. Fresh results cached

**Verification Checklist:**
- [ ] New hash used
- [ ] Cache miss
- [ ] Fresh compute
- [ ] New cache entry

### Scenario 2: Same Commit
**Persona:** Developer searching  
**Preconditions:** No changes  
**Steps:**
1. Search codebase
2. Cache hit
3. Fast result
4. Same hash

**Verification Checklist:**
- [ ] Same hash
- [ ] Cache hit
- [ ] Fast return
- [ ] Correct results

### Scenario 3: Dirty Worktree
**Persona:** Developer with changes  
**Preconditions:** Uncommitted changes  
**Steps:**
1. Make changes
2. Search
3. Dirty detected
4. Fresh compute

**Verification Checklist:**
- [ ] Dirty detected
- [ ] Marker added
- [ ] Cache miss
- [ ] Fresh results

### Scenario 4: Non-Git Directory
**Persona:** Developer with non-git  
**Preconditions:** No git repo  
**Steps:**
1. Open non-git dir
2. Search
3. Fallback used
4. File hash

**Verification Checklist:**
- [ ] Fallback works
- [ ] File hash used
- [ ] No error
- [ ] Caching works

### Scenario 5: Cross-Platform
**Persona:** Developer on different OS  
**Preconditions:** Same repo  
**Steps:**
1. Generate key on Windows
2. Generate key on Linux
3. Same key
4. Cache shared

**Verification Checklist:**
- [ ] Paths normalized
- [ ] Same key
- [ ] Cross-platform
- [ ] Consistent

### Scenario 6: Multiple Repos
**Persona:** Developer with monorepo  
**Preconditions:** Nested repos  
**Steps:**
1. File in parent
2. File in nested
3. Different hashes
4. Correct keys

**Verification Checklist:**
- [ ] Repos detected
- [ ] Correct hash each
- [ ] Keys different
- [ ] Both work

---

## Testing Requirements

### Unit Tests

| ID | Test Case | Validates |
|----|-----------|-----------|
| UT-044a-01 | Key with commit | FR-044a-01 |
| UT-044a-02 | Key with path | FR-044a-03 |
| UT-044a-03 | Path normalization | FR-044a-04 |
| UT-044a-04 | Determinism | FR-044a-07 |
| UT-044a-05 | Dirty detection | FR-044a-22 |
| UT-044a-06 | Dirty marker | FR-044a-23 |
| UT-044a-07 | Fallback to file hash | FR-044a-37 |
| UT-044a-08 | Timeout handling | FR-044a-42 |
| UT-044a-09 | Composite keys | FR-044a-51 |
| UT-044a-10 | Key length limit | FR-044a-12 |
| UT-044a-11 | Long path hashing | FR-044a-14 |
| UT-044a-12 | URL-safe | FR-044a-11 |
| UT-044a-13 | Version in key | FR-044a-58 |
| UT-044a-14 | Multiple repos | FR-044a-32 |
| UT-044a-15 | Submodule hash | FR-044a-30 |

### Integration Tests

| ID | Test Case | Validates |
|----|-----------|-----------|
| IT-044a-01 | Cache integration | Task 044 |
| IT-044a-02 | Git integration | Task 008 |
| IT-044a-03 | Real git repo | FR-044a-01 |
| IT-044a-04 | Commit change | E2E |
| IT-044a-05 | Dirty worktree | FR-044a-22 |
| IT-044a-06 | Non-git fallback | FR-044a-36 |
| IT-044a-07 | Cross-platform | NFR-044a-13 |
| IT-044a-08 | Performance | NFR-044a-01 |
| IT-044a-09 | Concurrent | NFR-044a-08 |
| IT-044a-10 | Large directory | NFR-044a-05 |
| IT-044a-11 | Logging | NFR-044a-21 |
| IT-044a-12 | Detached HEAD | FR-044a-28 |
| IT-044a-13 | Shallow clone | FR-044a-28 |
| IT-044a-14 | Unicode paths | NFR-044a-18 |
| IT-044a-15 | Long paths | NFR-044a-19 |

---

## Implementation Prompt

### File Structure

```
src/
├── Acode.Domain/
│   └── Cache/
│       └── CacheKeyParts.cs
├── Acode.Application/
│   └── Cache/
│       ├── ICacheKeyGenerator.cs
│       └── KeyGeneratorOptions.cs
├── Acode.Infrastructure/
│   └── Cache/
│       ├── CacheKeyGenerator.cs
│       ├── CommitHashProvider.cs
│       ├── FileHashProvider.cs
│       └── KeyBuilder.cs
```

### Key Format

```
Format: {version}::{operation}::{commit}::{path_hash}

Examples:
v1::index::abc123def456...::sha256_of_normalized_path
v1::search::abc123def456...-dirty::sha256_of_query_and_paths
v1::embed::abc123def456...::file_path_hash
```

### Key Generator

```csharp
public class CacheKeyGenerator : ICacheKeyGenerator
{
    public async Task<string> GenerateKeyAsync(
        string operation,
        string filePath,
        KeyGeneratorOptions? options = null,
        CancellationToken ct = default)
    {
        var version = "v1";
        var commit = await GetCommitHashAsync(filePath, ct);
        var pathHash = NormalizePath(filePath);
        
        var key = $"{version}::{operation}::{commit}::{pathHash}";
        
        if (key.Length > MaxKeyLength)
        {
            key = $"{version}::{operation}::{commit}::{Hash(pathHash)}";
        }
        
        return key;
    }
    
    private async Task<string> GetCommitHashAsync(string path, CancellationToken ct)
    {
        var repoRoot = FindRepoRoot(path);
        if (repoRoot == null)
            return await GetFileHashAsync(path, ct);
        
        var hash = await _commitCache.GetOrSetAsync(repoRoot, async () =>
        {
            var result = await _git.GetHeadCommitAsync(repoRoot, ct);
            var dirty = await _git.IsDirtyAsync(repoRoot, ct);
            return dirty ? $"{result}-dirty" : result;
        }, TimeSpan.FromSeconds(1));
        
        return hash;
    }
}
```

**End of Task 044.a Specification**
