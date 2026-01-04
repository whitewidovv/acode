# Task 016.a: Chunking Rules

**Priority:** P0 – Critical  
**Tier:** S – Core Infrastructure  
**Complexity:** 8 (Fibonacci points)  
**Phase:** Phase 3 – Intelligence Layer  
**Dependencies:** Task 016 (Context Packer)  

---

## Description

### Business Value

Chunking is the foundation of effective context assembly for Large Language Models. When the agent needs to understand code, it cannot simply include entire files—many files exceed token limits, and including irrelevant sections wastes valuable context space. Task 016.a delivers the chunking intelligence that transforms raw files into meaningful, appropriately-sized pieces that LLMs can process effectively.

The quality of chunks directly impacts agent performance. Poorly chunked code splits functions in half, separates method signatures from their bodies, or breaks up related logic. These fragmented chunks confuse the LLM and degrade response quality. Well-designed chunking respects code structure, preserves semantic units, and maintains enough context for the LLM to understand each piece independently.

Language-specific chunking provides significant advantages over naive approaches. By parsing C# with Roslyn and TypeScript with the compiler API, the chunker understands actual code structure rather than just counting lines. This structural awareness enables intelligent decisions—keeping a small method together rather than splitting it, or separating unrelated classes into distinct chunks. The result is higher-quality context that improves agent accuracy.

### Scope

This task defines the complete chunking subsystem for the Context Packer:

1. **Structural Chunking Engine:** The core system that respects code boundaries (classes, methods, functions) when dividing files into chunks.

2. **Language-Specific Parsers:** Dedicated parsers for C#, TypeScript, and JavaScript that understand each language's structure and produce optimal chunks.

3. **Line-Based Fallback:** A universal fallback chunker for unsupported file types that chunks by line count with configurable overlap.

4. **Token Estimation:** Accurate token counting to ensure chunks fit within LLM token limits.

5. **Chunk Metadata System:** Tracking of source file, line ranges, token estimates, chunk type, and structural hierarchy for each chunk.

### Integration Points

| Component | Integration Type | Description |
|-----------|------------------|-------------|
| Task 016 (Context Packer) | Parent System | Chunker is invoked by Context Packer to break files into pieces |
| Task 016.b (Ranking) | Downstream | Chunks are passed to ranking system for prioritization |
| Task 016.c (Budgeting) | Downstream | Token estimates used for budget allocation |
| Task 014 (RepoFS) | File Access | Reads file content via RepoFS abstraction |
| Task 002 (Config) | Configuration | Chunk settings loaded from `.agent/config.yml` |
| Task 015 (Indexing) | Index Storage | Chunks may be cached in index for performance |

### Failure Modes

| Failure | Impact | Mitigation |
|---------|--------|------------|
| Parse error in source code | Cannot use structural chunking | Automatic fallback to line-based chunking |
| File too large for memory | Out of memory exception | Progressive chunking with streaming, memory limits |
| Unsupported file type | No structural parser available | Graceful fallback to line-based chunking |
| Token estimation inaccuracy | Chunks exceed budget | Conservative estimation with safety margin |
| Malformed unicode content | Parsing/encoding errors | Robust encoding detection, UTF-8 fallback |
| Circular includes or dependencies | Infinite loop risk | Detection and termination safeguards |
| Empty or trivial files | Wasted processing | Skip files below minimum size threshold |
| Binary file misidentified as text | Garbled chunks | Binary detection before chunking |

### Assumptions

1. Source files are predominantly text with UTF-8 encoding
2. C# files are syntactically valid or recoverable by Roslyn
3. TypeScript/JavaScript files are parseable by the TypeScript compiler
4. Files are reasonably sized (< 10MB) and fit in memory for parsing
5. The target LLM tokenizer is known for accurate token estimation
6. Chunk configuration values are validated at startup
7. Line-based chunking is an acceptable fallback for any file type
8. Overlap configuration is reasonable (not exceeding chunk size)

### Security Considerations

1. **Input Validation:** All file content must be validated before parsing to prevent parser exploits or resource exhaustion attacks.

2. **Memory Limits:** Chunking must enforce memory limits to prevent denial-of-service via extremely large files.

3. **Path Sanitization:** File paths in chunk metadata must be sanitized to prevent information leakage.

4. **No Code Execution:** Parsing must never execute code from the files being chunked.

5. **Resource Cleanup:** Parser resources must be properly disposed to prevent resource leaks.

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

### Structural Chunking (FR-016a-01 to FR-016a-05)

| ID | Requirement |
|----|-------------|
| FR-016a-01 | System MUST detect class boundaries in source files |
| FR-016a-02 | System MUST detect method boundaries in source files |
| FR-016a-03 | System MUST detect function boundaries in source files |
| FR-016a-04 | System MUST detect block boundaries (if, for, while) |
| FR-016a-05 | System MUST preserve structural integrity when chunking |

### C# Chunking (FR-016a-06 to FR-016a-10)

| ID | Requirement |
|----|-------------|
| FR-016a-06 | System MUST parse C# files using Roslyn |
| FR-016a-07 | System MUST chunk C# files by namespace when configured |
| FR-016a-08 | System MUST chunk C# files by class when configured |
| FR-016a-09 | System MUST chunk C# files by method when configured |
| FR-016a-10 | System MUST handle nested types in C# files |

### TypeScript/JavaScript Chunking (FR-016a-11 to FR-016a-15)

| ID | Requirement |
|----|-------------|
| FR-016a-11 | System MUST parse TypeScript files using TypeScript compiler API |
| FR-016a-12 | System MUST chunk TypeScript files by module |
| FR-016a-13 | System MUST chunk TypeScript/JavaScript files by class |
| FR-016a-14 | System MUST chunk TypeScript/JavaScript files by function |
| FR-016a-15 | System MUST handle export statements in chunking |

### Line-Based Chunking (FR-016a-16 to FR-016a-20)

| ID | Requirement |
|----|-------------|
| FR-016a-16 | System MUST provide line-based chunking as fallback |
| FR-016a-17 | Line count per chunk MUST be configurable |
| FR-016a-18 | System MUST support overlap between adjacent chunks |
| FR-016a-19 | Overlap line count MUST be configurable |
| FR-016a-20 | System MUST respect line boundaries (no mid-line splits) |

### Token-Based Chunking (FR-016a-21 to FR-016a-24)

| ID | Requirement |
|----|-------------|
| FR-016a-21 | System MUST estimate token count for each chunk |
| FR-016a-22 | System MUST enforce maximum token limit per chunk |
| FR-016a-23 | System MUST enforce minimum token limit per chunk |
| FR-016a-24 | System MUST balance chunk sizes within configured limits |

### Strategy Selection (FR-016a-25 to FR-016a-28)

| ID | Requirement |
|----|-------------|
| FR-016a-25 | System MUST detect file type from extension and content |
| FR-016a-26 | System MUST select appropriate chunking strategy for file type |
| FR-016a-27 | System MUST fall back to line-based when parsing fails |
| FR-016a-28 | Chunking strategy MUST be configurable per file type |

### Chunk Metadata (FR-016a-29 to FR-016a-33)

| ID | Requirement |
|----|-------------|
| FR-016a-29 | Each chunk MUST include source file path |
| FR-016a-30 | Each chunk MUST include start and end line numbers |
| FR-016a-31 | Each chunk MUST include token count estimate |
| FR-016a-32 | Each chunk MUST include chunk type (structural, line-based) |
| FR-016a-33 | Each chunk MUST include hierarchy path (namespace, class, method) |

### Overlap Handling (FR-016a-34 to FR-016a-36)

| ID | Requirement |
|----|-------------|
| FR-016a-34 | Overlap line count MUST be configurable |
| FR-016a-35 | Overlap MUST preserve context at chunk boundaries |
| FR-016a-36 | Overlap MUST be compatible with deduplication (Task 016.c) |

### Large File Handling (FR-016a-37 to FR-016a-39)

| ID | Requirement |
|----|-------------|
| FR-016a-37 | System MUST handle files larger than memory limits |
| FR-016a-38 | System MUST use progressive/streaming chunking for large files |
| FR-016a-39 | System MUST minimize memory usage during chunking |

---

## Non-Functional Requirements

### Performance (NFR-016a-01 to NFR-016a-03)

| ID | Category | Requirement |
|----|----------|-------------|
| NFR-016a-01 | Performance | System MUST chunk 10KB file in less than 50ms |
| NFR-016a-02 | Performance | System MUST chunk 100KB file in less than 200ms |
| NFR-016a-03 | Performance | Memory usage MUST NOT exceed 2x file size during chunking |

### Quality (NFR-016a-04 to NFR-016a-06)

| ID | Category | Requirement |
|----|----------|-------------|
| NFR-016a-04 | Quality | Chunks MUST be semantically meaningful for LLM consumption |
| NFR-016a-05 | Quality | Chunk sizes MUST be consistent within configured tolerances |
| NFR-016a-06 | Quality | Chunk boundaries MUST align with logical code boundaries |

### Reliability (NFR-016a-07 to NFR-016a-09)

| ID | Category | Requirement |
|----|----------|-------------|
| NFR-016a-07 | Reliability | System MUST handle malformed or syntactically invalid files |
| NFR-016a-08 | Reliability | System MUST gracefully fall back to line-based on parse errors |
| NFR-016a-09 | Reliability | No file content MUST be lost during chunking operations |

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

## Best Practices

### Chunking Strategy

1. **Prefer semantic boundaries** - Split at function/class boundaries, not arbitrary lines
2. **Respect language syntax** - Don't break mid-statement or mid-block
3. **Include leading context** - Imports, namespace declarations for understanding
4. **Add trailing context** - Closing braces, related code if space permits

### Size Management

5. **Configurable chunk sizes** - Different contexts need different granularity
6. **Overlap for continuity** - Include few lines overlap between chunks
7. **Handle edge cases** - Very long lines, minified code, binary files
8. **Track source metadata** - Preserve file, line numbers through chunking

### Quality Assurance

9. **Validate chunk boundaries** - Verify syntax is valid after chunking
10. **Test language coverage** - Ensure rules work for all supported languages
11. **Measure chunk quality** - Are chunks useful in context? Test with LLM
12. **Log chunking decisions** - Record why boundaries were chosen

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