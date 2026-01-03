# Task 007.c: Truncation + Artifact Attachment Rules

**Priority:** P1 – High Priority  
**Tier:** Core Infrastructure  
**Complexity:** 8 (Fibonacci points)  
**Phase:** Foundation  
**Dependencies:** Task 007, Task 004.a (ToolResult types), Task 004.b (Response types), Task 001  

---

## Description

Task 007.c defines the rules for truncating large tool results and attaching artifacts to conversations. Tool execution often produces outputs that exceed practical limits—file contents, command output, search results, or log data. Without intelligent truncation, these outputs consume context window, degrade model performance, or exceed API limits. This task establishes consistent truncation policies and artifact handling.

Tool results vary dramatically in size. Reading a small config file produces bytes; listing a large directory produces megabytes; searching a codebase produces unbounded output. The model's context window is finite (typically 8K-128K tokens). Tool results compete with conversation history, system prompts, and model reasoning for this limited space. Truncation ensures tool results fit within budget.

Truncation is not simply cutting off text at a limit. Naive truncation destroys information and confuses models. Smart truncation preserves structure, shows representative samples, and indicates what was omitted. The truncation strategy varies by content type: code preserves syntax boundaries, logs preserve line boundaries, structured data preserves element boundaries.

Artifact attachment provides an alternative to inline tool results. Instead of including large content in the conversation, an artifact reference is created. The model sees a summary with artifact ID; the full content is available through a separate channel. This pattern separates "what the model needs to reason about" from "full data for reference."

Different tool types require different truncation strategies. File reads may preserve head and tail with middle omission. Command output may preserve last N lines (most recent). Search results may preserve top N matches. Directory listings may show structure at limited depth. Each strategy is optimized for the tool's typical use case.

Configuration controls truncation behavior. Global limits set defaults; tool-specific overrides enable fine-tuning. Users can adjust limits based on their model's context window and typical workflow. Limits are specified in characters (for predictability) and estimated in tokens (for model awareness).

Artifact storage is local and ephemeral. Artifacts are stored in the working directory during a session. They are not persisted across sessions. File references use relative paths within the workspace. Binary content is base64 encoded or summarized. Artifact IDs are unique within a session.

The truncation system integrates with the message pipeline. After tool execution, results pass through the truncation processor before becoming ToolResult messages. The processor checks size, applies appropriate truncation, creates artifacts if needed, and formats the final result. This happens transparently to tool implementations.

Metadata accompanies truncated results. The model sees not just the truncated content but metadata about the truncation: original size, truncation method, artifact reference if created, and how to request more content. This metadata enables the model to decide if it needs more information.

Performance is important since truncation happens on every tool call. The processor must handle large inputs efficiently without copying entire buffers. Streaming truncation processes input in chunks. Memory usage is bounded regardless of input size.

Security considerations apply to artifact handling. Artifacts may contain sensitive data. They are not exposed outside the session. File paths are validated to prevent directory traversal. Binary content is not executed. Artifact IDs are not predictable.

---

## Glossary / Terms

| Term | Definition |
|------|------------|
| Truncation | Reducing content size to fit limits |
| Artifact | Large content stored separately from conversation |
| Artifact ID | Unique identifier for an artifact |
| Context Window | Model's token limit for input |
| Token Budget | Allocated tokens for specific content |
| Head/Tail Truncation | Keeping beginning and end |
| Smart Truncation | Content-aware truncation preserving structure |
| Truncation Strategy | Method for reducing content |
| Truncation Metadata | Info about what was truncated |
| Character Limit | Max characters before truncation |
| Token Estimate | Approximate token count |
| Inline Result | Result included in conversation |
| Artifact Reference | Pointer to stored artifact |
| Content Type | Category of content (code, text, logs) |
| Omission Marker | Indicator of removed content |
| Streaming Truncation | Processing in chunks |
| Session Scope | Artifact lifetime bounds |

---

## Out of Scope

The following items are explicitly excluded from Task 007.c:

- **Artifact persistence** - Ephemeral storage only
- **Artifact sharing** - Single session only
- **Binary file handling** - Text content focus
- **Compression** - Truncation not compression
- **Content summarization** - Simple truncation only
- **Model-specific token counting** - Character-based limits
- **Artifact search** - Simple retrieval only
- **Distributed artifacts** - Local storage only
- **Artifact versioning** - Single version only
- **Rich artifact types** - Text/JSON only

---

## Functional Requirements

### Truncation Processor

- FR-001: TruncationProcessor MUST be defined in Application layer
- FR-002: Processor MUST accept tool result and limits
- FR-003: Processor MUST return truncated result
- FR-004: Processor MUST create artifact if needed
- FR-005: Processor MUST add truncation metadata
- FR-006: Processor MUST be injectable via DI

### Size Limits

- FR-007: Default inline limit MUST be 8000 characters
- FR-008: Default artifact threshold MUST be 50000 characters
- FR-009: Max artifact size MUST be 10MB
- FR-010: Limits MUST be configurable per-tool
- FR-011: Global limits MUST be configurable
- FR-012: Limits MUST be positive integers

### Truncation Detection

- FR-013: MUST detect when content exceeds inline limit
- FR-014: MUST detect when content exceeds artifact threshold
- FR-015: MUST detect when content exceeds max size
- FR-016: MUST measure size in characters (not bytes)
- FR-017: MUST estimate token count (÷4 approximation)

### Truncation Strategies

- FR-018: MUST support head-only truncation
- FR-019: MUST support tail-only truncation
- FR-020: MUST support head+tail truncation
- FR-021: MUST support line-based truncation
- FR-022: MUST support element-based truncation (JSON/XML)
- FR-023: Default strategy MUST be head+tail
- FR-024: Strategy MUST be configurable per tool

### Head+Tail Strategy

- FR-025: MUST keep first N characters
- FR-026: MUST keep last M characters
- FR-027: MUST insert omission marker in middle
- FR-028: Default split MUST be 60% head, 40% tail
- FR-029: Omission marker MUST show line count omitted
- FR-030: Omission marker MUST show character count

### Line-Based Strategy

- FR-031: MUST preserve complete lines
- FR-032: MUST NOT split lines mid-content
- FR-033: MUST keep first N lines or last N lines
- FR-034: Line counting MUST handle different line endings

### Element-Based Strategy

- FR-035: JSON truncation MUST preserve valid JSON
- FR-036: Array truncation MUST keep first/last elements
- FR-037: Object truncation MUST keep key subset
- FR-038: MUST insert count of omitted elements
- FR-039: Nested truncation MUST respect depth limits

### Omission Markers

- FR-040: Marker MUST indicate truncation occurred
- FR-041: Marker MUST show original size
- FR-042: Marker MUST show amount omitted
- FR-043: Marker format: `\n... [X lines / Y chars omitted] ...\n`
- FR-044: Marker MUST be visually distinct

### Artifact Creation

- FR-045: MUST create artifact when exceeding inline limit
- FR-046: Artifact MUST have unique ID
- FR-047: Artifact ID format: `art_{timestamp}_{random}`
- FR-048: Artifact MUST store full content
- FR-049: Artifact MUST have metadata (size, type, source)
- FR-050: MUST return artifact reference in result

### Artifact Storage

- FR-051: Storage MUST be local to session
- FR-052: Storage path: `.acode/artifacts/`
- FR-053: Artifacts MUST be cleaned up on session end
- FR-054: Storage MUST handle concurrent access
- FR-055: Storage MUST validate paths

### Artifact Reference Format

- FR-056: Reference MUST include artifact ID
- FR-057: Reference MUST include content summary
- FR-058: Reference MUST include size info
- FR-059: Reference format: `[Artifact: {id}] {summary} ({size})`
- FR-060: Summary MUST be ≤100 chars

### Artifact Retrieval

- FR-061: MUST support retrieving by ID
- FR-062: MUST return full content
- FR-063: MUST return 404 for unknown ID
- FR-064: MUST support partial retrieval (lines X-Y)
- FR-065: Retrieval MUST be via tool call

### Truncation Metadata

- FR-066: Metadata MUST include original_size
- FR-067: Metadata MUST include truncated_size
- FR-068: Metadata MUST include strategy_used
- FR-069: Metadata MUST include artifact_id (if created)
- FR-070: Metadata MUST include was_truncated flag

### Tool-Specific Configuration

- FR-071: read_file: head+tail strategy
- FR-072: execute_command: tail-only strategy
- FR-073: list_directory: element-based strategy
- FR-074: search_files: element-based strategy
- FR-075: git_diff: head+tail strategy

---

## Non-Functional Requirements

### Performance

- NFR-001: Truncation MUST complete in < 10ms for ≤100KB
- NFR-002: Memory usage MUST be ≤2x input size
- NFR-003: Streaming MUST handle inputs > memory
- NFR-004: Artifact writes MUST be async

### Reliability

- NFR-005: Truncation MUST NOT crash on malformed input
- NFR-006: Truncation MUST NOT produce invalid output
- NFR-007: Artifact storage MUST handle disk full
- NFR-008: Unicode MUST be preserved correctly

### Security

- NFR-009: Artifact paths MUST be validated
- NFR-010: Artifacts MUST NOT escape session directory
- NFR-011: Artifact IDs MUST NOT be predictable
- NFR-012: Sensitive content MUST NOT be logged

### Observability

- NFR-013: Truncation events MUST be logged
- NFR-014: Artifact creation MUST be logged
- NFR-015: Size metrics MUST be available
- NFR-016: Truncation rates MUST be trackable

### Maintainability

- NFR-017: Strategies MUST be extensible
- NFR-018: Configuration MUST be documented
- NFR-019: All public APIs MUST have XML docs

---

## User Manual Documentation

### Overview

Tool results often exceed practical limits. The truncation system ensures results fit within model context while preserving usefulness. Large content is stored as artifacts with references in the conversation.

### How It Works

1. Tool executes and produces output
2. Truncation processor checks size
3. If under limit: inline as-is
4. If over limit but under artifact threshold: truncate inline
5. If over artifact threshold: create artifact, inline reference

### Configuration

```yaml
tools:
  truncation:
    # Inline without truncation up to this size
    inline_limit: 8000
    
    # Create artifact above this size
    artifact_threshold: 50000
    
    # Maximum artifact size
    max_artifact_size: 10485760  # 10MB
    
    # Default strategy
    default_strategy: head_tail
    
    # Head/tail split ratio
    head_ratio: 0.6
    
    # Tool-specific overrides
    overrides:
      execute_command:
        strategy: tail
        inline_limit: 5000
      
      list_directory:
        strategy: element
        inline_limit: 10000
```

### Truncation Strategies

#### head_tail (default)

Keeps beginning and end:

```
First 60% of content...

... [150 lines / 12,340 chars omitted] ...

Last 40% of content...
```

#### tail

Keeps only the end (good for logs/command output):

```
... [Beginning omitted: 500 lines / 45,000 chars] ...

Last portion of output
including the most recent content
```

#### head

Keeps only the beginning:

```
First portion of output

... [Remainder omitted: 300 lines / 25,000 chars] ...
```

#### element

For structured data (JSON, search results):

```json
{
  "items": [
    { "first": "item" },
    { "second": "item" },
    "... 45 items omitted ...",
    { "second_to_last": "item" },
    { "last": "item" }
  ],
  "total_count": 50
}
```

### Artifact References

When content is too large even for truncation:

```
[Artifact: art_1699234567_a1b2c3] File contents of large_file.txt (2.3 MB)

Use the get_artifact tool to retrieve specific portions:
- get_artifact(id="art_1699234567_a1b2c3") for full content
- get_artifact(id="art_1699234567_a1b2c3", lines="1-100") for first 100 lines
```

### Retrieving Artifacts

The model can request artifact content:

```json
{
    "tool": "get_artifact",
    "arguments": {
        "artifact_id": "art_1699234567_a1b2c3",
        "start_line": 100,
        "end_line": 200
    }
}
```

### CLI Commands

```bash
# List current session artifacts
$ acode artifacts list
┌─────────────────────────────────────────────────────────────┐
│ Session Artifacts                                            │
├─────────────────────────────┬───────┬───────────────────────┤
│ ID                          │ Size  │ Source                │
├─────────────────────────────┼───────┼───────────────────────┤
│ art_1699234567_a1b2c3      │ 2.3MB │ read_file: large.txt  │
│ art_1699234890_d4e5f6      │ 890KB │ execute_command       │
└─────────────────────────────┴───────┴───────────────────────┘

# View artifact content
$ acode artifacts show art_1699234567_a1b2c3 --lines 1-50

# Clean up artifacts
$ acode artifacts clean
```

### Best Practices

1. **Tune limits per workflow**: Coding tasks may need more context
2. **Use tail for logs**: Most recent output is usually most relevant
3. **Use element for search**: Preserve result structure
4. **Monitor artifact creation**: May indicate tools returning too much

---

## Acceptance Criteria

### Processor

- [ ] AC-001: TruncationProcessor defined
- [ ] AC-002: Accepts result and limits
- [ ] AC-003: Returns truncated result
- [ ] AC-004: Creates artifact if needed
- [ ] AC-005: Adds metadata
- [ ] AC-006: Injectable via DI

### Limits

- [ ] AC-007: Default inline 8000
- [ ] AC-008: Default artifact 50000
- [ ] AC-009: Max artifact 10MB
- [ ] AC-010: Per-tool configurable
- [ ] AC-011: Global configurable
- [ ] AC-012: Validates positive

### Detection

- [ ] AC-013: Detects inline exceed
- [ ] AC-014: Detects artifact threshold
- [ ] AC-015: Detects max size
- [ ] AC-016: Uses characters
- [ ] AC-017: Estimates tokens

### Strategies

- [ ] AC-018: Head-only works
- [ ] AC-019: Tail-only works
- [ ] AC-020: Head+tail works
- [ ] AC-021: Line-based works
- [ ] AC-022: Element-based works
- [ ] AC-023: Default is head+tail
- [ ] AC-024: Per-tool configurable

### Head+Tail

- [ ] AC-025: Keeps first N
- [ ] AC-026: Keeps last M
- [ ] AC-027: Inserts marker
- [ ] AC-028: Default 60/40 split
- [ ] AC-029: Shows line count
- [ ] AC-030: Shows char count

### Line-Based

- [ ] AC-031: Preserves complete lines
- [ ] AC-032: No mid-line splits
- [ ] AC-033: First or last N lines
- [ ] AC-034: Handles line endings

### Element-Based

- [ ] AC-035: Preserves valid JSON
- [ ] AC-036: Keeps first/last elements
- [ ] AC-037: Keeps key subset
- [ ] AC-038: Shows omitted count
- [ ] AC-039: Respects depth

### Markers

- [ ] AC-040: Indicates truncation
- [ ] AC-041: Shows original size
- [ ] AC-042: Shows omitted amount
- [ ] AC-043: Format correct
- [ ] AC-044: Visually distinct

### Artifact Creation

- [ ] AC-045: Creates when exceeds
- [ ] AC-046: Unique ID
- [ ] AC-047: ID format correct
- [ ] AC-048: Stores full content
- [ ] AC-049: Has metadata
- [ ] AC-050: Returns reference

### Storage

- [ ] AC-051: Session-local
- [ ] AC-052: Correct path
- [ ] AC-053: Cleanup on end
- [ ] AC-054: Handles concurrency
- [ ] AC-055: Validates paths

### Reference

- [ ] AC-056: Includes ID
- [ ] AC-057: Includes summary
- [ ] AC-058: Includes size
- [ ] AC-059: Format correct
- [ ] AC-060: Summary ≤100 chars

### Retrieval

- [ ] AC-061: By ID works
- [ ] AC-062: Returns full content
- [ ] AC-063: 404 for unknown
- [ ] AC-064: Partial works
- [ ] AC-065: Via tool call

### Metadata

- [ ] AC-066: original_size present
- [ ] AC-067: truncated_size present
- [ ] AC-068: strategy_used present
- [ ] AC-069: artifact_id present
- [ ] AC-070: was_truncated flag

---

## Testing Requirements

### Unit Tests

```
Tests/Unit/Application/Truncation/
├── TruncationProcessorTests.cs
│   ├── Should_Not_Truncate_Under_Limit()
│   ├── Should_Truncate_Over_Limit()
│   ├── Should_Create_Artifact_Over_Threshold()
│   └── Should_Add_Metadata()
│
├── HeadTailStrategyTests.cs
│   ├── Should_Keep_Head_And_Tail()
│   ├── Should_Insert_Marker()
│   ├── Should_Respect_Split_Ratio()
│   └── Should_Handle_Small_Content()
│
├── TailStrategyTests.cs
│   ├── Should_Keep_Only_Tail()
│   ├── Should_Preserve_Complete_Lines()
│   └── Should_Insert_Head_Marker()
│
├── ElementStrategyTests.cs
│   ├── Should_Preserve_Valid_JSON()
│   ├── Should_Keep_First_Last_Elements()
│   └── Should_Show_Omitted_Count()
│
└── ArtifactStorageTests.cs
    ├── Should_Create_Artifact()
    ├── Should_Retrieve_Artifact()
    ├── Should_Cleanup_On_Session_End()
    └── Should_Handle_Concurrent_Access()
```

### Integration Tests

```
Tests/Integration/Truncation/
├── TruncationIntegrationTests.cs
│   ├── Should_Truncate_File_Content()
│   ├── Should_Truncate_Command_Output()
│   └── Should_Create_And_Retrieve_Artifact()
```

---

## User Verification Steps

### Scenario 1: Small Content No Truncation

1. Read small file (<8000 chars)
2. Verify: Full content in result
3. Verify: No truncation marker
4. Verify: No artifact created

### Scenario 2: Medium Content Truncation

1. Read medium file (8000-50000 chars)
2. Verify: Content truncated
3. Verify: Marker present
4. Verify: Head and tail preserved

### Scenario 3: Large Content Artifact

1. Read large file (>50000 chars)
2. Verify: Artifact created
3. Verify: Reference in result
4. Verify: Can retrieve via get_artifact

### Scenario 4: Tail Strategy

1. Run command with long output
2. Verify: Only tail preserved
3. Verify: Most recent lines visible

### Scenario 5: JSON Truncation

1. Search producing many results
2. Verify: Valid JSON preserved
3. Verify: First/last elements present
4. Verify: Count of omitted shown

### Scenario 6: Partial Artifact Retrieval

1. Get artifact with line range
2. Verify: Only requested lines returned
3. Verify: Correct content

---

## Implementation Prompt

### File Structure

```
src/AgenticCoder.Application/Truncation/
├── ITruncationProcessor.cs
├── ITruncationStrategy.cs
├── IArtifactStorage.cs
├── TruncationResult.cs
├── TruncationMetadata.cs
├── TruncationConfiguration.cs
├── Strategies/
│   ├── HeadTailStrategy.cs
│   ├── TailStrategy.cs
│   ├── HeadStrategy.cs
│   └── ElementStrategy.cs

src/AgenticCoder.Infrastructure/Truncation/
├── TruncationProcessor.cs
├── ArtifactStorage.cs
├── ArtifactReference.cs
└── Tools/
    └── GetArtifactTool.cs
```

### ITruncationProcessor Interface

```csharp
namespace AgenticCoder.Application.Truncation;

public interface ITruncationProcessor
{
    TruncationResult Process(
        string content,
        string toolName,
        TruncationConfiguration? config = null);
}
```

### TruncationResult Class

```csharp
namespace AgenticCoder.Application.Truncation;

public sealed class TruncationResult
{
    public required string Content { get; init; }
    public required TruncationMetadata Metadata { get; init; }
    public string? ArtifactId { get; init; }
    public string? ArtifactReference { get; init; }
}
```

### Implementation Checklist

1. [ ] Create ITruncationProcessor
2. [ ] Create ITruncationStrategy
3. [ ] Create IArtifactStorage
4. [ ] Create TruncationResult
5. [ ] Create TruncationMetadata
6. [ ] Create TruncationConfiguration
7. [ ] Implement HeadTailStrategy
8. [ ] Implement TailStrategy
9. [ ] Implement HeadStrategy
10. [ ] Implement ElementStrategy
11. [ ] Implement TruncationProcessor
12. [ ] Implement ArtifactStorage
13. [ ] Create GetArtifactTool
14. [ ] Wire up DI registration
15. [ ] Add CLI commands
16. [ ] Write unit tests
17. [ ] Add XML documentation

### Dependencies

- Task 007 (Tool Schema Registry)
- Task 004.a (ToolResult types)
- System.Text.Json

### Verification Command

```bash
dotnet test --filter "FullyQualifiedName~Truncation"
```

---

**End of Task 007.c Specification**