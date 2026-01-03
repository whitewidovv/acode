# Task 014: RepoFS Abstraction

**Priority:** P0 – Critical  
**Tier:** S – Core Infrastructure  
**Complexity:** 13 (Fibonacci points)  
**Phase:** Phase 3 – Intelligence Layer  
**Dependencies:** Task 002 (Config Contract)  

---

## Description

Task 014 implements the RepoFS abstraction. This is the file system layer that all agent operations use. It provides uniform access to files regardless of where they are stored.

RepoFS abstracts file system differences. Local files work one way. Docker-mounted files work another. The abstraction hides these differences. Consumers get a consistent API.

The agent needs to read files for context. It needs to write files for changes. It needs to enumerate directories for discovery. RepoFS provides all these operations.

Atomic operations are critical. When the agent makes changes, they must be all-or-nothing. Partial writes corrupt files. RepoFS ensures atomicity through transactions.

Patching is the primary write mechanism. The agent doesn't rewrite whole files. It applies patches—small targeted changes. RepoFS applies patches atomically and can roll back.

Path normalization is essential. Windows uses backslashes. Unix uses forward slashes. Docker mounts add complexity. RepoFS normalizes all paths internally.

File system events enable incremental indexing. When files change, indexes need updates. RepoFS can optionally report changes. This is used by the indexing layer.

Security boundaries are enforced. The agent only accesses files within the repository. RepoFS prevents path traversal attacks. It validates all paths.

Error handling is comprehensive. File not found. Permission denied. Disk full. Network error. Each has clear error codes and messages.

Performance is optimized for common patterns. Reading small files is fast. Enumerating large directories is efficient. Caching is used appropriately.

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

### IRepoFS Interface

- FR-001: IRepoFS interface MUST exist
- FR-002: Interface MUST support read
- FR-003: Interface MUST support write
- FR-004: Interface MUST support delete
- FR-005: Interface MUST support enumerate
- FR-006: Interface MUST support exists
- FR-007: Interface MUST support metadata

### Path Handling

- FR-008: Paths MUST be normalized
- FR-009: Forward slashes MUST work
- FR-010: Backslashes MUST work
- FR-011: Relative paths MUST work
- FR-012: Absolute paths MUST be converted

### Security

- FR-013: Root boundary MUST be enforced
- FR-014: Path traversal MUST be prevented
- FR-015: ../ MUST not escape root
- FR-016: Symlinks MUST not escape root

### Reading

- FR-017: ReadFileAsync MUST work
- FR-018: ReadLinesAsync MUST work
- FR-019: ReadBytesAsync MUST work
- FR-020: Encoding MUST be detected
- FR-021: UTF-8 MUST be default

### Writing

- FR-022: WriteFileAsync MUST work
- FR-023: WriteLinesAsync MUST work
- FR-024: WriteBytesAsync MUST work
- FR-025: Overwrite MUST work
- FR-026: Create new MUST work

### Deletion

- FR-027: DeleteFileAsync MUST work
- FR-028: DeleteDirectoryAsync MUST work
- FR-029: Recursive delete MUST work
- FR-030: Non-existent MUST not error

### Enumeration

- FR-031: EnumerateFilesAsync MUST work
- FR-032: EnumerateDirectoriesAsync MUST work
- FR-033: Recursive enumeration MUST work
- FR-034: Filtering MUST work
- FR-035: Ignores MUST be respected

### Metadata

- FR-036: ExistsAsync MUST work
- FR-037: GetMetadataAsync MUST work
- FR-038: Size MUST be returned
- FR-039: Last modified MUST be returned
- FR-040: Created MUST be returned

### Transactions

- FR-041: BeginTransaction MUST work
- FR-042: Commit MUST finalize
- FR-043: Rollback MUST undo
- FR-044: Auto-rollback on error
- FR-045: Nested transactions NOT supported

### Patching

- FR-046: ApplyPatchAsync MUST work
- FR-047: Unified diff MUST work
- FR-048: Line-based patches MUST work
- FR-049: Patch preview MUST work
- FR-050: Patch validation MUST work

### Factory

- FR-051: IRepoFSFactory MUST exist
- FR-052: Create from config MUST work
- FR-053: Type detection MUST work
- FR-054: Local type MUST work
- FR-055: Docker type MUST work

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
│   ├── Should_Normalize_Slashes()
│   ├── Should_Handle_Relative()
│   └── Should_Reject_Traversal()
│
├── LocalFileSystemTests.cs
│   ├── Should_Read_File()
│   ├── Should_Write_File()
│   └── Should_Enumerate()
│
├── TransactionTests.cs
│   ├── Should_Commit()
│   └── Should_Rollback()
│
└── PatchApplicatorTests.cs
    ├── Should_Apply_Unified_Diff()
    └── Should_Validate_Patch()
```

### Integration Tests

```
Tests/Integration/RepoFS/
├── LocalFSIntegrationTests.cs
│   ├── Should_Handle_Large_Files()
│   └── Should_Handle_Deep_Directories()
```

### E2E Tests

```
Tests/E2E/RepoFS/
├── FileToolE2ETests.cs
│   └── Should_Read_Write_Via_Agent()
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