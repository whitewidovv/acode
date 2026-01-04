# Task 016.b: Ranking Rules

**Priority:** P0 – Critical  
**Tier:** S – Core Infrastructure  
**Complexity:** 8 (Fibonacci points)  
**Phase:** Phase 3 – Intelligence Layer  
**Dependencies:** Task 016 (Context Packer), Task 016.a (Chunking)  

---

## Description

### Business Value

Ranking is the intelligence that determines which code chunks matter most for the agent's current task. With potentially thousands of chunks across a codebase, the agent cannot include everything in its context window. Task 016.b delivers the prioritization logic that ensures the most relevant, useful chunks appear first—maximizing the value of every token spent on context.

Effective ranking directly impacts agent response quality. When the agent is asked to modify a function, the ranking system must surface that function's code, its callers, its tests, and related interfaces—in priority order. Poor ranking buries critical context below less relevant code, forcing the LLM to work with incomplete information and producing lower-quality responses.

The multi-factor ranking approach recognizes that "relevance" is complex. A chunk might be relevant because it matched a search query, because the user has the file open, because it was recently modified, or because it's in a critical path of the codebase. By combining these factors with configurable weights, the ranking system adapts to different workflows and use cases.

### Scope

This task defines the complete ranking subsystem for the Context Packer:

1. **Relevance Scoring:** Calculate how well each chunk matches the current query or task based on search scores and keyword overlap.

2. **Source Priority Scoring:** Prioritize chunks based on their origin—tool results rank higher than search results, which rank higher than indirect references.

3. **Recency Scoring:** Factor in how recently files were modified or accessed, with configurable time decay.

4. **Position Scoring:** Boost chunks based on their position in files (headers, class declarations) and relationship to other selected chunks.

5. **Combined Ranking Engine:** Weighted combination of all factors producing a final score for deterministic ordering.

### Integration Points

| Component | Integration Type | Description |
|-----------|------------------|-------------|
| Task 016 (Context Packer) | Parent System | Ranker is invoked by Context Packer to prioritize chunks |
| Task 016.a (Chunking) | Upstream | Receives chunks with metadata for ranking |
| Task 016.c (Budgeting) | Downstream | Ranked chunks passed to budget selector |
| Task 002 (Config) | Configuration | Ranking weights and settings from `.agent/config.yml` |
| Task 015 (Indexing) | Metadata Source | File modification times and access patterns from index |
| Task 011 (Session) | Context Source | Current task context for relevance scoring |

### Failure Modes

| Failure | Impact | Mitigation |
|---------|--------|------------|
| Missing relevance signal | Cannot score by relevance | Default to neutral score (0.5), rely on other factors |
| Invalid weight configuration | Weights don't sum to 1.0 | Normalize weights automatically, log warning |
| Missing file metadata | Cannot score recency | Default to neutral score, warn in debug output |
| Pattern matching error | Boost/penalty not applied | Validate patterns at config load, skip invalid patterns |
| Tie-breaking inconsistency | Non-deterministic order | Strict tie-breaking rules: score → source → path → line |
| Score overflow | Numeric instability | Clamp scores to 0-1 range after all adjustments |
| Empty chunk set | No results to rank | Return empty result immediately, no error |
| Circular boost/penalty | Unexpected score behavior | Apply boosts and penalties in single pass only |

### Assumptions

1. All chunks have associated metadata (source, path, line numbers)
2. Search scores, when present, are normalized to 0-1 range
3. File modification times are available from the file system or index
4. Boost and penalty patterns use standard glob syntax
5. Weight configuration is validated at startup
6. The ranking algorithm is deterministic for identical inputs
7. Performance is acceptable for up to 10,000 chunks
8. Debug output is available for ranking troubleshooting

### Security Considerations

1. **Path Pattern Safety:** Boost and penalty patterns must be validated to prevent regex denial-of-service attacks.

2. **Information Leakage:** Ranking debug output must not expose sensitive file paths outside the repository.

3. **Configuration Tampering:** Ranking weights should be validated to prevent malicious configurations that could prioritize dangerous code.

4. **Deterministic Output:** Ranking must be deterministic to enable auditing and reproducibility.

5. **No External Calls:** Ranking must be purely computational with no external service dependencies.

---

## Glossary / Terms

| Term | Definition |
|------|------------|
| Ranking | Priority ordering |
| Relevance | Task match quality |
| Recency | How recent |
| Source Priority | Origin importance |
| Weight | Factor multiplier |
| Combined Score | Final rank value |
| Normalization | Scale 0-1 |
| Tie-Breaking | Equal score handling |
| Diversity | Variety in results |
| Boost | Temporary increase |
| Penalty | Temporary decrease |
| Factor | Scoring component |
| Aggregate | Combined value |
| Decay | Reduce over time |
| Threshold | Minimum score |

---

## Out of Scope

The following items are explicitly excluded from Task 016.b:

- **ML-based ranking** - Rule-based only
- **Personalization** - Same rules for all
- **Learning from feedback** - Static rules
- **Semantic similarity** - Keyword-based
- **User preferences** - Config only

---

## Functional Requirements

### Relevance Scoring (FR-016b-01 to FR-016b-04)

| ID | Requirement |
|----|-------------|
| FR-016b-01 | System MUST score chunks by search relevance when available |
| FR-016b-02 | System MUST score chunks by keyword match against current query |
| FR-016b-03 | System MUST score chunks by query term overlap |
| FR-016b-04 | Relevance scores MUST be normalized to 0-1 range |

### Source Scoring (FR-016b-05 to FR-016b-10)

| ID | Requirement |
|----|-------------|
| FR-016b-05 | System MUST assign priority score based on chunk source |
| FR-016b-06 | Tool results MUST receive high source priority (default: 100) |
| FR-016b-07 | Open files MUST receive medium-high source priority (default: 80) |
| FR-016b-08 | Search results MUST receive medium source priority (default: 60) |
| FR-016b-09 | References MUST receive low source priority (default: 40) |
| FR-016b-10 | Source priorities MUST be configurable |

### Recency Scoring (FR-016b-11 to FR-016b-14)

| ID | Requirement |
|----|-------------|
| FR-016b-11 | System MUST score chunks by file modification time |
| FR-016b-12 | System MUST score chunks by file access time when available |
| FR-016b-13 | System MUST apply time decay function to recency scores |
| FR-016b-14 | Decay rate MUST be configurable (hours until score halves) |

### Position Scoring (FR-016b-15 to FR-016b-17)

| ID | Requirement |
|----|-------------|
| FR-016b-15 | System MUST score chunks by position within file |
| FR-016b-16 | Top-of-file content (imports, class headers) MUST receive position boost |
| FR-016b-17 | Code related to other selected chunks MUST receive boost |

### Combined Scoring (FR-016b-18 to FR-016b-21)

| ID | Requirement |
|----|-------------|
| FR-016b-18 | System MUST apply configurable weights to each scoring factor |
| FR-016b-19 | System MUST sum weighted scores to produce final score |
| FR-016b-20 | Final scores MUST be normalized to 0-1 range |
| FR-016b-21 | All weights MUST be configurable in configuration file |

### Tie-Breaking (FR-016b-22 to FR-016b-25)

| ID | Requirement |
|----|-------------|
| FR-016b-22 | Primary sort MUST be by final combined score (descending) |
| FR-016b-23 | Secondary sort MUST be by source priority (descending) |
| FR-016b-24 | Tertiary sort MUST be by file path (alphabetical) |
| FR-016b-25 | Ranking MUST be deterministic for identical inputs |

### Filtering (FR-016b-26 to FR-016b-28)

| ID | Requirement |
|----|-------------|
| FR-016b-26 | System MUST support minimum score threshold |
| FR-016b-27 | Chunks below threshold MUST be excluded from results |
| FR-016b-28 | Minimum score threshold MUST be configurable |

### Boosting (FR-016b-29 to FR-016b-31)

| ID | Requirement |
|----|-------------|
| FR-016b-29 | System MUST support score boost for specific path patterns |
| FR-016b-30 | System MUST support score boost for specific file types |
| FR-016b-31 | System MUST support temporary session-level boosts |

### Penalties (FR-016b-32 to FR-016b-34)

| ID | Requirement |
|----|-------------|
| FR-016b-32 | System MUST apply score penalty for test file paths |
| FR-016b-33 | System MUST apply score penalty for generated file paths |
| FR-016b-34 | Penalty patterns and factors MUST be configurable |

---

## Non-Functional Requirements

### Performance (NFR-016b-01 to NFR-016b-03)

| ID | Category | Requirement |
|----|----------|-------------|
| NFR-016b-01 | Performance | System MUST rank 1000 chunks in less than 50ms |
| NFR-016b-02 | Performance | Individual score computation MUST complete in less than 1ms |
| NFR-016b-03 | Performance | Sorting MUST complete in less than 10ms for 1000 chunks |

### Quality (NFR-016b-04 to NFR-016b-06)

| ID | Category | Requirement |
|----|----------|-------------|
| NFR-016b-04 | Quality | Ranking MUST be consistent across identical inputs |
| NFR-016b-05 | Quality | Ranking results MUST be intuitive for developers |
| NFR-016b-06 | Quality | Ranking behavior MUST be configurable for different use cases |

### Reliability (NFR-016b-07 to NFR-016b-09)

| ID | Category | Requirement |
|----|----------|-------------|
| NFR-016b-07 | Reliability | System MUST handle missing scoring factors gracefully |
| NFR-016b-08 | Reliability | System MUST use sensible default values for missing data |
| NFR-016b-09 | Reliability | System MUST NOT crash on invalid input data |

---

## User Manual Documentation

### Overview

Ranking rules determine which chunks appear first in context. Better ranking means better context quality.

### Configuration

```yaml
# .agent/config.yml
context:
  ranking:
    # Factor weights (must sum to 1.0)
    weights:
      relevance: 0.50
      source: 0.25
      recency: 0.15
      position: 0.10
      
    # Source priorities (0-100)
    source_priority:
      tool_result: 100
      open_file: 80
      search_result: 60
      reference: 40
      
    # Time decay for recency (hours)
    recency_decay_hours: 24
    
    # Minimum score threshold (0-1)
    min_score: 0.1
    
    # Path boosts
    boosts:
      - pattern: "src/core/**"
        factor: 1.2
      - pattern: "*Service*.cs"
        factor: 1.1
        
    # Path penalties
    penalties:
      - pattern: "**/tests/**"
        factor: 0.8
      - pattern: "*.generated.cs"
        factor: 0.5
```

### Ranking Factors

#### Relevance (default: 50%)

How well the chunk matches the current task:
- Search score if from search
- Keyword overlap with query
- Direct reference from other code

#### Source Priority (default: 25%)

Where the chunk came from:
- Tool results: 100 (directly requested)
- Open files: 80 (user is viewing)
- Search results: 60 (query matched)
- References: 40 (indirectly related)

#### Recency (default: 15%)

How recently the file was touched:
- Modified today: 1.0
- Modified yesterday: 0.9
- Modified last week: 0.7
- Older: decays further

#### Position (default: 10%)

Where in the file:
- Top (imports, class header): slight boost
- Related to other chunks: boost

### Debugging Ranking

```bash
$ acode context debug-ranking

Ranking Debug
────────────────────
Top 10 chunks:

1. UserService.cs:25-50 (score: 0.92)
   Relevance: 0.95 × 0.50 = 0.475
   Source: 1.00 × 0.25 = 0.250
   Recency: 0.90 × 0.15 = 0.135
   Position: 0.60 × 0.10 = 0.060
   
2. IUserService.cs:1-20 (score: 0.85)
   Relevance: 0.90 × 0.50 = 0.450
   Source: 0.80 × 0.25 = 0.200
   ...
```

### Troubleshooting

#### Wrong Chunks Selected

**Problem:** Irrelevant code ranked higher

**Solutions:**
1. Increase relevance weight
2. Decrease source weight
3. Add penalties for irrelevant paths

#### Stale Code Selected

**Problem:** Old code ranked over new

**Solutions:**
1. Increase recency weight
2. Decrease recency decay
3. Manually update index

#### Test Code Included

**Problem:** Test files in context

**Solutions:**
1. Add test path penalty
2. Set penalty factor low (0.3)
3. Increase min_score threshold

---

## Acceptance Criteria

### Relevance

- [ ] AC-001: Search score used
- [ ] AC-002: Keyword match works
- [ ] AC-003: Normalized correctly

### Source

- [ ] AC-004: Priorities assigned
- [ ] AC-005: Configurable
- [ ] AC-006: Applied correctly

### Recency

- [ ] AC-007: Time factored
- [ ] AC-008: Decay works
- [ ] AC-009: Configurable

### Combined

- [ ] AC-010: Weights applied
- [ ] AC-011: Sum calculated
- [ ] AC-012: Normalized

### Tie-Breaking

- [ ] AC-013: Deterministic
- [ ] AC-014: Consistent order

### Boost/Penalty

- [ ] AC-015: Boosts applied
- [ ] AC-016: Penalties applied
- [ ] AC-017: Pattern matching

---

## Best Practices

### Relevance Scoring

1. **Multiple signals** - Combine text similarity, recency, structure signals
2. **Weight by confidence** - Strong signals count more than weak
3. **Normalize scores** - Put all signals on comparable scale before combining
4. **Explain rankings** - Log why each result scored as it did

### Signal Types

5. **Lexical similarity** - Text matching, keyword frequency
6. **Structural proximity** - Same file, same class, same namespace
7. **Edit recency** - Recently modified files may be more relevant
8. **Usage patterns** - Files edited together tend to be related

### Tuning

9. **Configurable weights** - Allow adjustment of signal weights
10. **Test with real queries** - Validate ranking quality with example searches
11. **Collect feedback** - Was selected context useful? Learn from outcomes
12. **A/B test changes** - Compare ranking quality before/after changes

---

## Testing Requirements

### Unit Tests

```
Tests/Unit/Context/Ranking/
├── RelevanceScorerTests.cs
│   ├── Should_Score_Search_Result()
│   ├── Should_Score_High_Relevance()
│   ├── Should_Score_Low_Relevance()
│   ├── Should_Score_Keyword_Match()
│   ├── Should_Score_Multiple_Keywords()
│   ├── Should_Normalize_To_Zero_One()
│   ├── Should_Handle_No_Relevance_Signal()
│   └── Should_Handle_Empty_Query()
│
├── SourceScorerTests.cs
│   ├── Should_Score_Tool_Result()
│   ├── Should_Score_Open_File()
│   ├── Should_Score_Search_Result()
│   ├── Should_Score_Reference()
│   ├── Should_Load_Config_Priorities()
│   ├── Should_Handle_Unknown_Source()
│   └── Should_Normalize_To_Zero_One()
│
├── RecencyScorerTests.cs
│   ├── Should_Score_Today()
│   ├── Should_Score_Yesterday()
│   ├── Should_Score_Last_Week()
│   ├── Should_Score_Last_Month()
│   ├── Should_Score_Older()
│   ├── Should_Apply_Decay_Function()
│   ├── Should_Load_Config_Decay()
│   ├── Should_Handle_Missing_Date()
│   └── Should_Normalize_To_Zero_One()
│
├── PositionScorerTests.cs
│   ├── Should_Boost_Top_Of_File()
│   ├── Should_Boost_Class_Header()
│   ├── Should_Boost_Related_To_Other_Chunks()
│   ├── Should_Handle_Middle_Of_File()
│   └── Should_Normalize_To_Zero_One()
│
├── CombinedRankerTests.cs
│   ├── Should_Apply_Default_Weights()
│   ├── Should_Apply_Custom_Weights()
│   ├── Should_Sum_Weighted_Scores()
│   ├── Should_Normalize_Final_Score()
│   ├── Should_Handle_Zero_Weight()
│   ├── Should_Handle_Single_Factor()
│   ├── Should_Sort_By_Score_Descending()
│   ├── Should_Handle_Tie_Deterministically()
│   ├── Should_Apply_Min_Score_Threshold()
│   └── Should_Return_Ranking_Factors()
│
├── BoostPenaltyTests.cs
│   ├── Should_Apply_Path_Boost()
│   ├── Should_Apply_Pattern_Boost()
│   ├── Should_Apply_Multiple_Boosts()
│   ├── Should_Apply_Path_Penalty()
│   ├── Should_Apply_Pattern_Penalty()
│   ├── Should_Apply_Multiple_Penalties()
│   ├── Should_Stack_Boosts_And_Penalties()
│   ├── Should_Match_Glob_Patterns()
│   ├── Should_Handle_No_Match()
│   └── Should_Cap_Final_Score()
│
└── TieBreakingTests.cs
    ├── Should_Break_By_Source_Priority()
    ├── Should_Break_By_Path_Alpha()
    ├── Should_Break_By_Line_Number()
    └── Should_Be_Deterministic()
```

### Integration Tests

```
Tests/Integration/Context/Ranking/
├── RankingIntegrationTests.cs
│   ├── Should_Rank_Real_Chunks()
│   ├── Should_Rank_Large_Chunk_Set()
│   ├── Should_Apply_Config_Settings()
│   └── Should_Produce_Stable_Ranking()
│
└── RankingDebugIntegrationTests.cs
    ├── Should_Output_Debug_Info()
    └── Should_Show_Factor_Breakdown()
```

### E2E Tests

```
Tests/E2E/Context/Ranking/
├── RankingE2ETests.cs
│   ├── Should_Rank_For_Agent_Context()
│   ├── Should_Debug_Via_CLI()
│   └── Should_Respect_Config_Changes()
```

### Performance Benchmarks

| Benchmark | Target | Maximum |
|-----------|--------|---------|
| Rank 100 chunks | 5ms | 10ms |
| Rank 1000 chunks | 25ms | 50ms |
| Score computation | 0.5ms | 1ms |

---

## User Verification Steps

### Scenario 1: Relevance

1. Search for specific term
2. Check ranking
3. Verify: Relevant chunks first

### Scenario 2: Source Priority

1. Include tool result and search
2. Check ranking
3. Verify: Tool result ranks higher

### Scenario 3: Boost

1. Configure path boost
2. Rank chunks
3. Verify: Boosted path higher

### Scenario 4: Penalty

1. Configure test penalty
2. Rank chunks
3. Verify: Test files lower

---

## Implementation Prompt

### File Structure

```
src/AgenticCoder.Domain/
├── Context/
│   └── IRanker.cs
│
src/AgenticCoder.Infrastructure/
├── Context/
│   └── Ranking/
│       ├── ChunkRanker.cs
│       ├── RelevanceScorer.cs
│       ├── SourceScorer.cs
│       ├── RecencyScorer.cs
│       └── BoostPenaltyApplicator.cs
```

### IRanker Interface

```csharp
namespace AgenticCoder.Domain.Context;

public interface IRanker
{
    IReadOnlyList<RankedChunk> Rank(
        IReadOnlyList<ContentChunk> chunks,
        RankingContext context,
        RankingOptions options);
}

public sealed record RankedChunk(
    ContentChunk Chunk,
    double Score,
    RankingFactors Factors);
```

### Error Codes

| Code | Meaning |
|------|---------|
| ACODE-RNK-001 | Scoring failed |
| ACODE-RNK-002 | Invalid weights |
| ACODE-RNK-003 | Pattern error |

### Implementation Checklist

1. [ ] Create ranker interface
2. [ ] Implement relevance scorer
3. [ ] Implement source scorer
4. [ ] Implement recency scorer
5. [ ] Implement combined ranker
6. [ ] Add boost/penalty
7. [ ] Add tie-breaking
8. [ ] Write tests

### Rollout Plan

1. **Phase 1:** Individual scorers
2. **Phase 2:** Combined ranking
3. **Phase 3:** Boost/penalty
4. **Phase 4:** Configuration
5. **Phase 5:** Debug tooling

---

**End of Task 016.b Specification**