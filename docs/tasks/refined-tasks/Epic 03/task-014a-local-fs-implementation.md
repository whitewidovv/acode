# Task 014.a: Local FS Implementation

**Priority:** P0 – Critical  
**Tier:** S – Core Infrastructure  
**Complexity:** 8 (Fibonacci points)  
**Phase:** Phase 3 – Intelligence Layer  
**Dependencies:** Task 014 (RepoFS Abstraction)  

---

## Description

### Business Value

The Local File System implementation is the primary file access mechanism for Agentic Coding Bot, serving as the default provider that most developers will use. When developers run acode directly on their machine, this implementation enables seamless file operations on local repositories without additional configuration or infrastructure.

This implementation is critical because it wraps .NET's System.IO with production-grade enhancements: atomic write operations prevent file corruption during agent modifications, streaming support handles large files without memory exhaustion, and comprehensive encoding detection ensures correct handling of diverse codebases. Without these safeguards, agent operations could corrupt files, crash on large repositories, or produce garbled content.

The Local FS implementation also establishes the security boundary that prevents the agent from accessing files outside the designated repository root. Path traversal prevention, null byte rejection, and invalid character filtering protect the user's system from accidental or malicious access attempts.

### Scope

This task delivers the complete local file system implementation:

1. **LocalFileSystem Class:** The primary IRepoFS implementation for native file access. Wraps System.IO with async operations, path validation, and security enforcement.

2. **Atomic Write Support:** Implements write-to-temp-then-rename pattern ensuring file modifications are never partially applied. Temp files use same directory for atomic rename guarantee.

3. **Encoding Detection:** Automatically detects file encoding via BOM detection and heuristics. Supports UTF-8, UTF-16, and explicit encoding override for legacy files.

4. **Large File Streaming:** Reads large files in chunks to prevent memory pressure. Configurable threshold determines streaming vs. full-read behavior.

5. **Lazy Enumeration:** Directory enumeration yields files as discovered, enabling early processing and constant memory usage regardless of directory size.

### Integration Points

| Component | Integration Type | Description |
|-----------|------------------|-------------|
| Task 014 (RepoFS) | Interface Implementation | Implements IRepoFS interface contract |
| Task 014.c (Patching) | File Access | Patch applicator uses LocalFS for reading and writing files |
| Task 003 (DI) | Dependency Injection | Registered as default IRepoFS implementation |
| Task 002 (Config) | Configuration | Local FS settings from `repo.local` config section |
| Task 011 (Session) | Session Context | Session determines repository root path |
| Task 015 (Indexing) | Content Reading | Indexer reads file content for indexing |
| Task 025 (File Tool) | Tool Backend | File read/write tools use LocalFS |
| Task 003.c (Audit) | Audit Logging | All operations logged with path and duration |

### Failure Modes

| Failure | Impact | Mitigation |
|---------|--------|------------|
| File not found | Read returns error | Clear FileNotFoundException with path context |
| Permission denied | Read/write blocked | Detect at startup, report with suggested remediation |
| Disk full during write | Atomic write fails | Temp file cleaned up, original unchanged |
| Lock conflict | Operation blocked | Configurable timeout with retry, deadlock detection |
| Path traversal attempt | Security violation | Strict validation, rejection, audit logging |
| Invalid encoding | Garbled content | Default to UTF-8, binary detection, explicit override option |
| Long path (Windows) | Operation fails | Detect MAX_PATH limits, report with shortening suggestion |
| Temp file cleanup fails | Orphaned temp files | Background cleanup on startup |

### Assumptions

1. The repository is stored on a local file system (not network share or cloud storage)
2. The file system supports atomic rename operations (required for atomic writes)
3. The agent process has read access to all files in the repository
4. Write access is explicitly granted via configuration when needed
5. Files are predominantly text (UTF-8) with occasional binary files
6. File sizes are reasonable for processing (< 10MB typical, streaming for larger)
7. The operating system provides reliable file locking primitives
8. Temp directory is on the same volume as repository (for atomic rename)

### Security Considerations

1. **Path Validation:** All paths MUST be validated against the repository root before any I/O operation. Path traversal via `../` sequences MUST be rejected.

2. **Null Byte Injection:** Paths containing null bytes MUST be rejected to prevent truncation attacks.

3. **Invalid Character Rejection:** Paths with invalid characters (control characters, reserved Windows characters) MUST be rejected.

4. **Lock Escalation Prevention:** Read operations MUST NOT acquire exclusive locks. Write locks MUST timeout to prevent DoS.

5. **Temp File Security:** Temporary files MUST be created with restricted permissions. Cleanup MUST occur on failure.

---

## Glossary / Terms

| Term | Definition |
|------|------------|
| Local FS | Native file system |
| System.IO | .NET file system APIs |
| Atomic Write | All-or-nothing write |
| Temp File | Intermediate file for atomic |
| BOM | Byte Order Mark |
| Encoding | Character encoding |
| Streaming | Read in chunks |
| Case Sensitivity | Upper/lower handling |
| File Lock | Exclusive access |
| Lazy Enumeration | On-demand iteration |
| Memory Mapped | OS file mapping |
| ENOENT | File not found |
| EACCES | Permission denied |
| Path Separator | / or \ |
| Normalized Path | Consistent format |

---

## Out of Scope

The following items are explicitly excluded from Task 014.a:

- **Docker file systems** - Task 014.b
- **Network shares** - Not supported
- **Remote files** - Not supported
- **Symbolic links** - Limited support
- **Hard links** - Not supported
- **Extended attributes** - Not supported
- **File system events** - Separate concern
- **Compression** - Not supported

---

## Functional Requirements

### File Reading (FR-014a-01 to FR-014a-07)

| ID | Requirement |
|----|-------------|
| FR-014a-01 | ReadFileAsync MUST return file content as string |
| FR-014a-02 | ReadFileAsync MUST default to UTF-8 encoding when no BOM present |
| FR-014a-03 | ReadFileAsync MUST detect and respect BOM encoding |
| FR-014a-04 | ReadLinesAsync MUST return file content as IAsyncEnumerable of lines |
| FR-014a-05 | ReadBytesAsync MUST return file content as byte array |
| FR-014a-06 | Large files (above threshold) MUST be streamed rather than fully loaded |
| FR-014a-07 | Binary files MUST be detected and handled without encoding conversion |

### File Writing (FR-014a-08 to FR-014a-14)

| ID | Requirement |
|----|-------------|
| FR-014a-08 | WriteFileAsync MUST write string content to specified path |
| FR-014a-09 | WriteFileAsync MUST use atomic write pattern (temp file then rename) |
| FR-014a-10 | Temp file MUST be created in same directory as target file |
| FR-014a-11 | Atomic rename MUST replace original file only on success |
| FR-014a-12 | WriteLinesAsync MUST write IEnumerable of lines with platform line endings |
| FR-014a-13 | WriteBytesAsync MUST write byte array content to specified path |
| FR-014a-14 | WriteFileAsync MUST create parent directories if they do not exist |

### File Deletion (FR-014a-15 to FR-014a-19)

| ID | Requirement |
|----|-------------|
| FR-014a-15 | DeleteFileAsync MUST delete file at specified path |
| FR-014a-16 | DeleteFileAsync MUST NOT throw error if file does not exist |
| FR-014a-17 | DeleteDirectoryAsync MUST delete directory at specified path |
| FR-014a-18 | DeleteDirectoryAsync MUST support recursive deletion |
| FR-014a-19 | DeleteDirectoryAsync MUST handle non-empty directories when recursive enabled |

### Directory Enumeration (FR-014a-20 to FR-014a-25)

| ID | Requirement |
|----|-------------|
| FR-014a-20 | EnumerateFilesAsync MUST return IAsyncEnumerable of file entries |
| FR-014a-21 | Enumeration MUST be lazy (yield files as discovered) |
| FR-014a-22 | EnumerateFilesAsync MUST support recursive option |
| FR-014a-23 | EnumerateFilesAsync MUST support glob pattern filtering |
| FR-014a-24 | EnumerateFilesAsync MUST support hidden file include/exclude option |
| FR-014a-25 | EnumerateDirectoriesAsync MUST return IAsyncEnumerable of directory entries |

### Metadata (FR-014a-26 to FR-014a-31)

| ID | Requirement |
|----|-------------|
| FR-014a-26 | ExistsAsync MUST return true if file or directory exists |
| FR-014a-27 | GetMetadataAsync MUST return file metadata object |
| FR-014a-28 | Metadata MUST include file size in bytes |
| FR-014a-29 | Metadata MUST include last modified timestamp |
| FR-014a-30 | Metadata MUST include created timestamp |
| FR-014a-31 | Metadata MUST include IsDirectory flag |

### Path Handling (FR-014a-32 to FR-014a-36)

| ID | Requirement |
|----|-------------|
| FR-014a-32 | Path normalization MUST convert all paths to consistent format |
| FR-014a-33 | Forward slashes MUST be accepted on all platforms |
| FR-014a-34 | Backslashes MUST be converted to forward slashes |
| FR-014a-35 | Trailing slashes MUST be handled consistently |
| FR-014a-36 | Path combining MUST respect repository root boundary |

### Security (FR-014a-37 to FR-014a-40)

| ID | Requirement |
|----|-------------|
| FR-014a-37 | All operations MUST enforce repository root boundary |
| FR-014a-38 | Path traversal via ../ MUST be prevented |
| FR-014a-39 | Null bytes in paths MUST be rejected |
| FR-014a-40 | Invalid characters in paths MUST be rejected |

### Error Handling (FR-014a-41 to FR-014a-45)

| ID | Requirement |
|----|-------------|
| FR-014a-41 | FileNotFoundException MUST be thrown for missing files |
| FR-014a-42 | AccessDeniedException MUST be thrown for permission failures |
| FR-014a-43 | DirectoryNotFoundException MUST be thrown for missing directories |
| FR-014a-44 | IOException MUST be thrown for disk/IO errors |
| FR-014a-45 | All exceptions MUST include clear, actionable error messages |

### Locking (FR-014a-46 to FR-014a-49)

| ID | Requirement |
|----|-------------|
| FR-014a-46 | Read operations MUST use shared locks allowing concurrent reads |
| FR-014a-47 | Write operations MUST use exclusive locks |
| FR-014a-48 | Lock timeout MUST be configurable |
| FR-014a-49 | Lock conflicts MUST trigger configurable retry behavior |

### Encoding (FR-014a-50 to FR-014a-54)

| ID | Requirement |
|----|-------------|
| FR-014a-50 | UTF-8 MUST be the default encoding when no BOM detected |
| FR-014a-51 | UTF-8 with BOM MUST be detected and used |
| FR-014a-52 | UTF-16 (LE/BE) MUST be detected via BOM |
| FR-014a-53 | Binary files MUST be detected to prevent encoding conversion |
| FR-014a-54 | Explicit encoding override MUST be supported for read operations |

---

## Non-Functional Requirements

### Performance (NFR-014a-01 to NFR-014a-04)

| ID | Category | Requirement |
|----|----------|-------------|
| NFR-014a-01 | Performance | 1KB file read MUST complete in < 5ms |
| NFR-014a-02 | Performance | 1MB file read MUST complete in < 50ms |
| NFR-014a-03 | Performance | Directory enumeration MUST process 1000 files in < 50ms |
| NFR-014a-04 | Performance | File write MUST complete at < 20ms per MB |

### Reliability (NFR-014a-05 to NFR-014a-07)

| ID | Category | Requirement |
|----|----------|-------------|
| NFR-014a-05 | Reliability | Atomic writes MUST prevent partial file content on failure |
| NFR-014a-06 | Reliability | Temp files MUST be cleaned up on failure |
| NFR-014a-07 | Reliability | File locks MUST be released on error or exception |

### Security (NFR-014a-08 to NFR-014a-10)

| ID | Category | Requirement |
|----|----------|-------------|
| NFR-014a-08 | Security | All paths MUST be validated before I/O operations |
| NFR-014a-09 | Security | Operations MUST NOT allow privilege escalation |
| NFR-014a-10 | Security | Repository root boundary MUST be enforced on all operations |

### Maintainability (NFR-014a-11 to NFR-014a-13)

| ID | Category | Requirement |
|----|----------|-------------|
| NFR-014a-11 | Maintainability | Error messages MUST be clear and actionable |
| NFR-014a-12 | Maintainability | All operations MUST be logged with appropriate detail level |
| NFR-014a-13 | Maintainability | Exceptions MUST include context for debugging |

---

## User Manual Documentation

### Overview

The Local FS implementation provides file access for local repositories. It is the default implementation when running acode directly on your machine.

### Configuration

```yaml
# .agent/config.yml
repo:
  fs_type: local  # Default
  root: .         # Repository root
  
  local:
    # Large file threshold (bytes)
    large_file_threshold: 1048576  # 1MB
    
    # Lock timeout (milliseconds)
    lock_timeout_ms: 5000
    
    # Retry on lock conflict
    lock_retry_count: 3
```

### Usage Examples

```csharp
// Create local file system
var fs = new LocalFileSystem(new LocalFSOptions
{
    RootPath = "/path/to/repo",
    LargeFileThreshold = 1024 * 1024  // 1MB
});

// Read a file
var content = await fs.ReadFileAsync("src/Program.cs");

// Write a file
await fs.WriteFileAsync("output.txt", "Hello, World!");

// Enumerate files
await foreach (var file in fs.EnumerateFilesAsync("src", recursive: true))
{
    Console.WriteLine($"{file.Path}: {file.Size} bytes");
}
```

### Encoding Handling

The local FS auto-detects encoding:

1. **BOM present**: Uses BOM encoding
2. **No BOM**: Attempts UTF-8
3. **Invalid UTF-8**: Falls back to system encoding

Force specific encoding:

```csharp
var content = await fs.ReadFileAsync("legacy.txt", Encoding.Latin1);
```

### Atomic Writes

Writes are atomic by default:

1. Content written to `filename.tmp`
2. Temp file flushed to disk
3. Rename replaces original
4. If any step fails, original unchanged

### Troubleshooting

#### File Locked

**Problem:** Cannot write, file in use

**Solutions:**
1. Close other applications using file
2. Increase lock_timeout_ms
3. Check for antivirus interference

#### Permission Denied

**Problem:** Cannot read or write

**Solutions:**
1. Check file permissions
2. Run with appropriate privileges
3. Check if file is read-only

#### Encoding Issues

**Problem:** Garbled text

**Solutions:**
1. Specify explicit encoding
2. Check file's actual encoding
3. Use binary read for non-text

---

## Acceptance Criteria

### Reading

- [ ] AC-001: Read file works
- [ ] AC-002: Read lines works
- [ ] AC-003: Read bytes works
- [ ] AC-004: Large file streaming works
- [ ] AC-005: Encoding detected

### Writing

- [ ] AC-006: Write file works
- [ ] AC-007: Atomic write works
- [ ] AC-008: Creates parent dirs
- [ ] AC-009: Temp file cleaned

### Enumeration

- [ ] AC-010: Files enumerated
- [ ] AC-011: Lazy enumeration works
- [ ] AC-012: Recursive works
- [ ] AC-013: Filtering works

### Security

- [ ] AC-014: Root enforced
- [ ] AC-015: Traversal blocked
- [ ] AC-016: Invalid chars rejected

### Error Handling

- [ ] AC-017: Not found exception
- [ ] AC-018: Access denied exception
- [ ] AC-019: Clear messages

---

## Testing Requirements

### Unit Tests

```
Tests/Unit/FileSystem/Local/
├── LocalFileReadTests.cs
│   ├── Should_Read_Small_File()
│   ├── Should_Read_Empty_File()
│   ├── Should_Read_File_With_Unicode()
│   ├── Should_Read_File_With_BOM()
│   ├── Should_Stream_Large_File()
│   ├── Should_Stream_Very_Large_File()
│   ├── Should_Detect_UTF8_Encoding()
│   ├── Should_Detect_UTF16_Encoding()
│   ├── Should_Detect_Binary_File()
│   ├── Should_Apply_Explicit_Encoding()
│   ├── Should_Handle_Read_Only_File()
│   ├── Should_Handle_Concurrent_Reads()
│   ├── Should_Support_Cancellation()
│   ├── Should_Throw_For_Missing_File()
│   └── Should_Throw_For_Access_Denied()
│
├── LocalFileWriteTests.cs
│   ├── Should_Write_Small_File()
│   ├── Should_Write_Large_File()
│   ├── Should_Write_Empty_File()
│   ├── Should_Write_Unicode_Content()
│   ├── Should_Write_Atomically()
│   ├── Should_Use_Temp_File()
│   ├── Should_Cleanup_Temp_On_Failure()
│   ├── Should_Create_Parent_Directories()
│   ├── Should_Create_Deep_Parent_Directories()
│   ├── Should_Overwrite_Existing_File()
│   ├── Should_Handle_Lock_Conflict()
│   ├── Should_Retry_On_Lock_Conflict()
│   ├── Should_Timeout_On_Locked_File()
│   ├── Should_Preserve_Original_On_Failure()
│   └── Should_Support_Cancellation()
│
├── LocalLineOperationsTests.cs
│   ├── Should_Read_Lines()
│   ├── Should_Read_Lines_Empty_File()
│   ├── Should_Read_Lines_Single_Line()
│   ├── Should_Read_Lines_No_Trailing_Newline()
│   ├── Should_Read_Lines_Mixed_Line_Endings()
│   ├── Should_Write_Lines()
│   ├── Should_Write_Lines_Empty_Array()
│   └── Should_Write_Lines_With_Platform_Newline()
│
├── LocalBinaryOperationsTests.cs
│   ├── Should_Read_Bytes()
│   ├── Should_Read_Bytes_Empty_File()
│   ├── Should_Write_Bytes()
│   └── Should_Write_Bytes_Empty_Array()
│
├── LocalEnumerationTests.cs
│   ├── Should_Enumerate_Files()
│   ├── Should_Enumerate_Empty_Directory()
│   ├── Should_Enumerate_Lazily()
│   ├── Should_Enumerate_Recursively()
│   ├── Should_Enumerate_NonRecursively()
│   ├── Should_Filter_By_Pattern()
│   ├── Should_Filter_By_Extension()
│   ├── Should_Include_Hidden_Files()
│   ├── Should_Exclude_Hidden_Files()
│   ├── Should_Enumerate_Directories()
│   ├── Should_Handle_Deep_Hierarchy()
│   └── Should_Support_Cancellation()
│
├── LocalMetadataTests.cs
│   ├── Should_Check_File_Exists()
│   ├── Should_Check_Directory_Exists()
│   ├── Should_Return_False_For_Missing()
│   ├── Should_Get_File_Size()
│   ├── Should_Get_Last_Modified()
│   ├── Should_Get_Created_Date()
│   ├── Should_Get_IsDirectory_Flag()
│   └── Should_Throw_For_Missing_Metadata()
│
├── LocalDeleteTests.cs
│   ├── Should_Delete_File()
│   ├── Should_Delete_Missing_File_NoError()
│   ├── Should_Delete_Empty_Directory()
│   ├── Should_Delete_Directory_Recursive()
│   ├── Should_Delete_Non_Empty_Directory()
│   └── Should_Throw_For_Locked_File()
│
├── LocalPathHandlingTests.cs
│   ├── Should_Normalize_Forward_Slashes()
│   ├── Should_Normalize_Back_Slashes()
│   ├── Should_Handle_Trailing_Slashes()
│   ├── Should_Combine_With_Root()
│   └── Should_Handle_Case_Sensitivity()
│
└── LocalSecurityTests.cs
    ├── Should_Block_Parent_Traversal()
    ├── Should_Block_Hidden_Traversal()
    ├── Should_Block_Absolute_Path()
    ├── Should_Enforce_Root_Boundary()
    ├── Should_Reject_Null_Bytes()
    └── Should_Reject_Invalid_Characters()
```

### Integration Tests

```
Tests/Integration/FileSystem/Local/
├── LocalFSIntegrationTests.cs
│   ├── Should_Handle_Deep_Hierarchy()
│   ├── Should_Handle_Large_Directory()
│   ├── Should_Handle_Many_Small_Files()
│   ├── Should_Handle_Concurrent_Operations()
│   ├── Should_Handle_Unicode_Filenames()
│   ├── Should_Handle_Long_Paths()
│   └── Should_Handle_Special_Characters()
│
└── LocalAtomicWriteIntegrationTests.cs
    ├── Should_Survive_Process_Crash()
    ├── Should_Handle_Disk_Full()
    └── Should_Handle_Power_Failure()
```

### E2E Tests

```
Tests/E2E/FileSystem/
├── LocalFSE2ETests.cs
│   ├── Should_Read_File_Via_Agent_Tool()
│   ├── Should_Write_File_Via_Agent_Tool()
│   ├── Should_List_Directory_Via_Agent_Tool()
│   └── Should_Handle_Real_Codebase()
```

### Performance Benchmarks

| Benchmark | Target | Maximum |
|-----------|--------|---------|
| 1KB read | 2ms | 5ms |
| 1MB read | 25ms | 50ms |
| Atomic write 1KB | 5ms | 10ms |
| Enumerate 1000 | 25ms | 50ms |

---

## User Verification Steps

### Scenario 1: Read File

1. Create test file
2. Read via local FS
3. Verify: Content correct

### Scenario 2: Write File

1. Write via local FS
2. Read back
3. Verify: Matches

### Scenario 3: Atomic Write

1. Start write
2. Simulate failure
3. Verify: Original unchanged

### Scenario 4: Enumeration

1. Create directory tree
2. Enumerate
3. Verify: All files found

### Scenario 5: Security

1. Try path traversal
2. Verify: Blocked

---

## Implementation Prompt

### File Structure

```
src/AgenticCoder.Infrastructure/
├── FileSystem/
│   └── Local/
│       ├── LocalFileSystem.cs
│       ├── LocalFSOptions.cs
│       ├── AtomicFileWriter.cs
│       ├── EncodingDetector.cs
│       └── LargeFileReader.cs
```

### LocalFileSystem Class

```csharp
namespace AgenticCoder.Infrastructure.FileSystem.Local;

public sealed class LocalFileSystem : IRepoFS
{
    private readonly LocalFSOptions _options;
    private readonly PathValidator _validator;
    private readonly EncodingDetector _encoding;
    
    public async Task<string> ReadFileAsync(
        string path,
        CancellationToken ct = default)
    {
        var fullPath = ResolvePath(path);
        _validator.ValidatePath(fullPath, _options.RootPath);
        
        var encoding = await _encoding.DetectAsync(fullPath, ct);
        return await File.ReadAllTextAsync(fullPath, encoding, ct);
    }
    
    public async Task WriteFileAsync(
        string path,
        string content,
        CancellationToken ct = default)
    {
        var fullPath = ResolvePath(path);
        _validator.ValidatePath(fullPath, _options.RootPath);
        
        await _atomicWriter.WriteAsync(fullPath, content, ct);
    }
}
```

### Error Codes

| Code | Meaning |
|------|---------|
| ACODE-LFS-001 | File not found |
| ACODE-LFS-002 | Permission denied |
| ACODE-LFS-003 | Path traversal |
| ACODE-LFS-004 | Lock timeout |
| ACODE-LFS-005 | Atomic write failed |

### Implementation Checklist

1. [ ] Create LocalFileSystem
2. [ ] Implement reading
3. [ ] Implement atomic writing
4. [ ] Implement enumeration
5. [ ] Add encoding detection
6. [ ] Add path validation
7. [ ] Add error handling
8. [ ] Write tests

### Rollout Plan

1. **Phase 1:** Basic read/write
2. **Phase 2:** Atomic writes
3. **Phase 3:** Enumeration
4. **Phase 4:** Encoding detection
5. **Phase 5:** Error handling

---

**End of Task 014.a Specification**