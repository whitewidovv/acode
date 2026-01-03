# Task 025.b: CLI add/list/show/retry/cancel

**Priority:** P0 – Critical  
**Tier:** S – Core Infrastructure  
**Complexity:** 5 (Fibonacci points)  
**Phase:** Phase 6 – Execution Layer  
**Dependencies:** Task 025 (Task Spec), Task 025.a (Schema), Task 009 (CLI)  

---

## Description

Task 025.b implements CLI commands for task queue management. Users MUST be able to add, list, show, retry, and cancel tasks from the command line.

Each command MUST have clear syntax, helpful output, and proper error handling. Commands MUST support common options like `--json` for machine-readable output and `--verbose` for detailed information.

The CLI MUST integrate with the task queue system. Commands MUST validate inputs before submission. Results MUST be displayed in human-readable format by default.

### Business Value

CLI commands enable:
- Human interaction with task queue
- Scripting and automation
- Quick task management
- Queue inspection and debugging
- Task lifecycle control

### Scope Boundaries

This task covers CLI commands only. Task spec format is in Task 025. Schema is in Task 025.a. Error formatting is in Task 025.c. Queue persistence is in Task 026.

### Integration Points

- Task 025: Task spec parsing
- Task 025.a: Validation
- Task 025.c: Error display
- Task 026: Queue operations
- Task 009: CLI framework

### Failure Modes

- Invalid task spec → Show validation errors
- Task not found → Show error with suggestion
- Queue unavailable → Show connection error
- Permission denied → Show authorization error

---

## Glossary / Terms

| Term | Definition |
|------|------------|
| add | Create new task in queue |
| list | Show tasks matching filter |
| show | Display task details |
| retry | Re-queue failed task |
| cancel | Stop and remove task |
| filter | Query criteria for list |
| format | Output format (text/json) |
| verbose | Show additional details |
| quiet | Suppress non-essential output |

---

## Out of Scope

- Web UI for task management
- Real-time task streaming
- Bulk task operations
- Task templates
- Interactive task editor
- Task scheduling

---

## Functional Requirements

### FR-001 to FR-025: add Command

- FR-001: `acode task add <file>` MUST parse file
- FR-002: `acode task add -` MUST read stdin
- FR-003: `acode task add --inline` MUST accept fields
- FR-004: `--title` flag MUST set title
- FR-005: `--description` flag MUST set description
- FR-006: `--priority` flag MUST set priority
- FR-007: `--tags` flag MUST set tags (comma-separated)
- FR-008: `--files` flag MUST set files (comma-separated)
- FR-009: `--depends-on` flag MUST set dependencies
- FR-010: Add MUST validate before queue
- FR-011: Add MUST return task ID on success
- FR-012: Add MUST show errors on failure
- FR-013: `--dry-run` MUST validate without adding
- FR-014: `--json` MUST output JSON result
- FR-015: `--quiet` MUST output ID only
- FR-016: Exit code 0 on success
- FR-017: Exit code 1 on validation error
- FR-018: Exit code 2 on queue error
- FR-019: Add MUST log operation
- FR-020: Add MUST emit TaskEnqueued event
- FR-021: Add MUST support batch files
- FR-022: Batch MUST stop on first error
- FR-023: `--continue-on-error` for batch
- FR-024: Batch MUST report summary
- FR-025: Add MUST respect rate limits

### FR-026 to FR-050: list Command

- FR-026: `acode task list` MUST show all tasks
- FR-027: `--status` MUST filter by status
- FR-028: `--priority` MUST filter by priority
- FR-029: `--tag` MUST filter by tag
- FR-030: `--since` MUST filter by created date
- FR-031: `--limit` MUST limit results
- FR-032: `--offset` MUST paginate
- FR-033: Default limit MUST be 50
- FR-034: List MUST show ID, title, status
- FR-035: `--verbose` MUST show all fields
- FR-036: `--json` MUST output JSON array
- FR-037: `--csv` MUST output CSV
- FR-038: `--quiet` MUST show IDs only
- FR-039: `--sort` MUST order results
- FR-040: Sort options: created, priority, status
- FR-041: `--reverse` MUST reverse sort order
- FR-042: Empty result MUST show message
- FR-043: List MUST show count
- FR-044: Exit code 0 always
- FR-045: List MUST handle large results
- FR-046: List MUST be streaming for large sets
- FR-047: List MUST respect timeout
- FR-048: `--watch` MUST refresh periodically
- FR-049: Watch interval MUST be configurable
- FR-050: Ctrl+C MUST stop watch

### FR-051 to FR-070: show Command

- FR-051: `acode task show <id>` MUST display task
- FR-052: Show MUST display all fields
- FR-053: Show MUST display status history
- FR-054: Show MUST display attempt count
- FR-055: Show MUST display last error
- FR-056: Show MUST display dependencies
- FR-057: Show MUST display dependent tasks
- FR-058: `--json` MUST output JSON
- FR-059: `--yaml` MUST output YAML
- FR-060: Show MUST handle not found
- FR-061: Not found MUST suggest similar
- FR-062: Exit code 0 on success
- FR-063: Exit code 1 on not found
- FR-064: Show MUST display timing
- FR-065: Show MUST display worker ID
- FR-066: Show MUST display files list
- FR-067: Show MUST display tags
- FR-068: Show MUST display metadata
- FR-069: Large metadata MUST be truncated
- FR-070: `--full` MUST show untruncated

### FR-071 to FR-085: retry Command

- FR-071: `acode task retry <id>` MUST retry task
- FR-072: Retry MUST work for failed tasks
- FR-073: Retry MUST work for cancelled tasks
- FR-074: Retry MUST NOT work for pending
- FR-075: Retry MUST NOT work for running
- FR-076: Retry MUST NOT work for completed
- FR-077: Retry MUST reset attempt count optionally
- FR-078: `--reset-attempts` MUST reset count
- FR-079: Retry MUST increment attempt by default
- FR-080: Retry past limit MUST require `--force`
- FR-081: Retry MUST emit TaskRetried event
- FR-082: Retry MUST log operation
- FR-083: Exit code 0 on success
- FR-084: Exit code 1 on invalid state
- FR-085: `--json` MUST output JSON result

### FR-086 to FR-100: cancel Command

- FR-086: `acode task cancel <id>` MUST cancel
- FR-087: Cancel MUST work for pending tasks
- FR-088: Cancel MUST work for running tasks
- FR-089: Running cancel MUST signal worker
- FR-090: Cancel MUST NOT work for completed
- FR-091: Cancel MUST NOT work for cancelled
- FR-092: `--force` MUST kill running worker
- FR-093: Cancel MUST emit TaskCancelled event
- FR-094: Cancel MUST log operation
- FR-095: Cancel MUST update status
- FR-096: Cancel MUST record cancellation time
- FR-097: Exit code 0 on success
- FR-098: Exit code 1 on invalid state
- FR-099: `--json` MUST output JSON result
- FR-100: Bulk cancel MUST accept multiple IDs

---

## Non-Functional Requirements

- NFR-001: Command response MUST be <500ms
- NFR-002: List MUST stream for >1000 results
- NFR-003: Output MUST be UTF-8
- NFR-004: Colors MUST respect NO_COLOR
- NFR-005: Help MUST be comprehensive
- NFR-006: Examples MUST be in help
- NFR-007: Tab completion MUST work
- NFR-008: History MUST be preserved
- NFR-009: Credentials MUST NOT appear
- NFR-010: IDs MUST be copy-pasteable

---

## User Manual Documentation

### Command Reference

```bash
# Add task from file
acode task add task.yaml

# Add task from stdin
cat task.yaml | acode task add -

# Add with inline fields
acode task add --title "Fix bug #123" \
  --description "Fix the login validation bug" \
  --priority 1 \
  --tags "bug,urgent"

# List all pending tasks
acode task list --status pending

# List high priority
acode task list --priority 1,2

# List with tag
acode task list --tag feature

# Show task details
acode task show 01ARZ3NDEKTSV4RRFFQ69G5FAV

# Retry failed task
acode task retry 01ARZ3NDEKTSV4RRFFQ69G5FAV

# Cancel running task
acode task cancel 01ARZ3NDEKTSV4RRFFQ69G5FAV

# Force kill
acode task cancel --force 01ARZ3NDEKTSV4RRFFQ69G5FAV
```

### Output Formats

**Default (human-readable):**
```
ID                          TITLE                STATUS    PRIORITY
01ARZ3NDEKTSV4RRFFQ69G5FAV  Fix login bug        pending   1
01ARZ3NDEKTSV4RRFFQ69G5FAW  Add user profile     running   3
```

**JSON:**
```json
[
  {
    "id": "01ARZ3NDEKTSV4RRFFQ69G5FAV",
    "title": "Fix login bug",
    "status": "pending",
    "priority": 1
  }
]
```

### Exit Codes

| Code | Meaning |
|------|---------|
| 0 | Success |
| 1 | Validation/state error |
| 2 | Queue/system error |
| 130 | Interrupted (Ctrl+C) |

---

## Acceptance Criteria / Definition of Done

- [ ] AC-001: add from file works
- [ ] AC-002: add from stdin works
- [ ] AC-003: add inline works
- [ ] AC-004: add validates input
- [ ] AC-005: add returns ID
- [ ] AC-006: list shows tasks
- [ ] AC-007: list filters work
- [ ] AC-008: list pagination works
- [ ] AC-009: list sorting works
- [ ] AC-010: show displays task
- [ ] AC-011: show handles not found
- [ ] AC-012: retry works for failed
- [ ] AC-013: retry blocked for invalid
- [ ] AC-014: cancel works for pending
- [ ] AC-015: cancel signals running
- [ ] AC-016: JSON output works
- [ ] AC-017: Exit codes correct
- [ ] AC-018: Help is complete
- [ ] AC-019: Tab completion works
- [ ] AC-020: Errors are clear

---

## Testing Requirements

### Unit Tests

- [ ] UT-001: add command parsing
- [ ] UT-002: list filter building
- [ ] UT-003: show ID parsing
- [ ] UT-004: retry state validation
- [ ] UT-005: cancel state validation

### Integration Tests

- [ ] IT-001: Full add workflow
- [ ] IT-002: List with filters
- [ ] IT-003: Show existing task
- [ ] IT-004: Retry failed task
- [ ] IT-005: Cancel running task

---

## Implementation Prompt

### Command Structure

```csharp
[Command("task", Description = "Task queue management")]
public class TaskCommand
{
    [Command("add", Description = "Add task to queue")]
    public async Task<int> AddAsync(
        [Argument] string? file = null,
        [Option("--title")] string? title = null,
        [Option("--description")] string? description = null,
        [Option("--priority")] int priority = 3,
        [Option("--tags")] string? tags = null,
        [Option("--dry-run")] bool dryRun = false,
        [Option("--json")] bool json = false);
        
    [Command("list", Description = "List tasks")]
    public async Task<int> ListAsync(
        [Option("--status")] string? status = null,
        [Option("--priority")] string? priority = null,
        [Option("--tag")] string? tag = null,
        [Option("--limit")] int limit = 50,
        [Option("--json")] bool json = false);
        
    [Command("show", Description = "Show task details")]
    public async Task<int> ShowAsync(
        [Argument] string id,
        [Option("--json")] bool json = false);
        
    [Command("retry", Description = "Retry failed task")]
    public async Task<int> RetryAsync(
        [Argument] string id,
        [Option("--reset-attempts")] bool reset = false,
        [Option("--force")] bool force = false);
        
    [Command("cancel", Description = "Cancel task")]
    public async Task<int> CancelAsync(
        [Argument] string id,
        [Option("--force")] bool force = false);
}
```

---

**End of Task 025.b Specification**