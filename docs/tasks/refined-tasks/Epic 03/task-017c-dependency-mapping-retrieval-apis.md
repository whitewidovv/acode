# Task 017.c: Dependency Mapping + Retrieval APIs

**Priority:** P0 – Critical  
**Tier:** S – Core Infrastructure  
**Complexity:** 8 (Fibonacci points)  
**Phase:** Phase 3 – Intelligence Layer  
**Dependencies:** Task 017 (Symbol Index), Task 017.a (C# Extraction), Task 017.b (TS/JS Extraction)  

---

## Description

### Business Value

Dependency mapping transforms isolated symbol data into an interconnected knowledge graph that reveals how code components relate to each other. Without dependency information, the agent can identify that `UserService` exists but cannot determine what calls it, what it depends on, or what would break if it were modified. This task implements the relationship layer that powers impact analysis, usage discovery, and intelligent context selection.

When a developer asks the agent to refactor a method, the dependency graph immediately identifies all callers that must be reviewed or updated. When the context packer selects code for a prompt, it can include relevant dependencies to provide complete context. When the agent modifies a class, it can warn about breaking changes based on what depends on the public interface. These capabilities are impossible without persistent, queryable dependency relationships.

The dependency graph persists across sessions and updates incrementally as code changes. When a file is modified, only its dependencies are re-extracted—the rest of the graph remains intact. This incremental approach enables near-instant queries even on large codebases, supporting the interactive response times users expect from a coding assistant.

### Scope

This task delivers the following components:

1. **IDependency/Dependency** - Domain model representing a directional relationship between two symbols
2. **DependencyKind** - Enumeration of relationship types (calls, uses, inherits, implements, references)
3. **IDependencyStore/DependencyStore** - Persistence layer for storing and querying dependencies
4. **DependencyExtractor** - Extracts dependencies from parsed symbols (integrates with Task 017.a and 017.b)
5. **IDependencyGraph/DependencyGraph** - Graph operations including transitive queries, cycle detection, and path finding
6. **DependencyQueryService** - High-level retrieval APIs for common query patterns

### Integration Points

| Component | Integration Type | Description |
|-----------|------------------|-------------|
| C# Symbol Extractor (Task 017.a) | Producer | Provides symbols from which dependencies are extracted |
| TS/JS Symbol Extractor (Task 017.b) | Producer | Provides symbols from which dependencies are extracted |
| Symbol Index (Task 017) | Dependency | Resolves symbol IDs for dependency endpoints |
| SQLite Database | Persistence | Stores dependency edges with indexes for fast queries |
| Context Packer | Consumer | Queries dependencies to include related code in prompts |
| CLI Commands | Consumer | Exposes dependency queries via `acode deps` commands |
| File Watcher | Trigger | Triggers incremental updates when source files change |

### Failure Modes

| Failure | Impact | Mitigation |
|---------|--------|------------|
| Symbol not found in index | Dependency endpoint unresolved | Log warning, store with unresolved flag, retry on index update |
| Circular dependency in query | Infinite loop potential | Cycle detection with visited set, max depth limits |
| Database corruption | Data loss | Transaction-based writes, backup before rebuild |
| Memory exhaustion on large graph | Query failure or crash | Streaming iterators, pagination, memory bounds |
| Stale dependencies after file delete | Phantom edges remain | File deletion triggers edge cleanup |
| Concurrent write conflicts | Inconsistent state | Write serialization per file, read-write locks |

### Assumptions

1. Symbol IDs from Task 017.a and 017.b are stable and deterministic
2. Dependencies are extracted after symbols are indexed (ordered pipeline)
3. Cross-language dependencies (C# → JS) are out of scope for this task
4. Dependency resolution is best-effort—unresolved references are logged but not blocking
5. The SQLite database has sufficient capacity for millions of edges
6. Transitive query depth is configurable with a sensible default (10 levels)
7. Cycle detection marks cycles rather than attempting to break them
8. Dependency extraction runs in the same process as symbol extraction

### Security Considerations

1. **Query Limits** - All traversal queries MUST have configurable depth and node limits to prevent DoS
2. **Input Validation** - Symbol IDs MUST be validated before database queries
3. **No Code Execution** - Dependency extraction is pure data analysis with no code execution
4. **Path Exposure** - File paths in dependencies MUST be relative to project root
5. **Audit Trail** - Dependency graph modifications SHOULD be logged for debugging

### Technical Approach

The dependency mapping system follows a three-phase architecture: **extraction**, **storage**, and **query**. Each phase is designed for incremental operation, enabling real-time updates without full graph rebuilds.

```
┌─────────────────────────────────────────────────────────────────────────────────────┐
│                           DEPENDENCY MAPPING ARCHITECTURE                            │
├─────────────────────────────────────────────────────────────────────────────────────┤
│                                                                                     │
│  ┌────────────────┐    ┌────────────────┐    ┌────────────────┐                     │
│  │  C# Extractor  │    │  TS/JS Extract │    │  File Watcher  │                     │
│  │  (Task 017.a)  │    │  (Task 017.b)  │    │   (Trigger)    │                     │
│  └───────┬────────┘    └───────┬────────┘    └───────┬────────┘                     │
│          │                     │                     │                               │
│          ▼                     ▼                     ▼                               │
│  ┌─────────────────────────────────────────────────────────────────┐                │
│  │                    DEPENDENCY EXTRACTOR                         │                │
│  │  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐             │                │
│  │  │ Call Parser │  │ Type Parser │  │ Inherit     │             │                │
│  │  │             │  │             │  │ Parser      │             │                │
│  │  └──────┬──────┘  └──────┬──────┘  └──────┬──────┘             │                │
│  └─────────┼────────────────┼────────────────┼─────────────────────┘                │
│            │                │                │                                       │
│            ▼                ▼                ▼                                       │
│  ┌─────────────────────────────────────────────────────────────────┐                │
│  │                      DEPENDENCY STORE                           │                │
│  │  ┌──────────────────────────────────────────────────────────┐  │                │
│  │  │  SQLite Database                                          │  │                │
│  │  │  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐       │  │                │
│  │  │  │ edges table │  │ idx_source  │  │ idx_target  │       │  │                │
│  │  │  │ (source,    │  │ (fast deps) │  │ (fast deps) │       │  │                │
│  │  │  │  target,    │  └─────────────┘  └─────────────┘       │  │                │
│  │  │  │  kind)      │  ┌─────────────┐  ┌─────────────┐       │  │                │
│  │  │  └─────────────┘  │ idx_kind    │  │ idx_file    │       │  │                │
│  │  │                   └─────────────┘  └─────────────┘       │  │                │
│  │  └──────────────────────────────────────────────────────────┘  │                │
│  └─────────────────────────────────────────────────────────────────┘                │
│                                      │                                               │
│                                      ▼                                               │
│  ┌─────────────────────────────────────────────────────────────────┐                │
│  │                      DEPENDENCY GRAPH                           │                │
│  │  ┌────────────┐  ┌────────────┐  ┌────────────┐  ┌───────────┐ │                │
│  │  │  Direct    │  │ Transitive │  │   Cycle    │  │   Path    │ │                │
│  │  │  Query     │  │  Traversal │  │ Detection  │  │  Finder   │ │                │
│  │  └────────────┘  └────────────┘  └────────────┘  └───────────┘ │                │
│  └─────────────────────────────────────────────────────────────────┘                │
│                                      │                                               │
│                                      ▼                                               │
│  ┌─────────────────────────────────────────────────────────────────┐                │
│  │                    QUERY SERVICE / CLI                          │                │
│  │  GetDependencies() │ GetDependents() │ GetCallGraph()           │                │
│  │  GetUsages()       │ FindPath()      │ GetImplementors()        │                │
│  └─────────────────────────────────────────────────────────────────┘                │
│                                                                                     │
└─────────────────────────────────────────────────────────────────────────────────────┘
```

#### Phase 1: Dependency Extraction

The `DependencyExtractor` class receives parsed symbols from Task 017.a (C#) and Task 017.b (TS/JS). For each symbol, it analyzes the source code to identify relationships:

1. **Call Analysis**: Method invocations create `DependencyKind.Calls` edges
2. **Type Analysis**: Type references in declarations create `DependencyKind.Uses` edges
3. **Inheritance Analysis**: Base class references create `DependencyKind.Inherits` edges
4. **Implementation Analysis**: Interface implementations create `DependencyKind.Implements` edges
5. **General References**: All other symbol usages create `DependencyKind.References` edges

#### Phase 2: Persistent Storage

The `DependencyStore` persists edges to a SQLite database with optimized indexes for graph traversal:

```sql
CREATE TABLE dependency_edges (
    id TEXT PRIMARY KEY,
    source_symbol_id TEXT NOT NULL,
    target_symbol_id TEXT NOT NULL,
    kind INTEGER NOT NULL,
    file_path TEXT NOT NULL,
    line_number INTEGER,
    column_number INTEGER,
    created_at TEXT NOT NULL,
    updated_at TEXT NOT NULL
);

CREATE INDEX idx_edges_source ON dependency_edges(source_symbol_id);
CREATE INDEX idx_edges_target ON dependency_edges(target_symbol_id);
CREATE INDEX idx_edges_kind ON dependency_edges(kind);
CREATE INDEX idx_edges_file ON dependency_edges(file_path);
CREATE INDEX idx_edges_source_kind ON dependency_edges(source_symbol_id, kind);
CREATE INDEX idx_edges_target_kind ON dependency_edges(target_symbol_id, kind);
```

#### Phase 3: Graph Query

The `DependencyGraph` class provides efficient graph traversal algorithms:

- **Direct Queries**: Simple index lookups for immediate neighbors
- **Transitive Queries**: Breadth-first traversal with depth limits
- **Cycle Detection**: DFS with visited set tracking
- **Path Finding**: BFS for shortest path between two symbols

### ROI Analysis

**Development Time Savings:**

| Task Without Dependency Graph | Time | With Dependency Graph | Time Saved |
|------------------------------|------|----------------------|------------|
| Find all callers of a method | 15 min (grep + manual) | 5 sec (query) | 14:55 |
| Impact analysis before refactor | 30 min | 10 sec | 29:50 |
| Understand new codebase | 2 hours | 15 min | 1:45:00 |
| Debug call flow | 20 min | 30 sec | 19:30 |
| Identify unused code | 45 min | 1 min | 44:00 |

**Weekly developer time saved:** ~2.5 hours/developer

**ROI Calculation:**
- 10 developers × 2.5 hours/week × $75/hour = **$1,875/week**
- Annual savings: **$97,500**
- Implementation cost: ~80 hours × $100/hour = $8,000
- **ROI: 1,118% in first year**

### Trade-offs and Alternative Approaches

| Approach | Pros | Cons | Decision |
|----------|------|------|----------|
| **In-memory graph** | Fast queries, simple | Memory limits, no persistence | Rejected |
| **SQLite with indexes** | Persistent, scalable, proven | Query overhead, disk I/O | **Selected** |
| **Graph database (Neo4j)** | Native graph ops, Cypher | External dependency, complex ops | Rejected |
| **File-based storage** | Simple, portable | Slow queries, no indexing | Rejected |

**Rationale for SQLite:**
1. Already used by Symbol Index - no new dependencies
2. Proven performance at millions of edges
3. ACID transactions for consistency
4. Embedded - no external services
5. Cross-platform compatibility

### Constraints and Limitations

1. **Single-Language Scope**: This task handles dependencies within a single language. Cross-language dependencies (e.g., C# calling a TypeScript API) are tracked only if explicitly configured.

2. **Static Analysis Only**: Dependencies are determined through static code analysis. Runtime-dynamic dependencies (reflection, dynamic dispatch) are not captured.

3. **Query Depth Bounds**: Transitive queries have a maximum depth limit (default 10) to prevent resource exhaustion.

4. **Memory Constraints**: Large graph traversals use streaming to avoid loading millions of edges into memory.

5. **No Semantic Inference**: The system tracks explicit code references only. Implied relationships (e.g., convention-based DI) require explicit configuration.

### Performance Targets

| Operation | P50 Target | P95 Target | Maximum |
|-----------|-----------|-----------|---------|
| Direct dependency query | 2ms | 5ms | 10ms |
| Direct dependent query | 2ms | 5ms | 10ms |
| Transitive query (depth 3) | 20ms | 50ms | 100ms |
| Transitive query (depth 5) | 50ms | 100ms | 200ms |
| Path finding (2 nodes) | 10ms | 30ms | 100ms |
| Batch insert (1000 edges) | 50ms | 100ms | 200ms |
| Full graph rebuild (10k edges) | 1s | 2s | 5s |
| Full graph rebuild (100k edges) | 10s | 20s | 60s |

### Incremental Update Protocol

```
┌─────────────────────────────────────────────────────────────────┐
│                    INCREMENTAL UPDATE FLOW                       │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│  1. File Changed                                                │
│     │                                                           │
│     ▼                                                           │
│  2. Symbol Extractor re-parses file                             │
│     │                                                           │
│     ▼                                                           │
│  3. DELETE FROM dependency_edges WHERE file_path = ?            │
│     │                                                           │
│     ▼                                                           │
│  4. Dependency Extractor analyzes new symbols                   │
│     │                                                           │
│     ▼                                                           │
│  5. INSERT new edges for file                                   │
│     │                                                           │
│     ▼                                                           │
│  6. Graph cache invalidated for affected symbols                │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

### Comparison with Industry Solutions

| Feature | Acode (this task) | IntelliSense | Language Server | Sourcegraph |
|---------|-------------------|--------------|-----------------|-------------|
| Cross-file deps | ✅ | ✅ | ✅ | ✅ |
| Transitive queries | ✅ | ❌ | ❌ | ✅ |
| Cycle detection | ✅ | ❌ | ❌ | ❌ |
| Path finding | ✅ | ❌ | ❌ | ❌ |
| Persistent | ✅ | ❌ | ❌ | ✅ |
| Incremental update | ✅ | ✅ | ✅ | ✅ |
| AI-optimized context | ✅ | ❌ | ❌ | ❌ |

---

## Use Cases

### Use Case 1: Developer Analyzes Impact Before Refactoring

**Persona:** Sarah Chen, Senior .NET Developer, working on a large e-commerce platform.

**Context:** Sarah needs to rename a method `ProcessOrder` to `ExecuteOrderProcessing` and wants to know all the places that call this method before making the change.

**Before Dependency Mapping:**
```
Sarah's Workflow:
1. Open Visual Studio Find in Files dialog
2. Search for "ProcessOrder" across solution
3. Get 847 results (many false positives - comments, strings, similar names)
4. Manually review each result (45 minutes)
5. Create a spreadsheet of actual call sites
6. Miss 3 calls in dynamically loaded assemblies
7. Deploy, break production, rollback
8. Spend 4 hours debugging

Total time: 5+ hours with production incident
```

**After Dependency Mapping:**
```
Sarah's Workflow:
$ acode deps on OrderService.ProcessOrder

Dependents of OrderService.ProcessOrder (15 total):
  Direct Callers:
    - OrderController.Create [src/Controllers/OrderController.cs:45]
    - OrderController.Update [src/Controllers/OrderController.cs:78]
    - CheckoutService.Complete [src/Services/CheckoutService.cs:112]
    - OrderBatchProcessor.ProcessBatch [src/Jobs/OrderBatchProcessor.cs:34]
    ...

  Transitive Callers (depth 2):
    - CheckoutController.Checkout → CheckoutService.Complete → ProcessOrder
    - ScheduledJobs.NightlyBatch → OrderBatchProcessor.ProcessBatch → ProcessOrder

1. Review 15 precise results (2 minutes)
2. Rename with confidence
3. Deploy successfully

Total time: 5 minutes with zero incidents
```

**Improvement:** 98% time reduction, eliminated production incidents

---

### Use Case 2: New Developer Understands Codebase Architecture

**Persona:** Marcus Rivera, Junior Developer, first week on the team.

**Context:** Marcus needs to understand how the authentication system works and what components depend on it.

**Before Dependency Mapping:**
```
Marcus's Workflow:
1. Ask senior developer for overview (interrupts colleague)
2. Search for "Auth" in codebase - 1,200 results
3. Open random files trying to find entry points
4. Create mental model that's incomplete
5. Make changes that break authorization checks
6. Code review catches issues (2 days later)
7. Rewrite the feature

Total time: 3 days of confusion plus 1 day rework
```

**After Dependency Mapping:**
```
Marcus's Workflow:
$ acode deps of AuthenticationService

Dependencies of AuthenticationService:
  Uses:
    - IUserRepository (data access)
    - ITokenService (JWT handling)
    - IPasswordHasher (security)
    - AuthenticationOptions (config)

$ acode deps on AuthenticationService

Dependents of AuthenticationService:
  Direct:
    - AuthController (API endpoints)
    - JwtMiddleware (request pipeline)
    - AuthorizationHandler (policy checks)

$ acode deps calls AuthenticationService.ValidateCredentials --depth 3

Call Graph:
AuthController.Login
└── AuthenticationService.ValidateCredentials
    ├── UserRepository.GetByEmail
    │   └── DbContext.Users.FirstOrDefault
    └── PasswordHasher.Verify
        └── BCrypt.Verify

1. Understand complete architecture (10 minutes)
2. Identify correct integration points
3. Implement feature correctly first time

Total time: 2 hours including implementation
```

**Improvement:** 4 days reduced to 2 hours (96% faster onboarding)

---

### Use Case 3: AI Agent Selects Relevant Context for Code Generation

**Persona:** DevBot, Acode's AI coding assistant.

**Context:** User asks DevBot to "add error handling to the PaymentProcessor.ChargeCard method."

**Before Dependency Mapping:**
```
DevBot's Process:
1. Find PaymentProcessor.ChargeCard (easy)
2. Guess what related code might be needed
3. Include random files that mention "payment"
4. Miss the PaymentException class
5. Miss the IPaymentGateway interface
6. Generate code that doesn't match existing patterns
7. User has to manually fix integration

Quality: 60% useful response
```

**After Dependency Mapping:**
```
DevBot's Process:
$ (internal) acode deps of PaymentProcessor.ChargeCard

Dependencies:
  - IPaymentGateway.Process (interface method being called)
  - PaymentException (existing exception class)
  - PaymentResult (return type)
  - TransactionLog.Record (logging pattern)

$ (internal) acode deps on PaymentProcessor.ChargeCard

Callers:
  - CheckoutService.ProcessPayment (see how it handles errors today)
  - OrderService.RefundOrder (another error handling example)

1. Include all dependency interfaces in context
2. Include existing exception types
3. Include example of how callers handle errors
4. Generate code that perfectly matches patterns

Quality: 95% useful response, minimal edits needed
```

**Improvement:** 35% higher AI response accuracy

---

## Glossary / Terms

| Term | Definition |
|------|------------|
| Dependency | Symbol A uses Symbol B |
| Dependent | Symbol B is used by Symbol A |
| Edge | Graph connection between symbols |
| Node | Symbol in the dependency graph |
| Outgoing Edge | Dependency (uses) relationship |
| Incoming Edge | Dependent (used by) relationship |
| Relationship Type | Kind of dependency |
| Transitive | Indirect dependency chain |
| Cycle | Circular dependency |
| Call Graph | Function call relationships |
| Type Graph | Type reference relationships |
| Inheritance | Parent-child type relationship |
| Implementation | Interface-class relationship |
| Reference | Any usage of a symbol |

---

## Out of Scope

The following items are explicitly excluded from Task 017.c:

1. **Cross-language dependencies** - Dependencies between C# and TypeScript/JavaScript are not tracked; single-language analysis only
2. **Runtime dependencies** - Dynamic dispatch, reflection-based calls, and runtime-resolved types are not captured; static analysis only
3. **Package/NuGet dependencies** - External package dependencies are not tracked; source code relationships only
4. **Dependency injection analysis** - Convention-based DI container bindings require manual configuration; no automatic inference
5. **Graph visualization** - No graphical rendering of dependency graphs; data access APIs only
6. **Automatic refactoring** - No code modification capabilities; query and analysis only
7. **Impact analysis scoring** - No risk scoring or change impact metrics; raw dependency data only
8. **Build dependency ordering** - No build system integration or compilation order determination
9. **Dead code detection** - No automated identification of unused code paths; raw usage data only
10. **Semantic versioning analysis** - No breaking change detection based on dependency patterns

---

## Functional Requirements

### Dependency Model (FR-017c-01 to FR-017c-11)

| ID | Requirement |
|----|-------------|
| FR-017c-01 | System MUST define IDependency interface in Domain layer |
| FR-017c-02 | System MUST define DependencyKind enum with all relationship types |
| FR-017c-03 | DependencyKind MUST include Calls (method/function invocation) |
| FR-017c-04 | DependencyKind MUST include Uses (type reference) |
| FR-017c-05 | DependencyKind MUST include Inherits (class inheritance) |
| FR-017c-06 | DependencyKind MUST include Implements (interface implementation) |
| FR-017c-07 | DependencyKind MUST include References (any symbol usage) |
| FR-017c-08 | Dependency MUST store source symbol ID (caller/user) |
| FR-017c-09 | Dependency MUST store target symbol ID (callee/used) |
| FR-017c-10 | Dependency MUST store relationship kind |
| FR-017c-11 | Dependency MUST store source location (where the reference occurs) |

### Dependency Store (FR-017c-12 to FR-017c-22)

| ID | Requirement |
|----|-------------|
| FR-017c-12 | System MUST define IDependencyStore interface in Domain layer |
| FR-017c-13 | Store MUST support adding single dependencies |
| FR-017c-14 | Store MUST support removing single dependencies |
| FR-017c-15 | Store MUST support removing all dependencies by source symbol |
| FR-017c-16 | Store MUST support removing all dependencies by target symbol |
| FR-017c-17 | Store MUST support querying dependencies by source symbol |
| FR-017c-18 | Store MUST support querying dependencies by target symbol |
| FR-017c-19 | Store MUST support querying dependencies by kind |
| FR-017c-20 | Store MUST support batch add/remove operations for performance |
| FR-017c-21 | Store MUST persist dependencies to SQLite database |
| FR-017c-22 | Store MUST load dependencies from database on startup |

### Dependency Extraction (FR-017c-23 to FR-017c-30)

| ID | Requirement |
|----|-------------|
| FR-017c-23 | Extractor MUST identify method/function calls and create Calls dependencies |
| FR-017c-24 | Extractor MUST identify property accesses and create References dependencies |
| FR-017c-25 | Extractor MUST identify field accesses and create References dependencies |
| FR-017c-26 | Extractor MUST identify type references and create Uses dependencies |
| FR-017c-27 | Extractor MUST identify base class and create Inherits dependencies |
| FR-017c-28 | Extractor MUST identify interface implementations and create Implements dependencies |
| FR-017c-29 | Extractor MUST identify constructor calls and create Calls dependencies |
| FR-017c-30 | Extractor MUST link symbols by resolving names to symbol IDs |

### Graph Operations (FR-017c-31 to FR-017c-38)

| ID | Requirement |
|----|-------------|
| FR-017c-31 | Graph MUST support getting direct dependencies of a symbol |
| FR-017c-32 | Graph MUST support getting direct dependents of a symbol |
| FR-017c-33 | Graph MUST support getting transitive dependencies with depth limit |
| FR-017c-34 | Graph MUST support getting transitive dependents with depth limit |
| FR-017c-35 | Graph MUST detect cycles and mark them without infinite loops |
| FR-017c-36 | Graph MUST support finding shortest path between two symbols |
| FR-017c-37 | Graph MUST enforce configurable maximum depth for traversals |
| FR-017c-38 | Graph MUST support filtering traversal results by dependency kind |

### Retrieval APIs (FR-017c-39 to FR-017c-45)

| ID | Requirement |
|----|-------------|
| FR-017c-39 | API MUST provide GetDependencies(symbolId) returning direct dependencies |
| FR-017c-40 | API MUST provide GetDependents(symbolId) returning direct dependents |
| FR-017c-41 | API MUST provide GetCallGraph(symbolId, depth) returning call tree |
| FR-017c-42 | API MUST provide GetTypeHierarchy(symbolId) returning inheritance chain |
| FR-017c-43 | API MUST provide GetImplementors(interfaceId) returning implementing classes |
| FR-017c-44 | API MUST provide GetUsages(symbolId) returning all reference locations |
| FR-017c-45 | API MUST provide FindPath(fromId, toId) returning dependency path if exists |

### Update Management (FR-017c-46 to FR-017c-50)

| ID | Requirement |
|----|-------------|
| FR-017c-46 | System MUST update dependencies when source file changes |
| FR-017c-47 | System MUST remove stale edges when symbols are deleted |
| FR-017c-48 | System MUST add new edges when symbols are added |
| FR-017c-49 | System MUST support incremental updates (file-level granularity) |
| FR-017c-50 | System MUST support full rebuild of dependency graph |

### Index Management (FR-017c-51 to FR-017c-54)

| ID | Requirement |
|----|-------------|
| FR-017c-51 | Store MUST index source-target pairs for bidirectional queries |
| FR-017c-52 | Store MUST index dependencies by kind for filtered queries |
| FR-017c-53 | Store MUST index dependencies by file for incremental updates |
| FR-017c-54 | Store MUST optimize index structure for graph traversal patterns |

---

## Non-Functional Requirements

### Performance (NFR-017c-01 to NFR-017c-08)

| ID | Category | Requirement |
|----|----------|-------------|
| NFR-017c-01 | Performance | Direct dependency query MUST complete in < 10ms |
| NFR-017c-02 | Performance | Direct dependent query MUST complete in < 10ms |
| NFR-017c-03 | Performance | Transitive query (depth 5) MUST complete in < 100ms |
| NFR-017c-04 | Performance | Batch insert MUST achieve < 0.1ms per edge |
| NFR-017c-05 | Performance | System MUST handle graphs with 1 million edges |
| NFR-017c-06 | Performance | Index rebuild MUST NOT block read queries |
| NFR-017c-07 | Performance | Incremental update MUST be < 100ms per file |
| NFR-017c-08 | Performance | Path finding MUST use efficient graph algorithms (BFS/Dijkstra) |

### Reliability (NFR-017c-09 to NFR-017c-14)

| ID | Category | Requirement |
|----|----------|-------------|
| NFR-017c-09 | Reliability | System MUST handle cycles without infinite loops |
| NFR-017c-10 | Reliability | System MUST handle queries for missing symbols gracefully |
| NFR-017c-11 | Reliability | Database state MUST remain consistent after crashes |
| NFR-017c-12 | Reliability | Database transactions MUST be atomic for batch operations |
| NFR-017c-13 | Reliability | Concurrent read/write MUST NOT cause data corruption |
| NFR-017c-14 | Reliability | System MUST recover from database lock timeouts |

### Security (NFR-017c-15 to NFR-017c-18)

| ID | Category | Requirement |
|----|----------|-------------|
| NFR-017c-15 | Security | Symbol IDs MUST be validated before database queries |
| NFR-017c-16 | Security | Query depth limits MUST be enforced to prevent resource exhaustion |
| NFR-017c-17 | Security | File paths MUST be stored as relative paths only |
| NFR-017c-18 | Security | SQL injection MUST be prevented via parameterized queries |

### Maintainability (NFR-017c-19 to NFR-017c-22)

| ID | Category | Requirement |
|----|----------|-------------|
| NFR-017c-19 | Maintainability | Domain interfaces MUST be independent of persistence implementation |
| NFR-017c-20 | Maintainability | All public APIs MUST have XML documentation |
| NFR-017c-21 | Maintainability | Database schema MUST be versioned with migrations |
| NFR-017c-22 | Maintainability | Unit test coverage MUST exceed 80% |

### Observability (NFR-017c-23 to NFR-017c-26)

| ID | Category | Requirement |
|----|----------|-------------|
| NFR-017c-23 | Observability | Query execution time MUST be logged for performance monitoring |
| NFR-017c-24 | Observability | Edge count and graph statistics MUST be available via metrics |
| NFR-017c-25 | Observability | Cycle detection events MUST be logged with involved symbols |
| NFR-017c-26 | Observability | Database maintenance operations MUST be logged |

---

## User Manual Documentation

### Overview

The Dependency Mapping system tracks relationships between code symbols in your project. It answers questions like "what calls this method?", "what does this class depend on?", and "how is this interface implemented?". This document provides complete instructions for configuring, querying, and troubleshooting dependency analysis.

### Quick Start

```bash
# Step 1: Index your project (dependencies are extracted automatically)
acode index src/

# Step 2: Query dependencies of a symbol
acode deps of UserService

# Step 3: Query what depends on a symbol
acode deps on UserService

# Step 4: View call graph
acode deps calls UserService.GetById --depth 3
```

### System Architecture Overview

```
┌─────────────────────────────────────────────────────────────────────┐
│                    DEPENDENCY QUERY SYSTEM                          │
├─────────────────────────────────────────────────────────────────────┤
│                                                                     │
│   USER QUERY                                                        │
│       │                                                             │
│       ▼                                                             │
│   ┌───────────────┐                                                 │
│   │  CLI Parser   │  acode deps of|on|calls|path ...                │
│   └───────┬───────┘                                                 │
│           │                                                         │
│           ▼                                                         │
│   ┌───────────────────────────────────────────────────────────┐     │
│   │              DependencyQueryService                       │     │
│   │  ┌─────────────┐ ┌─────────────┐ ┌─────────────┐         │     │
│   │  │ GetDeps()   │ │ GetDepsOn() │ │ FindPath()  │         │     │
│   │  └──────┬──────┘ └──────┬──────┘ └──────┬──────┘         │     │
│   └─────────┼───────────────┼───────────────┼─────────────────┘     │
│             │               │               │                       │
│             ▼               ▼               ▼                       │
│   ┌───────────────────────────────────────────────────────────┐     │
│   │                   DependencyGraph                         │     │
│   │  Direct Query │ Transitive BFS │ Cycle Detection         │     │
│   └───────────────────────────┬───────────────────────────────┘     │
│                               │                                     │
│                               ▼                                     │
│   ┌───────────────────────────────────────────────────────────┐     │
│   │                   SQLite Database                         │     │
│   │  dependency_edges table with optimized indexes            │     │
│   └───────────────────────────────────────────────────────────┘     │
│                                                                     │
└─────────────────────────────────────────────────────────────────────┘
```

### Configuration Reference

#### Full Configuration Example

```yaml
# .agent/config.yml
symbol_index:
  dependencies:
    # Enable/disable dependency mapping
    enabled: true
    
    # Relationship types to track
    # Available: calls, uses, inherits, implements, references
    kinds:
      - calls
      - uses
      - inherits
      - implements
      - references
      
    # Transitive query limits
    transitive:
      # Maximum depth for transitive queries (default: 10)
      max_depth: 10
      
      # Maximum nodes to return in a single query (default: 1000)
      max_nodes: 1000
      
      # Timeout for transitive queries in milliseconds (default: 5000)
      timeout_ms: 5000
      
    # Performance tuning
    performance:
      # Number of edges to insert per batch (default: 1000)
      batch_size: 1000
      
      # Enable query result caching (default: true)
      enable_cache: true
      
      # Cache TTL in seconds (default: 300)
      cache_ttl_seconds: 300
      
    # Logging and diagnostics
    diagnostics:
      # Log slow queries (default: true)
      log_slow_queries: true
      
      # Slow query threshold in milliseconds (default: 100)
      slow_query_threshold_ms: 100
      
      # Log cycle detection warnings (default: true)
      log_cycles: true
```

#### Configuration Options Table

| Option | Type | Default | Description |
|--------|------|---------|-------------|
| `enabled` | bool | true | Enable/disable dependency tracking |
| `kinds` | string[] | all | Relationship types to extract |
| `transitive.max_depth` | int | 10 | Maximum traversal depth |
| `transitive.max_nodes` | int | 1000 | Maximum nodes per query |
| `transitive.timeout_ms` | int | 5000 | Query timeout |
| `performance.batch_size` | int | 1000 | Batch insert size |
| `performance.enable_cache` | bool | true | Enable query caching |
| `performance.cache_ttl_seconds` | int | 300 | Cache time-to-live |
| `diagnostics.log_slow_queries` | bool | true | Log slow queries |
| `diagnostics.slow_query_threshold_ms` | int | 100 | Slow query threshold |
| `diagnostics.log_cycles` | bool | true | Log cycle warnings |

### Dependency Kinds Reference

| Kind | Symbol | Description | Example Code |
|------|--------|-------------|--------------|
| Calls | → | Method/function invocation | `service.GetUser(id)` |
| Uses | ◇ | Type reference in declaration | `UserService service;` |
| Inherits | ▷ | Class inheritance | `class Admin : User` |
| Implements | ◁ | Interface implementation | `class Repo : IRepository` |
| References | ○ | Any other symbol reference | `user.Name`, `Config.Timeout` |

### CLI Command Reference

#### `acode deps of <symbol>`

Get all symbols that the specified symbol depends on (outgoing edges).

```bash
# Basic usage
acode deps of UserService

# Filter by kind
acode deps of UserService --kind calls

# Transitive dependencies
acode deps of UserService --depth 3

# Output as JSON
acode deps of UserService --format json
```

**Example Output:**
```
Dependencies of UserService (12 total):

Calls (5):
  → UserRepository.GetById        [src/Data/UserRepository.cs:45]
  → UserRepository.Save           [src/Data/UserRepository.cs:78]
  → Logger.LogInformation         [Microsoft.Extensions.Logging]
  → Logger.LogWarning             [Microsoft.Extensions.Logging]
  → Logger.LogError               [Microsoft.Extensions.Logging]

Uses (4):
  ◇ IUserRepository               [src/Interfaces/IUserRepository.cs:1]
  ◇ ILogger<UserService>          [Microsoft.Extensions.Logging]
  ◇ User                          [src/Models/User.cs:1]
  ◇ UserNotFoundException         [src/Exceptions/UserNotFoundException.cs:1]

Implements (1):
  ◁ IUserService                  [src/Interfaces/IUserService.cs:1]

Inherits (1):
  ▷ BaseService                   [src/Services/BaseService.cs:1]

References (1):
  ○ UserServiceOptions.Timeout    [src/Options/UserServiceOptions.cs:15]
```

#### `acode deps on <symbol>`

Get all symbols that depend on the specified symbol (incoming edges).

```bash
# Basic usage
acode deps on UserService

# Filter by kind
acode deps on UserService --kind calls

# Show file locations
acode deps on UserService --show-locations
```

**Example Output:**
```
Dependents of UserService (8 total):

Called By (4):
  ← UserController.GetUser        [src/Controllers/UserController.cs:34]
  ← UserController.CreateUser     [src/Controllers/UserController.cs:56]
  ← UserController.UpdateUser     [src/Controllers/UserController.cs:78]
  ← UserController.DeleteUser     [src/Controllers/UserController.cs:92]

Used By (3):
  ◇ UserController                [src/Controllers/UserController.cs:12]
  ◇ UserFacade                    [src/Facades/UserFacade.cs:8]
  ◇ UserServiceTests              [tests/UserServiceTests.cs:15]

Extended By (1):
  ▷ AdminUserService              [src/Services/AdminUserService.cs:1]
```

#### `acode deps calls <symbol>`

Display call graph starting from the specified method/function.

```bash
# Basic usage
acode deps calls UserController.GetUser

# Limit depth
acode deps calls UserController.GetUser --depth 2

# Include return types
acode deps calls UserController.GetUser --show-types
```

**Example Output:**
```
Call Graph for UserController.GetUser (depth: 3)

UserController.GetUser(Guid id) → Task<UserDto>
├── UserService.GetById(Guid id) → Task<User>
│   ├── UserRepository.GetById(Guid id) → Task<User?>
│   │   └── DbContext.Users.FindAsync(Guid id) → Task<User?>
│   └── Logger.LogDebug(string message, params object[] args)
├── Mapper.Map<UserDto>(User source) → UserDto
└── Logger.LogInformation(string message, params object[] args)

Total calls: 6 | Max depth: 3 | Cycles: 0
```

#### `acode deps path <from> <to>`

Find the shortest dependency path between two symbols.

```bash
# Basic usage
acode deps path UserController UserRepository

# Limit search depth
acode deps path UserController UserRepository --max-depth 5
```

**Example Output:**
```
Path from UserController to UserRepository (3 hops):

  UserController
       │
       │ calls UserService.GetById
       ▼
  UserService
       │
       │ calls UserRepository.GetById
       ▼
  UserRepository

Alternative paths: 2 (use --all to show)
```

### Programmatic API Usage

```csharp
using Acode.Application.Interfaces;
using Acode.Domain.Symbols;

public class RefactoringAssistant
{
    private readonly IDependencyQueryService _deps;
    
    public RefactoringAssistant(IDependencyQueryService deps)
    {
        _deps = deps;
    }
    
    // Get direct dependencies of a symbol
    public async Task AnalyzeDependenciesAsync(Guid symbolId)
    {
        var deps = await _deps.GetDependenciesAsync(symbolId);
        
        foreach (var dep in deps)
        {
            Console.WriteLine($"{dep.Kind}: {dep.TargetSymbolId}");
        }
    }
    
    // Get all callers of a method (for refactoring impact)
    public async Task<IReadOnlyList<Guid>> GetCallersAsync(Guid methodId)
    {
        var dependents = await _deps.GetDependentsAsync(
            methodId, 
            DependencyKind.Calls);
            
        return dependents.Select(d => d.SourceSymbolId).ToList();
    }
    
    // Find if two symbols are connected
    public async Task<bool> AreConnectedAsync(Guid from, Guid to)
    {
        var path = await _deps.FindPathAsync(from, to, maxDepth: 10);
        return path != null && path.Count > 0;
    }
    
    // Get full call tree for documentation
    public async Task<CallTree> GetCallTreeAsync(Guid methodId, int depth = 3)
    {
        var tree = await _deps.GetTransitiveDependenciesAsync(
            methodId,
            maxDepth: depth,
            kinds: new[] { DependencyKind.Calls });
            
        return BuildTree(methodId, tree);
    }
}
```

### Frequently Asked Questions

**Q: How often is the dependency graph updated?**
A: Dependencies are updated incrementally whenever the symbol index is refreshed. By default, this happens on file save or when `acode index` is run.

**Q: Why are some dependencies showing as "unresolved"?**
A: Unresolved dependencies occur when the target symbol cannot be found in the index. This often happens with external NuGet packages or when files haven't been indexed yet. Run `acode index --rebuild` to ensure all symbols are indexed.

**Q: How do I find circular dependencies?**
A: Use `acode deps cycles` to find all cycles in the dependency graph. Cycles are logged as warnings during normal queries but don't block results.

**Q: Can I query dependencies for external libraries?**
A: No, dependency tracking only covers source files in your indexed workspace. External library calls are recorded but the target symbols aren't expanded.

**Q: What's the performance impact of enabling all dependency kinds?**
A: Minimal. All kinds are extracted in a single pass. Disabling kinds only reduces storage size and query result set, not extraction time.

**Q: How do I reset the dependency graph?**
A: Run `acode index --rebuild --include-deps` to completely rebuild the dependency graph from scratch.

### Troubleshooting Guide

#### Issue: Missing Dependencies

**Symptoms:**
- Expected dependencies not appearing in query results
- Symbol shows 0 dependencies when it clearly uses other code
- Partial results with some relationships missing

**Causes:**
1. Symbol extraction for source file failed
2. Target symbol not indexed
3. Dependency kind not configured
4. Query filter excluding results

**Solutions:**

1. Verify symbol extraction completed:
```bash
acode symbols show MyClass
# Should show symbol with metadata
```

2. Rebuild dependency graph:
```bash
acode index --rebuild --include-deps
```

3. Check dependency kinds configuration:
```yaml
symbol_index:
  dependencies:
    kinds:
      - calls      # Ensure needed kinds included
      - uses
      - inherits
      - implements
      - references
```

4. Remove query filters:
```bash
# Instead of filtered query
acode deps of MyClass --kind calls

# Try unfiltered
acode deps of MyClass
```

---

#### Issue: Slow Transitive Queries

**Symptoms:**
- Queries with depth > 3 take more than 5 seconds
- CLI appears to hang during dependency queries
- Memory usage spikes during queries

**Causes:**
1. Query depth too high for graph density
2. No cache warming
3. Large result sets

**Solutions:**

1. Reduce query depth:
```bash
acode deps of MyClass --depth 2  # Instead of depth 5
```

2. Enable and warm cache:
```yaml
symbol_index:
  dependencies:
    performance:
      enable_cache: true
      cache_ttl_seconds: 600
```

3. Add node limits:
```yaml
symbol_index:
  dependencies:
    transitive:
      max_nodes: 500  # Limit result size
```

---

#### Issue: Circular Dependency Warnings

**Symptoms:**
- Warning messages about cycles in logs
- Query results contain duplicate symbols
- Infinite loop concerns

**Causes:**
1. Actual circular dependencies in code
2. Normal for some patterns (e.g., event handlers)

**Solutions:**

1. Review and refactor circular dependencies (code quality issue)
2. Queries still return correct results—cycles are marked but don't cause infinite loops
3. Use cycle detection to find all cycles:
```bash
acode deps cycles
```

---

## Acceptance Criteria

### Dependency Model (AC-017c-001 to AC-017c-010)

- [ ] AC-017c-001: `IDependency` interface is defined in `Acode.Domain.Symbols` namespace
- [ ] AC-017c-002: `Dependency` class implements `IDependency` with all required properties
- [ ] AC-017c-003: `Id` property returns unique GUID for each dependency edge
- [ ] AC-017c-004: `SourceSymbolId` property returns GUID of the source symbol
- [ ] AC-017c-005: `TargetSymbolId` property returns GUID of the target symbol
- [ ] AC-017c-006: `Kind` property returns `DependencyKind` enum value
- [ ] AC-017c-007: `Location` property returns `SymbolLocation` with file path and position
- [ ] AC-017c-008: `DependencyKind` enum includes `Calls`, `Uses`, `Inherits`, `Implements`, `References`
- [ ] AC-017c-009: Dependency equality is based on source, target, and kind (not ID)
- [ ] AC-017c-010: Dependency implements proper `GetHashCode()` for use in collections

### Dependency Store (AC-017c-011 to AC-017c-020)

- [ ] AC-017c-011: `IDependencyStore` interface is defined in `Acode.Domain.Symbols` namespace
- [ ] AC-017c-012: `DependencyStore` class implements `IDependencyStore` in Infrastructure layer
- [ ] AC-017c-013: `AddAsync()` method persists a single dependency edge to SQLite
- [ ] AC-017c-014: `AddBatchAsync()` method persists 1000+ edges in under 200ms
- [ ] AC-017c-015: `RemoveAsync()` method removes a dependency by ID
- [ ] AC-017c-016: `RemoveBySourceAsync()` method removes all dependencies from a source symbol
- [ ] AC-017c-017: `RemoveByTargetAsync()` method removes all dependencies to a target symbol
- [ ] AC-017c-018: `RemoveByFileAsync()` method removes all dependencies from a source file
- [ ] AC-017c-019: Database schema includes all required indexes for performance
- [ ] AC-017c-020: Store handles concurrent read/write operations without corruption

### Query Operations (AC-017c-021 to AC-017c-030)

- [ ] AC-017c-021: `GetBySourceAsync()` returns all dependencies where given symbol is source
- [ ] AC-017c-022: `GetByTargetAsync()` returns all dependencies where given symbol is target
- [ ] AC-017c-023: `GetByKindAsync()` returns all dependencies of a specific kind
- [ ] AC-017c-024: `GetBySourceAndKindAsync()` returns dependencies filtered by source and kind
- [ ] AC-017c-025: Direct queries complete in under 10ms for graphs with 1M edges
- [ ] AC-017c-026: Query results are returned as `IReadOnlyList<IDependency>`
- [ ] AC-017c-027: Empty result set returns empty list (not null)
- [ ] AC-017c-028: Non-existent symbol ID returns empty list (not error)
- [ ] AC-017c-029: Queries support cancellation via `CancellationToken`
- [ ] AC-017c-030: Query execution time is logged for performance monitoring

### Dependency Extraction (AC-017c-031 to AC-017c-040)

- [ ] AC-017c-031: `DependencyExtractor` extracts method calls as `Calls` dependencies
- [ ] AC-017c-032: Constructor invocations are extracted as `Calls` dependencies
- [ ] AC-017c-033: Property accesses are extracted as `References` dependencies
- [ ] AC-017c-034: Field accesses are extracted as `References` dependencies
- [ ] AC-017c-035: Type declarations in parameters/returns are extracted as `Uses` dependencies
- [ ] AC-017c-036: Base class declarations are extracted as `Inherits` dependencies
- [ ] AC-017c-037: Interface implementations are extracted as `Implements` dependencies
- [ ] AC-017c-038: Generic type arguments are extracted as `Uses` dependencies
- [ ] AC-017c-039: Chained method calls create individual `Calls` dependencies
- [ ] AC-017c-040: Extraction completes in under 100ms per file

### Graph Operations (AC-017c-041 to AC-017c-050)

- [ ] AC-017c-041: `IDependencyGraph` interface is defined in `Acode.Domain.Symbols` namespace
- [ ] AC-017c-042: `GetDependenciesAsync()` returns direct dependencies of a symbol
- [ ] AC-017c-043: `GetDependentsAsync()` returns direct dependents of a symbol
- [ ] AC-017c-044: `GetTransitiveDependenciesAsync()` returns dependencies up to max depth
- [ ] AC-017c-045: `GetTransitiveDependentsAsync()` returns dependents up to max depth
- [ ] AC-017c-046: Transitive queries respect configurable `maxDepth` parameter
- [ ] AC-017c-047: Transitive queries respect configurable `maxNodes` parameter
- [ ] AC-017c-048: Transitive queries complete within configurable timeout
- [ ] AC-017c-049: `HasCycleAsync()` detects circular dependencies
- [ ] AC-017c-050: `GetCyclesAsync()` returns all cycles involving a symbol

### Path Finding (AC-017c-051 to AC-017c-055)

- [ ] AC-017c-051: `FindPathAsync()` returns shortest path between two symbols
- [ ] AC-017c-052: Path is returned as ordered list of symbol IDs
- [ ] AC-017c-053: Returns null if no path exists between symbols
- [ ] AC-017c-054: Respects `maxDepth` parameter to limit search
- [ ] AC-017c-055: Path finding completes in under 100ms for depth 5

### Retrieval APIs (AC-017c-056 to AC-017c-065)

- [ ] AC-017c-056: `IDependencyQueryService` interface is defined in Application layer
- [ ] AC-017c-057: `GetDependenciesAsync()` method returns formatted dependency list
- [ ] AC-017c-058: `GetDependentsAsync()` method returns formatted dependent list
- [ ] AC-017c-059: `GetCallGraphAsync()` method returns hierarchical call tree
- [ ] AC-017c-060: `GetTypeHierarchyAsync()` method returns inheritance chain
- [ ] AC-017c-061: `GetImplementorsAsync()` method returns classes implementing interface
- [ ] AC-017c-062: `GetUsagesAsync()` method returns all locations where symbol is referenced
- [ ] AC-017c-063: `FindPathAsync()` method returns formatted path with edge types
- [ ] AC-017c-064: All API methods support filtering by `DependencyKind`
- [ ] AC-017c-065: API responses include source locations for navigation

### CLI Integration (AC-017c-066 to AC-017c-075)

- [ ] AC-017c-066: `acode deps of <symbol>` command is implemented
- [ ] AC-017c-067: `acode deps on <symbol>` command is implemented
- [ ] AC-017c-068: `acode deps calls <symbol>` command is implemented
- [ ] AC-017c-069: `acode deps path <from> <to>` command is implemented
- [ ] AC-017c-070: `acode deps implementors <interface>` command is implemented
- [ ] AC-017c-071: `--depth` flag controls transitive query depth
- [ ] AC-017c-072: `--kind` flag filters results by dependency kind
- [ ] AC-017c-073: `--format json` outputs machine-readable JSON
- [ ] AC-017c-074: Commands display help with `--help` flag
- [ ] AC-017c-075: Commands validate symbol names and provide helpful errors

### Error Handling (AC-017c-076 to AC-017c-080)

- [ ] AC-017c-076: Symbol not found returns `ACODE-DEP-001` error with symbol name
- [ ] AC-017c-077: Query timeout returns `ACODE-DEP-002` error with timeout value
- [ ] AC-017c-078: Cycle detection logs `ACODE-DEP-003` warning with involved symbols
- [ ] AC-017c-079: Depth exceeded returns `ACODE-DEP-004` error with actual depth
- [ ] AC-017c-080: All errors include actionable resolution guidance

---

## Best Practices

### Dependency Model

1. **Explicit relationship types** - calls, extends, implements, imports, uses
2. **Bidirectional navigation** - Query both "depends on" and "depended by"
3. **Transitive closure** - Compute full dependency chains when needed
4. **Handle cycles** - Circular dependencies don't break analysis

### API Design

5. **Consistent response format** - Same structure for all dependency queries
6. **Depth control** - Allow specifying how deep to traverse
7. **Filter by type** - Query specific relationship types only
8. **Pagination for large results** - Don't return unbounded lists

### Performance

9. **Pre-compute common queries** - Cache frequently accessed dependency paths
10. **Lazy loading** - Load full details on demand, not upfront
11. **Index for traversal** - Optimize storage for graph queries
12. **Bound traversal depth** - Prevent runaway recursive queries

---

## Assumptions

This section documents explicit assumptions made during the design and implementation of dependency mapping.

### Technical Assumptions

1. **Symbol IDs are stable** - Symbol IDs generated by Task 017.a (C#) and Task 017.b (TS/JS) are deterministic and remain constant for the same symbol across indexing runs.

2. **SQLite is available** - The SQLite database engine is available in the runtime environment and supports all required SQL features (indexes, transactions, parameterized queries).

3. **Sufficient disk space** - The workspace has adequate disk space for the dependency graph database, estimated at approximately 100 bytes per edge (1GB for 10 million edges).

4. **Memory constraints respected** - Available memory allows for at least 10,000 in-memory dependency edges during transitive queries before pagination is required.

5. **Single-threaded writes** - Write operations to the dependency store are serialized; concurrent reads are allowed but writes are single-threaded per file.

6. **UTF-8 file paths** - All file paths in dependencies use UTF-8 encoding and are relative to the workspace root.

### Operational Assumptions

7. **Symbol extraction precedes dependency extraction** - Dependencies are extracted AFTER symbols are indexed; the symbol index is populated before dependency extraction begins.

8. **Incremental updates are file-scoped** - When a file changes, only dependencies from that file are updated; other files' dependencies remain unchanged.

9. **Users understand dependency concepts** - Users querying dependencies understand basic software engineering concepts (dependencies, dependents, transitive relationships).

10. **CLI is the primary interface** - Most users interact with dependency features via CLI commands; programmatic API is secondary.

11. **Query results fit in memory** - Individual query results (with max_nodes limit) fit in available memory for processing.

12. **Network is not required** - All dependency operations work offline; no network connectivity is required.

### Integration Assumptions

13. **Symbol Index API is stable** - The `ISymbolIndex` interface from Task 017 provides stable methods for symbol lookup by ID.

14. **File watcher triggers updates** - The file watcher system (when implemented) will trigger dependency updates when source files change.

15. **Context packer will consume dependencies** - The context packer module will use dependency APIs to include related code in LLM prompts.

16. **Configuration system is available** - The YAML configuration system from Task 002 is available for reading dependency settings.

17. **Logging infrastructure is available** - Microsoft.Extensions.Logging is available for diagnostic logging.

18. **DI container registration is handled externally** - Dependency injection registration for dependency services is configured in the composition root.

---

## Security Threats and Mitigations

### Threat 1: Resource Exhaustion via Unbounded Transitive Queries

**Threat ID:** THREAT-017c-001  
**Severity:** HIGH  
**Attack Vector:** Attacker crafts a query with very high depth that causes the graph traversal to consume all available memory or CPU.  
**Impact:** Denial of service, application crash, system instability

**Mitigation - Complete C# Implementation:**

```csharp
using System;
using System.Threading;
using Acode.Domain.Symbols;

namespace Acode.Infrastructure.Symbols.Dependencies.Security;

/// <summary>
/// Enforces resource limits on graph traversal operations to prevent DoS attacks.
/// </summary>
public sealed class BoundedTraversalGuard : IDisposable
{
    private readonly int _maxDepth;
    private readonly int _maxNodes;
    private readonly TimeSpan _timeout;
    private readonly CancellationTokenSource _timeoutCts;
    private int _currentDepth;
    private int _visitedNodes;

    public BoundedTraversalGuard(
        int maxDepth = 10,
        int maxNodes = 1000,
        int timeoutMs = 5000)
    {
        if (maxDepth <= 0)
            throw new ArgumentOutOfRangeException(nameof(maxDepth), "Max depth must be positive");
        if (maxNodes <= 0)
            throw new ArgumentOutOfRangeException(nameof(maxNodes), "Max nodes must be positive");
        if (timeoutMs <= 0)
            throw new ArgumentOutOfRangeException(nameof(timeoutMs), "Timeout must be positive");

        _maxDepth = maxDepth;
        _maxNodes = maxNodes;
        _timeout = TimeSpan.FromMilliseconds(timeoutMs);
        _timeoutCts = new CancellationTokenSource(_timeout);
        _currentDepth = 0;
        _visitedNodes = 0;
    }

    public CancellationToken TimeoutToken => _timeoutCts.Token;

    public void EnterLevel()
    {
        _currentDepth++;
        if (_currentDepth > _maxDepth)
        {
            throw new DependencyTraversalException(
                $"Maximum traversal depth ({_maxDepth}) exceeded",
                "ACODE-DEP-004");
        }
    }

    public void ExitLevel()
    {
        _currentDepth--;
    }

    public void RecordVisit()
    {
        var count = Interlocked.Increment(ref _visitedNodes);
        if (count > _maxNodes)
        {
            throw new DependencyTraversalException(
                $"Maximum node count ({_maxNodes}) exceeded. Use --depth or --limit to constrain results.",
                "ACODE-DEP-005");
        }
    }

    public TraversalStats GetStats() => new(_currentDepth, _visitedNodes, _maxDepth, _maxNodes);

    public void Dispose()
    {
        _timeoutCts.Dispose();
    }
}

public sealed record TraversalStats(int CurrentDepth, int VisitedNodes, int MaxDepth, int MaxNodes);

public sealed class DependencyTraversalException : Exception
{
    public string ErrorCode { get; }

    public DependencyTraversalException(string message, string errorCode) 
        : base(message)
    {
        ErrorCode = errorCode;
    }
}
```

---

### Threat 2: SQL Injection via Malicious Symbol IDs

**Threat ID:** THREAT-017c-002  
**Severity:** CRITICAL  
**Attack Vector:** Attacker provides crafted symbol ID containing SQL injection payload.  
**Impact:** Database corruption, unauthorized data access, data exfiltration

**Mitigation - Complete C# Implementation:**

```csharp
using System;
using System.Text.RegularExpressions;
using Microsoft.Data.Sqlite;

namespace Acode.Infrastructure.Symbols.Dependencies.Security;

/// <summary>
/// Validates and sanitizes all inputs to dependency queries to prevent SQL injection.
/// </summary>
public static partial class QueryInputValidator
{
    // Symbol IDs must be valid GUIDs
    private static readonly Regex GuidPattern = GeneratedGuidRegex();

    // File paths must be relative and not contain traversal sequences
    private static readonly string[] ForbiddenPathPatterns = { "..", "~", ":" };

    /// <summary>
    /// Validates that the input is a valid symbol ID (GUID format).
    /// </summary>
    public static Guid ValidateSymbolId(string input, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            throw new ArgumentException(
                $"{parameterName} cannot be null or empty",
                parameterName);
        }

        if (!GuidPattern.IsMatch(input))
        {
            throw new ArgumentException(
                $"{parameterName} must be a valid GUID, received: '{input}'",
                parameterName);
        }

        if (!Guid.TryParse(input, out var guid))
        {
            throw new ArgumentException(
                $"{parameterName} is not a valid GUID format",
                parameterName);
        }

        return guid;
    }

    /// <summary>
    /// Validates that the file path is relative and safe.
    /// </summary>
    public static string ValidateFilePath(string input, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            throw new ArgumentException(
                $"{parameterName} cannot be null or empty",
                parameterName);
        }

        foreach (var pattern in ForbiddenPathPatterns)
        {
            if (input.Contains(pattern, StringComparison.Ordinal))
            {
                throw new SecurityException(
                    $"File path contains forbidden pattern '{pattern}': {input}");
            }
        }

        if (Path.IsPathRooted(input))
        {
            throw new SecurityException(
                $"File path must be relative, not absolute: {input}");
        }

        return input;
    }

    /// <summary>
    /// Creates a parameterized SQL command to prevent injection.
    /// </summary>
    public static SqliteCommand CreateSafeQuery(
        SqliteConnection connection,
        string sql,
        params (string Name, object Value)[] parameters)
    {
        var command = connection.CreateCommand();
        command.CommandText = sql;

        foreach (var (name, value) in parameters)
        {
            command.Parameters.AddWithValue(name, value ?? DBNull.Value);
        }

        return command;
    }

    [GeneratedRegex(@"^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$")]
    private static partial Regex GeneratedGuidRegex();
}
```

---

### Threat 3: Information Disclosure via File Path Exposure

**Threat ID:** THREAT-017c-003  
**Severity:** MEDIUM  
**Attack Vector:** Dependency data contains absolute file paths that reveal system structure.  
**Impact:** Information disclosure, facilitates further attacks

**Mitigation - Complete C# Implementation:**

```csharp
using System;
using System.IO;

namespace Acode.Infrastructure.Symbols.Dependencies.Security;

/// <summary>
/// Ensures all file paths in dependencies are stored and returned as relative paths only.
/// </summary>
public sealed class PathNormalizer
{
    private readonly string _workspaceRoot;

    public PathNormalizer(string workspaceRoot)
    {
        if (string.IsNullOrWhiteSpace(workspaceRoot))
            throw new ArgumentNullException(nameof(workspaceRoot));

        _workspaceRoot = Path.GetFullPath(workspaceRoot);
        if (!_workspaceRoot.EndsWith(Path.DirectorySeparatorChar))
        {
            _workspaceRoot += Path.DirectorySeparatorChar;
        }
    }

    /// <summary>
    /// Converts an absolute path to a relative path within the workspace.
    /// </summary>
    public string ToRelative(string absolutePath)
    {
        if (string.IsNullOrWhiteSpace(absolutePath))
            return string.Empty;

        var fullPath = Path.GetFullPath(absolutePath);

        if (!fullPath.StartsWith(_workspaceRoot, StringComparison.OrdinalIgnoreCase))
        {
            throw new SecurityException(
                $"Path '{absolutePath}' is outside workspace root");
        }

        var relativePath = fullPath[_workspaceRoot.Length..];
        
        // Normalize to forward slashes for consistency
        return relativePath.Replace(Path.DirectorySeparatorChar, '/');
    }

    /// <summary>
    /// Converts a relative path back to absolute for file operations.
    /// </summary>
    public string ToAbsolute(string relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
            throw new ArgumentNullException(nameof(relativePath));

        // Security check: ensure no traversal
        if (relativePath.Contains(".."))
        {
            throw new SecurityException(
                "Relative path cannot contain parent directory references");
        }

        var normalized = relativePath.Replace('/', Path.DirectorySeparatorChar);
        var absolutePath = Path.Combine(_workspaceRoot, normalized);
        var fullPath = Path.GetFullPath(absolutePath);

        // Verify result is still within workspace
        if (!fullPath.StartsWith(_workspaceRoot, StringComparison.OrdinalIgnoreCase))
        {
            throw new SecurityException(
                $"Resolved path is outside workspace: {fullPath}");
        }

        return fullPath;
    }
}
```

---

### Threat 4: Denial of Service via Malformed Graph Data

**Threat ID:** THREAT-017c-004  
**Severity:** HIGH  
**Attack Vector:** Malicious code contains patterns that generate exponential dependency relationships.  
**Impact:** Database bloat, query performance degradation, storage exhaustion

**Mitigation - Complete C# Implementation:**

```csharp
using System;
using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace Acode.Infrastructure.Symbols.Dependencies.Security;

/// <summary>
/// Rate limits and bounds dependency extraction to prevent database bloat attacks.
/// </summary>
public sealed class ExtractionLimiter
{
    private readonly int _maxEdgesPerFile;
    private readonly int _maxEdgesPerSymbol;
    private readonly int _maxTotalEdges;
    private readonly ILogger<ExtractionLimiter> _logger;
    private readonly ConcurrentDictionary<string, int> _edgesPerFile = new();
    private int _totalEdges;

    public ExtractionLimiter(
        ILogger<ExtractionLimiter> logger,
        int maxEdgesPerFile = 10000,
        int maxEdgesPerSymbol = 500,
        int maxTotalEdges = 10_000_000)
    {
        _logger = logger;
        _maxEdgesPerFile = maxEdgesPerFile;
        _maxEdgesPerSymbol = maxEdgesPerSymbol;
        _maxTotalEdges = maxTotalEdges;
    }

    /// <summary>
    /// Validates whether an additional edge can be recorded for the given file.
    /// </summary>
    public bool CanAddEdge(string filePath, out string? rejectionReason)
    {
        rejectionReason = null;

        // Check total limit
        if (_totalEdges >= _maxTotalEdges)
        {
            rejectionReason = $"Maximum total edges ({_maxTotalEdges:N0}) exceeded";
            _logger.LogWarning("Dependency extraction blocked: {Reason}", rejectionReason);
            return false;
        }

        // Check per-file limit
        var fileCount = _edgesPerFile.GetOrAdd(filePath, 0);
        if (fileCount >= _maxEdgesPerFile)
        {
            rejectionReason = $"Maximum edges per file ({_maxEdgesPerFile:N0}) exceeded for {filePath}";
            _logger.LogWarning("Dependency extraction blocked: {Reason}", rejectionReason);
            return false;
        }

        return true;
    }

    /// <summary>
    /// Records that an edge was successfully added.
    /// </summary>
    public void RecordEdge(string filePath)
    {
        _edgesPerFile.AddOrUpdate(filePath, 1, (_, count) => count + 1);
        Interlocked.Increment(ref _totalEdges);
    }

    /// <summary>
    /// Resets counters for a file (called before re-extracting).
    /// </summary>
    public void ResetFile(string filePath)
    {
        if (_edgesPerFile.TryRemove(filePath, out var count))
        {
            Interlocked.Add(ref _totalEdges, -count);
        }
    }

    public ExtractionStats GetStats() => new(
        _totalEdges,
        _edgesPerFile.Count,
        _maxTotalEdges,
        _maxEdgesPerFile);
}

public sealed record ExtractionStats(
    int TotalEdges,
    int FilesWithEdges,
    int MaxTotalEdges,
    int MaxEdgesPerFile);
```

---

### Threat 5: Cycle-Based Infinite Loop Attack

**Threat ID:** THREAT-017c-005  
**Severity:** HIGH  
**Attack Vector:** Attacker creates code with circular dependencies designed to cause infinite loops in traversal.  
**Impact:** Application hang, CPU exhaustion, denial of service

**Mitigation - Complete C# Implementation:**

```csharp
using System;
using System.Collections.Generic;

namespace Acode.Infrastructure.Symbols.Dependencies.Security;

/// <summary>
/// Detects and safely handles cycles during graph traversal.
/// </summary>
public sealed class CycleDetectingVisitor
{
    private readonly HashSet<Guid> _visited = new();
    private readonly HashSet<Guid> _inCurrentPath = new();
    private readonly List<List<Guid>> _detectedCycles = new();
    private readonly Stack<Guid> _currentPath = new();

    /// <summary>
    /// Attempts to enter a node. Returns false if already visited (cycle detected).
    /// </summary>
    public VisitResult TryEnter(Guid symbolId)
    {
        _currentPath.Push(symbolId);

        if (_inCurrentPath.Contains(symbolId))
        {
            // Cycle detected - capture the cycle path
            var cycle = ExtractCycle(symbolId);
            _detectedCycles.Add(cycle);
            _currentPath.Pop();
            return VisitResult.CycleDetected(cycle);
        }

        if (_visited.Contains(symbolId))
        {
            // Already processed in another branch - skip
            _currentPath.Pop();
            return VisitResult.AlreadyVisited;
        }

        _inCurrentPath.Add(symbolId);
        _visited.Add(symbolId);
        return VisitResult.Success;
    }

    /// <summary>
    /// Exits the current node after processing.
    /// </summary>
    public void Exit(Guid symbolId)
    {
        _inCurrentPath.Remove(symbolId);
        if (_currentPath.Count > 0 && _currentPath.Peek() == symbolId)
        {
            _currentPath.Pop();
        }
    }

    /// <summary>
    /// Returns all cycles detected during traversal.
    /// </summary>
    public IReadOnlyList<IReadOnlyList<Guid>> GetDetectedCycles() 
        => _detectedCycles;

    /// <summary>
    /// Returns whether any cycles were detected.
    /// </summary>
    public bool HasCycles => _detectedCycles.Count > 0;

    private List<Guid> ExtractCycle(Guid cycleStart)
    {
        var cycle = new List<Guid>();
        var pathArray = _currentPath.ToArray();
        
        bool inCycle = false;
        for (int i = pathArray.Length - 1; i >= 0; i--)
        {
            if (pathArray[i] == cycleStart)
            {
                inCycle = true;
            }
            
            if (inCycle)
            {
                cycle.Add(pathArray[i]);
            }
        }
        
        cycle.Reverse();
        return cycle;
    }
}

public readonly struct VisitResult
{
    public bool CanContinue { get; }
    public bool IsCycle { get; }
    public IReadOnlyList<Guid>? CyclePath { get; }

    private VisitResult(bool canContinue, bool isCycle, IReadOnlyList<Guid>? cyclePath)
    {
        CanContinue = canContinue;
        IsCycle = isCycle;
        CyclePath = cyclePath;
    }

    public static VisitResult Success => new(true, false, null);
    public static VisitResult AlreadyVisited => new(false, false, null);
    public static VisitResult CycleDetected(IReadOnlyList<Guid> path) => new(false, true, path);
}
```

---

## Troubleshooting

This section provides diagnosis and resolution steps for common issues with dependency mapping.

---

### Issue 1: Dependencies Not Appearing in Query Results

**Symptoms:**
- `acode deps of MyClass` returns empty or fewer results than expected
- Known method calls not appearing in dependencies
- Symbol shows as having 0 dependencies when it clearly uses other code

**Possible Causes:**
1. Symbol extraction for the source file failed or was skipped
2. Target symbols are not indexed (external packages, missing files)
3. Dependency kind is not configured for extraction
4. Query is filtering out expected results
5. Incremental update has not processed recent changes

**Solutions:**

1. **Verify symbol extraction completed:**
```bash
acode symbols show MyClass
# Should display symbol metadata including file location
# If "Symbol not found", run: acode index src/
```

2. **Rebuild dependency graph:**
```bash
acode index --rebuild --include-deps
```

3. **Check dependency kinds configuration:**
```yaml
# .agent/config.yml - ensure all needed kinds are listed
symbol_index:
  dependencies:
    kinds:
      - calls      # Method invocations
      - uses       # Type references
      - inherits   # Base class
      - implements # Interface impl
      - references # All others
```

4. **Remove filters from query:**
```bash
# Instead of filtered query
acode deps of MyClass --kind calls

# Try unfiltered to see all dependencies
acode deps of MyClass
```

---

### Issue 2: Transitive Queries Are Extremely Slow

**Symptoms:**
- Queries with `--depth 3` or higher take more than 5 seconds
- CLI appears frozen during dependency queries
- High CPU usage during queries
- Memory usage spikes during large traversals

**Possible Causes:**
1. Query depth is too high for the graph density
2. No node limit configured - returning too many results
3. Cache is disabled or cold
4. Database indexes are not optimized or missing
5. Very dense graph (many symbols with many dependencies)

**Solutions:**

1. **Reduce query depth:**
```bash
# Use lower depth
acode deps of MyClass --depth 2

# Instead of
acode deps of MyClass --depth 5
```

2. **Add node limits:**
```yaml
symbol_index:
  dependencies:
    transitive:
      max_nodes: 500  # Limit total results
```

3. **Enable and verify cache:**
```yaml
symbol_index:
  dependencies:
    performance:
      enable_cache: true
      cache_ttl_seconds: 600
```

4. **Rebuild database indexes:**
```bash
acode index --rebuild-indexes
```

---

### Issue 3: Circular Dependency Warnings in Logs

**Symptoms:**
- Warning messages: "Cycle detected involving symbols: X → Y → Z → X"
- Query results contain duplicate symbol paths
- Graph traversal seems to visit same symbols multiple times

**Possible Causes:**
1. Actual circular dependencies exist in the codebase (code quality issue)
2. Mutual references between classes (normal in some patterns)
3. Event handler patterns creating bidirectional relationships

**Solutions:**

1. **Understand that cycles are handled safely:**
   - Queries still return correct results
   - Cycles are detected and short-circuited
   - No infinite loops will occur

2. **List all cycles for review:**
```bash
acode deps cycles
# Shows all circular dependency chains
```

3. **Refactor circular dependencies (optional, code quality improvement):**
   - Introduce interfaces to break direct dependencies
   - Use dependency injection
   - Separate concerns into different layers

---

### Issue 4: Database Locked or Corruption Errors

**Symptoms:**
- Error: "database is locked"
- Error: "database disk image is malformed"
- Queries fail intermittently
- Index operations hang

**Possible Causes:**
1. Multiple processes accessing database simultaneously
2. Previous process crashed leaving lock file
3. Disk space exhausted during write
4. File system corruption

**Solutions:**

1. **Close other Acode processes:**
```bash
# Windows
taskkill /IM acode.exe /F

# Linux/macOS
pkill -f acode
```

2. **Remove lock files:**
```bash
# Remove SQLite lock files
Remove-Item .agent/workspace.db-journal -ErrorAction SilentlyContinue
Remove-Item .agent/workspace.db-wal -ErrorAction SilentlyContinue
Remove-Item .agent/workspace.db-shm -ErrorAction SilentlyContinue
```

3. **Rebuild database from scratch:**
```bash
# Backup and rebuild
Move-Item .agent/workspace.db .agent/workspace.db.backup
acode index --rebuild
```

4. **Check disk space:**
```bash
# Ensure at least 1GB free
Get-PSDrive C | Select-Object Used, Free
```

---

### Issue 5: CLI Commands Not Recognized

**Symptoms:**
- Error: "Unknown command 'deps'"
- Help shows no dependency commands
- Commands work in some environments but not others

**Possible Causes:**
1. Acode version is outdated (pre-dependency support)
2. Command not included in current build
3. PATH not configured correctly

**Solutions:**

1. **Verify Acode version:**
```bash
acode --version
# Should be 0.3.0 or later for dependency commands
```

2. **Update Acode:**
```bash
dotnet tool update -g acode
```

3. **Check available commands:**
```bash
acode --help
# Look for 'deps' in command list
```

---

## Testing Requirements

### Unit Tests - Complete Implementation

#### DependencyModelTests.cs

```csharp
using Acode.Domain.Symbols;
using FluentAssertions;
using Xunit;

namespace Acode.Domain.Tests.Symbols;

public class DependencyModelTests
{
    [Fact]
    public void Dependency_Should_Store_Source_Id()
    {
        // Arrange
        var sourceId = Guid.NewGuid();
        var targetId = Guid.NewGuid();

        // Act
        var dependency = new Dependency(
            sourceSymbolId: sourceId,
            targetSymbolId: targetId,
            kind: DependencyKind.Calls,
            location: null);

        // Assert
        dependency.SourceSymbolId.Should().Be(sourceId);
    }

    [Fact]
    public void Dependency_Should_Store_Target_Id()
    {
        // Arrange
        var sourceId = Guid.NewGuid();
        var targetId = Guid.NewGuid();

        // Act
        var dependency = new Dependency(
            sourceSymbolId: sourceId,
            targetSymbolId: targetId,
            kind: DependencyKind.Uses,
            location: null);

        // Assert
        dependency.TargetSymbolId.Should().Be(targetId);
    }

    [Fact]
    public void Dependency_Should_Store_Kind()
    {
        // Arrange & Act
        var dependency = new Dependency(
            sourceSymbolId: Guid.NewGuid(),
            targetSymbolId: Guid.NewGuid(),
            kind: DependencyKind.Inherits,
            location: null);

        // Assert
        dependency.Kind.Should().Be(DependencyKind.Inherits);
    }

    [Fact]
    public void Dependency_Should_Store_Location()
    {
        // Arrange
        var location = new SymbolLocation("src/Services/UserService.cs", 45, 12);

        // Act
        var dependency = new Dependency(
            sourceSymbolId: Guid.NewGuid(),
            targetSymbolId: Guid.NewGuid(),
            kind: DependencyKind.Calls,
            location: location);

        // Assert
        dependency.Location.Should().NotBeNull();
        dependency.Location!.FilePath.Should().Be("src/Services/UserService.cs");
        dependency.Location.Line.Should().Be(45);
        dependency.Location.Column.Should().Be(12);
    }

    [Theory]
    [InlineData(DependencyKind.Calls)]
    [InlineData(DependencyKind.Uses)]
    [InlineData(DependencyKind.Inherits)]
    [InlineData(DependencyKind.Implements)]
    [InlineData(DependencyKind.References)]
    public void Dependency_Should_Support_All_Kinds(DependencyKind kind)
    {
        // Arrange & Act
        var dependency = new Dependency(
            sourceSymbolId: Guid.NewGuid(),
            targetSymbolId: Guid.NewGuid(),
            kind: kind,
            location: null);

        // Assert
        dependency.Kind.Should().Be(kind);
    }

    [Fact]
    public void Dependency_Should_Generate_Unique_Id()
    {
        // Arrange & Act
        var dep1 = new Dependency(Guid.NewGuid(), Guid.NewGuid(), DependencyKind.Calls, null);
        var dep2 = new Dependency(Guid.NewGuid(), Guid.NewGuid(), DependencyKind.Calls, null);

        // Assert
        dep1.Id.Should().NotBe(dep2.Id);
        dep1.Id.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void Dependency_Equality_Should_Be_Based_On_Source_Target_Kind()
    {
        // Arrange
        var sourceId = Guid.NewGuid();
        var targetId = Guid.NewGuid();

        var dep1 = new Dependency(sourceId, targetId, DependencyKind.Calls, null);
        var dep2 = new Dependency(sourceId, targetId, DependencyKind.Calls, null);
        var dep3 = new Dependency(sourceId, targetId, DependencyKind.Uses, null);

        // Assert
        dep1.Equals(dep2).Should().BeTrue();
        dep1.Equals(dep3).Should().BeFalse();
    }
}
```

---

#### DependencyStoreTests.cs

```csharp
using Acode.Domain.Symbols;
using Acode.Infrastructure.Symbols.Dependencies;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Acode.Infrastructure.Tests.Symbols.Dependencies;

public class DependencyStoreTests : IDisposable
{
    private readonly DependencyStore _sut;
    private readonly string _dbPath;

    public DependencyStoreTests()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"test-deps-{Guid.NewGuid()}.db");
        _sut = new DependencyStore(_dbPath, new NullLogger<DependencyStore>());
    }

    [Fact]
    public async Task AddAsync_Should_Add_Single_Dependency()
    {
        // Arrange
        var dependency = CreateDependency(DependencyKind.Calls);

        // Act
        await _sut.AddAsync(dependency, default);

        // Assert
        var result = await _sut.GetBySourceAsync(dependency.SourceSymbolId, default);
        result.Should().ContainSingle();
        result[0].TargetSymbolId.Should().Be(dependency.TargetSymbolId);
    }

    [Fact]
    public async Task AddBatchAsync_Should_Add_Multiple_Dependencies()
    {
        // Arrange
        var sourceId = Guid.NewGuid();
        var dependencies = Enumerable.Range(0, 100)
            .Select(_ => CreateDependency(DependencyKind.Calls, sourceId: sourceId))
            .ToList();

        // Act
        await _sut.AddBatchAsync(dependencies, default);

        // Assert
        var result = await _sut.GetBySourceAsync(sourceId, default);
        result.Should().HaveCount(100);
    }

    [Fact]
    public async Task RemoveAsync_Should_Remove_Dependency()
    {
        // Arrange
        var dependency = CreateDependency(DependencyKind.Uses);
        await _sut.AddAsync(dependency, default);

        // Act
        await _sut.RemoveAsync(dependency.Id, default);

        // Assert
        var result = await _sut.GetBySourceAsync(dependency.SourceSymbolId, default);
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task RemoveBySourceAsync_Should_Remove_All_From_Source()
    {
        // Arrange
        var sourceId = Guid.NewGuid();
        await _sut.AddAsync(CreateDependency(DependencyKind.Calls, sourceId: sourceId), default);
        await _sut.AddAsync(CreateDependency(DependencyKind.Uses, sourceId: sourceId), default);
        await _sut.AddAsync(CreateDependency(DependencyKind.Inherits, sourceId: sourceId), default);

        // Act
        await _sut.RemoveBySourceAsync(sourceId, default);

        // Assert
        var result = await _sut.GetBySourceAsync(sourceId, default);
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetBySourceAsync_Should_Return_Dependencies()
    {
        // Arrange
        var sourceId = Guid.NewGuid();
        await _sut.AddAsync(CreateDependency(DependencyKind.Calls, sourceId: sourceId), default);
        await _sut.AddAsync(CreateDependency(DependencyKind.Uses, sourceId: sourceId), default);

        // Act
        var result = await _sut.GetBySourceAsync(sourceId, default);

        // Assert
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetByTargetAsync_Should_Return_Dependents()
    {
        // Arrange
        var targetId = Guid.NewGuid();
        await _sut.AddAsync(CreateDependency(DependencyKind.Calls, targetId: targetId), default);
        await _sut.AddAsync(CreateDependency(DependencyKind.Calls, targetId: targetId), default);

        // Act
        var result = await _sut.GetByTargetAsync(targetId, default);

        // Assert
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetByKindAsync_Should_Filter_By_Kind()
    {
        // Arrange
        var sourceId = Guid.NewGuid();
        await _sut.AddAsync(CreateDependency(DependencyKind.Calls, sourceId: sourceId), default);
        await _sut.AddAsync(CreateDependency(DependencyKind.Uses, sourceId: sourceId), default);
        await _sut.AddAsync(CreateDependency(DependencyKind.Calls, sourceId: sourceId), default);

        // Act
        var result = await _sut.GetBySourceAndKindAsync(sourceId, DependencyKind.Calls, default);

        // Assert
        result.Should().HaveCount(2);
        result.Should().AllSatisfy(d => d.Kind.Should().Be(DependencyKind.Calls));
    }

    private static Dependency CreateDependency(
        DependencyKind kind,
        Guid? sourceId = null,
        Guid? targetId = null)
    {
        return new Dependency(
            sourceSymbolId: sourceId ?? Guid.NewGuid(),
            targetSymbolId: targetId ?? Guid.NewGuid(),
            kind: kind,
            location: null);
    }

    public void Dispose()
    {
        _sut.Dispose();
        if (File.Exists(_dbPath))
            File.Delete(_dbPath);
    }
}
```

---

#### DependencyGraphTests.cs

```csharp
using Acode.Domain.Symbols;
using Acode.Infrastructure.Symbols.Dependencies;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Acode.Infrastructure.Tests.Symbols.Dependencies;

public class DependencyGraphTests
{
    private readonly Mock<IDependencyStore> _storeMock;
    private readonly DependencyGraph _sut;

    public DependencyGraphTests()
    {
        _storeMock = new Mock<IDependencyStore>();
        _sut = new DependencyGraph(_storeMock.Object, new NullLogger<DependencyGraph>());
    }

    [Fact]
    public async Task GetDependenciesAsync_Should_Return_Direct_Dependencies()
    {
        // Arrange
        var sourceId = Guid.NewGuid();
        var deps = new List<IDependency>
        {
            new Dependency(sourceId, Guid.NewGuid(), DependencyKind.Calls, null),
            new Dependency(sourceId, Guid.NewGuid(), DependencyKind.Uses, null)
        };
        _storeMock.Setup(s => s.GetBySourceAsync(sourceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(deps);

        // Act
        var result = await _sut.GetDependenciesAsync(sourceId, default);

        // Assert
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetDependentsAsync_Should_Return_Direct_Dependents()
    {
        // Arrange
        var targetId = Guid.NewGuid();
        var deps = new List<IDependency>
        {
            new Dependency(Guid.NewGuid(), targetId, DependencyKind.Calls, null)
        };
        _storeMock.Setup(s => s.GetByTargetAsync(targetId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(deps);

        // Act
        var result = await _sut.GetDependentsAsync(targetId, default);

        // Assert
        result.Should().ContainSingle();
    }

    [Fact]
    public async Task GetTransitiveDependenciesAsync_Should_Respect_MaxDepth()
    {
        // Arrange - create chain: A → B → C → D
        var idA = Guid.NewGuid();
        var idB = Guid.NewGuid();
        var idC = Guid.NewGuid();
        var idD = Guid.NewGuid();

        _storeMock.Setup(s => s.GetBySourceAsync(idA, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<IDependency> { new Dependency(idA, idB, DependencyKind.Calls, null) });
        _storeMock.Setup(s => s.GetBySourceAsync(idB, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<IDependency> { new Dependency(idB, idC, DependencyKind.Calls, null) });
        _storeMock.Setup(s => s.GetBySourceAsync(idC, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<IDependency> { new Dependency(idC, idD, DependencyKind.Calls, null) });

        // Act - depth 2 should get B and C, not D
        var result = await _sut.GetTransitiveDependenciesAsync(idA, maxDepth: 2, default);

        // Assert
        result.Select(d => d.TargetSymbolId).Should().Contain(idB);
        result.Select(d => d.TargetSymbolId).Should().Contain(idC);
        result.Select(d => d.TargetSymbolId).Should().NotContain(idD);
    }

    [Fact]
    public async Task GetTransitiveDependenciesAsync_Should_Handle_Cycles()
    {
        // Arrange - create cycle: A → B → C → A
        var idA = Guid.NewGuid();
        var idB = Guid.NewGuid();
        var idC = Guid.NewGuid();

        _storeMock.Setup(s => s.GetBySourceAsync(idA, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<IDependency> { new Dependency(idA, idB, DependencyKind.Calls, null) });
        _storeMock.Setup(s => s.GetBySourceAsync(idB, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<IDependency> { new Dependency(idB, idC, DependencyKind.Calls, null) });
        _storeMock.Setup(s => s.GetBySourceAsync(idC, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<IDependency> { new Dependency(idC, idA, DependencyKind.Calls, null) });

        // Act - should not hang, should return results
        var result = await _sut.GetTransitiveDependenciesAsync(idA, maxDepth: 10, default);

        // Assert
        result.Should().NotBeEmpty();
        result.Should().HaveCountLessThanOrEqualTo(3); // Each unique edge once
    }

    [Fact]
    public async Task HasCycleAsync_Should_Detect_Cycle()
    {
        // Arrange - create cycle: A → B → A
        var idA = Guid.NewGuid();
        var idB = Guid.NewGuid();

        _storeMock.Setup(s => s.GetBySourceAsync(idA, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<IDependency> { new Dependency(idA, idB, DependencyKind.Calls, null) });
        _storeMock.Setup(s => s.GetBySourceAsync(idB, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<IDependency> { new Dependency(idB, idA, DependencyKind.Calls, null) });

        // Act
        var hasCycle = await _sut.HasCycleAsync(idA, default);

        // Assert
        hasCycle.Should().BeTrue();
    }

    [Fact]
    public async Task FindPathAsync_Should_Return_Shortest_Path()
    {
        // Arrange - A → B → C, A → C (two paths, one shorter)
        var idA = Guid.NewGuid();
        var idB = Guid.NewGuid();
        var idC = Guid.NewGuid();

        _storeMock.Setup(s => s.GetBySourceAsync(idA, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<IDependency> 
            { 
                new Dependency(idA, idB, DependencyKind.Calls, null),
                new Dependency(idA, idC, DependencyKind.Calls, null)
            });

        // Act
        var path = await _sut.FindPathAsync(idA, idC, default);

        // Assert
        path.Should().NotBeNull();
        path.Should().HaveCount(2); // A → C direct is shortest
        path![0].Should().Be(idA);
        path[1].Should().Be(idC);
    }

    [Fact]
    public async Task FindPathAsync_Should_Return_Null_If_No_Path()
    {
        // Arrange - A has no connection to C
        var idA = Guid.NewGuid();
        var idC = Guid.NewGuid();

        _storeMock.Setup(s => s.GetBySourceAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<IDependency>());

        // Act
        var path = await _sut.FindPathAsync(idA, idC, default);

        // Assert
        path.Should().BeNull();
    }
}
```

---

### Integration Tests

#### DependencyIntegrationTests.cs

```csharp
using Acode.Domain.Symbols;
using Acode.Infrastructure.Symbols.Dependencies;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Acode.Integration.Tests.Symbols.Dependencies;

public class DependencyIntegrationTests : IDisposable
{
    private readonly string _dbPath;
    private readonly DependencyStore _store;
    private readonly DependencyGraph _graph;

    public DependencyIntegrationTests()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"integration-deps-{Guid.NewGuid()}.db");
        _store = new DependencyStore(_dbPath, new NullLogger<DependencyStore>());
        _graph = new DependencyGraph(_store, new NullLogger<DependencyGraph>());
    }

    [Fact]
    public async Task Should_Persist_And_Query_Large_Graph()
    {
        // Arrange - create 10,000 edges
        var edges = new List<Dependency>();
        var rootId = Guid.NewGuid();

        for (int i = 0; i < 10000; i++)
        {
            edges.Add(new Dependency(
                sourceSymbolId: i < 100 ? rootId : Guid.NewGuid(),
                targetSymbolId: Guid.NewGuid(),
                kind: DependencyKind.Calls,
                location: null));
        }

        // Act
        var sw = System.Diagnostics.Stopwatch.StartNew();
        await _store.AddBatchAsync(edges, default);
        sw.Stop();

        // Assert
        sw.ElapsedMilliseconds.Should().BeLessThan(2000); // 2 seconds max

        var result = await _store.GetBySourceAsync(rootId, default);
        result.Should().HaveCount(100);
    }

    [Fact]
    public async Task Should_Handle_Complex_Transitive_Query()
    {
        // Arrange - create depth 5 graph
        var ids = Enumerable.Range(0, 6).Select(_ => Guid.NewGuid()).ToList();
        
        for (int i = 0; i < 5; i++)
        {
            await _store.AddAsync(
                new Dependency(ids[i], ids[i + 1], DependencyKind.Calls, null),
                default);
        }

        // Act
        var result = await _graph.GetTransitiveDependenciesAsync(ids[0], maxDepth: 5, default);

        // Assert
        result.Should().HaveCount(5);
    }

    public void Dispose()
    {
        _store.Dispose();
        if (File.Exists(_dbPath))
            File.Delete(_dbPath);
    }
}
```

---

### Performance Benchmarks

| Benchmark | Target | Maximum | Measurement Method |
|-----------|--------|---------|-------------------|
| Add single dependency | 1ms | 5ms | Stopwatch.ElapsedMilliseconds |
| Batch add 1000 edges | 50ms | 100ms | Stopwatch.ElapsedMilliseconds |
| Batch add 10000 edges | 500ms | 1000ms | Stopwatch.ElapsedMilliseconds |
| Query by source (single) | 2ms | 10ms | Stopwatch.ElapsedMilliseconds |
| Query by source (100 edges) | 5ms | 10ms | Stopwatch.ElapsedMilliseconds |
| Query by target (single) | 2ms | 10ms | Stopwatch.ElapsedMilliseconds |
| Transitive depth 3 | 20ms | 50ms | Stopwatch.ElapsedMilliseconds |
| Transitive depth 5 | 50ms | 100ms | Stopwatch.ElapsedMilliseconds |
| Path finding (direct) | 5ms | 20ms | Stopwatch.ElapsedMilliseconds |
| Path finding (depth 5) | 30ms | 100ms | Stopwatch.ElapsedMilliseconds |
| Memory per 1000 edges | 100KB | 500KB | GC.GetTotalMemory(true) delta |

---

## User Verification Steps

### Scenario 1: Query Direct Dependencies of a Class

**Objective:** Verify that `acode deps of` correctly returns all dependencies of a class.

**Prerequisites:**
- Acode CLI installed and in PATH
- Sample C# project with services and repositories

**Setup:**
```csharp
// Create file: src/Services/OrderService.cs
using OrderApp.Repositories;
using OrderApp.Models;
using Microsoft.Extensions.Logging;

namespace OrderApp.Services;

public class OrderService : IOrderService
{
    private readonly IOrderRepository _repository;
    private readonly ILogger<OrderService> _logger;
    
    public OrderService(IOrderRepository repository, ILogger<OrderService> logger)
    {
        _repository = repository;
        _logger = logger;
    }
    
    public async Task<Order> GetByIdAsync(Guid id)
    {
        _logger.LogInformation("Getting order {OrderId}", id);
        return await _repository.GetByIdAsync(id);
    }
}
```

**Steps:**
1. Index the project:
   ```bash
   cd /path/to/OrderApp
   acode index src/
   ```
2. Query dependencies:
   ```bash
   acode deps of OrderService
   ```

**Expected Output:**
```
Dependencies of OrderService (5 total):

Implements (1):
  ◁ IOrderService                 [src/Interfaces/IOrderService.cs:1]

Uses (3):
  ◇ IOrderRepository              [src/Interfaces/IOrderRepository.cs:1]
  ◇ ILogger<OrderService>         [Microsoft.Extensions.Logging]
  ◇ Order                         [src/Models/Order.cs:1]

Calls (2):
  → IOrderRepository.GetByIdAsync [src/Interfaces/IOrderRepository.cs:15]
  → ILogger.LogInformation        [Microsoft.Extensions.Logging]
```

**Verification Checklist:**
- [ ] All interface implementations listed as Implements
- [ ] Constructor parameters listed as Uses
- [ ] Method calls listed as Calls
- [ ] File locations displayed for each dependency

---

### Scenario 2: Query Dependents (What Uses This)

**Objective:** Verify that `acode deps on` correctly returns all symbols that depend on a given class.

**Prerequisites:**
- Indexed project with OrderService from Scenario 1
- Controllers that use OrderService

**Setup:**
```csharp
// Create file: src/Controllers/OrderController.cs
using OrderApp.Services;

namespace OrderApp.Controllers;

public class OrderController
{
    private readonly IOrderService _orderService;
    
    public OrderController(IOrderService orderService)
    {
        _orderService = orderService;
    }
    
    public async Task<Order> Get(Guid id)
    {
        return await _orderService.GetByIdAsync(id);
    }
}
```

**Steps:**
1. Ensure project is indexed:
   ```bash
   acode index src/
   ```
2. Query dependents:
   ```bash
   acode deps on OrderService
   ```

**Expected Output:**
```
Dependents of OrderService (3 total):

Used By (2):
  ◇ OrderController               [src/Controllers/OrderController.cs:8]
  ◇ OrderServiceTests             [tests/OrderServiceTests.cs:12]

Implements:
  OrderService implements IOrderService

Inherited By (0):
  (none)
```

**Verification Checklist:**
- [ ] All classes using OrderService listed
- [ ] Includes both production and test code
- [ ] File locations displayed

---

### Scenario 3: View Call Graph with Depth

**Objective:** Verify that `acode deps calls` displays a hierarchical call tree.

**Prerequisites:**
- Indexed project with OrderService and OrderRepository

**Steps:**
1. Query call graph:
   ```bash
   acode deps calls OrderController.Get --depth 3
   ```

**Expected Output:**
```
Call Graph for OrderController.Get (depth: 3)

OrderController.Get(Guid id) → Task<Order>
├── OrderService.GetByIdAsync(Guid id) → Task<Order>
│   ├── ILogger.LogInformation(string message, params object[] args)
│   └── OrderRepository.GetByIdAsync(Guid id) → Task<Order?>
│       └── DbContext.Orders.FindAsync(Guid id) → ValueTask<Order?>
└── (end)

Total calls: 4 | Max depth: 3 | Cycles: 0
```

**Verification Checklist:**
- [ ] Tree structure displayed correctly
- [ ] Method signatures shown
- [ ] Depth limited to specified value
- [ ] Total call count accurate

---

### Scenario 4: Find Path Between Symbols

**Objective:** Verify that `acode deps path` finds the shortest dependency path.

**Prerequisites:**
- Indexed project with multiple layers

**Steps:**
1. Query path:
   ```bash
   acode deps path OrderController OrderRepository
   ```

**Expected Output:**
```
Path from OrderController to OrderRepository (3 hops):

  OrderController
       │
       │ uses IOrderService / calls GetByIdAsync
       ▼
  OrderService
       │
       │ uses IOrderRepository / calls GetByIdAsync
       ▼
  OrderRepository

Path length: 3 | Relationship types: Uses, Calls
```

**Verification Checklist:**
- [ ] Path found and displayed
- [ ] Intermediate symbols shown
- [ ] Relationship types labeled
- [ ] Hop count accurate

---

### Scenario 5: Filter Dependencies by Kind

**Objective:** Verify that `--kind` flag filters results correctly.

**Prerequisites:**
- Indexed project from previous scenarios

**Steps:**
1. Query only Calls:
   ```bash
   acode deps of OrderService --kind calls
   ```

**Expected Output:**
```
Dependencies of OrderService (Calls only):

Calls (2):
  → IOrderRepository.GetByIdAsync [src/Interfaces/IOrderRepository.cs:15]
  → ILogger.LogInformation        [Microsoft.Extensions.Logging]
```

**Verification Checklist:**
- [ ] Only Calls dependencies shown
- [ ] Uses, Implements, Inherits excluded
- [ ] Count reflects filtered results

---

### Scenario 6: Handle Circular Dependencies

**Objective:** Verify that cycles are detected and reported without hanging.

**Prerequisites:**
- Project with circular dependency

**Setup:**
```csharp
// ServiceA.cs
public class ServiceA { 
    public ServiceA(ServiceB b) { } 
}

// ServiceB.cs
public class ServiceB { 
    public ServiceB(ServiceA a) { } 
}
```

**Steps:**
1. Index and query:
   ```bash
   acode index src/
   acode deps of ServiceA --depth 5
   ```

**Expected Output:**
```
Dependencies of ServiceA (depth: 5):

Uses (1):
  ◇ ServiceB                      [src/ServiceB.cs:1]
    └── Uses: ServiceA            [CYCLE DETECTED]

⚠️  Circular dependency detected: ServiceA → ServiceB → ServiceA
```

**Verification Checklist:**
- [ ] Query completes (no hang)
- [ ] Cycle detected and labeled
- [ ] Warning message displayed

---

### Scenario 7: Query Interface Implementors

**Objective:** Verify that `acode deps implementors` finds all implementations.

**Prerequisites:**
- Indexed project with interface and multiple implementations

**Setup:**
```csharp
// IPaymentProcessor.cs
public interface IPaymentProcessor { Task ProcessAsync(Payment p); }

// StripeProcessor.cs
public class StripeProcessor : IPaymentProcessor { ... }

// PayPalProcessor.cs
public class PayPalProcessor : IPaymentProcessor { ... }

// MockProcessor.cs (tests)
public class MockProcessor : IPaymentProcessor { ... }
```

**Steps:**
1. Query implementors:
   ```bash
   acode deps implementors IPaymentProcessor
   ```

**Expected Output:**
```
Implementors of IPaymentProcessor (3 total):

Classes:
  ◁ StripeProcessor               [src/Payments/StripeProcessor.cs:1]
  ◁ PayPalProcessor               [src/Payments/PayPalProcessor.cs:1]
  ◁ MockProcessor                 [tests/Mocks/MockProcessor.cs:1]

Summary: 2 production, 1 test
```

**Verification Checklist:**
- [ ] All implementations found
- [ ] Both production and test code included
- [ ] File locations displayed

---

### Scenario 8: JSON Output Format

**Objective:** Verify that `--format json` produces machine-readable output.

**Prerequisites:**
- Indexed project from previous scenarios

**Steps:**
1. Query with JSON format:
   ```bash
   acode deps of OrderService --format json
   ```

**Expected Output:**
```json
{
  "symbol": "OrderService",
  "symbolId": "a1b2c3d4-...",
  "totalDependencies": 5,
  "dependencies": [
    {
      "kind": "Implements",
      "targetSymbol": "IOrderService",
      "targetSymbolId": "e5f6g7h8-...",
      "location": {
        "filePath": "src/Interfaces/IOrderService.cs",
        "line": 1,
        "column": 1
      }
    },
    {
      "kind": "Uses",
      "targetSymbol": "IOrderRepository",
      "targetSymbolId": "i9j0k1l2-...",
      "location": {
        "filePath": "src/Interfaces/IOrderRepository.cs",
        "line": 1,
        "column": 1
      }
    }
  ]
}
```

**Verification Checklist:**
- [ ] Valid JSON output
- [ ] All dependencies included
- [ ] Symbol IDs present for programmatic use
- [ ] Location data included

---

## Implementation Prompt

### File Structure

```
src/Acode.Domain/
├── Symbols/
│   ├── IDependency.cs
│   ├── Dependency.cs
│   ├── DependencyKind.cs
│   ├── IDependencyStore.cs
│   └── IDependencyGraph.cs
│
src/Acode.Infrastructure/
├── Symbols/
│   └── Dependencies/
│       ├── DependencyStore.cs
│       ├── DependencyExtractor.cs
│       ├── DependencyGraph.cs
│       └── DependencyQueryService.cs
│
src/Acode.Application/
├── Interfaces/
│   └── IDependencyQueryService.cs
├── DTOs/
│   ├── DependencyResult.cs
│   └── CallGraphNode.cs
│
src/Acode.Cli/
├── Commands/
│   └── DepsCommand.cs
```

---

### Domain Layer - Complete Implementations

#### IDependency.cs

```csharp
using System;

namespace Acode.Domain.Symbols;

/// <summary>
/// Represents a dependency relationship between two symbols.
/// </summary>
public interface IDependency
{
    /// <summary>Unique identifier for this dependency edge.</summary>
    Guid Id { get; }
    
    /// <summary>The symbol that has the dependency (the user/caller).</summary>
    Guid SourceSymbolId { get; }
    
    /// <summary>The symbol being depended on (the used/called).</summary>
    Guid TargetSymbolId { get; }
    
    /// <summary>The type of dependency relationship.</summary>
    DependencyKind Kind { get; }
    
    /// <summary>Source code location where the dependency occurs.</summary>
    SymbolLocation? Location { get; }
}
```

---

#### Dependency.cs

```csharp
using System;

namespace Acode.Domain.Symbols;

/// <summary>
/// Concrete implementation of a dependency relationship.
/// </summary>
public sealed class Dependency : IDependency, IEquatable<Dependency>
{
    public Guid Id { get; }
    public Guid SourceSymbolId { get; }
    public Guid TargetSymbolId { get; }
    public DependencyKind Kind { get; }
    public SymbolLocation? Location { get; }

    public Dependency(
        Guid sourceSymbolId,
        Guid targetSymbolId,
        DependencyKind kind,
        SymbolLocation? location)
    {
        Id = Guid.NewGuid();
        SourceSymbolId = sourceSymbolId;
        TargetSymbolId = targetSymbolId;
        Kind = kind;
        Location = location;
    }

    // Constructor for database hydration
    public Dependency(
        Guid id,
        Guid sourceSymbolId,
        Guid targetSymbolId,
        DependencyKind kind,
        SymbolLocation? location)
    {
        Id = id;
        SourceSymbolId = sourceSymbolId;
        TargetSymbolId = targetSymbolId;
        Kind = kind;
        Location = location;
    }

    public bool Equals(Dependency? other)
    {
        if (other is null) return false;
        return SourceSymbolId == other.SourceSymbolId &&
               TargetSymbolId == other.TargetSymbolId &&
               Kind == other.Kind;
    }

    public override bool Equals(object? obj) => Equals(obj as Dependency);

    public override int GetHashCode() => HashCode.Combine(SourceSymbolId, TargetSymbolId, Kind);
}
```

---

#### DependencyKind.cs

```csharp
namespace Acode.Domain.Symbols;

/// <summary>
/// Types of dependency relationships between symbols.
/// </summary>
public enum DependencyKind
{
    /// <summary>Method or function call invocation.</summary>
    Calls = 1,
    
    /// <summary>Type reference (parameter, return, variable type).</summary>
    Uses = 2,
    
    /// <summary>Class inheritance (base class).</summary>
    Inherits = 3,
    
    /// <summary>Interface implementation.</summary>
    Implements = 4,
    
    /// <summary>General reference (property, field, constant).</summary>
    References = 5
}
```

---

#### IDependencyStore.cs

```csharp
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Acode.Domain.Symbols;

/// <summary>
/// Persistence interface for dependency edges.
/// </summary>
public interface IDependencyStore : IDisposable
{
    Task AddAsync(IDependency dependency, CancellationToken ct = default);
    Task AddBatchAsync(IEnumerable<IDependency> dependencies, CancellationToken ct = default);
    Task RemoveAsync(Guid dependencyId, CancellationToken ct = default);
    Task RemoveBySourceAsync(Guid sourceSymbolId, CancellationToken ct = default);
    Task RemoveByTargetAsync(Guid targetSymbolId, CancellationToken ct = default);
    Task RemoveByFileAsync(string filePath, CancellationToken ct = default);
    Task<IReadOnlyList<IDependency>> GetBySourceAsync(Guid sourceSymbolId, CancellationToken ct = default);
    Task<IReadOnlyList<IDependency>> GetByTargetAsync(Guid targetSymbolId, CancellationToken ct = default);
    Task<IReadOnlyList<IDependency>> GetBySourceAndKindAsync(Guid sourceSymbolId, DependencyKind kind, CancellationToken ct = default);
    Task<long> GetEdgeCountAsync(CancellationToken ct = default);
}
```

---

### Infrastructure Layer - Complete Implementations

#### DependencyStore.cs

```csharp
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Acode.Domain.Symbols;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;

namespace Acode.Infrastructure.Symbols.Dependencies;

/// <summary>
/// SQLite-based persistence for dependency edges.
/// </summary>
public sealed class DependencyStore : IDependencyStore
{
    private readonly string _connectionString;
    private readonly ILogger<DependencyStore> _logger;
    private readonly SemaphoreSlim _writeLock = new(1, 1);

    public DependencyStore(string databasePath, ILogger<DependencyStore> logger)
    {
        _connectionString = $"Data Source={databasePath}";
        _logger = logger;
        InitializeDatabase();
    }

    private void InitializeDatabase()
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();
        
        using var command = connection.CreateCommand();
        command.CommandText = @"
            CREATE TABLE IF NOT EXISTS dependency_edges (
                id TEXT PRIMARY KEY,
                source_symbol_id TEXT NOT NULL,
                target_symbol_id TEXT NOT NULL,
                kind INTEGER NOT NULL,
                file_path TEXT,
                line_number INTEGER,
                column_number INTEGER,
                created_at TEXT NOT NULL DEFAULT (datetime('now'))
            );
            
            CREATE INDEX IF NOT EXISTS idx_edges_source ON dependency_edges(source_symbol_id);
            CREATE INDEX IF NOT EXISTS idx_edges_target ON dependency_edges(target_symbol_id);
            CREATE INDEX IF NOT EXISTS idx_edges_kind ON dependency_edges(kind);
            CREATE INDEX IF NOT EXISTS idx_edges_file ON dependency_edges(file_path);
            CREATE INDEX IF NOT EXISTS idx_edges_source_kind ON dependency_edges(source_symbol_id, kind);
        ";
        command.ExecuteNonQuery();
    }

    public async Task AddAsync(IDependency dependency, CancellationToken ct = default)
    {
        await _writeLock.WaitAsync(ct);
        try
        {
            await using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync(ct);
            
            await using var command = connection.CreateCommand();
            command.CommandText = @"
                INSERT OR REPLACE INTO dependency_edges 
                (id, source_symbol_id, target_symbol_id, kind, file_path, line_number, column_number)
                VALUES (@id, @source, @target, @kind, @file, @line, @col)";
            
            command.Parameters.AddWithValue("@id", dependency.Id.ToString());
            command.Parameters.AddWithValue("@source", dependency.SourceSymbolId.ToString());
            command.Parameters.AddWithValue("@target", dependency.TargetSymbolId.ToString());
            command.Parameters.AddWithValue("@kind", (int)dependency.Kind);
            command.Parameters.AddWithValue("@file", dependency.Location?.FilePath ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@line", dependency.Location?.Line ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@col", dependency.Location?.Column ?? (object)DBNull.Value);
            
            await command.ExecuteNonQueryAsync(ct);
        }
        finally
        {
            _writeLock.Release();
        }
    }

    public async Task AddBatchAsync(IEnumerable<IDependency> dependencies, CancellationToken ct = default)
    {
        await _writeLock.WaitAsync(ct);
        try
        {
            await using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync(ct);
            
            await using var transaction = await connection.BeginTransactionAsync(ct);
            
            foreach (var dep in dependencies)
            {
                await using var command = connection.CreateCommand();
                command.CommandText = @"
                    INSERT OR REPLACE INTO dependency_edges 
                    (id, source_symbol_id, target_symbol_id, kind, file_path, line_number, column_number)
                    VALUES (@id, @source, @target, @kind, @file, @line, @col)";
                
                command.Parameters.AddWithValue("@id", dep.Id.ToString());
                command.Parameters.AddWithValue("@source", dep.SourceSymbolId.ToString());
                command.Parameters.AddWithValue("@target", dep.TargetSymbolId.ToString());
                command.Parameters.AddWithValue("@kind", (int)dep.Kind);
                command.Parameters.AddWithValue("@file", dep.Location?.FilePath ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@line", dep.Location?.Line ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@col", dep.Location?.Column ?? (object)DBNull.Value);
                
                await command.ExecuteNonQueryAsync(ct);
            }
            
            await transaction.CommitAsync(ct);
        }
        finally
        {
            _writeLock.Release();
        }
    }

    public async Task<IReadOnlyList<IDependency>> GetBySourceAsync(Guid sourceSymbolId, CancellationToken ct = default)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(ct);
        
        await using var command = connection.CreateCommand();
        command.CommandText = @"
            SELECT id, source_symbol_id, target_symbol_id, kind, file_path, line_number, column_number
            FROM dependency_edges WHERE source_symbol_id = @source";
        command.Parameters.AddWithValue("@source", sourceSymbolId.ToString());
        
        return await ReadDependenciesAsync(command, ct);
    }

    public async Task<IReadOnlyList<IDependency>> GetByTargetAsync(Guid targetSymbolId, CancellationToken ct = default)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(ct);
        
        await using var command = connection.CreateCommand();
        command.CommandText = @"
            SELECT id, source_symbol_id, target_symbol_id, kind, file_path, line_number, column_number
            FROM dependency_edges WHERE target_symbol_id = @target";
        command.Parameters.AddWithValue("@target", targetSymbolId.ToString());
        
        return await ReadDependenciesAsync(command, ct);
    }

    public async Task<IReadOnlyList<IDependency>> GetBySourceAndKindAsync(
        Guid sourceSymbolId, 
        DependencyKind kind, 
        CancellationToken ct = default)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(ct);
        
        await using var command = connection.CreateCommand();
        command.CommandText = @"
            SELECT id, source_symbol_id, target_symbol_id, kind, file_path, line_number, column_number
            FROM dependency_edges WHERE source_symbol_id = @source AND kind = @kind";
        command.Parameters.AddWithValue("@source", sourceSymbolId.ToString());
        command.Parameters.AddWithValue("@kind", (int)kind);
        
        return await ReadDependenciesAsync(command, ct);
    }

    public async Task RemoveAsync(Guid dependencyId, CancellationToken ct = default)
    {
        await _writeLock.WaitAsync(ct);
        try
        {
            await using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync(ct);
            
            await using var command = connection.CreateCommand();
            command.CommandText = "DELETE FROM dependency_edges WHERE id = @id";
            command.Parameters.AddWithValue("@id", dependencyId.ToString());
            
            await command.ExecuteNonQueryAsync(ct);
        }
        finally
        {
            _writeLock.Release();
        }
    }

    public async Task RemoveBySourceAsync(Guid sourceSymbolId, CancellationToken ct = default)
    {
        await _writeLock.WaitAsync(ct);
        try
        {
            await using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync(ct);
            
            await using var command = connection.CreateCommand();
            command.CommandText = "DELETE FROM dependency_edges WHERE source_symbol_id = @source";
            command.Parameters.AddWithValue("@source", sourceSymbolId.ToString());
            
            var deleted = await command.ExecuteNonQueryAsync(ct);
            _logger.LogDebug("Removed {Count} edges from source {SourceId}", deleted, sourceSymbolId);
        }
        finally
        {
            _writeLock.Release();
        }
    }

    public async Task RemoveByTargetAsync(Guid targetSymbolId, CancellationToken ct = default)
    {
        await _writeLock.WaitAsync(ct);
        try
        {
            await using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync(ct);
            
            await using var command = connection.CreateCommand();
            command.CommandText = "DELETE FROM dependency_edges WHERE target_symbol_id = @target";
            command.Parameters.AddWithValue("@target", targetSymbolId.ToString());
            
            await command.ExecuteNonQueryAsync(ct);
        }
        finally
        {
            _writeLock.Release();
        }
    }

    public async Task RemoveByFileAsync(string filePath, CancellationToken ct = default)
    {
        await _writeLock.WaitAsync(ct);
        try
        {
            await using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync(ct);
            
            await using var command = connection.CreateCommand();
            command.CommandText = "DELETE FROM dependency_edges WHERE file_path = @file";
            command.Parameters.AddWithValue("@file", filePath);
            
            var deleted = await command.ExecuteNonQueryAsync(ct);
            _logger.LogDebug("Removed {Count} edges from file {FilePath}", deleted, filePath);
        }
        finally
        {
            _writeLock.Release();
        }
    }

    public async Task<long> GetEdgeCountAsync(CancellationToken ct = default)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(ct);
        
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM dependency_edges";
        
        return (long)(await command.ExecuteScalarAsync(ct))!;
    }

    private static async Task<IReadOnlyList<IDependency>> ReadDependenciesAsync(
        SqliteCommand command, 
        CancellationToken ct)
    {
        var results = new List<Dependency>();
        
        await using var reader = await command.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            var location = reader.IsDBNull(4) ? null : new SymbolLocation(
                reader.GetString(4),
                reader.IsDBNull(5) ? 0 : reader.GetInt32(5),
                reader.IsDBNull(6) ? 0 : reader.GetInt32(6));
            
            results.Add(new Dependency(
                Guid.Parse(reader.GetString(0)),
                Guid.Parse(reader.GetString(1)),
                Guid.Parse(reader.GetString(2)),
                (DependencyKind)reader.GetInt32(3),
                location));
        }
        
        return results;
    }

    public void Dispose()
    {
        _writeLock.Dispose();
    }
}
```

---

#### DependencyGraph.cs

```csharp
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Acode.Domain.Symbols;
using Microsoft.Extensions.Logging;

namespace Acode.Infrastructure.Symbols.Dependencies;

/// <summary>
/// Graph operations for dependency traversal.
/// </summary>
public sealed class DependencyGraph : IDependencyGraph
{
    private readonly IDependencyStore _store;
    private readonly ILogger<DependencyGraph> _logger;

    public DependencyGraph(IDependencyStore store, ILogger<DependencyGraph> logger)
    {
        _store = store;
        _logger = logger;
    }

    public Task<IReadOnlyList<IDependency>> GetDependenciesAsync(Guid symbolId, CancellationToken ct = default)
    {
        return _store.GetBySourceAsync(symbolId, ct);
    }

    public Task<IReadOnlyList<IDependency>> GetDependentsAsync(Guid symbolId, CancellationToken ct = default)
    {
        return _store.GetByTargetAsync(symbolId, ct);
    }

    public async Task<IReadOnlyList<IDependency>> GetTransitiveDependenciesAsync(
        Guid symbolId,
        int maxDepth,
        CancellationToken ct = default)
    {
        var results = new List<IDependency>();
        var visited = new HashSet<Guid>();
        var queue = new Queue<(Guid SymbolId, int Depth)>();
        
        queue.Enqueue((symbolId, 0));
        visited.Add(symbolId);
        
        while (queue.Count > 0)
        {
            ct.ThrowIfCancellationRequested();
            
            var (currentId, depth) = queue.Dequeue();
            
            if (depth >= maxDepth) continue;
            
            var deps = await _store.GetBySourceAsync(currentId, ct);
            
            foreach (var dep in deps)
            {
                results.Add(dep);
                
                if (!visited.Contains(dep.TargetSymbolId))
                {
                    visited.Add(dep.TargetSymbolId);
                    queue.Enqueue((dep.TargetSymbolId, depth + 1));
                }
            }
        }
        
        return results;
    }

    public async Task<IReadOnlyList<Guid>?> FindPathAsync(
        Guid fromId,
        Guid toId,
        CancellationToken ct = default)
    {
        if (fromId == toId) return new[] { fromId };
        
        var visited = new HashSet<Guid>();
        var parent = new Dictionary<Guid, Guid>();
        var queue = new Queue<Guid>();
        
        queue.Enqueue(fromId);
        visited.Add(fromId);
        
        while (queue.Count > 0)
        {
            ct.ThrowIfCancellationRequested();
            
            var current = queue.Dequeue();
            var deps = await _store.GetBySourceAsync(current, ct);
            
            foreach (var dep in deps)
            {
                if (visited.Contains(dep.TargetSymbolId)) continue;
                
                visited.Add(dep.TargetSymbolId);
                parent[dep.TargetSymbolId] = current;
                
                if (dep.TargetSymbolId == toId)
                {
                    return ReconstructPath(parent, fromId, toId);
                }
                
                queue.Enqueue(dep.TargetSymbolId);
            }
        }
        
        return null; // No path found
    }

    public async Task<bool> HasCycleAsync(Guid symbolId, CancellationToken ct = default)
    {
        var visiting = new HashSet<Guid>();
        var visited = new HashSet<Guid>();
        
        return await HasCycleDfsAsync(symbolId, visiting, visited, ct);
    }

    private async Task<bool> HasCycleDfsAsync(
        Guid symbolId,
        HashSet<Guid> visiting,
        HashSet<Guid> visited,
        CancellationToken ct)
    {
        if (visited.Contains(symbolId)) return false;
        if (visiting.Contains(symbolId)) return true; // Cycle detected
        
        visiting.Add(symbolId);
        
        var deps = await _store.GetBySourceAsync(symbolId, ct);
        
        foreach (var dep in deps)
        {
            if (await HasCycleDfsAsync(dep.TargetSymbolId, visiting, visited, ct))
            {
                return true;
            }
        }
        
        visiting.Remove(symbolId);
        visited.Add(symbolId);
        
        return false;
    }

    private static IReadOnlyList<Guid> ReconstructPath(
        Dictionary<Guid, Guid> parent,
        Guid start,
        Guid end)
    {
        var path = new List<Guid> { end };
        var current = end;
        
        while (current != start)
        {
            current = parent[current];
            path.Add(current);
        }
        
        path.Reverse();
        return path;
    }
}
```

---

### Error Codes

| Code | Meaning | User Action |
|------|---------|-------------|
| ACODE-DEP-001 | Symbol not found in index | Run `acode index` to index the symbol |
| ACODE-DEP-002 | Query timeout exceeded | Reduce `--depth` or `--limit` parameters |
| ACODE-DEP-003 | Cycle detected in graph | Informational - query still returns results |
| ACODE-DEP-004 | Maximum depth exceeded | Reduce `--depth` parameter |
| ACODE-DEP-005 | Maximum nodes exceeded | Reduce `--limit` parameter or filter by `--kind` |

---

### Implementation Checklist

1. [ ] Create `IDependency` interface in Domain layer
2. [ ] Create `Dependency` class with equality implementation
3. [ ] Create `DependencyKind` enum with all relationship types
4. [ ] Create `IDependencyStore` interface in Domain layer
5. [ ] Implement `DependencyStore` with SQLite persistence
6. [ ] Create database schema with optimized indexes
7. [ ] Implement batch add with transaction support
8. [ ] Create `IDependencyGraph` interface in Domain layer
9. [ ] Implement `DependencyGraph` with BFS traversal
10. [ ] Implement cycle detection with DFS
11. [ ] Implement path finding with BFS
12. [ ] Create `IDependencyQueryService` in Application layer
13. [ ] Implement CLI `deps of` command
14. [ ] Implement CLI `deps on` command
15. [ ] Implement CLI `deps calls` command
16. [ ] Implement CLI `deps path` command
17. [ ] Add `--depth` and `--kind` CLI flags
18. [ ] Add `--format json` output option
19. [ ] Register services in DI container
20. [ ] Add unit tests for all components
21. [ ] Add integration tests with real database
22. [ ] Add performance benchmarks

---

### Rollout Plan

| Phase | Components | Duration | Dependencies |
|-------|------------|----------|--------------|
| Phase 1 | Domain models, enums | 1 day | None |
| Phase 2 | DependencyStore with SQLite | 2 days | Phase 1 |
| Phase 3 | DependencyGraph traversal | 2 days | Phase 2 |
| Phase 4 | Query service and DTOs | 1 day | Phase 3 |
| Phase 5 | CLI commands | 1 day | Phase 4 |
| Phase 6 | Testing and benchmarks | 2 days | Phase 5 |

**Total estimated time:** 9 days

---

**End of Task 017.c Specification**