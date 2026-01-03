# Task 014: RepoFS Abstraction

**Priority:** P0 – Critical  
**Tier:** S – Core Infrastructure  
**Complexity:** 13 (Fibonacci points)  
**Phase:** Phase 3 – Intelligence Layer  
**Dependencies:** Task 002 (Config Contract), Task 003 (DI Container), Task 011 (Run Session)  

---

## Description

### Business Value

RepoFS is the foundational file system abstraction that enables Agentic Coding Bot to interact with repository files safely and consistently. This abstraction is critical because:

1. **Platform Independence:** Developers work on Windows, macOS, and Linux. Docker containers add another dimension. Without RepoFS, every file operation would need platform-specific handling scattered throughout the codebase.

2. **Security Boundary:** The agent must NEVER access files outside the repository. A single path traversal vulnerability could expose sensitive system files or credentials. RepoFS provides the security boundary that protects user systems.

3. **Transactional Integrity:** When the agent modifies files, partial failures can corrupt code. RepoFS transactions ensure changes are atomic—either all succeed or all are rolled back.

4. **Testability:** By abstracting file system operations behind an interface, unit tests can use in-memory implementations. This enables fast, reliable testing without touching the actual file system.

5. **Future Extensibility:** The abstraction allows adding new file system types (cloud storage, network shares) without modifying consuming code.

### Scope

This task defines the complete file system abstraction layer:

1. **IRepoFS Interface:** The primary contract for file system operations. Defines reading, writing, deletion, enumeration, metadata, transactions, and patching. All file system implementations MUST implement this interface.

2. **Path Handling:** Normalization and validation of file paths. Handles platform differences (slashes, case sensitivity). Prevents path traversal attacks.

3. **Local File System Implementation:** The primary implementation for native file system access. Optimized for common development scenarios.

4. **Docker File System Implementation:** Enables file operations within Docker containers via mounted volumes or Docker API.

5. **Transaction Support:** Groups multiple file operations into atomic units. Supports commit and rollback.

6. **Patch Application:** Applies unified diff patches to files. Critical for the agent's primary modification mechanism.

7. **Factory Pattern:** Creates appropriate file system instances based on configuration.

### Integration Points

| Component | Integration Type | Description |
|-----------|------------------|-------------|
| Task 002 (Config) | Configuration | RepoFS settings in `.agent/config.yml` under `repo` section |
| Task 003 (DI) | Dependency Injection | IRepoFS registered as scoped service |
| Task 011 (Session) | Transaction Context | Sessions wrap file operations in transactions |
| Task 015 (Indexing) | Content Access | Indexer reads files via RepoFS for indexing |
| Task 016 (Context) | Context Building | Context packer reads files via RepoFS |
| Task 025 (File Tool) | Tool Operations | File read/write tools use RepoFS |
| Task 050 (Git Sync) | Change Detection | Git operations observe RepoFS changes |
| Task 003.c (Audit) | Audit Logging | All file operations are audited |

### Failure Modes

| Failure | Impact | Mitigation |
|---------|--------|------------|
| File not found | Read operation fails | Clear error message, file existence check in tooling |
| Permission denied | Cannot read/write | Detect at startup, report permissions issue |
| Disk full | Write fails mid-operation | Transaction rollback, disk space check |
| Path traversal attempt | Security violation | Strict validation, request rejection, audit log |
| Encoding detection fails | Garbled content | Default to UTF-8, warn user |
| Docker container unavailable | Cannot access files | Health check, clear error, retry guidance |
| Transaction timeout | Operation blocked | Configurable timeout, deadlock detection |
| Concurrent modification | Race condition | File locking for writes, optimistic concurrency for reads |
| Long path (Windows) | Operation fails | Detect and warn, suggest path shortening |
| Symbolic link escape | Security violation | Resolve symlinks, validate final path |

### Assumptions

1. The repository is stored on a local or Docker-mounted file system
2. Files are predominantly text (UTF-8) with occasional binary files
3. The agent has read access to all files in the repository
4. Write access may be restricted to certain directories
5. File operations complete in reasonable time (no network latency)
6. The file system supports atomic rename operations (for transactions)
7. File paths are valid for the target platform
8. File sizes are reasonable for in-memory processing (< 10MB typical)
9. Concurrent agents are not modifying the same repository
10. Git ignore patterns are respected for enumeration

### Security Considerations

RepoFS is a critical security boundary. All file operations MUST:

1. **Validate Paths:** Every path MUST be validated before use. Path traversal attempts (../) MUST be rejected.

2. **Enforce Boundaries:** Access MUST be limited to the repository root. No operation may access parent directories.

3. **Handle Symlinks Safely:** Symbolic links MUST be resolved and the final path validated. Links pointing outside the repository MUST be rejected.

4. **Audit Operations:** All file modifications MUST be logged to the audit system with user context, paths, and operation type.

5. **Sanitize Errors:** Error messages MUST NOT expose sensitive path information beyond the repository root.

6. **Limit Permissions:** RepoFS SHOULD operate with minimum necessary permissions. Write access SHOULD be explicit.

7. **Protect Sensitive Files:** Certain files (.agent/secrets.yml, .env) SHOULD have additional access controls.

---

## Glossary / Terms

| Term | Definition |
|------|------------|
| RepoFS | Repository File System abstraction |
| Local FS | Native file system |
| Docker FS | Docker-mounted file system |
| Patch | Targeted file modification |
| Atomic | All-or-nothing operation |
| Transaction | Grouped operations |
| Rollback | Undo transaction |
| Path Normalization | Consistent path format |
| Traversal Attack | Escaping repo boundary |
| Root Path | Repository root directory |
| Relative Path | Path from root |
| Absolute Path | Full system path |
| File Handle | Open file reference |
| Enumeration | Directory listing |
| Watch | File change monitoring |

---

## Out of Scope

The following items are explicitly excluded from Task 014:

- **Remote file systems** - No network shares
- **Cloud storage** - No S3, Azure Blob
- **Git operations** - Epic 05
- **Symbolic links** - Not supported v1
- **Hard links** - Not supported v1
- **Extended attributes** - Not supported
- **ACLs** - Not supported
- **Compression** - Raw files only
- **Encryption** - Raw files only

---

## Functional Requirements

### IRepoFS Interface (FR-014-01 to FR-014-20)

| ID | Requirement |
|----|-------------|
| FR-014-01 | System MUST define IRepoFS interface |
| FR-014-02 | IRepoFS MUST have RootPath property returning repository root |
| FR-014-03 | IRepoFS MUST be disposable for resource cleanup |
| FR-014-04 | All operations MUST accept CancellationToken |
| FR-014-05 | All operations MUST validate paths before execution |
| FR-014-06 | All write operations MUST be auditable |
| FR-014-07 | Interface MUST support reading files |
| FR-014-08 | Interface MUST support writing files |
| FR-014-09 | Interface MUST support deleting files |
| FR-014-10 | Interface MUST support directory enumeration |
| FR-014-11 | Interface MUST support file existence checking |
| FR-014-12 | Interface MUST support metadata retrieval |
| FR-014-13 | Interface MUST support transactions |
| FR-014-14 | Interface MUST support patch application |
| FR-014-15 | IRepoFS MUST have GetCapabilities method |
| FR-014-16 | Capabilities MUST report read-only mode |
| FR-014-17 | Capabilities MUST report transaction support |
| FR-014-18 | Capabilities MUST report watch support |
| FR-014-19 | IRepoFS MAY support file watching |
| FR-014-20 | Watch events MUST include path and change type |

### Path Handling (FR-014-21 to FR-014-40)

| ID | Requirement |
|----|-------------|
| FR-014-21 | System MUST define IPathNormalizer interface |
| FR-014-22 | Normalize MUST convert backslashes to forward slashes |
| FR-014-23 | Normalize MUST collapse multiple slashes |
| FR-014-24 | Normalize MUST handle ./ (current directory) |
| FR-014-25 | Normalize MUST resolve ../ (parent directory) safely |
| FR-014-26 | Normalize MUST remove trailing slashes |
| FR-014-27 | Normalize MUST handle empty path as root |
| FR-014-28 | System MUST define IPathValidator interface |
| FR-014-29 | Validate MUST reject null paths |
| FR-014-30 | Validate MUST reject empty paths |
| FR-014-31 | Validate MUST reject absolute paths |
| FR-014-32 | Validate MUST reject UNC paths (\\\\server\\share) |
| FR-014-33 | Validate MUST reject paths escaping root via ../ |
| FR-014-34 | Validate MUST reject encoded traversal (%2e%2e) |
| FR-014-35 | Validate MUST reject null bytes in paths |
| FR-014-36 | Validate MUST reject invalid characters |
| FR-014-37 | Validate MUST return normalized path on success |
| FR-014-38 | Validation failure MUST throw PathValidationException |
| FR-014-39 | Exception MUST include sanitized path info |
| FR-014-40 | Exception MUST NOT expose system paths |

### Reading Operations (FR-014-41 to FR-014-55)

| ID | Requirement |
|----|-------------|
| FR-014-41 | ReadFileAsync MUST return file content as string |
| FR-014-42 | ReadFileAsync MUST auto-detect encoding |
| FR-014-43 | ReadFileAsync MUST handle UTF-8 with and without BOM |
| FR-014-44 | ReadFileAsync MUST handle UTF-16 LE and BE |
| FR-014-45 | ReadFileAsync MUST default to UTF-8 if detection fails |
| FR-014-46 | ReadFileAsync MUST throw FileNotFoundException if missing |
| FR-014-47 | ReadFileAsync MUST support cancellation |
| FR-014-48 | ReadLinesAsync MUST return IReadOnlyList<string> |
| FR-014-49 | ReadLinesAsync MUST handle LF, CR, and CRLF |
| FR-014-50 | ReadLinesAsync MUST handle empty files |
| FR-014-51 | ReadLinesAsync MUST handle files without trailing newline |
| FR-014-52 | ReadBytesAsync MUST return raw byte array |
| FR-014-53 | ReadBytesAsync MUST support large files (> 10MB) |
| FR-014-54 | All read operations MUST NOT modify files |
| FR-014-55 | All read operations MUST be thread-safe |

### Writing Operations (FR-014-56 to FR-014-70)

| ID | Requirement |
|----|-------------|
| FR-014-56 | WriteFileAsync MUST write string content |
| FR-014-57 | WriteFileAsync MUST use UTF-8 without BOM |
| FR-014-58 | WriteFileAsync MUST create file if not exists |
| FR-014-59 | WriteFileAsync MUST overwrite existing content |
| FR-014-60 | WriteFileAsync MUST create parent directories |
| FR-014-61 | WriteFileAsync MUST support cancellation |
| FR-014-62 | WriteLinesAsync MUST write lines with configurable newlines |
| FR-014-63 | WriteLinesAsync MUST default to platform line endings |
| FR-014-64 | WriteBytesAsync MUST write raw bytes |
| FR-014-65 | All writes MUST be atomic (temp file + rename) |
| FR-014-66 | Atomic write failure MUST NOT corrupt original |
| FR-014-67 | Write operations MUST acquire file lock |
| FR-014-68 | Lock acquisition MUST timeout (configurable) |
| FR-014-69 | Write operations MUST fire change events |
| FR-014-70 | Write operations MUST be audited |

### Deletion Operations (FR-014-71 to FR-014-80)

| ID | Requirement |
|----|-------------|
| FR-014-71 | DeleteFileAsync MUST remove specified file |
| FR-014-72 | DeleteFileAsync MUST NOT error if file missing |
| FR-014-73 | DeleteFileAsync MUST return bool indicating deletion |
| FR-014-74 | DeleteDirectoryAsync MUST remove directory |
| FR-014-75 | DeleteDirectoryAsync MUST support recursive flag |
| FR-014-76 | Non-recursive MUST fail on non-empty directory |
| FR-014-77 | Recursive MUST remove all contents |
| FR-014-78 | Deletion MUST NOT follow symlinks |
| FR-014-79 | Deletion MUST fire change events |
| FR-014-80 | Deletion MUST be audited |

### Enumeration Operations (FR-014-81 to FR-014-95)

| ID | Requirement |
|----|-------------|
| FR-014-81 | EnumerateFilesAsync MUST return IAsyncEnumerable |
| FR-014-82 | EnumerateFilesAsync MUST yield FileEntry records |
| FR-014-83 | FileEntry MUST include relative path |
| FR-014-84 | FileEntry MUST include file name |
| FR-014-85 | FileEntry MAY include size and modified time |
| FR-014-86 | EnumerateFilesAsync MUST support recursive flag |
| FR-014-87 | EnumerateFilesAsync MUST support glob pattern filter |
| FR-014-88 | EnumerateFilesAsync MUST respect .gitignore patterns |
| FR-014-89 | EnumerateFilesAsync MUST respect .agentignore patterns |
| FR-014-90 | EnumerateDirectoriesAsync MUST return directory entries |
| FR-014-91 | Enumeration MUST skip hidden files by default |
| FR-014-92 | Enumeration MUST have option to include hidden |
| FR-014-93 | Enumeration MUST support cancellation |
| FR-014-94 | Enumeration MUST handle inaccessible directories |
| FR-014-95 | Inaccessible directories MUST be skipped with warning |

### Metadata Operations (FR-014-96 to FR-014-105)

| ID | Requirement |
|----|-------------|
| FR-014-96 | ExistsAsync MUST return bool |
| FR-014-97 | ExistsAsync MUST check both files and directories |
| FR-014-98 | ExistsAsync MUST distinguish file from directory |
| FR-014-99 | GetMetadataAsync MUST return FileMetadata |
| FR-014-100 | FileMetadata MUST include Size in bytes |
| FR-014-101 | FileMetadata MUST include LastModified timestamp |
| FR-014-102 | FileMetadata MUST include CreatedAt timestamp |
| FR-014-103 | FileMetadata MUST include IsReadOnly flag |
| FR-014-104 | FileMetadata MUST include IsDirectory flag |
| FR-014-105 | GetMetadataAsync MUST throw if path not found |

### Transaction Support (FR-014-106 to FR-014-120)

| ID | Requirement |
|----|-------------|
| FR-014-106 | BeginTransactionAsync MUST return IRepoFSTransaction |
| FR-014-107 | IRepoFSTransaction MUST implement IAsyncDisposable |
| FR-014-108 | Transaction MUST buffer all write operations |
| FR-014-109 | CommitAsync MUST apply all buffered writes atomically |
| FR-014-110 | CommitAsync MUST use two-phase commit |
| FR-014-111 | RollbackAsync MUST discard all buffered writes |
| FR-014-112 | Dispose without commit MUST auto-rollback |
| FR-014-113 | Transaction MUST track affected files |
| FR-014-114 | Transaction MUST prevent concurrent transactions |
| FR-014-115 | Transaction MUST support timeout |
| FR-014-116 | Timeout MUST trigger auto-rollback |
| FR-014-117 | Nested transactions MUST throw NotSupportedException |
| FR-014-118 | Transaction MUST create backup of modified files |
| FR-014-119 | Rollback MUST restore backups |
| FR-014-120 | Backups MUST be cleaned after commit |

### Patch Application (FR-014-121 to FR-014-140)

| ID | Requirement |
|----|-------------|
| FR-014-121 | ApplyPatchAsync MUST accept unified diff format |
| FR-014-122 | ApplyPatchAsync MUST return PatchResult |
| FR-014-123 | PatchResult MUST include Success flag |
| FR-014-124 | PatchResult MUST include AffectedFiles list |
| FR-014-125 | PatchResult MUST include Error on failure |
| FR-014-126 | Patch MUST support adding lines |
| FR-014-127 | Patch MUST support removing lines |
| FR-014-128 | Patch MUST support modifying lines |
| FR-014-129 | Patch MUST support context matching |
| FR-014-130 | Patch MUST support multiple hunks |
| FR-014-131 | Patch MUST support multiple files |
| FR-014-132 | Patch MUST support new file creation |
| FR-014-133 | Patch MUST support file deletion |
| FR-014-134 | PreviewPatchAsync MUST show changes without applying |
| FR-014-135 | Preview MUST return line-by-line diff |
| FR-014-136 | ValidatePatchAsync MUST check patch applicability |
| FR-014-137 | Validation MUST check context match |
| FR-014-138 | Validation MUST check file existence |
| FR-014-139 | Patch application MUST be transactional |
| FR-014-140 | Partial patch failure MUST rollback entire patch |

### Factory and Configuration (FR-014-141 to FR-014-155)

| ID | Requirement |
|----|-------------|
| FR-014-141 | System MUST define IRepoFSFactory interface |
| FR-014-142 | CreateAsync MUST return configured IRepoFS |
| FR-014-143 | Factory MUST read config from RepoConfig section |
| FR-014-144 | Factory MUST support "local" fs_type |
| FR-014-145 | Factory MUST support "docker" fs_type |
| FR-014-146 | Factory MUST auto-detect type if not specified |
| FR-014-147 | Auto-detect MUST check for Docker environment |
| FR-014-148 | Local type MUST create LocalFileSystem |
| FR-014-149 | Docker type MUST create DockerFileSystem |
| FR-014-150 | Factory MUST validate root path exists |
| FR-014-151 | Factory MUST validate root is directory |
| FR-014-152 | Factory MUST set read-only mode if configured |
| FR-014-153 | Factory MUST configure ignore patterns |
| FR-014-154 | Factory MUST register for DI as scoped |
| FR-014-155 | Factory MUST log configuration on creation |

---

## Non-Functional Requirements

### Performance

- NFR-001: Small file read < 10ms
- NFR-002: Large file read < 100ms/MB
- NFR-003: Directory enumeration < 50ms/1000 files
- NFR-004: Write < 20ms/MB

### Reliability

- NFR-005: No partial writes
- NFR-006: Rollback always works
- NFR-007: Corruption detection

### Security

- NFR-008: Path validation
- NFR-009: Boundary enforcement
- NFR-010: No privilege escalation

### Usability

- NFR-011: Clear error messages
- NFR-012: Helpful exceptions
- NFR-013: Good logging

---

## User Manual Documentation

### Overview

RepoFS provides file system access for the agent. It abstracts local and Docker file systems behind a common interface.

### Configuration

```yaml
# .agent/config.yml
repo:
  # File system type: local, docker
  fs_type: local
  
  # Root path (default: current directory)
  root: .
  
  # Docker-specific settings
  docker:
    container: my-container
    mount_path: /workspace
```

### API Overview

```csharp
// Reading files
var content = await repoFS.ReadFileAsync("src/Program.cs");
var lines = await repoFS.ReadLinesAsync("README.md");
var bytes = await repoFS.ReadBytesAsync("image.png");

// Writing files
await repoFS.WriteFileAsync("output.txt", content);
await repoFS.WriteLinesAsync("data.txt", lines);

// Checking existence
if (await repoFS.ExistsAsync("config.json"))
{
    var meta = await repoFS.GetMetadataAsync("config.json");
}

// Enumerating
await foreach (var file in repoFS.EnumerateFilesAsync("src"))
{
    Console.WriteLine(file.Path);
}
```

### Transactions

```csharp
await using var transaction = await repoFS.BeginTransactionAsync();

try
{
    await repoFS.WriteFileAsync("file1.txt", content1);
    await repoFS.WriteFileAsync("file2.txt", content2);
    await transaction.CommitAsync();
}
catch
{
    // Auto-rollback on exception
    throw;
}
```

### Patching

```csharp
var patch = @"
--- a/src/Program.cs
+++ b/src/Program.cs
@@ -10,3 +10,4 @@
 using System;
+using System.Linq;
";

var result = await repoFS.ApplyPatchAsync(patch);
if (!result.Success)
{
    Console.WriteLine($"Patch failed: {result.Error}");
}
```

### Error Handling

```csharp
try
{
    var content = await repoFS.ReadFileAsync("missing.txt");
}
catch (FileNotFoundException ex)
{
    // File doesn't exist
}
catch (PathTraversalException ex)
{
    // Attempted to escape root
}
catch (AccessDeniedException ex)
{
    // Permission denied
}
```

### CLI Integration

The file tools use RepoFS internally:

```bash
$ acode run "Read the Program.cs file"

[Tool: read_file]
  Path: src/Program.cs
  Result: (file content)
```

### Troubleshooting

#### Path Not Found

**Problem:** File exists but not found

**Solutions:**
1. Check path is relative to root
2. Check case sensitivity
3. Verify .gitignore isn't excluding

#### Permission Denied

**Problem:** Cannot read/write file

**Solutions:**
1. Check file permissions
2. Check if file is locked
3. For Docker: check mount permissions

#### Path Traversal Blocked

**Problem:** Cannot access parent directory

**Solutions:**
This is intentional. RepoFS prevents accessing files outside the repository root for security.

---

## Acceptance Criteria

### Interface

- [ ] AC-001: IRepoFS interface defined
- [ ] AC-002: All methods documented
- [ ] AC-003: Async throughout

### Reading

- [ ] AC-004: Read file works
- [ ] AC-005: Read lines works
- [ ] AC-006: Read bytes works
- [ ] AC-007: Encoding handled

### Writing

- [ ] AC-008: Write file works
- [ ] AC-009: Write lines works
- [ ] AC-010: Write bytes works
- [ ] AC-011: Overwrite works

### Deletion

- [ ] AC-012: Delete file works
- [ ] AC-013: Delete directory works
- [ ] AC-014: Recursive works

### Enumeration

- [ ] AC-015: Files enumerated
- [ ] AC-016: Directories enumerated
- [ ] AC-017: Recursive works
- [ ] AC-018: Filtering works

### Security

- [ ] AC-019: Root boundary enforced
- [ ] AC-020: Traversal prevented
- [ ] AC-021: No escapes possible

### Transactions

- [ ] AC-022: Begin works
- [ ] AC-023: Commit works
- [ ] AC-024: Rollback works

### Patching

- [ ] AC-025: Apply works
- [ ] AC-026: Preview works
- [ ] AC-027: Validation works

---

## Testing Requirements

### Unit Tests

```
Tests/Unit/RepoFS/
├── PathNormalizerTests.cs
│   ├── Should_Normalize_Forward_Slashes()
│   ├── Should_Normalize_Back_Slashes()
│   ├── Should_Normalize_Mixed_Slashes()
│   ├── Should_Handle_Relative_Paths()
│   ├── Should_Handle_Current_Directory_Dot()
│   ├── Should_Collapse_Double_Slashes()
│   ├── Should_Preserve_Leading_Slash()
│   └── Should_Trim_Trailing_Slashes()
│
├── PathValidatorTests.cs
│   ├── Should_Accept_Valid_Relative_Path()
│   ├── Should_Accept_Subdirectory_Path()
│   ├── Should_Accept_Deep_Nested_Path()
│   ├── Should_Reject_Parent_Traversal()
│   ├── Should_Reject_Hidden_Parent_Traversal()
│   ├── Should_Reject_Encoded_Traversal()
│   ├── Should_Reject_Absolute_Path()
│   ├── Should_Reject_UNC_Path()
│   ├── Should_Reject_Null_Path()
│   └── Should_Reject_Empty_Path()
│
├── LocalFileSystemTests.cs
│   ├── ReadFileAsync_Should_Return_Content()
│   ├── ReadFileAsync_Should_Handle_UTF8_BOM()
│   ├── ReadFileAsync_Should_Handle_UTF16()
│   ├── ReadFileAsync_Should_Throw_FileNotFound()
│   ├── ReadFileAsync_Should_Support_Cancellation()
│   ├── ReadLinesAsync_Should_Return_Lines()
│   ├── ReadLinesAsync_Should_Handle_Empty_File()
│   ├── ReadLinesAsync_Should_Handle_No_Trailing_Newline()
│   ├── ReadBytesAsync_Should_Return_Binary()
│   ├── WriteFileAsync_Should_Create_New_File()
│   ├── WriteFileAsync_Should_Overwrite_Existing()
│   ├── WriteFileAsync_Should_Create_Parent_Directories()
│   ├── WriteFileAsync_Should_Use_UTF8_No_BOM()
│   ├── WriteLinesAsync_Should_Write_With_Newlines()
│   ├── WriteBytesAsync_Should_Write_Binary()
│   ├── DeleteFileAsync_Should_Remove_File()
│   ├── DeleteFileAsync_Should_Ignore_Missing()
│   ├── DeleteDirectoryAsync_Should_Remove_Empty()
│   ├── DeleteDirectoryAsync_Should_Remove_Recursive()
│   ├── ExistsAsync_Should_Return_True_For_File()
│   ├── ExistsAsync_Should_Return_True_For_Directory()
│   ├── ExistsAsync_Should_Return_False_For_Missing()
│   ├── GetMetadataAsync_Should_Return_Size()
│   ├── GetMetadataAsync_Should_Return_LastModified()
│   ├── GetMetadataAsync_Should_Return_CreatedDate()
│   └── GetMetadataAsync_Should_Throw_FileNotFound()
│
├── EnumerationTests.cs
│   ├── EnumerateFilesAsync_Should_List_Files()
│   ├── EnumerateFilesAsync_Should_Skip_Directories()
│   ├── EnumerateFilesAsync_Should_Support_Recursive()
│   ├── EnumerateFilesAsync_Should_Apply_Filter()
│   ├── EnumerateFilesAsync_Should_Respect_Ignores()
│   ├── EnumerateFilesAsync_Should_Handle_Empty_Directory()
│   ├── EnumerateDirectoriesAsync_Should_List_Directories()
│   ├── EnumerateDirectoriesAsync_Should_Skip_Files()
│   ├── EnumerateDirectoriesAsync_Should_Support_Recursive()
│   └── EnumerateDirectoriesAsync_Should_Handle_Hidden()
│
├── TransactionTests.cs
│   ├── BeginTransaction_Should_Create_Transaction()
│   ├── Commit_Should_Finalize_Writes()
│   ├── Commit_Should_Be_Atomic()
│   ├── Rollback_Should_Undo_Writes()
│   ├── Rollback_Should_Restore_Original()
│   ├── AutoRollback_On_Exception()
│   ├── AutoRollback_On_Dispose_Without_Commit()
│   ├── Transaction_Should_Handle_Multiple_Files()
│   ├── Transaction_Should_Handle_Create_And_Delete()
│   └── Nested_Transaction_Should_Throw()
│
└── PatchApplicatorTests.cs
    ├── ApplyPatch_Should_Add_Lines()
    ├── ApplyPatch_Should_Remove_Lines()
    ├── ApplyPatch_Should_Modify_Lines()
    ├── ApplyPatch_Should_Handle_Context()
    ├── ApplyPatch_Should_Handle_Multiple_Hunks()
    ├── ApplyPatch_Should_Handle_Multiple_Files()
    ├── ApplyPatch_Should_Create_New_File()
    ├── ApplyPatch_Should_Delete_File()
    ├── ApplyPatch_Should_Fail_On_Mismatch()
    ├── ApplyPatch_Should_Fail_On_Missing_File()
    ├── PreviewPatch_Should_Show_Changes()
    ├── PreviewPatch_Should_Not_Modify()
    ├── ValidatePatch_Should_Accept_Valid()
    └── ValidatePatch_Should_Reject_Malformed()
```

### Integration Tests

```
Tests/Integration/RepoFS/
├── LocalFSIntegrationTests.cs
│   ├── Should_Read_Large_File()
│   ├── Should_Write_Large_File()
│   ├── Should_Handle_Deep_Directory_Tree()
│   ├── Should_Handle_Many_Files()
│   ├── Should_Handle_Unicode_Filenames()
│   ├── Should_Handle_Long_Paths()
│   ├── Should_Handle_Special_Characters()
│   ├── Should_Handle_Concurrent_Reads()
│   ├── Should_Handle_Read_While_Write()
│   └── Should_Survive_Disk_Full()
│
├── TransactionIntegrationTests.cs
│   ├── Should_Rollback_On_Crash()
│   ├── Should_Handle_Concurrent_Transactions()
│   └── Should_Recover_From_Partial_Commit()
│
└── PatchIntegrationTests.cs
    ├── Should_Apply_Real_Git_Diff()
    ├── Should_Handle_Binary_Detection()
    └── Should_Apply_Patch_Atomically()
```

### E2E Tests

```
Tests/E2E/RepoFS/
├── FileToolE2ETests.cs
│   ├── Should_Read_File_Via_Agent_Tool()
│   ├── Should_Write_File_Via_Agent_Tool()
│   ├── Should_List_Directory_Via_Agent_Tool()
│   ├── Should_Apply_Patch_Via_Agent_Tool()
│   └── Should_Handle_Agent_Error_Recovery()
```

### Performance Benchmarks

| Benchmark | Target | Maximum |
|-----------|--------|---------|
| 1KB file read | 5ms | 10ms |
| 1MB file read | 50ms | 100ms |
| 1000 file enum | 25ms | 50ms |
| Patch apply | 10ms | 25ms |

---

## User Verification Steps

### Scenario 1: Read File

1. Create test file
2. Read via RepoFS
3. Verify: Content matches

### Scenario 2: Write File

1. Write via RepoFS
2. Read back
3. Verify: Content matches

### Scenario 3: Transaction

1. Begin transaction
2. Write file
3. Rollback
4. Verify: File unchanged

### Scenario 4: Apply Patch

1. Create file
2. Apply patch
3. Verify: Changes applied

### Scenario 5: Path Traversal

1. Try reading ../outside.txt
2. Verify: Exception thrown

---

## Implementation Prompt

### File Structure

```
src/AgenticCoder.Domain/
├── FileSystem/
│   ├── IRepoFS.cs
│   ├── FileMetadata.cs
│   └── PatchResult.cs
│
src/AgenticCoder.Application/
├── FileSystem/
│   └── IRepoFSFactory.cs
│
src/AgenticCoder.Infrastructure/
├── FileSystem/
│   ├── RepoFSFactory.cs
│   ├── PathNormalizer.cs
│   ├── PathValidator.cs
│   ├── Local/
│   │   └── LocalFileSystem.cs
│   ├── Docker/
│   │   └── DockerFileSystem.cs
│   └── Patching/
│       └── UnifiedDiffApplicator.cs
```

### IRepoFS Interface

```csharp
namespace AgenticCoder.Domain.FileSystem;

public interface IRepoFS
{
    // Reading
    Task<string> ReadFileAsync(string path, CancellationToken ct = default);
    Task<IReadOnlyList<string>> ReadLinesAsync(string path, CancellationToken ct = default);
    Task<byte[]> ReadBytesAsync(string path, CancellationToken ct = default);
    
    // Writing
    Task WriteFileAsync(string path, string content, CancellationToken ct = default);
    Task WriteLinesAsync(string path, IEnumerable<string> lines, CancellationToken ct = default);
    Task WriteBytesAsync(string path, byte[] bytes, CancellationToken ct = default);
    
    // Deletion
    Task DeleteFileAsync(string path, CancellationToken ct = default);
    Task DeleteDirectoryAsync(string path, bool recursive, CancellationToken ct = default);
    
    // Enumeration
    IAsyncEnumerable<FileEntry> EnumerateFilesAsync(string path, bool recursive = false);
    IAsyncEnumerable<DirectoryEntry> EnumerateDirectoriesAsync(string path, bool recursive = false);
    
    // Metadata
    Task<bool> ExistsAsync(string path, CancellationToken ct = default);
    Task<FileMetadata> GetMetadataAsync(string path, CancellationToken ct = default);
    
    // Transactions
    Task<IRepoFSTransaction> BeginTransactionAsync(CancellationToken ct = default);
    
    // Patching
    Task<PatchResult> ApplyPatchAsync(string patch, CancellationToken ct = default);
    Task<PatchPreview> PreviewPatchAsync(string patch, CancellationToken ct = default);
}
```

### Error Codes

| Code | Meaning |
|------|---------|
| ACODE-FS-001 | File not found |
| ACODE-FS-002 | Permission denied |
| ACODE-FS-003 | Path traversal |
| ACODE-FS-004 | Transaction failed |
| ACODE-FS-005 | Patch failed |

### Implementation Checklist

1. [ ] Create IRepoFS interface
2. [ ] Create path normalizer
3. [ ] Create path validator
4. [ ] Implement local FS
5. [ ] Implement transactions
6. [ ] Implement patching
7. [ ] Create factory
8. [ ] Write tests

### Rollout Plan

1. **Phase 1:** Interface and path handling
2. **Phase 2:** Local FS implementation
3. **Phase 3:** Transactions
4. **Phase 4:** Patching
5. **Phase 5:** Factory and DI

---

**End of Task 014 Specification**