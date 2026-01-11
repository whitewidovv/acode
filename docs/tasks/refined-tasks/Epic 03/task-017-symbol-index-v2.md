# Task 017: Symbol Index v2

**Priority:** P0 – Critical  
**Tier:** S – Core Infrastructure  
**Complexity:** 13 (Fibonacci points)  
**Phase:** Phase 3 – Intelligence Layer  
**Dependencies:** Task 014 (RepoFS), Task 015 (Text Index), Task 016 (Context Packer), Task 050 (Workspace DB)  

---

## Description

### Business Value

Symbol Index v2 elevates the agent from text-level to semantic-level code understanding. While Task 015's text index finds strings, Symbol Index finds meaning—classes, methods, properties, and their relationships.

This semantic understanding provides:

1. **Precise Code Navigation:** When the agent needs `GetUserById`, symbol search returns the method definition, not every file containing those characters. This precision dramatically improves context quality.

2. **Relationship Awareness:** Symbols have relationships—methods belong to classes, classes implement interfaces, functions call other functions. This enables the agent to understand code structure, not just content.

3. **Language-Aware Indexing:** Each programming language has unique symbol types and conventions. Language-specific extractors (Roslyn for C#, TypeScript Compiler API for TS) provide accurate semantic analysis.

4. **Incremental Efficiency:** Only changed files are re-analyzed. The symbol index stays current with minimal overhead, enabling real-time updates as developers work.

5. **Context Enhancement:** Symbol-based retrieval provides more precise context to the LLM. Instead of "file containing UserService," the agent gets "the CreateUser method in UserService."

### Return on Investment (ROI)

Symbol Index v2 delivers significant economic value through enhanced code intelligence:

#### Quantified Benefits

| Benefit Category | Annual Value | Calculation Basis |
|------------------|--------------|-------------------|
| **Reduced Context Window Waste** | **$180,000/year** | 60% reduction in token usage for code retrieval; avg 10 developers × 50 queries/day × $0.10/query saved × 250 days |
| **Faster Code Discovery** | **$120,000/year** | Semantic search is 5x faster than grep; 30 min/day saved × 10 developers × $80/hour × 250 days |
| **Improved LLM Response Quality** | **$75,000/year** | Precise context = better completions; 25% fewer regeneration cycles; 2 regenerations/day saved × $15/regeneration × 10 developers × 250 days |
| **Reduced Context Pollution** | **$45,000/year** | Symbol-level retrieval excludes irrelevant code; 15% fewer hallucinations; debugging time saved |
| **Code Navigation Accuracy** | **$30,000/year** | Jump-to-definition functionality; 10 min/day saved per developer × $80/hour × 250 days |
| **Total Annual Value** | **$450,000/year** | For a 10-developer team |

#### Break-Even Analysis

| Metric | Value |
|--------|-------|
| Implementation Cost | 2 developers × 4 weeks × $80/hour × 40 hours = $25,600 |
| Monthly Operational Cost | ~$200 (compute, storage) |
| Break-Even Point | < 1 month of production use |
| 3-Year ROI | ($450,000 × 3) - $25,600 - ($200 × 36) = $1,317,200 (5,144% return) |

#### Strategic Value

1. **Foundation for Advanced Features:** Symbol relationships enable call graph analysis, dependency mapping, impact analysis—high-value features that require semantic understanding.

2. **Language Extensibility:** Extractor architecture allows adding new languages without modifying core infrastructure—future-proofing the investment.

3. **Competitive Differentiation:** Semantic code intelligence differentiates from basic text search tools, enabling premium positioning.

### Architecture Diagram

```
┌─────────────────────────────────────────────────────────────────────────────────┐
│                              SYMBOL INDEX v2 ARCHITECTURE                       │
├─────────────────────────────────────────────────────────────────────────────────┤
│                                                                                 │
│  ┌─────────────────────────────────────────────────────────────────────────┐   │
│  │                           CLI / API Layer                                │   │
│  │  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐ │   │
│  │  │symbols build │  │symbols search│  │symbols update│  │symbols status│ │   │
│  │  └──────┬───────┘  └──────┬───────┘  └──────┬───────┘  └──────┬───────┘ │   │
│  └─────────┼─────────────────┼─────────────────┼─────────────────┼─────────┘   │
│            │                 │                 │                 │             │
│            ▼                 ▼                 ▼                 ▼             │
│  ┌─────────────────────────────────────────────────────────────────────────┐   │
│  │                        INDEX SERVICE (ISymbolIndex)                      │   │
│  │  ┌─────────────────┐  ┌─────────────────┐  ┌─────────────────────────┐  │   │
│  │  │  Full Build     │  │  Incremental    │  │  Status/Progress        │  │   │
│  │  │  Orchestrator   │  │  Update Engine  │  │  Reporter               │  │   │
│  │  └────────┬────────┘  └────────┬────────┘  └────────────┬────────────┘  │   │
│  │           │                    │                        │               │   │
│  │           ▼                    ▼                        │               │   │
│  │  ┌─────────────────────────────────────────┐           │               │   │
│  │  │       Change Detection Engine           │           │               │   │
│  │  │  ┌──────────────┐  ┌──────────────────┐ │           │               │   │
│  │  │  │ Hash Tracker │  │ Modified Files   │ │           │               │   │
│  │  │  │ (SHA256)     │  │ Detector         │ │           │               │   │
│  │  │  └──────────────┘  └──────────────────┘ │           │               │   │
│  │  └───────────────────────┬─────────────────┘           │               │   │
│  └──────────────────────────┼─────────────────────────────┼───────────────┘   │
│                             │                             │                   │
│                             ▼                             │                   │
│  ┌─────────────────────────────────────────────────────────────────────────┐   │
│  │                    EXTRACTOR LAYER                                       │   │
│  │                                                                          │   │
│  │  ┌──────────────────────────────────────────────────────────────────┐   │   │
│  │  │                  Extractor Registry (IExtractorRegistry)          │   │   │
│  │  │  ┌──────────────────────────────────────────────────────────┐    │   │   │
│  │  │  │  Extension → Extractor Mapping                           │    │   │   │
│  │  │  │  .cs      → CSharpExtractor (Roslyn)                     │    │   │   │
│  │  │  │  .ts      → TypeScriptExtractor (TS Compiler API)        │    │   │   │
│  │  │  │  .js      → JavaScriptExtractor (TS Compiler API)        │    │   │   │
│  │  │  │  .*       → FallbackExtractor (minimal/none)             │    │   │   │
│  │  │  └──────────────────────────────────────────────────────────┘    │   │   │
│  │  └──────────────────────────────────────────────────────────────────┘   │   │
│  │                                                                          │   │
│  │  ┌────────────────┐  ┌────────────────┐  ┌────────────────────────────┐ │   │
│  │  │ ISymbolExtractor│  │ ISymbolExtractor│  │ ISymbolExtractor          │ │   │
│  │  │ (C# - Roslyn)   │  │ (TS - tsserver) │  │ (JS - tsserver)           │ │   │
│  │  │                 │  │                 │  │                            │ │   │
│  │  │ ┌─────────────┐ │  │ ┌─────────────┐ │  │ ┌────────────────────────┐│ │   │
│  │  │ │ SemanticModel│ │  │ │ AST Parser  │ │  │ │ AST Parser            ││ │   │
│  │  │ │ SyntaxTree   │ │  │ │ Symbol Table│ │  │ │ Symbol Table          ││ │   │
│  │  │ │ Symbol Walker│ │  │ │ Type Checker│ │  │ │ Type Checker          ││ │   │
│  │  │ └─────────────┘ │  │ └─────────────┘ │  │ └────────────────────────┘│ │   │
│  │  └────────────────┘  └────────────────┘  └────────────────────────────┘ │   │
│  └──────────────────────────────────────────────────────────────────────────┘   │
│                             │                                                   │
│                             ▼                                                   │
│  ┌─────────────────────────────────────────────────────────────────────────┐   │
│  │                        SYMBOL MODEL LAYER                                │   │
│  │                                                                          │   │
│  │  ┌──────────────────────────────────────────────────────────────────┐   │   │
│  │  │                      ISymbol                                      │   │   │
│  │  │  ┌────────────┐  ┌────────────────────┐  ┌────────────────────┐  │   │   │
│  │  │  │ Id (Guid)  │  │ FullyQualifiedName │  │ Kind (SymbolKind)  │  │   │   │
│  │  │  │ Name       │  │ Signature          │  │ Visibility         │  │   │   │
│  │  │  │ Location   │  │ ContainingSymbolId │  │ Language           │  │   │   │
│  │  │  └────────────┘  └────────────────────┘  └────────────────────┘  │   │   │
│  │  └──────────────────────────────────────────────────────────────────┘   │   │
│  │                                                                          │   │
│  │  ┌───────────────────┐  ┌────────────────────────────────────────────┐  │   │
│  │  │     SymbolKind    │  │              SymbolLocation                 │  │   │
│  │  │  ├─ Namespace     │  │  ┌──────────┐  ┌──────────┐  ┌──────────┐  │  │   │
│  │  │  ├─ Class         │  │  │ FilePath │  │ StartLine│  │ EndLine  │  │  │   │
│  │  │  ├─ Interface     │  │  │          │  │ StartCol │  │ EndCol   │  │  │   │
│  │  │  ├─ Struct        │  │  └──────────┘  └──────────┘  └──────────┘  │  │   │
│  │  │  ├─ Enum          │  └────────────────────────────────────────────┘  │   │
│  │  │  ├─ Method        │                                                   │   │
│  │  │  ├─ Property      │                                                   │   │
│  │  │  ├─ Field         │                                                   │   │
│  │  │  ├─ Constructor   │                                                   │   │
│  │  │  ├─ Function      │                                                   │   │
│  │  │  ├─ Variable      │                                                   │   │
│  │  │  └─ TypeAlias     │                                                   │   │
│  │  └───────────────────┘                                                   │   │
│  └──────────────────────────────────────────────────────────────────────────┘   │
│                             │                                                   │
│                             ▼                                                   │
│  ┌─────────────────────────────────────────────────────────────────────────┐   │
│  │                    QUERY LAYER (ISymbolQuery)                            │   │
│  │                                                                          │   │
│  │  ┌────────────────────────────────────────────────────────────────────┐ │   │
│  │  │                        Query Builder                                │ │   │
│  │  │  ┌──────────────┐  ┌──────────────┐  ┌──────────────────────────┐  │ │   │
│  │  │  │ ExactMatch   │  │ PrefixMatch  │  │ FuzzyMatch (Levenshtein) │  │ │   │
│  │  │  └──────────────┘  └──────────────┘  └──────────────────────────┘  │ │   │
│  │  │  ┌──────────────┐  ┌──────────────┐  ┌──────────────────────────┐  │ │   │
│  │  │  │ KindFilter   │  │ FileFilter   │  │ NamespaceFilter          │  │ │   │
│  │  │  └──────────────┘  └──────────────┘  └──────────────────────────┘  │ │   │
│  │  │  ┌──────────────┐  ┌──────────────┐  ┌──────────────────────────┐  │ │   │
│  │  │  │VisibilityFilt│  │ ContainingFilt│  │ Pagination (skip/take)   │  │ │   │
│  │  │  └──────────────┘  └──────────────┘  └──────────────────────────┘  │ │   │
│  │  └────────────────────────────────────────────────────────────────────┘ │   │
│  └──────────────────────────────────────────────────────────────────────────┘   │
│                             │                                                   │
│                             ▼                                                   │
│  ┌─────────────────────────────────────────────────────────────────────────┐   │
│  │                    STORAGE LAYER (ISymbolStore)                          │   │
│  │                                                                          │   │
│  │  ┌────────────────────────────────────────────────────────────────────┐ │   │
│  │  │                    Symbol Store                                     │ │   │
│  │  │  ┌──────────────────┐  ┌──────────────────┐  ┌──────────────────┐  │ │   │
│  │  │  │ Add/AddBatch     │  │ Remove/RemoveFile│  │ Update           │  │ │   │
│  │  │  └──────────────────┘  └──────────────────┘  └──────────────────┘  │ │   │
│  │  │  ┌──────────────────┐  ┌──────────────────┐  ┌──────────────────┐  │ │   │
│  │  │  │ Query Interface  │  │ Get by ID        │  │ Get by File      │  │ │   │
│  │  │  └──────────────────┘  └──────────────────┘  └──────────────────┘  │ │   │
│  │  └────────────────────────────────────────────────────────────────────┘ │   │
│  │                             │                                            │   │
│  │  ┌────────────────────────────────────────────────────────────────────┐ │   │
│  │  │                 SQLite Persistence (Task 050)                       │ │   │
│  │  │  ┌────────────────┐  ┌────────────────┐  ┌────────────────────────┐│ │   │
│  │  │  │ symbols table  │  │ file_hashes    │  │ index_metadata         ││ │   │
│  │  │  │ - id           │  │ - file_path    │  │ - last_build_time      ││ │   │
│  │  │  │ - name         │  │ - hash         │  │ - symbol_count         ││ │   │
│  │  │  │ - fqn          │  │ - indexed_at   │  │ - file_count           ││ │   │
│  │  │  │ - kind         │  └────────────────┘  └────────────────────────┘│ │   │
│  │  │  │ - location_*   │                                                 │ │   │
│  │  │  │ - signature    │  Indexes: name, fqn, kind, file_path            │ │   │
│  │  │  │ - visibility   │  Connection pooling: WAL mode, pooled conns    │ │   │
│  │  │  │ - containing_id│                                                 │ │   │
│  │  │  └────────────────┘                                                 │ │   │
│  │  └────────────────────────────────────────────────────────────────────┘ │   │
│  └──────────────────────────────────────────────────────────────────────────┘   │
│                                                                                 │
│  ┌─────────────────────────────────────────────────────────────────────────┐   │
│  │                    DEPENDENCIES                                          │   │
│  │                                                                          │   │
│  │  ┌────────────────┐  ┌────────────────┐  ┌────────────────────────────┐ │   │
│  │  │ Task 014       │  │ Task 015       │  │ Task 016                   │ │   │
│  │  │ RepoFS         │  │ Text Index     │  │ Context Packer             │ │   │
│  │  │ (File Access)  │  │ (Complementary)│  │ (Consumes Symbols)         │ │   │
│  │  └────────────────┘  └────────────────┘  └────────────────────────────┘ │   │
│  │  ┌────────────────────────────────────────────────────────────────────┐ │   │
│  │  │ Task 050 - Workspace Database (SQLite Storage)                     │ │   │
│  │  └────────────────────────────────────────────────────────────────────┘ │   │
│  └──────────────────────────────────────────────────────────────────────────┘   │
│                                                                                 │
└─────────────────────────────────────────────────────────────────────────────────┘
```

### Data Flow

```
┌─────────────────────────────────────────────────────────────────────────────────┐
│                           SYMBOL INDEX DATA FLOW                                │
├─────────────────────────────────────────────────────────────────────────────────┤
│                                                                                 │
│  FULL BUILD FLOW:                                                               │
│  ═══════════════                                                                │
│                                                                                 │
│  ┌─────────┐    ┌─────────┐    ┌───────────┐    ┌──────────┐    ┌───────────┐ │
│  │ RepoFS  │───▶│ File    │───▶│ Extractor │───▶│ Symbol   │───▶│ Symbol    │ │
│  │ Walk    │    │ Filter  │    │ Registry  │    │ Model    │    │ Store     │ │
│  └─────────┘    └─────────┘    └───────────┘    └──────────┘    └───────────┘ │
│       │              │              │                 │              │         │
│       ▼              ▼              ▼                 ▼              ▼         │
│  All files     Apply ignore   Route to lang    Create ISymbol   Persist to    │
│  in repo       patterns       extractor        instances        SQLite        │
│                                                                                 │
│  INCREMENTAL UPDATE FLOW:                                                       │
│  ════════════════════════                                                       │
│                                                                                 │
│  ┌─────────┐    ┌─────────┐    ┌───────────┐    ┌──────────┐    ┌───────────┐ │
│  │ Change  │───▶│ Hash    │───▶│ Modified  │───▶│ Extract  │───▶│ Update    │ │
│  │ Detect  │    │ Compare │    │ Files     │    │ Symbols  │    │ Store     │ │
│  └─────────┘    └─────────┘    └───────────┘    └──────────┘    └───────────┘ │
│       │              │              │                 │              │         │
│       ▼              ▼              ▼                 ▼              ▼         │
│  File system   Current hash   Only changed     Delete old       Atomic        │
│  events        vs stored      files need       + insert new     transaction   │
│                               re-extraction                                     │
│                                                                                 │
│  QUERY FLOW:                                                                    │
│  ═══════════                                                                    │
│                                                                                 │
│  ┌─────────┐    ┌─────────┐    ┌───────────┐    ┌──────────┐    ┌───────────┐ │
│  │ Query   │───▶│ Query   │───▶│ Store     │───▶│ Result   │───▶│ Paginated │ │
│  │ Request │    │ Builder │    │ Query     │    │ Ranking  │    │ Response  │ │
│  └─────────┘    └─────────┘    └───────────┘    └──────────┘    └───────────┘ │
│       │              │              │                 │              │         │
│       ▼              ▼              ▼                 ▼              ▼         │
│  Search term   Apply filters  Execute SQL      Score & rank    Return top     │
│  + filters     (kind,file)    with indexes     by relevance    N results      │
│                                                                                 │
└─────────────────────────────────────────────────────────────────────────────────┘
```

### Trade-Offs Analysis

#### Trade-Off 1: Language-Specific Extractors vs Generic Parser

| Approach | Pros | Cons |
|----------|------|------|
| **Language-Specific (Chosen)** | Accurate semantic analysis, full type info, proper visibility detection | Higher implementation cost per language, dependency on language toolchains |
| **Generic Tree-Sitter Parser** | Single implementation for many languages, consistent behavior | Less semantic accuracy, no type resolution, limited symbol metadata |

**Decision:** Language-specific extractors provide the semantic depth required for effective code intelligence. The investment pays off in query precision and context quality.

**Quantified Impact:**
- Language-specific: 98% symbol accuracy, full signature/visibility info
- Generic parser: 75% accuracy, limited metadata
- Context quality improvement: ~40% better LLM responses with precise symbols

#### Trade-Off 2: SQLite Single-File vs Distributed Store

| Approach | Pros | Cons |
|----------|------|------|
| **SQLite (Chosen)** | Zero deployment, ACID transactions, excellent read performance, portable | Single-writer bottleneck, limited to ~50GB practical limit |
| **PostgreSQL/External** | Unlimited scale, concurrent writers, enterprise features | Deployment complexity, network latency, operational overhead |

**Decision:** SQLite with WAL mode handles codebases up to 100K files (50M+ symbols) with sub-50ms query latency. Most workspaces are 10-100x smaller than this limit.

**Quantified Impact:**
- SQLite: <30ms query latency, zero ops overhead
- External DB: 5-10ms network overhead per query, $200+/month hosting
- Scale ceiling: SQLite handles 99.9% of real-world codebases

#### Trade-Off 3: Full Rebuild vs Incremental Updates

| Approach | Pros | Cons |
|----------|------|------|
| **Hybrid (Chosen)** | Fast incremental for most cases, full rebuild for edge cases | Complexity in change detection, potential stale state |
| **Always Full Rebuild** | Guaranteed consistency, simpler implementation | Slow for large codebases, wastes resources |
| **Pure Incremental** | Always fast | Complex merge logic, risk of accumulated errors |

**Decision:** Hash-based change detection enables incremental updates. Full rebuild available for inconsistency recovery or major updates.

**Quantified Impact:**
- Full rebuild: 10K files @ 20s, 100K files @ 200s
- Incremental: 10 changed files @ 200ms regardless of total size
- 99%+ of updates are incremental in normal development

#### Trade-Off 4: Eager vs Lazy Symbol Loading

| Approach | Pros | Cons |
|----------|------|------|
| **Eager Loading (Chosen)** | Predictable memory, fast queries, simpler caching | Higher initial memory, startup cost |
| **Lazy Loading** | Minimal memory for unused symbols | Cold query latency, complex cache management |

**Decision:** Eager loading of symbol metadata (not source content) provides consistent sub-50ms query performance. Source content loaded on demand via RepoFS.

**Quantified Impact:**
- Memory: ~100 bytes per symbol, 10MB for 100K symbols (acceptable)
- Query latency: Always <50ms vs variable 50-500ms with lazy loading
- Startup cost: ~2 seconds for 100K symbols (amortized across session)

#### Trade-Off 5: Synchronous vs Asynchronous Indexing

| Approach | Pros | Cons |
|----------|------|------|
| **Background Async (Chosen)** | Non-blocking user operations, progressive availability | Stale results during build, complexity |
| **Synchronous Blocking** | Guaranteed consistency, simpler implementation | Blocks user on large operations |

**Decision:** Background indexing with progress reporting. Queries return available results during build. Status indicates completeness.

**Quantified Impact:**
- Async: User unblocked, 95% of symbols available within 5s of build start
- Sync: User blocked 20-200s for large codebases
- Developer productivity: ~10 min/day saved by non-blocking indexing

### Scope

Task 017 defines the core symbol indexing infrastructure:

1. **Symbol Model:** Defines ISymbol interface and related types (SymbolKind, SymbolLocation, visibility, signatures). Common representation across all languages.

2. **Symbol Store:** Persistent storage for symbols with CRUD operations. Supports queries by name, kind, file, namespace, and containment. Uses workspace database from Task 050.

3. **Extractor Interface:** Defines ISymbolExtractor contract. Language-specific implementations in subtasks (017.a for C#, 017.b for TypeScript).

4. **Extractor Registry:** Maps file extensions to extractors. Fallback for unsupported languages. Enables future language additions.

5. **Index Service:** Orchestrates full and incremental indexing. Tracks file hashes for change detection. Supports parallel processing.

6. **Query Interface:** Rich querying capabilities—exact match, prefix, fuzzy, filtered by kind/visibility/file.

### Integration Points

| Component | Integration Type | Description |
|-----------|------------------|-------------|
| Task 014 (RepoFS) | File Access | Reads files for extraction |
| Task 015 (Text Index) | Complementary | Text index for full-text, symbol index for semantic |
| Task 016 (Context) | Data Source | Symbol definitions feed context packer |
| Task 050 (Workspace DB) | Persistence | Stores symbols in SQLite database |
| Task 017.a (C# Extractor) | Implementation | Roslyn-based C# extraction |
| Task 017.b (TS Extractor) | Implementation | TypeScript extraction |
| Task 017.c (Dependencies) | Enhancement | Cross-reference and dependency mapping |
| Task 003.c (Audit) | Audit Logging | Index operations are audited |

### Failure Modes

| Failure | Impact | Mitigation |
|---------|--------|------------|
| Parse error in file | File not indexed | Log warning, skip file, continue |
| Extractor crash | Indexing fails | Isolate extraction, timeout, skip file |
| Database corruption | Index unavailable | Corruption detection, rebuild |
| Memory exhaustion | Indexing crashes | Stream processing, file size limits |
| Concurrent updates | Inconsistent state | Locking, transaction isolation |
| Unknown language | No symbols extracted | Warn, fallback to text-only |
| Large file timeout | Slow indexing | Size limits, timeout per file |
| Duplicate symbols | Query ambiguity | Unique ID, include location in disambiguation |

### Assumptions

1. Target languages have parseable syntax (valid source files)
2. Language extractors are available (Roslyn, TS Compiler API)
3. Symbols have unique IDs within the index
4. Symbol locations are stable within a file version
5. Incremental updates are more common than full rebuilds
6. Most queries are by name or kind
7. Containment relationships are tree-structured
8. File hashes reliably detect changes
9. Parallel extraction is safe
10. Database can handle 1M+ symbols

### Security Considerations

Symbol indexing involves parsing and analyzing code:

1. **Parse Safety:** Extractors MUST handle malicious input. Parser crashes MUST NOT affect the host process.

2. **Resource Limits:** Extraction MUST have memory and time limits. Adversarial files MUST NOT cause DoS.

3. **Path Validation:** All file paths MUST go through RepoFS. No direct file system access.

4. **Content Protection:** Indexed content MUST have same access controls as source. Index permissions MUST match repository.

5. **Audit Trail:** Index operations SHOULD be logged for troubleshooting.

---

## Use Cases

### Use Case 1: Developer Searches for Service Implementation

**Persona:** Marcus Chen, Senior Backend Developer, 8 years experience, maintaining a 150K-line C# microservices codebase with 45 services.

**Context:** Marcus needs to find the `OrderProcessingService` to understand how order validation works before adding a new validation rule.

**Before Symbol Index:**
1. Marcus opens VS Code and runs grep: `grep -r "OrderProcessingService" .`
2. Gets 127 results: class definition, DI registrations, usages, comments, test mocks
3. Opens 15 files to find the actual implementation
4. Scrolls through 2,400-line file to find validation method
5. Total time: 8 minutes of context switching

**After Symbol Index:**
```bash
$ acode symbols search "OrderProcessingService" --kind class
Symbols matching "OrderProcessingService":
  1. OrderProcessingService (class) - src/Services/Orders/OrderProcessingService.cs:25

$ acode symbols search "Validate*" --kind method --file "*OrderProcessing*"
Symbols matching "Validate*" in OrderProcessingService:
  1. ValidateOrder (method) - OrderProcessingService.cs:145
  2. ValidatePayment (method) - OrderProcessingService.cs:201
  3. ValidateInventory (method) - OrderProcessingService.cs:267
```
1. Direct navigation to class definition
2. Immediate listing of all validation methods
3. Total time: 30 seconds

**Quantified Metrics:**
| Metric | Before | After | Improvement |
|--------|--------|-------|-------------|
| Time to locate | 8 min | 30 sec | 16x faster |
| Files opened | 15 | 1 | 93% reduction |
| Context switches | 12 | 0 | 100% reduction |
| Cognitive load | High | Low | Significant |

---

### Use Case 2: AI Agent Builds Precise Context for Code Generation

**Persona:** The AI coding agent, operating in autonomous mode, generating a new feature implementation.

**Context:** User requests: "Add a discount calculation feature to the Order service." Agent needs to understand existing Order-related symbols.

**Before Symbol Index:**
1. Agent uses text search for "Order"
2. Retrieves 340 file matches, 2.1MB of content
3. Must include all matches in context (token budget exceeded)
4. Truncates arbitrarily, loses important interfaces
5. Generated code incompatible with existing patterns
6. User must debug and provide corrections

**After Symbol Index:**
```csharp
// Agent query: Find Order-related classes and interfaces
var orderSymbols = await symbolIndex.SearchAsync(new SymbolQuery
{
    NamePattern = "*Order*",
    Kinds = new[] { SymbolKind.Class, SymbolKind.Interface },
    Visibility = new[] { "public", "internal" }
});

// Result: 12 precise symbols with signatures
// IOrderService, OrderService, Order, OrderItem, OrderDto...
// Context size: 15KB instead of 2.1MB
```
1. Agent retrieves only class/interface definitions
2. Gets signatures, visibility, relationships
3. Context is precise and within budget
4. Generated code follows existing patterns
5. First attempt compiles and integrates correctly

**Quantified Metrics:**
| Metric | Before | After | Improvement |
|--------|--------|-------|-------------|
| Context size | 2.1 MB | 15 KB | 99.3% reduction |
| Tokens used | 525K | 3.8K | 99.3% reduction |
| API cost | $5.25 | $0.04 | 99.2% savings |
| Generation success | 40% | 92% | 130% improvement |
| Iterations needed | 3.5 avg | 1.2 avg | 66% reduction |

---

### Use Case 3: New Developer Explores Unfamiliar Codebase

**Persona:** Sofia Rodriguez, Junior Developer, first week at company, onboarding to a 200K-line TypeScript monorepo.

**Context:** Sofia needs to understand the authentication system to add a new OAuth provider.

**Before Symbol Index:**
1. Sofia asks senior dev for guidance
2. Senior dev spends 30 min explaining architecture
3. Sofia takes notes, still confused about structure
4. Searches for "auth" - 450 matches
5. Randomly opens files, builds mental model slowly
6. After 4 hours, has basic understanding

**After Symbol Index:**
```bash
# Explore auth-related types
$ acode symbols search "*Auth*" --kind class,interface

Symbols matching "*Auth*":
Types:
  1. IAuthService (interface) - src/auth/IAuthService.ts:5
  2. AuthService (class) - src/auth/AuthService.ts:12
  3. AuthConfig (class) - src/auth/AuthConfig.ts:3
  4. AuthMiddleware (class) - src/auth/middleware/AuthMiddleware.ts:8
  5. OAuthProvider (interface) - src/auth/providers/OAuthProvider.ts:4
  6. GoogleAuthProvider (class) - src/auth/providers/GoogleAuthProvider.ts:10
  7. GitHubAuthProvider (class) - src/auth/providers/GitHubAuthProvider.ts:10

# See methods on IAuthService
$ acode symbols search "*" --kind method --containing "IAuthService"

Methods in IAuthService:
  1. authenticate (method) - IAuthService.ts:8
  2. validateToken (method) - IAuthService.ts:12
  3. refreshToken (method) - IAuthService.ts:15
  4. logout (method) - IAuthService.ts:18

# See existing OAuth provider implementation pattern
$ acode symbols search "*" --kind method --containing "GoogleAuthProvider"
```

1. Sofia explores symbol hierarchy independently
2. Understands auth architecture in 20 minutes
3. Sees existing OAuth provider implementations
4. Implements new provider following established pattern

**Quantified Metrics:**
| Metric | Before | After | Improvement |
|--------|--------|-------|-------------|
| Senior dev time | 30 min | 5 min | 83% reduction |
| Onboarding time | 4 hours | 45 min | 81% reduction |
| Files explored | 50+ random | 8 targeted | 84% reduction |
| Pattern discovery | Manual | Automatic | Faster learning |

---

### Use Case 4: Build System Performs Impact Analysis

**Persona:** CI/CD Pipeline, automated build system evaluating PR changes.

**Context:** Developer submits PR modifying `IPaymentGateway` interface. System needs to identify affected components.

**Before Symbol Index:**
1. Build system compiles entire codebase
2. Finds errors in 12 files after 8-minute build
3. Developer gets failure email listing files
4. No indication of what specifically broke
5. Developer spends 20 minutes analyzing errors

**After Symbol Index:**
```csharp
// CI pre-check: Find all implementations and usages of changed interface
var changedSymbol = await symbolIndex.GetByNameAsync("IPaymentGateway");
var impactedSymbols = await symbolIndex.SearchAsync(new SymbolQuery
{
    ContainingSymbolId = null, // Any container
    ReferencesSymbol = changedSymbol.Id
});

// Result: 
// Implementations: StripeGateway, PayPalGateway, MockGateway
// Usages: PaymentService.ProcessPayment, CheckoutController.Submit
// Test references: PaymentGatewayTests, IntegrationTests
```

1. Pre-build analysis in 200ms
2. PR comment: "This change affects: StripeGateway, PayPalGateway, PaymentService, CheckoutController"
3. Developer knows exactly what to update
4. Targeted testing instead of full suite

**Quantified Metrics:**
| Metric | Before | After | Improvement |
|--------|--------|-------|-------------|
| Analysis time | 8 min build | 200 ms | 2,400x faster |
| Feedback loop | After compile | Before compile | Proactive |
| Error context | File list only | Symbol relationships | Actionable |
| Developer debug time | 20 min | 2 min | 90% reduction |

---

### Use Case 5: Security Audit Identifies Sensitive Code Patterns

**Persona:** Alex Kim, Security Engineer, performing quarterly security review of authentication and encryption code.

**Context:** Alex needs to audit all cryptographic operations and password handling.

**Before Symbol Index:**
1. Manually searches for "password", "encrypt", "hash", "crypto"
2. Gets 800+ results across code, comments, tests, configs
3. Spends 2 days filtering false positives
4. May miss non-obvious implementations
5. Creates spreadsheet tracking findings

**After Symbol Index:**
```bash
# Find all cryptography-related classes
$ acode symbols search "*Crypt*|*Hash*|*Cipher*" --kind class,method

Security-relevant symbols:
Classes:
  1. PasswordHasher (class) - src/Security/PasswordHasher.cs:15
  2. TokenEncryptor (class) - src/Security/TokenEncryptor.cs:8
  3. AesCipherService (class) - src/Security/Crypto/AesCipherService.cs:12

Methods:
  1. HashPassword (method) - PasswordHasher.cs:25
  2. VerifyPassword (method) - PasswordHasher.cs:45
  3. Encrypt (method) - TokenEncryptor.cs:20
  4. Decrypt (method) - TokenEncryptor.cs:35
  5. GenerateKey (method) - AesCipherService.cs:28

# Find password-related symbols
$ acode symbols search "*Password*" --kind method,property

Password handling:
  1. Password (property) - UserCredentials.cs:12
  2. SetPassword (method) - UserService.cs:67
  3. ResetPassword (method) - UserService.cs:89
  4. ValidatePasswordStrength (method) - PasswordPolicy.cs:15
```

1. Comprehensive list of security-relevant symbols in seconds
2. Navigate directly to implementations
3. Review complete in 4 hours instead of 2 days
4. Higher confidence in coverage

**Quantified Metrics:**
| Metric | Before | After | Improvement |
|--------|--------|-------|-------------|
| Audit time | 16 hours | 4 hours | 75% reduction |
| Coverage confidence | ~80% | ~99% | 24% improvement |
| False positives | 600+ | <10 | 98% reduction |
| Missed vulnerabilities | Unknown | Near zero | Risk reduction |

---

## Glossary / Terms

| Term | Definition |
|------|------------|
| Symbol | Code element: class, method, function, property, field, type |
| Symbol Index | Database of extracted symbols |
| Extractor | Language-specific symbol parser |
| Roslyn | .NET compiler platform for C# analysis |
| TypeScript Compiler API | TypeScript language services |
| Semantic Model | Compiler's type/binding information |
| Symbol Reference | Location where symbol is used |
| Symbol Definition | Location where symbol is declared |
| Symbol Kind | Category: class, method, property, etc. |
| Fully Qualified Name | Complete name including namespace/module |
| Signature | Method/function parameter and return types |
| Visibility | Public, private, protected, internal |
| Containment | Parent-child symbol relationship |
| Relationship | Symbol-to-symbol connection |
| Incremental Update | Update only changed files |

---

## Out of Scope

The following items are explicitly excluded from Task 017:

- **C# extraction details** - See Task 017.a
- **TypeScript extraction details** - See Task 017.b
- **Dependency mapping** - See Task 017.c
- **Retrieval APIs** - See Task 017.c
- **Python/Go/Rust support** - Future versions
- **Cross-repo symbols** - Single repo only
- **Semantic analysis** - Structure only
- **IDE integration** - CLI only

---

## Functional Requirements

### Symbol Model (FR-017-01 to FR-017-25)

| ID | Requirement |
|----|-------------|
| FR-017-01 | System MUST define ISymbol interface |
| FR-017-02 | ISymbol MUST have Id property (Guid) |
| FR-017-03 | ISymbol MUST have Name property (short name) |
| FR-017-04 | ISymbol MUST have FullyQualifiedName property |
| FR-017-05 | ISymbol MUST have Kind property (SymbolKind enum) |
| FR-017-06 | ISymbol MUST have Location property (SymbolLocation) |
| FR-017-07 | ISymbol MUST have Signature property (nullable) |
| FR-017-08 | ISymbol MUST have Visibility property |
| FR-017-09 | ISymbol MUST have ContainingSymbolId property (nullable) |
| FR-017-10 | System MUST define SymbolKind enum |
| FR-017-11 | SymbolKind MUST include Namespace |
| FR-017-12 | SymbolKind MUST include Class |
| FR-017-13 | SymbolKind MUST include Interface |
| FR-017-14 | SymbolKind MUST include Struct |
| FR-017-15 | SymbolKind MUST include Enum |
| FR-017-16 | SymbolKind MUST include Method |
| FR-017-17 | SymbolKind MUST include Property |
| FR-017-18 | SymbolKind MUST include Field |
| FR-017-19 | SymbolKind MUST include Constructor |
| FR-017-20 | SymbolKind MUST include Function |
| FR-017-21 | SymbolKind MUST include Variable |
| FR-017-22 | SymbolKind MUST include TypeAlias |
| FR-017-23 | SymbolLocation MUST include FilePath |
| FR-017-24 | SymbolLocation MUST include StartLine/EndLine |
| FR-017-25 | SymbolLocation MUST include StartColumn/EndColumn |

### Symbol Store (FR-017-26 to FR-017-50)

| ID | Requirement |
|----|-------------|
| FR-017-26 | System MUST define ISymbolStore interface |
| FR-017-27 | AddAsync MUST insert single symbol |
| FR-017-28 | AddRangeAsync MUST batch insert symbols |
| FR-017-29 | Batch insert MUST be transactional |
| FR-017-30 | RemoveAsync MUST delete symbol by ID |
| FR-017-31 | RemoveByFileAsync MUST delete all symbols in file |
| FR-017-32 | UpdateAsync MUST modify existing symbol |
| FR-017-33 | GetByIdAsync MUST retrieve symbol by ID |
| FR-017-34 | Store MUST persist to SQLite database |
| FR-017-35 | Store MUST load from database on startup |
| FR-017-36 | Store MUST support concurrent reads |
| FR-017-37 | Store MUST serialize writes |
| FR-017-38 | Store MUST use connection pooling |
| FR-017-39 | Store MUST create indexes for common queries |
| FR-017-40 | Store MUST index Name column |
| FR-017-41 | Store MUST index Kind column |
| FR-017-42 | Store MUST index FilePath column |
| FR-017-43 | Store MUST index ContainingSymbolId column |
| FR-017-44 | Store MUST handle 1M+ symbols |
| FR-017-45 | Store MUST support pagination |
| FR-017-46 | Store MUST report symbol count |
| FR-017-47 | Store MUST report file count |
| FR-017-48 | ClearAsync MUST delete all symbols |
| FR-017-49 | Store MUST be disposable |
| FR-017-50 | Disposal MUST release connections |

### Symbol Extractor (FR-017-51 to FR-017-70)

| ID | Requirement |
|----|-------------|
| FR-017-51 | System MUST define ISymbolExtractor interface |
| FR-017-52 | ExtractAsync MUST accept file path |
| FR-017-53 | ExtractAsync MUST accept file content |
| FR-017-54 | ExtractAsync MUST return list of symbols |
| FR-017-55 | ExtractAsync MUST accept CancellationToken |
| FR-017-56 | Extractor MUST report supported extensions |
| FR-017-57 | Extractor MUST report supported languages |
| FR-017-58 | Extractor MUST handle parse errors gracefully |
| FR-017-59 | Parse errors MUST be logged |
| FR-017-60 | Parse errors MUST return partial results |
| FR-017-61 | Extractor MUST respect extraction depth config |
| FR-017-62 | Depth 0 MUST extract types only |
| FR-017-63 | Depth 1 MUST extract types and members |
| FR-017-64 | Depth 2 MUST extract nested and local |
| FR-017-65 | Extractor MUST respect file size limit |
| FR-017-66 | Oversized files MUST be skipped |
| FR-017-67 | Extractor MUST respect timeout |
| FR-017-68 | Timeout MUST return partial results |
| FR-017-69 | System MUST define IExtractorRegistry |
| FR-017-70 | Registry MUST map extensions to extractors |

### Index Management (FR-017-71 to FR-017-95)

| ID | Requirement |
|----|-------------|
| FR-017-71 | System MUST define ISymbolIndex interface |
| FR-017-72 | BuildAsync MUST perform full index rebuild |
| FR-017-73 | Build MUST clear existing symbols first |
| FR-017-74 | Build MUST enumerate all source files |
| FR-017-75 | Build MUST respect ignore patterns |
| FR-017-76 | Build MUST extract symbols per file |
| FR-017-77 | Build MUST store extracted symbols |
| FR-017-78 | Build MUST track file hashes |
| FR-017-79 | Build MUST report progress |
| FR-017-80 | UpdateAsync MUST perform incremental update |
| FR-017-81 | Update MUST detect modified files |
| FR-017-82 | Update MUST detect new files |
| FR-017-83 | Update MUST detect deleted files |
| FR-017-84 | Modified files MUST be re-extracted |
| FR-017-85 | New files MUST be extracted and added |
| FR-017-86 | Deleted files MUST have symbols removed |
| FR-017-87 | IndexFilesAsync MUST index specific files |
| FR-017-88 | RemoveFilesAsync MUST remove specific files |
| FR-017-89 | ClearAsync MUST clear entire index |
| FR-017-90 | GetStatusAsync MUST return index status |
| FR-017-91 | Status MUST include file count |
| FR-017-92 | Status MUST include symbol count |
| FR-017-93 | Status MUST include last build time |
| FR-017-94 | Status MUST include last update time |
| FR-017-95 | All operations MUST support cancellation |

### Query Interface (FR-017-96 to FR-017-120)

| ID | Requirement |
|----|-------------|
| FR-017-96 | System MUST define ISymbolQuery interface |
| FR-017-97 | SearchAsync MUST accept query string |
| FR-017-98 | Search MUST support exact name match |
| FR-017-99 | Search MUST support prefix match |
| FR-017-100 | Search MUST support fuzzy match |
| FR-017-101 | Fuzzy MUST tolerate typos |
| FR-017-102 | Search MUST support wildcard (*) |
| FR-017-103 | Search MUST filter by SymbolKind |
| FR-017-104 | Search MUST filter by visibility |
| FR-017-105 | Search MUST filter by file pattern |
| FR-017-106 | Search MUST filter by namespace |
| FR-017-107 | Search MUST support multiple filters |
| FR-017-108 | Search MUST return ordered results |
| FR-017-109 | Order MUST support by relevance |
| FR-017-110 | Order MUST support by name |
| FR-017-111 | Order MUST support by file |
| FR-017-112 | Search MUST support pagination |
| FR-017-113 | Pagination MUST accept skip and take |
| FR-017-114 | Search MUST return total count |
| FR-017-115 | ResolveAsync MUST get symbol by ID |
| FR-017-116 | GetSourceAsync MUST return symbol source code |
| FR-017-117 | GetDocumentationAsync MUST return doc comments |
| FR-017-118 | GetContainingAsync MUST return containing context |
| FR-017-119 | GetChildrenAsync MUST return contained symbols |
| FR-017-120 | Query operations MUST be read-only |

---

## Non-Functional Requirements

### Performance (NFR-017-01 to NFR-017-20)

| ID | Category | Requirement |
|----|----------|-------------|
| NFR-017-01 | Performance | Full index of 1000 files MUST complete in < 30s |
| NFR-017-02 | Performance | Full index of 10,000 files MUST complete in < 5 min |
| NFR-017-03 | Performance | Incremental update per file MUST complete in < 100ms |
| NFR-017-04 | Performance | Incremental update for 100 files MUST complete in < 5s |
| NFR-017-05 | Performance | Name search MUST complete in < 50ms |
| NFR-017-06 | Performance | Fuzzy search MUST complete in < 100ms |
| NFR-017-07 | Performance | Filtered search MUST complete in < 75ms |
| NFR-017-08 | Performance | Symbol resolution by ID MUST complete in < 10ms |
| NFR-017-09 | Performance | Batch insert MUST achieve > 1000 symbols/s |
| NFR-017-10 | Performance | Index load MUST complete in < 2s |
| NFR-017-11 | Performance | Memory usage during indexing MUST be < 1GB |
| NFR-017-12 | Performance | Memory usage for loaded index MUST be < 200MB |
| NFR-017-13 | Performance | Parallel indexing MUST use configurable workers |
| NFR-017-14 | Performance | Default worker count MUST be CPU cores - 1 |
| NFR-017-15 | Performance | Database queries MUST use prepared statements |
| NFR-017-16 | Performance | Database MUST use WAL mode |
| NFR-017-17 | Performance | Database indexes MUST cover common queries |
| NFR-017-18 | Performance | Connection pooling MUST be used |
| NFR-017-19 | Performance | Extraction MUST stream large files |
| NFR-017-20 | Performance | Progress reporting MUST NOT slow indexing |

### Scalability (NFR-017-21 to NFR-017-30)

| ID | Category | Requirement |
|----|----------|-------------|
| NFR-017-21 | Scalability | Index MUST handle 100,000 files |
| NFR-017-22 | Scalability | Index MUST handle 1,000,000 symbols |
| NFR-017-23 | Scalability | Queries MUST remain < 100ms at 1M symbols |
| NFR-017-24 | Scalability | Pagination MUST work at any offset |
| NFR-017-25 | Scalability | Large result sets MUST be streamed |
| NFR-017-26 | Scalability | Index file size MUST scale linearly |
| NFR-017-27 | Scalability | Memory MUST NOT scale with symbol count |
| NFR-017-28 | Scalability | Concurrent queries MUST be supported |
| NFR-017-29 | Scalability | Concurrent updates MUST be serialized |
| NFR-017-30 | Scalability | Large files (>1MB) MUST NOT block |

### Reliability (NFR-017-31 to NFR-017-45)

| ID | Category | Requirement |
|----|----------|-------------|
| NFR-017-31 | Reliability | Parse errors MUST NOT stop indexing |
| NFR-017-32 | Reliability | Extractor crash MUST NOT crash host |
| NFR-017-33 | Reliability | Partial results MUST be returned on failure |
| NFR-017-34 | Reliability | Index state MUST remain consistent |
| NFR-017-35 | Reliability | Interrupted build MUST be resumable |
| NFR-017-36 | Reliability | Corruption MUST be detected on load |
| NFR-017-37 | Reliability | Corruption MUST trigger rebuild prompt |
| NFR-017-38 | Reliability | Crash during update MUST NOT corrupt index |
| NFR-017-39 | Reliability | Database transactions MUST be atomic |
| NFR-017-40 | Reliability | Rollback MUST restore previous state |
| NFR-017-41 | Reliability | File locks MUST be handled |
| NFR-017-42 | Reliability | Network errors (if any) MUST retry |
| NFR-017-43 | Reliability | Out of disk space MUST be handled |
| NFR-017-44 | Reliability | Symbols MUST have unique IDs |
| NFR-017-45 | Reliability | Duplicate detection MUST prevent conflicts |

### Accuracy (NFR-017-46 to NFR-017-55)

| ID | Category | Requirement |
|----|----------|-------------|
| NFR-017-46 | Accuracy | Symbol names MUST match source exactly |
| NFR-017-47 | Accuracy | Symbol locations MUST be precise |
| NFR-017-48 | Accuracy | Line numbers MUST be 1-based |
| NFR-017-49 | Accuracy | Column numbers MUST be 1-based |
| NFR-017-50 | Accuracy | Containment MUST reflect actual structure |
| NFR-017-51 | Accuracy | Visibility MUST match source |
| NFR-017-52 | Accuracy | Signatures MUST be parseable |
| NFR-017-53 | Accuracy | Deleted file symbols MUST be removed |
| NFR-017-54 | Accuracy | Renamed files MUST update correctly |
| NFR-017-55 | Accuracy | No orphaned symbols after update |

### Maintainability (NFR-017-56 to NFR-017-65)

| ID | Category | Requirement |
|----|----------|-------------|
| NFR-017-56 | Maintainability | Schema MUST be versioned |
| NFR-017-57 | Maintainability | Schema migrations MUST be automatic |
| NFR-017-58 | Maintainability | All interfaces MUST have XML docs |
| NFR-017-59 | Maintainability | Code coverage MUST be > 80% |
| NFR-017-60 | Maintainability | Cyclomatic complexity MUST be < 10 |
| NFR-017-61 | Maintainability | Dependencies MUST be injected |
| NFR-017-62 | Maintainability | Extractors MUST be pluggable |
| NFR-017-63 | Maintainability | Adding language MUST NOT modify core |
| NFR-017-64 | Maintainability | Configuration MUST be documented |
| NFR-017-65 | Maintainability | Error codes MUST be documented |

### Observability (NFR-017-66 to NFR-017-75)

| ID | Category | Requirement |
|----|----------|-------------|
| NFR-017-66 | Observability | Build progress MUST be logged |
| NFR-017-67 | Observability | Extraction errors MUST be logged |
| NFR-017-68 | Observability | Query operations MUST log at Debug |
| NFR-017-69 | Observability | Metrics MUST track indexed file count |
| NFR-017-70 | Observability | Metrics MUST track symbol count |
| NFR-017-71 | Observability | Metrics MUST track build duration |
| NFR-017-72 | Observability | Metrics MUST track query latency |
| NFR-017-73 | Observability | Metrics MUST track extraction errors |
| NFR-017-74 | Observability | Structured logging MUST be used |
| NFR-017-75 | Observability | Correlation IDs MUST be propagated |

---

## User Manual Documentation

### Overview

Symbol Index v2 provides semantic code search. Find classes, methods, and functions by name and type.

### Configuration

```yaml
# .agent/config.yml
symbol_index:
  # Enable symbol indexing
  enabled: true
  
  # Languages to index
  languages:
    - csharp
    - typescript
    - javascript
    
  # Indexing options
  options:
    # Include test files
    include_tests: false
    
    # Include generated files
    include_generated: false
    
    # Max file size to index (KB)
    max_file_size_kb: 500
    
    # Parallel workers
    workers: 4
    
  # Auto-update strategy
  update:
    # on_save, on_commit, manual
    trigger: on_save
    
    # Debounce delay (ms)
    debounce_ms: 500
```

### CLI Commands

```bash
# Build full symbol index
acode symbols build

# Build with progress
acode symbols build --progress

# Rebuild specific files
acode symbols build --files "src/**/*.cs"

# Incremental update
acode symbols update

# Search symbols
acode symbols search "UserService"

# Search by kind
acode symbols search "GetUser" --kind method

# Show index status
acode symbols status

# Clear index
acode symbols clear
```

### Search Examples

```bash
# Find all classes named "User*"
$ acode symbols search "User*" --kind class

Symbols matching "User*":
  1. UserService (class) - src/Services/UserService.cs:15
  2. UserRepository (class) - src/Data/UserRepository.cs:8
  3. UserController (class) - src/Controllers/UserController.cs:12
  4. UserDto (class) - src/Models/UserDto.cs:5

# Find methods containing "Get"
$ acode symbols search "*Get*" --kind method

Symbols matching "*Get*":
  1. GetById (method) - UserService.cs:45
  2. GetAll (method) - UserService.cs:60
  3. GetConnection (method) - DbContext.cs:25
```

### Index Status

```bash
$ acode symbols status

Symbol Index Status
───────────────────
Files indexed: 1,234
Symbols stored: 45,678
Last full build: 2024-01-15 10:30:00
Last update: 2024-01-15 14:45:00

By Language:
  C#: 28,450 symbols (650 files)
  TypeScript: 15,200 symbols (480 files)
  JavaScript: 2,028 symbols (104 files)

By Kind:
  Classes: 1,250
  Interfaces: 340
  Methods: 28,500
  Properties: 12,100
  Functions: 3,488
```

### Troubleshooting

#### Issue 1: Missing Symbols After Indexing

**Symptoms:**
- `acode symbols search "ClassName"` returns empty results
- Known classes/methods not appearing in search
- Symbol count lower than expected in `acode symbols status`

**Possible Causes:**
1. File extension not registered with an extractor
2. File exceeds maximum size limit (default 5MB)
3. File matches ignore patterns (e.g., in `node_modules`, `.gitignore`)
4. Parse errors preventing symbol extraction
5. Index not rebuilt after adding new files
6. Language extractor not installed or configured

**Solutions:**
```bash
# Check if file extension is supported
$ acode symbols languages
Supported languages:
  C# (.cs) - Roslyn extractor
  TypeScript (.ts, .tsx) - TypeScript extractor
  JavaScript (.js, .jsx) - JavaScript extractor

# Check file size
$ acode symbols check-file src/MyClass.cs
File: src/MyClass.cs
  Size: 2.3 MB (within 5 MB limit)
  Language: C#
  Indexed: Yes
  Symbols: 45

# Force rebuild for specific file
$ acode symbols build --files "src/MyClass.cs" --force

# Check for parse errors
$ acode symbols build --verbose 2>&1 | grep -i error

# If file is ignored, add explicit include
# In .agent/config.yml:
symbol_index:
  include_patterns:
    - "src/generated/**/*.cs"  # Include generated files
```

---

#### Issue 2: Parse Errors During Symbol Extraction

**Symptoms:**
- Error messages during `acode symbols build`
- Partial symbol extraction for some files
- Warnings about skipped files in verbose output
- `ACODE-SYM-001` error codes in logs

**Possible Causes:**
1. Source code has syntax errors (doesn't compile)
2. Language version mismatch (e.g., C# 12 features with C# 10 extractor)
3. Missing project references or dependencies
4. Encoding issues in source files
5. Malformed Unicode characters
6. Incomplete file save (file locked during read)

**Solutions:**
```bash
# Verify code compiles
$ dotnet build  # For C# projects
$ tsc --noEmit  # For TypeScript projects

# Check language version configuration
# In .agent/config.yml:
symbol_index:
  extractors:
    csharp:
      language_version: "12.0"
    typescript:
      target: "ES2022"

# View detailed parse errors
$ acode symbols build --verbose --show-errors
Parse errors:
  src/Legacy.cs:45 - Unexpected token ';'
  src/NewFeature.cs:12 - Unknown keyword 'required' (C# 11+)

# Skip problematic files
# In .agent/config.yml:
symbol_index:
  exclude_patterns:
    - "src/Legacy/**"

# Force UTF-8 encoding
$ acode symbols build --encoding utf-8
```

---

#### Issue 3: Slow Symbol Index Build

**Symptoms:**
- Full index build takes more than 5 minutes
- Progress stalls on certain files
- High CPU usage during indexing
- Memory usage growing unboundedly

**Possible Causes:**
1. Too many files to index (large monorepo)
2. Large individual files (generated code, minified bundles)
3. Insufficient parallelization
4. Database on slow storage (HDD vs SSD)
5. Antivirus scanning indexed files
6. Complex nested types causing slow parsing

**Solutions:**
```bash
# Increase worker count
$ acode symbols build --workers 8

# Exclude large generated files
# In .agent/config.yml:
symbol_index:
  exclude_patterns:
    - "**/*.generated.cs"
    - "**/*.Designer.cs"
    - "**/node_modules/**"
    - "**/dist/**"
    - "**/*.min.js"
  max_file_size_kb: 500  # Skip files >500KB

# Use incremental builds after initial index
$ acode symbols update  # Only changed files

# Check which files are slow
$ acode symbols build --profile
Slow files (>5s extraction):
  src/Generated/Models.cs - 12.3s (8,500 symbols)
  src/Database/Migrations.cs - 8.7s (3,200 symbols)

# Move database to faster storage
# In .agent/config.yml:
workspace:
  database_path: "D:/ssd/acode/workspace.db"

# Disable real-time antivirus for workspace
# Add workspace folder to antivirus exclusions
```

---

#### Issue 4: Incremental Updates Not Detecting Changes

**Symptoms:**
- Modified files not re-indexed after `acode symbols update`
- Old symbol information returned after code changes
- Hash mismatch warnings in verbose output
- Status shows "last update" time not changing

**Possible Causes:**
1. File timestamp not updated (some editors don't update mtime)
2. File content hash cache corrupted
3. Database write lock preventing updates
4. File system events not propagating
5. File saved to different location than expected
6. Symbolic links causing path confusion

**Solutions:**
```bash
# Force full rebuild to reset hash cache
$ acode symbols build --force

# Clear hash cache only
$ acode symbols clear --hashes-only

# Check file modification detection
$ acode symbols check-file src/MyClass.cs --verbose
File: src/MyClass.cs
  Stored hash: a1b2c3d4...
  Current hash: e5f6g7h8...  # Different = should update
  Last indexed: 2024-01-15 10:30:00
  Last modified: 2024-01-15 14:45:00  # Newer = should update

# Enable filesystem watcher for real-time updates
# In .agent/config.yml:
symbol_index:
  update:
    trigger: on_save
    use_filesystem_watcher: true

# Resolve symbolic link issues
$ acode symbols build --resolve-symlinks
```

---

#### Issue 5: Query Returns Unexpected or Duplicate Results

**Symptoms:**
- Search returns symbols from wrong files
- Duplicate entries for same symbol
- Fuzzy search matching unrelated symbols
- Filter not applying correctly

**Possible Causes:**
1. Index corruption after crash or power loss
2. Duplicate symbol IDs from concurrent writes
3. Stale symbols from deleted files
4. Overly broad search pattern
5. Fuzzy matching threshold too low
6. Case sensitivity mismatch

**Solutions:**
```bash
# Verify index integrity
$ acode symbols verify
Index integrity check:
  Database checksum: VALID
  SQLite integrity: OK
  Orphaned symbols: 0
  Duplicate IDs: 0

# If corruption detected, rebuild
$ acode symbols clear
$ acode symbols build

# Clean up stale entries
$ acode symbols prune --deleted-files

# Adjust search parameters
$ acode symbols search "MyClass" --exact  # Exact match only
$ acode symbols search "MyClass" --case-sensitive
$ acode symbols search "MyClass" --fuzzy-threshold 0.9  # Higher = stricter

# Debug query execution
$ acode symbols search "User*" --explain
Query plan:
  Pattern: User* (prefix match)
  Filter: none
  Index used: idx_symbols_name
  Estimated rows: 45
  Actual rows: 42
```

---

#### Issue 6: Index Database File Errors

**Symptoms:**
- Error: "Database is locked"
- Error: "Database disk image is malformed"
- Error: "SQLITE_BUSY" during operations
- Index file grows unexpectedly large

**Possible Causes:**
1. Concurrent processes accessing database
2. Database corruption from crash
3. Disk space exhaustion
4. WAL file not checkpointed
5. Antivirus quarantining database file
6. File system corruption

**Solutions:**
```bash
# Check database file status
$ acode symbols status --database
Database status:
  Path: .agent/workspace.db
  Size: 45.2 MB
  WAL size: 12.1 MB
  Journal mode: WAL
  Locked: No

# Force WAL checkpoint to reduce file size
$ acode symbols vacuum

# Repair corrupted database
$ acode symbols repair
Attempting database repair...
  Exporting valid records: 45,230 symbols
  Creating new database...
  Importing records...
  Repair complete: 45,230 symbols recovered

# If repair fails, rebuild from scratch
$ acode symbols clear --force
$ acode symbols build

# Prevent concurrent access issues
# In .agent/config.yml:
workspace:
  database:
    busy_timeout_ms: 5000
    journal_mode: WAL
    synchronous: NORMAL
```

---

## Acceptance Criteria

### Symbol Model (AC-001 to AC-015)

- [ ] AC-001: ISymbol interface defined with all required properties
- [ ] AC-002: Symbol.Id is a unique Guid
- [ ] AC-003: Symbol.Name stores the simple name
- [ ] AC-004: Symbol.FullyQualifiedName stores complete path
- [ ] AC-005: Symbol.Kind is one of SymbolKind enum values
- [ ] AC-006: Symbol.Location stores file path and line/column ranges
- [ ] AC-007: Symbol.Signature stores method/property signatures
- [ ] AC-008: Symbol.Visibility stores public/private/protected/internal
- [ ] AC-009: Symbol.ContainingSymbolId references parent symbol
- [ ] AC-010: SymbolKind includes Namespace, Class, Interface, Struct
- [ ] AC-011: SymbolKind includes Enum, Method, Property, Field
- [ ] AC-012: SymbolKind includes Constructor, Function, Variable, TypeAlias
- [ ] AC-013: SymbolLocation includes FilePath, StartLine, EndLine
- [ ] AC-014: SymbolLocation includes StartColumn, EndColumn
- [ ] AC-015: Symbol equality based on Id

### Symbol Store (AC-016 to AC-030)

- [ ] AC-016: ISymbolStore interface defined
- [ ] AC-017: AddAsync adds single symbol
- [ ] AC-018: AddBatchAsync adds multiple symbols efficiently
- [ ] AC-019: RemoveAsync removes symbol by Id
- [ ] AC-020: RemoveByFileAsync removes all symbols for a file
- [ ] AC-021: UpdateAsync updates existing symbol
- [ ] AC-022: GetByIdAsync retrieves symbol by Id
- [ ] AC-023: GetByFileAsync retrieves all symbols in file
- [ ] AC-024: SearchAsync supports exact name match
- [ ] AC-025: SearchAsync supports prefix match (Name*)
- [ ] AC-026: SearchAsync supports fuzzy match (Levenshtein)
- [ ] AC-027: SearchAsync supports SymbolKind filter
- [ ] AC-028: SearchAsync supports visibility filter
- [ ] AC-029: SearchAsync supports file pattern filter
- [ ] AC-030: SearchAsync supports pagination (skip/take)

### Symbol Store Persistence (AC-031 to AC-040)

- [ ] AC-031: Store persists to SQLite database
- [ ] AC-032: Database schema includes symbols table
- [ ] AC-033: Database schema includes file_hashes table
- [ ] AC-034: Database schema includes index_metadata table
- [ ] AC-035: Indexes exist for name, fqn, kind, file_path columns
- [ ] AC-036: WAL mode enabled for concurrent reads
- [ ] AC-037: Connection pooling implemented
- [ ] AC-038: Schema versioning with migrations
- [ ] AC-039: Data survives process restart
- [ ] AC-040: Atomic transactions for batch operations

### Extractor Interface (AC-041 to AC-050)

- [ ] AC-041: ISymbolExtractor interface defined
- [ ] AC-042: ExtractAsync extracts symbols from file path
- [ ] AC-043: ExtractFromContentAsync extracts from in-memory content
- [ ] AC-044: SupportedExtensions property lists handled extensions
- [ ] AC-045: LanguageName property identifies the language
- [ ] AC-046: Extractor handles parse errors gracefully
- [ ] AC-047: Extractor supports cancellation
- [ ] AC-048: Extractor respects timeout limits
- [ ] AC-049: Extractor respects file size limits
- [ ] AC-050: Extractor returns partial results on error

### Extractor Registry (AC-051 to AC-058)

- [ ] AC-051: IExtractorRegistry interface defined
- [ ] AC-052: RegisterExtractor adds extractor for extensions
- [ ] AC-053: GetExtractor returns extractor for file extension
- [ ] AC-054: GetExtractor returns extractor for language name
- [ ] AC-055: GetExtractor returns null for unknown extensions
- [ ] AC-056: GetSupportedLanguages lists all registered languages
- [ ] AC-057: Fallback extractor configurable
- [ ] AC-058: Multiple extensions map to same extractor

### Index Service (AC-059 to AC-075)

- [ ] AC-059: ISymbolIndex interface defined
- [ ] AC-060: BuildAsync performs full index build
- [ ] AC-061: BuildAsync respects ignore patterns
- [ ] AC-062: BuildAsync supports file filtering
- [ ] AC-063: BuildAsync supports parallel processing
- [ ] AC-064: BuildAsync reports progress
- [ ] AC-065: BuildAsync supports cancellation
- [ ] AC-066: UpdateAsync performs incremental update
- [ ] AC-067: UpdateAsync detects changed files via hash
- [ ] AC-068: UpdateAsync detects new files
- [ ] AC-069: UpdateAsync detects deleted files
- [ ] AC-070: UpdateAsync removes symbols for deleted files
- [ ] AC-071: GetStatusAsync returns index metadata
- [ ] AC-072: GetStatusAsync returns file and symbol counts
- [ ] AC-073: GetStatusAsync returns last build timestamp
- [ ] AC-074: ClearAsync removes all indexed data
- [ ] AC-075: Index handles parse errors without stopping

### CLI Commands (AC-076 to AC-090)

- [ ] AC-076: `acode symbols build` executes full build
- [ ] AC-077: `acode symbols build --progress` shows progress
- [ ] AC-078: `acode symbols build --files <pattern>` builds specific files
- [ ] AC-079: `acode symbols build --force` ignores cached hashes
- [ ] AC-080: `acode symbols update` executes incremental update
- [ ] AC-081: `acode symbols search <query>` searches symbols
- [ ] AC-082: `acode symbols search --kind <kind>` filters by type
- [ ] AC-083: `acode symbols search --file <pattern>` filters by file
- [ ] AC-084: `acode symbols search --exact` disables fuzzy matching
- [ ] AC-085: `acode symbols status` shows index statistics
- [ ] AC-086: `acode symbols clear` removes all index data
- [ ] AC-087: `acode symbols languages` lists supported languages
- [ ] AC-088: `acode symbols verify` checks index integrity
- [ ] AC-089: `acode symbols vacuum` optimizes database
- [ ] AC-090: All CLI commands respect --verbose flag

### Performance (AC-091 to AC-100)

- [ ] AC-091: Full build of 1K files completes in <20 seconds
- [ ] AC-092: Full build of 10K files completes in <200 seconds
- [ ] AC-093: Incremental update of 10 files completes in <500ms
- [ ] AC-094: Exact name query returns in <30ms
- [ ] AC-095: Prefix query returns in <50ms
- [ ] AC-096: Fuzzy query returns in <100ms
- [ ] AC-097: Memory usage during build <500MB for 10K files
- [ ] AC-098: Database size <100MB for 100K symbols
- [ ] AC-099: Parallel extraction utilizes all configured workers
- [ ] AC-100: Query results paginate correctly

### CLI

- [ ] AC-013: Build command works
- [ ] AC-014: Search command works
- [ ] AC-015: Status command works

---

## Security

### Threat 1: Malicious Code Injection via Symbol Names

**Risk Level:** HIGH  
**Attack Vector:** An attacker crafts source files with symbol names containing SQL injection, path traversal, or command injection payloads.

**Scenario:** Malicious file contains class `UserService; DROP TABLE symbols; --` that gets indexed, potentially corrupting the symbol store.

**Complete Mitigation Code:**

```csharp
using System.Text.RegularExpressions;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;

namespace Acode.Infrastructure.Symbols.Security;

/// <summary>
/// Provides sanitization for symbol data before storage or query execution.
/// Prevents SQL injection, path traversal, and command injection attacks.
/// </summary>
public sealed class SymbolSanitizer : ISymbolSanitizer
{
    private readonly ILogger<SymbolSanitizer> _logger;
    
    // Pattern to detect SQL injection attempts
    private static readonly Regex SqlInjectionPattern = new(
        @"(\b(SELECT|INSERT|UPDATE|DELETE|DROP|UNION|EXEC|EXECUTE|DECLARE|CAST|CONVERT)\b|--|;|'|\x00)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);
    
    // Pattern to detect path traversal attempts
    private static readonly Regex PathTraversalPattern = new(
        @"(\.\.[\\/]|[\\/]\.\.)|[\x00-\x1f]",
        RegexOptions.Compiled);
    
    // Valid symbol name characters (alphanumeric, underscore, common generics)
    private static readonly Regex ValidSymbolNamePattern = new(
        @"^[\w\.<>\[\],\s`]+$",
        RegexOptions.Compiled);

    public SymbolSanitizer(ILogger<SymbolSanitizer> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Sanitizes a symbol name for safe storage and querying.
    /// </summary>
    public string SanitizeSymbolName(string name, string sourceFile)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return string.Empty;
        }

        // Check for SQL injection patterns
        if (SqlInjectionPattern.IsMatch(name))
        {
            _logger.LogWarning(
                "Potential SQL injection detected in symbol name from {SourceFile}: {Name}",
                sourceFile,
                name.Substring(0, Math.Min(50, name.Length)));
            
            // Strip dangerous characters
            name = SqlInjectionPattern.Replace(name, "_");
        }

        // Validate against allowed character set
        if (!ValidSymbolNamePattern.IsMatch(name))
        {
            _logger.LogWarning(
                "Invalid characters in symbol name from {SourceFile}",
                sourceFile);
            
            // Replace invalid characters
            name = Regex.Replace(name, @"[^\w\.<>\[\],\s`]", "_");
        }

        // Truncate excessively long names (potential DoS)
        const int maxSymbolNameLength = 1024;
        if (name.Length > maxSymbolNameLength)
        {
            _logger.LogWarning(
                "Symbol name truncated from {OriginalLength} to {MaxLength} chars",
                name.Length,
                maxSymbolNameLength);
            name = name.Substring(0, maxSymbolNameLength);
        }

        return name;
    }

    /// <summary>
    /// Sanitizes a file path for safe querying.
    /// </summary>
    public string SanitizeFilePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return string.Empty;
        }

        // Check for path traversal
        if (PathTraversalPattern.IsMatch(path))
        {
            _logger.LogWarning("Path traversal attempt detected: {Path}", path);
            throw new SecurityException($"Invalid path pattern detected");
        }

        // Normalize path separators
        path = path.Replace('\\', '/');
        
        return path;
    }

    /// <summary>
    /// Creates a parameterized query to prevent SQL injection.
    /// </summary>
    public SqliteCommand CreateSafeQuery(
        SqliteConnection connection,
        string baseQuery,
        Dictionary<string, object> parameters)
    {
        var command = connection.CreateCommand();
        command.CommandText = baseQuery;

        foreach (var (key, value) in parameters)
        {
            var sanitizedValue = value switch
            {
                string s => SanitizeSymbolName(s, "query"),
                _ => value
            };
            command.Parameters.AddWithValue($"@{key}", sanitizedValue ?? DBNull.Value);
        }

        return command;
    }
}
```

---

### Threat 2: Denial of Service via Large or Malformed Files

**Risk Level:** HIGH  
**Attack Vector:** An attacker introduces extremely large files, deeply nested code, or infinite loop constructs to exhaust memory or CPU during extraction.

**Scenario:** A 500MB generated file or infinitely recursive type definition causes the extractor to hang or crash.

**Complete Mitigation Code:**

```csharp
using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Acode.Infrastructure.Symbols.Security;

/// <summary>
/// Guards against resource exhaustion during symbol extraction.
/// Enforces file size, parsing time, memory, and complexity limits.
/// </summary>
public sealed class ExtractionResourceGuard : IExtractionResourceGuard
{
    private readonly ILogger<ExtractionResourceGuard> _logger;
    private readonly ExtractionLimits _limits;

    public ExtractionResourceGuard(
        ILogger<ExtractionResourceGuard> logger,
        ExtractionLimits limits)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _limits = limits ?? throw new ArgumentNullException(nameof(limits));
    }

    /// <summary>
    /// Validates file size before extraction.
    /// </summary>
    public bool ValidateFileSize(string filePath, long fileSize)
    {
        if (fileSize > _limits.MaxFileSizeBytes)
        {
            _logger.LogWarning(
                "File {FilePath} exceeds size limit: {ActualSize} > {MaxSize} bytes",
                filePath,
                fileSize,
                _limits.MaxFileSizeBytes);
            return false;
        }
        return true;
    }

    /// <summary>
    /// Executes extraction with timeout protection.
    /// </summary>
    public async Task<ExtractionResult> ExecuteWithTimeoutAsync(
        string filePath,
        Func<CancellationToken, Task<IReadOnlyList<ISymbol>>> extractionFunc,
        CancellationToken cancellationToken = default)
    {
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(_limits.MaxExtractionTimeMs);

        var stopwatch = Stopwatch.StartNew();
        var memoryBefore = GC.GetTotalMemory(false);

        try
        {
            var symbols = await extractionFunc(timeoutCts.Token);
            stopwatch.Stop();

            var memoryAfter = GC.GetTotalMemory(false);
            var memoryUsed = memoryAfter - memoryBefore;

            // Log metrics
            _logger.LogDebug(
                "Extraction completed for {FilePath}: {SymbolCount} symbols, " +
                "{Duration}ms, {Memory} bytes",
                filePath,
                symbols.Count,
                stopwatch.ElapsedMilliseconds,
                memoryUsed);

            // Validate symbol count (potential zip bomb equivalent)
            if (symbols.Count > _limits.MaxSymbolsPerFile)
            {
                _logger.LogWarning(
                    "File {FilePath} produced excessive symbols: {Count} > {Max}",
                    filePath,
                    symbols.Count,
                    _limits.MaxSymbolsPerFile);
                
                return new ExtractionResult
                {
                    Success = false,
                    FilePath = filePath,
                    Error = $"Symbol count exceeds limit: {symbols.Count}",
                    Symbols = symbols.Take(_limits.MaxSymbolsPerFile).ToList()
                };
            }

            return new ExtractionResult
            {
                Success = true,
                FilePath = filePath,
                Symbols = symbols,
                DurationMs = stopwatch.ElapsedMilliseconds,
                MemoryUsedBytes = memoryUsed
            };
        }
        catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested)
        {
            stopwatch.Stop();
            _logger.LogWarning(
                "Extraction timeout for {FilePath} after {Duration}ms (limit: {Limit}ms)",
                filePath,
                stopwatch.ElapsedMilliseconds,
                _limits.MaxExtractionTimeMs);

            return new ExtractionResult
            {
                Success = false,
                FilePath = filePath,
                Error = "Extraction timeout exceeded",
                DurationMs = stopwatch.ElapsedMilliseconds
            };
        }
        catch (OutOfMemoryException)
        {
            _logger.LogError(
                "Out of memory during extraction of {FilePath}",
                filePath);
            
            // Force garbage collection
            GC.Collect(2, GCCollectionMode.Forced, blocking: true);
            
            return new ExtractionResult
            {
                Success = false,
                FilePath = filePath,
                Error = "Memory limit exceeded"
            };
        }
    }
}

/// <summary>
/// Configuration limits for extraction resource guard.
/// </summary>
public sealed class ExtractionLimits
{
    public long MaxFileSizeBytes { get; init; } = 5 * 1024 * 1024; // 5 MB
    public int MaxExtractionTimeMs { get; init; } = 30_000; // 30 seconds
    public int MaxSymbolsPerFile { get; init; } = 10_000;
    public long MaxMemoryPerExtractionBytes { get; init; } = 100 * 1024 * 1024; // 100 MB
    public int MaxNestingDepth { get; init; } = 50;
}

public sealed class ExtractionResult
{
    public bool Success { get; init; }
    public required string FilePath { get; init; }
    public string? Error { get; init; }
    public IReadOnlyList<ISymbol> Symbols { get; init; } = Array.Empty<ISymbol>();
    public long DurationMs { get; init; }
    public long MemoryUsedBytes { get; init; }
}
```

---

### Threat 3: Unauthorized File Access via Path Manipulation

**Risk Level:** MEDIUM  
**Attack Vector:** An attacker attempts to access files outside the repository boundary by exploiting path handling vulnerabilities.

**Scenario:** A crafted query or extracted symbol path references `../../../etc/passwd` or similar sensitive paths.

**Complete Mitigation Code:**

```csharp
using Microsoft.Extensions.Logging;

namespace Acode.Infrastructure.Symbols.Security;

/// <summary>
/// Validates and enforces repository boundary for all file operations.
/// Prevents path traversal and access to files outside workspace.
/// </summary>
public sealed class RepositoryBoundaryGuard : IRepositoryBoundaryGuard
{
    private readonly ILogger<RepositoryBoundaryGuard> _logger;
    private readonly string _repositoryRoot;
    private readonly string _normalizedRoot;

    public RepositoryBoundaryGuard(
        ILogger<RepositoryBoundaryGuard> logger,
        string repositoryRoot)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        
        if (string.IsNullOrWhiteSpace(repositoryRoot))
        {
            throw new ArgumentException("Repository root cannot be empty", nameof(repositoryRoot));
        }

        _repositoryRoot = repositoryRoot;
        _normalizedRoot = NormalizePath(Path.GetFullPath(repositoryRoot));

        if (!Directory.Exists(_repositoryRoot))
        {
            throw new DirectoryNotFoundException($"Repository root not found: {repositoryRoot}");
        }
    }

    /// <summary>
    /// Validates that a path is within the repository boundary.
    /// </summary>
    public bool IsWithinBoundary(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return false;
        }

        try
        {
            // Resolve to absolute path
            var absolutePath = Path.IsPathRooted(path)
                ? path
                : Path.Combine(_repositoryRoot, path);

            // Get canonical path (resolves symlinks, .., etc.)
            var normalizedPath = NormalizePath(Path.GetFullPath(absolutePath));

            // Check if path starts with repository root
            var isWithin = normalizedPath.StartsWith(_normalizedRoot, StringComparison.OrdinalIgnoreCase);

            if (!isWithin)
            {
                _logger.LogWarning(
                    "Path outside repository boundary rejected: {Path} (root: {Root})",
                    path,
                    _repositoryRoot);
            }

            return isWithin;
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
        {
            _logger.LogWarning(ex, "Invalid path format: {Path}", path);
            return false;
        }
    }

    /// <summary>
    /// Converts a repository-relative path to absolute, with boundary validation.
    /// </summary>
    public string ToAbsolutePath(string relativePath)
    {
        if (!IsWithinBoundary(relativePath))
        {
            throw new UnauthorizedAccessException(
                $"Access denied: path is outside repository boundary");
        }

        return Path.GetFullPath(Path.Combine(_repositoryRoot, relativePath));
    }

    /// <summary>
    /// Converts an absolute path to repository-relative, with boundary validation.
    /// </summary>
    public string ToRelativePath(string absolutePath)
    {
        if (!IsWithinBoundary(absolutePath))
        {
            throw new UnauthorizedAccessException(
                $"Access denied: path is outside repository boundary");
        }

        var normalizedAbsolute = NormalizePath(Path.GetFullPath(absolutePath));
        return normalizedAbsolute.Substring(_normalizedRoot.Length).TrimStart(Path.DirectorySeparatorChar);
    }

    /// <summary>
    /// Validates file path before extraction.
    /// </summary>
    public void ValidateExtractionPath(string filePath)
    {
        // Check boundary
        if (!IsWithinBoundary(filePath))
        {
            throw new SecurityException($"File path outside repository: {filePath}");
        }

        // Check for null bytes (C string terminator attacks)
        if (filePath.Contains('\0'))
        {
            throw new SecurityException("Null byte detected in file path");
        }

        // Check for device paths on Windows
        var fileName = Path.GetFileName(filePath);
        var reservedNames = new[] { "CON", "PRN", "AUX", "NUL", 
            "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9",
            "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9" };
        
        var nameWithoutExt = Path.GetFileNameWithoutExtension(fileName).ToUpperInvariant();
        if (reservedNames.Contains(nameWithoutExt))
        {
            throw new SecurityException($"Reserved device name in path: {fileName}");
        }
    }

    private static string NormalizePath(string path)
    {
        // Normalize to forward slashes and ensure trailing separator
        return path.Replace('\\', '/').TrimEnd('/') + '/';
    }
}
```

---

### Threat 4: Index Database Tampering

**Risk Level:** MEDIUM  
**Attack Vector:** An attacker directly modifies the SQLite database file to inject malicious symbol data or corrupt the index.

**Scenario:** Database file is modified to include fake symbols pointing to malicious code locations, misleading developers.

**Complete Mitigation Code:**

```csharp
using System.Security.Cryptography;
using System.Text;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;

namespace Acode.Infrastructure.Symbols.Security;

/// <summary>
/// Provides integrity verification for the symbol index database.
/// Detects tampering and corruption of index data.
/// </summary>
public sealed class IndexIntegrityGuard : IIndexIntegrityGuard
{
    private readonly ILogger<IndexIntegrityGuard> _logger;
    private readonly string _databasePath;
    private readonly string _checksumPath;

    public IndexIntegrityGuard(
        ILogger<IndexIntegrityGuard> logger,
        string databasePath)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _databasePath = databasePath ?? throw new ArgumentNullException(nameof(databasePath));
        _checksumPath = databasePath + ".checksum";
    }

    /// <summary>
    /// Computes and stores the database checksum after modifications.
    /// </summary>
    public async Task UpdateChecksumAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(_databasePath))
        {
            return;
        }

        var checksum = await ComputeDatabaseChecksumAsync(cancellationToken);
        var metadata = new IntegrityMetadata
        {
            Checksum = checksum,
            Timestamp = DateTimeOffset.UtcNow,
            Version = GetSchemaVersion()
        };

        await File.WriteAllTextAsync(
            _checksumPath,
            System.Text.Json.JsonSerializer.Serialize(metadata),
            cancellationToken);

        _logger.LogDebug("Index checksum updated: {Checksum}", checksum.Substring(0, 16));
    }

    /// <summary>
    /// Verifies database integrity before querying.
    /// </summary>
    public async Task<IntegrityCheckResult> VerifyIntegrityAsync(
        CancellationToken cancellationToken = default)
    {
        if (!File.Exists(_databasePath))
        {
            return IntegrityCheckResult.DatabaseNotFound;
        }

        // Check for checksum file
        if (!File.Exists(_checksumPath))
        {
            _logger.LogWarning("Index checksum file missing - rebuilding required");
            return IntegrityCheckResult.ChecksumMissing;
        }

        try
        {
            // Load stored metadata
            var metadataJson = await File.ReadAllTextAsync(_checksumPath, cancellationToken);
            var stored = System.Text.Json.JsonSerializer.Deserialize<IntegrityMetadata>(metadataJson);

            if (stored == null)
            {
                return IntegrityCheckResult.ChecksumCorrupted;
            }

            // Compute current checksum
            var currentChecksum = await ComputeDatabaseChecksumAsync(cancellationToken);

            // Compare checksums
            if (!string.Equals(stored.Checksum, currentChecksum, StringComparison.Ordinal))
            {
                _logger.LogError(
                    "Index integrity check FAILED - database may have been tampered with. " +
                    "Stored: {Stored}, Current: {Current}",
                    stored.Checksum.Substring(0, 16),
                    currentChecksum.Substring(0, 16));

                return IntegrityCheckResult.TamperingDetected;
            }

            // Run SQLite integrity check
            if (!await RunSqliteIntegrityCheckAsync(cancellationToken))
            {
                return IntegrityCheckResult.DatabaseCorrupted;
            }

            _logger.LogDebug("Index integrity verified successfully");
            return IntegrityCheckResult.Valid;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Index integrity check failed with exception");
            return IntegrityCheckResult.CheckError;
        }
    }

    private async Task<string> ComputeDatabaseChecksumAsync(CancellationToken cancellationToken)
    {
        await using var stream = File.OpenRead(_databasePath);
        var hash = await SHA256.HashDataAsync(stream, cancellationToken);
        return Convert.ToHexString(hash);
    }

    private async Task<bool> RunSqliteIntegrityCheckAsync(CancellationToken cancellationToken)
    {
        await using var connection = new SqliteConnection($"Data Source={_databasePath};Mode=ReadOnly");
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA integrity_check;";
        
        var result = await command.ExecuteScalarAsync(cancellationToken);
        return result?.ToString() == "ok";
    }

    private int GetSchemaVersion()
    {
        if (!File.Exists(_databasePath))
        {
            return 0;
        }

        using var connection = new SqliteConnection($"Data Source={_databasePath};Mode=ReadOnly");
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA user_version;";
        
        var result = command.ExecuteScalar();
        return Convert.ToInt32(result);
    }
}

public enum IntegrityCheckResult
{
    Valid,
    DatabaseNotFound,
    ChecksumMissing,
    ChecksumCorrupted,
    TamperingDetected,
    DatabaseCorrupted,
    CheckError
}

public sealed class IntegrityMetadata
{
    public required string Checksum { get; init; }
    public DateTimeOffset Timestamp { get; init; }
    public int Version { get; init; }
}
```

---

### Threat 5: Sensitive Data Exposure via Symbol Index

**Risk Level:** MEDIUM  
**Attack Vector:** The symbol index inadvertently stores sensitive information (API keys, passwords, connection strings) from source code.

**Scenario:** A developer commits code with hardcoded credentials; the symbol index stores these in queryable form.

**Complete Mitigation Code:**

```csharp
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace Acode.Infrastructure.Symbols.Security;

/// <summary>
/// Detects and redacts sensitive information from symbol data.
/// Prevents exposure of secrets, credentials, and PII.
/// </summary>
public sealed class SensitiveDataFilter : ISensitiveDataFilter
{
    private readonly ILogger<SensitiveDataFilter> _logger;
    private readonly List<SensitivePattern> _patterns;

    public SensitiveDataFilter(ILogger<SensitiveDataFilter> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _patterns = InitializePatterns();
    }

    /// <summary>
    /// Checks if a symbol name contains potentially sensitive information.
    /// </summary>
    public SensitiveDataCheckResult CheckSymbolName(string symbolName)
    {
        if (string.IsNullOrEmpty(symbolName))
        {
            return SensitiveDataCheckResult.Clean;
        }

        foreach (var pattern in _patterns)
        {
            if (pattern.Regex.IsMatch(symbolName))
            {
                return new SensitiveDataCheckResult
                {
                    IsSensitive = true,
                    Category = pattern.Category,
                    Description = pattern.Description
                };
            }
        }

        return SensitiveDataCheckResult.Clean;
    }

    /// <summary>
    /// Checks if a symbol signature contains hardcoded sensitive values.
    /// </summary>
    public SensitiveDataCheckResult CheckSignature(string? signature)
    {
        if (string.IsNullOrEmpty(signature))
        {
            return SensitiveDataCheckResult.Clean;
        }

        // Check for hardcoded strings that look like secrets
        var secretPatterns = new[]
        {
            // API keys (various formats)
            @"['""][A-Za-z0-9_\-]{20,}['""]",
            // Connection strings
            @"(?i)(password|pwd|secret|key)\s*=\s*['""][^'""]+['""]",
            // AWS access keys
            @"AKIA[0-9A-Z]{16}",
            // Private keys
            @"-----BEGIN\s+(RSA\s+)?PRIVATE\s+KEY-----",
            // JWT tokens
            @"eyJ[A-Za-z0-9_-]*\.eyJ[A-Za-z0-9_-]*\.[A-Za-z0-9_-]*"
        };

        foreach (var pattern in secretPatterns)
        {
            if (Regex.IsMatch(signature, pattern))
            {
                _logger.LogWarning(
                    "Potential secret detected in symbol signature - consider removing from source");
                
                return new SensitiveDataCheckResult
                {
                    IsSensitive = true,
                    Category = "HardcodedSecret",
                    Description = "Signature contains potential hardcoded secret"
                };
            }
        }

        return SensitiveDataCheckResult.Clean;
    }

    /// <summary>
    /// Redacts sensitive portions of a signature for safe storage.
    /// </summary>
    public string RedactSignature(string? signature)
    {
        if (string.IsNullOrEmpty(signature))
        {
            return string.Empty;
        }

        var redacted = signature;

        // Redact quoted strings that look like secrets (>16 chars)
        redacted = Regex.Replace(
            redacted,
            @"(['""])[A-Za-z0-9_\-]{16,}\1",
            "$1[REDACTED]$1");

        // Redact password parameters
        redacted = Regex.Replace(
            redacted,
            @"(?i)(password|pwd|secret|apikey|token)\s*=\s*['""][^'""]*['""]",
            "$1=[REDACTED]");

        return redacted;
    }

    /// <summary>
    /// Filters symbol list to exclude sensitive patterns.
    /// </summary>
    public IReadOnlyList<ISymbol> FilterSymbols(IReadOnlyList<ISymbol> symbols)
    {
        var filtered = new List<ISymbol>(symbols.Count);

        foreach (var symbol in symbols)
        {
            var nameCheck = CheckSymbolName(symbol.Name);
            var sigCheck = CheckSignature(symbol.Signature);

            if (nameCheck.IsSensitive || sigCheck.IsSensitive)
            {
                _logger.LogDebug(
                    "Filtering sensitive symbol: {Name} ({Category})",
                    symbol.Name,
                    nameCheck.Category ?? sigCheck.Category);
                continue;
            }

            filtered.Add(symbol);
        }

        if (filtered.Count < symbols.Count)
        {
            _logger.LogInformation(
                "Filtered {Count} potentially sensitive symbols from index",
                symbols.Count - filtered.Count);
        }

        return filtered;
    }

    private static List<SensitivePattern> InitializePatterns()
    {
        return new List<SensitivePattern>
        {
            new("Password|Credential|Secret|ApiKey|PrivateKey", 
                "Credential", "Credential-related symbol name"),
            new(@"Token|AccessToken|RefreshToken|BearerToken",
                "Token", "Token-related symbol name"),
            new(@"ConnectionString|DbPassword",
                "Connection", "Database connection credential"),
            new(@"SocialSecurityNumber|SSN|TaxId",
                "PII", "Personally identifiable information"),
            new(@"CreditCard|CardNumber|CVV|CVC",
                "Financial", "Financial data field")
        };
    }
}

public sealed class SensitivePattern
{
    public Regex Regex { get; }
    public string Category { get; }
    public string Description { get; }

    public SensitivePattern(string pattern, string category, string description)
    {
        Regex = new Regex(pattern, RegexOptions.IgnoreCase | RegexOptions.Compiled);
        Category = category;
        Description = description;
    }
}

public sealed class SensitiveDataCheckResult
{
    public bool IsSensitive { get; init; }
    public string? Category { get; init; }
    public string? Description { get; init; }

    public static SensitiveDataCheckResult Clean => new() { IsSensitive = false };
}
```

---

## Best Practices

### Index Design

1. **Separate storage from extraction** - Index format independent of language parsers
2. **Support incremental updates** - Only re-extract changed files
3. **Include relationships** - Store call graphs, inheritance, not just symbols
4. **Version the schema** - Allow index format evolution with migration

### Query Optimization

5. **Index by multiple keys** - Name, file, type for fast lookups
6. **Support prefix queries** - Find symbols starting with pattern
7. **Fuzzy matching optional** - Exact match fast, fuzzy if needed
8. **Limit result sets** - Pagination for large result sets

### Scalability

9. **Handle large codebases** - Tested with 100K+ files
10. **Background building** - Don't block user operations
11. **Memory bounded** - Stream processing for large files
12. **Parallel extraction** - Utilize multiple cores for parsing

---

## Testing Requirements

### Unit Tests

```
Tests/Unit/Symbols/
├── SymbolTests.cs
├── SymbolLocationTests.cs
├── SymbolStoreTests.cs
├── SymbolIndexTests.cs
├── ExtractorRegistryTests.cs
├── ExtractorConfigTests.cs
└── SymbolResolutionTests.cs
```

### Complete Test Code: SymbolTests.cs

```csharp
using Acode.Domain.Symbols;
using FluentAssertions;
using Xunit;

namespace Acode.Domain.Tests.Symbols;

public sealed class SymbolTests
{
    [Fact]
    public void Symbol_Should_Store_All_Properties()
    {
        // Arrange
        var id = Guid.NewGuid();
        var containingId = Guid.NewGuid();
        var location = new SymbolLocation
        {
            FilePath = "src/Services/UserService.cs",
            StartLine = 25,
            EndLine = 45,
            StartColumn = 5,
            EndColumn = 6
        };

        // Act
        var symbol = new Symbol
        {
            Id = id,
            Name = "GetUserById",
            FullyQualifiedName = "MyApp.Services.UserService.GetUserById",
            Kind = SymbolKind.Method,
            Location = location,
            Signature = "public async Task<User> GetUserById(Guid id)",
            Visibility = "public",
            ContainingSymbolId = containingId
        };

        // Assert
        symbol.Id.Should().Be(id);
        symbol.Name.Should().Be("GetUserById");
        symbol.FullyQualifiedName.Should().Be("MyApp.Services.UserService.GetUserById");
        symbol.Kind.Should().Be(SymbolKind.Method);
        symbol.Location.Should().Be(location);
        symbol.Signature.Should().Be("public async Task<User> GetUserById(Guid id)");
        symbol.Visibility.Should().Be("public");
        symbol.ContainingSymbolId.Should().Be(containingId);
    }

    [Theory]
    [InlineData(SymbolKind.Namespace)]
    [InlineData(SymbolKind.Class)]
    [InlineData(SymbolKind.Interface)]
    [InlineData(SymbolKind.Struct)]
    [InlineData(SymbolKind.Enum)]
    [InlineData(SymbolKind.Method)]
    [InlineData(SymbolKind.Property)]
    [InlineData(SymbolKind.Field)]
    [InlineData(SymbolKind.Constructor)]
    [InlineData(SymbolKind.Function)]
    [InlineData(SymbolKind.Variable)]
    [InlineData(SymbolKind.TypeAlias)]
    public void Symbol_Should_Support_All_Kinds(SymbolKind kind)
    {
        // Arrange & Act
        var symbol = new Symbol
        {
            Id = Guid.NewGuid(),
            Name = "TestSymbol",
            FullyQualifiedName = "Test.TestSymbol",
            Kind = kind,
            Location = CreateTestLocation(),
            Visibility = "public"
        };

        // Assert
        symbol.Kind.Should().Be(kind);
    }

    [Fact]
    public void Symbol_Equality_Should_Be_Based_On_Id()
    {
        // Arrange
        var id = Guid.NewGuid();
        var symbol1 = new Symbol
        {
            Id = id,
            Name = "Symbol1",
            FullyQualifiedName = "Test.Symbol1",
            Kind = SymbolKind.Class,
            Location = CreateTestLocation(),
            Visibility = "public"
        };

        var symbol2 = new Symbol
        {
            Id = id,  // Same ID
            Name = "DifferentName",  // Different name
            FullyQualifiedName = "Test.DifferentName",
            Kind = SymbolKind.Interface,  // Different kind
            Location = CreateTestLocation(),
            Visibility = "internal"
        };

        // Act & Assert
        symbol1.Equals(symbol2).Should().BeTrue();
        symbol1.GetHashCode().Should().Be(symbol2.GetHashCode());
    }

    [Fact]
    public void Symbol_Should_Allow_Null_Optional_Properties()
    {
        // Arrange & Act
        var symbol = new Symbol
        {
            Id = Guid.NewGuid(),
            Name = "SimpleClass",
            FullyQualifiedName = "SimpleClass",
            Kind = SymbolKind.Class,
            Location = CreateTestLocation(),
            Signature = null,  // Optional
            Visibility = "public",
            ContainingSymbolId = null  // Top-level
        };

        // Assert
        symbol.Signature.Should().BeNull();
        symbol.ContainingSymbolId.Should().BeNull();
    }

    private static SymbolLocation CreateTestLocation() => new()
    {
        FilePath = "test.cs",
        StartLine = 1,
        EndLine = 1,
        StartColumn = 1,
        EndColumn = 10
    };
}
```

### Complete Test Code: SymbolStoreTests.cs

```csharp
using Acode.Domain.Symbols;
using Acode.Infrastructure.Symbols;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Acode.Infrastructure.Tests.Symbols;

public sealed class SymbolStoreTests : IAsyncLifetime
{
    private readonly SymbolStore _store;
    private readonly string _dbPath;

    public SymbolStoreTests()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"symbols_test_{Guid.NewGuid()}.db");
        _store = new SymbolStore(NullLogger<SymbolStore>.Instance, _dbPath);
    }

    public async Task InitializeAsync()
    {
        await _store.InitializeAsync();
    }

    public Task DisposeAsync()
    {
        _store.Dispose();
        if (File.Exists(_dbPath))
        {
            File.Delete(_dbPath);
        }
        return Task.CompletedTask;
    }

    [Fact]
    public async Task AddAsync_Should_Store_Symbol()
    {
        // Arrange
        var symbol = CreateTestSymbol("UserService", SymbolKind.Class);

        // Act
        await _store.AddAsync(symbol);
        var retrieved = await _store.GetByIdAsync(symbol.Id);

        // Assert
        retrieved.Should().NotBeNull();
        retrieved!.Name.Should().Be("UserService");
        retrieved.Kind.Should().Be(SymbolKind.Class);
    }

    [Fact]
    public async Task AddBatchAsync_Should_Store_Multiple_Symbols()
    {
        // Arrange
        var symbols = new[]
        {
            CreateTestSymbol("Class1", SymbolKind.Class),
            CreateTestSymbol("Class2", SymbolKind.Class),
            CreateTestSymbol("Method1", SymbolKind.Method)
        };

        // Act
        await _store.AddBatchAsync(symbols);
        var results = await _store.SearchAsync(new SymbolQuery());

        // Assert
        results.Should().HaveCount(3);
    }

    [Fact]
    public async Task RemoveAsync_Should_Delete_Symbol()
    {
        // Arrange
        var symbol = CreateTestSymbol("ToDelete", SymbolKind.Class);
        await _store.AddAsync(symbol);

        // Act
        await _store.RemoveAsync(symbol.Id);
        var retrieved = await _store.GetByIdAsync(symbol.Id);

        // Assert
        retrieved.Should().BeNull();
    }

    [Fact]
    public async Task RemoveByFileAsync_Should_Delete_All_Symbols_In_File()
    {
        // Arrange
        var filePath = "src/Services/TestService.cs";
        var symbols = new[]
        {
            CreateTestSymbol("Method1", SymbolKind.Method, filePath),
            CreateTestSymbol("Method2", SymbolKind.Method, filePath),
            CreateTestSymbol("OtherFile", SymbolKind.Method, "other.cs")
        };
        await _store.AddBatchAsync(symbols);

        // Act
        await _store.RemoveByFileAsync(filePath);
        var remainingResults = await _store.SearchAsync(new SymbolQuery());

        // Assert
        remainingResults.Should().HaveCount(1);
        remainingResults.First().Name.Should().Be("OtherFile");
    }

    [Fact]
    public async Task SearchAsync_Should_Match_Exact_Name()
    {
        // Arrange
        await _store.AddBatchAsync(new[]
        {
            CreateTestSymbol("UserService", SymbolKind.Class),
            CreateTestSymbol("UserRepository", SymbolKind.Class),
            CreateTestSymbol("OrderService", SymbolKind.Class)
        });

        // Act
        var results = await _store.SearchAsync(new SymbolQuery
        {
            NamePattern = "UserService",
            MatchType = MatchType.Exact
        });

        // Assert
        results.Should().HaveCount(1);
        results.First().Name.Should().Be("UserService");
    }

    [Fact]
    public async Task SearchAsync_Should_Match_Prefix()
    {
        // Arrange
        await _store.AddBatchAsync(new[]
        {
            CreateTestSymbol("UserService", SymbolKind.Class),
            CreateTestSymbol("UserRepository", SymbolKind.Class),
            CreateTestSymbol("OrderService", SymbolKind.Class)
        });

        // Act
        var results = await _store.SearchAsync(new SymbolQuery
        {
            NamePattern = "User*",
            MatchType = MatchType.Prefix
        });

        // Assert
        results.Should().HaveCount(2);
        results.Select(s => s.Name).Should().Contain(new[] { "UserService", "UserRepository" });
    }

    [Fact]
    public async Task SearchAsync_Should_Filter_By_Kind()
    {
        // Arrange
        await _store.AddBatchAsync(new[]
        {
            CreateTestSymbol("UserService", SymbolKind.Class),
            CreateTestSymbol("IUserService", SymbolKind.Interface),
            CreateTestSymbol("GetUser", SymbolKind.Method)
        });

        // Act
        var results = await _store.SearchAsync(new SymbolQuery
        {
            Kinds = new[] { SymbolKind.Class }
        });

        // Assert
        results.Should().HaveCount(1);
        results.First().Kind.Should().Be(SymbolKind.Class);
    }

    [Fact]
    public async Task SearchAsync_Should_Filter_By_Visibility()
    {
        // Arrange
        var publicSymbol = CreateTestSymbol("PublicClass", SymbolKind.Class);
        var internalSymbol = CreateTestSymbol("InternalClass", SymbolKind.Class) with { Visibility = "internal" };
        await _store.AddBatchAsync(new[] { publicSymbol, internalSymbol });

        // Act
        var results = await _store.SearchAsync(new SymbolQuery
        {
            Visibility = new[] { "public" }
        });

        // Assert
        results.Should().HaveCount(1);
        results.First().Visibility.Should().Be("public");
    }

    [Fact]
    public async Task SearchAsync_Should_Support_Pagination()
    {
        // Arrange
        var symbols = Enumerable.Range(1, 100)
            .Select(i => CreateTestSymbol($"Class{i:D3}", SymbolKind.Class))
            .ToArray();
        await _store.AddBatchAsync(symbols);

        // Act
        var page1 = await _store.SearchAsync(new SymbolQuery { Skip = 0, Take = 10 });
        var page2 = await _store.SearchAsync(new SymbolQuery { Skip = 10, Take = 10 });

        // Assert
        page1.Should().HaveCount(10);
        page2.Should().HaveCount(10);
        page1.Select(s => s.Name).Should().NotIntersectWith(page2.Select(s => s.Name));
    }

    [Fact]
    public async Task SearchAsync_Should_Return_Empty_For_No_Matches()
    {
        // Arrange
        await _store.AddAsync(CreateTestSymbol("Existing", SymbolKind.Class));

        // Act
        var results = await _store.SearchAsync(new SymbolQuery
        {
            NamePattern = "NonExistent",
            MatchType = MatchType.Exact
        });

        // Assert
        results.Should().BeEmpty();
    }

    private static Symbol CreateTestSymbol(
        string name,
        SymbolKind kind,
        string? filePath = null) => new()
    {
        Id = Guid.NewGuid(),
        Name = name,
        FullyQualifiedName = $"Test.{name}",
        Kind = kind,
        Location = new SymbolLocation
        {
            FilePath = filePath ?? "test.cs",
            StartLine = 1,
            EndLine = 10,
            StartColumn = 1,
            EndColumn = 1
        },
        Visibility = "public"
    };
}
```

### Complete Test Code: SymbolIndexTests.cs

```csharp
using Acode.Domain.Symbols;
using Acode.Infrastructure.Symbols;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace Acode.Infrastructure.Tests.Symbols;

public sealed class SymbolIndexTests : IAsyncLifetime
{
    private readonly string _testDir;
    private readonly string _dbPath;
    private readonly ISymbolStore _store;
    private readonly IExtractorRegistry _registry;
    private readonly SymbolIndexService _indexService;

    public SymbolIndexTests()
    {
        _testDir = Path.Combine(Path.GetTempPath(), $"symbol_index_test_{Guid.NewGuid()}");
        _dbPath = Path.Combine(_testDir, "workspace.db");
        Directory.CreateDirectory(_testDir);

        _store = new SymbolStore(NullLogger<SymbolStore>.Instance, _dbPath);
        _registry = Substitute.For<IExtractorRegistry>();
        
        var mockExtractor = CreateMockExtractor();
        _registry.GetExtractor(".cs").Returns(mockExtractor);
        _registry.GetExtractor(".ts").Returns(mockExtractor);

        _indexService = new SymbolIndexService(
            NullLogger<SymbolIndexService>.Instance,
            _store,
            _registry,
            new SymbolIndexOptions { WorkerCount = 2 });
    }

    public async Task InitializeAsync()
    {
        await (_store as SymbolStore)!.InitializeAsync();
    }

    public Task DisposeAsync()
    {
        (_store as IDisposable)?.Dispose();
        if (Directory.Exists(_testDir))
        {
            Directory.Delete(_testDir, recursive: true);
        }
        return Task.CompletedTask;
    }

    [Fact]
    public async Task BuildAsync_Should_Index_All_Files()
    {
        // Arrange
        CreateTestFile("Service1.cs", "public class Service1 { }");
        CreateTestFile("Service2.cs", "public class Service2 { }");

        // Act
        var result = await _indexService.BuildAsync(_testDir);

        // Assert
        result.FilesIndexed.Should().Be(2);
        result.SymbolsExtracted.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task BuildAsync_Should_Report_Progress()
    {
        // Arrange
        CreateTestFile("File1.cs", "class A {}");
        CreateTestFile("File2.cs", "class B {}");
        CreateTestFile("File3.cs", "class C {}");

        var progressReports = new List<IndexProgress>();
        var progress = new Progress<IndexProgress>(p => progressReports.Add(p));

        // Act
        await _indexService.BuildAsync(_testDir, progress: progress);

        // Assert
        progressReports.Should().NotBeEmpty();
        progressReports.Last().PercentComplete.Should().Be(100);
    }

    [Fact]
    public async Task UpdateAsync_Should_Detect_Changed_Files()
    {
        // Arrange
        var filePath = CreateTestFile("Changeable.cs", "class Original {}");
        await _indexService.BuildAsync(_testDir);

        // Modify file
        await Task.Delay(100); // Ensure different timestamp
        File.WriteAllText(filePath, "class Modified { void NewMethod() {} }");

        // Act
        var result = await _indexService.UpdateAsync(_testDir);

        // Assert
        result.FilesUpdated.Should().Be(1);
    }

    [Fact]
    public async Task UpdateAsync_Should_Detect_New_Files()
    {
        // Arrange
        CreateTestFile("Existing.cs", "class Existing {}");
        await _indexService.BuildAsync(_testDir);

        // Add new file
        CreateTestFile("NewFile.cs", "class NewClass {}");

        // Act
        var result = await _indexService.UpdateAsync(_testDir);

        // Assert
        result.FilesAdded.Should().Be(1);
    }

    [Fact]
    public async Task UpdateAsync_Should_Remove_Deleted_File_Symbols()
    {
        // Arrange
        var filePath = CreateTestFile("ToDelete.cs", "class ToDelete {}");
        await _indexService.BuildAsync(_testDir);

        // Delete file
        File.Delete(filePath);

        // Act
        var result = await _indexService.UpdateAsync(_testDir);

        // Assert
        result.FilesRemoved.Should().Be(1);
        var symbols = await _store.SearchAsync(new SymbolQuery
        {
            FilePattern = "*ToDelete*"
        });
        symbols.Should().BeEmpty();
    }

    [Fact]
    public async Task BuildAsync_Should_Support_Cancellation()
    {
        // Arrange
        for (int i = 0; i < 100; i++)
        {
            CreateTestFile($"File{i}.cs", $"class Class{i} {{}}");
        }

        using var cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromMilliseconds(50));

        // Act
        var act = () => _indexService.BuildAsync(_testDir, cancellationToken: cts.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task GetStatusAsync_Should_Return_Accurate_Counts()
    {
        // Arrange
        CreateTestFile("File1.cs", "class A {} class B {}");
        CreateTestFile("File2.cs", "interface C {}");
        await _indexService.BuildAsync(_testDir);

        // Act
        var status = await _indexService.GetStatusAsync();

        // Assert
        status.FilesIndexed.Should().Be(2);
        status.SymbolCount.Should().BeGreaterThan(0);
        status.LastBuildTime.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromMinutes(1));
    }

    [Fact]
    public async Task ClearAsync_Should_Remove_All_Data()
    {
        // Arrange
        CreateTestFile("ToClear.cs", "class ToBeCleared {}");
        await _indexService.BuildAsync(_testDir);

        // Act
        await _indexService.ClearAsync();
        var status = await _indexService.GetStatusAsync();

        // Assert
        status.SymbolCount.Should().Be(0);
        status.FilesIndexed.Should().Be(0);
    }

    [Fact]
    public async Task BuildAsync_Should_Handle_Parse_Errors_Gracefully()
    {
        // Arrange
        CreateTestFile("Valid.cs", "class Valid {}");
        CreateTestFile("Invalid.cs", "this is not valid C# syntax }{][");

        // Act
        var result = await _indexService.BuildAsync(_testDir);

        // Assert
        result.FilesIndexed.Should().BeGreaterThanOrEqualTo(1);
        result.Errors.Should().HaveCountGreaterThan(0);
        result.Errors.First().FilePath.Should().Contain("Invalid.cs");
    }

    private string CreateTestFile(string name, string content)
    {
        var path = Path.Combine(_testDir, name);
        File.WriteAllText(path, content);
        return path;
    }

    private static ISymbolExtractor CreateMockExtractor()
    {
        var extractor = Substitute.For<ISymbolExtractor>();
        extractor.SupportedExtensions.Returns(new[] { ".cs", ".ts" });
        extractor.LanguageName.Returns("Test");
        
        extractor.ExtractAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                var path = callInfo.ArgAt<string>(0);
                return Task.FromResult<IReadOnlyList<ISymbol>>(new[]
                {
                    new Symbol
                    {
                        Id = Guid.NewGuid(),
                        Name = Path.GetFileNameWithoutExtension(path),
                        FullyQualifiedName = $"Test.{Path.GetFileNameWithoutExtension(path)}",
                        Kind = SymbolKind.Class,
                        Location = new SymbolLocation
                        {
                            FilePath = path,
                            StartLine = 1,
                            EndLine = 1,
                            StartColumn = 1,
                            EndColumn = 1
                        },
                        Visibility = "public"
                    }
                });
            });

        return extractor;
    }
}
```

### Integration Tests

```
Tests/Integration/Symbols/
├── SymbolStoreIntegrationTests.cs
├── SymbolIndexIntegrationTests.cs
└── QueryIntegrationTests.cs
```

### E2E Tests

```
Tests/E2E/Symbols/
├── SymbolE2ETests.cs
│   ├── Should_Build_Index_Via_CLI()
│   ├── Should_Build_With_Progress_Via_CLI()
│   ├── Should_Update_Index_Via_CLI()
│   ├── Should_Search_Symbols_Via_CLI()
│   ├── Should_Search_By_Kind_Via_CLI()
│   ├── Should_Show_Status_Via_CLI()
│   ├── Should_Clear_Index_Via_CLI()
│   └── Should_Integrate_With_Context_Packer()
```

### Performance Benchmarks

| Benchmark | Target | Maximum |
|-----------|--------|---------|
| Index 1K files | 20s | 30s |
| Index 10K files | 200s | 300s |
| Query by exact name | 30ms | 50ms |
| Query by prefix | 40ms | 75ms |
| Query by fuzzy match | 80ms | 150ms |
| Incremental update (10 files) | 200ms | 500ms |
| Database write (1K symbols) | 100ms | 200ms |

---

## User Verification Steps

### Scenario 1: Full Index Build

**Objective:** Verify that the symbol index can be built from scratch for a codebase.

**Prerequisites:**
- Acode CLI installed and configured
- Sample C# project with at least 10 files

**Steps:**
1. Navigate to project root directory
2. Run `acode symbols build --progress`
3. Observe progress indicator showing files processed
4. Wait for "Index build complete" message

**Expected Results:**
```bash
$ acode symbols build --progress
Building symbol index...
[========================================] 100% (47/47 files)
Index build complete:
  Files indexed: 47
  Symbols extracted: 1,234
  Duration: 3.2s
  Errors: 0
```

**Verification Checklist:**
- [ ] Progress indicator updates as files are processed
- [ ] Final message shows accurate file and symbol counts
- [ ] No error messages in output
- [ ] Database file created in .agent/workspace.db
- [ ] Exit code is 0

---

### Scenario 2: Symbol Search by Name

**Objective:** Verify that symbols can be found by exact and prefix name matching.

**Prerequisites:**
- Index built from Scenario 1
- Known class name in codebase (e.g., "UserService")

**Steps:**
1. Run exact search: `acode symbols search "UserService" --exact`
2. Run prefix search: `acode symbols search "User*"`
3. Run fuzzy search: `acode symbols search "UsrService" --fuzzy`

**Expected Results:**
```bash
$ acode symbols search "UserService" --exact
Symbols matching "UserService" (exact):
  1. UserService (class) - src/Services/UserService.cs:15
     Visibility: public
     Signature: public class UserService : IUserService

$ acode symbols search "User*"
Symbols matching "User*" (prefix):
  1. UserService (class) - src/Services/UserService.cs:15
  2. UserRepository (class) - src/Data/UserRepository.cs:8
  3. UserDto (class) - src/Models/UserDto.cs:5
  4. UserController (class) - src/Controllers/UserController.cs:12

$ acode symbols search "UsrService" --fuzzy
Symbols matching "UsrService" (fuzzy, distance ≤ 2):
  1. UserService (class) - src/Services/UserService.cs:15 [distance: 1]
```

**Verification Checklist:**
- [ ] Exact search returns only exact matches
- [ ] Prefix search returns all symbols starting with pattern
- [ ] Fuzzy search finds similar names with typos
- [ ] Results include file path and line number
- [ ] Results include symbol kind and visibility

---

### Scenario 3: Search by Symbol Kind

**Objective:** Verify that symbols can be filtered by type (class, method, interface, etc.).

**Prerequisites:**
- Index built with multiple symbol types

**Steps:**
1. Search for classes only: `acode symbols search "*" --kind class`
2. Search for methods only: `acode symbols search "Get*" --kind method`
3. Search for interfaces only: `acode symbols search "I*" --kind interface`

**Expected Results:**
```bash
$ acode symbols search "*" --kind class --take 5
Classes in codebase:
  1. UserService (class) - src/Services/UserService.cs:15
  2. OrderService (class) - src/Services/OrderService.cs:12
  3. ProductRepository (class) - src/Data/ProductRepository.cs:8
  4. ApiController (class) - src/Controllers/ApiController.cs:10
  5. DatabaseContext (class) - src/Data/DatabaseContext.cs:14
Showing 5 of 45 results. Use --take to see more.

$ acode symbols search "Get*" --kind method
Methods matching "Get*":
  1. GetById (method) - UserService.cs:45
  2. GetAll (method) - UserService.cs:60
  3. GetProducts (method) - ProductRepository.cs:28
```

**Verification Checklist:**
- [ ] Kind filter excludes symbols of other types
- [ ] Results are paginated with count shown
- [ ] Multiple kinds can be combined (--kind class,interface)

---

### Scenario 4: Incremental Update After Code Changes

**Objective:** Verify that the index updates efficiently when files change.

**Prerequisites:**
- Index built from Scenario 1

**Steps:**
1. Note current index status: `acode symbols status`
2. Add a new method to an existing file
3. Create a new file with a new class
4. Delete an existing file
5. Run incremental update: `acode symbols update`
6. Verify changes reflected in search

**Expected Results:**
```bash
$ acode symbols status
Symbol Index Status (before):
  Files indexed: 47
  Symbols: 1,234
  Last build: 2024-01-15 10:30:00

# After making changes...

$ acode symbols update
Incremental update complete:
  Files added: 1
  Files updated: 1
  Files removed: 1
  Symbols added: 8
  Symbols removed: 5
  Duration: 0.4s

$ acode symbols status
Symbol Index Status (after):
  Files indexed: 47 (no change in count)
  Symbols: 1,237 (+3)
  Last update: 2024-01-15 14:45:00
```

**Verification Checklist:**
- [ ] New file's symbols appear in search
- [ ] Modified file's new method appears in search
- [ ] Deleted file's symbols no longer in search
- [ ] Update duration is much faster than full build
- [ ] Status reflects accurate counts

---

### Scenario 5: Index Status and Diagnostics

**Objective:** Verify comprehensive status reporting and diagnostics.

**Prerequisites:**
- Index built with multiple languages

**Steps:**
1. Check basic status: `acode symbols status`
2. Check detailed status: `acode symbols status --detailed`
3. Verify integrity: `acode symbols verify`

**Expected Results:**
```bash
$ acode symbols status
Symbol Index Status
═══════════════════════
Files indexed: 1,234
Total symbols: 45,678
Last full build: 2024-01-15 10:30:00
Last update: 2024-01-15 14:45:00

$ acode symbols status --detailed
Symbol Index Detailed Status
════════════════════════════

By Language:
  C# (.cs): 28,450 symbols (650 files)
  TypeScript (.ts): 15,200 symbols (480 files)
  JavaScript (.js): 2,028 symbols (104 files)

By Kind:
  Classes: 1,250 (2.7%)
  Interfaces: 340 (0.7%)
  Methods: 28,500 (62.4%)
  Properties: 12,100 (26.5%)
  Functions: 3,488 (7.6%)

Database:
  Path: .agent/workspace.db
  Size: 12.4 MB
  Schema version: 3

$ acode symbols verify
Index Integrity Check
═════════════════════
Database checksum: OK
SQLite integrity: OK
Orphaned symbols: 0
Duplicate IDs: 0
Hash consistency: OK

Result: ✅ Index is healthy
```

**Verification Checklist:**
- [ ] Status shows file and symbol counts
- [ ] Detailed status shows breakdown by language and kind
- [ ] Verify command checks all integrity aspects
- [ ] Database path and size shown

---

### Scenario 6: Search with File Path Filter

**Objective:** Verify that searches can be scoped to specific directories or files.

**Prerequisites:**
- Index built with files in multiple directories

**Steps:**
1. Search in specific directory: `acode symbols search "Get*" --file "src/Services/**"`
2. Search in specific file: `acode symbols search "*" --file "*UserService*"`
3. Exclude directory: `acode symbols search "*" --exclude-file "**/Tests/**"`

**Expected Results:**
```bash
$ acode symbols search "Get*" --file "src/Services/**"
Symbols matching "Get*" in src/Services/:
  1. GetById (method) - UserService.cs:45
  2. GetAll (method) - UserService.cs:60
  3. GetOrder (method) - OrderService.cs:35

$ acode symbols search "*" --file "*UserService*" --kind method
Methods in UserService:
  1. GetById (method) - UserService.cs:45
  2. GetAll (method) - UserService.cs:60
  3. CreateUser (method) - UserService.cs:78
  4. UpdateUser (method) - UserService.cs:95
  5. DeleteUser (method) - UserService.cs:112
```

**Verification Checklist:**
- [ ] File pattern filter limits results to matching paths
- [ ] Glob patterns work correctly (**, *)
- [ ] Exclude pattern removes matching files from results

---

### Scenario 7: Clear and Rebuild Index

**Objective:** Verify index can be completely reset and rebuilt.

**Prerequisites:**
- Existing index with data

**Steps:**
1. Verify current index has data: `acode symbols status`
2. Clear the index: `acode symbols clear`
3. Verify index is empty: `acode symbols status`
4. Rebuild: `acode symbols build`

**Expected Results:**
```bash
$ acode symbols status
Files indexed: 47
Symbols: 1,234

$ acode symbols clear
Are you sure you want to clear the symbol index? [y/N] y
Symbol index cleared.

$ acode symbols status
Files indexed: 0
Symbols: 0
Last build: never

$ acode symbols build --progress
Building symbol index...
[========================================] 100%
Index build complete.
```

**Verification Checklist:**
- [ ] Clear command asks for confirmation
- [ ] Status shows zero counts after clear
- [ ] Rebuild works correctly after clear
- [ ] Database file is reset or deleted

---

### Scenario 8: Handle Parse Errors Gracefully

**Objective:** Verify that malformed files don't break indexing.

**Prerequisites:**
- Codebase with at least one file containing syntax errors

**Steps:**
1. Create a file with invalid syntax
2. Run build with verbose output: `acode symbols build --verbose`
3. Verify other files are still indexed
4. Check error reporting

**Expected Results:**
```bash
$ acode symbols build --verbose
Building symbol index...
Processing: src/Valid.cs (12 symbols)
Processing: src/Services/UserService.cs (45 symbols)
WARNING: Parse error in src/Broken.cs
  Line 15: Unexpected token ';'
  Skipping file, continuing...
Processing: src/Models/User.cs (8 symbols)
...

Index build complete:
  Files indexed: 46 of 47
  Symbols extracted: 1,220
  Errors: 1
  Duration: 3.1s

Files with errors:
  - src/Broken.cs: Unexpected token ';' at line 15
```

**Verification Checklist:**
- [ ] Build completes despite parse errors
- [ ] Valid files are indexed correctly
- [ ] Error details show file and line number
- [ ] Summary shows error count
- [ ] Exit code is 0 (partial success) or 1 (if --strict)

---

## Implementation Prompt

### File Structure

```
src/Acode.Domain/
├── Symbols/
│   ├── ISymbol.cs
│   ├── Symbol.cs
│   ├── SymbolKind.cs
│   ├── SymbolLocation.cs
│   ├── SymbolQuery.cs
│   ├── ISymbolStore.cs
│   ├── ISymbolExtractor.cs
│   ├── IExtractorRegistry.cs
│   └── ISymbolIndex.cs

src/Acode.Infrastructure/
├── Symbols/
│   ├── SymbolStore.cs
│   ├── ExtractorRegistry.cs
│   ├── SymbolIndexService.cs
│   └── Security/
│       ├── SymbolSanitizer.cs
│       ├── ExtractionResourceGuard.cs
│       └── RepositoryBoundaryGuard.cs
```

### Complete Implementation: ISymbol.cs

```csharp
namespace Acode.Domain.Symbols;

/// <summary>
/// Represents a code symbol extracted from source code.
/// Symbols include classes, methods, properties, fields, and other named code elements.
/// </summary>
public interface ISymbol
{
    /// <summary>
    /// Unique identifier for this symbol instance.
    /// </summary>
    Guid Id { get; }

    /// <summary>
    /// Simple name of the symbol (e.g., "GetUserById").
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Fully qualified name including namespace/module path
    /// (e.g., "MyApp.Services.UserService.GetUserById").
    /// </summary>
    string FullyQualifiedName { get; }

    /// <summary>
    /// The kind of symbol (Class, Method, Property, etc.).
    /// </summary>
    SymbolKind Kind { get; }

    /// <summary>
    /// Location of this symbol in source code.
    /// </summary>
    SymbolLocation Location { get; }

    /// <summary>
    /// Method/property signature if applicable.
    /// For methods: "public async Task<User> GetUserById(Guid id)"
    /// For properties: "public string Name { get; set; }"
    /// </summary>
    string? Signature { get; }

    /// <summary>
    /// Visibility modifier: public, private, protected, internal.
    /// </summary>
    string Visibility { get; }

    /// <summary>
    /// Reference to the containing symbol (e.g., the class containing a method).
    /// Null for top-level symbols.
    /// </summary>
    Guid? ContainingSymbolId { get; }

    /// <summary>
    /// The programming language of this symbol.
    /// </summary>
    string Language { get; }
}
```

### Complete Implementation: Symbol.cs

```csharp
namespace Acode.Domain.Symbols;

/// <summary>
/// Concrete implementation of ISymbol using records for immutability.
/// </summary>
public sealed record Symbol : ISymbol
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public required string FullyQualifiedName { get; init; }
    public required SymbolKind Kind { get; init; }
    public required SymbolLocation Location { get; init; }
    public string? Signature { get; init; }
    public required string Visibility { get; init; }
    public Guid? ContainingSymbolId { get; init; }
    public string Language { get; init; } = "unknown";

    /// <summary>
    /// Equality is based solely on Id for deduplication purposes.
    /// </summary>
    public bool Equals(Symbol? other) => other is not null && Id == other.Id;
    
    public override int GetHashCode() => Id.GetHashCode();
}
```

### Complete Implementation: SymbolKind.cs

```csharp
namespace Acode.Domain.Symbols;

/// <summary>
/// Enumeration of all supported symbol kinds.
/// </summary>
public enum SymbolKind
{
    /// <summary>Namespace or module declaration.</summary>
    Namespace,
    
    /// <summary>Class declaration.</summary>
    Class,
    
    /// <summary>Interface declaration.</summary>
    Interface,
    
    /// <summary>Struct/record declaration.</summary>
    Struct,
    
    /// <summary>Enum declaration.</summary>
    Enum,
    
    /// <summary>Method or function member of a class.</summary>
    Method,
    
    /// <summary>Property member.</summary>
    Property,
    
    /// <summary>Field member.</summary>
    Field,
    
    /// <summary>Constructor.</summary>
    Constructor,
    
    /// <summary>Standalone function (TypeScript/JavaScript).</summary>
    Function,
    
    /// <summary>Variable declaration.</summary>
    Variable,
    
    /// <summary>Type alias (TypeScript).</summary>
    TypeAlias,
    
    /// <summary>Enum member.</summary>
    EnumMember,
    
    /// <summary>Event declaration.</summary>
    Event,
    
    /// <summary>Delegate declaration.</summary>
    Delegate
}
```

### Complete Implementation: SymbolLocation.cs

```csharp
namespace Acode.Domain.Symbols;

/// <summary>
/// Represents the source location of a symbol.
/// </summary>
public sealed record SymbolLocation
{
    /// <summary>
    /// Relative file path from repository root.
    /// </summary>
    public required string FilePath { get; init; }

    /// <summary>
    /// 1-based starting line number.
    /// </summary>
    public required int StartLine { get; init; }

    /// <summary>
    /// 1-based ending line number.
    /// </summary>
    public required int EndLine { get; init; }

    /// <summary>
    /// 1-based starting column number.
    /// </summary>
    public required int StartColumn { get; init; }

    /// <summary>
    /// 1-based ending column number.
    /// </summary>
    public required int EndColumn { get; init; }

    /// <summary>
    /// Calculates the span of lines this symbol occupies.
    /// </summary>
    public int LineSpan => EndLine - StartLine + 1;

    /// <summary>
    /// Creates a string representation for display.
    /// </summary>
    public override string ToString() => $"{FilePath}:{StartLine}";
}
```

### Complete Implementation: SymbolQuery.cs

```csharp
namespace Acode.Domain.Symbols;

/// <summary>
/// Query parameters for searching symbols.
/// </summary>
public sealed record SymbolQuery
{
    /// <summary>
    /// Name pattern to match. Supports wildcards (* for any chars).
    /// </summary>
    public string? NamePattern { get; init; }

    /// <summary>
    /// Type of matching to perform.
    /// </summary>
    public MatchType MatchType { get; init; } = MatchType.Prefix;

    /// <summary>
    /// Filter to specific symbol kinds.
    /// </summary>
    public IReadOnlyList<SymbolKind>? Kinds { get; init; }

    /// <summary>
    /// Filter to specific visibility levels.
    /// </summary>
    public IReadOnlyList<string>? Visibility { get; init; }

    /// <summary>
    /// File path pattern filter (glob syntax).
    /// </summary>
    public string? FilePattern { get; init; }

    /// <summary>
    /// Namespace prefix filter.
    /// </summary>
    public string? NamespacePrefix { get; init; }

    /// <summary>
    /// Filter to symbols contained within a specific symbol.
    /// </summary>
    public Guid? ContainingSymbolId { get; init; }

    /// <summary>
    /// Filter to specific languages.
    /// </summary>
    public IReadOnlyList<string>? Languages { get; init; }

    /// <summary>
    /// Number of results to skip (for pagination).
    /// </summary>
    public int Skip { get; init; }

    /// <summary>
    /// Maximum number of results to return.
    /// </summary>
    public int Take { get; init; } = 100;

    /// <summary>
    /// Field to order results by.
    /// </summary>
    public OrderBy OrderBy { get; init; } = OrderBy.Name;

    /// <summary>
    /// Order direction.
    /// </summary>
    public bool Descending { get; init; }
}

/// <summary>
/// Type of name matching for queries.
/// </summary>
public enum MatchType
{
    Exact,
    Prefix,
    Contains,
    Fuzzy
}

/// <summary>
/// Fields to order results by.
/// </summary>
public enum OrderBy
{
    Name,
    FullyQualifiedName,
    FilePath,
    Kind,
    Relevance
}
```

### Complete Implementation: ISymbolStore.cs

```csharp
namespace Acode.Domain.Symbols;

/// <summary>
/// Persistent storage for symbols with CRUD and query operations.
/// </summary>
public interface ISymbolStore
{
    /// <summary>
    /// Initializes the store (creates database, runs migrations).
    /// </summary>
    Task InitializeAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a single symbol to the store.
    /// </summary>
    Task AddAsync(ISymbol symbol, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds multiple symbols efficiently in a single transaction.
    /// </summary>
    Task AddBatchAsync(
        IEnumerable<ISymbol> symbols,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a symbol by its ID.
    /// </summary>
    Task RemoveAsync(Guid symbolId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes all symbols from a specific file.
    /// </summary>
    Task RemoveByFileAsync(string filePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing symbol.
    /// </summary>
    Task UpdateAsync(ISymbol symbol, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a symbol by its ID.
    /// </summary>
    Task<ISymbol?> GetByIdAsync(Guid symbolId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all symbols from a specific file.
    /// </summary>
    Task<IReadOnlyList<ISymbol>> GetByFileAsync(
        string filePath,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches for symbols matching the query criteria.
    /// </summary>
    Task<IReadOnlyList<ISymbol>> SearchAsync(
        SymbolQuery query,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the total count of stored symbols.
    /// </summary>
    Task<int> GetCountAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Clears all symbols from the store.
    /// </summary>
    Task ClearAsync(CancellationToken cancellationToken = default);
}
```

### Complete Implementation: ISymbolExtractor.cs

```csharp
namespace Acode.Domain.Symbols;

/// <summary>
/// Extracts symbols from source code files.
/// Language-specific implementations parse source and emit symbols.
/// </summary>
public interface ISymbolExtractor
{
    /// <summary>
    /// File extensions this extractor handles (e.g., ".cs", ".ts").
    /// </summary>
    IReadOnlyList<string> SupportedExtensions { get; }

    /// <summary>
    /// Name of the language (e.g., "C#", "TypeScript").
    /// </summary>
    string LanguageName { get; }

    /// <summary>
    /// Extracts symbols from a file on disk.
    /// </summary>
    Task<ExtractionResult> ExtractAsync(
        string filePath,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Extracts symbols from in-memory content.
    /// </summary>
    Task<ExtractionResult> ExtractFromContentAsync(
        string content,
        string virtualPath,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of symbol extraction from a file.
/// </summary>
public sealed record ExtractionResult
{
    /// <summary>
    /// Whether extraction completed successfully.
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// Path of the extracted file.
    /// </summary>
    public required string FilePath { get; init; }

    /// <summary>
    /// Extracted symbols.
    /// </summary>
    public IReadOnlyList<ISymbol> Symbols { get; init; } = Array.Empty<ISymbol>();

    /// <summary>
    /// Parse errors encountered during extraction.
    /// </summary>
    public IReadOnlyList<ParseError> Errors { get; init; } = Array.Empty<ParseError>();

    /// <summary>
    /// Time taken for extraction in milliseconds.
    /// </summary>
    public long DurationMs { get; init; }
}

/// <summary>
/// Represents a parse error during extraction.
/// </summary>
public sealed record ParseError
{
    public required string FilePath { get; init; }
    public required int Line { get; init; }
    public required int Column { get; init; }
    public required string Message { get; init; }
    public string? Code { get; init; }
}
```

### Complete Implementation: IExtractorRegistry.cs

```csharp
namespace Acode.Domain.Symbols;

/// <summary>
/// Registry for language-specific symbol extractors.
/// </summary>
public interface IExtractorRegistry
{
    /// <summary>
    /// Registers an extractor for its supported extensions.
    /// </summary>
    void RegisterExtractor(ISymbolExtractor extractor);

    /// <summary>
    /// Gets the extractor for a file extension.
    /// </summary>
    ISymbolExtractor? GetExtractor(string fileExtension);

    /// <summary>
    /// Gets the extractor by language name.
    /// </summary>
    ISymbolExtractor? GetExtractorByLanguage(string languageName);

    /// <summary>
    /// Lists all supported file extensions.
    /// </summary>
    IReadOnlyList<string> SupportedExtensions { get; }

    /// <summary>
    /// Lists all supported language names.
    /// </summary>
    IReadOnlyList<string> SupportedLanguages { get; }

    /// <summary>
    /// Sets a fallback extractor for unknown file types.
    /// </summary>
    void SetFallbackExtractor(ISymbolExtractor? extractor);
}
```

### Complete Implementation: ISymbolIndex.cs

```csharp
namespace Acode.Domain.Symbols;

/// <summary>
/// Symbol index service for building and maintaining the symbol index.
/// </summary>
public interface ISymbolIndex
{
    /// <summary>
    /// Builds a complete index from scratch.
    /// </summary>
    Task<IndexBuildResult> BuildAsync(
        string rootPath,
        IProgress<IndexProgress>? progress = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs incremental update for changed files.
    /// </summary>
    Task<IndexUpdateResult> UpdateAsync(
        string rootPath,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets current index status.
    /// </summary>
    Task<IndexStatus> GetStatusAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Clears the entire index.
    /// </summary>
    Task ClearAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Verifies index integrity.
    /// </summary>
    Task<IntegrityResult> VerifyAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of a full index build operation.
/// </summary>
public sealed record IndexBuildResult
{
    public int FilesIndexed { get; init; }
    public int SymbolsExtracted { get; init; }
    public int FilesSkipped { get; init; }
    public IReadOnlyList<ParseError> Errors { get; init; } = Array.Empty<ParseError>();
    public TimeSpan Duration { get; init; }
}

/// <summary>
/// Result of an incremental update operation.
/// </summary>
public sealed record IndexUpdateResult
{
    public int FilesAdded { get; init; }
    public int FilesUpdated { get; init; }
    public int FilesRemoved { get; init; }
    public int SymbolsAdded { get; init; }
    public int SymbolsRemoved { get; init; }
    public IReadOnlyList<ParseError> Errors { get; init; } = Array.Empty<ParseError>();
    public TimeSpan Duration { get; init; }
}

/// <summary>
/// Current index status information.
/// </summary>
public sealed record IndexStatus
{
    public int FilesIndexed { get; init; }
    public int SymbolCount { get; init; }
    public DateTimeOffset? LastBuildTime { get; init; }
    public DateTimeOffset? LastUpdateTime { get; init; }
    public IReadOnlyDictionary<string, int> SymbolsByLanguage { get; init; } 
        = new Dictionary<string, int>();
    public IReadOnlyDictionary<SymbolKind, int> SymbolsByKind { get; init; } 
        = new Dictionary<SymbolKind, int>();
}

/// <summary>
/// Progress report during indexing.
/// </summary>
public sealed record IndexProgress
{
    public int FilesProcessed { get; init; }
    public int TotalFiles { get; init; }
    public int PercentComplete => TotalFiles > 0 
        ? (int)(100.0 * FilesProcessed / TotalFiles) 
        : 0;
    public string? CurrentFile { get; init; }
}

/// <summary>
/// Result of integrity verification.
/// </summary>
public sealed record IntegrityResult
{
    public bool IsValid { get; init; }
    public IReadOnlyList<string> Issues { get; init; } = Array.Empty<string>();
}
```

### Complete Implementation: SymbolStore.cs

```csharp
using System.Text;
using Acode.Domain.Symbols;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;

namespace Acode.Infrastructure.Symbols;

/// <summary>
/// SQLite-backed implementation of ISymbolStore.
/// </summary>
public sealed class SymbolStore : ISymbolStore, IDisposable
{
    private readonly ILogger<SymbolStore> _logger;
    private readonly string _databasePath;
    private SqliteConnection? _connection;
    private bool _disposed;

    public SymbolStore(ILogger<SymbolStore> logger, string databasePath)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _databasePath = databasePath ?? throw new ArgumentNullException(nameof(databasePath));
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        var directory = Path.GetDirectoryName(_databasePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        _connection = new SqliteConnection($"Data Source={_databasePath}");
        await _connection.OpenAsync(cancellationToken);

        // Enable WAL mode for better concurrency
        await ExecuteNonQueryAsync("PRAGMA journal_mode=WAL;", cancellationToken);
        await ExecuteNonQueryAsync("PRAGMA synchronous=NORMAL;", cancellationToken);

        await CreateSchemaAsync(cancellationToken);
        _logger.LogDebug("Symbol store initialized at {Path}", _databasePath);
    }

    private async Task CreateSchemaAsync(CancellationToken cancellationToken)
    {
        const string schema = """
            CREATE TABLE IF NOT EXISTS symbols (
                id TEXT PRIMARY KEY,
                name TEXT NOT NULL,
                fully_qualified_name TEXT NOT NULL,
                kind INTEGER NOT NULL,
                file_path TEXT NOT NULL,
                start_line INTEGER NOT NULL,
                end_line INTEGER NOT NULL,
                start_column INTEGER NOT NULL,
                end_column INTEGER NOT NULL,
                signature TEXT,
                visibility TEXT NOT NULL,
                containing_symbol_id TEXT,
                language TEXT NOT NULL
            );

            CREATE INDEX IF NOT EXISTS idx_symbols_name ON symbols(name);
            CREATE INDEX IF NOT EXISTS idx_symbols_fqn ON symbols(fully_qualified_name);
            CREATE INDEX IF NOT EXISTS idx_symbols_kind ON symbols(kind);
            CREATE INDEX IF NOT EXISTS idx_symbols_file_path ON symbols(file_path);
            CREATE INDEX IF NOT EXISTS idx_symbols_visibility ON symbols(visibility);

            CREATE TABLE IF NOT EXISTS file_hashes (
                file_path TEXT PRIMARY KEY,
                hash TEXT NOT NULL,
                indexed_at TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS index_metadata (
                key TEXT PRIMARY KEY,
                value TEXT NOT NULL
            );
            """;

        await ExecuteNonQueryAsync(schema, cancellationToken);
    }

    public async Task AddAsync(ISymbol symbol, CancellationToken cancellationToken = default)
    {
        EnsureNotDisposed();
        const string sql = """
            INSERT INTO symbols (id, name, fully_qualified_name, kind, file_path, 
                start_line, end_line, start_column, end_column, signature, 
                visibility, containing_symbol_id, language)
            VALUES (@id, @name, @fqn, @kind, @filePath, @startLine, @endLine, 
                @startCol, @endCol, @signature, @visibility, @containingId, @language);
            """;

        await using var cmd = CreateCommand(sql);
        AddSymbolParameters(cmd, symbol);
        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task AddBatchAsync(
        IEnumerable<ISymbol> symbols,
        CancellationToken cancellationToken = default)
    {
        EnsureNotDisposed();
        await using var transaction = await _connection!.BeginTransactionAsync(cancellationToken);

        try
        {
            foreach (var symbol in symbols)
            {
                await AddAsync(symbol, cancellationToken);
            }
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task RemoveAsync(Guid symbolId, CancellationToken cancellationToken = default)
    {
        EnsureNotDisposed();
        const string sql = "DELETE FROM symbols WHERE id = @id;";
        await using var cmd = CreateCommand(sql);
        cmd.Parameters.AddWithValue("@id", symbolId.ToString());
        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task RemoveByFileAsync(string filePath, CancellationToken cancellationToken = default)
    {
        EnsureNotDisposed();
        const string sql = "DELETE FROM symbols WHERE file_path = @filePath;";
        await using var cmd = CreateCommand(sql);
        cmd.Parameters.AddWithValue("@filePath", filePath);
        await cmd.ExecuteNonQueryAsync(cancellationToken);
        _logger.LogDebug("Removed symbols for file: {FilePath}", filePath);
    }

    public async Task<IReadOnlyList<ISymbol>> SearchAsync(
        SymbolQuery query,
        CancellationToken cancellationToken = default)
    {
        EnsureNotDisposed();
        var sql = new StringBuilder("SELECT * FROM symbols WHERE 1=1");
        var parameters = new Dictionary<string, object>();

        if (!string.IsNullOrEmpty(query.NamePattern))
        {
            var pattern = query.MatchType switch
            {
                MatchType.Exact => query.NamePattern,
                MatchType.Prefix => query.NamePattern.TrimEnd('*') + "%",
                MatchType.Contains => "%" + query.NamePattern.Trim('*') + "%",
                _ => query.NamePattern
            };
            sql.Append(" AND name LIKE @namePattern");
            parameters["@namePattern"] = pattern;
        }

        if (query.Kinds?.Count > 0)
        {
            var kindValues = string.Join(",", query.Kinds.Select(k => (int)k));
            sql.Append($" AND kind IN ({kindValues})");
        }

        if (query.Visibility?.Count > 0)
        {
            var visParams = query.Visibility
                .Select((v, i) => $"@vis{i}")
                .ToList();
            sql.Append($" AND visibility IN ({string.Join(",", visParams)})");
            for (int i = 0; i < query.Visibility.Count; i++)
            {
                parameters[$"@vis{i}"] = query.Visibility[i];
            }
        }

        if (!string.IsNullOrEmpty(query.FilePattern))
        {
            sql.Append(" AND file_path LIKE @filePattern");
            parameters["@filePattern"] = query.FilePattern.Replace("*", "%");
        }

        sql.Append($" ORDER BY {GetOrderByColumn(query.OrderBy)}");
        if (query.Descending) sql.Append(" DESC");
        sql.Append($" LIMIT {query.Take} OFFSET {query.Skip}");

        await using var cmd = CreateCommand(sql.ToString());
        foreach (var (key, value) in parameters)
        {
            cmd.Parameters.AddWithValue(key, value);
        }

        var results = new List<ISymbol>();
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(ReadSymbol(reader));
        }
        return results;
    }

    public async Task<int> GetCountAsync(CancellationToken cancellationToken = default)
    {
        EnsureNotDisposed();
        await using var cmd = CreateCommand("SELECT COUNT(*) FROM symbols;");
        var result = await cmd.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt32(result);
    }

    public async Task ClearAsync(CancellationToken cancellationToken = default)
    {
        EnsureNotDisposed();
        await ExecuteNonQueryAsync("DELETE FROM symbols;", cancellationToken);
        await ExecuteNonQueryAsync("DELETE FROM file_hashes;", cancellationToken);
        _logger.LogInformation("Symbol store cleared");
    }

    // Helper methods...
    private SqliteCommand CreateCommand(string sql)
    {
        var cmd = _connection!.CreateCommand();
        cmd.CommandText = sql;
        return cmd;
    }

    private async Task ExecuteNonQueryAsync(string sql, CancellationToken cancellationToken)
    {
        await using var cmd = CreateCommand(sql);
        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }

    private static void AddSymbolParameters(SqliteCommand cmd, ISymbol symbol)
    {
        cmd.Parameters.AddWithValue("@id", symbol.Id.ToString());
        cmd.Parameters.AddWithValue("@name", symbol.Name);
        cmd.Parameters.AddWithValue("@fqn", symbol.FullyQualifiedName);
        cmd.Parameters.AddWithValue("@kind", (int)symbol.Kind);
        cmd.Parameters.AddWithValue("@filePath", symbol.Location.FilePath);
        cmd.Parameters.AddWithValue("@startLine", symbol.Location.StartLine);
        cmd.Parameters.AddWithValue("@endLine", symbol.Location.EndLine);
        cmd.Parameters.AddWithValue("@startCol", symbol.Location.StartColumn);
        cmd.Parameters.AddWithValue("@endCol", symbol.Location.EndColumn);
        cmd.Parameters.AddWithValue("@signature", symbol.Signature ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@visibility", symbol.Visibility);
        cmd.Parameters.AddWithValue("@containingId", 
            symbol.ContainingSymbolId?.ToString() ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@language", symbol.Language);
    }

    private static Symbol ReadSymbol(SqliteDataReader reader) => new()
    {
        Id = Guid.Parse(reader.GetString(0)),
        Name = reader.GetString(1),
        FullyQualifiedName = reader.GetString(2),
        Kind = (SymbolKind)reader.GetInt32(3),
        Location = new SymbolLocation
        {
            FilePath = reader.GetString(4),
            StartLine = reader.GetInt32(5),
            EndLine = reader.GetInt32(6),
            StartColumn = reader.GetInt32(7),
            EndColumn = reader.GetInt32(8)
        },
        Signature = reader.IsDBNull(9) ? null : reader.GetString(9),
        Visibility = reader.GetString(10),
        ContainingSymbolId = reader.IsDBNull(11) ? null : Guid.Parse(reader.GetString(11)),
        Language = reader.GetString(12)
    };

    private static string GetOrderByColumn(OrderBy orderBy) => orderBy switch
    {
        OrderBy.Name => "name",
        OrderBy.FullyQualifiedName => "fully_qualified_name",
        OrderBy.FilePath => "file_path",
        OrderBy.Kind => "kind",
        _ => "name"
    };

    private void EnsureNotDisposed()
    {
        if (_disposed) throw new ObjectDisposedException(nameof(SymbolStore));
        if (_connection == null) throw new InvalidOperationException("Store not initialized");
    }

    public void Dispose()
    {
        if (_disposed) return;
        _connection?.Dispose();
        _disposed = true;
    }

    // Additional required interface methods...
    public Task UpdateAsync(ISymbol symbol, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<ISymbol?> GetByIdAsync(Guid symbolId, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<IReadOnlyList<ISymbol>> GetByFileAsync(string filePath, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}
```

### Error Codes

| Code | Meaning | Resolution |
|------|---------|------------|
| ACODE-SYM-001 | Parse error during extraction | Check file syntax, verify language version |
| ACODE-SYM-002 | Symbol store database error | Check database path, verify permissions |
| ACODE-SYM-003 | Index build/update failure | Check file access, verify extractor registry |
| ACODE-SYM-004 | Query execution error | Verify query parameters, check database state |
| ACODE-SYM-005 | Extractor not found | Register extractor for file extension |
| ACODE-SYM-006 | Timeout during extraction | Reduce file size limit or increase timeout |
| ACODE-SYM-007 | Database corruption detected | Run `acode symbols clear` and rebuild |
| ACODE-SYM-008 | Path validation failure | Check file path is within repository |

### Implementation Checklist

1. [ ] Create Symbol domain model (ISymbol, Symbol, SymbolKind, SymbolLocation)
2. [ ] Create SymbolQuery and related types
3. [ ] Implement ISymbolStore with SQLite persistence
4. [ ] Create database schema with indexes
5. [ ] Implement ISymbolExtractor interface
6. [ ] Create ExtractorRegistry for language mapping
7. [ ] Implement ISymbolIndex for build/update orchestration
8. [ ] Add file hash tracking for incremental updates
9. [ ] Implement CLI commands (symbols build, search, update, status)
10. [ ] Add security components (sanitizer, resource guard, boundary guard)
11. [ ] Add comprehensive unit tests
12. [ ] Add integration tests with real database
13. [ ] Add E2E tests for CLI commands
14. [ ] Performance benchmarking and optimization

### Rollout Plan

1. **Phase 1 (Week 1):** Symbol model and store
   - Define all domain types
   - Implement SQLite store with CRUD
   - Unit tests for model and store

2. **Phase 2 (Week 2):** Extractor infrastructure
   - Define extractor interface
   - Implement registry
   - Create mock extractor for testing

3. **Phase 3 (Week 3):** Index service
   - Full build implementation
   - Incremental update with hash tracking
   - Progress reporting

4. **Phase 4 (Week 4):** CLI integration
   - Add all symbol commands
   - Integration testing
   - Documentation

5. **Phase 5 (Week 5):** Security and persistence
   - Add security guards
   - Database integrity checks
   - Performance optimization

---

**End of Task 017 Specification**