# Task 049d Gap Analysis - Indexing + Fast Search

## INSTRUCTIONS FOR RESUMPTION AFTER CONTEXT COMPACTION

**Current Status**: Phases 0-5 complete (61% done), Phase 6 implementation complete but tests need API signature fixes.

**Last Updated**: 2026-01-10 (Session 2 - in progress)

**Task Summary**:
- **Task**: 049d - Indexing + Fast Search Over Chats/Runs/Messages
- **Spec File**: docs/tasks/refined-tasks/Epic 02/task-049d-indexing-fast-search.md
- **Total Lines**: 3596
- **Acceptance Criteria**: 132 (AC-001 through AC-132)
- **Dependencies**: Task 049a (Data Model), Task 049b (CLI), Task 030 (Search - NOT FOUND)

**Key Sections**:
- Acceptance Criteria: line 1195
- Testing Requirements: line 1577
- Implementation Prompt: line 2858

**What to do next**:
1. Create detailed implementation plan with phases
2. Create feature branch
3. Implement components following TDD
4. Verify against all 132 acceptance criteria
5. Create PR when complete

---

## Specification Requirements Summary

**From Acceptance Criteria** (lines 1195-1577, 382 lines):
- Total acceptance criteria: 132 (AC-001 through AC-132)
- Indexing (AC-001 through AC-024): 24 criteria
- Basic queries (AC-025 through AC-031): 7 criteria
- Boolean operators (AC-032 through AC-037): 6 criteria
- Field-specific queries (AC-038 through AC-043): 6 criteria
- Ranking relevance (AC-044 through AC-050): 7 criteria
- Ranking recency (AC-051 through AC-056): 6 criteria
- Snippets generation (AC-057 through AC-062): 6 criteria
- Snippets highlighting (AC-063 through AC-068): 6 criteria
- Filters chat (AC-069 through AC-074): 6 criteria
- Filters date (AC-075 through AC-080): 6 criteria
- Filters role (AC-081 through AC-085): 5 criteria
- Filters combined (AC-086 through AC-090): 5 criteria
- Index maintenance incremental (AC-091 through AC-095): 5 criteria
- Index maintenance optimization (AC-096 through AC-100): 5 criteria
- Index maintenance rebuild (AC-101 through AC-105): 5 criteria
- Index maintenance status (AC-106 through AC-110): 5 criteria
- CLI search command (AC-111 through AC-120): 10 criteria
- Error handling (AC-121 through AC-127): 7 criteria
- Performance SLAs (AC-128 through AC-132): 5 criteria

**From Implementation Prompt** (lines 2858-3596, 738 lines):

### Production Files Expected:

1. **src/Acode.Domain/Search/SearchQuery.cs** (50 lines)
   - Properties: QueryText, ChatId, Since, Until, RoleFilter, PageSize, PageNumber, SortBy
   - Methods: Validate() → Result<SearchQuery, Error>
   - Enum: SortOrder (Relevance, DateDescending, DateAscending)

2. **src/Acode.Domain/Search/SearchResult.cs** (40 lines)
   - SearchResult record: MessageId, ChatId, ChatTitle, Role, CreatedAt, Snippet, Score, Matches
   - MatchLocation record: Field, StartOffset, Length
   - SearchResults record: Results, TotalCount, PageNumber, PageSize, QueryTimeMs, NextCursor
   - Computed properties: TotalPages, HasNextPage, HasPreviousPage

3. **src/Acode.Application/Interfaces/ISearchService.cs** (25 lines)
   - SearchAsync(SearchQuery, CancellationToken) → Result<SearchResults, Error>
   - IndexMessageAsync(Message, CancellationToken) → Result<Unit, Error>
   - UpdateMessageIndexAsync(Message, CancellationToken) → Result<Unit, Error>
   - RemoveFromIndexAsync(Guid messageId, CancellationToken) → Result<Unit, Error>
   - GetIndexStatusAsync(CancellationToken) → Result<IndexStatus, Error>
   - RebuildIndexAsync(IProgress<int>?, CancellationToken) → Result<Unit, Error>

4. **src/Acode.Infrastructure/Search/SqliteFtsSearchService.cs** (150 lines)
   - Implements ISearchService
   - Uses SQLite FTS5 virtual table
   - Dependencies: SqliteConnection, ILogger, SafeQueryParser, BM25Ranker, SnippetGenerator
   - Methods: All ISearchService methods implemented

5. **src/Acode.Infrastructure/Search/BM25Ranker.cs** (80 lines)
   - ApplyRecencyBoost(double baseScore, DateTime messageDate) → double
   - CalculateBM25(int termFrequency, ...) → double
   - ApplyFieldBoost(double score, string field) → double
   - Constants: K1=1.2, B=0.75, RecencyBoost values

6. **src/Acode.Infrastructure/Search/SnippetGenerator.cs** (70 lines)
   - Generate(string content, IEnumerable<int> matchOffsets) → string
   - HighlightTerms(string snippet, IEnumerable<string> terms) → string
   - TruncateAtWordBoundary(string text, int maxLength) → string
   - Constants: DefaultMaxLength=150, HighlightOpen/Close, Ellipsis

7. **src/Acode.Cli/Commands/SearchCommand.cs** (90 lines)
   - Inherits: AsyncCommand<SearchCommand.Settings>
   - Settings: QueryText, ChatId, Since, Until, RoleFilter, PageSize, PageNumber, JsonOutput
   - ExecuteAsync: Calls ISearchService, formats output (table or JSON)

8. **migrations/006_add_search_index.sql** (NEW - required but not in spec)
   - CREATE VIRTUAL TABLE conversation_search USING fts5(...)
   - Tokenizer: porter unicode61
   - External content table: conv_messages

**Total Production Files**: 8 (7 C# + 1 SQL migration)

**From Testing Requirements** (lines 1577-2858, 1281 lines):

### Test Files Expected:

1. **tests/Acode.Domain.Tests/Search/SearchQueryTests.cs** (~80 lines, 10 tests)
   - Validate_WithEmptyQueryText_ReturnsFailure
   - Validate_WithQueryTextTooLong_ReturnsFailure
   - Validate_WithInvalidPageSize_ReturnsFailure
   - Validate_WithSinceAfterUntil_ReturnsFailure
   - Validate_WithValidQuery_ReturnsSuccess
   - [+5 more tests]

2. **tests/Acode.Domain.Tests/Search/SearchResultTests.cs** (~60 lines, 8 tests)
   - TotalPages_Calculated_Correctly
   - HasNextPage_WhenNotLastPage_ReturnsTrue
   - HasPreviousPage_WhenNotFirstPage_ReturnsTrue
   - [+5 more tests]

3. **tests/Acode.Infrastructure.Tests/Search/SqliteFtsSearchServiceTests.cs** (~200 lines, 20 tests)
   - SearchAsync_WithSingleTerm_ReturnsMatchingMessages
   - SearchAsync_WithPhrase_ReturnsExactMatches
   - SearchAsync_WithChatFilter_ReturnsOnlyFilteredResults
   - SearchAsync_WithDateFilter_ReturnsOnlyFilteredResults
   - SearchAsync_WithRoleFilter_ReturnsOnlyFilteredResults
   - IndexMessageAsync_AddsMessageToIndex
   - UpdateMessageIndexAsync_UpdatesExistingEntry
   - RemoveFromIndexAsync_RemovesMessageFromIndex
   - GetIndexStatusAsync_ReturnsCorrectCounts
   - RebuildIndexAsync_ReindexesAllMessages
   - [+10 more tests]

4. **tests/Acode.Infrastructure.Tests/Search/BM25RankerTests.cs** (~100 lines, 12 tests)
   - ApplyRecencyBoost_RecentMessage_MultipliesBy1_5
   - ApplyRecencyBoost_NormalMessage_MultipliesBy1_0
   - ApplyRecencyBoost_OldMessage_MultipliesBy0_8
   - CalculateBM25_WithHighTermFrequency_ReturnsHigherScore
   - ApplyFieldBoost_TitleField_MultipliesBy2
   - [+7 more tests]

5. **tests/Acode.Infrastructure.Tests/Search/SnippetGeneratorTests.cs** (~90 lines, 10 tests)
   - Generate_WithMatchOffsets_CentersAroundFirstMatch
   - Generate_WithNoMatches_TruncatesAtMaxLength
   - Generate_PreservesWordBoundaries
   - HighlightTerms_WrapsTermsInMarkTags
   - HighlightTerms_WithMultipleTerms_HighlightsAll
   - [+5 more tests]

6. **tests/Acode.Cli.Tests/Commands/SearchCommandTests.cs** (~120 lines, 12 tests)
   - ExecuteAsync_WithValidQuery_ReturnsResults
   - ExecuteAsync_WithJsonFlag_OutputsJson
   - ExecuteAsync_WithChatFilter_FiltersResults
   - ExecuteAsync_WithPagination_ShowsCorrectPage
   - ExecuteAsync_WithInvalidQuery_ReturnsError
   - [+7 more tests]

**Total Test Files**: 6
**Total Test Methods**: ~72 tests

---

## Current Implementation State (VERIFIED)

### Production Files

#### ✅ COMPLETE: Phases 0-6 Implementation

**Verification Commands Run** (2026-01-10):
```bash
$ find src/Acode.Domain/Search -name "*.cs"
src/Acode.Domain/Search/SearchQuery.cs
src/Acode.Domain/Search/SearchResult.cs
src/Acode.Domain/Search/SearchResults.cs
src/Acode.Domain/Search/MatchLocation.cs
src/Acode.Domain/Search/SortOrder.cs

$ find src/Acode.Application/Interfaces -name "*Search*"
src/Acode.Application/Interfaces/ISearchService.cs

$ find src/Acode.Infrastructure/Search -name "*.cs"
src/Acode.Infrastructure/Search/BM25Ranker.cs
src/Acode.Infrastructure/Search/SnippetGenerator.cs
src/Acode.Infrastructure/Search/SafeQueryParser.cs
src/Acode.Infrastructure/Search/SqliteFtsSearchService.cs

$ find migrations -name "006*"
migrations/006_add_search_index.sql
migrations/006_add_search_index_down.sql
```

**Status**: Phases 0-6 implementation complete (8 commits: 4ad1156 through 546c5ba).

**Files Complete** (11 of 15 exist):
- [x] src/Acode.Domain/Search/SearchQuery.cs (84 lines, commit 1482c34)
- [x] src/Acode.Domain/Search/SearchResult.cs (50 lines, commit 1482c34)
- [x] src/Acode.Domain/Search/SearchResults.cs (53 lines, commit 1482c34)
- [x] src/Acode.Domain/Search/MatchLocation.cs (22 lines, commit 1482c34)
- [x] src/Acode.Domain/Search/SortOrder.cs (22 lines, commit 1482c34)
- [x] src/Acode.Application/Interfaces/ISearchService.cs (88 lines, commit f38b85d)
- [x] src/Acode.Infrastructure/Search/BM25Ranker.cs (143 lines, commit ac0fb69)
- [x] src/Acode.Infrastructure/Search/SnippetGenerator.cs (162 lines, commit e4ee54f)
- [x] src/Acode.Infrastructure/Search/SafeQueryParser.cs (120 lines, commit 511fb32)
- [x] src/Acode.Infrastructure/Search/SqliteFtsSearchService.cs (283 lines, commit 546c5ba)
- [x] migrations/006_add_search_index.sql (108 lines, commit 4ad1156)
- [x] migrations/006_add_search_index_down.sql (13 lines, commit 4ad1156)
- [ ] src/Acode.Cli/Commands/SearchCommand.cs (Phase 7 - pending)
- [ ] Integration E2E tests (Phase 8 - pending)
- [ ] Documentation (Phase 9 - pending)

### Test Files

#### ✅ PARTIALLY COMPLETE: Phases 1-6 Tests (47 of 72 passing, 19 need fixes)

**Verification Commands Run** (2026-01-10):
```bash
$ find tests -name "*Search*Tests.cs"
tests/Acode.Domain.Tests/Search/SearchQueryTests.cs
tests/Acode.Domain.Tests/Search/SearchResultTests.cs
tests/Acode.Infrastructure.Tests/Search/BM25RankerTests.cs
tests/Acode.Infrastructure.Tests/Search/SnippetGeneratorTests.cs
tests/Acode.Infrastructure.Tests/Search/SafeQueryParserTests.cs
tests/Acode.Infrastructure.Tests/Search/SqliteFtsSearchServiceTests.cs
```

**Status**: 5 test files complete with 47 tests passing, 1 test file needs API signature fixes.

**Files Status** (5 of 6 exist):
- [x] tests/Acode.Domain.Tests/Search/SearchQueryTests.cs (11 tests ✅, commit 1482c34)
- [x] tests/Acode.Domain.Tests/Search/SearchResultTests.cs (8 tests ✅, commit 1482c34)
- [x] tests/Acode.Infrastructure.Tests/Search/BM25RankerTests.cs (12 tests ✅, commit ac0fb69)
- [x] tests/Acode.Infrastructure.Tests/Search/SnippetGeneratorTests.cs (10 tests ✅, commit e4ee54f)
- [x] tests/Acode.Infrastructure.Tests/Search/SafeQueryParserTests.cs (8 tests ✅, commit 511fb32)
- [x] tests/Acode.Infrastructure.Tests/Search/SqliteFtsSearchServiceTests.cs (20 tests ⚠️ API fixes needed, commit unstaged)
  - **Issue**: Repository constructor signatures (string vs SqliteConnection)
  - **Issue**: Message.Create parameter order mismatch
  - **Issue**: Run.Create missing modelId/maxTokens parameters
- [ ] tests/Acode.Cli.Tests/Commands/SearchCommandTests.cs (Phase 7 - pending)

### Dependencies Available

**Verified Dependencies**:
- ✅ **Task 049a (Data Model)**: Message, Chat, Run entities exist in src/Acode.Domain/Conversation/
- ✅ **Task 049b (CLI)**: CLI framework exists in src/Acode.Cli/
- ✅ **Database schema**: conv_chats, conv_messages, conv_runs tables exist (migration 002)
- ❌ **Task 030 (Search Infrastructure)**: ISearchService interface does NOT exist (need to create)

**Note**: Task 030 dependency is listed in spec but ISearchService doesn't exist. We'll create it as part of this task.

---

## Gap Summary (Updated 2026-01-10)

### Files Requiring Work

| Category | Complete | Needs Fixes | Missing | Total |
|----------|----------|-------------|---------|-------|
| Production | 11 | 1 (test file) | 3 | 15 |
| Tests | 5 | 1 (API fixes) | 1 | 7 |
| SQL Migrations | 2 | 0 | 0 | 2 |
| **TOTAL** | **18** | **2** | **4** | **24** |

**Completion Percentage**: 75% (18 complete / 24 total, 2 need fixes, 4 pending)

### Test Coverage Gap

| Component | Tests Expected | Tests Passing | Gap |
|-----------|---------------|---------------|-----|
| SearchQuery | 11 | 11 | ✅ Complete |
| SearchResult | 8 | 8 | ✅ Complete |
| BM25Ranker | 12 | 12 | ✅ Complete |
| SnippetGenerator | 10 | 10 | ✅ Complete |
| SafeQueryParser | 8 | 8 | ✅ Complete |
| SqliteFtsSearchService | 20 | 0 | ⚠️ 20 need API fixes |
| SearchCommand (CLI) | 12 | 0 | ❌ 12 missing (Phase 7) |
| Integration E2E | 10 | 0 | ❌ 10 missing (Phase 8) |
| **TOTAL** | **91** | **49** | **⚠️ 20 fixable, ❌ 22 pending** |

### Next Actions

**Immediate (Phase 6 completion)**:
1. Fix SqliteFtsSearchServiceTests API signatures (repository constructors, Message.Create, Run.Create)
2. Run tests → target 20/20 passing
3. Commit Phase 6 tests

**Upcoming (Phases 7-9)**:
4. Phase 7: SearchCommand CLI + 12 tests
5. Phase 8: Integration E2E + 10 tests
6. Phase 9: Audit + Documentation + PR

**Test Completion Percentage**: 0% (0 passing / 72 expected)

---

## Strategic Implementation Plan

This task will be implemented in 9 phases following strict TDD (RED-GREEN-REFACTOR).

### Phase 0: Database Migration (FOUNDATION)

**Objective**: Create FTS5 virtual table for search indexing.

**Files to Create**:
1. migrations/006_add_search_index.sql (~40 lines)
2. migrations/006_add_search_index_down.sql (~10 lines)

**Migration Content**:
```sql
CREATE VIRTUAL TABLE IF NOT EXISTS conversation_search USING fts5(
    message_id UNINDEXED,
    chat_id UNINDEXED,
    run_id UNINDEXED,
    created_at UNINDEXED,
    role UNINDEXED,
    content,
    chat_title,
    tags,
    tokenize='porter unicode61',
    content='conv_messages',
    content_rowid='rowid'
);

-- Triggers for automatic indexing
CREATE TRIGGER IF NOT EXISTS conv_messages_after_insert AFTER INSERT ON conv_messages
BEGIN
    INSERT INTO conversation_search (message_id, chat_id, content, ...)
    SELECT NEW.id, (SELECT chat_id FROM conv_runs WHERE id = NEW.run_id), NEW.content, ...;
END;

CREATE TRIGGER IF NOT EXISTS conv_messages_after_update AFTER UPDATE ON conv_messages
BEGIN
    DELETE FROM conversation_search WHERE message_id = OLD.id;
    INSERT INTO conversation_search (message_id, chat_id, content, ...) VALUES (...);
END;

CREATE TRIGGER IF NOT EXISTS conv_messages_after_delete AFTER DELETE ON conv_messages
BEGIN
    DELETE FROM conversation_search WHERE message_id = OLD.id;
END;
```

**Acceptance**:
- [ ] Migration file created
- [ ] Migration runs without errors
- [ ] conversation_search table exists
- [ ] Triggers created for automatic indexing

**Commit**: `feat(task-049d): Add FTS5 search index migration`

---

### Phase 1: Domain Value Objects (TDD)

**Objective**: Create SearchQuery and SearchResult value objects with validation.

**TDD Process**:

1. **RED**: Create test files first
   - tests/Acode.Domain.Tests/Search/SearchQueryTests.cs (10 tests)
   - tests/Acode.Domain.Tests/Search/SearchResultTests.cs (8 tests)
   - Run tests → Compilation fails (types don't exist)

2. **GREEN**: Create production files
   - src/Acode.Domain/Search/SearchQuery.cs (50 lines)
   - src/Acode.Domain/Search/SearchResult.cs (40 lines)
   - Run tests → All 18 tests passing

3. **REFACTOR**: Fix StyleCop violations, add XML docs

**Acceptance**:
- [ ] SearchQuery.cs created with Validate() method
- [ ] SearchResult.cs created with computed properties
- [ ] SearchQueryTests.cs created with 10 tests
- [ ] SearchResultTests.cs created with 8 tests
- [ ] All 18 tests passing
- [ ] Build GREEN (0 errors, 0 warnings)

**Commit**: `feat(task-049d): Add SearchQuery and SearchResult value objects`

---

### Phase 2: Application Interfaces (TDD)

**Objective**: Create ISearchService interface defining search contract.

**TDD Process**:

1. **RED**: Create interface test (indirect via mock)
2. **GREEN**: Create src/Acode.Application/Interfaces/ISearchService.cs (25 lines)
3. **REFACTOR**: Add XML docs

**Acceptance**:
- [ ] ISearchService.cs created with 6 methods
- [ ] Interface compiles cleanly
- [ ] XML docs complete

**Commit**: `feat(task-049d): Add ISearchService interface`

---

### Phase 3: Infrastructure - BM25Ranker (TDD)

**Objective**: Implement BM25 ranking algorithm with recency boost.

**TDD Process**:

1. **RED**: Create tests/Acode.Infrastructure.Tests/Search/BM25RankerTests.cs (12 tests)
2. **GREEN**: Create src/Acode.Infrastructure/Search/BM25Ranker.cs (80 lines)
3. **REFACTOR**: Fix violations

**Acceptance**:
- [ ] BM25Ranker.cs created with 3 methods
- [ ] BM25RankerTests.cs created with 12 tests
- [ ] All 12 tests passing
- [ ] Recency boost verified: <7 days (1.5x), 7-30 days (1.0x), >30 days (0.8x)

**Commit**: `feat(task-049d): Add BM25Ranker with recency boost`

---

### Phase 4: Infrastructure - SnippetGenerator (TDD)

**Objective**: Implement snippet generation with term highlighting.

**TDD Process**:

1. **RED**: Create tests/Acode.Infrastructure.Tests/Search/SnippetGeneratorTests.cs (10 tests)
2. **GREEN**: Create src/Acode.Infrastructure/Search/SnippetGenerator.cs (70 lines)
3. **REFACTOR**: Fix violations

**Acceptance**:
- [ ] SnippetGenerator.cs created with 3 methods
- [ ] SnippetGeneratorTests.cs created with 10 tests
- [ ] All 10 tests passing
- [ ] Highlights wrap terms in <mark> tags
- [ ] Truncation preserves word boundaries

**Commit**: `feat(task-049d): Add SnippetGenerator with highlighting`

---

### Phase 5: Infrastructure - SafeQueryParser (NEW - not in spec but needed)

**Objective**: Parse and sanitize user queries for FTS5.

**Note**: Implementation Prompt shows SqliteFtsSearchService depends on SafeQueryParser but doesn't provide its implementation. We need to create it.

**TDD Process**:

1. **RED**: Create tests/Acode.Infrastructure.Tests/Search/SafeQueryParserTests.cs (8 tests)
2. **GREEN**: Create src/Acode.Infrastructure/Search/SafeQueryParser.cs (60 lines)
3. **REFACTOR**: Fix violations

**Acceptance**:
- [ ] SafeQueryParser.cs created
- [ ] Escapes special FTS5 characters
- [ ] Validates Boolean operator syntax
- [ ] SafeQueryParserTests.cs created with 8 tests
- [ ] All 8 tests passing

**Commit**: `feat(task-049d): Add SafeQueryParser for FTS5 query sanitization`

---

### Phase 6: Infrastructure - SqliteFtsSearchService (TDD)

**Objective**: Implement full-text search service using SQLite FTS5.

**TDD Process**:

1. **RED**: Create tests/Acode.Infrastructure.Tests/Search/SqliteFtsSearchServiceTests.cs (20 tests)
2. **GREEN**: Create src/Acode.Infrastructure/Search/SqliteFtsSearchService.cs (150 lines)
3. **REFACTOR**: Fix violations

**Acceptance**:
- [ ] SqliteFtsSearchService.cs created implementing ISearchService
- [ ] All 6 ISearchService methods implemented
- [ ] SearchAsync supports filters (chat, date, role)
- [ ] IndexMessageAsync inserts into FTS5 table
- [ ] UpdateMessageIndexAsync performs DELETE+INSERT
- [ ] RemoveFromIndexAsync deletes from FTS5 table
- [ ] GetIndexStatusAsync returns index health
- [ ] RebuildIndexAsync reindexes all messages
- [ ] SqliteFtsSearchServiceTests.cs created with 20 tests
- [ ] All 20 tests passing

**Commit**: `feat(task-049d): Add SqliteFtsSearchService with FTS5 integration`

---

### Phase 7: CLI - SearchCommand (TDD)

**Objective**: Implement CLI search command with filters and output formatting.

**TDD Process**:

1. **RED**: Create tests/Acode.Cli.Tests/Commands/SearchCommandTests.cs (12 tests)
2. **GREEN**: Create src/Acode.Cli/Commands/SearchCommand.cs (90 lines)
3. **REFACTOR**: Fix violations

**Acceptance**:
- [ ] SearchCommand.cs created with Settings class
- [ ] Supports flags: --chat, --since, --until, --role, --page, --page-size, --json
- [ ] Table output formatted with columns: Chat, Date, Role, Snippet, Score
- [ ] JSON output includes full SearchResults object
- [ ] SearchCommandTests.cs created with 12 tests
- [ ] All 12 tests passing

**Commit**: `feat(task-049d): Add search CLI command with filters`

---

### Phase 8: Integration Testing

**Objective**: Test full search flow end-to-end.

**TDD Process**:

1. Create tests/Acode.Integration.Tests/Search/SearchE2ETests.cs (10 tests)
2. Test: Insert messages → Index automatically → Search → Verify results
3. Test: Performance (search 10k messages in <500ms)
4. Test: Concurrency (10 simultaneous searches)

**Acceptance**:
- [ ] SearchE2ETests.cs created with 10 tests
- [ ] All 10 tests passing
- [ ] Performance SLA met (AC-128, AC-129)
- [ ] Concurrency test passes (AC-130)

**Commit**: `test(task-049d): Add E2E integration tests for search`

---

### Phase 9: Documentation and Audit

**Objective**: Complete documentation and verify against all 132 acceptance criteria.

**Files to Update**:
1. Update this gap analysis document with completion status
2. Create docs/audits/task-049d-audit-report.md
3. Update PROGRESS_NOTES.md

**Audit Process** (per docs/AUDIT-GUIDELINES.md):
1. Verify all 15 files created (8 production + 6 tests + 1 migration)
2. Verify no NotImplementedException in any file
3. Verify all 72+ tests passing (18 domain + 12 BM25 + 10 snippet + 8 parser + 20 service + 12 CLI + 10 E2E)
4. Verify build GREEN (0 errors, 0 warnings)
5. Cross-check all 132 acceptance criteria with evidence
6. Create audit report with file paths and line numbers

**Acceptance**:
- [ ] All 15 files created and verified
- [ ] All 72+ tests passing
- [ ] Build GREEN
- [ ] All 132 acceptance criteria verified
- [ ] Audit report created
- [ ] PROGRESS_NOTES.md updated

**Commit**: `docs(task-049d): Complete audit - task ready for PR`

---

## Verification Checklist (Before Marking Task Complete)

### File Existence Check
- [ ] All 8 production files from spec exist
- [ ] All 6 test files from spec exist
- [ ] Migration file exists (006_add_search_index.sql)
- [ ] SafeQueryParser files exist (not in spec but required)

### Implementation Verification Check
For each production file:
- [ ] No NotImplementedException
- [ ] No TODO/FIXME comments
- [ ] All methods from spec present (grep verification)
- [ ] Method signatures match spec
- [ ] Method bodies contain logic (not just `return null;`)

### Test Verification Check
For each test file:
- [ ] Test count matches spec (±2)
- [ ] No NotImplementedException
- [ ] Tests contain assertions (Should/Assert/Verify)
- [ ] All tests passing when run

### Build & Test Execution Check
- [ ] `dotnet build` → 0 errors, 0 warnings
- [ ] `dotnet test` → all tests passing
- [ ] Test count: 72+ passing (matches spec expected count)

### Functional Verification Check
- [ ] Create test database with sample messages
- [ ] Run `acode search "test query"` → returns results
- [ ] Verify snippets show highlighted terms
- [ ] Verify filters work (--chat, --since, --role)
- [ ] Verify performance (search 10k messages in <500ms)

### Acceptance Criteria Cross-Check
- [ ] All 132 acceptance criteria verified with evidence
- [ ] Evidence documented in audit report with file:line references

### Completeness Cross-Check
- [ ] Compare file count: Built 15 / Spec requires 15
- [ ] Compare test count: Passing 72+ / Spec expects 72
- [ ] Compare AC coverage: Verified 132 / Spec has 132
- [ ] Review Implementation Prompt: All code examples implemented

---

## Execution Checklist

- [ ] Phase 0 complete (Migration)
- [ ] Phase 1 complete (Domain value objects - 18 tests)
- [ ] Phase 2 complete (Application interfaces)
- [ ] Phase 3 complete (BM25Ranker - 12 tests)
- [ ] Phase 4 complete (SnippetGenerator - 10 tests)
- [ ] Phase 5 complete (SafeQueryParser - 8 tests)
- [ ] Phase 6 complete (SqliteFtsSearchService - 20 tests)
- [ ] Phase 7 complete (SearchCommand - 12 tests)
- [ ] Phase 8 complete (Integration tests - 10 tests)
- [ ] Phase 9 complete (Documentation and audit)
- [ ] All verification checks passed
- [ ] Audit report created
- [ ] PR created

**Task Status**: NOT STARTED (Gap analysis complete, ready to create implementation plan)

---

## Notes

**Gap Analysis Completed**: 2026-01-10
**Estimated Effort**: 8-12 hours (9 phases, 15 files, 72+ tests)
**Next Step**: Create detailed implementation plan in docs/implementation-plans/task-049d-plan.md
