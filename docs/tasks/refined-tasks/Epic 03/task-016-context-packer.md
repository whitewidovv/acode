# Task 016: Context Packer

**Priority:** P0 – Critical  
**Tier:** S – Core Infrastructure  
**Complexity:** 13 (Fibonacci points)  
**Phase:** Phase 3 – Intelligence Layer  
**Dependencies:** Task 015 (Indexing), Task 014 (RepoFS)  

---

## Description

Task 016 implements the Context Packer. The Context Packer assembles prompts for the LLM. It selects relevant content. It fits content within token limits. It maximizes information density.

LLM context windows are limited. Not all code fits. The packer decides what to include. Better selection means better agent performance.

The packer works with multiple sources. Search results. Open files. Referenced files. Recent changes. Each source contributes candidate content.

Chunking breaks files into pieces. Whole files may be too large. Chunks are meaningful pieces—functions, classes, sections. Good chunks preserve context.

Ranking prioritizes chunks. More relevant chunks rank higher. Relevance considers the current task. Higher-ranked chunks are included first.

Token budgeting enforces limits. Count tokens accurately. Fill budget optimally. Leave room for agent responses. Handle multi-turn conversations.

Deduplication removes redundancy. The same content might come from multiple sources. Include it once. Save tokens for other content.

The packer is used throughout the agent loop. The planner needs context. The executor needs context. Each stage may need different context.

Output format is optimized for LLM consumption. Clear file boundaries. Line numbers for reference. Syntax highlighting hints.

---

## Glossary / Terms

| Term | Definition |
|------|------------|
| Context Packer | Assembles LLM prompts |
| Context Window | LLM token limit |
| Chunk | Content piece |
| Ranking | Priority ordering |
| Token Budget | Allowed tokens |
| Deduplication | Remove duplicates |
| Relevance | Task applicability |
| Source | Content origin |
| Candidate | Potential inclusion |
| Selection | Chosen content |
| Overflow | Exceeds budget |
| Truncation | Cut to fit |
| Density | Info per token |
| Format | Output structure |
| Prompt | LLM input |

---

## Out of Scope

The following items are explicitly excluded from Task 016:

- **Semantic ranking** - Embedding-based (v2)
- **Dynamic budgeting** - Fixed budgets v1
- **Multi-model support** - Single model v1
- **Streaming assembly** - Batch assembly
- **Caching** - Fresh assembly each time
- **Compression** - No summarization v1

---

## Functional Requirements

### Source Collection

- FR-001: Collect from search results
- FR-002: Collect from open files
- FR-003: Collect from references
- FR-004: Collect from tool results
- FR-005: Source priority configurable

### Chunking

- FR-006: Chunk files by structure
- FR-007: Chunk by line count
- FR-008: Chunk by token count
- FR-009: Preserve meaningful units
- FR-010: Handle oversized files

### Ranking

- FR-011: Rank by relevance
- FR-012: Rank by recency
- FR-013: Rank by source priority
- FR-014: Combined ranking
- FR-015: Configurable weights

### Token Budgeting

- FR-016: Count tokens accurately
- FR-017: Enforce total budget
- FR-018: Reserve for response
- FR-019: Reserve for system prompt
- FR-020: Allocate by category

### Deduplication

- FR-021: Detect exact duplicates
- FR-022: Detect overlapping chunks
- FR-023: Keep highest-ranked duplicate
- FR-024: Merge overlapping chunks

### Selection

- FR-025: Select top-ranked chunks
- FR-026: Fill to budget
- FR-027: Handle chunk boundaries
- FR-028: Balance sources

### Formatting

- FR-029: File path headers
- FR-030: Line number annotations
- FR-031: Language hints
- FR-032: Separator markers
- FR-033: Consistent structure

### API

- FR-034: PackContextAsync MUST work
- FR-035: Accept sources list
- FR-036: Accept budget parameters
- FR-037: Return packed context
- FR-038: Return token count

---

## Non-Functional Requirements

### Performance

- NFR-001: Pack < 500ms
- NFR-002: Token count < 50ms
- NFR-003: Ranking < 100ms

### Accuracy

- NFR-004: Token count within 1%
- NFR-005: No budget overflow
- NFR-006: Consistent results

### Quality

- NFR-007: Meaningful chunks
- NFR-008: Relevant selection
- NFR-009: Clear formatting

---

## User Manual Documentation

### Overview

The Context Packer assembles prompts for the LLM. It selects relevant content, chunks it appropriately, ranks by importance, and fits within token limits.

### How It Works

```
Sources → Chunking → Ranking → Budgeting → Selection → Formatting → Context
```

1. **Sources**: Gather candidates from search, files, tools
2. **Chunking**: Break large files into meaningful pieces
3. **Ranking**: Order by relevance to current task
4. **Budgeting**: Calculate available tokens
5. **Selection**: Pick top-ranked to fill budget
6. **Formatting**: Structure for LLM consumption

### Configuration

```yaml
# .agent/config.yml
context:
  # Token budgets
  budget:
    total: 100000              # Total context window
    response_reserve: 8000     # Reserve for response
    system_reserve: 2000       # Reserve for system prompt
    
  # Source priorities (higher = more important)
  source_priority:
    tool_results: 100
    open_files: 80
    search_results: 60
    references: 40
    
  # Ranking weights
  ranking:
    relevance_weight: 0.5
    recency_weight: 0.3
    source_weight: 0.2
    
  # Chunking
  chunking:
    max_chunk_tokens: 2000
    min_chunk_tokens: 100
    prefer_structural: true
```

### Context Structure

The packed context looks like:

```
## File: src/Services/UserService.cs (lines 1-50)
```csharp
using System;
using Microsoft.Extensions.Logging;

namespace MyApp.Services
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _repository;
        private readonly ILogger<UserService> _logger;
        
        public UserService(IUserRepository repository, ILogger<UserService> logger)
        {
            _repository = repository;
            _logger = logger;
        }
        // ... (lines 16-50)
    }
}
```

## File: src/Interfaces/IUserService.cs (lines 1-20)
```csharp
namespace MyApp.Services
{
    public interface IUserService
    {
        Task<User> GetByIdAsync(int id);
        Task<User> CreateAsync(CreateUserRequest request);
    }
}
```
```

### Debugging Context

```bash
$ acode context show

Context Summary
────────────────────
Total budget: 100,000 tokens
Used: 45,234 tokens (45%)

Sources:
  Tool results: 15,234 tokens (5 items)
  Open files: 20,000 tokens (3 files)
  Search results: 10,000 tokens (10 chunks)

Chunks:
  1. UserService.cs:1-50 (2,000 tokens) [tool]
  2. IUserService.cs:1-20 (500 tokens) [search]
  3. UserController.cs:15-80 (1,500 tokens) [open]
  ...
```

### Troubleshooting

#### Context Too Small

**Problem:** Important code not included

**Solutions:**
1. Increase total budget
2. Adjust source priorities
3. Improve search queries

#### Context Overflow

**Problem:** Budget exceeded

**Solutions:**
1. This shouldn't happen (bug)
2. Check token counting
3. Reduce chunk sizes

#### Poor Relevance

**Problem:** Irrelevant code included

**Solutions:**
1. Adjust ranking weights
2. Improve chunking strategy
3. Better search queries

---

## Acceptance Criteria

### Sources

- [ ] AC-001: Search results collected
- [ ] AC-002: Open files collected
- [ ] AC-003: Tool results collected
- [ ] AC-004: Priority applied

### Chunking

- [ ] AC-005: Files chunked
- [ ] AC-006: Meaningful units
- [ ] AC-007: Size limits respected

### Ranking

- [ ] AC-008: Relevance ranked
- [ ] AC-009: Sources weighted
- [ ] AC-010: Combined scoring

### Budgeting

- [ ] AC-011: Accurate count
- [ ] AC-012: Budget enforced
- [ ] AC-013: Reserves respected

### Deduplication

- [ ] AC-014: Duplicates removed
- [ ] AC-015: Best version kept
- [ ] AC-016: Overlap handled

### Formatting

- [ ] AC-017: Headers added
- [ ] AC-018: Line numbers shown
- [ ] AC-019: Consistent format

---

## Testing Requirements

### Unit Tests

```
Tests/Unit/Context/
├── ChunkerTests.cs
│   ├── Should_Chunk_By_Structure()
│   ├── Should_Respect_Limits()
│   └── Should_Preserve_Units()
│
├── RankerTests.cs
│   ├── Should_Rank_By_Relevance()
│   ├── Should_Weight_Sources()
│   └── Should_Combine_Scores()
│
├── BudgetManagerTests.cs
│   ├── Should_Count_Tokens()
│   ├── Should_Enforce_Limit()
│   └── Should_Reserve_Space()
│
├── DeduplicatorTests.cs
│   ├── Should_Remove_Duplicates()
│   └── Should_Handle_Overlap()
│
└── FormatterTests.cs
    ├── Should_Add_Headers()
    └── Should_Format_Consistently()
```

### Integration Tests

```
Tests/Integration/Context/
├── ContextPackerIntegrationTests.cs
│   └── Should_Pack_Real_Context()
```

### E2E Tests

```
Tests/E2E/Context/
├── ContextE2ETests.cs
│   └── Should_Work_With_Agent()
```

### Performance Benchmarks

| Benchmark | Target | Maximum |
|-----------|--------|---------|
| Pack context | 250ms | 500ms |
| Token count | 25ms | 50ms |
| Ranking | 50ms | 100ms |

---

## User Verification Steps

### Scenario 1: Basic Packing

1. Search for code
2. Pack context
3. Verify: Results included

### Scenario 2: Budget Limit

1. Many sources
2. Pack with small budget
3. Verify: Top-ranked included

### Scenario 3: Deduplication

1. Same file from multiple sources
2. Pack context
3. Verify: Included once

### Scenario 4: Formatting

1. Pack context
2. Examine output
3. Verify: Clear structure

---

## Implementation Prompt

### File Structure

```
src/AgenticCoder.Domain/
├── Context/
│   ├── IContextPacker.cs
│   ├── ContextSource.cs
│   ├── ContextChunk.cs
│   └── PackedContext.cs
│
src/AgenticCoder.Infrastructure/
├── Context/
│   ├── ContextPacker.cs
│   ├── Chunker.cs
│   ├── Ranker.cs
│   ├── BudgetManager.cs
│   ├── Deduplicator.cs
│   └── Formatter.cs
```

### IContextPacker Interface

```csharp
namespace AgenticCoder.Domain.Context;

public interface IContextPacker
{
    Task<PackedContext> PackAsync(
        IReadOnlyList<ContextSource> sources,
        ContextBudget budget,
        CancellationToken ct);
}

public sealed record PackedContext(
    string Content,
    int TokenCount,
    IReadOnlyList<ContextChunk> Chunks);
```

### Error Codes

| Code | Meaning |
|------|---------|
| ACODE-CTX-001 | Packing failed |
| ACODE-CTX-002 | Token count error |
| ACODE-CTX-003 | Budget exceeded |
| ACODE-CTX-004 | No content |

### Implementation Checklist

1. [ ] Create context interfaces
2. [ ] Implement chunker
3. [ ] Implement ranker
4. [ ] Implement budget manager
5. [ ] Implement deduplicator
6. [ ] Implement formatter
7. [ ] Create packer orchestrator
8. [ ] Write tests

### Rollout Plan

1. **Phase 1:** Chunking
2. **Phase 2:** Ranking
3. **Phase 3:** Budgeting
4. **Phase 4:** Deduplication
5. **Phase 5:** Formatting

---

**End of Task 016 Specification**