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

### File Structure

```
src/
└── Acode.Cli/
    └── Commands/
        └── Task/
            ├── TaskCommand.cs           # Root command group
            ├── TaskAddCommand.cs        # Add task
            ├── TaskListCommand.cs       # List tasks
            ├── TaskShowCommand.cs       # Show task details
            ├── TaskRetryCommand.cs      # Retry failed task
            ├── TaskCancelCommand.cs     # Cancel task
            └── Formatters/
                ├── ITaskFormatter.cs    # Format interface
                ├── TableTaskFormatter.cs
                ├── JsonTaskFormatter.cs
                └── CsvTaskFormatter.cs

tests/
└── Acode.Cli.Tests/
    └── Commands/
        └── Task/
            ├── TaskAddCommandTests.cs
            ├── TaskListCommandTests.cs
            ├── TaskShowCommandTests.cs
            ├── TaskRetryCommandTests.cs
            └── TaskCancelCommandTests.cs
```

### Exit Codes Enum

```csharp
// Acode.Cli/ExitCodes.cs
namespace Acode.Cli;

public static class ExitCodes
{
    public const int Success = 0;
    public const int ValidationError = 1;
    public const int SystemError = 2;
    public const int NotFound = 3;
    public const int InvalidState = 4;
    public const int Timeout = 5;
    public const int Interrupted = 130;
}
```

### Task Add Command

```csharp
// Acode.Cli/Commands/Task/TaskAddCommand.cs
namespace Acode.Cli.Commands.Task;

[Command("task add", Description = "Add task to queue")]
public sealed class TaskAddCommand : ICommand
{
    private readonly ITaskSpecParser _parser;
    private readonly ISchemaValidator _validator;
    private readonly ITaskQueue _queue;
    private readonly ILogger<TaskAddCommand> _logger;
    private readonly IErrorFormatter _errorFormatter;
    private readonly IAnsiConsole _console;
    
    [CommandArgument(0, "[file]", Description = "Task spec file path (use - for stdin)")]
    public string? FilePath { get; init; }
    
    [CommandOption("--inline", Description = "Create task from options")]
    public bool Inline { get; init; }
    
    [CommandOption("--title|-t", Description = "Task title")]
    public string? Title { get; init; }
    
    [CommandOption("--description|-d", Description = "Task description")]
    public string? Description { get; init; }
    
    [CommandOption("--priority|-p", Description = "Priority (1-5)")]
    public int Priority { get; init; } = 3;
    
    [CommandOption("--tags", Description = "Comma-separated tags")]
    public string? Tags { get; init; }
    
    [CommandOption("--files", Description = "Comma-separated file paths")]
    public string? Files { get; init; }
    
    [CommandOption("--depends-on", Description = "Comma-separated dependency IDs")]
    public string? DependsOn { get; init; }
    
    [CommandOption("--dry-run", Description = "Validate without adding")]
    public bool DryRun { get; init; }
    
    [CommandOption("--json", Description = "Output as JSON")]
    public bool Json { get; init; }
    
    [CommandOption("--quiet|-q", Description = "Output ID only")]
    public bool Quiet { get; init; }
    
    [CommandOption("--batch", Description = "Process multiple tasks from file")]
    public bool Batch { get; init; }
    
    [CommandOption("--continue-on-error", Description = "Continue batch on errors")]
    public bool ContinueOnError { get; init; }
    
    public TaskAddCommand(
        ITaskSpecParser parser,
        ISchemaValidator validator,
        ITaskQueue queue,
        IErrorFormatter errorFormatter,
        IAnsiConsole console,
        ILogger<TaskAddCommand> logger)
    {
        _parser = parser;
        _validator = validator;
        _queue = queue;
        _errorFormatter = errorFormatter;
        _console = console;
        _logger = logger;
    }
    
    public async ValueTask ExecuteAsync(IConsole console)
    {
        try
        {
            if (Inline)
            {
                await AddInlineAsync();
            }
            else if (FilePath == "-")
            {
                await AddFromStdinAsync();
            }
            else if (!string.IsNullOrEmpty(FilePath))
            {
                if (Batch)
                    await AddBatchAsync();
                else
                    await AddFromFileAsync();
            }
            else
            {
                _console.MarkupLine("[red]Error:[/] Specify file path, use - for stdin, or --inline");
                Environment.ExitCode = ExitCodes.ValidationError;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add task");
            _console.MarkupLine($"[red]Error:[/] {ex.Message}");
            Environment.ExitCode = ExitCodes.SystemError;
        }
    }
    
    private async Task AddInlineAsync()
    {
        if (string.IsNullOrWhiteSpace(Title))
        {
            _console.MarkupLine("[red]Error:[/] --title is required for inline mode");
            Environment.ExitCode = ExitCodes.ValidationError;
            return;
        }
        
        if (string.IsNullOrWhiteSpace(Description))
        {
            _console.MarkupLine("[red]Error:[/] --description is required for inline mode");
            Environment.ExitCode = ExitCodes.ValidationError;
            return;
        }
        
        var spec = new TaskSpec
        {
            Id = TaskId.New(),
            Title = Title,
            Description = Description,
            Priority = Priority,
            Tags = Tags?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToList() ?? new List<string>(),
            Files = Files?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToList() ?? new List<string>(),
            Dependencies = DependsOn?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(id => new TaskId(id))
                .ToList() ?? new List<TaskId>(),
            Status = TaskStatus.Pending,
            CreatedAt = DateTimeOffset.UtcNow
        };
        
        await ProcessTaskAsync(spec);
    }
    
    private async Task AddFromStdinAsync()
    {
        using var reader = new StreamReader(Console.OpenStandardInput());
        var content = await reader.ReadToEndAsync();
        await ParseAndAddAsync(content, "stdin");
    }
    
    private async Task AddFromFileAsync()
    {
        if (!File.Exists(FilePath))
        {
            _console.MarkupLine($"[red]Error:[/] File not found: {FilePath}");
            Environment.ExitCode = ExitCodes.ValidationError;
            return;
        }
        
        var content = await File.ReadAllTextAsync(FilePath!);
        await ParseAndAddAsync(content, FilePath!);
    }
    
    private async Task AddBatchAsync()
    {
        if (!File.Exists(FilePath))
        {
            _console.MarkupLine($"[red]Error:[/] File not found: {FilePath}");
            Environment.ExitCode = ExitCodes.ValidationError;
            return;
        }
        
        var content = await File.ReadAllTextAsync(FilePath!);
        var parseResult = _parser.ParseBatch(content, FilePath!);
        
        if (!parseResult.IsSuccess)
        {
            var errorOutput = _errorFormatter.Format(parseResult.Errors);
            _console.MarkupLine(errorOutput);
            Environment.ExitCode = ExitCodes.ValidationError;
            return;
        }
        
        var results = new List<BatchTaskResult>();
        var errors = 0;
        
        foreach (var spec in parseResult.Tasks)
        {
            var validationResult = await _validator.ValidateAsync(spec);
            
            if (!validationResult.IsValid)
            {
                errors++;
                results.Add(new BatchTaskResult(spec.Id, false, validationResult.Errors.First().Message));
                
                if (!ContinueOnError)
                {
                    _console.MarkupLine($"[red]Error:[/] Validation failed, stopping batch");
                    break;
                }
                continue;
            }
            
            if (!DryRun)
            {
                await _queue.EnqueueAsync(spec);
            }
            
            results.Add(new BatchTaskResult(spec.Id, true, null));
        }
        
        OutputBatchResults(results, errors);
    }
    
    private async Task ParseAndAddAsync(string content, string sourceName)
    {
        var parseResult = _parser.Parse(content, sourceName);
        
        if (!parseResult.IsSuccess)
        {
            var errorOutput = _errorFormatter.Format(parseResult.Errors);
            _console.MarkupLine(errorOutput);
            Environment.ExitCode = ExitCodes.ValidationError;
            return;
        }
        
        await ProcessTaskAsync(parseResult.Task!);
    }
    
    private async Task ProcessTaskAsync(TaskSpec spec)
    {
        // Validate
        var validationResult = await _validator.ValidateAsync(spec);
        
        if (!validationResult.IsValid)
        {
            var errorOutput = _errorFormatter.Format(validationResult.Errors);
            _console.MarkupLine(errorOutput);
            Environment.ExitCode = ExitCodes.ValidationError;
            return;
        }
        
        if (DryRun)
        {
            OutputDryRunResult(spec);
            return;
        }
        
        // Enqueue
        await _queue.EnqueueAsync(spec);
        _logger.LogInformation("Task {TaskId} added to queue", spec.Id);
        
        OutputResult(spec.Id);
    }
    
    private void OutputResult(TaskId id)
    {
        if (Json)
        {
            var result = new { id = id.Value, status = "added" };
            _console.WriteLine(JsonSerializer.Serialize(result));
        }
        else if (Quiet)
        {
            _console.WriteLine(id.Value);
        }
        else
        {
            _console.MarkupLine($"[green]✓[/] Task added: [bold]{id.Value}[/]");
        }
    }
    
    private void OutputDryRunResult(TaskSpec spec)
    {
        if (Json)
        {
            var result = new { id = spec.Id.Value, status = "valid", dryRun = true };
            _console.WriteLine(JsonSerializer.Serialize(result));
        }
        else
        {
            _console.MarkupLine($"[blue]ℹ[/] Dry run: Task [bold]{spec.Id.Value}[/] is valid");
        }
    }
    
    private void OutputBatchResults(List<BatchTaskResult> results, int errors)
    {
        var successes = results.Count(r => r.Success);
        
        if (Json)
        {
            _console.WriteLine(JsonSerializer.Serialize(new
            {
                total = results.Count,
                succeeded = successes,
                failed = errors,
                results
            }));
        }
        else
        {
            _console.MarkupLine($"Batch complete: [green]{successes}[/] added, [red]{errors}[/] failed");
        }
        
        if (errors > 0)
            Environment.ExitCode = ExitCodes.ValidationError;
    }
    
    private record BatchTaskResult(TaskId Id, bool Success, string? Error);
}
```

### Task List Command

```csharp
// Acode.Cli/Commands/Task/TaskListCommand.cs
namespace Acode.Cli.Commands.Task;

[Command("task list", Description = "List tasks")]
public sealed class TaskListCommand : ICommand
{
    private readonly ITaskQueue _queue;
    private readonly IAnsiConsole _console;
    
    [CommandOption("--status|-s", Description = "Filter by status")]
    public string? Status { get; init; }
    
    [CommandOption("--priority|-p", Description = "Filter by priority (comma-separated)")]
    public string? Priority { get; init; }
    
    [CommandOption("--tag|-t", Description = "Filter by tag")]
    public string? Tag { get; init; }
    
    [CommandOption("--since", Description = "Created after (ISO 8601)")]
    public string? Since { get; init; }
    
    [CommandOption("--until", Description = "Created before (ISO 8601)")]
    public string? Until { get; init; }
    
    [CommandOption("--limit|-l", Description = "Maximum results")]
    public int Limit { get; init; } = 50;
    
    [CommandOption("--offset", Description = "Skip results")]
    public int Offset { get; init; } = 0;
    
    [CommandOption("--sort", Description = "Sort by: created, priority, status")]
    public string Sort { get; init; } = "created";
    
    [CommandOption("--reverse|-r", Description = "Reverse sort order")]
    public bool Reverse { get; init; }
    
    [CommandOption("--json", Description = "Output as JSON")]
    public bool Json { get; init; }
    
    [CommandOption("--csv", Description = "Output as CSV")]
    public bool Csv { get; init; }
    
    [CommandOption("--quiet|-q", Description = "Output IDs only")]
    public bool Quiet { get; init; }
    
    [CommandOption("--verbose|-v", Description = "Show all fields")]
    public bool Verbose { get; init; }
    
    [CommandOption("--watch|-w", Description = "Watch mode")]
    public bool Watch { get; init; }
    
    [CommandOption("--interval", Description = "Watch interval (seconds)")]
    public int WatchInterval { get; init; } = 2;
    
    public TaskListCommand(ITaskQueue queue, IAnsiConsole console)
    {
        _queue = queue;
        _console = console;
    }
    
    public async ValueTask ExecuteAsync(IConsole console)
    {
        try
        {
            var filter = BuildFilter();
            
            if (Watch)
            {
                await WatchAsync(filter);
            }
            else
            {
                var tasks = await _queue.ListAsync(filter);
                OutputTasks(tasks);
            }
        }
        catch (Exception ex)
        {
            _console.MarkupLine($"[red]Error:[/] {ex.Message}");
            Environment.ExitCode = ExitCodes.SystemError;
        }
    }
    
    private QueueFilter BuildFilter()
    {
        var filter = new QueueFilter
        {
            Limit = Limit,
            Offset = Offset,
            SortBy = Sort switch
            {
                "priority" => QueueSortField.Priority,
                "status" => QueueSortField.Status,
                _ => QueueSortField.CreatedAt
            },
            SortDescending = Reverse
        };
        
        if (!string.IsNullOrEmpty(Status))
        {
            filter.Statuses = Status
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(s => Enum.Parse<TaskStatus>(s, ignoreCase: true))
                .ToList();
        }
        
        if (!string.IsNullOrEmpty(Priority))
        {
            filter.Priorities = Priority
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(int.Parse)
                .ToList();
        }
        
        if (!string.IsNullOrEmpty(Tag))
        {
            filter.Tags = Tag.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList();
        }
        
        if (!string.IsNullOrEmpty(Since) && DateTimeOffset.TryParse(Since, out var since))
        {
            filter.CreatedAfter = since;
        }
        
        if (!string.IsNullOrEmpty(Until) && DateTimeOffset.TryParse(Until, out var until))
        {
            filter.CreatedBefore = until;
        }
        
        return filter;
    }
    
    private void OutputTasks(IReadOnlyList<QueuedTask> tasks)
    {
        if (tasks.Count == 0)
        {
            if (!Quiet && !Json && !Csv)
                _console.MarkupLine("[grey]No tasks found[/]");
            return;
        }
        
        if (Json)
        {
            var json = JsonSerializer.Serialize(tasks.Select(t => new
            {
                id = t.Spec.Id.Value,
                title = t.Spec.Title,
                status = t.Status.ToString().ToLowerInvariant(),
                priority = t.Spec.Priority,
                createdAt = t.CreatedAt,
                attempts = t.AttemptCount
            }));
            _console.WriteLine(json);
            return;
        }
        
        if (Csv)
        {
            _console.WriteLine("id,title,status,priority,created_at");
            foreach (var task in tasks)
            {
                _console.WriteLine($"{task.Spec.Id.Value},{EscapeCsv(task.Spec.Title)},{task.Status},{task.Spec.Priority},{task.CreatedAt:O}");
            }
            return;
        }
        
        if (Quiet)
        {
            foreach (var task in tasks)
                _console.WriteLine(task.Spec.Id.Value);
            return;
        }
        
        // Table output
        var table = new Table();
        table.AddColumn("ID");
        table.AddColumn("Title");
        table.AddColumn("Status");
        table.AddColumn("Priority");
        
        if (Verbose)
        {
            table.AddColumn("Tags");
            table.AddColumn("Attempts");
            table.AddColumn("Created");
        }
        
        foreach (var task in tasks)
        {
            var statusColor = task.Status switch
            {
                TaskStatus.Pending => "grey",
                TaskStatus.Running => "yellow",
                TaskStatus.Completed => "green",
                TaskStatus.Failed => "red",
                TaskStatus.Cancelled => "orange3",
                TaskStatus.Blocked => "blue",
                _ => "white"
            };
            
            var row = new List<string>
            {
                task.Spec.Id.Value[..8] + "...",
                Truncate(task.Spec.Title, 40),
                $"[{statusColor}]{task.Status}[/]",
                task.Spec.Priority.ToString()
            };
            
            if (Verbose)
            {
                row.Add(string.Join(", ", task.Spec.Tags.Take(3)));
                row.Add(task.AttemptCount.ToString());
                row.Add(task.CreatedAt.ToString("g"));
            }
            
            table.AddRow(row.ToArray());
        }
        
        _console.Write(table);
        _console.MarkupLine($"\n[grey]Showing {tasks.Count} task(s)[/]");
    }
    
    private async Task WatchAsync(QueueFilter filter)
    {
        var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true;
            cts.Cancel();
        };
        
        while (!cts.Token.IsCancellationRequested)
        {
            Console.Clear();
            var tasks = await _queue.ListAsync(filter, cts.Token);
            OutputTasks(tasks);
            _console.MarkupLine($"\n[grey]Refreshing every {WatchInterval}s. Press Ctrl+C to stop.[/]");
            
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(WatchInterval), cts.Token);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
        
        Environment.ExitCode = ExitCodes.Interrupted;
    }
    
    private static string Truncate(string text, int maxLength) =>
        text.Length <= maxLength ? text : text[..(maxLength - 3)] + "...";
    
    private static string EscapeCsv(string value) =>
        value.Contains(',') || value.Contains('"') ? $"\"{value.Replace("\"", "\"\"")}\"" : value;
}
```

### Task Show Command

```csharp
// Acode.Cli/Commands/Task/TaskShowCommand.cs
namespace Acode.Cli.Commands.Task;

[Command("task show", Description = "Show task details")]
public sealed class TaskShowCommand : ICommand
{
    private readonly ITaskQueue _queue;
    private readonly IAnsiConsole _console;
    
    [CommandArgument(0, "<id>", Description = "Task ID (ULID)")]
    public string Id { get; init; } = string.Empty;
    
    [CommandOption("--json", Description = "Output as JSON")]
    public bool Json { get; init; }
    
    [CommandOption("--yaml", Description = "Output as YAML")]
    public bool Yaml { get; init; }
    
    [CommandOption("--full", Description = "Show untruncated output")]
    public bool Full { get; init; }
    
    public TaskShowCommand(ITaskQueue queue, IAnsiConsole console)
    {
        _queue = queue;
        _console = console;
    }
    
    public async ValueTask ExecuteAsync(IConsole console)
    {
        try
        {
            if (!TaskId.TryParse(Id, out var taskId))
            {
                _console.MarkupLine($"[red]Error:[/] Invalid task ID format: {Id}");
                Environment.ExitCode = ExitCodes.ValidationError;
                return;
            }
            
            var task = await _queue.GetAsync(taskId);
            
            if (task == null)
            {
                _console.MarkupLine($"[red]Error:[/] Task not found: {Id}");
                await SuggestSimilarAsync(Id);
                Environment.ExitCode = ExitCodes.NotFound;
                return;
            }
            
            OutputTask(task);
        }
        catch (Exception ex)
        {
            _console.MarkupLine($"[red]Error:[/] {ex.Message}");
            Environment.ExitCode = ExitCodes.SystemError;
        }
    }
    
    private void OutputTask(QueuedTask task)
    {
        if (Json)
        {
            var json = JsonSerializer.Serialize(task, new JsonSerializerOptions { WriteIndented = true });
            _console.WriteLine(json);
            return;
        }
        
        if (Yaml)
        {
            var serializer = new YamlDotNet.Serialization.Serializer();
            var yaml = serializer.Serialize(task);
            _console.WriteLine(yaml);
            return;
        }
        
        // Human-readable output
        var statusColor = task.Status switch
        {
            TaskStatus.Completed => "green",
            TaskStatus.Failed => "red",
            TaskStatus.Running => "yellow",
            TaskStatus.Cancelled => "orange3",
            TaskStatus.Blocked => "blue",
            _ => "grey"
        };
        
        _console.MarkupLine($"[bold]Task:[/] {task.Spec.Id.Value}");
        _console.MarkupLine($"[bold]Title:[/] {task.Spec.Title}");
        _console.MarkupLine($"[bold]Status:[/] [{statusColor}]{task.Status}[/]");
        _console.MarkupLine($"[bold]Priority:[/] {task.Spec.Priority}");
        _console.WriteLine();
        
        _console.MarkupLine("[bold]Description:[/]");
        var desc = Full ? task.Spec.Description : Truncate(task.Spec.Description, 500);
        _console.WriteLine(desc);
        _console.WriteLine();
        
        if (task.Spec.Tags.Count > 0)
        {
            _console.MarkupLine($"[bold]Tags:[/] {string.Join(", ", task.Spec.Tags)}");
        }
        
        if (task.Spec.Files.Count > 0)
        {
            _console.MarkupLine($"[bold]Files:[/] {task.Spec.Files.Count} file(s)");
            foreach (var file in task.Spec.Files.Take(Full ? int.MaxValue : 10))
            {
                _console.MarkupLine($"  - {file}");
            }
            if (!Full && task.Spec.Files.Count > 10)
                _console.MarkupLine($"  [grey]...and {task.Spec.Files.Count - 10} more[/]");
        }
        
        if (task.Spec.Dependencies.Count > 0)
        {
            _console.MarkupLine($"[bold]Dependencies:[/]");
            foreach (var dep in task.Spec.Dependencies)
            {
                _console.MarkupLine($"  - {dep.Value}");
            }
        }
        
        _console.WriteLine();
        _console.MarkupLine("[bold]Execution:[/]");
        _console.MarkupLine($"  Attempts: {task.AttemptCount}/{task.Spec.RetryLimit}");
        _console.MarkupLine($"  Created: {task.CreatedAt:G}");
        
        if (task.StartedAt.HasValue)
            _console.MarkupLine($"  Started: {task.StartedAt:G}");
        
        if (task.CompletedAt.HasValue)
            _console.MarkupLine($"  Completed: {task.CompletedAt:G}");
        
        if (task.WorkerId.HasValue)
            _console.MarkupLine($"  Worker: {task.WorkerId.Value}");
        
        if (task.LastError != null)
        {
            _console.WriteLine();
            _console.MarkupLine("[bold red]Last Error:[/]");
            _console.MarkupLine(task.LastError);
        }
        
        if (task.TransitionHistory.Count > 0)
        {
            _console.WriteLine();
            _console.MarkupLine("[bold]History:[/]");
            foreach (var transition in task.TransitionHistory.TakeLast(Full ? int.MaxValue : 5))
            {
                _console.MarkupLine($"  {transition.Timestamp:G}: {transition.FromStatus} → {transition.ToStatus}");
            }
        }
    }
    
    private async Task SuggestSimilarAsync(string id)
    {
        var prefix = id.Length >= 4 ? id[..4] : id;
        var similar = await _queue.SearchByPrefixAsync(prefix, 3);
        
        if (similar.Count > 0)
        {
            _console.MarkupLine("\n[grey]Did you mean?[/]");
            foreach (var task in similar)
            {
                _console.MarkupLine($"  {task.Spec.Id.Value}  {task.Spec.Title}");
            }
        }
    }
    
    private static string Truncate(string text, int maxLength) =>
        text.Length <= maxLength ? text : text[..(maxLength - 3)] + "...";
}
```

### Task Retry Command

```csharp
// Acode.Cli/Commands/Task/TaskRetryCommand.cs
namespace Acode.Cli.Commands.Task;

[Command("task retry", Description = "Retry failed task")]
public sealed class TaskRetryCommand : ICommand
{
    private readonly ITaskQueue _queue;
    private readonly IAnsiConsole _console;
    private readonly ILogger<TaskRetryCommand> _logger;
    
    [CommandArgument(0, "<id>", Description = "Task ID (ULID)")]
    public string Id { get; init; } = string.Empty;
    
    [CommandOption("--reset-attempts", Description = "Reset attempt counter")]
    public bool ResetAttempts { get; init; }
    
    [CommandOption("--force", Description = "Retry even if over limit")]
    public bool Force { get; init; }
    
    [CommandOption("--json", Description = "Output as JSON")]
    public bool Json { get; init; }
    
    public TaskRetryCommand(ITaskQueue queue, IAnsiConsole console, ILogger<TaskRetryCommand> logger)
    {
        _queue = queue;
        _console = console;
        _logger = logger;
    }
    
    public async ValueTask ExecuteAsync(IConsole console)
    {
        try
        {
            if (!TaskId.TryParse(Id, out var taskId))
            {
                _console.MarkupLine($"[red]Error:[/] Invalid task ID format: {Id}");
                Environment.ExitCode = ExitCodes.ValidationError;
                return;
            }
            
            var task = await _queue.GetAsync(taskId);
            
            if (task == null)
            {
                _console.MarkupLine($"[red]Error:[/] Task not found: {Id}");
                Environment.ExitCode = ExitCodes.NotFound;
                return;
            }
            
            // Validate state
            var retryableStates = new[] { TaskStatus.Failed, TaskStatus.Cancelled };
            if (!retryableStates.Contains(task.Status))
            {
                _console.MarkupLine($"[red]Error:[/] Cannot retry task in '{task.Status}' state");
                _console.MarkupLine("[grey]Only failed or cancelled tasks can be retried[/]");
                Environment.ExitCode = ExitCodes.InvalidState;
                return;
            }
            
            // Check retry limit
            if (task.AttemptCount >= task.Spec.RetryLimit && !Force)
            {
                _console.MarkupLine($"[red]Error:[/] Task has reached retry limit ({task.AttemptCount}/{task.Spec.RetryLimit})");
                _console.MarkupLine("[grey]Use --force to retry anyway[/]");
                Environment.ExitCode = ExitCodes.InvalidState;
                return;
            }
            
            // Perform retry
            var options = new RetryOptions
            {
                ResetAttempts = ResetAttempts,
                Force = Force
            };
            
            await _queue.RetryAsync(taskId, options);
            _logger.LogInformation("Task {TaskId} retried", taskId);
            
            OutputResult(taskId, task.AttemptCount, ResetAttempts);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retry task {TaskId}", Id);
            _console.MarkupLine($"[red]Error:[/] {ex.Message}");
            Environment.ExitCode = ExitCodes.SystemError;
        }
    }
    
    private void OutputResult(TaskId id, int previousAttempts, bool wasReset)
    {
        if (Json)
        {
            var result = new
            {
                id = id.Value,
                status = "retried",
                previousAttempts,
                attemptsReset = wasReset
            };
            _console.WriteLine(JsonSerializer.Serialize(result));
        }
        else
        {
            _console.MarkupLine($"[green]✓[/] Task [bold]{id.Value}[/] queued for retry");
            if (wasReset)
                _console.MarkupLine("[grey]Attempt count reset to 0[/]");
        }
    }
}
```

### Task Cancel Command

```csharp
// Acode.Cli/Commands/Task/TaskCancelCommand.cs
namespace Acode.Cli.Commands.Task;

[Command("task cancel", Description = "Cancel task")]
public sealed class TaskCancelCommand : ICommand
{
    private readonly ITaskQueue _queue;
    private readonly IWorkerPool _workerPool;
    private readonly IAnsiConsole _console;
    private readonly ILogger<TaskCancelCommand> _logger;
    
    [CommandArgument(0, "<ids>", Description = "Task ID(s) - comma-separated")]
    public string Ids { get; init; } = string.Empty;
    
    [CommandOption("--force", Description = "Kill running worker")]
    public bool Force { get; init; }
    
    [CommandOption("--json", Description = "Output as JSON")]
    public bool Json { get; init; }
    
    public TaskCancelCommand(
        ITaskQueue queue,
        IWorkerPool workerPool,
        IAnsiConsole console,
        ILogger<TaskCancelCommand> logger)
    {
        _queue = queue;
        _workerPool = workerPool;
        _console = console;
        _logger = logger;
    }
    
    public async ValueTask ExecuteAsync(IConsole console)
    {
        var idList = Ids
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();
        
        if (idList.Count == 0)
        {
            _console.MarkupLine("[red]Error:[/] At least one task ID required");
            Environment.ExitCode = ExitCodes.ValidationError;
            return;
        }
        
        var results = new List<CancelResult>();
        
        foreach (var id in idList)
        {
            var result = await CancelTaskAsync(id);
            results.Add(result);
        }
        
        OutputResults(results);
        
        if (results.Any(r => !r.Success))
            Environment.ExitCode = ExitCodes.InvalidState;
    }
    
    private async Task<CancelResult> CancelTaskAsync(string id)
    {
        try
        {
            if (!TaskId.TryParse(id, out var taskId))
            {
                return new CancelResult(id, false, "Invalid ID format");
            }
            
            var task = await _queue.GetAsync(taskId);
            
            if (task == null)
            {
                return new CancelResult(id, false, "Not found");
            }
            
            // Validate state
            var cancelableStates = new[] { TaskStatus.Pending, TaskStatus.Running, TaskStatus.Blocked };
            if (!cancelableStates.Contains(task.Status))
            {
                return new CancelResult(id, false, $"Cannot cancel: {task.Status}");
            }
            
            // If running, signal worker
            if (task.Status == TaskStatus.Running)
            {
                if (Force && task.WorkerId.HasValue)
                {
                    await _workerPool.KillAsync(task.WorkerId.Value);
                    _logger.LogWarning("Force killed worker {WorkerId} for task {TaskId}", task.WorkerId, taskId);
                }
                else
                {
                    await _workerPool.CancelAsync(task.WorkerId!.Value);
                    _logger.LogInformation("Signaled cancellation for task {TaskId}", taskId);
                }
            }
            
            await _queue.CancelAsync(taskId);
            _logger.LogInformation("Task {TaskId} cancelled", taskId);
            
            return new CancelResult(id, true, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cancel task {TaskId}", id);
            return new CancelResult(id, false, ex.Message);
        }
    }
    
    private void OutputResults(List<CancelResult> results)
    {
        if (Json)
        {
            _console.WriteLine(JsonSerializer.Serialize(results));
            return;
        }
        
        foreach (var result in results)
        {
            if (result.Success)
            {
                _console.MarkupLine($"[green]✓[/] Cancelled: {result.Id}");
            }
            else
            {
                _console.MarkupLine($"[red]✗[/] Failed: {result.Id} - {result.Error}");
            }
        }
    }
    
    private record CancelResult(string Id, bool Success, string? Error);
}
```

### Implementation Checklist

- [ ] Create `TaskAddCommand` with file/stdin/inline modes
- [ ] Add batch processing support
- [ ] Create `TaskListCommand` with all filters
- [ ] Implement table, JSON, CSV formatters
- [ ] Add watch mode with Ctrl+C handling
- [ ] Create `TaskShowCommand` with full detail view
- [ ] Add similar task suggestion on not found
- [ ] Create `TaskRetryCommand` with state validation
- [ ] Add retry limit checking
- [ ] Create `TaskCancelCommand` with worker signaling
- [ ] Add bulk cancel support
- [ ] Implement proper exit codes
- [ ] Add comprehensive help text
- [ ] Register all commands in DI
- [ ] Write unit tests for each command
- [ ] Write integration tests for workflows

### Rollout Plan

1. **Phase 1: Add Command** (Day 1)
   - File and stdin input
   - Inline mode
   - Validation integration

2. **Phase 2: List Command** (Day 2)
   - Filter building
   - Output formatters
   - Pagination

3. **Phase 3: Show Command** (Day 2)
   - Detail formatting
   - History display
   - Similar suggestions

4. **Phase 4: Retry/Cancel** (Day 3)
   - State validation
   - Worker signaling
   - Force options

5. **Phase 5: Polish** (Day 4)
   - Tab completion
   - Help text
   - Exit codes

---

**End of Task 025.b Specification**