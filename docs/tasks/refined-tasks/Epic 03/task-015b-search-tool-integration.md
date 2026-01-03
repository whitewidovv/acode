# Task 015.b: Search Tool Integration

**Priority:** P0 – Critical  
**Tier:** S – Core Infrastructure  
**Complexity:** 5 (Fibonacci points)  
**Phase:** Phase 3 – Intelligence Layer  
**Dependencies:** Task 015 (Indexing v1), Task 010 (CLI Framework)  

---

## Description

Task 015.b integrates the search index with the tool system. The agent uses tools to interact with the codebase. Search is one of the most important tools.

The search tool enables the agent to find relevant code. When the agent needs to understand something, it searches. Search results inform context selection.

The tool follows the standard tool interface. It has defined inputs and outputs. It logs its usage. It handles errors gracefully.

Multiple search modes are supported. Text search finds content. File search finds paths. Grep search finds patterns. Each mode has its use cases.

Results are formatted for the agent. Snippets show relevant context. Line numbers enable navigation. Scores indicate relevance.

Rate limiting prevents runaway searches. Too many searches slow everything down. Limits are configurable. The agent is informed when limits apply.

The search tool integrates with context budgeting. Results count toward token limits. The tool can return fewer results when budget is tight.

Error handling is comprehensive. Index not ready. Search too broad. No results. Each case has clear handling and messaging.

---

## Glossary / Terms

| Term | Definition |
|------|------------|
| Search Tool | Agent-callable search |
| Tool Interface | Standard tool API |
| Text Search | Content search |
| File Search | Path search |
| Grep Search | Pattern search |
| Snippet | Result context |
| Rate Limiting | Search throttling |
| Token Budget | Context limit |
| Result Cap | Maximum results |
| Relevance | Match quality |
| Query | Search input |
| Filter | Result limiting |
| Pagination | Result batching |
| Cache | Result reuse |
| Timeout | Search limit |

---

## Out of Scope

The following items are explicitly excluded from Task 015.b:

- **Semantic search** - v2
- **Symbol search** - Task 017
- **Cross-repo search** - Single repo only
- **Real-time results** - Batch results
- **Search history** - No persistence
- **Search suggestions** - No autocomplete

---

## Functional Requirements

### Text Search Tool

- FR-001: search_text tool MUST exist
- FR-002: Query parameter required
- FR-003: Return matching files
- FR-004: Return line numbers
- FR-005: Return snippets
- FR-006: Return relevance scores

### File Search Tool

- FR-007: search_files tool MUST exist
- FR-008: Pattern parameter required
- FR-009: Return matching paths
- FR-010: Support wildcards
- FR-011: Support directory filtering

### Grep Tool

- FR-012: grep tool MUST exist
- FR-013: Pattern parameter required
- FR-014: Return all matches
- FR-015: Support regex
- FR-016: Support case options

### Tool Parameters

- FR-017: max_results parameter
- FR-018: include_path filter
- FR-019: exclude_path filter
- FR-020: file_type filter
- FR-021: context_lines parameter

### Tool Results

- FR-022: JSON result format
- FR-023: File path in result
- FR-024: Line number in result
- FR-025: Content snippet in result
- FR-026: Score in result
- FR-027: Total count in result

### Rate Limiting

- FR-028: Per-session limits
- FR-029: Configurable limits
- FR-030: Limit notification
- FR-031: Backoff suggestion

### Error Handling

- FR-032: Index not ready error
- FR-033: Invalid query error
- FR-034: No results message
- FR-035: Timeout handling

### Integration

- FR-036: Tool registry integration
- FR-037: Logging integration
- FR-038: Metrics integration
- FR-039: Budget integration

---

## Non-Functional Requirements

### Performance

- NFR-001: Search < 100ms
- NFR-002: Result formatting < 50ms
- NFR-003: Total < 200ms

### Reliability

- NFR-004: Graceful degradation
- NFR-005: Timeout handling
- NFR-006: Error recovery

### Usability

- NFR-007: Clear error messages
- NFR-008: Helpful result format
- NFR-009: Good documentation

---

## User Manual Documentation

### Overview

The search tools enable the agent to find code in the repository. Three search modes are available.

### Search Text Tool

Searches file contents:

```json
{
  "tool": "search_text",
  "parameters": {
    "query": "UserService",
    "max_results": 10,
    "file_type": ".cs",
    "context_lines": 2
  }
}
```

Result:
```json
{
  "total": 15,
  "results": [
    {
      "path": "src/Services/UserService.cs",
      "line": 10,
      "score": 0.95,
      "snippet": "public class UserService : IUserService",
      "context": {
        "before": ["namespace MyApp.Services", "{"],
        "after": ["{", "    private readonly IUserRepository _repo;"]
      }
    }
  ]
}
```

### Search Files Tool

Searches file paths:

```json
{
  "tool": "search_files",
  "parameters": {
    "pattern": "*Controller*.cs",
    "directory": "src"
  }
}
```

Result:
```json
{
  "total": 5,
  "results": [
    { "path": "src/Controllers/UserController.cs" },
    { "path": "src/Controllers/OrderController.cs" }
  ]
}
```

### Grep Tool

Pattern matching:

```json
{
  "tool": "grep",
  "parameters": {
    "pattern": "TODO:|FIXME:",
    "regex": true,
    "include_path": "src/**"
  }
}
```

Result:
```json
{
  "total": 8,
  "results": [
    {
      "path": "src/Services/OrderService.cs",
      "line": 45,
      "match": "// TODO: Add validation"
    }
  ]
}
```

### Configuration

```yaml
# .agent/config.yml
tools:
  search:
    # Default max results
    default_max_results: 20
    
    # Rate limits
    rate_limit:
      searches_per_minute: 30
      
    # Timeout
    timeout_seconds: 10
    
    # Context lines for snippets
    default_context_lines: 2
```

### Rate Limiting

When rate limited:

```json
{
  "error": "rate_limit_exceeded",
  "message": "Search rate limit reached (30/min)",
  "retry_after_seconds": 45
}
```

### Troubleshooting

#### No Results

**Problem:** Search returns empty

**Solutions:**
1. Check query spelling
2. Try broader search
3. Ensure index is built

#### Too Many Results

**Problem:** Results not relevant

**Solutions:**
1. Add file type filter
2. Add path filter
3. Use more specific query

#### Index Not Ready

**Problem:** Search fails with index error

**Solutions:**
1. Build index: `acode index build`
2. Wait for background indexing

---

## Acceptance Criteria

### Text Search

- [ ] AC-001: Tool registered
- [ ] AC-002: Query works
- [ ] AC-003: Filtering works
- [ ] AC-004: Snippets returned

### File Search

- [ ] AC-005: Tool registered
- [ ] AC-006: Pattern works
- [ ] AC-007: Wildcards work

### Grep

- [ ] AC-008: Tool registered
- [ ] AC-009: Pattern works
- [ ] AC-010: Regex works

### Rate Limiting

- [ ] AC-011: Limits enforced
- [ ] AC-012: Notification sent
- [ ] AC-013: Configurable

### Integration

- [ ] AC-014: Tool system works
- [ ] AC-015: Logging works
- [ ] AC-016: Errors handled

---

## Testing Requirements

### Unit Tests

```
Tests/Unit/Tools/Search/
├── SearchTextToolTests.cs
│   ├── Should_Search_Single_Word()
│   ├── Should_Search_Multiple_Words()
│   ├── Should_Search_Phrase()
│   ├── Should_Filter_By_File_Type()
│   ├── Should_Filter_By_Directory()
│   ├── Should_Limit_Max_Results()
│   ├── Should_Return_Snippets()
│   ├── Should_Return_Line_Numbers()
│   ├── Should_Return_Relevance_Score()
│   ├── Should_Handle_No_Results()
│   ├── Should_Handle_Empty_Query()
│   ├── Should_Handle_Invalid_Query()
│   └── Should_Validate_Input_Parameters()
│
├── SearchFilesToolTests.cs
│   ├── Should_Match_Exact_Filename()
│   ├── Should_Match_Wildcard_Pattern()
│   ├── Should_Match_Extension_Pattern()
│   ├── Should_Filter_By_Directory()
│   ├── Should_Support_Recursive()
│   ├── Should_Support_Non_Recursive()
│   ├── Should_Limit_Max_Results()
│   ├── Should_Return_File_Paths()
│   ├── Should_Return_File_Metadata()
│   ├── Should_Handle_No_Matches()
│   └── Should_Validate_Input_Parameters()
│
├── GrepToolTests.cs
│   ├── Should_Match_Literal_String()
│   ├── Should_Match_Regex_Pattern()
│   ├── Should_Handle_Case_Insensitive()
│   ├── Should_Handle_Case_Sensitive()
│   ├── Should_Filter_By_File_Pattern()
│   ├── Should_Filter_By_Directory()
│   ├── Should_Return_Line_Content()
│   ├── Should_Return_Line_Numbers()
│   ├── Should_Return_Context_Lines()
│   ├── Should_Handle_No_Matches()
│   ├── Should_Handle_Invalid_Regex()
│   └── Should_Validate_Input_Parameters()
│
├── RateLimitingTests.cs
│   ├── Should_Allow_Under_Limit()
│   ├── Should_Block_Over_Limit()
│   ├── Should_Reset_After_Window()
│   ├── Should_Return_Retry_After()
│   ├── Should_Track_Per_Tool()
│   ├── Should_Load_Config_Limits()
│   └── Should_Handle_Concurrent_Requests()
│
├── SearchResultFormatterTests.cs
│   ├── Should_Format_Single_Result()
│   ├── Should_Format_Multiple_Results()
│   ├── Should_Include_Snippet()
│   ├── Should_Include_Line_Number()
│   ├── Should_Include_Path()
│   ├── Should_Truncate_Long_Snippets()
│   └── Should_Handle_Empty_Results()
│
└── SearchInputValidationTests.cs
    ├── Should_Require_Query()
    ├── Should_Validate_Max_Results()
    ├── Should_Validate_File_Type()
    ├── Should_Validate_Path_Pattern()
    └── Should_Sanitize_Input()
```

### Integration Tests

```
Tests/Integration/Tools/Search/
├── SearchToolIntegrationTests.cs
│   ├── Should_Work_With_Real_Index()
│   ├── Should_Return_Correct_Results()
│   ├── Should_Handle_Large_Index()
│   ├── Should_Handle_Concurrent_Searches()
│   └── Should_Update_After_Index_Change()
│
└── SearchToolRegistrationTests.cs
    ├── Should_Register_All_Search_Tools()
    ├── Should_Appear_In_Tool_List()
    └── Should_Have_Correct_Schema()
```

### E2E Tests

```
Tests/E2E/Tools/Search/
├── SearchToolE2ETests.cs
│   ├── Should_Work_In_Agent_Loop()
│   ├── Should_Provide_Context_For_Planning()
│   ├── Should_Handle_Agent_Follow_Up()
│   └── Should_Log_Tool_Usage()
```

### Performance Benchmarks

| Benchmark | Target | Maximum |
|-----------|--------|---------|
| Text search | 50ms | 100ms |
| File search | 25ms | 50ms |
| Grep | 75ms | 150ms |

---

## User Verification Steps

### Scenario 1: Text Search

1. Agent calls search_text
2. Query for known content
3. Verify: Results returned

### Scenario 2: File Search

1. Agent calls search_files
2. Pattern for known file
3. Verify: Path returned

### Scenario 3: Grep

1. Agent calls grep
2. Regex pattern
3. Verify: Matches found

### Scenario 4: Rate Limit

1. Exceed rate limit
2. Verify: Error returned
3. Verify: Retry info included

---

## Implementation Prompt

### File Structure

```
src/AgenticCoder.Application/
├── Tools/
│   └── Search/
│       ├── SearchTextTool.cs
│       ├── SearchFilesTool.cs
│       └── GrepTool.cs
│
src/AgenticCoder.Infrastructure/
├── Tools/
│   └── Search/
│       ├── SearchToolRateLimiter.cs
│       └── SearchResultFormatter.cs
```

### SearchTextTool Class

```csharp
namespace AgenticCoder.Application.Tools.Search;

public sealed class SearchTextTool : ITool
{
    public string Name => "search_text";
    
    public async Task<ToolResult> ExecuteAsync(
        ToolInput input,
        CancellationToken ct)
    {
        var query = input.GetRequired<string>("query");
        var maxResults = input.GetOptional("max_results", 20);
        
        await _rateLimiter.CheckAsync(ct);
        
        var results = await _indexService.SearchAsync(
            new SearchQuery(query) { MaxResults = maxResults },
            ct);
            
        return new ToolResult(_formatter.Format(results));
    }
}
```

### Error Codes

| Code | Meaning |
|------|---------|
| ACODE-SRC-001 | Index not ready |
| ACODE-SRC-002 | Invalid query |
| ACODE-SRC-003 | Rate limited |
| ACODE-SRC-004 | Timeout |

### Implementation Checklist

1. [ ] Create search text tool
2. [ ] Create search files tool
3. [ ] Create grep tool
4. [ ] Implement rate limiting
5. [ ] Add result formatting
6. [ ] Register with tool system
7. [ ] Add logging
8. [ ] Write tests

### Rollout Plan

1. **Phase 1:** Search text
2. **Phase 2:** Search files
3. **Phase 3:** Grep
4. **Phase 4:** Rate limiting
5. **Phase 5:** Integration

---

**End of Task 015.b Specification**