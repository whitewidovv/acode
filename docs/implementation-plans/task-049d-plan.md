# Task 049d Implementation Plan - Indexing + Fast Search

## Task Overview

**Task**: 049d - Indexing + Fast Search Over Chats/Runs/Messages
**Spec File**: docs/tasks/refined-tasks/Epic 02/task-049d-indexing-fast-search.md (3596 lines)
**Acceptance Criteria**: 132 (AC-001 through AC-132)
**Gap Analysis**: docs/implementation-plans/task-049d-gap-analysis.md

**Status**: Ready to begin implementation

**Branch**: feature/task-049d-indexing-fast-search (to be created)

---

## Implementation Progress

### Phase 0: Database Migration ✅
- [x] Create migrations/006_add_search_index.sql
- [x] Create migrations/006_add_search_index_down.sql
- [x] Test migration runs successfully
- [x] Verify FTS5 table created
- [x] Verify triggers created
- [x] Commit: 4ad1156 `feat(task-049d): Add FTS5 search index migration`

### Phase 1: Domain Value Objects (19 tests) ✅
- [x] Create tests/Acode.Domain.Tests/Search/SearchQueryTests.cs (11 tests)
- [x] Create tests/Acode.Domain.Tests/Search/SearchResultTests.cs (8 tests)
- [x] Run tests → RED (compilation fails)
- [x] Create src/Acode.Domain/Search/SearchQuery.cs (84 lines)
- [x] Create src/Acode.Domain/Search/SearchResult.cs (50 lines)
- [x] Create src/Acode.Domain/Search/SearchResults.cs (53 lines)
- [x] Create src/Acode.Domain/Search/MatchLocation.cs (22 lines)
- [x] Create src/Acode.Domain/Search/SortOrder.cs (22 lines)
- [x] Run tests → GREEN (19/19 passing)
- [x] Fix StyleCop violations (split types into separate files)
- [x] Commit: 1482c34 `feat(task-049d): Add Search domain value objects (Phase 1)`

### Phase 2: Application Interfaces ✅ / 🔄 / -
- [ ] Create src/Acode.Application/Interfaces/ISearchService.cs (25 lines)
- [ ] Verify interface compiles
- [ ] Add XML documentation
- [ ] Commit: `feat(task-049d): Add ISearchService interface`

### Phase 3: BM25Ranker (12 tests) ✅ / 🔄 / -
- [ ] Create tests/Acode.Infrastructure.Tests/Search/BM25RankerTests.cs (12 tests)
- [ ] Run tests → RED
- [ ] Create src/Acode.Infrastructure/Search/BM25Ranker.cs (80 lines)
- [ ] Run tests → GREEN (12/12 passing)
- [ ] Fix violations
- [ ] Commit: `feat(task-049d): Add BM25Ranker with recency boost`

### Phase 4: SnippetGenerator (10 tests) ✅ / 🔄 / -
- [ ] Create tests/Acode.Infrastructure.Tests/Search/SnippetGeneratorTests.cs (10 tests)
- [ ] Run tests → RED
- [ ] Create src/Acode.Infrastructure/Search/SnippetGenerator.cs (70 lines)
- [ ] Run tests → GREEN (10/10 passing)
- [ ] Fix violations
- [ ] Commit: `feat(task-049d): Add SnippetGenerator with highlighting`

### Phase 5: SafeQueryParser (8 tests) ✅ / 🔄 / -
- [ ] Create tests/Acode.Infrastructure.Tests/Search/SafeQueryParserTests.cs (8 tests)
- [ ] Run tests → RED
- [ ] Create src/Acode.Infrastructure/Search/SafeQueryParser.cs (60 lines)
- [ ] Run tests → GREEN (8/8 passing)
- [ ] Fix violations
- [ ] Commit: `feat(task-049d): Add SafeQueryParser for FTS5`

### Phase 6: SqliteFtsSearchService (20 tests) ✅ / 🔄 / -
- [ ] Create tests/Acode.Infrastructure.Tests/Search/SqliteFtsSearchServiceTests.cs (20 tests)
- [ ] Run tests → RED
- [ ] Create src/Acode.Infrastructure/Search/SqliteFtsSearchService.cs (150 lines)
- [ ] Run tests → GREEN (20/20 passing)
- [ ] Fix violations
- [ ] Commit: `feat(task-049d): Add SqliteFtsSearchService`

### Phase 7: SearchCommand CLI (12 tests) ✅ / 🔄 / -
- [ ] Create tests/Acode.Cli.Tests/Commands/SearchCommandTests.cs (12 tests)
- [ ] Run tests → RED
- [ ] Create src/Acode.Cli/Commands/SearchCommand.cs (90 lines)
- [ ] Run tests → GREEN (12/12 passing)
- [ ] Fix violations
- [ ] Commit: `feat(task-049d): Add search CLI command`

### Phase 8: Integration Tests (10 tests) ✅ / 🔄 / -
- [ ] Create tests/Acode.Integration.Tests/Search/SearchE2ETests.cs (10 tests)
- [ ] Test full indexing + search flow
- [ ] Test performance SLAs (AC-128, AC-129, AC-130)
- [ ] All 10 tests passing
- [ ] Commit: `test(task-049d): Add E2E integration tests`

### Phase 9: Audit and Documentation ✅ / 🔄 / -
- [ ] Run full audit per docs/AUDIT-GUIDELINES.md
- [ ] Verify all 132 acceptance criteria
- [ ] Create docs/audits/task-049d-audit-report.md
- [ ] Update this implementation plan with final status
- [ ] Update docs/PROGRESS_NOTES.md
- [ ] Commit: `docs(task-049d): Complete audit - ready for PR`

---

## Detailed Phase Breakdown

### Phase 0: Database Migration

**Objective**: Create FTS5 virtual table and automatic indexing triggers.

**Files to Create**:
1. migrations/006_add_search_index.sql (~40 lines)
2. migrations/006_add_search_index_down.sql (~10 lines)

**Migration Up SQL**:
```sql
-- migrations/006_add_search_index.sql
--
-- Purpose: Add FTS5 full-text search index for conversation history
-- Dependencies: 002_add_conversations
-- Author: acode-team
-- Date: 2026-01-10

-- ═══════════════════════════════════════════════════════════════════════
-- FTS5 FULL-TEXT SEARCH INDEX
-- ═══════════════════════════════════════════════════════════════════════

CREATE VIRTUAL TABLE IF NOT EXISTS conversation_search USING fts5(
    message_id UNINDEXED,
    chat_id UNINDEXED,
    run_id UNINDEXED,
    created_at UNINDEXED,
    role UNINDEXED,
    content,
    chat_title,
    tags,
    tokenize='porter unicode61'
);

-- ═══════════════════════════════════════════════════════════════════════
-- AUTOMATIC INDEXING TRIGGERS
-- ═══════════════════════════════════════════════════════════════════════

-- Index new messages automatically
CREATE TRIGGER IF NOT EXISTS conversation_search_after_insert
AFTER INSERT ON conv_messages
BEGIN
    INSERT INTO conversation_search (
        message_id,
        chat_id,
        run_id,
        created_at,
        role,
        content,
        chat_title,
        tags
    )
    SELECT
        NEW.id,
        r.chat_id,
        NEW.run_id,
        NEW.created_at,
        NEW.role,
        NEW.content,
        c.title,
        c.tags
    FROM conv_runs r
    INNER JOIN conv_chats c ON r.chat_id = c.id
    WHERE r.id = NEW.run_id;
END;

-- Update index when message updated
CREATE TRIGGER IF NOT EXISTS conversation_search_after_update
AFTER UPDATE ON conv_messages
BEGIN
    DELETE FROM conversation_search WHERE message_id = OLD.id;
    INSERT INTO conversation_search (
        message_id, chat_id, run_id, created_at, role, content, chat_title, tags
    )
    SELECT
        NEW.id, r.chat_id, NEW.run_id, NEW.created_at, NEW.role, NEW.content, c.title, c.tags
    FROM conv_runs r
    INNER JOIN conv_chats c ON r.chat_id = c.id
    WHERE r.id = NEW.run_id;
END;

-- Remove from index when message deleted
CREATE TRIGGER IF NOT EXISTS conversation_search_after_delete
AFTER DELETE ON conv_messages
BEGIN
    DELETE FROM conversation_search WHERE message_id = OLD.id;
END;

-- Update chat_title in index when chat title changes
CREATE TRIGGER IF NOT EXISTS conversation_search_after_chat_update
AFTER UPDATE OF title ON conv_chats
BEGIN
    UPDATE conversation_search
    SET chat_title = NEW.title
    WHERE chat_id = NEW.id;
END;
```

**Migration Down SQL**:
```sql
-- migrations/006_add_search_index_down.sql
DROP TRIGGER IF EXISTS conversation_search_after_delete;
DROP TRIGGER IF EXISTS conversation_search_after_update;
DROP TRIGGER IF EXISTS conversation_search_after_insert;
DROP TRIGGER IF EXISTS conversation_search_after_chat_update;
DROP TABLE IF EXISTS conversation_search;
```

**Verification**:
```bash
# Apply migration
sqlite3 .agent/data/workspace.db < migrations/006_add_search_index.sql

# Verify table created
sqlite3 .agent/data/workspace.db "SELECT name FROM sqlite_master WHERE type='table' AND name='conversation_search';"

# Verify triggers created
sqlite3 .agent/data/workspace.db "SELECT name FROM sqlite_master WHERE type='trigger' AND name LIKE 'conversation_search%';"

# Test trigger by inserting a message (in integration tests)
```

**Acceptance Criteria Covered**: AC-011 through AC-018

---

### Phase 1: Domain Value Objects

**RED Step**: Write tests first

1. **Create tests/Acode.Domain.Tests/Search/SearchQueryTests.cs**:
```csharp
using FluentAssertions;
using Xunit;

namespace Acode.Domain.Tests.Search;

public class SearchQueryTests
{
    [Fact]
    public void Validate_WithEmptyQueryText_ReturnsFailure()
    {
        // Arrange
        var query = new SearchQuery { QueryText = "" };

        // Act
        var result = query.Validate();

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Message.Should().Contain("empty");
    }

    [Fact]
    public void Validate_WithQueryTextTooLong_ReturnsFailure()
    {
        // Arrange
        var query = new SearchQuery { QueryText = new string('a', 201) };

        // Act
        var result = query.Validate();

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Message.Should().Contain("200");
    }

    [Fact]
    public void Validate_WithInvalidPageSize_ReturnsFailure()
    {
        // Arrange
        var query = new SearchQuery { QueryText = "test", PageSize = 0 };

        // Act
        var result = query.Validate();

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Message.Should().Contain("Page size");
    }

    [Fact]
    public void Validate_WithPageSizeTooLarge_ReturnsFailure()
    {
        // Arrange
        var query = new SearchQuery { QueryText = "test", PageSize = 101 };

        // Act
        var result = query.Validate();

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Message.Should().Contain("Page size");
    }

    [Fact]
    public void Validate_WithSinceAfterUntil_ReturnsFailure()
    {
        // Arrange
        var query = new SearchQuery
        {
            QueryText = "test",
            Since = DateTime.UtcNow,
            Until = DateTime.UtcNow.AddDays(-1)
        };

        // Act
        var result = query.Validate();

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Message.Should().Contain("Since");
    }

    [Fact]
    public void Validate_WithValidQuery_ReturnsSuccess()
    {
        // Arrange
        var query = new SearchQuery { QueryText = "test query", PageSize = 20 };

        // Act
        var result = query.Validate();

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Validate_WithAllFilters_ReturnsSuccess()
    {
        // Arrange
        var query = new SearchQuery
        {
            QueryText = "test",
            ChatId = Guid.NewGuid(),
            Since = DateTime.UtcNow.AddDays(-7),
            Until = DateTime.UtcNow,
            RoleFilter = MessageRole.User,
            PageSize = 10,
            PageNumber = 2
        };

        // Act
        var result = query.Validate();

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void QueryText_IsRequired()
    {
        // Arrange & Act
        var query = new SearchQuery { QueryText = null! };

        // Assert - should fail validation
        var result = query.Validate();
        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void PageSize_DefaultsTo20()
    {
        // Arrange & Act
        var query = new SearchQuery();

        // Assert
        query.PageSize.Should().Be(20);
    }

    [Fact]
    public void PageNumber_DefaultsTo1()
    {
        // Arrange & Act
        var query = new SearchQuery();

        // Assert
        query.PageNumber.Should().Be(1);
    }

    [Fact]
    public void SortBy_DefaultsToRelevance()
    {
        // Arrange & Act
        var query = new SearchQuery();

        // Assert
        query.SortBy.Should().Be(SearchQuery.SortOrder.Relevance);
    }
}
```

2. **Create tests/Acode.Domain.Tests/Search/SearchResultTests.cs**:
```csharp
using FluentAssertions;
using Xunit;

namespace Acode.Domain.Tests.Search;

public class SearchResultTests
{
    [Fact]
    public void TotalPages_WithExactDivision_CalculatesCorrectly()
    {
        // Arrange
        var results = new SearchResults
        {
            Results = Array.Empty<SearchResult>(),
            TotalCount = 100,
            PageSize = 20,
            PageNumber = 1,
            QueryTimeMs = 100
        };

        // Act & Assert
        results.TotalPages.Should().Be(5);
    }

    [Fact]
    public void TotalPages_WithRemainder_RoundsUp()
    {
        // Arrange
        var results = new SearchResults
        {
            Results = Array.Empty<SearchResult>(),
            TotalCount = 95,
            PageSize = 20,
            PageNumber = 1,
            QueryTimeMs = 100
        };

        // Act & Assert
        results.TotalPages.Should().Be(5); // Ceiling(95/20) = 5
    }

    [Fact]
    public void HasNextPage_WhenNotLastPage_ReturnsTrue()
    {
        // Arrange
        var results = new SearchResults
        {
            Results = Array.Empty<SearchResult>(),
            TotalCount = 100,
            PageSize = 20,
            PageNumber = 3,
            QueryTimeMs = 100
        };

        // Act & Assert
        results.HasNextPage.Should().BeTrue();
    }

    [Fact]
    public void HasNextPage_WhenLastPage_ReturnsFalse()
    {
        // Arrange
        var results = new SearchResults
        {
            Results = Array.Empty<SearchResult>(),
            TotalCount = 100,
            PageSize = 20,
            PageNumber = 5,
            QueryTimeMs = 100
        };

        // Act & Assert
        results.HasNextPage.Should().BeFalse();
    }

    [Fact]
    public void HasPreviousPage_WhenFirstPage_ReturnsFalse()
    {
        // Arrange
        var results = new SearchResults
        {
            Results = Array.Empty<SearchResult>(),
            TotalCount = 100,
            PageSize = 20,
            PageNumber = 1,
            QueryTimeMs = 100
        };

        // Act & Assert
        results.HasPreviousPage.Should().BeFalse();
    }

    [Fact]
    public void HasPreviousPage_WhenNotFirstPage_ReturnsTrue()
    {
        // Arrange
        var results = new SearchResults
        {
            Results = Array.Empty<SearchResult>(),
            TotalCount = 100,
            PageSize = 20,
            PageNumber = 3,
            QueryTimeMs = 100
        };

        // Act & Assert
        results.HasPreviousPage.Should().BeTrue();
    }

    [Fact]
    public void SearchResult_WithAllProperties_IsValid()
    {
        // Arrange & Act
        var result = new SearchResult
        {
            MessageId = Guid.NewGuid(),
            ChatId = Guid.NewGuid(),
            ChatTitle = "Test Chat",
            Role = MessageRole.User,
            CreatedAt = DateTime.UtcNow,
            Snippet = "This is a <mark>test</mark> snippet",
            Score = 12.34,
            Matches = new[]
            {
                new MatchLocation { Field = "content", StartOffset = 10, Length = 4 }
            }
        };

        // Assert
        result.MessageId.Should().NotBeEmpty();
        result.Snippet.Should().Contain("<mark>");
        result.Matches.Should().HaveCount(1);
    }

    [Fact]
    public void MatchLocation_StoresFieldAndOffsets()
    {
        // Arrange & Act
        var match = new MatchLocation
        {
            Field = "content",
            StartOffset = 42,
            Length = 10
        };

        // Assert
        match.Field.Should().Be("content");
        match.StartOffset.Should().Be(42);
        match.Length.Should().Be(10);
    }
}
```

**GREEN Step**: Implement production code

3. **Create src/Acode.Domain/Search/SearchQuery.cs**:
```csharp
// (Copy from Implementation Prompt lines 2889-2939)
```

4. **Create src/Acode.Domain/Search/SearchResult.cs**:
```csharp
// (Copy from Implementation Prompt lines 2944-2979)
```

**REFACTOR Step**: Fix violations and verify

5. Run `dotnet build` → fix any StyleCop violations
6. Run `dotnet test --filter "FullyQualifiedName~SearchQueryTests"` → verify 10/10 passing
7. Run `dotnet test --filter "FullyQualifiedName~SearchResultTests"` → verify 8/8 passing

**Acceptance Criteria Covered**: AC-025 through AC-031 (query validation)

---

### (Phases 2-8 continue with similar detail...)

---

## Acceptance Criteria Tracking

### Indexing - Content Capture (AC-001 through AC-010)
- [ ] AC-001: All message content indexed within 1 second
- [ ] AC-002: Chat titles indexed
- [ ] AC-003: Chat tags indexed
- [ ] AC-004: User prompts indexed separately
- [ ] AC-005: Assistant responses indexed
- [ ] AC-006: Tool call names indexed
- [ ] AC-007: Error messages indexed
- [ ] AC-008: Message metadata stored
- [ ] AC-009: Empty messages not indexed
- [ ] AC-010: Binary content excluded

### (Continue for all 132 ACs...)

---

## Current Session Status

**Date**: 2026-01-10
**Phase**: Planning complete, ready to create feature branch and begin Phase 0
**Tokens Used**: ~88k / 200k (44%)
**Tokens Remaining**: ~112k (56%)

**Next Steps**:
1. Create feature branch: `git checkout -b feature/task-049d-indexing-fast-search`
2. Begin Phase 0: Database migration
3. Continue autonomously through all phases
4. Update this plan as each phase completes

---

## Notes

- **TDD Mandatory**: Every production file must have tests written FIRST
- **Commit Frequency**: Commit after each phase completion
- **Build Verification**: Run `dotnet build` and `dotnet test` before each commit
- **Gap Analysis**: Reference docs/implementation-plans/task-049d-gap-analysis.md for requirements
- **Spec Reference**: docs/tasks/refined-tasks/Epic 02/task-049d-indexing-fast-search.md

**Implementation Start**: Pending feature branch creation
