# Task 022.a: status/diff/log

**Priority:** P1 – High  
**Tier:** S – Core Infrastructure  
**Complexity:** 5 (Fibonacci points)  
**Phase:** Phase 5 – Git Integration Layer  
**Dependencies:** Task 022 (Git Tool Layer), Task 018 (Command Execution)  

---

## Description

Task 022.a implements Git status, diff, and log operations. These read-only operations provide repository state information. The agent MUST be able to query repository state before making changes.

The status operation MUST return current branch, clean/dirty state, and file change list. Each file MUST include its path and change type (untracked, modified, added, deleted, renamed). Submodule status MUST also be detected.

The diff operation MUST return differences between states. Supported comparisons MUST include working tree vs index, index vs HEAD, and arbitrary commit pairs. Output MUST be unified diff format. Stat summaries MUST also be available.

The log operation MUST return commit history. Commits MUST include SHA, author, date, and message. Filtering MUST support date ranges, author patterns, and path restrictions. Pagination MUST support large histories.

All operations MUST work in all operating modes (local-only, burst, airgapped). These are read-only operations that do NOT require network access.

Output parsing MUST be robust. Git's porcelain output formats MUST be used where available for reliable parsing. Unexpected output MUST NOT crash the parser but MUST be logged.

### Business Value

Repository state awareness enables intelligent agent behavior. Before modifying files, the agent checks for existing changes. Before committing, the agent verifies expected changes are staged. Log history enables finding related commits.

### Scope Boundaries

This task covers status, diff, and log operations only. Branch operations are in Task 022.b. Commit and push operations are in Task 022.c.

### Integration Points

- Task 022: IGitService interface definition
- Task 018: Command execution for running git commands
- Task 021: Artifact capture for diff output
- Task 016: Context building uses status information

### Failure Modes

- Not a git repository → Return NotARepositoryException
- Binary file in diff → Indicate binary, skip content
- Deleted file status → Handle "deleted" state correctly
- Renamed file → Detect rename, include old and new paths
- Large log history → Stream results, respect limits

---

## Assumptions

### Technical Assumptions
1. **Git installed** - Git 2.20+ available in PATH
2. **Porcelain output** - `--porcelain=v2` format is stable across git versions
3. **Repository initialized** - Working directory is valid git repository
4. **At least one commit** - Repository not empty (status/log/diff need commit history)
5. **UTF-8 encoding** - Commit messages and file paths use UTF-8
6. **Read permissions** - User has read access to .git/ directory and working tree
7. **No concurrent modifications** - No other processes modifying repository during read
8. **Reasonable repository size** - <100k files, <50k commits for performance targets
9. **Standard git config** - No exotic git configurations that alter output format
10. **Diff algorithm** - Default git diff algorithm (Myers) produces readable output

### Operational Assumptions
11. **Read-only operations** - Status/diff/log don't modify repository state
12. **No network required** - All operations work offline (local-only/airgapped modes)
13. **Filesystem responsive** - File system stat() calls complete in <10ms
14. **Clean merges** - No in-progress merges or rebases (or handle gracefully)
15. **No submodule complexity** - Submodules detected but not recursively analyzed

---

## Use Cases

### Use Case 1: DevBot Pre-Commit Status Check

**Persona:** DevBot modifies 8 files while implementing a feature. Before committing, it needs to verify all expected changes are staged and no unintended files were modified.

**Before (manual):** Developer runs `git status`, manually reviews 8 files listed, visually confirms each is expected (2 minutes).

**After (automated):** DevBot calls `GetStatusAsync()`, receives structured `GitStatus` with 8 `StagedFiles`. Programmatically verifies each file path matches expected list (200ms).

**ROI:** Automated verification prevents accidental commits. Saves 2 min/commit × 20 commits/day × 250 days/year = 10,000 minutes/year = 167 hours × $100/hour = **$16,700/year per developer**.

### Use Case 2: Jordan Diff Review Before Deployment

**Persona:** Jordan needs to review all code changes between current release (v1.2.0) and release candidate (v1.3.0-rc1) before production deployment.

**Before (manual):** Jordan runs `git diff v1.2.0 v1.3.0-rc1`, manually scrolls through 5,000-line diff output, visually inspects for suspicious changes (45 minutes per release).

**After (automated):** Agent calls `GetDiffAsync(diffOptions)`, receives diff as string. Automatically scans for security patterns (hardcoded credentials, backdoors), highlights risky changes for human review (5 minutes automated + 10 minutes human review).

**ROI:** Automated diff scanning reduces review time by 67% (45 min → 15 min). 12 releases/year × 30 min saved = 360 min = 6 hours × $120/hour = **$720/year per release engineer**.

### Use Case 3: Alex Automated Release Notes from Git Log

**Persona:** Alex generates release notes every sprint by reviewing commit history and categorizing changes (features, fixes, breaking changes).

**Before (manual):** Alex runs `git log --since="2 weeks ago"`, copies 50 commits to spreadsheet, manually categorizes each commit by reading message and diff (2 hours per sprint).

**After (automated):** Agent calls `GetLogAsync(logOptions)`, receives structured `GitCommit[]`. Automatically categorizes commits using conventional commit format (feat:, fix:, BREAKING:), generates markdown release notes (5 minutes automated).

**ROI:** Automated release notes save 115 minutes/sprint. 26 sprints/year × 115 min = 2,990 min = 50 hours × $120/hour = **$6,000/year per product manager**.

**Combined ROI:** $16,700 (DevBot) + $720 (Jordan) + $6,000 (Alex) = **$23,420/year per engineering team**.

---

## Glossary / Terms

| Term | Definition |
|------|------------|
| Status | Current state of working tree and index |
| Index | Staging area for next commit |
| Working Tree | Files on disk in repository |
| Diff | Line-by-line comparison between states |
| Unified Diff | Standard diff format with +/- prefixes |
| Stat | Summary showing files changed and lines added/removed |
| Porcelain | Machine-readable git output format |
| HEAD | Reference to current branch tip |
| Commit | Snapshot of repository state |
| SHA | 40-character commit identifier |
| Short SHA | 7-character abbreviated commit ID |
| Log | Chronological list of commits |

---

## Out of Scope

- Branch creation or switching (Task 022.b)
- Staging or committing (Task 022.c)
- Push or fetch operations
- Merge operations
- Interactive diff viewing
- Blame/annotate operations
- Stash inspection
- Tag operations
- Submodule deep inspection

---

## Functional Requirements

### FR-001 to FR-020: Status Operation

- FR-001: `GetStatusAsync` MUST return `GitStatus` object
- FR-002: Status MUST include current branch name
- FR-003: Status MUST include `IsClean` boolean
- FR-004: Status MUST include list of changed files
- FR-005: Each file MUST have path relative to repo root
- FR-006: Each file MUST have state enum value
- FR-007: Untracked files MUST be included
- FR-008: Modified files MUST be included
- FR-009: Added (staged) files MUST be included
- FR-010: Deleted files MUST be included
- FR-011: Renamed files MUST include old and new paths
- FR-012: Copied files MUST be detected
- FR-013: Status MUST use `git status --porcelain=v2`
- FR-014: Detached HEAD MUST be indicated
- FR-015: Status MUST include ahead/behind counts if tracking
- FR-016: Submodule changes MUST be detected
- FR-017: Ignored files MUST NOT be included by default
- FR-018: `--include-ignored` MUST include ignored files
- FR-019: Status MUST work on worktrees
- FR-020: Status MUST handle empty repository

### FR-021 to FR-040: Diff Operation

- FR-021: `GetDiffAsync` MUST accept `DiffOptions`
- FR-022: DiffOptions MUST include source reference
- FR-023: DiffOptions MUST include target reference
- FR-024: Default source MUST be working tree
- FR-025: Default target MUST be index
- FR-026: `--cached` MUST diff index vs HEAD
- FR-027: `--head` MUST diff working tree vs HEAD
- FR-028: Arbitrary refs MUST be supported
- FR-029: Path filtering MUST be supported
- FR-030: Output MUST be unified diff format
- FR-031: `--stat` option MUST return stat summary
- FR-032: `--numstat` MUST return machine-readable stats
- FR-033: Binary files MUST be indicated, not diff'd
- FR-034: Rename detection MUST be enabled
- FR-035: Context lines MUST be configurable
- FR-036: Default context MUST be 3 lines
- FR-037: Whitespace options MUST be supported
- FR-038: `--ignore-space-change` MUST be supported
- FR-039: Large diffs MUST stream output
- FR-040: Empty diff MUST return empty string, not error

### FR-041 to FR-060: Log Operation

- FR-041: `GetLogAsync` MUST accept `LogOptions`
- FR-042: LogOptions MUST include limit count
- FR-043: LogOptions MUST include since date
- FR-044: LogOptions MUST include until date
- FR-045: LogOptions MUST include author filter
- FR-046: LogOptions MUST include path filter
- FR-047: LogOptions MUST include grep message filter
- FR-048: Default limit MUST be 100 commits
- FR-049: Each commit MUST include full SHA
- FR-050: Each commit MUST include short SHA (7 chars)
- FR-051: Each commit MUST include author name
- FR-052: Each commit MUST include author email
- FR-053: Each commit MUST include commit date
- FR-054: Each commit MUST include subject line
- FR-055: Each commit MUST include body (optional)
- FR-056: Log MUST use `--format` for parsing
- FR-057: Parent SHAs MUST be included
- FR-058: Merge commits MUST be identified
- FR-059: `--first-parent` MUST be supported
- FR-060: `--no-merges` MUST be supported

---

## Non-Functional Requirements

### NFR-001 to NFR-010: Performance

- NFR-001: Status MUST complete in <500ms for repos <10000 files
- NFR-002: Status MUST complete in <2s for repos <50000 files
- NFR-003: Diff MUST complete in <1s for diffs <1000 lines
- NFR-004: Diff MUST stream for outputs >10MB
- NFR-005: Log MUST complete in <500ms for 100 commits
- NFR-006: Log MUST complete in <2s for 1000 commits
- NFR-007: Memory MUST NOT exceed 50MB for status
- NFR-008: Memory MUST NOT exceed 100MB for large diffs
- NFR-009: Parsing MUST NOT require full output buffering
- NFR-010: Parallel status calls MUST NOT deadlock

### NFR-011 to NFR-020: Reliability

- NFR-011: Malformed porcelain output MUST be handled
- NFR-012: Parsing errors MUST log details
- NFR-013: Partial output MUST be returned on timeout
- NFR-014: Cancelled operations MUST cleanup
- NFR-015: File locks MUST NOT hang indefinitely
- NFR-016: Retry MUST NOT occur for read operations
- NFR-017: Operation timeout MUST be configurable
- NFR-018: Default timeout MUST be 60 seconds
- NFR-019: Empty repository MUST return valid empty results
- NFR-020: Corrupted index MUST produce clear error

### NFR-021 to NFR-025: Security

- NFR-021: Path arguments MUST be sanitized
- NFR-022: No credentials in status/diff/log output
- NFR-023: Remote URLs MUST NOT be logged with credentials
- NFR-024: File content MUST NOT be logged
- NFR-025: Shell injection MUST be prevented

---

## User Manual Documentation

### Quick Start

```bash
# Check repository status
acode git status

# View working tree changes
acode git diff

# View staged changes
acode git diff --cached

# View commit history
acode git log --limit 10
```

### Status Command

```bash
acode git status [options]

Options:
  --include-ignored     Include ignored files
  --format FORMAT       Output format: text (default), json
```

**Output:**

```
On branch: main
Status: clean

Changes:
  M  src/file.cs          (modified)
  A  src/new.cs           (staged)
  ?  untracked.txt        (untracked)
```

**JSON Output:**

```json
{
  "branch": "main",
  "isClean": false,
  "files": [
    {"path": "src/file.cs", "state": "Modified"},
    {"path": "src/new.cs", "state": "Added"}
  ]
}
```

### Diff Command

```bash
acode git diff [options] [ref1] [ref2] [-- paths...]

Options:
  --cached              Diff staged changes vs HEAD
  --head                Diff working tree vs HEAD
  --stat                Show stat summary only
  --numstat             Show machine-readable stats
  --context N           Lines of context (default: 3)
  --ignore-space-change Ignore whitespace changes
  --format FORMAT       Output format: text (default), json
```

**Examples:**

```bash
# Working tree vs index
acode git diff

# Staged vs HEAD
acode git diff --cached

# Working tree vs HEAD
acode git diff --head

# Between commits
acode git diff abc123 def456

# Specific file
acode git diff -- src/file.cs

# Stat summary
acode git diff --stat
```

### Log Command

```bash
acode git log [options]

Options:
  --limit N             Maximum commits (default: 100)
  --since DATE          Commits after this date
  --until DATE          Commits before this date
  --author PATTERN      Filter by author
  --grep PATTERN        Filter by message
  --path PATH           Filter by file path
  --first-parent        Follow only first parent
  --no-merges           Exclude merge commits
  --format FORMAT       Output format: text (default), json
```

**Examples:**

```bash
# Recent commits
acode git log --limit 20

# Last week's commits
acode git log --since "1 week ago"

# By author
acode git log --author "john"

# Affecting a file
acode git log --path src/file.cs
```

### Troubleshooting

**Q: Status shows unexpected files**

Check your `.gitignore` patterns. Untracked files may need to be ignored.

**Q: Diff output is empty but status shows changes**

Changes may be staged. Use `acode git diff --cached` to see staged changes.

**Q: Log is slow on large repositories**

Use `--limit` to restrict results:
```bash
acode git log --limit 50
```

---

## Acceptance Criteria / Definition of Done

### Status Functionality

- [ ] AC-001: `GetStatusAsync` returns GitStatus
- [ ] AC-002: Branch name correctly identified
- [ ] AC-003: IsClean true when no changes
- [ ] AC-004: IsClean false when changes exist
- [ ] AC-005: Untracked files included
- [ ] AC-006: Modified files included
- [ ] AC-007: Staged files included
- [ ] AC-008: Deleted files included
- [ ] AC-009: Renamed files detected
- [ ] AC-010: Detached HEAD handled
- [ ] AC-011: Ahead/behind counts included

### Diff Functionality

- [ ] AC-012: `GetDiffAsync` returns diff string
- [ ] AC-013: Working tree vs index works
- [ ] AC-014: Index vs HEAD works
- [ ] AC-015: Commit vs commit works
- [ ] AC-016: Path filtering works
- [ ] AC-017: Stat output works
- [ ] AC-018: Binary files indicated
- [ ] AC-019: Empty diff returns empty string

### Log Functionality

- [ ] AC-020: `GetLogAsync` returns commit list
- [ ] AC-021: Limit option works
- [ ] AC-022: Since date filter works
- [ ] AC-023: Until date filter works
- [ ] AC-024: Author filter works
- [ ] AC-025: Path filter works
- [ ] AC-026: Grep filter works
- [ ] AC-027: First-parent option works
- [ ] AC-028: No-merges option works

### CLI

- [ ] AC-029: `acode git status` works
- [ ] AC-030: `acode git diff` works
- [ ] AC-031: `acode git log` works
- [ ] AC-032: JSON format output works
- [ ] AC-033: Exit codes correct

### Performance

- [ ] AC-034: Status <500ms for 10000 files
- [ ] AC-035: Log <500ms for 100 commits
- [ ] AC-036: Memory under limits

---

## Security Considerations

### Threat 1: Path Injection in Diff Operations

**Risk:** If file paths from user input are passed to `git diff` without validation, attacker can use path traversal to read files outside repository.

**Attack Scenario:** Attacker provides path `../../../../etc/passwd` to diff operation, git attempts to diff system file, error message reveals file contents.

**Mitigation:**

```csharp
public async Task<string> GetDiffAsync(string workingDir, DiffOptions options, CancellationToken ct = default)
{
    await EnsureRepositoryAsync(workingDir, ct);

    // Validate all paths before passing to git
    if (options.Paths != null)
    {
        foreach (var path in options.Paths)
        {
            var sanitized = _pathSanitizer.SanitizePath(path);
            var normalized = Path.GetFullPath(Path.Combine(workingDir, sanitized));
            if (!normalized.StartsWith(workingDir, StringComparison.Ordinal))
                throw new GitException($"Path escapes repository: {path}", 0, null, workingDir, "GIT_INVALID_PATH");
        }
    }

    // Safe to proceed with validated paths
    var args = new List<string> { "diff" };
    if (options.Paths != null)
        args.AddRange(options.Paths.Select(p => _pathSanitizer.SanitizePath(p)));

    var result = await ExecuteGitAsync(workingDir, args, ct);
    return _diffRedactor.RedactDiff(result.StdOut);
}
```

### Threat 2: Information Disclosure via Verbose Log Output

**Risk:** Git log output may contain sensitive information in commit messages (issue numbers, internal project names, email addresses). If logged without redaction, this information is exposed.

**Attack Scenario:** Agent logs full commit history including messages like "Fixed prod password: admin123". Logs are sent to external aggregator, attacker extracts credentials.

**Mitigation:**

```csharp
public async Task<IReadOnlyList<GitCommit>> GetLogAsync(string workingDir, LogOptions options, CancellationToken ct = default)
{
    await EnsureRepositoryAsync(workingDir, ct);

    var result = await ExecuteGitAsync(workingDir, BuildLogArgs(options), ct);
    var commits = _parser.ParseLog(result.StdOut, LogFormat);

    // Redact sensitive patterns from commit messages and bodies
    var redactedCommits = commits.Select(c => new GitCommit
    {
        Sha = c.Sha,
        ShortSha = c.ShortSha,
        Author = c.Author,
        AuthorEmail = RedactEmail(c.AuthorEmail), // mask@domain.com
        AuthorDate = c.AuthorDate,
        Committer = c.Committer,
        CommitterEmail = RedactEmail(c.CommitterEmail),
        CommitDate = c.CommitDate,
        Subject = _messageRedactor.RedactSensitivePatterns(c.Subject),
        Body = c.Body != null ? _messageRedactor.RedactSensitivePatterns(c.Body) : null,
        ParentShas = c.ParentShas
    }).ToList();

    return redactedCommits;
}

private string RedactEmail(string email)
{
    if (string.IsNullOrEmpty(email)) return email;
    var parts = email.Split('@');
    if (parts.Length != 2) return email;
    var localPart = parts[0].Length > 2 ? parts[0][..2] + "***" : "***";
    return $"{localPart}@{parts[1]}";
}
```

---

## Troubleshooting

### Issue 1: Status Shows Hundreds of Untracked Files Slowing Performance

**Symptoms:**
- `GetStatusAsync()` takes 3-5 seconds to complete
- Repository has many untracked files (node_modules/, build/, .idea/)
- Status output lists hundreds of untracked files
- Performance acceptable on command-line git but slow via Acode

**Solutions:**

```bash
# Solution 1: Add directories to .gitignore
echo "node_modules/" >> .gitignore
echo "build/" >> .gitignore
echo ".idea/" >> .gitignore
git add .gitignore
git commit -m "chore: ignore untracked directories"

# Solution 2: Use --untracked-files=no flag in GitService
# Modify GetStatusAsync to add option for skipping untracked files
args.Add("--untracked-files=no");

# Solution 3: Clean untracked files if not needed
git clean -fdx  # Remove all untracked files (DANGEROUS)
git clean -fdn  # Dry run first to see what would be deleted
```

### Issue 2: Diff Output Truncated for Large Files

**Symptoms:**
- Diff for large file (>10MB) returns incomplete output
- Error message: "diff too large"
- Binary files shown as "Binary files differ" with no content

**Solutions:**

```bash
# Solution 1: Use --stat for summary instead of full diff
var args = new List<string> { "diff", "--stat" };
# Returns file list with line counts, not full content

# Solution 2: Set diff size limits
git config diff.renameLimit 1000
git config diff.renames true

# Solution 3: Stream large diffs in chunks
# Implement streaming parser instead of loading full output to memory
```

### Issue 3: Log Operation Missing Recent Commits

**Symptoms:**
- `GetLogAsync()` returns commits but missing most recent ones
- Command-line `git log` shows all commits
- Only returns commits from specific branch

**Solutions:**

```bash
# Solution 1: Verify HEAD is up to date
git fetch origin
git log origin/main  # Shows remote commits

# Solution 2: Use --all flag to see all branches
var args = new List<string> { "log", "--all", "--format=..." };

# Solution 3: Check if detached HEAD
git branch  # Shows current branch or "(HEAD detached at ...)"
git checkout main  # Switch to main branch
```

---

## Best Practices

### Status Operations

1. **Use porcelain v1/v2** - Stable parsing format for status
2. **Handle staged vs unstaged** - Track both index and working tree changes
3. **Detect renames** - Include rename detection for accurate status
4. **Batch status checks** - One status call, not per-file

### Diff Operations

5. **Use unified diff format** - Standard patch format for interoperability
6. **Include context lines** - 3 lines of context by default
7. **Handle binary files** - Detect and skip binary content
8. **Support filtering** - Allow diff for specific files or paths

### Log Operations

9. **Use structured format** - --format= with delimiters for parsing
10. **Limit by default** - Don't fetch entire history without limit
11. **Include author and date** - Essential metadata for commits
12. **Support range queries** - Allow rev1..rev2 syntax

---

## Testing Requirements

### Unit Tests

- [ ] UT-001: Test GitStatus parsing from porcelain output
- [ ] UT-002: Test file state enum mapping
- [ ] UT-003: Test detached HEAD detection
- [ ] UT-004: Test rename parsing
- [ ] UT-005: Test diff output parsing
- [ ] UT-006: Test stat parsing
- [ ] UT-007: Test commit parsing from log
- [ ] UT-008: Test date parsing
- [ ] UT-009: Test author parsing
- [ ] UT-010: Test merge commit detection

### Integration Tests

- [ ] IT-001: Status on real repository
- [ ] IT-002: Status with various file states
- [ ] IT-003: Diff on real changes
- [ ] IT-004: Diff with path filter
- [ ] IT-005: Log on real history
- [ ] IT-006: Log with filters
- [ ] IT-007: Empty repository handling
- [ ] IT-008: Large repository handling

### End-to-End Tests

- [ ] E2E-001: CLI status command
- [ ] E2E-002: CLI diff command
- [ ] E2E-003: CLI log command
- [ ] E2E-004: JSON output format
- [ ] E2E-005: Error handling

### Performance/Benchmarks

- [ ] PB-001: Status 10000 files in <500ms
- [ ] PB-002: Log 1000 commits in <2s
- [ ] PB-003: Diff 10MB in <3s

### Regression

- [ ] RG-001: Task 022 interface compatibility
- [ ] RG-002: Task 018 command execution

---

## User Verification Steps

1. **Verify status on clean repo:**
   ```bash
   acode git status
   ```
   Verify: Shows "Status: clean"

2. **Verify status with changes:**
   ```bash
   echo "test" >> file.txt
   acode git status
   ```
   Verify: Shows file.txt as modified

3. **Verify diff shows changes:**
   ```bash
   acode git diff
   ```
   Verify: Shows +test line

4. **Verify log shows history:**
   ```bash
   acode git log --limit 5
   ```
   Verify: Shows 5 commits

5. **Verify JSON output:**
   ```bash
   acode git status --format json
   ```
   Verify: Valid JSON returned

---

## Implementation Prompt

### File Structure

```
src/
├── Acode.Domain/
│   └── Git/
│       ├── GitStatus.cs
│       ├── FileStatus.cs
│       ├── GitFileState.cs
│       ├── GitCommit.cs
│       ├── DiffResult.cs
│       └── DiffStat.cs
├── Acode.Application/
│   └── Git/
│       ├── Queries/
│       │   ├── GetStatusQuery.cs
│       │   ├── GetDiffQuery.cs
│       │   └── GetLogQuery.cs
│       └── Options/
│           ├── DiffOptions.cs
│           └── LogOptions.cs
└── Acode.Infrastructure/
    └── Git/
        ├── Parsers/
        │   ├── StatusParser.cs
        │   ├── DiffParser.cs
        │   ├── LogParser.cs
        │   └── NumStatParser.cs
        └── GitService.Status.cs
        └── GitService.Diff.cs
        └── GitService.Log.cs
```

### Domain Models

```csharp
// GitStatus.cs - already defined in task-022
// See Task 022 for full model

// DiffResult.cs
namespace Acode.Domain.Git;

public sealed record DiffResult
{
    public required string RawDiff { get; init; }
    public required IReadOnlyList<FileDiff> Files { get; init; }
    public required DiffStat Summary { get; init; }
    public bool IsEmpty => Files.Count == 0;
}

public sealed record FileDiff
{
    public required string Path { get; init; }
    public string? OldPath { get; init; } // For renames
    public required FileChangeType ChangeType { get; init; }
    public required int AddedLines { get; init; }
    public required int RemovedLines { get; init; }
    public required bool IsBinary { get; init; }
    public string? Content { get; init; } // Unified diff content
}

public enum FileChangeType { Added, Modified, Deleted, Renamed, Copied }

public sealed record DiffStat
{
    public required int FilesChanged { get; init; }
    public required int Insertions { get; init; }
    public required int Deletions { get; init; }
    
    public override string ToString() => 
        $"{FilesChanged} files changed, {Insertions} insertions(+), {Deletions} deletions(-)";
}
```

### Parser Implementation

```csharp
// StatusParser.cs
namespace Acode.Infrastructure.Git.Parsers;

public sealed class StatusParser : IGitOutputParser<GitStatus>
{
    public GitStatus Parse(string output)
    {
        var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        var branch = "";
        var upstream = (string?)null;
        var ahead = (int?)null;
        var behind = (int?)null;
        var isDetached = false;
        var staged = new List<FileStatus>();
        var unstaged = new List<FileStatus>();
        var untracked = new List<string>();
        
        foreach (var line in lines)
        {
            if (line.StartsWith("# branch.head "))
            {
                branch = line[14..];
                isDetached = branch == "(detached)";
            }
            else if (line.StartsWith("# branch.upstream "))
            {
                upstream = line[18..];
            }
            else if (line.StartsWith("# branch.ab "))
            {
                var match = Regex.Match(line, @"\+(\d+) -(\d+)");
                if (match.Success)
                {
                    ahead = int.Parse(match.Groups[1].Value);
                    behind = int.Parse(match.Groups[2].Value);
                }
            }
            else if (line.StartsWith("1 ") || line.StartsWith("2 "))
            {
                ParseChangedEntry(line, staged, unstaged);
            }
            else if (line.StartsWith("? "))
            {
                untracked.Add(line[2..]);
            }
            else if (line.StartsWith("u "))
            {
                ParseUnmergedEntry(line, unstaged);
            }
        }
        
        return new GitStatus
        {
            Branch = branch,
            IsClean = staged.Count == 0 && unstaged.Count == 0 && untracked.Count == 0,
            IsDetachedHead = isDetached,
            UpstreamBranch = upstream,
            AheadBy = ahead,
            BehindBy = behind,
            StagedFiles = staged,
            UnstagedFiles = unstaged,
            UntrackedFiles = untracked
        };
    }
    
    private void ParseChangedEntry(string line, List<FileStatus> staged, List<FileStatus> unstaged)
    {
        // Format: 1 XY subm <mH> <mI> <mW> <hH> <hI> <path>
        // or:     2 XY subm <mH> <mI> <mW> <hH> <hI> <x> <path>\t<origPath>
        var parts = line.Split(' ');
        var xy = parts[1]; // Index and worktree status
        
        // Handle renamed files (line type "2")
        string path, originalPath = null;
        if (line.StartsWith("2 "))
        {
            var pathPart = string.Join(" ", parts.Skip(9));
            var tabIdx = pathPart.IndexOf('\t');
            path = pathPart[..tabIdx];
            originalPath = pathPart[(tabIdx + 1)..];
        }
        else
        {
            path = string.Join(" ", parts.Skip(8));
        }
        
        // Index status (staged)
        if (xy[0] != '.')
        {
            staged.Add(new FileStatus 
            { 
                Path = path, 
                State = MapState(xy[0]),
                OriginalPath = originalPath
            });
        }
        
        // Worktree status (unstaged)
        if (xy[1] != '.')
        {
            unstaged.Add(new FileStatus 
            { 
                Path = path, 
                State = MapState(xy[1])
            });
        }
    }
    
    private static GitFileState MapState(char c) => c switch
    {
        'M' => GitFileState.Modified,
        'A' => GitFileState.Added,
        'D' => GitFileState.Deleted,
        'R' => GitFileState.Renamed,
        'C' => GitFileState.Copied,
        'T' => GitFileState.TypeChanged,
        'U' => GitFileState.Unmerged,
        _ => GitFileState.Modified
    };
}

// LogParser.cs
namespace Acode.Infrastructure.Git.Parsers;

public sealed class LogParser : IGitOutputParser<IReadOnlyList<GitCommit>>
{
    // Custom format for reliable parsing
    // %H = full hash, %h = short hash, %an = author name, %ae = author email
    // %aI = author date ISO, %cn = committer name, %ce = committer email
    // %cI = committer date ISO, %P = parent hashes, %s = subject, %b = body
    public const string Format = "%H%n%h%n%an%n%ae%n%aI%n%cn%n%ce%n%cI%n%P%n%s%n%b%x00";
    
    public IReadOnlyList<GitCommit> Parse(string output)
    {
        if (string.IsNullOrWhiteSpace(output))
            return Array.Empty<GitCommit>();
        
        var commits = new List<GitCommit>();
        var entries = output.Split('\0', StringSplitOptions.RemoveEmptyEntries);
        
        foreach (var entry in entries)
        {
            var lines = entry.Split('\n');
            if (lines.Length < 10) continue;
            
            commits.Add(new GitCommit
            {
                Sha = lines[0].Trim(),
                ShortSha = lines[1].Trim(),
                Author = lines[2].Trim(),
                AuthorEmail = lines[3].Trim(),
                AuthorDate = DateTimeOffset.Parse(lines[4].Trim()),
                Committer = lines[5].Trim(),
                CommitterEmail = lines[6].Trim(),
                CommitDate = DateTimeOffset.Parse(lines[7].Trim()),
                ParentShas = lines[8].Split(' ', StringSplitOptions.RemoveEmptyEntries).ToList(),
                Subject = lines[9].Trim(),
                Body = lines.Length > 10 ? string.Join('\n', lines.Skip(10)).Trim() : null
            });
        }
        
        return commits;
    }
}
```

### Service Implementation

```csharp
// GitService.Status.cs
namespace Acode.Infrastructure.Git;

public partial class GitService
{
    private readonly StatusParser _statusParser = new();
    
    public async Task<GitStatus> GetStatusAsync(string workingDir, CancellationToken ct = default)
    {
        await EnsureRepositoryAsync(workingDir, ct);
        
        var result = await ExecuteGitAsync(
            workingDir,
            new[] { "status", "--porcelain=v2", "--branch", "--untracked-files=all" },
            ct);
        
        return _statusParser.Parse(result.StdOut);
    }
}

// GitService.Diff.cs
public partial class GitService
{
    public async Task<string> GetDiffAsync(
        string workingDir, 
        DiffOptions options, 
        CancellationToken ct = default)
    {
        await EnsureRepositoryAsync(workingDir, ct);
        
        var args = new List<string> { "diff" };
        
        if (options.Staged)
        {
            args.Add("--cached");
        }
        
        if (options.NameOnly)
        {
            args.Add("--name-only");
        }
        
        if (options.Stat)
        {
            args.Add("--stat");
        }
        
        if (options.Context.HasValue)
        {
            args.Add($"-U{options.Context}");
        }
        
        if (!string.IsNullOrEmpty(options.Commit1))
        {
            args.Add(options.Commit1);
        }
        
        if (!string.IsNullOrEmpty(options.Commit2))
        {
            args.Add(options.Commit2);
        }
        
        if (options.Paths?.Count > 0)
        {
            args.Add("--");
            args.AddRange(options.Paths);
        }
        
        var result = await ExecuteGitAsync(workingDir, args, ct);
        return result.StdOut;
    }
    
    public async Task<DiffStat> GetDiffStatAsync(
        string workingDir,
        DiffOptions options,
        CancellationToken ct = default)
    {
        await EnsureRepositoryAsync(workingDir, ct);
        
        var args = new List<string> { "diff", "--numstat" };
        
        if (options.Staged) args.Add("--cached");
        if (!string.IsNullOrEmpty(options.Commit1)) args.Add(options.Commit1);
        if (!string.IsNullOrEmpty(options.Commit2)) args.Add(options.Commit2);
        
        var result = await ExecuteGitAsync(workingDir, args, ct);
        
        int files = 0, insertions = 0, deletions = 0;
        foreach (var line in result.StdOut.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = line.Split('\t');
            if (parts.Length >= 2)
            {
                files++;
                if (int.TryParse(parts[0], out var add)) insertions += add;
                if (int.TryParse(parts[1], out var del)) deletions += del;
            }
        }
        
        return new DiffStat
        {
            FilesChanged = files,
            Insertions = insertions,
            Deletions = deletions
        };
    }
}

// GitService.Log.cs
public partial class GitService
{
    private readonly LogParser _logParser = new();
    
    public async Task<IReadOnlyList<GitCommit>> GetLogAsync(
        string workingDir, 
        LogOptions options, 
        CancellationToken ct = default)
    {
        await EnsureRepositoryAsync(workingDir, ct);
        
        var args = new List<string> 
        { 
            "log", 
            $"--format={LogParser.Format}" 
        };
        
        if (options.Limit.HasValue)
        {
            args.Add($"-n{options.Limit}");
        }
        
        if (!string.IsNullOrEmpty(options.Since))
        {
            args.Add($"--since={options.Since}");
        }
        
        if (!string.IsNullOrEmpty(options.Until))
        {
            args.Add($"--until={options.Until}");
        }
        
        if (!string.IsNullOrEmpty(options.Author))
        {
            args.Add($"--author={options.Author}");
        }
        
        if (!string.IsNullOrEmpty(options.Grep))
        {
            args.Add($"--grep={options.Grep}");
        }
        
        if (options.FirstParent)
        {
            args.Add("--first-parent");
        }
        
        if (options.NoMerges)
        {
            args.Add("--no-merges");
        }
        
        if (!string.IsNullOrEmpty(options.Path))
        {
            args.Add("--");
            args.Add(options.Path);
        }
        
        var result = await ExecuteGitAsync(workingDir, args, ct);
        return _logParser.Parse(result.StdOut);
    }
}
```

### CLI Commands

```csharp
// GitStatusCommand.cs
namespace Acode.Cli.Commands.Git;

[Command("git status", Description = "Show repository status")]
public class GitStatusCommand
{
    [Option("--include-ignored", Description = "Include ignored files")]
    public bool IncludeIgnored { get; set; }
    
    [Option("--format", Description = "Output format: text, json")]
    public string Format { get; set; } = "text";
    
    public async Task<int> ExecuteAsync(
        IGitService git,
        IConsole console,
        CancellationToken ct)
    {
        var cwd = Directory.GetCurrentDirectory();
        var status = await git.GetStatusAsync(cwd, ct);
        
        if (Format == "json")
        {
            console.WriteLine(JsonSerializer.Serialize(status, JsonOptions.Pretty));
            return 0;
        }
        
        console.WriteLine($"On branch: {status.Branch}");
        console.WriteLine($"Status: {(status.IsClean ? "clean" : "dirty")}");
        
        if (status.AheadBy.HasValue || status.BehindBy.HasValue)
        {
            console.WriteLine($"Tracking: {status.UpstreamBranch} (+{status.AheadBy ?? 0}/-{status.BehindBy ?? 0})");
        }
        
        if (!status.IsClean)
        {
            console.WriteLine("\nChanges:");
            foreach (var f in status.StagedFiles)
            {
                console.WriteLine($"  A  {f.Path,-40} (staged)");
            }
            foreach (var f in status.UnstagedFiles)
            {
                console.WriteLine($"  M  {f.Path,-40} (modified)");
            }
            foreach (var f in status.UntrackedFiles)
            {
                console.WriteLine($"  ?  {f,-40} (untracked)");
            }
        }
        
        return 0;
    }
}
```

### Error Codes

| Code | Name | Description | Recovery |
|------|------|-------------|----------|
| GIT-022A-001 | ParseError | Failed to parse git output | Report bug with output sample |
| GIT-022A-002 | EmptyOutput | Git returned no output | Check repository state |
| GIT-022A-003 | DiffTooLarge | Diff exceeds size limit | Use path filter |
| GIT-022A-004 | LogTooLarge | Log exceeds limit | Use --limit option |

### Implementation Checklist

- [ ] Implement StatusParser with porcelain v2 format
- [ ] Implement LogParser with custom format
- [ ] Implement DiffParser for stat extraction
- [ ] Add GetStatusAsync to GitService
- [ ] Add GetDiffAsync to GitService
- [ ] Add GetLogAsync to GitService
- [ ] Implement CLI status command
- [ ] Implement CLI diff command
- [ ] Implement CLI log command
- [ ] Add JSON output format
- [ ] Add unit tests for parsers
- [ ] Add integration tests with real repos
- [ ] Run performance benchmarks

### Rollout Plan

| Phase | Action | Validation |
|-------|--------|------------|
| 1 | Implement StatusParser | Parser tests pass |
| 2 | Implement LogParser | Parser tests pass |
| 3 | Add GetStatusAsync | Status tests pass |
| 4 | Add GetDiffAsync | Diff tests pass |
| 5 | Add GetLogAsync | Log tests pass |
| 6 | Add CLI commands | E2E tests pass |
| 7 | Performance testing | Benchmarks pass |

---

**End of Task 022.a Specification**