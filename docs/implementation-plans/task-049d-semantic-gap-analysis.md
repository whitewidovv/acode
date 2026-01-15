# Task-049d Semantic Gap Analysis: Indexing + Fast Search Over Chats/Runs/Messages

**Status:** ✅ GAP ANALYSIS COMPLETE - ~0% IMPLEMENTATION (Clean Slate)

**Date:** 2026-01-15

**Analyzed By:** Claude Code

**Methodology:** CLAUDE.md Section 3.2

**Spec Reference:** `docs/tasks/refined-tasks/Epic 02/task-049d-indexing-fast-search.md` (3596 lines)

---

## EXECUTIVE SUMMARY

**Semantic Completeness: ~0% (0/132 ACs) - Completely Unimplemented**

**Scope:** Full-text search indexing using SQLite FTS5 (primary) and PostgreSQL tsvector (secondary). Implements 3-layer architecture: Indexing → Search Engine → Ranking & Snippets.

**Result:** Clean slate. All 132 ACs must be implemented from scratch.

---

## SECTION 1: SPECIFICATION BREAKDOWN

### Acceptance Criteria Distribution

- **Total ACs:** 132 (AC-001 through AC-132)
- **Breakdown by Domain:**
  - Indexing: Content Capture (AC-001-010): 10 ACs
  - Indexing: Backend Implementation (AC-011-018): 8 ACs
  - Indexing: Performance (AC-019-024): 6 ACs
  - Search: Basic Queries (AC-025-031): 7 ACs
  - Search: Boolean Operators (AC-032-037): 6 ACs
  - Search: Field-Specific Queries (AC-038-043): 6 ACs
  - Ranking: Relevance (AC-044-050): 7 ACs
  - Ranking: Recency Boost (AC-051-056): 6 ACs
  - Snippets: Generation (AC-057-062): 6 ACs
  - Snippets: Highlighting (AC-063-068): 6 ACs
  - Filters: Chat Scope (AC-069-074): 6 ACs
  - Filters: Date Range (AC-075-080): 6 ACs
  - Filters: Role (AC-081-085): 5 ACs
  - Filters: Combined (AC-086-090): 5 ACs
  - Index Maintenance: Incremental (AC-091-095): 5 ACs
  - Index Maintenance: Optimization (AC-096-100): 5 ACs
  - Index Maintenance: Rebuild (AC-101-105): 5 ACs
  - Index Maintenance: Status (AC-106-110): 5 ACs
  - CLI Search Command (AC-111-120): 10 ACs
  - Error Handling (AC-121-127): 7 ACs
  - Performance SLAs (AC-128-132): 5 ACs

### Error Codes Required

**6 error codes** (ACODE-SRCH-001 through ACODE-SRCH-006):
1. ACODE-SRCH-001: Invalid query syntax
2. ACODE-SRCH-002: Query timeout (>5 seconds)
3. ACODE-SRCH-003: Invalid date filter
4. ACODE-SRCH-004: Invalid role filter
5. ACODE-SRCH-005: Index corruption detected
6. ACODE-SRCH-006: Index not initialized

### Key Technologies & Dependencies

- **SQLite FTS5:** Full-text search virtual table with Porter stemming
- **PostgreSQL tsvector:** Alternative backend with GIN index
- **BM25 Algorithm:** Industry-standard relevance ranking
- **Event System:** IEventPublisher for incremental indexing (Task 023)
- **Data Model:** Chat, Run, Message entities (Task 049a)
- **CLI:** Integration with existing command framework

---

## SECTION 2: CURRENT IMPLEMENTATION STATE

**Status: ❌ ZERO IMPLEMENTATION**

**Verification:**
- ✅ No search/indexing infrastructure exists
- ✅ No FTS5 virtual table created
- ✅ No search query parser implemented
- ✅ No ranking/snippet generation

**Available Dependencies:**
- ✅ Message, Chat, Run entities (Task 049a)
- ✅ Event system (Task 023)
- ✅ SQLite integration (Task 049a)

---

## SECTION 3: PRODUCTION FILES NEEDED (~25 files)

**DOMAIN LAYER (2 files):**
1. `src/AgenticCoder.Domain/Search/SearchQuery.cs` - Parsed query with terms, filters, operators
2. `src/AgenticCoder.Domain/Search/SearchResult.cs` - Single result with snippet, score, metadata

**APPLICATION LAYER (5 files):**
3. `src/AgenticCoder.Application/Search/ISearchIndexer.cs` - Index operations interface
4. `src/AgenticCoder.Application/Search/ISearchEngine.cs` - Query execution interface
5. `src/AgenticCoder.Application/Search/IQueryParser.cs` - Query parsing interface
6. `src/AgenticCoder.Application/Search/ISnippetGenerator.cs` - Snippet generation interface
7. `src/AgenticCoder.Application/Search/SearchOptions.cs` - Configuration options

**INFRASTRUCTURE LAYER - Indexing (6 files):**
8. `src/AgenticCoder.Infrastructure/Search/Indexing/SqliteFtsIndexer.cs` - FTS5 indexing
9. `src/AgenticCoder.Infrastructure/Search/Indexing/PostgresSearchIndexer.cs` - PostgreSQL tsvector indexing
10. `src/AgenticCoder.Infrastructure/Search/Indexing/IndexMaintenanceService.cs` - Incremental updates
11. `src/AgenticCoder.Infrastructure/Search/Indexing/IndexOptimizer.cs` - Index optimization
12. `src/AgenticCoder.Infrastructure/Search/Indexing/IndexValidator.cs` - Integrity checking
13. `src/AgenticCoder.Infrastructure/Search/Indexing/SearchIndexEventHandler.cs` - Event subscriptions

**INFRASTRUCTURE LAYER - Querying (5 files):**
14. `src/AgenticCoder.Infrastructure/Search/Querying/SqliteFtsSearchEngine.cs` - FTS5 queries
15. `src/AgenticCoder.Infrastructure/Search/Querying/PostgresSearchEngine.cs` - PostgreSQL queries
16. `src/AgenticCoder.Infrastructure/Search/Querying/QueryParser.cs` - Query parsing/validation
17. `src/AgenticCoder.Infrastructure/Search/Querying/BM25Ranker.cs` - Relevance scoring
18. `src/AgenticCoder.Infrastructure/Search/Querying/RecencyBooster.cs` - Recency adjustment

**INFRASTRUCTURE LAYER - Results (3 files):**
19. `src/AgenticCoder.Infrastructure/Search/Results/SnippetGenerator.cs` - Snippet generation
20. `src/AgenticCoder.Infrastructure/Search/Results/HighlightFormatter.cs` - Markup highlighting
21. `src/AgenticCoder.Infrastructure/Search/Results/ResultFormatter.cs` - Output formatting (table, JSON)

**DATABASE MIGRATIONS (2 files):**
22. `src/AgenticCoder.Infrastructure/Persistence/Migrations/Migration_YYYYMM_CreateFts5Index.cs` - SQLite FTS5 setup
23. `src/AgenticCoder.Infrastructure/Persistence/Migrations/Migration_YYYYMM_CreatePostgresSearch.cs` - PostgreSQL setup

**CLI LAYER (2 files):**
24. `src/AgenticCoder.Cli/Commands/SearchCommand.cs` - `acode search` CLI command
25. `src/AgenticCoder.Cli/Commands/SearchIndexCommand.cs` - `acode search index` sub-commands

---

## SECTION 4: TEST FILES NEEDED (~10 files, 50+ tests)

**Unit Tests:**
- `Tests/Unit/Search/QueryParserTests.cs` - Query parsing validation
- `Tests/Unit/Search/BM25RankerTests.cs` - Relevance scoring
- `Tests/Unit/Search/RecencyBoosterTests.cs` - Recency adjustment
- `Tests/Unit/Search/SnippetGeneratorTests.cs` - Snippet extraction

**Integration Tests:**
- `Tests/Integration/Search/SqliteFtsIndexerTests.cs` - Indexing operations
- `Tests/Integration/Search/SqliteFtsSearchEngineTests.cs` - Query execution
- `Tests/Integration/Search/IndexMaintenanceTests.cs` - Incremental updates
- `Tests/Integration/Search/SearchIntegrationTests.cs` - Full workflows

**E2E Tests:**
- `Tests/E2E/Search/SearchCommandTests.cs` - CLI search command
- `Tests/E2E/Search/IndexManagementTests.cs` - Index operations via CLI

**Performance Tests:**
- `Tests/Performance/Search/SearchBenchmarks.cs` - Performance validation

---

## SECTION 5: EFFORT BREAKDOWN

| Component | ACs | Files | Hours | Status |
|-----------|-----|-------|-------|--------|
| Domain Models | 2 | 2 | 1 | 🔴 Missing |
| Application Interfaces | 5 | 5 | 1.5 | 🔴 Missing |
| Indexing Infrastructure | 24 | 6 | 12 | 🔴 Missing |
| Search Engine | 30 | 5 | 14 | 🔴 Missing |
| Ranking & Snippets | 18 | 5 | 10 | 🔴 Missing |
| Filters & Options | 26 | 3 | 8 | 🔴 Missing |
| Index Maintenance | 20 | 4 | 8 | 🔴 Missing |
| Database Migrations | 4 | 2 | 2 | 🔴 Missing |
| CLI Commands | 10 | 2 | 4 | 🔴 Missing |
| Testing (Unit/Integ/E2E) | - | 10 | 25 | 🔴 Missing |
| Documentation | 5 | - | 3 | 🔴 Missing |
| **TOTAL** | **132** | **25** | **88-95** | 🔴 **0%** |

**Estimated Effort:** 90-100 hours (12-14 days at full capacity)

---

## SECTION 6: SEMANTIC COMPLETENESS

```
Task-049d Semantic Completeness = (ACs fully implemented / Total ACs) × 100

ACs Fully Implemented: 0
Total ACs: 132

Semantic Completeness: (0 / 132) × 100 = 0%
```

---

## CRITICAL NOTES

**Blocking Dependencies:** None (all dependencies available)

**Complex Components:**
- BM25 ranking algorithm (non-trivial math)
- Query parser (complex syntax with operators)
- FTS5 virtual table setup (requires SQLite 3.9.0+)
- Incremental indexing coordination (event-driven)

**Performance Targets:**
- Single message index: <10ms (AC-019)
- Batch 100 messages: <1 second (AC-020)
- Full rebuild 10k messages: <60 seconds (AC-021)
- Search 10k messages: <500ms (AC-128)
- Search 100k messages: <1.5 seconds (AC-129)

---

**Status:** 🔴 COMPLETELY UNIMPLEMENTED

**Recommendation:** Create detailed completion checklist with 6 implementation phases:
1. Domain models & interfaces
2. Indexing infrastructure (FTS5 + Postgres)
3. Search engine & query parser
4. Ranking, relevance, recency
5. Snippets & highlighting
6. CLI commands & maintenance tools

---
