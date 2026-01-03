# Task 016.a: Chunking Rules

**Priority:** P0 – Critical  
**Tier:** S – Core Infrastructure  
**Complexity:** 8 (Fibonacci points)  
**Phase:** Phase 3 – Intelligence Layer  
**Dependencies:** Task 016 (Context Packer)  

---

## Description

Task 016.a defines the chunking rules for the Context Packer. Chunking breaks files into meaningful pieces. Good chunks preserve context while fitting token budgets.

Files vary in size. Some are small enough to include whole. Others are thousands of lines. Chunking handles both cases appropriately.

Structural chunking respects code boundaries. Chunk at class boundaries. Chunk at function boundaries. Don't split a function in half.

Line-based chunking is the fallback. When structure isn't clear, chunk by line count. Overlap ensures context at boundaries.

Token-aware chunking considers LLM limits. Estimate tokens per chunk. Ensure chunks fit within max limits. Avoid oversized chunks.

Language-specific chunking uses parsing. C# uses Roslyn. TypeScript uses the compiler API. Generic files use line-based.

Chunk quality matters for LLM understanding. A chunk should be self-contained when possible. It should have enough context to be understandable.

Overlap between chunks helps with boundary cases. When searching finds a match at chunk boundary, overlap ensures context is available.

Chunk metadata enables later processing. Track source file. Track line ranges. Track token estimates. Track chunk type.

---

## Glossary / Terms

| Term | Definition |
|------|------------|
| Chunk | Content piece |
| Structural | Respects code boundaries |
| Line-Based | Fixed line count |
| Token-Based | Fixed token count |
| Overlap | Shared content at edges |
| Boundary | Chunk edge |
| Parser | Code structure analyzer |
| AST | Abstract Syntax Tree |
| Self-Contained | Understandable alone |
| Fallback | Alternative strategy |
| Max Size | Upper limit |
| Min Size | Lower limit |
| Metadata | Chunk information |
| Strategy | Chunking approach |
| Hierarchy | Nesting structure |

---

## Out of Scope

The following items are explicitly excluded from Task 016.a:

- **Semantic chunking** - Structure only
- **ML-based chunking** - Rule-based only
- **Cross-file chunks** - Single file only
- **Streaming chunking** - Batch only
- **Dynamic sizing** - Fixed rules v1

---

## Functional Requirements

### Structural Chunking

- FR-001: Detect class boundaries
- FR-002: Detect method boundaries
- FR-003: Detect function boundaries
- FR-004: Detect block boundaries
- FR-005: Preserve structure

### C# Chunking

- FR-006: Parse with Roslyn
- FR-007: Chunk by namespace
- FR-008: Chunk by class
- FR-009: Chunk by method
- FR-010: Handle nested types

### TypeScript/JavaScript Chunking

- FR-011: Parse with TypeScript
- FR-012: Chunk by module
- FR-013: Chunk by class
- FR-014: Chunk by function
- FR-015: Handle exports

### Line-Based Chunking

- FR-016: Chunk by line count
- FR-017: Configurable line count
- FR-018: Overlap support
- FR-019: Configurable overlap
- FR-020: Respect line boundaries

### Token-Based Chunking

- FR-021: Estimate token count
- FR-022: Enforce max tokens
- FR-023: Enforce min tokens
- FR-024: Balance chunk sizes

### Strategy Selection

- FR-025: Detect file type
- FR-026: Select appropriate strategy
- FR-027: Fall back to line-based
- FR-028: Configurable per type

### Chunk Metadata

- FR-029: Track source file
- FR-030: Track line range
- FR-031: Track token estimate
- FR-032: Track chunk type
- FR-033: Track hierarchy level

### Overlap Handling

- FR-034: Configurable overlap
- FR-035: Context preservation
- FR-036: Dedup-friendly

### Large File Handling

- FR-037: Handle large files
- FR-038: Progressive chunking
- FR-039: Memory efficiency

---

## Non-Functional Requirements

### Performance

- NFR-001: Chunk 10KB file < 50ms
- NFR-002: Chunk 100KB file < 200ms
- NFR-003: Memory < 2x file size

### Quality

- NFR-004: Meaningful chunks
- NFR-005: Consistent sizing
- NFR-006: Good boundaries

### Reliability

- NFR-007: Handle malformed files
- NFR-008: Graceful fallback
- NFR-009: No data loss

---

## User Manual Documentation

### Overview

Chunking rules determine how files are broken into pieces. The right strategy depends on file type and content.

### Configuration

```yaml
# .agent/config.yml
context:
  chunking:
    # Maximum tokens per chunk
    max_tokens: 2000
    
    # Minimum tokens per chunk
    min_tokens: 100
    
    # Prefer structural chunking
    prefer_structural: true
    
    # Line-based fallback settings
    line_based:
      lines_per_chunk: 50
      overlap_lines: 5
      
    # Language-specific settings
    languages:
      csharp:
        chunk_level: method  # class, method, or block
      typescript:
        chunk_level: function
```

### Chunking Strategies

#### Structural Chunking

Best for supported languages. Respects code structure:

```
File: UserService.cs
├── Chunk 1: Using statements + namespace declaration
├── Chunk 2: Class UserService (header + fields)
├── Chunk 3: Constructor
├── Chunk 4: GetUserAsync method
├── Chunk 5: CreateUserAsync method
└── Chunk 6: Remaining methods
```

#### Line-Based Chunking

Fallback for unsupported files:

```
File: config.yaml
├── Chunk 1: Lines 1-50
├── Chunk 2: Lines 46-95 (5 line overlap)
├── Chunk 3: Lines 91-140
└── Chunk 4: Lines 136-180
```

### Chunk Metadata

Each chunk includes:

```json
{
  "source_file": "src/Services/UserService.cs",
  "line_start": 25,
  "line_end": 50,
  "token_estimate": 450,
  "chunk_type": "method",
  "hierarchy": ["namespace:MyApp", "class:UserService", "method:GetUserAsync"]
}
```

### Troubleshooting

#### Chunks Too Large

**Problem:** Chunks exceed token limit

**Solutions:**
1. Reduce max_tokens setting
2. Change chunk_level to smaller units
3. Check for very long methods

#### Chunks Too Small

**Problem:** Many tiny chunks

**Solutions:**
1. Increase min_tokens
2. Change chunk_level to larger units
3. Combine small files

#### Parse Errors

**Problem:** Structural chunking fails

**Solutions:**
1. Falls back to line-based automatically
2. Check file for syntax errors
3. Report issue if valid file fails

---

## Acceptance Criteria

### Structural

- [ ] AC-001: Class boundaries respected
- [ ] AC-002: Method boundaries respected
- [ ] AC-003: Function boundaries respected

### Language Support

- [ ] AC-004: C# chunking works
- [ ] AC-005: TypeScript chunking works
- [ ] AC-006: JavaScript chunking works

### Line-Based

- [ ] AC-007: Line chunking works
- [ ] AC-008: Overlap works
- [ ] AC-009: Configurable size

### Token-Based

- [ ] AC-010: Token limits enforced
- [ ] AC-011: Estimates accurate
- [ ] AC-012: Balance achieved

### Metadata

- [ ] AC-013: Source tracked
- [ ] AC-014: Lines tracked
- [ ] AC-015: Tokens tracked

---

## Testing Requirements

### Unit Tests

```
Tests/Unit/Context/Chunking/
├── StructuralChunkerTests.cs
│   ├── Should_Chunk_By_Class()
│   ├── Should_Chunk_By_Method()
│   ├── Should_Chunk_By_Function()
│   ├── Should_Handle_Nested_Classes()
│   ├── Should_Handle_Nested_Methods()
│   ├── Should_Preserve_Imports()
│   ├── Should_Preserve_Namespace()
│   ├── Should_Respect_Max_Tokens()
│   ├── Should_Respect_Min_Tokens()
│   ├── Should_Split_Large_Methods()
│   ├── Should_Combine_Small_Methods()
│   └── Should_Track_Hierarchy()
│
├── CSharpChunkerTests.cs
│   ├── Should_Parse_Class()
│   ├── Should_Parse_Interface()
│   ├── Should_Parse_Record()
│   ├── Should_Parse_Struct()
│   ├── Should_Parse_Method()
│   ├── Should_Parse_Property()
│   ├── Should_Parse_Constructor()
│   ├── Should_Parse_Lambda()
│   ├── Should_Parse_Local_Function()
│   ├── Should_Handle_Expression_Body()
│   ├── Should_Handle_Partial_Class()
│   ├── Should_Handle_Generics()
│   ├── Should_Handle_Attributes()
│   └── Should_Handle_Documentation()
│
├── TypeScriptChunkerTests.cs
│   ├── Should_Parse_Function()
│   ├── Should_Parse_Arrow_Function()
│   ├── Should_Parse_Class()
│   ├── Should_Parse_Interface()
│   ├── Should_Parse_Type_Alias()
│   ├── Should_Parse_Enum()
│   ├── Should_Parse_Module()
│   ├── Should_Handle_Export_Default()
│   ├── Should_Handle_Named_Export()
│   └── Should_Handle_Decorators()
│
├── JavaScriptChunkerTests.cs
│   ├── Should_Parse_Function()
│   ├── Should_Parse_Arrow_Function()
│   ├── Should_Parse_Class()
│   ├── Should_Parse_Object_Method()
│   ├── Should_Handle_CommonJS()
│   └── Should_Handle_ES_Modules()
│
├── LineBasedChunkerTests.cs
│   ├── Should_Chunk_By_Line_Count()
│   ├── Should_Add_Overlap()
│   ├── Should_Handle_Small_File()
│   ├── Should_Handle_Empty_File()
│   ├── Should_Handle_Single_Line()
│   ├── Should_Respect_Token_Limit()
│   ├── Should_Handle_Long_Lines()
│   └── Should_Preserve_Line_Numbers()
│
├── TokenEstimatorTests.cs
│   ├── Should_Estimate_English_Text()
│   ├── Should_Estimate_Code()
│   ├── Should_Handle_Identifiers()
│   ├── Should_Handle_Unicode()
│   ├── Should_Handle_Whitespace()
│   ├── Should_Match_Model_Tokenizer()
│   └── Should_Cache_Estimates()
│
├── ChunkMetadataTests.cs
│   ├── Should_Track_Source_File()
│   ├── Should_Track_Line_Range()
│   ├── Should_Track_Token_Count()
│   ├── Should_Track_Chunk_Type()
│   └── Should_Track_Hierarchy()
│
└── ChunkerFactoryTests.cs
    ├── Should_Select_CSharp_Chunker()
    ├── Should_Select_TypeScript_Chunker()
    ├── Should_Select_JavaScript_Chunker()
    ├── Should_Fallback_To_LineBasedlm()
    ├── Should_Handle_Unknown_Extension()
    └── Should_Load_Config()
```

### Integration Tests

```
Tests/Integration/Context/Chunking/
├── ChunkingIntegrationTests.cs
│   ├── Should_Chunk_Real_CSharp_File()
│   ├── Should_Chunk_Real_TypeScript_File()
│   ├── Should_Chunk_Real_JavaScript_File()
│   ├── Should_Chunk_Large_File()
│   ├── Should_Handle_Parse_Errors_Gracefully()
│   └── Should_Chunk_Mixed_Content()
│
└── TokenEstimatorIntegrationTests.cs
    ├── Should_Match_GPT4_Tokenizer()
    └── Should_Match_Claude_Tokenizer()
```

### E2E Tests

```
Tests/E2E/Context/Chunking/
├── ChunkingE2ETests.cs
│   ├── Should_Chunk_For_Context_Packer()
│   ├── Should_Work_With_Real_Codebase()
│   └── Should_Respect_Config_Settings()
```

### Performance Benchmarks

| Benchmark | Target | Maximum |
|-----------|--------|---------|
| 10KB file | 25ms | 50ms |
| 100KB file | 100ms | 200ms |
| 1MB file | 500ms | 1000ms |

---

## User Verification Steps

### Scenario 1: Structural

1. Chunk C# file with classes
2. Verify: Class boundaries respected

### Scenario 2: Line-Based

1. Chunk plain text file
2. Verify: Even chunks with overlap

### Scenario 3: Large File

1. Chunk very large file
2. Verify: Handles without OOM

### Scenario 4: Fallback

1. Chunk file with syntax errors
2. Verify: Falls back to line-based

---

## Implementation Prompt

### File Structure

```
src/AgenticCoder.Domain/
├── Context/
│   └── IChunker.cs
│
src/AgenticCoder.Infrastructure/
├── Context/
│   └── Chunking/
│       ├── ChunkerFactory.cs
│       ├── StructuralChunker.cs
│       ├── CSharpChunker.cs
│       ├── TypeScriptChunker.cs
│       ├── LineBasedChunker.cs
│       └── TokenEstimator.cs
```

### IChunker Interface

```csharp
namespace AgenticCoder.Domain.Context;

public interface IChunker
{
    IReadOnlyList<ContentChunk> Chunk(string content, ChunkOptions options);
}

public sealed record ContentChunk(
    string Content,
    int LineStart,
    int LineEnd,
    int TokenEstimate,
    ChunkType Type,
    IReadOnlyList<string> Hierarchy);
```

### Error Codes

| Code | Meaning |
|------|---------|
| ACODE-CHK-001 | Parse failed |
| ACODE-CHK-002 | Chunk too large |
| ACODE-CHK-003 | Memory exceeded |

### Implementation Checklist

1. [ ] Create chunker interface
2. [ ] Implement C# chunker
3. [ ] Implement TypeScript chunker
4. [ ] Implement line-based
5. [ ] Implement token estimation
6. [ ] Create factory
7. [ ] Add metadata
8. [ ] Write tests

### Rollout Plan

1. **Phase 1:** Line-based
2. **Phase 2:** Token estimation
3. **Phase 3:** C# structural
4. **Phase 4:** TypeScript structural
5. **Phase 5:** Factory and fallback

---

**End of Task 016.a Specification**