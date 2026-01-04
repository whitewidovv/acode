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

### Business Value

Search is the primary mechanism by which the AI agent discovers and understands code in the repository. Without effective search tools, the agent would be limited to files explicitly provided by the user, dramatically reducing its ability to understand context, find related implementations, and make informed changes across a codebase. The search tool integration transforms a passive index into an active capability that the agent can leverage autonomously.

The business value is multiplied by the tool interface approach. By exposing search as a standard tool with well-defined inputs and outputs, the agent can reason about when and how to search. It can formulate queries based on its current understanding, interpret results, and refine searches iteratively. This creates a feedback loop where the agent becomes progressively better at finding relevant code as it explores the repository.

Rate limiting and error handling protect both the user and the system from pathological behavior. An agent that searches too aggressively can slow down the entire interaction, while poor error messages leave the agent unable to recover. By providing clear limits and actionable error information, the search tools enable robust, predictable agent behavior even in edge cases.

### Scope

1. **Text Search Tool** - Content-based search across indexed files with relevance scoring, snippet extraction, and configurable result limits
2. **File Search Tool** - Path-based search with glob pattern support for finding files by name or location
3. **Grep Tool** - Pattern matching search with regex support for finding specific text patterns across the codebase
4. **Rate Limiting** - Per-session and configurable limits on search frequency with informative feedback to the agent
5. **Result Formatting** - Structured JSON output with snippets, line numbers, context, and relevance scores optimized for LLM consumption

### Integration Points

| Component | Integration Type | Description |
|-----------|------------------|-------------|
| Tool Registry | Registration | Search tools register with the central tool registry for agent discovery |
| Index Service | Dependency | All search tools query the index service for fast search execution |
| Logging Service | Integration | Tool invocations and results are logged for debugging and analytics |
| Metrics Service | Integration | Search latency, result counts, and rate limit hits are recorded as metrics |
| Context Budget | Consumer | Search tools respect token budgets and adjust result counts accordingly |
| Agent Orchestrator | Consumer | Agent orchestrator invokes search tools during planning and execution |

### Failure Modes

| Failure | Impact | Mitigation |
|---------|--------|------------|
| Index not ready | Search cannot execute | Return clear error with retry guidance; suggest index build |
| Search query too broad | Excessive results, slow response | Apply result caps, suggest query refinement in response |
| No results found | Agent lacks needed information | Provide helpful message with alternative search suggestions |
| Rate limit exceeded | Agent blocked from searching | Return retry-after time, allow agent to adjust strategy |
| Search timeout | Long-running search abandoned | Return partial results if available, timeout notification |
| Invalid regex pattern | Grep tool fails | Validate regex before search, return syntax error with position |

### Assumptions

1. The index service is available and has been built before search tools are invoked
2. The agent understands the tool interface and can formulate valid search queries
3. Rate limits are sufficient for typical agent workflows without causing excessive blocking
4. JSON result format is optimal for LLM consumption and token efficiency
5. Relevance scoring from the index service accurately reflects result quality
6. Context lines around matches provide sufficient information for the agent to understand results
7. The tool registry follows a standard pattern that search tools can integrate with
8. Search performance depends on index quality and size; results are approximate not exhaustive

### Security Considerations

1. **Query Injection** - Search queries must be sanitized to prevent injection attacks if patterns are compiled to regex
2. **Path Disclosure** - Search results must only include files within the repository; no system file exposure
3. **Resource Exhaustion** - Rate limiting prevents denial of service through excessive search requests
4. **Sensitive Content** - Search results may contain sensitive code; ensure access controls are respected
5. **Log Sanitization** - Search queries logged for debugging must not expose sensitive search terms to unauthorized viewers

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

| ID | Requirement |
|----|-------------|
| FR-015b-01 | The system MUST expose a search_text tool in the tool registry |
| FR-015b-02 | The tool MUST require a query parameter specifying the search terms |
| FR-015b-03 | The tool MUST return a list of matching files with their paths |
| FR-015b-04 | The tool MUST return line numbers for each match location |
| FR-015b-05 | The tool MUST return code snippets showing the matching content with context |
| FR-015b-06 | The tool MUST return relevance scores for result ranking |

### File Search Tool

| ID | Requirement |
|----|-------------|
| FR-015b-07 | The system MUST expose a search_files tool in the tool registry |
| FR-015b-08 | The tool MUST require a pattern parameter for matching file paths |
| FR-015b-09 | The tool MUST return all matching file paths |
| FR-015b-10 | The tool MUST support glob wildcards (* and **) in patterns |
| FR-015b-11 | The tool MUST support optional directory filtering to scope the search |

### Grep Tool

| ID | Requirement |
|----|-------------|
| FR-015b-12 | The system MUST expose a grep tool in the tool registry |
| FR-015b-13 | The tool MUST require a pattern parameter for content matching |
| FR-015b-14 | The tool MUST return all lines matching the pattern across all files |
| FR-015b-15 | The tool MUST support regular expression patterns when regex flag is set |
| FR-015b-16 | The tool MUST support case sensitivity options for pattern matching |

### Tool Parameters

| ID | Requirement |
|----|-------------|
| FR-015b-17 | All search tools MUST support a max_results parameter to limit output |
| FR-015b-18 | All search tools MUST support an include_path filter for scoping searches |
| FR-015b-19 | All search tools MUST support an exclude_path filter for excluding paths |
| FR-015b-20 | Text and grep tools MUST support a file_type filter for extension filtering |
| FR-015b-21 | Text and grep tools MUST support a context_lines parameter for snippet size |

### Tool Results

| ID | Requirement |
|----|-------------|
| FR-015b-22 | All tool results MUST be returned in structured JSON format |
| FR-015b-23 | Each result MUST include the full relative file path |
| FR-015b-24 | Content results MUST include the line number of the match |
| FR-015b-25 | Content results MUST include a snippet of the matching content with context |
| FR-015b-26 | Text search results MUST include a relevance score between 0 and 1 |
| FR-015b-27 | All results MUST include a total count of matches found |

### Rate Limiting

| ID | Requirement |
|----|-------------|
| FR-015b-28 | The system MUST enforce per-session limits on search invocations |
| FR-015b-29 | Rate limits MUST be configurable via .agent/config.yml |
| FR-015b-30 | The system MUST notify the agent when rate limits are approached or exceeded |
| FR-015b-31 | Rate limit responses MUST include a suggested backoff duration |

### Error Handling

| ID | Requirement |
|----|-------------|
| FR-015b-32 | The system MUST return a clear error when the index is not ready |
| FR-015b-33 | The system MUST return a clear error for malformed or invalid queries |
| FR-015b-34 | The system MUST return a meaningful message when no results are found |
| FR-015b-35 | The system MUST handle search timeouts gracefully with partial results if available |

### Integration

| ID | Requirement |
|----|-------------|
| FR-015b-36 | All search tools MUST register with the central tool registry on startup |
| FR-015b-37 | All search tool invocations MUST be logged with query, timing, and result count |
| FR-015b-38 | All search tools MUST emit metrics for latency, result count, and error rate |
| FR-015b-39 | Search tools MUST integrate with context budget and reduce results when budget is tight |

---

## Non-Functional Requirements

### Performance

| ID | Category | Requirement |
|----|----------|-------------|
| NFR-015b-01 | Performance | Text search execution MUST complete in less than 100ms for typical queries |
| NFR-015b-02 | Performance | Result formatting and serialization MUST complete in less than 50ms |
| NFR-015b-03 | Performance | Total tool execution time MUST be less than 200ms end-to-end |

### Reliability

| ID | Category | Requirement |
|----|----------|-------------|
| NFR-015b-04 | Reliability | Search tools MUST degrade gracefully when the index is unavailable |
| NFR-015b-05 | Reliability | Search timeouts MUST be enforced to prevent runaway operations |
| NFR-015b-06 | Reliability | The system MUST recover from individual search errors without affecting other operations |

### Usability

| ID | Category | Requirement |
|----|----------|-------------|
| NFR-015b-07 | Usability | Error messages MUST be clear and actionable for the AI agent |
| NFR-015b-08 | Usability | Result format MUST be optimized for LLM token efficiency |
| NFR-015b-09 | Usability | Tool documentation MUST be comprehensive for agent understanding |

### Maintainability

| ID | Category | Requirement |
|----|----------|-------------|
| NFR-015b-10 | Maintainability | Search tools MUST follow the standard tool interface pattern |
| NFR-015b-11 | Maintainability | Rate limiting logic MUST be reusable across different tool types |
| NFR-015b-12 | Maintainability | Result formatting MUST be centralized for consistent output |

### Observability

| ID | Category | Requirement |
|----|----------|-------------|
| NFR-015b-13 | Observability | Search latency percentiles MUST be available in metrics |
| NFR-015b-14 | Observability | Rate limit violations MUST be logged and counted |
| NFR-015b-15 | Observability | Search queries MUST be traceable through the logging system |

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

## Best Practices

### Tool Design

1. **Provide structured output** - Return JSON with file, line, match context
2. **Support filtering** - Allow file patterns, path exclusions in query
3. **Limit output size** - Cap results to prevent overwhelming context
4. **Include match context** - Return lines before/after match for context

### Agent Integration

5. **Clear tool descriptions** - Help LLM understand when to use search vs grep
6. **Validate inputs early** - Check query parameters before searching
7. **Handle empty results** - Return informative message, not just empty array
8. **Log tool invocations** - Track what searches agent performs for debugging

### Error Handling

9. **Graceful degradation** - If index unavailable, fall back to grep
10. **Timeout protection** - Cancel searches exceeding time limit
11. **Report partial results** - Return what was found if search interrupted
12. **Informative errors** - Include what went wrong and how to fix

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