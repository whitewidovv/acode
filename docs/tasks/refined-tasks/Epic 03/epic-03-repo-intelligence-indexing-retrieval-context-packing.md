# EPIC 3 — Repo Intelligence (Indexing, Retrieval, Context Packing)

**Priority:** P0 – Critical  
**Phase:** Phase 3 – Intelligence Layer  
**Dependencies:** Epic 02 (CLI + Agent Orchestration), Epic 00 (Constraints)  

---

## Epic Overview

Epic 3 implements the repository intelligence layer. This layer understands code. It indexes files. It extracts symbols. It packs context for the agent.

The RepoFS abstraction provides uniform file access. It works with local files. It works with Docker-mounted files. It provides atomic patch application.

Indexing builds searchable representations. Text search finds content. Symbol search finds code constructs. Ignore rules respect .gitignore.

The Context Packer assembles prompts. It chunks files appropriately. It ranks by relevance. It budgets tokens. It deduplicates content.

The Symbol Index extracts code structure. It understands C# classes, methods, properties. It understands TypeScript/JavaScript modules. It maps dependencies.

This epic operates fully offline. No external LLM APIs required for indexing. All processing happens locally. This aligns with Task 001 constraints.

### Boundaries

Epic 3 owns:
- File system abstraction
- Text indexing and search
- Symbol extraction and indexing
- Context assembly for prompts
- Dependency mapping

Epic 3 does NOT own:
- LLM interactions (Epic 04)
- Tool execution (Epic 02)
- Git operations (Epic 05)

### Integration Points

| Component | Interface | Direction |
|-----------|-----------|-----------|
| Agent Loop | IContextPacker | Consumer |
| Tool System | IRepoSearch | Consumer |
| File Tools | IRepoFS | Provider |
| Session State | Index metadata | Storage |

---

## Outcomes

1. RepoFS provides uniform file access
2. Local FS implementation works
3. Docker-mounted FS implementation works
4. Atomic patch application prevents partial changes
5. Text index enables full-text search
6. Ignore rules respect .gitignore
7. Search integrates with tool system
8. Index updates incrementally
9. Context packer chunks files appropriately
10. Ranking prioritizes relevant content
11. Token budgeting fits context window
12. Deduplication removes redundant content
13. C# symbol extraction works
14. TypeScript/JavaScript symbol extraction works
15. Dependency mapping tracks relationships
16. Retrieval APIs serve agent queries

---

## Non-Goals

1. Real-time file watching (v2)
2. Language server protocol integration
3. IDE integration
4. Remote repository indexing
5. Multi-repository search
6. Semantic code search (embedding-based)
7. Code compilation/execution
8. Syntax highlighting
9. Code formatting
10. Refactoring tools
11. Cross-repository dependencies
12. Package manager integration
13. Build system integration
14. Test discovery
15. Coverage integration

---

## Architecture & Integration Points

### RepoFS Layer

```
IRepoFS
├── IFileReader
├── IFileWriter
├── IDirectoryEnumerator
└── IPatchApplicator
```

### Indexing Layer

```
IIndexService
├── ITextIndex
├── ISymbolIndex
├── IIgnoreService
└── IIndexUpdater
```

### Context Layer

```
IContextPacker
├── IChunker
├── IRanker
├── ITokenBudgeter
└── IDeduplicator
```

### Data Flow

```
Files → RepoFS → Indexer → Index
                    ↓
Query → Search → Results → Ranker → Chunks → Budget → Context
```

---

## Operational Considerations

### Task 001 Compliance

- All indexing is local
- No external API calls
- Works in air-gapped mode
- Symbol extraction uses Roslyn/TypeScript locally

### Performance

- Index builds in background
- Incremental updates for changes
- Caching for repeated queries
- Lazy loading where possible

### Safety

- Read-only operations don't modify files
- Atomic patches prevent corruption
- Index corruption is recoverable

---

## Acceptance Criteria / Definition of Done

### RepoFS

- [ ] IRepoFS interface defined
- [ ] Local FS implementation works
- [ ] Docker FS implementation works
- [ ] File reading works
- [ ] File writing works
- [ ] Directory enumeration works
- [ ] Atomic patches work
- [ ] Patch rollback works

### Indexing

- [ ] Text index builds
- [ ] Text search works
- [ ] Ignore rules work
- [ ] .gitignore integration works
- [ ] Custom ignores work
- [ ] Search tool integration works
- [ ] Incremental updates work
- [ ] Index persistence works

### Context Packer

- [ ] Chunking works
- [ ] Ranking works
- [ ] Token budgeting works
- [ ] Deduplication works
- [ ] Context assembly works

### Symbol Index

- [ ] C# classes extracted
- [ ] C# methods extracted
- [ ] C# properties extracted
- [ ] TypeScript modules extracted
- [ ] JavaScript functions extracted
- [ ] Dependencies mapped
- [ ] Retrieval APIs work

---

## Risks & Mitigations

| Risk | Impact | Mitigation |
|------|--------|------------|
| Large repos slow indexing | High | Incremental updates, background processing |
| Memory pressure | High | Streaming, lazy loading |
| Complex .gitignore patterns | Medium | Well-tested parser |
| Symbol extraction accuracy | High | Comprehensive tests |
| Token counting accuracy | Medium | Use tiktoken |
| Dependency cycle detection | Medium | Cycle breaking logic |
| Docker mount latency | Medium | Caching, batching |
| Index corruption | High | Checksums, rebuild |
| Roslyn version conflicts | Medium | Isolated loading |
| TypeScript parser updates | Medium | Pin versions |
| Large files OOM | High | Streaming reads |
| Binary file detection | Medium | Magic number checks |

---

## Milestone Plan

### Milestone 1: RepoFS Foundation (Task 014)
- IRepoFS interface
- Local implementation
- Docker implementation
- Atomic patches

### Milestone 2: Basic Indexing (Task 015)
- Text indexing
- Ignore rules
- Search integration
- Updates

### Milestone 3: Context Assembly (Task 016)
- Chunking
- Ranking
- Token budgeting
- Deduplication

### Milestone 4: Symbol Intelligence (Task 017)
- C# extraction
- TS/JS extraction
- Dependencies
- Retrieval

---

## Definition of Epic Complete

- [ ] All Task 014 subtasks complete
- [ ] All Task 015 subtasks complete
- [ ] All Task 016 subtasks complete
- [ ] All Task 017 subtasks complete
- [ ] Integration tests pass
- [ ] Performance benchmarks met
- [ ] Documentation complete
- [ ] No P0/P1 bugs
- [ ] Code reviewed
- [ ] Merged to main

---

**End of Epic 3 Specification**