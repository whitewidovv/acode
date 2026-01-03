# Task 016.b: Ranking Rules

**Priority:** P0 – Critical  
**Tier:** S – Core Infrastructure  
**Complexity:** 8 (Fibonacci points)  
**Phase:** Phase 3 – Intelligence Layer  
**Dependencies:** Task 016 (Context Packer), Task 016.a (Chunking)  

---

## Description

Task 016.b defines the ranking rules for the Context Packer. Ranking determines which chunks are most important. Higher-ranked chunks are included first in context.

Ranking is multi-factor. No single factor determines rank. Relevance, recency, source priority, and other factors combine.

Relevance is the primary factor. How well does the chunk match the current task? Search score indicates relevance. Keyword overlap matters.

Source priority favors certain origins. Tool results often most relevant. Open files are directly referenced. Search results vary in quality.

Recency considers time factors. Recently modified files may be more relevant. Recently viewed files are likely important.

Weights are configurable. Different use cases need different balances. Search-heavy tasks weight relevance higher. Exploration tasks weight diversity higher.

Combined scoring produces final rank. Normalize each factor. Apply weights. Sum to final score. Sort descending.

Tie-breaking ensures determinism. When scores are equal, use consistent ordering. File path is the final tiebreaker.

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

### Relevance Scoring

- FR-001: Score by search relevance
- FR-002: Score by keyword match
- FR-003: Score by query overlap
- FR-004: Normalize 0-1

### Source Scoring

- FR-005: Assign source priority
- FR-006: Tool results: high
- FR-007: Open files: medium-high
- FR-008: Search results: medium
- FR-009: References: low
- FR-010: Configurable priorities

### Recency Scoring

- FR-011: Score by modification time
- FR-012: Score by access time
- FR-013: Apply time decay
- FR-014: Configurable decay

### Position Scoring

- FR-015: Score by file position
- FR-016: Top of file: boost
- FR-017: Related code: boost

### Combined Scoring

- FR-018: Apply weights to factors
- FR-019: Sum weighted scores
- FR-020: Normalize final score
- FR-021: Configurable weights

### Tie-Breaking

- FR-022: Primary: score
- FR-023: Secondary: source priority
- FR-024: Tertiary: file path
- FR-025: Deterministic order

### Filtering

- FR-026: Minimum score threshold
- FR-027: Exclude below threshold
- FR-028: Configurable threshold

### Boosting

- FR-029: Boost specific paths
- FR-030: Boost specific types
- FR-031: Temporary boosts

### Penalties

- FR-032: Penalize test files
- FR-033: Penalize generated files
- FR-034: Configurable penalties

---

## Non-Functional Requirements

### Performance

- NFR-001: Rank 1000 chunks < 50ms
- NFR-002: Score computation < 1ms each
- NFR-003: Sort < 10ms

### Quality

- NFR-004: Consistent ranking
- NFR-005: Intuitive results
- NFR-006: Configurable behavior

### Reliability

- NFR-007: Handle missing factors
- NFR-008: Default values
- NFR-009: No crashes

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

## Testing Requirements

### Unit Tests

```
Tests/Unit/Context/Ranking/
├── RelevanceScorerTests.cs
│   ├── Should_Score_Search_Results()
│   ├── Should_Score_Keywords()
│   └── Should_Normalize()
│
├── SourceScorerTests.cs
│   ├── Should_Assign_Priorities()
│   └── Should_Be_Configurable()
│
├── RecencyScorerTests.cs
│   ├── Should_Score_By_Time()
│   └── Should_Apply_Decay()
│
├── CombinedRankerTests.cs
│   ├── Should_Apply_Weights()
│   ├── Should_Sum_Correctly()
│   └── Should_Handle_Ties()
│
└── BoostPenaltyTests.cs
    ├── Should_Apply_Boosts()
    └── Should_Apply_Penalties()
```

### Integration Tests

```
Tests/Integration/Context/Ranking/
├── RankingIntegrationTests.cs
│   └── Should_Rank_Real_Chunks()
```

### E2E Tests

```
Tests/E2E/Context/Ranking/
├── RankingE2ETests.cs
│   └── Should_Rank_For_Agent()
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