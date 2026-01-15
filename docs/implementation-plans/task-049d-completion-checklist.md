# Task-049d Completion Checklist: Indexing + Fast Search Over Chats/Runs/Messages

**Status:** 🔴 0% COMPLETE (Clean Slate)

**Objective:** Implement 132 acceptance criteria across 25 files using TDD with BM25 relevance ranking and FTS5/PostgreSQL backends

**Methodology:** RED → GREEN → REFACTOR with per-gap commits

**Effort Estimate:** 90-100 hours total (6 phases, 14-16 days)

---

## INSTRUCTIONS FOR FRESH AGENT

This checklist guides full-text search implementation. Follow these steps:

1. **Read this entire document** - Understand full scope and phase dependencies
2. **For each gap in order:**
   - Write test code FIRST (RED)
   - Implement minimum production code (GREEN)
   - Refactor for clarity
   - Mark gap [✅] complete
   - Commit: `feat(task-049d/phase-N): [Gap title]`
3. **Verify performance targets** at end of each phase
4. **Performance is critical** - Multiple ACs require specific latency/throughput
5. **Audit before PR** - Follow docs/AUDIT-GUIDELINES.md

**Success Criteria:** All 132 ACs passing tests, performance targets met, 85%+ coverage

---

## WHAT EXISTS

**Available from other tasks:**
- ✅ Message, Chat, Run entities (Task 049a)
- ✅ Event system: IEventPublisher (Task 023)
- ✅ SQLite integration (Task 049a)

**NOT Available (blockers):**
- ❌ None - Task is self-contained

**Must Create (All New):**
- ❌ All 25 production files
- ❌ All 10 test files
- ❌ All database migrations
- ❌ FTS5 virtual table configuration

---

## SUMMARY BY PHASE

| Phase | Objective | Hours | Tests | ACs |
|-------|-----------|-------|-------|-----|
| 0 | Domain models & interfaces | 3-4 | 0 | 2 |
| 1 | Indexing (FTS5/Postgres) | 15-18 | 8 | 24 |
| 2 | Search engine & ranking | 18-22 | 12 | 30 |
| 3 | Snippets & highlighting | 6-8 | 6 | 12 |
| 4 | Filtering & advanced queries | 6-8 | 6 | 26 |
| 5 | CLI commands & maintenance | 6-8 | 6 | 26 |
| 6 | Integration & verification | 4-6 | - | 12 |
| **TOTAL** | | **88-100h** | **38+** | **132** |

---

## PHASE-BY-PHASE BREAKDOWN

### Phase 0: Domain Models & Interfaces (3-4 hours)
- Gap 1: SearchQuery entity with parsed terms, filters, operators
- Gap 2: SearchResult record with score, snippet, match info
- Gap 3-6: Application interfaces (ISearchIndexer, ISearchEngine, IQueryParser, ISnippetGenerator)
- **No tests** - Interfaces only
- **Success:** All 6 files compile

### Phase 1: Indexing Infrastructure (15-18 hours, 8 tests)
- Gap 7: FTS5 migration (virtual table creation)
- Gap 8: SqliteFtsIndexer with <10ms single message, <1s for batch
- Gap 9: PostgresSearchIndexer (alternate backend)
- Gap 10: SearchIndexEventHandler (automatic incremental indexing)
- Gap 11: IndexMaintenanceService (background optimization)
- Gap 12: PostgreSQL migration
- **Tests:** Indexing latency, batch processing, event handling
- **Performance:** AC-019 <10ms, AC-020 <1s/100, AC-021 <60s/10k

### Phase 2: Search & Ranking (18-22 hours, 12 tests)
- Gap 13: QueryParser (tokenize, expand, parse operators/fields)
- Gap 14: SqliteFtsSearchEngine (query execution with snippet/offsets)
- Gap 15: PostgresSearchEngine (alternate backend)
- Gap 16: BM25Ranker (relevance scoring algorithm)
- Gap 17: RecencyBooster (1.5x/1.2x/1.0x by age)
- **Tests:** Parser correctness, engine accuracy, ranking algorithm, latency
- **Performance:** AC-030 <5ms parse, AC-050 <1ms score, AC-128 <500ms search

### Phase 3: Snippets & Highlighting (6-8 hours, 6 tests)
- Gap 18: SnippetGenerator (150-char default, center on match, preserve words)
- Gap 19: HighlightFormatter (wrap terms in <mark> tags)
- Gap 20: ResultFormatter (table/JSON output with colors/tags)
- **Tests:** Snippet extraction, highlighting, format output
- **Performance:** AC-062 <50ms per snippet

### Phase 4: Filtering & Advanced Queries (6-8 hours, 6 tests)
- Gap 21: SearchFilters (chat, role, date filters with AND logic)
- Gap 22: Field-specific search (role:user, chat:id, tag:name)
- **Tests:** Individual filters, combined filters, date parsing, edge cases
- **Success:** All filter combinations work correctly

### Phase 5: CLI & Maintenance (6-8 hours, 6 tests)
- Gap 23: IndexOptimizer (segment merging, <30s/50k)
- Gap 24: IndexValidator (health check, corruption detection)
- Gap 25: SearchCommand CLI (`acode search <query> [options]`)
- Gap 26: SearchIndexCommand CLI (`acode search index {status|optimize|rebuild}`)
- **Tests:** CLI integration, index operations, output formatting
- **Success:** All CLI commands work end-to-end

### Phase 6: Integration & Verification (4-6 hours)
- Gap 27: E2E integration tests (create→index→search lifecycle)
- Gap 28: Performance benchmarks (all latency targets verified)
- Gap 29: Code coverage >85%, audit checklist
- **Success:** 132 ACs verified, performance targets met

---

## STATUS

**Status:** 🔴 READY FOR IMPLEMENTATION (0% Complete)

**Critical Path:**
1. Phase 0 (interfaces) → enables all other work
2. Phase 1 (indexing) → enables Phase 2-4 testing
3. Phase 2 (search) → core functionality
4. Phases 3-5 → complete feature set
5. Phase 6 → verification

**Next Action:** Start Phase 0 with SearchQuery entity

---
