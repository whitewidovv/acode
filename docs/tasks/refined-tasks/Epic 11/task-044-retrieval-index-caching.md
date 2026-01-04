# Task 044: Retrieval/Index Caching

**Priority:** P1 – High  
**Tier:** F – Foundation Layer  
**Complexity:** 8 (Fibonacci points)  
**Phase:** Phase 11 – Optimization  
**Dependencies:** Task 025 (Retrieval), Task 008 (Git Integration)  

---

## Description

Task 044 implements caching for retrieval operations and code indexing. Caching dramatically reduces redundant computation—when the codebase hasn't changed, there's no need to re-parse, re-embed, or re-index files. The cache stores expensive intermediate results and serves them on subsequent requests.

The caching layer sits between the retrieval system and the expensive computation layers (parsing, embedding, indexing). Before computing, it checks the cache. If a valid entry exists and the source hasn't changed, the cached result is returned. Otherwise, computation proceeds and results are cached.

### Business Value

Caching provides:
- Faster retrieval
- Reduced compute
- Better responsiveness
- Resource efficiency
- Improved UX

### Scope Boundaries

This task covers the core caching framework. Cache key generation with commit hashes is Task 044.a. Statistics and clear commands are Task 044.b. Hit/miss telemetry is Task 044.c.

### Integration Points

| Component | Interface | Data Flow | Notes |
|-----------|-----------|-----------|-------|
| Retrieval | Task 025 | Cache check | Consumer |
| Indexer | Task 025 | Cache store | Producer |
| Git | Task 008 | Commit hash | Invalidation |
| File System | Watch | Change detect | Invalidation |
| CLI | Commands | Stats/clear | Management |

### Failure Modes

| Failure | Detection | Recovery | User Impact |
|---------|-----------|----------|-------------|
| Cache miss | Lookup fail | Compute | Slower |
| Cache corrupt | Checksum | Evict | Recompute |
| Cache full | Size check | Evict LRU | OK |
| Stale entry | Hash mismatch | Evict | Recompute |
| Storage fail | I/O error | Skip cache | Slower |
| Memory pressure | OOM | Evict | Reduced cache |

### Assumptions

1. **Computation expensive**: Worth caching
2. **Content-addressable**: Hash-based
3. **Invalidation clear**: Change detectable
4. **Storage available**: Disk space
5. **Cache beneficial**: High hit rate

### Security Considerations

1. **Cache location safe**: Protected
2. **No secrets cached**: Or encrypted
3. **Clear fully clears**: No remnants
4. **Concurrent safe**: No corruption
5. **User isolation**: If multi-user

---

## Glossary / Terms

| Term | Definition |
|------|------------|
| Cache | Stored results |
| Hit | Found in cache |
| Miss | Not in cache |
| Eviction | Remove entry |
| LRU | Least recently used |
| Invalidation | Mark stale |
| Key | Cache lookup ID |
| Value | Cached data |
| TTL | Time to live |
| Warm | Pre-populate |

---

## Out of Scope

- Distributed caching
- Cache replication
- External cache services (Redis)
- Cache compression (separate concern)
- Predictive pre-caching
- Cache sharing between users

---

## Functional Requirements

### FR-001 to FR-015: Cache Core

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-044-01 | Cache MUST store results | P0 |
| FR-044-02 | Cache MUST return on hit | P0 |
| FR-044-03 | Cache MUST allow miss | P0 |
| FR-044-04 | Cache MUST be generic | P0 |
| FR-044-05 | Cache MUST use keys | P0 |
| FR-044-06 | Keys MUST be unique | P0 |
| FR-044-07 | Values MUST be serializable | P0 |
| FR-044-08 | Cache MUST be persistent | P0 |
| FR-044-09 | Persistence MUST be SQLite | P0 |
| FR-044-10 | Memory cache MUST exist | P0 |
| FR-044-11 | Two-tier MUST work | P0 |
| FR-044-12 | Memory first, disk second | P0 |
| FR-044-13 | Write-through MUST work | P0 |
| FR-044-14 | Cache MUST be async | P0 |
| FR-044-15 | Cache MUST be thread-safe | P0 |

### FR-016 to FR-035: Cache Operations

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-044-16 | Get MUST work | P0 |
| FR-044-17 | Set MUST work | P0 |
| FR-044-18 | Delete MUST work | P0 |
| FR-044-19 | Exists MUST work | P0 |
| FR-044-20 | GetOrSet MUST work | P0 |
| FR-044-21 | GetOrSet MUST be atomic | P0 |
| FR-044-22 | Batch get MUST work | P1 |
| FR-044-23 | Batch set MUST work | P1 |
| FR-044-24 | Clear all MUST work | P0 |
| FR-044-25 | Clear by prefix MUST work | P1 |
| FR-044-26 | Expire MUST work | P0 |
| FR-044-27 | TTL MUST be supported | P0 |
| FR-044-28 | Default TTL MUST be 7 days | P0 |
| FR-044-29 | TTL MUST be configurable | P0 |
| FR-044-30 | Per-entry TTL MUST work | P1 |
| FR-044-31 | Sliding expiration MUST work | P2 |
| FR-044-32 | Absolute expiration MUST work | P0 |
| FR-044-33 | Count MUST be available | P0 |
| FR-044-34 | Size MUST be trackable | P0 |
| FR-044-35 | Keys MUST be listable | P1 |

### FR-036 to FR-055: Eviction

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-044-36 | Eviction policy MUST exist | P0 |
| FR-044-37 | LRU MUST be default | P0 |
| FR-044-38 | LRU MUST track access | P0 |
| FR-044-39 | Size limit MUST exist | P0 |
| FR-044-40 | Default size MUST be 1GB | P0 |
| FR-044-41 | Size MUST be configurable | P0 |
| FR-044-42 | Eviction at limit MUST work | P0 |
| FR-044-43 | Eviction MUST be automatic | P0 |
| FR-044-44 | Eviction MUST log | P0 |
| FR-044-45 | Count limit MUST exist | P1 |
| FR-044-46 | Default count MUST be 10000 | P1 |
| FR-044-47 | Count MUST be configurable | P1 |
| FR-044-48 | Eviction callback MUST exist | P2 |
| FR-044-49 | Priority eviction MUST work | P2 |
| FR-044-50 | Low priority first | P2 |
| FR-044-51 | Memory pressure eviction MUST work | P1 |
| FR-044-52 | Background cleanup MUST run | P0 |
| FR-044-53 | Cleanup interval MUST be configurable | P1 |
| FR-044-54 | Default cleanup MUST be 1 hour | P0 |
| FR-044-55 | Orphan cleanup MUST work | P0 |

### FR-056 to FR-070: Invalidation

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-044-56 | Invalidation MUST work | P0 |
| FR-044-57 | Hash-based invalidation MUST work | P0 |
| FR-044-58 | File change MUST invalidate | P0 |
| FR-044-59 | Commit change MUST invalidate | P0 |
| FR-044-60 | Dependency tracking MUST work | P1 |
| FR-044-61 | Cascade invalidation MUST work | P1 |
| FR-044-62 | Tag-based invalidation MUST work | P1 |
| FR-044-63 | Tag group clear MUST work | P1 |
| FR-044-64 | Invalidation MUST log | P0 |
| FR-044-65 | Invalidation MUST be fast | P0 |
| FR-044-66 | Bulk invalidation MUST work | P0 |
| FR-044-67 | Pattern invalidation MUST work | P1 |
| FR-044-68 | Version invalidation MUST work | P0 |
| FR-044-69 | Stale-while-revalidate MUST work | P2 |
| FR-044-70 | Warm after invalidate MUST option | P2 |

---

## Non-Functional Requirements

### Performance Requirements

| ID | Requirement | Target | Priority |
|----|-------------|--------|----------|
| NFR-044-01 | Memory hit | <1ms | P0 |
| NFR-044-02 | Disk hit | <10ms | P0 |
| NFR-044-03 | Miss detection | <5ms | P0 |
| NFR-044-04 | Set operation | <20ms | P0 |
| NFR-044-05 | Eviction | <50ms/entry | P0 |
| NFR-044-06 | Cleanup batch | <1s/1000 | P0 |
| NFR-044-07 | Memory overhead | <100MB | P0 |
| NFR-044-08 | Hit ratio | >80% typical | P1 |
| NFR-044-09 | Concurrent ops | 100+ | P0 |
| NFR-044-10 | Throughput | 1000 ops/s | P0 |

### Reliability Requirements

| ID | Requirement | Target | Priority |
|----|-------------|--------|----------|
| NFR-044-11 | Data integrity | 100% | P0 |
| NFR-044-12 | Thread safety | 100% | P0 |
| NFR-044-13 | Crash recovery | Consistent | P0 |
| NFR-044-14 | Corruption detect | Checksum | P0 |
| NFR-044-15 | Cross-platform | All OS | P0 |
| NFR-044-16 | Graceful degrade | Miss OK | P0 |
| NFR-044-17 | No deadlocks | Guaranteed | P0 |
| NFR-044-18 | Transaction safe | SQLite | P0 |
| NFR-044-19 | Atomic ops | Guaranteed | P0 |
| NFR-044-20 | Recovery auto | On startup | P0 |

### Observability Requirements

| ID | Requirement | Target | Priority |
|----|-------------|--------|----------|
| NFR-044-21 | Hit logged | Debug | P0 |
| NFR-044-22 | Miss logged | Debug | P0 |
| NFR-044-23 | Eviction logged | Info | P0 |
| NFR-044-24 | Error logged | Warning | P0 |
| NFR-044-25 | Metrics: hits | Counter | P0 |
| NFR-044-26 | Metrics: misses | Counter | P0 |
| NFR-044-27 | Metrics: size | Gauge | P0 |
| NFR-044-28 | Metrics: count | Gauge | P0 |
| NFR-044-29 | Structured logging | JSON | P0 |
| NFR-044-30 | Performance trace | Optional | P1 |

---

## Acceptance Criteria / Definition of Done

### Core
- [ ] AC-001: Cache stores results
- [ ] AC-002: Cache returns on hit
- [ ] AC-003: Cache allows miss
- [ ] AC-004: Generic cache works
- [ ] AC-005: Keys unique
- [ ] AC-006: Persistent
- [ ] AC-007: Two-tier works
- [ ] AC-008: Thread-safe

### Operations
- [ ] AC-009: Get works
- [ ] AC-010: Set works
- [ ] AC-011: Delete works
- [ ] AC-012: GetOrSet works
- [ ] AC-013: Clear all works
- [ ] AC-014: TTL works
- [ ] AC-015: Count available
- [ ] AC-016: Size trackable

### Eviction
- [ ] AC-017: LRU works
- [ ] AC-018: Size limit enforced
- [ ] AC-019: Auto eviction works
- [ ] AC-020: Cleanup runs
- [ ] AC-021: Logging works
- [ ] AC-022: Configurable
- [ ] AC-023: Memory pressure
- [ ] AC-024: Orphan cleanup

### Invalidation
- [ ] AC-025: Invalidation works
- [ ] AC-026: Hash-based works
- [ ] AC-027: File change works
- [ ] AC-028: Bulk works
- [ ] AC-029: Fast invalidation
- [ ] AC-030: Cross-platform
- [ ] AC-031: Tests pass
- [ ] AC-032: Documented

---

## User Verification Scenarios

### Scenario 1: Cache Hit
**Persona:** Developer searching code  
**Preconditions:** Previous search cached  
**Steps:**
1. Search codebase
2. Results cached
3. Search again
4. Instant return

**Verification Checklist:**
- [ ] First search works
- [ ] Results cached
- [ ] Second instant
- [ ] Same results

### Scenario 2: Cache Invalidation
**Persona:** Developer after edit  
**Preconditions:** File cached  
**Steps:**
1. Edit file
2. Cache invalidated
3. Search again
4. Fresh results

**Verification Checklist:**
- [ ] Edit detected
- [ ] Cache cleared
- [ ] Fresh compute
- [ ] Correct results

### Scenario 3: Size Limit
**Persona:** Developer with large codebase  
**Preconditions:** Cache near limit  
**Steps:**
1. Cache fills
2. Limit reached
3. LRU eviction
4. New entries work

**Verification Checklist:**
- [ ] Limit respected
- [ ] LRU evicted
- [ ] New entries work
- [ ] No errors

### Scenario 4: Two-Tier
**Persona:** Developer restarting  
**Preconditions:** Cache populated  
**Steps:**
1. Restart app
2. Memory cleared
3. Search
4. Disk hit

**Verification Checklist:**
- [ ] Memory cleared
- [ ] Disk persisted
- [ ] Hit from disk
- [ ] Loaded to memory

### Scenario 5: Concurrent Access
**Persona:** Developer with parallel ops  
**Preconditions:** Multiple operations  
**Steps:**
1. Parallel searches
2. Cache accessed
3. No corruption
4. All complete

**Verification Checklist:**
- [ ] Parallel works
- [ ] No corruption
- [ ] Thread-safe
- [ ] All succeed

### Scenario 6: Corruption Recovery
**Persona:** Developer after crash  
**Preconditions:** Corrupted entry  
**Steps:**
1. Entry corrupt
2. Access attempt
3. Detected
4. Re-compute

**Verification Checklist:**
- [ ] Corruption detected
- [ ] Entry evicted
- [ ] Fresh compute
- [ ] No error

---

## Testing Requirements

### Unit Tests

| ID | Test Case | Validates |
|----|-----------|-----------|
| UT-044-01 | Get operation | FR-044-16 |
| UT-044-02 | Set operation | FR-044-17 |
| UT-044-03 | Delete operation | FR-044-18 |
| UT-044-04 | GetOrSet | FR-044-20 |
| UT-044-05 | TTL expiration | FR-044-27 |
| UT-044-06 | LRU tracking | FR-044-38 |
| UT-044-07 | Size limit | FR-044-39 |
| UT-044-08 | Eviction | FR-044-42 |
| UT-044-09 | Invalidation | FR-044-56 |
| UT-044-10 | Two-tier | FR-044-11 |
| UT-044-11 | Thread safety | FR-044-15 |
| UT-044-12 | Serialization | FR-044-07 |
| UT-044-13 | Key generation | FR-044-05 |
| UT-044-14 | Batch ops | FR-044-22 |
| UT-044-15 | Clear | FR-044-24 |

### Integration Tests

| ID | Test Case | Validates |
|----|-----------|-----------|
| IT-044-01 | Retrieval integration | Task 025 |
| IT-044-02 | Git integration | Task 008 |
| IT-044-03 | Persistence | FR-044-08 |
| IT-044-04 | Restart recovery | NFR-044-13 |
| IT-044-05 | Large cache | NFR-044-07 |
| IT-044-06 | Concurrent | NFR-044-09 |
| IT-044-07 | Cross-platform | NFR-044-15 |
| IT-044-08 | Performance | NFR-044-01 |
| IT-044-09 | Cleanup | FR-044-52 |
| IT-044-10 | Corruption | NFR-044-14 |
| IT-044-11 | Memory pressure | FR-044-51 |
| IT-044-12 | Logging | NFR-044-21 |
| IT-044-13 | Metrics | NFR-044-25 |
| IT-044-14 | Tag invalidation | FR-044-62 |
| IT-044-15 | Dependency cascade | FR-044-61 |

---

## Implementation Prompt

### File Structure

```
src/
├── Acode.Domain/
│   └── Cache/
│       ├── CacheEntry.cs
│       ├── CacheKey.cs
│       └── EvictionPolicy.cs
├── Acode.Application/
│   └── Cache/
│       ├── ICache.cs
│       ├── ICacheInvalidator.cs
│       └── CacheOptions.cs
├── Acode.Infrastructure/
│   └── Cache/
│       ├── MemoryCache.cs
│       ├── SqliteCache.cs
│       ├── TwoTierCache.cs
│       ├── LruTracker.cs
│       └── CacheCleanup.cs
```

### Core Interface

```csharp
public interface ICache<TKey, TValue>
{
    Task<TValue?> GetAsync(TKey key, CancellationToken ct = default);
    Task SetAsync(TKey key, TValue value, CacheOptions? options = null, CancellationToken ct = default);
    Task<bool> DeleteAsync(TKey key, CancellationToken ct = default);
    Task<TValue> GetOrSetAsync(TKey key, Func<Task<TValue>> factory, CacheOptions? options = null, CancellationToken ct = default);
    Task ClearAsync(CancellationToken ct = default);
    Task InvalidateAsync(Func<TKey, bool> predicate, CancellationToken ct = default);
    Task<CacheStats> GetStatsAsync(CancellationToken ct = default);
}
```

**End of Task 044 Specification**
