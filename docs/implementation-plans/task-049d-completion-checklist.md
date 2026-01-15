# Task-049d Completion Checklist: Indexing + Fast Search Over Chats/Runs/Messages

**Status:** 🔴 0% COMPLETE (Clean Slate - No Code Exists)

**Objective:** Implement 132 acceptance criteria across 25 files using Test-Driven Development

**Methodology:** RED → GREEN → REFACTOR cycle, with per-gap commits

**Effort Estimate:** 90-100 hours total (6 phases, 2-3 weeks at full capacity)

---

## INSTRUCTIONS FOR FRESH AGENT

This checklist guides full-text search implementation. Follow these steps:

1. **Read this entire document** to understand the full scope
2. **For each gap in order:**
   - Read the spec line numbers provided
   - Write test code FIRST (RED phase)
   - Run test, verify it fails with meaningful error
   - Write minimum production code (GREEN phase)
   - Refactor while keeping tests green
   - Mark gap as [✅] complete
   - Commit with message `feat(task-049d/phase-N): [Gap title]`
3. **Never skip sections** - Each section depends on previous completions
4. **Commit after each gap** - Don't batch multiple gaps per commit
5. **Performance is critical** - Verify latency targets specified for each component
6. **Audit before PR** - Follow docs/AUDIT-GUIDELINES.md checklist

**Success Criteria:** All 132 ACs passing tests, performance targets met, 85%+ code coverage, no warnings/errors on build

---

## WHAT EXISTS

**Already Available (from other tasks):**
- ✅ Domain value types: `ChatId`, `MessageId` (Task 049a)
- ✅ Event system: `IEventPublisher` (Task 023)
- ✅ Message, Chat, Run entities with repositories (Task 049a)
- ✅ SQLite integration (Dapper/ADO.NET) (Task 049a)

**Completely New (Must Implement):**
- ❌ FTS5 virtual table for full-text search
- ❌ Search query parsing and validation
- ❌ BM25 ranking algorithm
- ❌ Snippet generation with highlighting
- ❌ Filtering by chat, date, role
- ❌ CLI search commands
- ❌ Index optimization and maintenance
- ❌ All test files (unit, integration, E2E, performance)

---

## PHASE 0: DOMAIN MODELS & APPLICATION INTERFACES (3-4 hours, 0 tests)

**Objective:** Create search domain models and service contracts

### Gap 1: SearchQuery Domain Model [⬜]

**Spec Reference:** Lines 38-71 (Query Processing diagram and description)

**What to Implement:**
- File: `src/AgenticCoder.Domain/Search/SearchQuery.cs`
- Properties (immutable):
  - `string QueryText` - Original user query
  - `IReadOnlyList<string> Terms` - Parsed search terms
  - `IReadOnlyDictionary<string, string> Filters` - Field filters (chat:id, role:user, etc.)
  - `IReadOnlyList<string> BooleanOperators` - AND, OR, NOT operators
  - `bool HasFilters` - True if any filters present
  - `DateRange? DateRange` - For --since/--until filters
- Methods:
  - `static SearchQuery Create(string queryText, IEnumerable<string> terms, ...)` - Factory
- Should be immutable record or class with no setters

**Success Criteria:**
- [✅] File compiles without errors
- [✅] All properties are read-only
- [✅] DateRange property for date filtering

**Effort:** 30 minutes

---

### Gap 2: SearchResult Domain Model [⬜]

**Spec Reference:** Lines 160-170 (Result format example)

**What to Implement:**
- File: `src/AgenticCoder.Domain/Search/SearchResult.cs`
- Record type (immutable) with properties:
  - `MessageId` - ID of indexed message
  - `double Score` - BM25 relevance score
  - `string Snippet` - Contextual text excerpt (150 chars default)
  - `IReadOnlyList<(int start, int length)> MatchPositions` - For highlighting
  - `DateTimeOffset CreatedAt` - Message timestamp
  - `ChatId ChatId` - Chat ID for filtering
  - `string Role` - user|assistant|system|tool

**Success Criteria:**
- [✅] Record compiles
- [✅] Used by ISearchEngine return type
- [✅] Immutable (no public setters)

**Effort:** 20 minutes

---

### Gap 3: ISearchIndexer Application Interface [⬜]

**Spec Reference:** Lines 107-131 (Incremental Indexing Flow section)

**What to Implement:**
- File: `src/AgenticCoder.Application/Search/ISearchIndexer.cs`
- Methods:
  - `Task IndexMessageAsync(Message message, CancellationToken ct)` - Add/update message in index
  - `Task DeleteMessageAsync(MessageId messageId, CancellationToken ct)` - Remove from index
  - `Task RebuildIndexAsync(CancellationToken ct)` - Full reindex all messages
  - `Task OptimizeAsync(CancellationToken ct)` - Optimize index segments
- Performance targets documented in XML comments:
  - Single message: <10ms (AC-019)
  - Batch 100: <1 second (AC-020)
  - Full rebuild 10k: <60 seconds (AC-021)

**Success Criteria:**
- [✅] Interface compiles
- [✅] All methods declared with correct signatures
- [✅] XML comments document performance SLAs

**Effort:** 15 minutes

---

### Gap 4: ISearchEngine Application Interface [⬜]

**Spec Reference:** Lines 143-153 (Build FTS5 query example)

**What to Implement:**
- File: `src/AgenticCoder.Application/Search/ISearchEngine.cs`
- Methods:
  - `Task<IReadOnlyList<SearchResult>> SearchAsync(SearchQuery query, SearchFilters filters, int limit, int offset, CancellationToken ct)` - Execute search
  - `Task<IndexStatus> GetIndexStatusAsync(CancellationToken ct)` - Get index health
- Record: `IndexStatus(int MessageCount, int PendingCount, long SizeBytes, DateTime LastOptimized, int SegmentCount, string HealthStatus)`
- Performance target: <500ms for 10k messages (AC-128)

**Success Criteria:**
- [✅] Interface compiles
- [✅] Return types correct (SearchResult list, IndexStatus)

**Effort:** 15 minutes

---

### Gap 5: IQueryParser Application Interface [⬜]

**Spec Reference:** Lines 136-170 (Query Processing section)

**What to Implement:**
- File: `src/AgenticCoder.Application/Search/IQueryParser.cs`
- Methods:
  - `Task<SearchQuery> ParseAsync(string queryText, CancellationToken ct)` - Parse user input
  - `Task<ValidationResult> ValidateAsync(SearchQuery query, CancellationToken ct)` - Validate parsed query
- Performance: <5ms per parse (AC-030)

**Success Criteria:**
- [✅] Interface compiles
- [✅] Async methods with proper cancellation

**Effort:** 10 minutes

---

### Gap 6: ISnippetGenerator Application Interface [⬜]

**Spec Reference:** Lines 57-67, AC-057-062 (Snippets section)

**What to Implement:**
- File: `src/AgenticCoder.Application/Search/ISnippetGenerator.cs`
- Method:
  - `Task<string> GenerateAsync(string content, IEnumerable<(int start, int length)> matchPositions, int length, CancellationToken ct)` → Snippet string
- Performance: <50ms per snippet (AC-062)

**Success Criteria:**
- [✅] Interface compiles
- [✅] Match positions parameter for highlighting

**Effort:** 10 minutes

---

### Gap 7: IHighlightFormatter Application Interface [⬜]

**File:** `src/AgenticCoder.Application/Search/IHighlightFormatter.cs`
- Method: `Task<string> HighlightAsync(string snippet, IEnumerable<(int start, int length)> matchPositions, CancellationToken ct)` → HTML-marked snippet

**Effort:** 10 minutes

---

---

## PHASE 1: INDEXING INFRASTRUCTURE - FTS5 & POSTGRES (15-18 hours, 8 tests)

**Objective:** Implement SQLite FTS5 and PostgreSQL full-text search indexing with incremental updates

### Gap 8: Database Migration - FTS5 Virtual Table [⬜]

**Spec Reference:** Lines 83-98 (FTS5 Virtual Table Schema section)

**What to Implement:**
- File: `src/AgenticCoder.Infrastructure/Persistence/Migrations/Migration_YYYYMM_CreateFts5Index.cs`
- Create virtual table for full-text search:

```sql
CREATE VIRTUAL TABLE conversation_search USING fts5(
    message_id UNINDEXED,
    chat_id UNINDEXED,
    created_at UNINDEXED,
    role UNINDEXED,
    content,
    chat_title,
    tags,
    tokenize='porter unicode61',
    content='messages',
    content_rowid='rowid'
);
```

- Key design:
  - UNINDEXED columns: metadata for filtering, not searched
  - Porter tokenizer: English stemming
  - External content: References `messages` table (avoid duplication)
  - Unicode61: International character support

**Test:** Migration applies without error, table created correctly

**Effort:** 1 hour

---

### Gap 9: SqliteFtsIndexer Implementation [⬜]

**Spec Reference:** Lines 107-131 (Incremental Indexing Flow)

**What to Implement:**
- File: `src/AgenticCoder.Infrastructure/Search/Indexing/SqliteFtsIndexer.cs`
- Implements: ISearchIndexer
- Constructor: Takes `IDbConnection`, `ILogger<SqliteFtsIndexer>`
- Method: `IndexMessageAsync(Message message, CancellationToken ct)`
  - Extract: message.Content, chat.Title, tags
  - Exclude: Empty content (AC-009), binary content (AC-010)
  - INSERT INTO conversation_search with parameterized query
  - Latency <10ms (AC-019)
  - Logging: DEBUG on success
- Method: `DeleteMessageAsync(MessageId messageId, CancellationToken ct)`
  - DELETE from conversation_search WHERE message_id = ?
- Method: `RebuildIndexAsync(CancellationToken ct)`
  - SELECT all messages, call IndexMessageAsync for each
  - Batch updates for performance
  - Latency <60 seconds for 10k messages (AC-021)
  - Progress logging at INFO level
- Method: `OptimizeAsync(CancellationToken ct)`
  - PRAGMA optimize to compact segments

**Test File to Write First (RED):**
- File: `Tests/Unit/Search/SqliteFtsIndexerTests.cs`
- Test 1: `Should_Index_Message_Content()` - INSERT works, message findable
- Test 2: `Should_Tokenize_With_Porter_Stemmer()` - "authenticate" matches "authenticated"
- Test 3: `Should_Update_Existing_Message_In_Index()` - Re-index updates content
- Test 4: `Should_Remove_Message_From_Index()` - DELETE works
- Test 5: `Should_Meet_Single_Message_Latency_Target()` - <10ms
- Test 6: `Should_Batch_Index_100_Messages_Under_1_Second()` - <1s for batch
- Test 7: `Should_Rebuild_10k_Messages_Under_60_Seconds()` - Full reindex performance
- Test 8: `Should_Exclude_Empty_Messages_From_Index()` - AC-009

**Success Criteria:**
- [✅] All 8 tests pass
- [✅] Latency <10ms single, <1s batch, <60s rebuild
- [✅] Porter stemming works (authenticate → authenticated match)
- [✅] Parameterized SQL (no string concatenation)
- [✅] Logging at correct levels

**Effort:** 3.5 hours implementation + 2.5 hours testing

---

### Gap 10: PostgresSearchIndexer Implementation [⬜]

**Spec Reference:** Lines 14-15 (AC-014-015, Postgres backend)

**What to Implement:**
- File: `src/AgenticCoder.Infrastructure/Search/Indexing/PostgresSearchIndexer.cs`
- Implements: ISearchIndexer
- Uses PostgreSQL tsvector for full-text search
- Create GIN index on tsvector column
- Same interface as SqliteFtsIndexer
- Methods: IndexMessageAsync, DeleteMessageAsync, RebuildIndexAsync, OptimizeAsync

**Test File:**
- File: `Tests/Integration/Search/PostgresSearchIndexerTests.cs`
- Test 1: `Should_Index_Message_With_Tsvector()` (if PostgreSQL available)
- Test 2: `Should_Create_GIN_Index_For_Performance()` (AC-015)
- Test 3: `Should_Match_Postgres_Latency_Targets()`

**Success Criteria:**
- [✅] Tests pass (conditional on PostgreSQL)
- [✅] Compatible with ISearchIndexer interface
- [✅] GIN index created on tsvector

**Effort:** 2 hours implementation + 1.5 hours testing

---

### Gap 11: SearchIndexEventHandler Implementation [⬜]

**Spec Reference:** Lines 119-120 (MessageCreated event triggers indexing)

**What to Implement:**
- File: `src/AgenticCoder.Infrastructure/Search/Indexing/SearchIndexEventHandler.cs`
- Implements: IMessageCreatedEventHandler, IMessageUpdatedEventHandler, IMessageDeletedEventHandler (from event system)
- On MessageCreated: Call `_indexer.IndexMessageAsync(message)` (AC-091)
- On MessageUpdated: Re-index message
- On MessageDeleted: Call `_indexer.DeleteMessageAsync(messageId)`
- Queue updates: Process within 1 second (AC-095)
- Logging: DEBUG level for indexing events
- Exception handling: Log ERROR, continue processing

**Test File:**
- File: `Tests/Unit/Search/SearchIndexEventHandlerTests.cs`
- Test 1: `Should_Index_New_Messages_On_Created_Event()` (AC-091)
- Test 2: `Should_Remove_Deleted_Messages_From_Index()` (AC-093)
- Test 3: `Should_Queue_Updates_Within_1_Second()` (AC-095)

**Success Criteria:**
- [✅] All 3 tests pass
- [✅] Incremental indexing works automatically
- [✅] Event subscriptions registered correctly
- [✅] Queue processes within 1s

**Effort:** 1.5 hours implementation + 1 hour testing

---

### Gap 12: IndexMaintenanceService Background Task [⬜]

**Spec Reference:** Lines AC-096-100 (Index optimization)

**What to Implement:**
- File: `src/AgenticCoder.Infrastructure/Search/Indexing/IndexMaintenanceService.cs`
- Implements: IHostedService
- Runs on schedule (e.g., every 5 minutes)
- Calls `_indexer.OptimizeAsync()` to merge segments
- Cleans up pending index queue
- Can run while searches continue (no blocking - AC-099)
- Logging: INFO on start/completion, DEBUG on intermediate steps

**Test:**
- File: `Tests/Integration/Search/IndexMaintenanceTests.cs`
- Test 1: `Should_Run_Background_Optimization_Task()`
- Test 2: `Should_Not_Block_Concurrent_Searches()`

**Success Criteria:**
- [✅] Both tests pass
- [✅] Background task runs without blocking
- [✅] Index optimization executes

**Effort:** 1.5 hours

---

### Gap 13: PostgreSQL Migration [⬜]

**Spec Reference:** AC-014-015 (Postgres backend setup)

**What to Implement:**
- File: `src/AgenticCoder.Infrastructure/Persistence/Migrations/Migration_YYYYMM_CreatePostgresSearch.cs`
- Create tsvector column on messages table
- Create GIN index on tsvector
- Setup English dictionary for stemming

**Effort:** 1 hour (conditional on PostgreSQL support)

---

---

## PHASE 2: SEARCH ENGINE, QUERY PARSER & RANKING (18-22 hours, 12 tests)

**Objective:** Implement query execution, parsing, and BM25 relevance ranking

### Gap 14: QueryParser Implementation [⬜]

**Spec Reference:** Lines 136-153 (Query Processing), AC-025-037 (Basic and Boolean Queries)

**What to Implement:**
- File: `src/AgenticCoder.Infrastructure/Search/Querying/QueryParser.cs`
- Implements: IQueryParser
- Method: `ParseAsync(queryText, ct) → SearchQuery`
  - Tokenize: Split on whitespace, preserve case-insensitive
  - Extract terms: ["JWT", "validation"]
  - Extract Boolean operators: AND, OR, NOT (max 5 - AC-036)
  - Extract field prefixes: `role:user`, `chat:id`, `tag:name`
  - Handle phrases: Quoted text "JWT token" (AC-027)
  - Handle parentheses: Group expressions (AC-035)
  - Error: Return ACODE-SRCH-001 for invalid syntax (AC-037)
- Method: `ValidateAsync(query, ct) → ValidationResult`
  - Check: Empty query error (AC-031)
  - Check: Max 5 Boolean operators (AC-036)
  - Check: Balanced parentheses
- Performance: <5ms per parse (AC-030)

**Test File:**
- File: `Tests/Unit/Search/QueryParserTests.cs`
- Test 1: `Should_Parse_Single_Term()` - "JWT" → Terms=["JWT"]
- Test 2: `Should_Parse_Multi_Term_Query()` - "JWT validation" → Terms=["JWT", "validation"]
- Test 3: `Should_Extract_Phrase_Queries_With_Quotes()` - '"JWT token"' → phrase match
- Test 4: `Should_Parse_Boolean_Operators()` - "auth AND token" → BooleanOperators=[AND]
- Test 5: `Should_Handle_OR_Operator_Expansion()` - "auth OR token" (AC-033)
- Test 6: `Should_Handle_NOT_Operator_Exclusion()` - "auth NOT token" (AC-034)
- Test 7: `Should_Extract_Field_Prefixes()` - "role:user chat:id" → Filters extracted
- Test 8: `Should_Reject_Invalid_Syntax()` - Unbalanced quotes, too many operators → Error
- Test 9: `Should_Parse_In_Under_5ms()` - Latency target (AC-030)
- Test 10: `Should_Error_On_Empty_Query()` - AC-031

**Success Criteria:**
- [✅] All 10 tests pass
- [✅] <5ms per parse
- [✅] Boolean operators limited to 5 (AC-036)
- [✅] Field prefixes correctly extracted

**Effort:** 4 hours implementation + 2.5 hours testing

---

### Gap 15: SqliteFtsSearchEngine Implementation [⬜]

**Spec Reference:** Lines 143-153 (FTS5 Query execution example), AC-025-031 (Basic searches)

**What to Implement:**
- File: `src/AgenticCoder.Infrastructure/Search/Querying/SqliteFtsSearchEngine.cs`
- Implements: ISearchEngine
- Constructor: Takes `IDbConnection`, `ILogger<SqliteFtsSearchEngine>`
- Method: `SearchAsync(query, filters, limit, offset, ct) → List<SearchResult>`
  1. Build FTS5 MATCH query from parsed terms:
     ```sql
     SELECT message_id, snippet(conversation_search, 2, '<mark>', '</mark>', '...', 32) AS snippet,
            bm25(conversation_search) AS score
     FROM conversation_search
     WHERE conversation_search MATCH 'JWT AND validation'
       AND chat_id = ?  -- filters applied
       AND created_at >= ?  -- date filter
     ORDER BY score DESC
     LIMIT ?
     ```
  2. Execute parameterized query (AC-093 security)
  3. Extract match offsets from snippet for highlighting
  4. Build SearchResult objects with score, snippet, positions
  5. Performance: <500ms for 10k messages (AC-128), <1.5s for 100k (AC-129)
- Method: `GetIndexStatusAsync(ct) → IndexStatus`
  - Query pragma index_info, measure segment count
  - Return: MessageCount, SegmentCount, SizeBytes, etc.

**Test File:**
- File: `Tests/Integration/Search/SqliteFtsSearchEngineTests.cs`
- Test 1: `Should_Find_Messages_By_Single_Term()` - (AC-025)
- Test 2: `Should_Find_Messages_By_Multiple_Terms()` - (AC-026, default OR logic)
- Test 3: `Should_Execute_Phrase_Query()` - (AC-027, quoted phrases)
- Test 4: `Should_Find_Messages_Case_Insensitive()` - (AC-028, "JWT" finds "jwt")
- Test 5: `Should_Find_Stemmed_Matches()` - (AC-029, "authenticate" finds "authenticated")
- Test 6: `Should_Apply_AND_Operator()` - (AC-032, both terms required)
- Test 7: `Should_Apply_OR_Operator()` - (AC-033, either term matches)
- Test 8: `Should_Apply_NOT_Operator()` - (AC-034, exclude term)
- Test 9: `Should_Generate_Snippet_With_Context()` - (AC-057, snippet extraction)
- Test 10: `Should_Meet_10k_Search_Latency_Target()` - (AC-128, <500ms)
- Test 11: `Should_Meet_100k_Search_Latency_Target()` - (AC-129, <1.5s)
- Test 12: `Should_Return_Index_Status()` - (AC-106-110)

**Success Criteria:**
- [✅] All 12 tests pass
- [✅] Boolean operators work correctly
- [✅] Phrase queries work
- [✅] Stemming matches work
- [✅] Latency targets met (<500ms/10k, <1.5s/100k)
- [✅] Snippets generated with positions
- [✅] Parameterized queries (no injection risk)

**Effort:** 4.5 hours implementation + 2.5 hours testing

---

### Gap 16: PostgresSearchEngine Implementation [⬜]

**Spec Reference:** AC-014 (Postgres tsvector backend)

**What to Implement:**
- File: `src/AgenticCoder.Infrastructure/Search/Querying/PostgresSearchEngine.cs`
- Implements: ISearchEngine
- Uses PostgreSQL to_tsquery, tsvector, GIN index
- Same interface and performance targets as SQLite
- Query format: `WHERE tsvector_col @@ to_tsquery(?)`

**Test:** Conditional on PostgreSQL availability

**Effort:** 2.5 hours

---

### Gap 17: BM25Ranker Implementation [⬜]

**Spec Reference:** Lines 172-186 (BM25 algorithm description), AC-044-050 (Ranking section)

**What to Implement:**
- File: `src/AgenticCoder.Infrastructure/Search/Querying/BM25Ranker.cs`
- BM25 formula (mathematical):
  ```
  score = Σ IDF(term) × (TF(term) × (k1 + 1)) / (TF(term) + k1 × (1 - b + b × (|D| / avgDL)))
  ```
  Where: k1=1.2 (term saturation), b=0.75 (length normalization)
- Method: `CalculateScoreAsync(document, terms, ct) → double`
  - Calculate IDF for each term (inverse document frequency)
  - Calculate TF for each term in document (term frequency)
  - Apply formula with k1=1.2, b=0.75
  - Return final BM25 score
- Apply weighting:
  - Title matches: 2x (AC-048)
  - Exact phrases: Higher than term matches (AC-049)
- Performance: <1ms per result (AC-050)

**Test File:**
- File: `Tests/Unit/Search/BM25RankerTests.cs`
- Test 1: `Should_Calculate_Inverse_Document_Frequency()` - IDF formula correct
- Test 2: `Should_Calculate_Term_Frequency()` - TF matches document content
- Test 3: `Should_Apply_BM25_Formula()` - Full formula calculation
- Test 4: `Should_Apply_Title_Match_Weighting()` - 2x for titles (AC-048)
- Test 5: `Should_Rate_Exact_Phrases_Higher()` - (AC-049)

**Success Criteria:**
- [✅] All 5 tests pass
- [✅] BM25 calculation mathematically correct
- [✅] Weighting rules applied
- [✅] <1ms per score calculation

**Effort:** 2.5 hours implementation + 1.5 hours testing

---

### Gap 18: RecencyBooster Implementation [⬜]

**Spec Reference:** Lines 188-198 (Recency Boost section), AC-051-056

**What to Implement:**
- File: `src/AgenticCoder.Infrastructure/Search/Querying/RecencyBooster.cs`
- Apply multiplier based on message age:
  - <24h: 1.5x (AC-051)
  - 7-30d: 1.2x (AC-052)
  - >30d: 1.0x (no boost - AC-053)
- Constructor: Takes `IOptions<SearchOptions>` for configuration (AC-054)
- Method: `ApplyBoostAsync(result, createdAt, ct) → boostedScore`
  - Calculate age: DateTimeOffset.UtcNow - createdAt
  - Apply appropriate multiplier
  - Return: baseScore × multiplier
- Disable-able via config (AC-055)
- Alternative: Sort by date available (AC-056)

**Test File:**
- File: `Tests/Unit/Search/RecencyBoosterTests.cs`
- Test 1: `Should_Apply_1_5x_Boost_For_Messages_Under_24_Hours()` (AC-051)
- Test 2: `Should_Apply_1_2x_Boost_For_Messages_7_to_30_Days_Old()` (AC-052)
- Test 3: `Should_Not_Boost_Messages_Older_Than_30_Days()` (AC-053)
- Test 4: `Should_Be_Configurable_Via_Settings()` (AC-054)
- Test 5: `Should_Be_Disableable()` (AC-055)

**Success Criteria:**
- [✅] All 5 tests pass
- [✅] Multipliers correctly applied
- [✅] Configurable and disableable
- [✅] Date calculation accurate

**Effort:** 1.5 hours

---

---

## PHASE 3: SNIPPETS & HIGHLIGHTING (6-8 hours, 6 tests)

**Objective:** Generate contextual snippets with term highlighting

### Gap 19: SnippetGenerator Implementation [⬜]

**Spec Reference:** Lines 57-67, AC-057-062 (Snippets - Generation section)

**What to Implement:**
- File: `src/AgenticCoder.Infrastructure/Search/Results/SnippetGenerator.cs`
- Implements: ISnippetGenerator
- Method: `GenerateAsync(content, matchPositions, length, ct) → string`
  - Default length: 150 characters (AC-058)
  - Configurable: 50-500 characters (AC-059)
  - Center snippet around first match (AC-060)
  - Preserve word boundaries - don't truncate mid-word (AC-061)
  - Add "..." before/after if truncated
  - Performance: <50ms per snippet (AC-062)
  - Algorithm:
    1. Find first match position
    2. Calculate start = max(0, position - length/2)
    3. Find word boundary before start
    4. Find word boundary after (start + length)
    5. Return substring with boundaries preserved

**Test File:**
- File: `Tests/Unit/Search/SnippetGeneratorTests.cs`
- Test 1: `Should_Generate_Snippet_Centered_On_First_Match()` (AC-060)
- Test 2: `Should_Preserve_Word_Boundaries()` (AC-061, no mid-word truncation)
- Test 3: `Should_Use_Default_150_Character_Length()` (AC-058)
- Test 4: `Should_Respect_Configurable_Length()` (AC-059, 50-500 range)
- Test 5: `Should_Add_Ellipsis_When_Truncated()`
- Test 6: `Should_Generate_In_Under_50ms()` (AC-062)

**Success Criteria:**
- [✅] All 6 tests pass
- [✅] Word boundaries preserved
- [✅] <50ms per snippet
- [✅] Configurable length (50-500)
- [✅] Default 150 characters

**Effort:** 1.5 hours implementation + 1.5 hours testing

---

### Gap 20: HighlightFormatter Implementation [⬜]

**Spec Reference:** Lines AC-063-068 (Snippets - Highlighting section)

**What to Implement:**
- File: `src/AgenticCoder.Infrastructure/Search/Results/HighlightFormatter.cs`
- Implements: IHighlightFormatter
- Method: `HighlightAsync(snippet, matchPositions, ct) → string`
  - Wrap matching terms in `<mark>` tags (AC-063)
  - Handle multiple matches in same snippet (AC-064)
  - Configurable tags: `<mark>` default, but customizable (AC-065)
  - Algorithm:
    1. Sort match positions by offset
    2. Build string from left to right
    3. Insert opening tag at match start
    4. Insert closing tag at match end
    5. Adjust offsets as tags are added
  - Return: HTML-marked snippet ready for display

**Test File:**
- File: `Tests/Unit/Search/HighlightFormatterTests.cs`
- Test 1: `Should_Wrap_Single_Match_In_Mark_Tags()` (AC-063)
- Test 2: `Should_Highlight_Multiple_Matches_In_Snippet()` (AC-064)
- Test 3: `Should_Support_Configurable_Highlight_Tags()` (AC-065)
- Test 4: `Should_Handle_Overlapping_Match_Positions()`

**Success Criteria:**
- [✅] All 4 tests pass
- [✅] Multiple matches highlighted
- [✅] Tags configurable
- [✅] Correct offset calculation

**Effort:** 1.5 hours

---

### Gap 21: ResultFormatter Implementation [⬜]

**Spec Reference:** Lines AC-066-067 (Highlighting section), AC-114-120 (CLI output)

**What to Implement:**
- File: `src/AgenticCoder.Infrastructure/Search/Results/ResultFormatter.cs`
- Methods:
  - `FormatTableAsync(results, ct) → string` - ASCII table format
    - Columns: Score, Chat, Timestamp, Role, Snippet
    - Colors: Use ANSI codes for highlighted terms (AC-066)
    - Headers: Column names
    - Table layout: ┌─────┬──────┬──────────┬──────┬──────────┐
  - `FormatJsonAsync(results, ct) → string` - JSON array
    - Output raw HTML tags (not rendered - AC-067)
    - Field names: messageId, score, snippet, matchPositions, createdAt, chatId, role
    - Valid JSON array format (AC-116)
  - `ConfigureAsync(outputFormat, highlightTag, ct)`

**Test File:**
- File: `Tests/Unit/Search/ResultFormatterTests.cs`
- Test 1: `Should_Format_Table_With_Columns()` (AC-114-115)
- Test 2: `Should_Apply_ANSI_Colors_For_Highlights()` (AC-066)
- Test 3: `Should_Format_Valid_JSON_Array()` (AC-116-167)
- Test 4: `Should_Output_Raw_HTML_Tags_In_JSON()` (AC-067)

**Success Criteria:**
- [✅] All 4 tests pass
- [✅] Table format readable
- [✅] JSON valid and parseable
- [✅] Colors work in table mode

**Effort:** 1.5 hours

---

---

## PHASE 4: FILTERING & ADVANCED QUERIES (6-8 hours, 6 tests)

**Objective:** Implement field-specific filters and combined query logic

### Gap 22: SearchFilters Implementation [⬜]

**Spec Reference:** Lines AC-069-090 (Filters sections)

**What to Implement:**
- File: `src/AgenticCoder.Infrastructure/Search/Querying/SearchFilters.cs`
- Class with properties:
  - `IReadOnlyList<ChatId> ChatIds` - `--chat` filter (AC-069-074)
  - `IReadOnlyList<string> Roles` - `--role` filter (AC-081-085)
  - `DateRange? DateRange` - `--since`/`--until` (AC-075-080)
  - `IReadOnlyList<string> Tags` - `--tag` filter (AC-042)
- Parsing methods:
  - `ParseDateRange(since, until)` - ISO 8601 or relative (7d, 2w, 1m)
  - `ValidateRoles()` - user|assistant|system|tool only
  - `ResolveChat(nameOrId)` - Look up chat by name or ID
- Application logic (AND logic for all filters - AC-087):
  - Each filter narrows results
  - Empty result set OK, not error (AC-089)
  - Filter stats available in verbose mode (AC-090)

**Test File:**
- File: `Tests/Unit/Search/SearchFiltersTests.cs`
- Test 1: `Should_Parse_Chat_Filter_By_ID()` (AC-069)
- Test 2: `Should_Resolve_Chat_Filter_By_Name()` (AC-070)
- Test 3: `Should_Combine_Multiple_Chat_Filters_With_OR()` (AC-071)
- Test 4: `Should_Parse_Role_Filter()` (AC-081-085)
- Test 5: `Should_Parse_ISO_Date_Format()` (AC-078)
- Test 6: `Should_Parse_Relative_Date_Format()` (AC-079, "7d", "2w")
- Test 7: `Should_Combine_All_Filters_With_AND_Logic()` (AC-087)
- Test 8: `Should_Return_Empty_Results_Not_Error()` (AC-089)

**Success Criteria:**
- [✅] All 8 tests pass
- [✅] All filter types work
- [✅] AND logic enforced
- [✅] Date parsing works

**Effort:** 2.5 hours implementation + 1.5 hours testing

---

### Gap 23: Field-Specific Search Integration [⬜]

**Spec Reference:** Lines AC-038-043 (Field-Specific Queries)

**What to Implement:**
- Enhance QueryParser to support field prefixes:
  - `role:user` → Filter by role
  - `role:assistant` → Filter by role
  - `chat:name` → Filter by chat
  - `title:term` → Search titles only
  - `tag:name` → Filter by tag
  - Combined: `role:user chat:auth title:JWT` (AC-043)
- Extract field filters from query string
- Populate SearchFilters object from extracted fields
- Integration: QueryParser → SearchFilters

**Test:** Add to existing QueryParserTests
- Test: `Should_Extract_Field_Filters_From_Query_String()` (AC-038-043)

**Effort:** 1.5 hours

---

---

## PHASE 5: CLI COMMANDS & INDEX MAINTENANCE (6-8 hours, 6 tests)

**Objective:** Implement CLI search interface and index management tools

### Gap 24: IndexOptimizer Implementation [⬜]

**Spec Reference:** Lines AC-096-100 (Index Optimization)

**What to Implement:**
- File: `src/AgenticCoder.Infrastructure/Search/Indexing/IndexOptimizer.cs`
- Constructor: Takes `ISearchIndexer`, `ILogger`
- Method: `OptimizeAsync(progressCallback, ct)`
  - Merge FTS5 segments into single segment (AC-096-097)
  - Reduce segment count to 1 (AC-097)
  - Performance: <30 seconds for 50k messages (AC-098)
  - Can run while searches continue - no blocking (AC-099)
  - Call progressCallback(percentComplete) for CLI display (AC-100)
  - Logging: INFO start/completion, DEBUG intermediate

**Test:**
- File: `Tests/Integration/Search/IndexOptimizerTests.cs`
- Test 1: `Should_Merge_FTS5_Segments()` (AC-096-097)
- Test 2: `Should_Complete_In_Under_30_Seconds()` (AC-098)
- Test 3: `Should_Not_Block_Concurrent_Searches()` (AC-099)

**Success Criteria:**
- [✅] All 3 tests pass
- [✅] Segments properly merged
- [✅] <30s performance target
- [✅] Non-blocking

**Effort:** 1.5 hours

---

### Gap 25: IndexValidator Implementation [⬜]

**Spec Reference:** Lines AC-106-110 (Index Maintenance - Status)

**What to Implement:**
- File: `src/AgenticCoder.Infrastructure/Search/Indexing/IndexValidator.cs`
- Constructor: Takes `IDbConnection`, `ILogger`
- Method: `ValidateAsync(ct) → ValidationResult`
  - Check: Index message count matches source count
  - Check: No orphaned entries
  - Detect: Index corruption (AC-105)
  - Return: Healthy/Unhealthy with reason
  - Collect metrics:
    - IndexedMessageCount (AC-107)
    - PendingCount
    - IndexSizeBytes
    - LastOptimized timestamp (AC-108)
    - SegmentCount
    - HealthStatus (Healthy|Unhealthy)
  - Performance: <100ms (AC-110)

**Test:**
- File: `Tests/Unit/Search/IndexValidatorTests.cs`
- Test 1: `Should_Detect_Healthy_Index()` (AC-109)
- Test 2: `Should_Detect_Corrupted_Index()` (AC-105)
- Test 3: `Should_Return_Index_Status()` (AC-106-108)

**Success Criteria:**
- [✅] All 3 tests pass
- [✅] Corruption detection works
- [✅] Status collection complete
- [✅] <100ms latency (AC-110)

**Effort:** 1.5 hours

---

### Gap 26: SearchCommand CLI Implementation [⬜]

**Spec Reference:** Lines AC-111-120 (CLI - Search Command)

**What to Implement:**
- File: `src/AgenticCoder.Cli/Commands/SearchCommand.cs`
- Command: `acode search <query> [options]`
- Options:
  - `--chat <id>` - Filter by chat
  - `--since <date>` - Date range start
  - `--until <date>` - Date range end
  - `--role <role>` - Filter by role
  - `--json` - JSON output (AC-116)
  - `--page <n>` - Page number (AC-117)
  - `--page-size <n>` - Results per page, default 20 (AC-118)
  - `--verbose` - Show execution time and stats (AC-119)
  - `--help` - Show help (AC-112)
- Logic:
  1. Parse command arguments
  2. Validate query not empty (AC-113)
  3. Build SearchFilters from options
  4. Call ISearchEngine.SearchAsync()
  5. Format output: Table by default (AC-114), JSON with --json
  6. Show pagination info
  7. Return exit code 0 (AC-120)
- Errors: Show usage help if no query (AC-113)

**Test File:**
- File: `Tests/E2E/Search/SearchCommandTests.cs`
- Test 1: `Should_Execute_Search_Command()` (AC-111)
- Test 2: `Should_Show_Help_With_Help_Flag()` (AC-112)
- Test 3: `Should_Error_When_No_Query()` (AC-113)
- Test 4: `Should_Display_Results_In_Table_Format()` (AC-114-115)
- Test 5: `Should_Output_JSON_With_Flag()` (AC-116)
- Test 6: `Should_Handle_Pagination()` (AC-117-118)
- Test 7: `Should_Show_Stats_With_Verbose_Flag()` (AC-119)

**Success Criteria:**
- [✅] All 7 tests pass
- [✅] All options work
- [✅] Output formats correct
- [✅] Help works
- [✅] Exit codes correct

**Effort:** 2 hours implementation + 1.5 hours testing

---

### Gap 27: SearchIndexCommand CLI Implementation [⬜]

**Spec Reference:** Lines AC-96-110 (Index Maintenance CLI commands)

**What to Implement:**
- File: `src/AgenticCoder.Cli/Commands/SearchIndexCommand.cs`
- Sub-commands:
  - `acode search index status` - Show health (AC-106)
    - Output: MessageCount, SegmentCount, SizeBytes, LastOptimized, HealthStatus
    - Performance: <100ms (AC-110)
  - `acode search index optimize` - Merge segments (AC-096)
    - Show progress (AC-100)
  - `acode search index rebuild [--chat <id>]` - Full reindex (AC-101)
    - Reprocess all messages (AC-102)
    - Performance: <60s for 10k (AC-103)
    - Support Ctrl+C cancellation (AC-104)
    - Support partial rebuild by chat (AC-105)
  - `acode search index cleanup` - Remove orphaned entries

**Test File:**
- File: `Tests/E2E/Search/IndexManagementTests.cs`
- Test 1: `Should_Show_Index_Status()` (AC-106-110)
- Test 2: `Should_Optimize_Index_With_Progress()` (AC-96-100)
- Test 3: `Should_Rebuild_Full_Index()` (AC-101-103)
- Test 4: `Should_Support_Cancellation()` (AC-104)
- Test 5: `Should_Support_Partial_Rebuild_By_Chat()` (AC-105)

**Success Criteria:**
- [✅] All 5 tests pass
- [✅] All sub-commands work
- [✅] Performance targets met
- [✅] Progress display works
- [✅] Cancellation supported

**Effort:** 2 hours implementation + 1.5 hours testing

---

---

## PHASE 6: INTEGRATION & VERIFICATION (4-6 hours)

**Objective:** Full end-to-end integration, performance validation, audit

### Gap 28: End-to-End Integration Tests [⬜]

**File:** `Tests/E2E/Search/SearchIntegrationTests.cs`
- Test 1: `Should_Create_Message_Index_And_Find_It()` - Create → Index → Search
- Test 2: `Should_Update_Message_In_Index()` - Modify → Reindex → Search updated
- Test 3: `Should_Delete_Message_From_Index()` - Delete → Remove from index → Not found
- Test 4: `Should_Execute_Complex_Query()` - Multi-filter, Boolean ops, scoring
- Test 5: `Should_Handle_Large_Search_Results()` - 10k message search <500ms

**Effort:** 1.5 hours

---

### Gap 29: Performance Benchmarks [⬜]

**File:** `Tests/Performance/Search/SearchBenchmarks.cs`
- Benchmark: Single message indexing <10ms
- Benchmark: Batch 100 messages <1s
- Benchmark: Full rebuild 10k <60s
- Benchmark: Search 10k messages <500ms
- Benchmark: Search 100k messages <1.5s
- Benchmark: Snippet generation <50ms
- Benchmark: Query parsing <5ms

**Effort:** 2 hours

---

### Gap 30: Code Coverage & Audit [⬜]

**Verification:**
- [ ] Run: `dotnet test --collect:"XPlat Code Coverage"`
- [ ] Verify: >85% overall coverage
- [ ] Run: `dotnet build` → No errors/warnings
- [ ] Verify all 132 ACs have passing tests
- [ ] Performance SLAs met (AC-128-132)
- [ ] Error codes defined (ACODE-SRCH-001 through 006)
- [ ] Documentation complete

**Effort:** 1.5 hours

---

---

## EFFORT SUMMARY

| Phase | Objective | Hours | Tests | ACs |
|-------|-----------|-------|-------|-----|
| 0 | Domain & Interfaces | 3-4 | 0 | 7 |
| 1 | Indexing (FTS5/Postgres) | 15-18 | 8 | 24 |
| 2 | Search & Ranking | 18-22 | 12 | 30 |
| 3 | Snippets & Highlighting | 6-8 | 6 | 12 |
| 4 | Filtering & Advanced | 6-8 | 6 | 26 |
| 5 | CLI & Maintenance | 6-8 | 6 | 26 |
| 6 | Integration & Verification | 4-6 | - | 12 |
| **TOTAL** | | **88-100h** | **38+** | **132** |

---

## STATUS

**Status:** 🔴 READY FOR IMPLEMENTATION (0% Complete)

**Next Step:** Begin Phase 0 with SearchQuery entity and SearchResult record

---
