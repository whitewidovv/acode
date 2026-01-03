# Task 014.a: Local FS Implementation

**Priority:** P0 – Critical  
**Tier:** S – Core Infrastructure  
**Complexity:** 8 (Fibonacci points)  
**Phase:** Phase 3 – Intelligence Layer  
**Dependencies:** Task 014 (RepoFS Abstraction)  

---

## Description

Task 014.a implements the local file system provider for RepoFS. This is the primary file system implementation. It handles files on the local disk.

The local file system is the most common deployment. Developers run acode directly on their machine. Files are in a local directory. This implementation makes that work.

The implementation wraps System.IO. It provides async operations. It handles encoding detection. It normalizes paths. It enforces security boundaries.

File reading uses streaming for large files. Small files are read entirely. Large files stream to avoid memory pressure. The threshold is configurable.

File writing uses atomic operations. Content goes to a temp file first. Then rename replaces the original. This prevents partial writes.

Directory enumeration is lazy. Files are yielded as discovered. This enables processing before enumeration completes. Memory stays constant regardless of directory size.

Encoding detection uses BOM and heuristics. UTF-8 is the default. BOM overrides default. Binary files are detected and handled appropriately.

Case sensitivity depends on the operating system. Windows is case-insensitive. Linux is case-sensitive. The implementation respects the native behavior.

Locking is handled appropriately. Read locks allow concurrent reads. Write locks are exclusive. Deadlock detection prevents hangs.

Error handling maps OS errors to domain errors. ENOENT becomes FileNotFoundException. EACCES becomes AccessDeniedException. Each error has clear semantics.

Logging captures all operations. File path (redacted if needed). Operation type. Duration. Success or failure. This enables debugging and auditing.

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

### File Reading

- FR-001: ReadFileAsync MUST work
- FR-002: UTF-8 default encoding
- FR-003: BOM detection MUST work
- FR-004: ReadLinesAsync MUST work
- FR-005: ReadBytesAsync MUST work
- FR-006: Large file streaming MUST work
- FR-007: Binary file detection MUST work

### File Writing

- FR-008: WriteFileAsync MUST work
- FR-009: Atomic write MUST work
- FR-010: Temp file in same directory
- FR-011: Rename replaces original
- FR-012: WriteLinesAsync MUST work
- FR-013: WriteBytesAsync MUST work
- FR-014: Create parent directories

### File Deletion

- FR-015: DeleteFileAsync MUST work
- FR-016: Non-existent: no error
- FR-017: DeleteDirectoryAsync MUST work
- FR-018: Recursive delete MUST work
- FR-019: Non-empty directory handling

### Directory Enumeration

- FR-020: EnumerateFilesAsync MUST work
- FR-021: Lazy enumeration MUST work
- FR-022: Recursive MUST work
- FR-023: Pattern filtering MUST work
- FR-024: Hidden files handling
- FR-025: EnumerateDirectoriesAsync MUST work

### Metadata

- FR-026: ExistsAsync MUST work
- FR-027: GetMetadataAsync MUST work
- FR-028: Size returned
- FR-029: LastModified returned
- FR-030: CreatedAt returned
- FR-031: IsDirectory flag

### Path Handling

- FR-032: Path normalization MUST work
- FR-033: Forward slashes MUST work
- FR-034: Backslashes MUST work
- FR-035: Trailing slashes handled
- FR-036: Root path combining

### Security

- FR-037: Root boundary enforced
- FR-038: Path traversal prevented
- FR-039: Null bytes rejected
- FR-040: Invalid characters rejected

### Error Handling

- FR-041: FileNotFoundException for missing
- FR-042: AccessDeniedException for denied
- FR-043: DirectoryNotFoundException mapping
- FR-044: IOException for disk errors
- FR-045: Clear error messages

### Locking

- FR-046: Shared read locks
- FR-047: Exclusive write locks
- FR-048: Lock timeout configurable
- FR-049: Retry on lock conflict

### Encoding

- FR-050: UTF-8 default
- FR-051: UTF-8-BOM detected
- FR-052: UTF-16 detected
- FR-053: Binary file detection
- FR-054: Explicit encoding override

---

## Non-Functional Requirements

### Performance

- NFR-001: 1KB read < 5ms
- NFR-002: 1MB read < 50ms
- NFR-003: Enumeration < 50ms/1000 files
- NFR-004: Write < 20ms/MB

### Reliability

- NFR-005: No partial writes
- NFR-006: Temp file cleanup
- NFR-007: Lock release on error

### Security

- NFR-008: Path validation
- NFR-009: No privilege escalation
- NFR-010: Boundary enforcement

### Usability

- NFR-011: Clear errors
- NFR-012: Good logging
- NFR-013: Helpful exceptions

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