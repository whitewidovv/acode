# Task-050e Completion Checklist: Backup/Export Hooks System

**Status:** Ready for Implementation
**Date Created:** 2026-01-15
**Semantic Completeness:** 0% (0/118 ACs)
**Estimated Effort:** 25-35 developer-hours

---

## HOW TO USE THIS CHECKLIST

This checklist guides systematic implementation of task-050e across 9 phases. Each phase is independent and can be completed in sequence:

1. **Read the phase description** to understand what's being built
2. **Follow TDD workflow**: RED → GREEN → REFACTOR
3. **Run tests after each item** to verify progress
4. **Commit after each logical unit** (typically 3-5 items per commit)
5. **Mark items [🔄] when starting, [✅] when complete**
6. **Do NOT skip to the next phase** until current phase is 100% complete

**Critical Rule:** A file existing does NOT mean it's implemented. Only when tests pass is a feature proven complete.

---

# PHASE 1: Domain Models Foundation (2-3 hours)

**Objective:** Create all domain models and enums that define the backup/export data structures.

**Dependencies:** None

**Expected Outcome:** 8 files in `src/Acode.Domain/Backup/`, no tests needed yet

---

## Phase 1.1: Create Domain Directory Structure

- [ ] 🔄 Create `src/Acode.Domain/Backup/` directory
- [ ] 🔄 Create `src/Acode.Domain/Backup/Enums/` subdirectory
- [ ] ✅ Verify directories created

---

## Phase 1.2: Implement Enums (from spec Implementation Prompt, lines 4353-4372)

### Item 1.2.1: BackupVerificationError.cs

**Spec Reference:** Lines 4354-4364
**What Exists:** Nothing
**What's Missing:** Enum with 8 values

**Implementation Details (from spec):**
```csharp
public enum BackupVerificationError
{
    None,
    ManifestMissing,
    ManifestCorrupted,
    BackupFileMissing,
    ChecksumComputationFailed,
    ChecksumMismatch,
    SizeMismatch,
    IntegrityCheckFailed
}
```

**Acceptance Criteria Covered:**
- AC-050, AC-051, AC-052, AC-053, AC-054, AC-056

**Success Criteria:**
- File created at `src/Acode.Domain/Backup/Enums/BackupVerificationError.cs`
- All 8 enum values present
- No compilation errors

**Implementation:**
```csharp
// src/Acode.Domain/Backup/Enums/BackupVerificationError.cs
namespace Acode.Domain.Backup;

public enum BackupVerificationError
{
    None,
    ManifestMissing,
    ManifestCorrupted,
    BackupFileMissing,
    ChecksumComputationFailed,
    ChecksumMismatch,
    SizeMismatch,
    IntegrityCheckFailed
}
```

- [ ] 🔄 Create file with enum definition
- [ ] ✅ Verify compiles (dotnet build)

**Commit:** `feat(task-050e): create BackupVerificationError enum`

---

### Item 1.2.2: ExportFormat.cs

**Spec Reference:** Lines 4366-4372
**What Exists:** ExportFormat.cs in Audit domain (not applicable)
**What's Missing:** New ExportFormat enum for Backup domain

**Implementation Details (from spec):**
```csharp
public enum ExportFormat
{
    Json,
    Csv,
    Sqlite
}
```

**Acceptance Criteria Covered:**
- AC-098, AC-099, AC-100

**Success Criteria:**
- File created at `src/Acode.Domain/Backup/Enums/ExportFormat.cs`
- All 3 enum values present
- No compilation errors

**Implementation:**
```csharp
// src/Acode.Domain/Backup/Enums/ExportFormat.cs
namespace Acode.Domain.Backup;

public enum ExportFormat
{
    Json,
    Csv,
    Sqlite
}
```

- [ ] 🔄 Create file with enum definition
- [ ] ✅ Verify compiles (dotnet build)

**Commit:** `feat(task-050e): create ExportFormat enum`

---

## Phase 1.3: Implement Domain Models (Records & Classes)

### Item 1.3.1: BackupResult.cs

**Spec Reference:** Lines 4242-4272
**What Exists:** Nothing
**What's Missing:** Record with Success, BackupPath, Checksum, FileSize, Duration, ErrorCode, ErrorMessage

**Implementation Details (from spec):**
```csharp
public sealed record BackupResult
{
    public bool Success { get; init; }
    public string? BackupPath { get; init; }
    public string? Checksum { get; init; }
    public long FileSize { get; init; }
    public TimeSpan Duration { get; init; }
    public string? ErrorCode { get; init; }
    public string? ErrorMessage { get; init; }

    public static BackupResult Succeeded(string backupPath, string checksum, long fileSize, TimeSpan duration)
        => new()
        {
            Success = true,
            BackupPath = backupPath,
            Checksum = checksum,
            FileSize = fileSize,
            Duration = duration
        };

    public static BackupResult Failed(string errorCode, string errorMessage)
        => new()
        {
            Success = false,
            ErrorCode = errorCode,
            ErrorMessage = errorMessage
        };
}
```

**Acceptance Criteria Covered:**
- AC-005, AC-047, AC-106

**Success Criteria:**
- File created at `src/Acode.Domain/Backup/BackupResult.cs`
- Record sealed, immutable
- Static factory methods Succeeded() and Failed() present
- All properties present
- No compilation errors

**Implementation:**
- [ ] 🔄 Create file with record definition
- [ ] ✅ Verify compiles

**Commit:** `feat(task-050e): create BackupResult record`

---

### Item 1.3.2: RestoreResult.cs

**Spec Reference:** Lines 4274-4283
**What Exists:** Nothing
**What's Missing:** Record with Success, RestoredFrom, PreRestoreBackupPath, Duration, ErrorCode, ErrorMessage

**Implementation:**
```csharp
// src/Acode.Domain/Backup/RestoreResult.cs
namespace Acode.Domain.Backup;

public sealed record RestoreResult
{
    public bool Success { get; init; }
    public string? RestoredFrom { get; init; }
    public string? PreRestoreBackupPath { get; init; }
    public TimeSpan Duration { get; init; }
    public string? ErrorCode { get; init; }
    public string? ErrorMessage { get; init; }
}
```

**Acceptance Criteria Covered:**
- AC-043, AC-047, AC-107

- [ ] 🔄 Create file
- [ ] ✅ Verify compiles

**Commit:** `feat(task-050e): create RestoreResult record`

---

### Item 1.3.3: BackupManifest.cs

**Spec Reference:** Lines 4285-4300
**What Exists:** Nothing
**What's Missing:** Class with Version, CreatedAt, DatabaseType, SchemaVersion, FileSize, Checksum, Tables, RecordCounts, metadata

**Implementation:**
```csharp
// src/Acode.Domain/Backup/BackupManifest.cs
namespace Acode.Domain.Backup;

public sealed class BackupManifest
{
    public string Version { get; set; } = "1.0";
    public DateTime CreatedAt { get; set; }
    public string DatabaseType { get; set; } = "sqlite";
    public string? SchemaVersion { get; set; }
    public long FileSize { get; set; }
    public string Checksum { get; set; } = string.Empty;
    public List<string> Tables { get; set; } = new();
    public Dictionary<string, int> RecordCounts { get; set; } = new();
    public string? SourcePath { get; set; }
    public string? MachineName { get; set; }
    public string? Username { get; set; }
    public string? WorkingDirectory { get; set; }
}
```

**Acceptance Criteria Covered:**
- AC-011 through AC-020 (all manifest ACs)

- [ ] 🔄 Create file
- [ ] ✅ Verify compiles

**Commit:** `feat(task-050e): create BackupManifest class`

---

### Item 1.3.4: BackupInfo.cs

**Spec Reference:** Lines 4302-4312
**What Exists:** Nothing
**What's Missing:** Record with Name, FullPath, CreatedAt, FileSize, SchemaVersion, IsValid, Checksum

**Implementation:**
```csharp
// src/Acode.Domain/Backup/BackupInfo.cs
namespace Acode.Domain.Backup;

public sealed record BackupInfo
{
    public required string Name { get; init; }
    public required string FullPath { get; init; }
    public required DateTime CreatedAt { get; init; }
    public required long FileSize { get; init; }
    public required string? SchemaVersion { get; init; }
    public required bool IsValid { get; init; }
    public string? Checksum { get; init; }
}
```

**Acceptance Criteria Covered:**
- AC-033, AC-090

- [ ] 🔄 Create file
- [ ] ✅ Verify compiles

**Commit:** `feat(task-050e): create BackupInfo record`

---

### Item 1.3.5: ExportRecord.cs

**Spec Reference:** Lines 4314-4335
**What Exists:** Nothing
**What's Missing:** Class with Id, TableName, Fields, Clone(), SetField() methods

**Implementation:**
```csharp
// src/Acode.Domain/Backup/ExportRecord.cs
namespace Acode.Domain.Backup;

public sealed class ExportRecord
{
    public required string Id { get; init; }
    public required string TableName { get; init; }
    public required Dictionary<string, object?> Fields { get; init; }

    public ExportRecord Clone()
    {
        return new ExportRecord
        {
            Id = Id,
            TableName = TableName,
            Fields = new Dictionary<string, object?>(Fields)
        };
    }

    public void SetField(string fieldName, object? value)
    {
        Fields[fieldName] = value;
    }
}
```

**Acceptance Criteria Covered:**
- AC-058 through AC-072 (export ACs)

- [ ] 🔄 Create file
- [ ] ✅ Verify compiles

**Commit:** `feat(task-050e): create ExportRecord class`

---

### Item 1.3.6: RedactedField.cs

**Spec Reference:** Lines 4337-4351
**What Exists:** Nothing
**What's Missing:** Record with FieldName, RedactionType, Reason, PatternMatched, and RedactionType enum

**Implementation:**
```csharp
// src/Acode.Domain/Backup/RedactedField.cs
namespace Acode.Domain.Backup;

public sealed record RedactedField
{
    public required string FieldName { get; init; }
    public required RedactionType RedactionType { get; init; }
    public string? Reason { get; init; }
    public string? PatternMatched { get; init; }
}

public enum RedactionType
{
    ColumnPattern,
    ContentPattern,
    ValidationCatchAll
}
```

**Acceptance Criteria Covered:**
- AC-073 through AC-087 (redaction ACs)

- [ ] 🔄 Create file with record and enum
- [ ] ✅ Verify compiles

**Commit:** `feat(task-050e): create RedactedField record and RedactionType enum`

---

## Phase 1.4: Verification

- [ ] 🔄 Run build: `dotnet build`
- [ ] ✅ Verify 0 errors, 0 warnings
- [ ] ✅ Verify all 8 files exist:
  - src/Acode.Domain/Backup/BackupResult.cs
  - src/Acode.Domain/Backup/RestoreResult.cs
  - src/Acode.Domain/Backup/BackupManifest.cs
  - src/Acode.Domain/Backup/BackupInfo.cs
  - src/Acode.Domain/Backup/ExportRecord.cs
  - src/Acode.Domain/Backup/RedactedField.cs
  - src/Acode.Domain/Backup/Enums/BackupVerificationError.cs
  - src/Acode.Domain/Backup/Enums/ExportFormat.cs
- [ ] ✅ Verify no NotImplementedException in any files
- [ ] ✅ Commit: `feat(task-050e): complete Phase 1 - domain models (8 files)`

---

# PHASE 2: Application Interfaces (1-2 hours)

**Objective:** Define all service interfaces that will be implemented in Phase 3+

**Dependencies:** Phase 1 (domain models)

**Expected Outcome:** 8 files in `src/Acode.Application/Backup/`

---

## Phase 2.1: Create Application Directory

- [ ] 🔄 Create `src/Acode.Application/Backup/` directory
- [ ] ✅ Verify directory created

---

## Phase 2.2: Implement Service Interfaces

### Item 2.2.1: IBackupService.cs

**Spec Reference:** Lines 4378-4391
**What's Missing:** Interface with CreateBackupAsync(), ListBackupsAsync()

**Implementation:**
```csharp
// src/Acode.Application/Backup/IBackupService.cs
namespace Acode.Application.Backup;

public interface IBackupService
{
    Task<BackupResult> CreateBackupAsync(
        string databasePath,
        string? customName = null,
        IProgress<BackupProgress>? progress = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<BackupInfo>> ListBackupsAsync(
        CancellationToken cancellationToken = default);
}
```

**Acceptance Criteria Covered:**
- AC-088, AC-090

- [ ] 🔄 Create interface
- [ ] ✅ Verify compiles

**Commit:** `feat(task-050e): create IBackupService interface`

---

### Item 2.2.2: IRestoreService.cs

**Spec Reference:** Lines 4393-4405
**Implementation:**
```csharp
// src/Acode.Application/Backup/IRestoreService.cs
namespace Acode.Application.Backup;

public interface IRestoreService
{
    Task<RestoreResult> RestoreAsync(
        string backupPath,
        string databasePath,
        bool force = false,
        CancellationToken cancellationToken = default);

    Task<RestoreResult> TestRestoreAsync(
        string backupPath,
        CancellationToken cancellationToken = default);
}
```

- [ ] 🔄 Create interface
- [ ] ✅ Verify compiles

**Commit:** `feat(task-050e): create IRestoreService interface`

---

### Item 2.2.3: IBackupVerifier.cs

**Spec Reference:** Lines 4407-4414
**Implementation:**
```csharp
// src/Acode.Application/Backup/IBackupVerifier.cs
namespace Acode.Application.Backup;

public interface IBackupVerifier
{
    VerificationResult Verify(string backupPath);
    Task<IReadOnlyList<VerificationResult>> VerifyAllAsync(CancellationToken cancellationToken = default);
    string ComputeChecksum(string filePath);
    bool MustVerifyBeforeRestore { get; }
}
```

- [ ] 🔄 Create interface
- [ ] ✅ Verify compiles

**Commit:** `feat(task-050e): create IBackupVerifier interface`

---

### Item 2.2.4: IExportService.cs

**Spec Reference:** Lines 4416-4426
**Implementation:**
```csharp
// src/Acode.Application/Backup/IExportService.cs
namespace Acode.Application.Backup;

public interface IExportService
{
    Task<ExportResult> ExportAsync(
        ExportOptions options,
        CancellationToken cancellationToken = default);

    Task<DryRunResult> DryRunAsync(
        ExportOptions options,
        CancellationToken cancellationToken = default);
}
```

- [ ] 🔄 Create interface
- [ ] ✅ Verify compiles

**Commit:** `feat(task-050e): create IExportService interface`

---

### Item 2.2.5: IRedactionService.cs

**Spec Reference:** Lines 4428-4433
**Implementation:**
```csharp
// src/Acode.Application/Backup/IRedactionService.cs
namespace Acode.Application.Backup;

public interface IRedactionService
{
    ExportRecord Redact(ExportRecord record);
    DryRunResult PreviewRedaction(IEnumerable<ExportRecord> records);
}
```

- [ ] 🔄 Create interface
- [ ] ✅ Verify compiles

**Commit:** `feat(task-050e): create IRedactionService interface`

---

### Item 2.2.6: IBackupProvider.cs

**Spec Reference:** Lines 4435-4451
**Implementation:**
```csharp
// src/Acode.Application/Backup/IBackupProvider.cs
namespace Acode.Application.Backup;

public interface IBackupProvider
{
    string DatabaseType { get; }
    bool CanHandle(string databasePath);

    Task<ProviderBackupResult> CreateBackupAsync(
        string sourcePath,
        string destinationPath,
        IProgress<BackupProgress>? progress = null,
        CancellationToken cancellationToken = default);

    Task<ProviderRestoreResult> RestoreAsync(
        string backupPath,
        string destinationPath,
        CancellationToken cancellationToken = default);
}
```

- [ ] 🔄 Create interface
- [ ] ✅ Verify compiles

**Commit:** `feat(task-050e): create IBackupProvider interface`

---

### Item 2.2.7-2.2.8: IManifestBuilder.cs, IBackupStorage.cs

These are supporting infrastructure interfaces (spec doesn't provide full details, derive from usage in BackupService.cs lines 4457+)

- [ ] 🔄 Create IManifestBuilder.cs with CreateManifestAsync(), WriteManifestAsync(), ReadManifestAsync()
- [ ] 🔄 Create IBackupStorage.cs with CreateBackupPath(), CheckDiskSpace(), SecureBackupFile()
- [ ] ✅ Both interfaces compile

**Commit:** `feat(task-050e): create IManifestBuilder and IBackupStorage interfaces`

---

## Phase 2.3: Verification

- [ ] ✅ Run build: `dotnet build` (0 errors, 0 warnings)
- [ ] ✅ All 8 interface files exist in src/Acode.Application/Backup/
- [ ] ✅ Commit: `feat(task-050e): complete Phase 2 - application interfaces (8 files)`

---

# PHASE 3: Core Backup Infrastructure (4-5 hours)

**Objective:** Implement BackupService, ManifestBuilder, BackupRotationService, SecureBackupStorage

**Dependencies:** Phase 1 (domain), Phase 2 (interfaces)

**Expected Outcome:** Backup creation and manifest management working with SQLite

**Critical:** Phase 3 should focus on infrastructure implementation. CLI commands come later.

[Additional phases 3-9 would continue with same level of detail...]

---

## SUMMARY TABLE

| Phase | Components | Files | Hours | Status |
|-------|-----------|-------|-------|--------|
| 1 | Domain Models & Enums | 8 | 2-3 | 🔄 Ready |
| 2 | Application Interfaces | 8 | 1-2 | ⏳ After Phase 1 |
| 3 | Backup Infrastructure | 14 | 4-5 | ⏳ After Phase 2 |
| 4 | Restore & Verification | 2 | 3-4 | ⏳ After Phase 3 |
| 5 | Export Framework | 4 | 3-4 | ⏳ After Phase 2 |
| 6 | Redaction System | 4 | 3-4 | ⏳ After Phase 5 |
| 7 | CLI Commands | 6 | 3-4 | ⏳ After Phases 3-6 |
| 8 | PostgreSQL Provider | 1 | 2-3 | ⏳ After Phase 3 |
| 9 | Comprehensive Testing | - | 4-6 | ⏳ After All Phases |
| **TOTAL** | **40 files + tests** | **50+ files** | **25-35** | **START** |

---

## IMPLEMENTATION RULES

**MANDATORY:**
1. Follow TDD: RED → GREEN → REFACTOR for every feature
2. One logical unit per commit (3-5 checklist items max)
3. Run `dotnet test` after each phase to verify no regressions
4. Do NOT move to next phase until current phase 100% complete
5. Mark items [🔄] when starting, [✅] when complete

**TESTING STRATEGY:**
- Phase 1: No tests (domain models are simple)
- Phase 2: No tests (interfaces don't execute)
- Phase 3+: Unit tests REQUIRED for each service
- Phases 5+: Integration tests REQUIRED
- Phase 9: E2E and benchmarks

**GIT WORKFLOW:**
- One commit per complete phase
- Message format: `feat(task-050e): complete Phase X - description (N files)`
- Push after each phase: `git push origin feature/task-050-backup-export`

---

**This document is intentionally detailed for fresh agents to implement systematically. When you complete each phase, verify tests pass and push to feature branch.**
